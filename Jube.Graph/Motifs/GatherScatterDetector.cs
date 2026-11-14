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
    public static class GatherScatterDetector
    {
        public static IReadOnlyList<PatternInstance> Detect(IReadOnlyList<GraphEvent> events,
            GatherScatterParameters? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            var p = parameters ?? GatherScatterParameters.Default;

            if (p.K < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters));
            }

            var index = new EventIndex(events);
            var result = new List<PatternInstance>();

            foreach (var node in index.Nodes)
            {
                var merged = index.Inbound(node).Concat(index.Outbound(node)).Where(e => e.From != e.To)
                    .OrderBy(e => e.At).ThenBy(e => e.ArchiveId).ToArray();
                var i = 0;

                while (i < merged.Length)
                {
                    var end = EventIndex.WindowEnd(merged, i, merged[i].At + p.Window);
                    var slice = merged.Skip(i).Take(end - i + 1).ToArray();
                    var sources = slice.Where(e => e.To == node).Select(e => e.From).Distinct().OrderBy(s => s)
                        .ToArray();
                    var destinations = slice.Where(e => e.From == node).Select(e => e.To).Distinct().OrderBy(d => d)
                        .ToArray();

                    if (sources.Length < p.K || destinations.Length < p.K)
                    {
                        i++;

                        continue;
                    }

                    var members = new List<PatternMember> { new(node, "Hub") };
                    members.AddRange(sources.Select(s => new PatternMember(s, "Source")));
                    members.AddRange(destinations.Select(d => new PatternMember(d, "Destination")));

                    result.Add(new PatternInstance(PatternKind.GatherScatter, node, members, TimeWindow.Of(slice),
                        sources.Length + destinations.Length, new SortedDictionary<string, double>
                        {
                            ["K"] = p.K,
                            ["WindowSeconds"] = p.Window.TotalSeconds,
                            ["Sources"] = sources.Length,
                            ["Destinations"] = destinations.Length
                        }, PatternInstance.ArchiveIdsOf(slice)));

                    i = end + 1;
                }
            }

            return result;
        }
    }
}
