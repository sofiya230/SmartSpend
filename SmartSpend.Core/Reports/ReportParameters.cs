namespace SmartSpend.Core.Reports;

public class ReportParameters
{
    public string slug { get; set; }

    public int? year { get; set; }

    public int? month { get; set; }

    public bool? showmonths { get; set; }

    public int? level { get; set; }
}
