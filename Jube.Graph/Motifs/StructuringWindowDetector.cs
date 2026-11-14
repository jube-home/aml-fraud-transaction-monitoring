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

namespace Jube.Graph.Motifs
{
    public static class StructuringWindowDetector
    {
        public static IReadOnlyList<PatternInstance> Detect(IReadOnlyList<GraphEvent> events,
            StructuringWindowParameters? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            var p = parameters ?? StructuringWindowParameters.Default;

            if (p.Threshold <= 0 || p.Delta <= 0 || p.Delta >= 1 || p.MinimumCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters));
            }

            var lower = p.Threshold * (1d - p.Delta);
            var index = new EventIndex(events.Where(e => e.Amount >= lower && e.Amount < p.Threshold));
            var result = new List<PatternInstance>();

            foreach (var node in index.Nodes)
            {
                var deposits = index.Inbound(node);
                var i = 0;

                while (i < deposits.Count)
                {
                    var end = EventIndex.WindowEnd(deposits, i, deposits[i].At + p.Window);
                    var count = end - i + 1;

                    if (count < p.MinimumCount)
                    {
                        i++;

                        continue;
                    }

                    var slice = deposits.Skip(i).Take(count).ToArray();
                    var members = new List<PatternMember> { new(node, "Account") };
                    members.AddRange(slice.Select(e => e.From).Distinct().OrderBy(d => d)
                        .Select(d => new PatternMember(d, "Depositor")));

                    result.Add(new PatternInstance(PatternKind.StructuringWindow, node, members, TimeWindow.Of(slice),
                        count, new SortedDictionary<string, double>
                        {
                            ["Threshold"] = p.Threshold,
                            ["Delta"] = p.Delta,
                            ["WindowSeconds"] = p.Window.TotalSeconds,
                            ["MinimumCount"] = p.MinimumCount,
                            ["Count"] = count,
                            ["Total"] = slice.Sum(e => e.Amount)
                        }, PatternInstance.ArchiveIdsOf(slice)));

                    i = end + 1;
                }
            }

            return result;
        }
    }
}
