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


namespace Jube.CaseRisk.Journal
{
    public sealed record ProgressionPoint(DateTime Start, double Median, int Count);

    public sealed record ProgressionSeries(
        string Field,
        IReadOnlyList<ProgressionPoint> Points,
        double Latest,
        double? PreviousMedian,
        double? Change);

    public sealed record ProgressionReport(string Bucket, IReadOnlyList<ProgressionSeries> Series);

    public static class ProgressionCalculator
    {
        public const int MaximumPoints = 200;
        private const int PreviousPoints = 7;

        public static ProgressionReport Compute(IReadOnlyList<JournalEvent> events, IReadOnlyList<string> measures,
            int maximumPoints = MaximumPoints)
        {
            if (events.Count == 0)
            {
                return new ProgressionReport("day", []);
            }

            var span = events.Max(e => e.At) - events.Min(e => e.At);
            var bucket = span.TotalDays <= 3 ? "hour" : span.TotalDays <= 200 ? "day" : "week";

            if (Distinct(events, bucket) > maximumPoints)
            {
                bucket = "week";
            }

            var series = new List<ProgressionSeries>();

            foreach (var field in measures)
            {
                var points = events.Where(e => e.Measures.ContainsKey(field))
                    .GroupBy(e => Start(e.At, bucket))
                    .OrderBy(group => group.Key)
                    .Select(group => new ProgressionPoint(group.Key,
                        RobustStatistics.Quantile(RobustStatistics.Sorted(group.Select(e => e.Measures[field])), 0.5),
                        group.Count()))
                    .ToList();

                if (points.Count == 0)
                {
                    continue;
                }

                var latest = events.Where(e => e.Measures.ContainsKey(field)).OrderByDescending(e => e.At)
                    .ThenByDescending(e => e.Id).First().Measures[field];

                var earlier = points.SkipLast(1).TakeLast(PreviousPoints).Select(point => point.Median).ToArray();
                double? previous = earlier.Length == 0
                    ? null
                    : RobustStatistics.Quantile(RobustStatistics.Sorted(earlier), 0.5);

                double? change = previous is { } baseline && baseline != 0 ? latest / baseline - 1 : null;

                series.Add(new ProgressionSeries(field, points, latest, previous, change));
            }

            return new ProgressionReport(bucket, series);
        }

        public static DateTime Start(DateTime at, string bucket)
        {
            var utc = DateTime.SpecifyKind(at, DateTimeKind.Utc);

            return bucket switch
            {
                "hour" => new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc),
                "week" => utc.Date.AddDays(-(((int)utc.DayOfWeek + 6) % 7)),
                _ => utc.Date
            };
        }

        private static int Distinct(IReadOnlyList<JournalEvent> events, string bucket)
        {
            return events.Select(e => Start(e.At, bucket)).Distinct().Count();
        }
    }
}
