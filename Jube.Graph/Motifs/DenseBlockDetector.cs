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

using Jube.Graph.Algorithms;
using Jube.Graph.Model;

namespace Jube.Graph.Motifs
{
    public static class DenseBlockDetector
    {
        public static IReadOnlyList<PatternInstance> Detect(IReadOnlyList<GraphEvent> events,
            DenseBlockParameters? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            var p = parameters ?? DenseBlockParameters.Default;

            if (p.MinimumNodes < 2 || p.MinimumDensity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters));
            }

            var ordered = events.Where(e => e.From != e.To).OrderBy(e => e.At).ThenBy(e => e.ArchiveId).ToArray();

            if (ordered.Length == 0)
            {
                return [];
            }

            var graph = CompressedGraphBuilder.FromEdges(ordered.Select(e => (e.From, e.To, 1f, (byte)0)), false);
            var (nodes, density) = GreedyPeeling.DensestBlock(graph);

            if (nodes.Length < p.MinimumNodes || density < p.MinimumDensity)
            {
                return [];
            }

            var block = nodes.Select(i => graph.NodeIds[i]).ToHashSet();
            var evidence = ordered.Where(e => block.Contains(e.From) && block.Contains(e.To)).ToArray();
            var degree = new SortedDictionary<long, int>();

            foreach (var e in evidence)
            {
                degree[e.From] = degree.GetValueOrDefault(e.From) + 1;
                degree[e.To] = degree.GetValueOrDefault(e.To) + 1;
            }

            var anchor = degree.OrderByDescending(d => d.Value).ThenBy(d => d.Key).First().Key;
            var members = block.OrderBy(n => n).Select(n => new PatternMember(n, "Member")).ToArray();

            return
            [
                new PatternInstance(PatternKind.DenseBlock, anchor, members, TimeWindow.Of(evidence), density,
                    new SortedDictionary<string, double>
                    {
                        ["MinimumNodes"] = p.MinimumNodes,
                        ["MinimumDensity"] = p.MinimumDensity,
                        ["Nodes"] = nodes.Length,
                        ["Density"] = density,
                        ["Events"] = evidence.Length
                    }, PatternInstance.ArchiveIdsOf(evidence))
            ];
        }
    }
}
