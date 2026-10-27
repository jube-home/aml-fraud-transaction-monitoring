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
    public sealed record FieldDrift(
        string Field,
        double Statistic,
        double Critical,
        bool Drifted,
        double PriorMedian,
        double RecentMedian,
        double? Shift);

    public sealed record DriftReport(DateTime? SplitAt, int Prior, int Recent, IReadOnlyList<FieldDrift> Fields);

    public static class DriftCalculator
    {
        public const int MinimumEvents = 40;
        public const int MinimumPerSide = 20;
        private const double CriticalConstant = 1.358;

        public static DriftReport Compute(IReadOnlyList<JournalEvent> events, IReadOnlyList<string> measures)
        {
            if (events.Count < MinimumEvents)
            {
                return new DriftReport(null, 0, 0, []);
            }

            var ordered = events.OrderBy(e => e.At).ThenBy(e => e.Id).ToList();
            var half = ordered.Count / 2;
            var prior = ordered.Take(half).ToList();
            var recent = ordered.Skip(half).ToList();
            var fields = new List<FieldDrift>();

            foreach (var field in measures)
            {
                var before = RobustStatistics.Sorted(prior.Where(e => e.Measures.ContainsKey(field))
                    .Select(e => e.Measures[field]));
                var after = RobustStatistics.Sorted(recent.Where(e => e.Measures.ContainsKey(field))
                    .Select(e => e.Measures[field]));

                if (before.Length < MinimumPerSide || after.Length < MinimumPerSide)
                {
                    continue;
                }

                var statistic = Kolmogorov(before, after);
                var critical = CriticalConstant * Math.Sqrt((before.Length + after.Length) /
                                                            ((double)before.Length * after.Length));
                var fit = RobustStatistics.Fit(before);
                var priorMedian = RobustStatistics.Quantile(before, 0.5);
                var recentMedian = RobustStatistics.Quantile(after, 0.5);

                fields.Add(new FieldDrift(field, statistic, critical, statistic > critical, priorMedian,
                    recentMedian, fit.Scale > 0 ? (recentMedian - priorMedian) / fit.Scale : null));
            }

            return new DriftReport(recent[0].At, prior.Count, recent.Count,
                fields.OrderByDescending(item => item.Statistic).ToList());
        }

        public static double Kolmogorov(double[] first, double[] second)
        {
            var maximum = 0d;
            int i = 0, j = 0;

            while (i < first.Length && j < second.Length)
            {
                var value = Math.Min(first[i], second[j]);

                while (i < first.Length && first[i] <= value)
                {
                    i++;
                }

                while (j < second.Length && second[j] <= value)
                {
                    j++;
                }

                maximum = Math.Max(maximum, Math.Abs((double)i / first.Length - (double)j / second.Length));
            }

            return maximum;
        }
    }
}
