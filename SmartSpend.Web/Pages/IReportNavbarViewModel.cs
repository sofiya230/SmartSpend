using System.Collections.Generic;
using SmartSpend.Core.Reports;

namespace SmartSpend.AspNet.Pages
{
    public interface IReportNavbarViewModel
    {
        ReportParameters Parameters { get; }

        IEnumerable<ReportDefinition> Definitions { get; }

        int MaxLevels { get; }
    }
}
