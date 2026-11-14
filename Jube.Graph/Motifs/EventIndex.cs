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
    internal sealed class EventIndex
    {
        private static readonly List<GraphEvent> None = [];

        private readonly SortedDictionary<long, List<GraphEvent>> outbound = new();
        private readonly SortedDictionary<long, List<GraphEvent>> inbound = new();
        private readonly SortedSet<long> nodes = [];

        public EventIndex(IEnumerable<GraphEvent> events)
        {
            ArgumentNullException.ThrowIfNull(events);

            foreach (var e in events.OrderBy(e => e.At).ThenBy(e => e.ArchiveId))
            {
                Add(outbound, e.From, e);
                Add(inbound, e.To, e);
                nodes.Add(e.From);
                nodes.Add(e.To);
            }
        }

        public IReadOnlyCollection<long> Nodes => nodes;

        public IReadOnlyList<GraphEvent> Outbound(long node)
        {
            return outbound.TryGetValue(node, out var list) ? list : None;
        }

        public IReadOnlyList<GraphEvent> Inbound(long node)
        {
            return inbound.TryGetValue(node, out var list) ? list : None;
        }

        public int DistinctCounterparts(long node)
        {
            var counterparts = new HashSet<long>();

            foreach (var e in Outbound(node))
            {
                counterparts.Add(e.To);
            }

            foreach (var e in Inbound(node))
            {
                counterparts.Add(e.From);
            }

            counterparts.Remove(node);

            return counterparts.Count;
        }

        public static int WindowEnd(IReadOnlyList<GraphEvent> ordered, int start, DateTimeOffset until)
        {
            var end = start;

            while (end + 1 < ordered.Count && ordered[end + 1].At <= until)
            {
                end++;
            }

            return end;
        }

        private static void Add(SortedDictionary<long, List<GraphEvent>> map, long key, GraphEvent e)
        {
            if (!map.TryGetValue(key, out var list))
            {
                list = [];
                map[key] = list;
            }

            list.Add(e);
        }
    }
}
