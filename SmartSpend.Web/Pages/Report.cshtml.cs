using Common.DotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using SmartSpend.AspNet.Pages.Helpers;
using SmartSpend.Core.Reports;

namespace SmartSpend.AspNet.Pages
{
    [Authorize(Policy = "CanRead")]
    public class ReportModel : PageModel, IReportNavbarViewModel, IReportAndChartViewModel
    {
        public ReportModel(IReportEngine reports, IClock clock)
        {
            _reports = reports;
            _clock = clock;
        }

        public string Title { get; set; }

        public ReportParameters Parameters { get; set; }

        IEnumerable<ReportDefinition> IReportNavbarViewModel.Definitions => _reports.Definitions;

        public Report Report { get; set; }

        public string ChartJson { get; set; } = null;

        public bool ShowSideChart { get; set; } = false;

        public bool ShowTopChart { get; set; } = false;

        IDisplayReport IReportAndChartViewModel.Report => Report;

        int IReportNavbarViewModel.MaxLevels => Report?.MaxLevels ?? 4;

        public void OnGet([Bind] ReportParameters parms)
        {
            Parameters = parms;

            if (string.IsNullOrEmpty(parms.slug))
            {
                parms.slug = "all";
            }

            var sessionvars = new SessionVariables(HttpContext);

            if (parms.year.HasValue)
                sessionvars.Year = parms.year.Value;
            else
                parms.year = sessionvars.Year ?? _clock.Now.Year;

            if (!parms.month.HasValue)
            {
                bool iscurrentyear = (parms.year == _clock.Now.Year);

                if (iscurrentyear)
                    parms.month = _clock.Now.Month;
                else
                    parms.month = 12;
            }

            Title = _reports.Definitions.Where(x=>x.slug == parms.slug).SingleOrDefault()?.Name ?? "Not Found";
        }

        private readonly IReportEngine _reports;
        private readonly IClock _clock;
    }
}
