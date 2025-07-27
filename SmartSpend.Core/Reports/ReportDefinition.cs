namespace SmartSpend.Core.Reports;

public class ReportDefinition
{
    public string slug { get; set; }

    #region Interpreted by QueryBuilder

    public string Source { get; set; }

    public string SourceParameters { get; set; }

    #endregion

    #region Interpreted by ReportBuilder

    public string CustomColumns { get; set; }

    public bool? WholeYear { get; set; }

    public bool? YearProgress { get; set; }

    #endregion

    #region Direct Members of Report

    public string Name { get; set; }

    public string SortOrder { get; set; }

    public bool? WithMonthColumns { get; set; }

    public bool? WithTotalColumn { get; set; }

    public int? SkipLevels { get; set; }

    public int? NumLevels { get; set; }

    public int? DisplayLevelAdjustment { get; set; }

    #endregion
}
