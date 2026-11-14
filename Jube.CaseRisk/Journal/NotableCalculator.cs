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


using MessagePack;

namespace Jube.CaseRisk.Journal
{
    [MessagePackObject]
    public sealed record NotableReason([property: Key(0)] string Field, [property: Key(1)] double Value, [property: Key(2)] double Median, [property: Key(3)] double Z);

    [MessagePackObject]
    public sealed record NotableRow([property: Key(0)] long Id, [property: Key(1)] double Score, [property: Key(2)] string Worst, [property: Key(3)] IReadOnlyList<NotableReason> Reasons);

    [MessagePackObject]
    public sealed record FieldNorm([property: Key(0)] string Field, [property: Key(1)] int Count, [property: Key(2)] double Median, [property: Key(3)] double Scale, [property: Key(4)] bool Confident);

    [MessagePackObject]
    public sealed record NotableReport(
        [property: Key(0)] IReadOnlyList<FieldNorm> Norms,
        [property: Key(1)] IReadOnlyList<NotableRow> Rows,
        [property: Key(2)] IReadOnlyList<string> LowConfidenceFields);

    public static class NotableCalculator
    {
        public const double DefaultThreshold = 3;
        public const int DefaultMaximumRows = 200;

        private static double Finite(double value)
        {
            return double.IsFinite(value) ? value : 0;
        }

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
                measures.Select(field => new FieldNorm(field, fits[field].Count, Finite(fits[field].Median),
                    Finite(fits[field].Scale), fits[field].Confident)).ToList(),
                rows.OrderByDescending(row => row.Score).ThenByDescending(row => row.Id).Take(maximumRows).ToList(),
                measures.Where(field => fits[field].Count > 0 && !fits[field].Confident).ToList());
        }
    }
}
