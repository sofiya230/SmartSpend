using System;
using System.Collections.Generic;

namespace SmartSpend.Core.Reports;

public class ManualReport : Table<ColumnLabel, RowLabel, decimal>, IDisplayReport
{
    public string Name { get; set; }

    public string Description { get; set; }

    public IEnumerable<ColumnLabel> ColumnLabelsFiltered => base.ColumnLabels;

    public IEnumerable<RowLabel> RowLabelsOrdered => base.RowLabels;

    public int DisplayLevelAdjustment { get; set; }

    public static RowLabel TotalRow => RowLabel.Total;

    public static ColumnLabel TotalColumn => ColumnLabel.Total;

    public decimal GrandTotal => this[TotalColumn, TotalRow];

    public string Definition { get; set; }

    internal void SetData((ColumnLabel col, RowLabel row, decimal value)[] datapoints)
    {
        foreach (var (col, row, value) in datapoints)
            this[col, row] = value;
    }
}
