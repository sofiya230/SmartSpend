using System.Collections.Generic;

namespace SmartSpend.Core.Reports;

public interface IReportEngine
{
    IEnumerable<ReportDefinition> Definitions { get; }

    Report Build(ReportParameters parameters);

    IEnumerable<IEnumerable<IDisplayReport>> BuildSummary(ReportParameters parameters);
}
