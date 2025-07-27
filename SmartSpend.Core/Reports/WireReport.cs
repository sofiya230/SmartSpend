using System.Collections.Generic;
using System.Linq;

namespace SmartSpend.Core.Reports;

public class WireReport : IDisplayReport
{
    public string Name { get; set; }

    public string Description { get; set; }

    public string Definition { get; set; }

    public List<ColumnLabel> ColumnLabels { get; set; }

    public List<RowLabel> RowLabels { get; set; }

    public int DisplayLevelAdjustment { get; set; }

    public decimal GrandTotal { get; set; }

    public List<WireReportLine> Lines { get; set; }

    IEnumerable<ColumnLabel> IDisplayReport.ColumnLabelsFiltered => ColumnLabels;

    IEnumerable<RowLabel> IDisplayReport.RowLabelsOrdered => RowLabels;

    decimal IDisplayReport.this[ColumnLabel column, RowLabel row] 
    {
        get
        {
            var rownum = RowLabels.IndexOf(row);
            var rowdata = Lines[rownum];
            var colnum = ColumnLabels.IndexOf(column);
            var result = rowdata.Values[colnum];
            return result;
        }
    }

    public static WireReport BuildFrom(IDisplayReport report)
    {
        var result = new WireReport();

        result.Name = report.Name;
        result.Description = report.Description;
        result.Definition = report.Definition;
        result.DisplayLevelAdjustment = report.DisplayLevelAdjustment;
        result.GrandTotal = report.GrandTotal;

        result.ColumnLabels = report.ColumnLabelsFiltered.ToList();
        result.RowLabels = report.RowLabelsOrdered.ToList();

        result.Lines = result.RowLabels
            .Select(row => 
                new WireReportLine() 
                { 
                    Name = row.ToString(),
                    Values = result.ColumnLabels.Select(col => report[col,row]).ToList()
                })
            .ToList();

        return result;
    }
}

public class WireReportLine
{
    public string Name { get; set; }

    public List<decimal> Values { get; set; }
}
