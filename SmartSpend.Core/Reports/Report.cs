using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Reports;

public class Report : IDisplayReport, IComparer<RowLabel>
{
    #region Configuration Properties

    public string Name { get; set; } = "Report";

    public string Description { get; set; }

    public string Definition { get; set; }

    public IEnumerable<NamedQuery> Source { get; set; }

    public bool WithMonthColumns { get; set; } = false;

    public bool WithTotalColumn { get; set; } = true;

    public int SkipLevels { get; set; }

    public int? NumLevels { get; set; } = 1;

    public int DisplayLevelAdjustment { get; set; }

    public enum SortOrders { NameAscending, TotalAscending, TotalDescending };

    public SortOrders SortOrder { get; set; } = SortOrders.TotalDescending;

    public ColumnLabel OrderingColumn
    {
        get
        {
            if (WithTotalColumn)
                return TotalColumn;

            return ColumnLabels.First();
        }
    }

    #endregion

    #region Informative Properties

    public decimal this[ColumnLabel column, RowLabel row]
    {
        get
        {
            if (column.IsCalculated)
            {
                var cols = RowDetails(row);
                cols["GRANDTOTAL"] = GrandTotal;
                return column.Custom(cols);
            }
            else
                return Table[column, row];
        }
    }

    public IEnumerable<RowLabel> RowLabelsOrdered => Table.RowLabels.OrderBy(x => x, this);

    public IEnumerable<RowLabel> RowLabels => Table.RowLabels.OrderBy(x => x);

    public IEnumerable<ColumnLabel> ColumnLabelsFiltered => ColumnLabels.Where(x => WithTotalColumn || !x.IsTotal);

    public IEnumerable<ColumnLabel> ColumnLabels => Table.ColumnLabels.OrderBy(x => x);

    public static RowLabel TotalRow => RowLabel.Total;

    public static ColumnLabel TotalColumn => ColumnLabel.Total;

    public decimal GrandTotal => this[TotalColumn, TotalRow];

    public double YearProgress { get; set; }

    public int MaxLevels { get; set; }

    #endregion

    #region Public Methods

    public void AddCustomColumn(ColumnLabel column)
    {
        if (column.Custom == null)
            throw new ArgumentOutOfRangeException(nameof(column), "Column must contain a custom function");

        column.IsCalculated = true;
        column.IsTotal = false;
        Table.ColumnLabels.Add(column);
    }

