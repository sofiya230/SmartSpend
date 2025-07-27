using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Common.ChartJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartSpend.Core.Reports;

namespace SmartSpend.AspNet.Pages
{
    [Authorize(Policy = "CanRead")]
    public class ReportPartialModel : PageModel, IReportAndChartViewModel
    {
        private readonly IReportEngine _reportengine;

        public Report Report { get; set; }

        public string ChartJson { get; set; }

        public bool ShowSideChart { get; set; }

        public bool ShowTopChart { get; set; }

        IDisplayReport IReportAndChartViewModel.Report => Report;

        public ReportPartialModel(IReportEngine reportengine)
        {
            _reportengine = reportengine;
        }

        public Task<IActionResult> OnGetAsync([Bind] ReportParameters parms)
        {
            try
            {

                Report = _reportengine.Build(parms);


                var multisigned = Report.Source?.Any(x => x.IsMultiSigned) ?? true;
                if (!Report.WithMonthColumns && !multisigned)
                {
                    decimal factor = Report.SortOrder == Report.SortOrders.TotalAscending ? 1m : -1m;

                    if (Report.WithTotalColumn)
                    {

                        ShowSideChart = true;

                        var col = Report.TotalColumn;
                        var rows = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
                        var points = rows.Select(row => (row.Name, (int)(Report[col, row] * factor)));

                        ChartConfig Chart = null;

                        if (points.All(x => x.Item2 >= 0))
                            Chart = ChartConfig.CreatePieChart(points, palette);
                        else
                            Chart = ChartConfig.CreateBarChart(points, palette);

                        ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
                    }
                    else
                    {
                        ShowTopChart = true;

                        var rows = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
                        var labels = rows.Select(x => x.Name);

                        var cols = Report.ColumnLabelsFiltered.Where(x => !x.IsTotal && !x.IsCalculated);
                        var series = cols.Select(col => (col.Name, rows.Select(row => (int)(Report[col, row] * factor)))).ToList();


                        if (Report.YearProgress != 0.0)
                        {
                            var i = series.FindIndex(x => x.Name == "Budget");

                            series[i] = (series[i].Name + " (YTD)", series[i].Item2.Select(x => (int)((double)x * Report.YearProgress)));
                        }

                        var Chart = ChartConfig.CreateMultiBarChart(labels, series, palette);


                        ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
                    }
                }

                if (Report.WithMonthColumns)
                {
                    ShowTopChart = true;

                    decimal factor = Report.SortOrder == Report.SortOrders.TotalAscending ? 1m : -1m;

                    var cols = Report.ColumnLabelsFiltered.Where(x => !x.IsTotal && !x.IsCalculated);
                    var labels = cols.Select(x => x.Name);
                    var rows = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
                    var series = rows.Select(row => (row.Name, cols.Select(col => (int)(Report[col, row] * factor))));
                    var Chart = ChartConfig.CreateLineChart(labels, series, palette);

                    ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
                }


                var result = new PartialViewResult
                {
                    ViewName = "DisplayReportAndChart",
                    ViewData = ViewData,
                };
                return Task.FromResult(result as IActionResult);
            }
            catch (KeyNotFoundException ex)
            {
                return Task.FromResult(NotFound(ex.Message) as IActionResult);
            }
        }

        private static readonly ChartColor[] palette = new ChartColor[]
        {
            new ChartColor("540D6E"),
            new ChartColor("EE4266"),
            new ChartColor("FFD23F"),
            new ChartColor("875D5A"),
            new ChartColor("FFD3DA"),
            new ChartColor("8EE3EF"),
            new ChartColor("7A918D"),
        };
    }
}
