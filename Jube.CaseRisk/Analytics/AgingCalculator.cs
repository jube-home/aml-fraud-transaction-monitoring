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

namespace Jube.CaseRisk.Analytics
{
    public static class AgingCalculator
    {
        public static readonly double[] Edges = [60, 240, 1440, 4320, 10080, 20160, 43200];

        public static AgingReport Open(IReadOnlyCollection<CaseFact> facts, DateTime now, int oldestTake = 10)
        {
            var open = facts.Where(f => !f.Closed).Select(f => new
            {
                f.CaseId,
                Age = Math.Max(0, (now - f.CreatedDate).TotalMinutes),
                Lane = f.Lane ?? 4,
                f.Priority
            }).ToList();

            var buckets = new List<AgingBucket>();

            for (var i = 0; i <= Edges.Length; i++)
            {
                var from = i == 0 ? 0 : Edges[i - 1];
                double? to = i == Edges.Length ? null : Edges[i];
                var inside = open.Where(o => o.Age >= from && (to == null || o.Age < to)).ToList();

                var label = i == 0 ? "< " + Histograms.Minutes(Edges[0])
                    : i == Edges.Length ? Histograms.Minutes(Edges[^1]) + "+"
                    : Histograms.Minutes(Edges[i - 1]) + " – " + Histograms.Minutes(Edges[i]);

                buckets.Add(new AgingBucket(label, from, to, inside.Count,
                    inside.GroupBy(o => o.Lane).ToDictionary(g => g.Key, g => g.Count())));
            }

            return new AgingReport(open.Count, buckets,
                open.OrderByDescending(o => o.Age).Take(oldestTake)
                    .Select(o => new AgedCase(o.CaseId, o.Age, o.Lane, o.Priority)).ToList());
        }
    }
}