    public void LoadFrom(ReportDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.Name))
            Name = definition.Name;

        if (!string.IsNullOrEmpty(definition.SortOrder) && Enum.TryParse<Report.SortOrders>(definition.SortOrder, out Report.SortOrders order))
            SortOrder = order;

        if (definition.WithMonthColumns.HasValue)
            WithMonthColumns = definition.WithMonthColumns.Value;

        if (definition.WithTotalColumn.HasValue)
            WithTotalColumn = definition.WithTotalColumn.Value;

        if (definition.SkipLevels.HasValue)
            SkipLevels = definition.SkipLevels.Value;

        if (definition.NumLevels.HasValue)
            NumLevels = definition.NumLevels.Value;

        if (definition.DisplayLevelAdjustment.HasValue)
            DisplayLevelAdjustment = definition.DisplayLevelAdjustment.Value;

        if (!string.IsNullOrEmpty(definition.slug))
            Definition = definition.slug;
    }

    public void Build()
    {
        if (NumLevels.HasValue && NumLevels.Value < 1)
            throw new ArgumentOutOfRangeException(nameof(NumLevels), "Must be 1 or greater or null");

        if (Source == null)
            throw new ArgumentOutOfRangeException(nameof(Source), "Must set a source");

        foreach (var query in Source)
            BuildPhase_Group(query);
    }

    public Report TakeSlice(string rowname)
    {
        var result = new Report() { Name = rowname, Description = Description };

        var findrow = RowLabels.Where(x => x.Name == rowname && !x.IsTotal);
        if (!findrow.Any())
            return result;

        var sliceparent = findrow.Single();

        var includedrows = RowLabels.Where(x => x.DescendsFrom(sliceparent)).ToList();

        foreach (var column in ColumnLabelsFiltered)
            if (!column.IsCalculated)
            {
                foreach (var row in includedrows)
                {
                    var value = this[column, row];
                    result.Table[column, row] = value;

                    if (row.Parent == sliceparent)
                        result.Table[column, TotalRow] += value;
                }
            }
            else
                result.AddCustomColumn(column);

        result.NumLevels = NumLevels - 1;

        return result;
    }

    public Report TakeSliceExcept(IEnumerable<string> rownames)
    {
        var result = new Report();

        var excludedparentrows = RowLabels.Where(x => rownames.Contains(x.Name) && x.Parent == null && !x.IsTotal);

        var excluded = excludedparentrows.SelectMany(x => RowLabels.Where(y => y.IsTotal || y.Equals(x) || y.DescendsFrom(x)));

        var includedrows = RowLabels.Except(excluded);

        foreach (var column in ColumnLabelsFiltered)
            if (!column.IsCalculated)
            {
                foreach (var row in includedrows)
                {
                    var value = this[column, row];
                    result.Table[column, row] = value;

                    if (row.Parent == null && includedrows.Contains(row))
                        result.Table[column, TotalRow] += value;
                }
            }
            else
                result.AddCustomColumn(column);

        result.Name = string.Join(',',rownames);
        result.NumLevels = NumLevels;

        return result;
    }

    public void PruneToLevel(int newlevel)
    {
        if (!NumLevels.HasValue)
            throw new ApplicationException("Report has no set level");

        if (newlevel < 1 || newlevel > NumLevels)
            throw new ArgumentException("Invalid level", nameof(newlevel));

        var minuslevels = NumLevels.Value - newlevel;
        foreach (var row in RowLabels.Where(x=>!x.IsTotal))
            row.Level -= minuslevels;

        Table.RowLabels.RemoveWhere(x => x.Level < 0);

        NumLevels = newlevel;
    }

    public string ToJson()
    {
        string result = string.Empty;

        using (var stream = new MemoryStream())
        {
            using var writer = new Utf8JsonWriter(stream, options: new JsonWriterOptions() { Indented = true });
            writer.WriteStartArray();

            foreach (var line in RowLabelsOrdered)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("Name");
                writer.WriteStringValue(line.Name ?? "-");
                writer.WritePropertyName("ID");
                writer.WriteStringValue(line.UniqueID);
                writer.WritePropertyName("IsTotal");
                writer.WriteBooleanValue(line.IsTotal);
                writer.WritePropertyName("Level");
                writer.WriteNumberValue(line.Level);

                foreach (var col in ColumnLabelsFiltered)
                {
                    var val = this[col, line];
                    var name = col.ToString();
                    if (col.DisplayAsPercent)
                        name += "%";

                    writer.WritePropertyName(name);
                    writer.WriteNumberValue(val);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.Flush();

            var bytes = stream.ToArray();
            result = Encoding.UTF8.GetString(bytes);
        }

        return result;

    }

    #endregion

    #region Fields
    readonly Table<ColumnLabel, RowLabel, decimal> Table = new();
    #endregion

    #region Internal Methods
    
    class CellGroupDto
    {
        public string Row { get; set; }
        public int? Column { get; set; } = null;

        public string FilteredRowName => string.IsNullOrEmpty(Row) ? "[Blank]" : Row;
    };

    class CellTotalDto
    {
        public CellGroupDto Location { get; set; }

        public decimal Value { get; set; }
    };


    private void BuildPhase_Group(NamedQuery source)
    {
        IQueryable<IGrouping<CellGroupDto, IReportable>> groups;
        if (WithMonthColumns)
            groups = source.Query.GroupBy(x => new CellGroupDto() { Row = x.Category, Column = x.Timestamp.Month });
        else
            groups = source.Query.GroupBy(x => new CellGroupDto() { Row = x.Category });

        var selected = groups.Select(g => new CellTotalDto() { Location = g.Key, Value = g.Sum(y => y.Amount) });

        BuildPhase_Place(cells: selected, oquery: source);

        CollectorRows = RowLabels.Where(x => x.Collector != null).ToLookup(x => x.UniqueID.Count(y => y == ':'));
    }

    private ILookup<int,RowLabel> CollectorRows = null;

    private static Dictionary<int,ColumnLabel> MonthColumnLabels { get; } = Enumerable.Range(1,12).ToDictionary(x=>x,x=>new ColumnLabel() 
        { 
            UniqueID = x.ToString("D2"), 
            Name = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(x)
        }
    );

    private void BuildPhase_Place(IQueryable<CellTotalDto> cells, NamedQuery oquery)
    {
        var seriescolumn = string.IsNullOrEmpty(oquery?.Name) ? 
            null : 
            new ColumnLabel() 
            { 
                Name = oquery.Name, 
                UniqueID = oquery.Name, 
                LeafNodesOnly = oquery.LeafRowsOnly,
                IsSeries = true
            };

        if (null != seriescolumn)
            Table.ColumnLabels.Add(seriescolumn);

        var collectorregex = new Regex("(.+?)\\[(.+?)\\]");

        foreach (var cell in cells)
        {
            var keysplit = cell.Location.FilteredRowName.Split(':');
            if (keysplit.Length == SkipLevels)
                keysplit = keysplit.AsEnumerable().Append("[Blank]").ToArray();
            var keys = keysplit.Skip(SkipLevels).ToList();
            if (keys.Any())
            {
                var match = collectorregex.Match(keys[^1]);
                string collector = null;
                if (match.Success)
                {
                    keys[^1] = match.Groups[1].Value;

                    collector = match.Groups[2].Value;
                }

                var id = string.Join(':', keys) + (!oquery.LeafRowsOnly ? ":" : string.Empty);

                var name = oquery.LeafRowsOnly ? id : null;

                var row = RowLabels.Where(x => x.UniqueID == id).SingleOrDefault();

                if (row == null)
                    row = new RowLabel() { Name = name, UniqueID = id, Collector = collector };

                ColumnLabel monthcolumn = null;
                if (WithMonthColumns && cell.Location.Column.HasValue)
                {
                    monthcolumn = MonthColumnLabels[cell.Location.Column.Value];
                    Table[monthcolumn, row] += cell.Value;
                }

                if (seriescolumn != null)
                    Table[seriescolumn, row] += cell.Value;

                Table[TotalColumn, row] += cell.Value;

                if (!oquery.LeafRowsOnly)
                    BuildPhase_Propagate(row: row, column: monthcolumn, seriescolumn: seriescolumn, amount: cell.Value);
            }
        }

        BuildPhase_Prune();
    }

    private void BuildPhase_Propagate(decimal amount, RowLabel row, ColumnLabel column = null, ColumnLabel seriescolumn = null)
    {
        var split = row.UniqueID.Split(':');
        var parentsplit = split.SkipLast(1);

        if (parentsplit.Any())
        {
            var parentid = string.Join(':', parentsplit);

            var parentrow = new RowLabel() { Name = parentsplit.Last(), UniqueID = parentid };
            if (Table.RowLabels.TryGetValue(parentrow, out var foundrow))
                parentrow = foundrow;
            row.Parent = parentrow;

            Table[TotalColumn, parentrow] += amount;
            if (column != null)
                Table[column, parentrow] += amount;
            if (seriescolumn != null)
                Table[seriescolumn, parentrow] += amount;

            BuildPhase_Propagate(amount: amount, row: parentrow, column: column, seriescolumn: seriescolumn);

            row.Level = parentrow.Level - 1;

            if (seriescolumn != null && CollectorRows != null && CollectorRows.Any())
            {
                var peerstart = parentid;

                var depth = 1 + parentid.Count(x => x == ':');
                var collectors = CollectorRows[depth];
                var foundrows = collectors.Where(x => x.UniqueID.StartsWith(parentid));

                foreach(var collectorrow in foundrows)
                {
                    var rule = collectorrow.Collector;
                    var isnotlist = rule.StartsWith('^');
                    if (isnotlist)
                        rule = rule[1..];
                    var categories = rule.Split(';');

                    if (categories.Contains(split[^1]) ^ isnotlist && collectorrow.UniqueID != row.UniqueID)
                    {
                        Table[seriescolumn, collectorrow] += amount;
                    }
                }
            }
        }
        else
        {
            Table[TotalColumn, TotalRow] += amount;
            if (column != null)
                Table[column, TotalRow] += amount;
            if (seriescolumn != null)
                Table[seriescolumn, TotalRow] += amount;

            if (NumLevels.HasValue)
                row.Level = NumLevels.Value - 1;
        }
    }

    private void BuildPhase_Prune()
    {

        var leafnodecolumns = ColumnLabelsFiltered.Where(x => x.LeafNodesOnly);


        var pruned = new HashSet<RowLabel>();
        foreach (var row in RowLabels)
        {
            if (!leafnodecolumns.Any())
                if (string.IsNullOrEmpty(row.UniqueID.Split(':')[^1]))
                    if (row.Parent != null)
                        if (Table[TotalColumn, row] == Table[TotalColumn, row.Parent])
                            pruned.Add(row);

        }
        Table.RowLabels.RemoveWhere(x => pruned.Contains(x));

        if (RowLabels.Any())
        {
            var minrow = RowLabels.Min(x => x.Level);
            MaxLevels = 1 - minrow;

            if (!NumLevels.HasValue && RowLabels.Any())
            {
                NumLevels = MaxLevels;
                if (NumLevels > 1)
                {
                    foreach (var row in RowLabels.Where(x => !x.IsTotal))
                        row.Level += NumLevels.Value - 1;
                }
            }
        }
        else
            MaxLevels = 1;

        pruned = new HashSet<RowLabel>();
        foreach (var row in RowLabels)
        {
            if (row.Level < 0)
                pruned.Add(row);

            if (leafnodecolumns.Any())
            {
                if (leafnodecolumns.Sum(x => Table[x, row]) == 0)
                    pruned.Add(row);
                else
                {
                    row.Level = 0;
                    row.Parent = null;
                }
            }
        }

        Table.RowLabels.RemoveWhere(x => pruned.Contains(x));
    }

    Dictionary<string, decimal> RowDetails(RowLabel rowLabel) => ColumnLabels.ToDictionary(x => x.ToString(), x => Table[x, rowLabel]);

    int CompareRows(RowLabel first, RowLabel second)
    {
        if (first.IsTotal && second.IsTotal)
            return 0;
        else if (first.IsTotal)
            return 1;
        else if (second.IsTotal)
            return -1;

        if (first.Parent == second)
            return 1;
        else if (second.Parent == first)
            return -1;

        if (first.Parent == second.Parent)
        {
            var secondval = Table[OrderingColumn, second];
            var firstval = Table[OrderingColumn, first];
            switch (SortOrder)
            {
                case SortOrders.TotalAscending:
                    return secondval.CompareTo(firstval);
                case SortOrders.TotalDescending:
                    return firstval.CompareTo(secondval);
                case SortOrders.NameAscending:
                    return first.Name?.CompareTo(second.Name) ?? -1;
            }
        }


        if (first.Level < second.Level)
            return CompareRows(first.Parent, second);
        else if (second.Level < first.Level)
            return CompareRows(first, second.Parent);
        else
            return CompareRows(first.Parent, second.Parent);
    }

    int IComparer<RowLabel>.Compare(RowLabel first, RowLabel second) => CompareRows(first, second);

#endregion
}


