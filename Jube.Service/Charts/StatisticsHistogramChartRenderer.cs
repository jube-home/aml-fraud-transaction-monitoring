/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

// ReSharper disable NotAccessedPositionalProperty.Global

using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Exceptions;
using Jube.Dto.Payload;

namespace Jube.Service.Charts
{
    public static class StatisticsHistogramChartRenderer
    {
        public const int MaximumBuckets = 200;
        public const int MaximumTitleLength = 120;

        public static string Render(StatisticsHistogramChartRequest request)
        {
            var statistics = request.Statistics;
            var buckets = statistics.Histogram;
            var title = request.Title.Length > MaximumTitleLength ? request.Title[..MaximumTitleLength] : request.Title;

            if (buckets.Count is < 1 or > MaximumBuckets || !AllFinite(statistics))
            {
                throw new ArgumentException("The statistics need between 1 and " + MaximumBuckets +
                                            " finite histogram buckets.");
            }

            var unit = string.IsNullOrWhiteSpace(request.Unit) ? string.Empty : " (" + request.Unit.Trim() + ")";

            try
            {
                return Chart.Histogram()
                    .Title(title)
                    .Subtitle("Distribution of the values in the rows shown")
                    .XAxisTitle("Value" + unit)
                    .YAxisTitle("Rows")
                    .Size(640, 320)
                    .Legend(LegendPosition.None)
                    .Theme(request.Dark ? ThemeMode.Dark : ThemeMode.Light)
                    .Bins("Rows", Edges(buckets, statistics), buckets.Select(b => (double)b.Frequency),
                        string.IsNullOrWhiteSpace(request.Color) ? null : request.Color)
                    .Reference(statistics.Mean, "Mean")
                    .Reference(statistics.Median, "Median")
                    .Render();
            }
            catch (ChartSpecificationException e)
            {
                throw new ArgumentException(e.Message, e);
            }
        }

        internal static double[] Edges(IReadOnlyList<HistogramBucket> buckets, ColumnStatistics statistics)
        {
            var edges = new double[buckets.Count + 1];

            for (var i = 0; i < buckets.Count; i++)
            {
                edges[i] = buckets[i].Bin;
            }

            var width = buckets.Count > 1 ? buckets[1].Bin - buckets[0].Bin : statistics.Max - statistics.Min;

            edges[buckets.Count] = buckets[^1].Bin + (width > 0 ? width : 1);

            for (var i = 1; i < edges.Length; i++)
            {
                if (!(edges[i] > edges[i - 1]))
                {
                    return Even(buckets.Count, statistics);
                }
            }

            return edges;
        }

        private static double[] Even(int count, ColumnStatistics statistics)
        {
            var span = statistics.Max > statistics.Min ? statistics.Max - statistics.Min : count;

            return [.. Enumerable.Range(0, count + 1).Select(i => statistics.Min + span * i / count)];
        }

        private static bool AllFinite(ColumnStatistics statistics)
        {
            return double.IsFinite(statistics.Min) && double.IsFinite(statistics.Max) &&
                   double.IsFinite(statistics.Mean) && double.IsFinite(statistics.Median) &&
                   statistics.Histogram.All(b => double.IsFinite(b.Bin) && b.Frequency >= 0);
        }
    }
}
