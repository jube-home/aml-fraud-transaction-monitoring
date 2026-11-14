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
using System.Globalization;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Svg;
using Jube.Charting.Text;

namespace Jube.Charting.Diagrams
{
    public static class FlowLayout
    {
        public const double TickHeight = 18;

        public static GraphLayoutResult Compute(GraphDiagramSpec spec, Dictionary<string, int> index)
        {
            var n = spec.Nodes.Count;
            var sizes = NetworkGeometry.Sizes(spec, index);
            var edges = NetworkGeometry.Edges(spec, index);
            var laneNames = LaneOrder(spec);
            var laneOf = new int[n];

            for (var i = 0; i < n; i++)
            {
                laneOf[i] = laneNames.IndexOf(LaneOf(spec, i));
            }

            var times = NodeTimes(spec, index);
            var column = new int[n];
            var laneLast = Enumerable.Repeat(-1, laneNames.Count).ToArray();
            var timeColumn = new Dictionary<DateTimeOffset, int>();
            var maxColumn = -1;

            var timed = Enumerable.Range(0, n).Where(i => times[i].HasValue).OrderBy(i => times[i]!.Value)
                .ThenBy(i => i);

            foreach (var i in timed)
            {
                var when = times[i]!.Value;

                if (!timeColumn.TryGetValue(when, out var col))
                {
                    col = maxColumn + 1;
                    timeColumn[when] = col;
                }

                col = Math.Max(col, laneLast[laneOf[i]] + 1);
                column[i] = col;
                laneLast[laneOf[i]] = col;
                maxColumn = Math.Max(maxColumn, col);
            }

            var firstUntimed = maxColumn + 1;

            foreach (var i in Enumerable.Range(0, n).Where(i => !times[i].HasValue))
            {
                var col = Math.Max(firstUntimed, laneLast[laneOf[i]] + 1);
                column[i] = col;
                laneLast[laneOf[i]] = col;
                maxColumn = Math.Max(maxColumn, col);
            }

            var columns = maxColumn + 1;
            var maxDiameter = sizes.Max(s => Math.Max(s.W, s.H));
            var leftPad = LaneLabelWidth(spec);
            var available = spec.MaxExtent - leftPad - 2 * NetworkGeometry.Margin - NetworkGeometry.LabelWidth;
            var columnWidth = Math.Max(maxDiameter + 48, 72);

            if (columns * columnWidth > available)
            {
                columnWidth = available / columns;

                if (columnWidth < maxDiameter + 10)
                {
                    throw new DiagramTooLargeException("flow columns", columns,
                        (long)Math.Floor(available / (maxDiameter + 10)),
                        "Narrow the time window or draw fewer entities per lane.");
                }
            }

            var rowHeight = maxDiameter + 64;
            var centres = new (double X, double Y)[n];

            for (var i = 0; i < n; i++)
            {
                centres[i] = (column[i] * columnWidth + columnWidth / 2, laneOf[i] * rowHeight + rowHeight / 2);
            }

            var (nodes, width, height) = NetworkGeometry.Place(spec, centres, sizes, laneOf, leftPad, TickHeight);
            var links = Links(nodes, edges, rowHeight);

            return new GraphLayoutResult(nodes, links, width, height, GraphLayoutKind.Flow, laneNames.Count, 0);
        }

        public static string LaneOf(GraphDiagramSpec spec, int i)
        {
            var node = spec.Nodes[i];

            return spec.Lanes is { } lanes && lanes.TryGetValue(node.Id, out var lane) ? lane
                : node.Group ?? node.Role.ToString();
        }

        public static List<string> LaneOrder(GraphDiagramSpec spec)
        {
            var present = new List<string>();

            for (var i = 0; i < spec.Nodes.Count; i++)
            {
                var lane = LaneOf(spec, i);

                if (!present.Contains(lane))
                {
                    present.Add(lane);
                }
            }

            var ordered = spec.GroupOrder.Where(present.Contains).Distinct().ToList();
            ordered.AddRange(present.Where(lane => !ordered.Contains(lane)));

            return ordered;
        }

        public static double LaneLabelWidth(GraphDiagramSpec spec)
        {
            return Math.Min(TextMetrics.MaxWidth(LaneOrder(spec), 11, true), 160) + 14;
        }