public abstract class BaseLabel: IComparable<BaseLabel>
{
    public string UniqueID { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsTotal { get; set; }

    public bool IsSortingAfterTotal { get; set; }

    public override bool Equals(object obj)
    {
        return obj is BaseLabel label
            && ToString().Equals(label.ToString());
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ToString());
    }

    public override string ToString()
    {
        if (IsTotal)
            return "TOTAL";
        if (!string.IsNullOrEmpty(UniqueID))
            return $"ID:{UniqueID}";
        if (!string.IsNullOrEmpty(Name))
            return $"Name:{Name}";
        return "-";
    }

    public int CompareTo(BaseLabel other)
    {
        bool thisidnull = string.IsNullOrEmpty(UniqueID);
        bool otheridnull = string.IsNullOrEmpty(other.UniqueID);
        bool thisnamenull = string.IsNullOrEmpty(Name);
        bool othernamenull = string.IsNullOrEmpty(other.Name);

        int result = IsSortingAfterTotal.CompareTo(other.IsSortingAfterTotal);
        if (result == 0)
            result = IsTotal.CompareTo(other.IsTotal);
        if (result == 0) // Empty orders sort at the END
            result = thisidnull.CompareTo(otheridnull);
        if (result == 0 && !thisidnull)
            result = UniqueID.CompareTo(other.UniqueID);
        if (result == 0) 
            result = thisnamenull.CompareTo(othernamenull);
        if (result == 0 && !thisnamenull)
            result = Name.CompareTo(other.Name);

        return result;
    }       
}

public class RowLabel: BaseLabel
{
    [JsonIgnore]
    public RowLabel Parent { get; set; }

    public int Level { get; set; }

    public string Collector { get; set; } = null;

    public bool DescendsFrom(RowLabel ancestor)
    {
        if (Parent == ancestor)
            return true;
        else if (Parent == null)
            return false;
        else
            return Parent.DescendsFrom(ancestor);
    }

    public static readonly RowLabel Total = new() { IsTotal = true };
}

public class ColumnLabel : BaseLabel
{
    public bool IsCalculated { get; set; } = false;

    public bool DisplayAsPercent { get; set; } = false;

    public bool LeafNodesOnly { get; set; } = false;

    public bool IsSeries { get; set; } = false;

    [JsonIgnore]
    public Func<Dictionary<string,decimal>, decimal> Custom { get; set; }

    public static readonly ColumnLabel Total = new() { IsTotal = true };

    public int Priority
    {
        get
        {
            if (IsTotal)
                return 1;
            if (IsSeries)
                return 2;
            if (DisplayAsPercent)
                return 3;
            if (IsSortingAfterTotal)
                return 4;


            if (int.TryParse(UniqueID,out var month))
            {
                return 20 - month;
            }

            return 100;
        }
    }

}
