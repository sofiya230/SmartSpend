using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Reports;

namespace SmartSpend.AspNet.Pages
{
    public interface IReportAndChartViewModel
    {
        public IDisplayReport Report { get; }

        public string ChartJson { get; }

        public bool ShowSideChart { get; }

        public bool ShowTopChart { get; }
    }
}
