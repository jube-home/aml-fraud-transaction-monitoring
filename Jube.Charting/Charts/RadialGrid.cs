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

using Jube.Charting.Core;
using Jube.Charting.Svg;
using Jube.Charting.Text;

namespace Jube.Charting.Charts
{
    internal static class RadialGrid
    {
        public static void Draw(ChartCanvas canvas, double cx, double cy, double radius, int spokes,
            Func<int, double> angle, IReadOnlyList<(double Radius, string Label)> rings,
            IReadOnlyList<string> spokeLabels,
            int firstRingLabelSpoke)
        {
            foreach (var (r, _) in rings.Where(t => t.Radius > 0.5))
            {
                var ring = new PathBuilder();

                for (var i = 0; i < spokes; i++)
                {
                    ring.Line(cx + Math.Cos(angle(i)) * r, cy + Math.Sin(angle(i)) * r);
                }

                if (spokes >= 3)
                {
                    ring.Close();
                    canvas.W.Empty("path", ("class", "gl"), ("d", ring.ToString()));
                }
            }

            for (var i = 0; i < spokes; i++)
            {
                var a = angle(i);

                canvas.W.Empty("line", ("class", "gl"), ("x1", Num.F(cx)), ("y1", Num.F(cy)),
                    ("x2", Num.F(cx + Math.Cos(a) * radius)), ("y2", Num.F(cy + Math.Sin(a) * radius)));

                var lx = cx + Math.Cos(a) * (radius + 10);
                var ly = cy + Math.Sin(a) * (radius + 10);
                var cos = Math.Cos(a);
                var anchor = Math.Abs(cos) < 0.2 ? "ctr" : cos > 0 ? string.Empty : "end";
                var dy = Math.Sin(a) > 0.5 ? 10 : Math.Sin(a) < -0.5 ? -2 : 4;
                var label = TextMetrics.Truncate(spokeLabels[i], 110, 11);

                canvas.W.Element("text", label, ("class", ("tk " + anchor).Trim()), ("x", Num.F(lx)),
                    ("y", Num.F(ly + dy)));
            }

            var labelAngle = spokes > 0 ? angle(Math.Min(firstRingLabelSpoke, spokes - 1)) : -Math.PI / 2;

            foreach (var (r, label) in rings.Where(t => t.Radius > 0.5))
            {
                canvas.W.Element("text", label, ("class", "tk ring"), ("x", Num.F(cx + Math.Cos(labelAngle) * r + 3)),
                    ("y", Num.F(cy + Math.Sin(labelAngle) * r - 2)));
            }
        }
    }
}