        public static DateTimeOffset?[] NodeTimes(GraphDiagramSpec spec, Dictionary<string, int> index)
        {
            var times = new DateTimeOffset?[spec.Nodes.Count];

            foreach (var e in spec.Edges)
            {
                if (e.First is not { } first)
                {
                    continue;
                }

                foreach (var i in new[] { index[e.From], index[e.To] })
                {
                    if (times[i] is null || first < times[i]!.Value)
                    {
                        times[i] = first;
                    }
                }
            }

            if (spec.Times is { } given)
            {
                foreach (var pair in given)
                {
                    if (index.TryGetValue(pair.Key, out var i))
                    {
                        times[i] = pair.Value;
                    }
                }
            }

            return times;
        }

        public static string FormatTime(DateTimeOffset when, TimeSpan span)
        {
            var utc = when.UtcDateTime;

            var format = span < TimeSpan.FromDays(2) ? "HH:mm"
                : span < TimeSpan.FromDays(300) ? "MMM d" : "yyyy-MM-dd";

            return utc.ToString(format, CultureInfo.InvariantCulture);
        }

        private static List<PlacedGraphEdge> Links(PlacedGraphNode[] nodes, (int From, int To)[] edges,
            double rowHeight)
        {
            var groups = edges.Select((e, k) => (e.From, e.To, K: k)).GroupBy(t => (t.From, t.To))
                .OrderBy(g => g.Min(t => t.K)).ToList();

            var pairs = new HashSet<(int, int)>(groups.Select(g => g.Key));
            var result = new List<PlacedGraphEdge>();

            foreach (var group in groups)
            {
                var (from, to) = group.Key;
                var members = group.Select(t => t.K).OrderBy(k => k).ToList();
                var k = members[0];

                if (from == to)
                {
                    result.Add(NetworkGeometry.Loop(k, nodes[from], nodes) with { Merged = members });

                    continue;
                }

                var a = nodes[from];
                var b = nodes[to];
                var reverse = pairs.Contains((to, from));
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var distance = Math.Max(Math.Sqrt(dx * dx + dy * dy), 1e-6);
                (double X, double Y) control;

                if (Math.Abs(dy) < 1e-6)
                {
                    var bend = Math.Clamp(Math.Abs(dx) * 0.22, 14, rowHeight * 0.45);
                    control = ((a.X + b.X) / 2, a.Y + (dx >= 0 ? -bend : bend));
                }
                else
                {
                    var side = reverse && from > to ? -1 : 1;
                    var offset = 0.12 * distance * side;
                    control = ((a.X + b.X) / 2 - dy / distance * offset, (a.Y + b.Y) / 2 + dx / distance * offset);
                }

                var ra = Math.Max(a.Width, a.Height) / 2 + 1;
                var rb = Math.Max(b.Width, b.Height) / 2 + 1;
                var t0 = Clip((a.X, a.Y), control, (b.X, b.Y), (a.X, a.Y), ra, true);
                var t1 = Clip((a.X, a.Y), control, (b.X, b.Y), (b.X, b.Y), rb, false);
                var start = Point((a.X, a.Y), control, (b.X, b.Y), t0);
                var end = Point((a.X, a.Y), control, (b.X, b.Y), t1);
                var mid = Point(start, control, end, 0.5);
                var tx = end.X - control.X;
                var ty = end.Y - control.Y;
                var tl = Math.Max(Math.Sqrt(tx * tx + ty * ty), 1e-6);
                var path = new PathBuilder().Move(start.X, start.Y).Quadratic(control.X, control.Y, end.X, end.Y);

                result.Add(new PlacedGraphEdge(k, from, to, path.ToString(), mid.X, mid.Y - 5, end.X, end.Y, tx / tl,
                    ty / tl, false, false, [start, mid, end]) { Merged = members });
            }

            return result;
        }

        private static (double X, double Y) Point((double X, double Y) a, (double X, double Y) c,
            (double X, double Y) b, double t)
        {
            var u = 1 - t;

            return (u * u * a.X + 2 * u * t * c.X + t * t * b.X, u * u * a.Y + 2 * u * t * c.Y + t * t * b.Y);
        }

        private static double Clip((double X, double Y) a, (double X, double Y) c, (double X, double Y) b,
            (double X, double Y) centre, double radius, bool fromStart)
        {
            var lo = fromStart ? 0.0 : 0.5;
            var hi = fromStart ? 0.5 : 1.0;

            for (var step = 0; step < 24; step++)
            {
                var mid = (lo + hi) / 2;
                var p = Point(a, c, b, mid);
                var dx = p.X - centre.X;
                var dy = p.Y - centre.Y;
                var outside = dx * dx + dy * dy >= radius * radius;

                if (fromStart == outside)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            return (lo + hi) / 2;
        }
    }
}
