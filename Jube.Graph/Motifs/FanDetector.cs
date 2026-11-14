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
    public static class FanDetector
    {
        public static IReadOnlyList<PatternInstance> Detect(IReadOnlyList<GraphEvent> events,
            FanParameters? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            var p = parameters ?? FanParameters.Default;

            if (p.K < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters));
            }

            var index = new EventIndex(events);
            var result = new List<PatternInstance>();

            foreach (var node in index.Nodes)
            {
                Scan(node, index.Outbound(node), e => e.To, PatternKind.FanOut, "Source", "Destination", p, result);
            }

            foreach (var node in index.Nodes)
            {
                Scan(node, index.Inbound(node), e => e.From, PatternKind.FanIn, "Destination", "Source", p, result);
            }

            return result;
        }

        private static void Scan(long anchor, IReadOnlyList<GraphEvent> ordered, Func<GraphEvent, long> counterpart,
            PatternKind kind, string anchorRole, string counterpartRole, FanParameters p, List<PatternInstance> result)
        {
            var i = 0;

            while (i < ordered.Count)
            {
                var end = EventIndex.WindowEnd(ordered, i, ordered[i].At + p.Window);
                var slice = ordered.Skip(i).Take(end - i + 1).ToArray();
                var counterparts = slice.Select(counterpart).Where(c => c != anchor).Distinct().OrderBy(c => c)
                    .ToArray();

                if (counterparts.Length < p.K)
                {
                    i++;

                    continue;
                }

                var members = new List<PatternMember> { new(anchor, anchorRole) };
                members.AddRange(counterparts.Select(c => new PatternMember(c, counterpartRole)));

                result.Add(new PatternInstance(kind, anchor, members, TimeWindow.Of(slice), counterparts.Length,
                    new SortedDictionary<string, double>
                    {
                        ["K"] = p.K,
                        ["WindowSeconds"] = p.Window.TotalSeconds,
                        ["DistinctCounterparts"] = counterparts.Length
                    }, PatternInstance.ArchiveIdsOf(slice)));

                i = end + 1;
            }
        }
    }
}
