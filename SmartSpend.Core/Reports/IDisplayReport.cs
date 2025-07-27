using System.Collections.Generic;

namespace SmartSpend.Core.Reports;

public interface IDisplayReport
{
    string Name { get; }

    string Description { get; }

    string Definition { get; }

    IEnumerable<ColumnLabel> ColumnLabelsFiltered { get; }

    IEnumerable<RowLabel> RowLabelsOrdered { get; }

    int DisplayLevelAdjustment { get; }

    decimal this[ColumnLabel column, RowLabel row] { get; }

    public decimal GrandTotal { get; }

}
