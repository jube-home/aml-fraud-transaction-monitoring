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
    public static class ScatterGatherDetector
    {
        public static IReadOnlyList<PatternInstance> Detect(IReadOnlyList<GraphEvent> events,
            ScatterGatherParameters? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            var p = parameters ?? ScatterGatherParameters.Default;

            if (p.MinimumIntermediaries < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters));
            }

            var index = new EventIndex(events);
            var result = new List<PatternInstance>();

            foreach (var source in index.Nodes)
            {
                var legs = new SortedDictionary<long, List<(long Intermediary, GraphEvent First, GraphEvent Second)>>();

                foreach (var first in index.Outbound(source))
                {
                    var intermediary = first.To;

                    if (intermediary == source)
                    {
                        continue;
                    }

                    foreach (var second in index.Outbound(intermediary))
                    {
                        if (second.At <= first.At || second.At > first.At + p.Window)
                        {
                            continue;
                        }

                        var sink = second.To;

                        if (sink == source || sink == intermediary)
                        {
                            continue;
                        }

                        if (!legs.TryGetValue(sink, out var list))
                        {
                            list = [];
                            legs[sink] = list;
                        }

                        list.Add((intermediary, first, second));
                    }
                }

                foreach (var (sink, list) in legs)
                {
                    var intermediaries = list.Select(l => l.Intermediary).Distinct().OrderBy(m => m).ToArray();

                    if (intermediaries.Length < p.MinimumIntermediaries)
                    {
                        continue;
                    }

                    var evidence = list.Select(l => l.First).Concat(list.Select(l => l.Second)).ToArray();
                    var members = new List<PatternMember> { new(source, "Source") };
                    members.AddRange(intermediaries.Select(m => new PatternMember(m, "Intermediary")));
                    members.Add(new PatternMember(sink, "Sink"));

                    result.Add(new PatternInstance(PatternKind.ScatterGather, source, members,
                        TimeWindow.Of(evidence), intermediaries.Length, new SortedDictionary<string, double>
                        {
                            ["MinimumIntermediaries"] = p.MinimumIntermediaries,
                            ["WindowSeconds"] = p.Window.TotalSeconds,
                            ["Intermediaries"] = intermediaries.Length
                        }, PatternInstance.ArchiveIdsOf(evidence)));
                }
            }

            return result;
        }
    }
}
