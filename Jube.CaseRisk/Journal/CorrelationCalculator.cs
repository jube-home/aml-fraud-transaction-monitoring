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
    public sealed record CorrelationPair(string First, string Second, double R, int Pairs);

    public sealed record CorrelationReport(
        IReadOnlyList<string> Fields,
        IReadOnlyList<IReadOnlyList<double?>> Matrix,
        IReadOnlyList<CorrelationPair> Strong);

    public static class CorrelationCalculator
    {
        public const int MaximumFields = 12;
        public const int MinimumPairs = 10;
        public const double StrongR = 0.8;

        public static CorrelationReport Compute(IReadOnlyList<JournalEvent> events, IReadOnlyList<string> measures)
        {
            var chosen = measures
                .Select(field => (Field: field, Values: events.Where(e => e.Measures.ContainsKey(field))
                    .Select(e => e.Measures[field]).ToArray()))
                .Where(item => item.Values.Length >= MinimumPairs && item.Values.Distinct().Count() > 1)
                .OrderByDescending(item => item.Values.Length).ThenBy(item => item.Field, StringComparer.Ordinal)
                .Take(MaximumFields).Select(item => item.Field).ToList();

            var matrix = new List<IReadOnlyList<double?>>();
            var strong = new List<CorrelationPair>();

            for (var row = 0; row < chosen.Count; row++)
            {
                var cells = new List<double?>();

                for (var column = 0; column < chosen.Count; column++)
                {
                    if (row == column)
                    {
                        cells.Add(1);

                        continue;
                    }

                    var (r, pairs) = Pearson(events, chosen[row], chosen[column]);

                    cells.Add(r);

                    if (column > row && r is { } value && Math.Abs(value) >= StrongR)
                    {
                        strong.Add(new CorrelationPair(chosen[row], chosen[column], value, pairs));
                    }
                }

                matrix.Add(cells);
            }

            return new CorrelationReport(chosen, matrix, strong.OrderByDescending(pair => Math.Abs(pair.R)).ToList());
        }

        public static (double? R, int Pairs) Pearson(IReadOnlyList<JournalEvent> events, string first, string second)
        {
            var pairs = events.Where(e => e.Measures.ContainsKey(first) && e.Measures.ContainsKey(second))
                .Select(e => (X: e.Measures[first], Y: e.Measures[second])).ToList();

            if (pairs.Count < MinimumPairs)
            {
                return (null, pairs.Count);
            }

            var meanX = pairs.Average(p => p.X);
            var meanY = pairs.Average(p => p.Y);
            var covariance = pairs.Sum(p => (p.X - meanX) * (p.Y - meanY));
            var varianceX = pairs.Sum(p => (p.X - meanX) * (p.X - meanX));
            var varianceY = pairs.Sum(p => (p.Y - meanY) * (p.Y - meanY));

            if (varianceX == 0 || varianceY == 0)
            {
                return (null, pairs.Count);
            }

            return (Math.Clamp(covariance / Math.Sqrt(varianceX * varianceY), -1, 1), pairs.Count);
        }
    }
}
