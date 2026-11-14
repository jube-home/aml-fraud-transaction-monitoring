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
    public static class TemporalCycleDetector
    {
        public const int MaximumSteps = 2000000;

        public static IReadOnlyList<PatternInstance> Detect(IReadOnlyList<GraphEvent> events,
            TemporalCycleParameters? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            var p = parameters ?? TemporalCycleParameters.Default;

            if (p.MaxLength < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters));
            }

            var index = new EventIndex(events);
            var result = new List<PatternInstance>();
            var path = new List<GraphEvent>();
            var onPath = new HashSet<long>();
            var budget = new[] { MaximumSteps };

            foreach (var start in index.Nodes)
            {
                foreach (var first in index.Outbound(start))
                {
                    if (first.To == start)
                    {
                        continue;
                    }

                    path.Add(first);
                    onPath.Add(start);
                    onPath.Add(first.To);
                    Extend(index, start, first.To, first.At + p.Window, p, path, onPath, result, budget);
                    onPath.Clear();
                    path.Clear();
                }
            }

            return result;
        }

        private static void Extend(EventIndex index, long start, long current, DateTimeOffset until,
            TemporalCycleParameters p, List<GraphEvent> path, HashSet<long> onPath, List<PatternInstance> result,
            int[] budget)
        {
            var lastAt = path[^1].At;

            foreach (var e in index.Outbound(current))
            {
                if (--budget[0] < 0)
                {
                    return;
                }

                if (e.At <= lastAt)
                {
                    continue;
                }

                if (e.At > until)
                {
                    break;
                }

                if (e.To == start)
                {
                    path.Add(e);
                    result.Add(Instance(start, path, p));
                    path.RemoveAt(path.Count - 1);

                    continue;
                }

                if (onPath.Contains(e.To) || path.Count + 1 >= p.MaxLength)
                {
                    continue;
                }

                path.Add(e);
                onPath.Add(e.To);
                Extend(index, start, e.To, until, p, path, onPath, result, budget);
                onPath.Remove(e.To);
                path.RemoveAt(path.Count - 1);
            }
        }

        private static PatternInstance Instance(long start, List<GraphEvent> path, TemporalCycleParameters p)
        {
            var members = path.Select(e => new PatternMember(e.From, "Member")).ToArray();
            var leaving = path[0].Amount;
            var returning = path[^1].Amount;
            var roundTrip = leaving > 0 ? returning / leaving : 0d;

            return new PatternInstance(PatternKind.TemporalCycle, start, members, TimeWindow.Of(path), path.Count,
                new SortedDictionary<string, double>
                {
                    ["MaxLength"] = p.MaxLength,
                    ["WindowSeconds"] = p.Window.TotalSeconds,
                    ["Length"] = path.Count,
                    ["ReturnRatio"] = roundTrip
                }, PatternInstance.ArchiveIdsOf(path));
        }
    }
}
