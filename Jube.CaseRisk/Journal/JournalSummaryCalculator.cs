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
    public sealed record FieldSummary(string Field, int Count, double Sum, double Mean, double Median, double Max,
        double Latest);

    public sealed record JournalSummary(
        int Count,
        DateTime? First,
        DateTime? Last,
        int ActiveDays,
        int Activations,
        int ActivationRows,
        double MaxElevation,
        IReadOnlyList<FieldSummary> Fields);

    public static class JournalSummaryCalculator
    {
        public static JournalSummary Compute(IReadOnlyList<JournalEvent> events, IReadOnlyList<string> measures)
        {
            if (events.Count == 0)
            {
                return new JournalSummary(0, null, null, 0, 0, 0, 0, []);
            }

            var fields = new List<FieldSummary>();

            foreach (var field in measures)
            {
                var present = events.Where(e => e.Measures.ContainsKey(field)).ToList();

                if (present.Count == 0)
                {
                    continue;
                }

                var values = present.Select(e => e.Measures[field]).Where(double.IsFinite).ToArray();

                if (values.Length == 0)
                {
                    continue;
                }

                fields.Add(new FieldSummary(field, values.Length, values.Sum(), values.Average(),
                    RobustStatistics.Quantile(RobustStatistics.Sorted(values), 0.5), values.Max(),
                    present.OrderByDescending(e => e.At).ThenByDescending(e => e.Id).First().Measures[field]));
            }

            return new JournalSummary(events.Count, events.Min(e => e.At), events.Max(e => e.At),
                events.Select(e => e.At.Date).Distinct().Count(), events.Sum(e => e.Activations),
                events.Count(e => e.Activations > 0), events.Max(e => e.Elevation), fields);
        }
    }
}
