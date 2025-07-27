using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.ChartJS
{
    public class ChartConfig
    {
        public string Type { get; set; }

        public ChartData Data { get; } = new ChartData();

        public ChartOptions Options { get; } = new ChartOptions();

        public const int MaxSegments = 7;

        public static ChartConfig CreatePieChart(IEnumerable<(string Label,int Data)> points, IEnumerable<ChartColor> colors)
        {
            var result = new ChartConfig() { Type = "doughnut" };

            result.FillSingle(points, colors);

            return result;
        }

        public static ChartConfig CreateBarChart(IEnumerable<(string Label, int Data)> points, IEnumerable<ChartColor> colors)
        {
            var result = new ChartConfig() { Type = "bar" };

            result.Options.Plugins.Legend.Display = false;
            result.FillSingle(points, colors);

            return result;
        }

        public static ChartConfig CreateMultiBarChart(IEnumerable<string> labels, IEnumerable<(string Label, IEnumerable<int> Data)> series, IEnumerable<ChartColor> colors)
        {
            var result = new ChartConfig() { Type = "bar" };

            result.Options.Plugins.Legend.Display = true;
            result.Options.Plugins.Legend.Position = "top";

            result.FillMulti(labels, series, colors);

            result.Data.Datasets = result.Data.Datasets.ToList();

            double alpha = 1.0;
            foreach (var dataset in result.Data.Datasets)
            {
                var setcolors = colors.Select(x => x.WithAlpha(alpha)).ToList();
                dataset.BackgroundColor = setcolors;
                dataset.BorderColor = setcolors;
                alpha /= 2;
            }

            return result;
        }

        public static ChartConfig CreateLineChart(IEnumerable<string> labels, IEnumerable<(string Label,IEnumerable<int> Data)> series, IEnumerable<ChartColor> colors)
        {
            var result = new ChartConfig() { Type = "line" };

            const int maxitems = 6;

            var numitems = series.Count();
            if (numitems > maxitems)
            {
                series = series.Take(maxitems);
            }

            result.FillMulti(labels, series, colors);

            return result;
        }
        private void FillSingle(IEnumerable<(string Label, int Data)> points, IEnumerable<ChartColor> colors)
        {

            var numitems = points.Count();
            if (numitems > MaxSegments)
            {
                numitems = MaxSegments;
                var total = points.Skip(MaxSegments - 1).Sum(x => x.Data);
                points = points.Take(MaxSegments - 1).Append(("Others", total));
            }

            Data.Labels = points.Select(x => x.Label);

            Data.Datasets = new List<ChartDataSet>() { new ChartDataSet() { Data = points.Select(x => x.Data), BorderWidth = 2 } };

            Data.Datasets.Last().BorderColor = colors.Take(numitems);
            Data.Datasets.Last().BackgroundColor = colors.Take(numitems).Select(x => x.WithAlpha(0.8));
        }

        private void FillMulti(IEnumerable<string> labels, IEnumerable<(string Label, IEnumerable<int> Data)> series, IEnumerable<ChartColor> colors)
        {
            var maxitems = colors.Count();
            var numitems = series.Count();
            if (numitems > maxitems)
            {
                series = series.Take(maxitems);
            }

            Data.Labels = labels;
            Data.Datasets = series.Select((x, i) => new ChartDataSet() { Label = x.Label, Data = x.Data, BackgroundColor = new ChartColor[] { colors.Skip(i).First() }, BorderColor = new ChartColor[] { colors.Skip(i).First() } });
        }

    };

    public class ChartDataSet
    {
        public string Label { get; set; } = null;
        public IEnumerable<int> Data { get; set; }
        public IEnumerable<ChartColor> BackgroundColor { get; set; }
        public IEnumerable<ChartColor> BorderColor { get; set; }
        public int? BorderWidth { get; set; }
    }

    public class ChartData
    {
        public IEnumerable<string> Labels { get; set; }

        public IEnumerable<ChartDataSet> Datasets { get; set; }
    }

    public class ChartLegend
    {
        public string Position { get; set; } = "bottom";
        public bool Display { get; set; } = true;
    }

    public class ChartPlugins
    {
        public ChartLegend Legend { get; set; } = new ChartLegend();
    }

    public class ChartOptions
    {
        public ChartPlugins Plugins { get; set; } = new ChartPlugins();
    }

}
