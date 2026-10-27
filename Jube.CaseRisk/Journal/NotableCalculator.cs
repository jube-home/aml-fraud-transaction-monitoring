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
    public sealed record NotableReason(string Field, double Value, double Median, double Z);

    public sealed record NotableRow(long Id, double Score, string Worst, IReadOnlyList<NotableReason> Reasons);

    public sealed record FieldNorm(string Field, int Count, double Median, double Scale, bool Confident);

    public sealed record NotableReport(
        IReadOnlyList<FieldNorm> Norms,
        IReadOnlyList<NotableRow> Rows,
        IReadOnlyList<string> LowConfidenceFields);

    public static class NotableCalculator
    {
        public const double DefaultThreshold = 3;
        public const int DefaultMaximumRows = 200;

        public static NotableReport Compute(IReadOnlyList<JournalEvent> events, IReadOnlyList<string> measures,
            double threshold = DefaultThreshold, int maximumRows = DefaultMaximumRows)
        {
            var fits = measures.ToDictionary(field => field, field => RobustStatistics.Fit(
                events.Where(e => e.Measures.ContainsKey(field)).Select(e => e.Measures[field])));

            var rows = new List<NotableRow>();

            foreach (var evt in events)
            {
                var reasons = new List<NotableReason>();

                foreach (var field in measures)
                {
                    if (!evt.Measures.TryGetValue(field, out var value))
                    {
                        continue;
                    }

                    var fit = fits[field];
                    var z = RobustStatistics.ZScore(fit, value);

                    if (z is { } score && Math.Abs(score) >= threshold)
                    {
                        reasons.Add(new NotableReason(field, value, fit.Median, score));
                    }
                }

                if (reasons.Count == 0)
                {
                    continue;
                }

                var ordered = reasons.OrderByDescending(reason => Math.Abs(reason.Z)).ToList();

                rows.Add(new NotableRow(evt.Id, Math.Abs(ordered[0].Z), ordered[0].Field, ordered));
            }

            return new NotableReport(
                measures.Select(field => new FieldNorm(field, fits[field].Count, fits[field].Median,
                    fits[field].Scale, fits[field].Confident)).ToList(),
                rows.OrderByDescending(row => row.Score).ThenByDescending(row => row.Id).Take(maximumRows).ToList(),
                measures.Where(field => fits[field].Count > 0 && !fits[field].Confident).ToList());
        }
    }
}
