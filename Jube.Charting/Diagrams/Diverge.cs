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

using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Ticks;

namespace Jube.Charting.Diagrams
{
    internal static class Diverge
    {
        public static int Bin(double value, double maxAbs)
        {
            if (!double.IsFinite(value) || !(maxAbs > 0) || value == 0)
            {
                return 5;
            }

            var t = Math.Clamp(Math.Abs(value) / maxAbs, 0, 1);
            var step = 2 + (int)Math.Round(t * 3);

            return value > 0 ? 5 + step : 5 - step;
        }

        public static int CellBin(double value, double maxAbs)
        {
            if (!double.IsFinite(value) || !(maxAbs > 0))
            {
                return 5;
            }

            var t = Math.Clamp(value / maxAbs, -1, 1);

            return Math.Clamp(5 + (int)Math.Round(t * 5), 0, 10);
        }

        public static double Width(double value, double maxAbs, double min = 0.6, double max = 4)
        {
            if (!double.IsFinite(value) || !(maxAbs > 0))
            {
                return 1.2;
            }

            return min + (max - min) * Math.Clamp(Math.Abs(value) / maxAbs, 0, 1);
        }

        public static void Key(Scene scene, double x, double y, double maxAbs, string label)
        {
            const double cell = 16;
            var w = scene.W;
            w.Element("text", label, ("class", "key"), ("x", Num.F(x)), ("y", Num.F(y + 11)));
            var x0 = x + TextMetrics.Width(label, 11) + 10;

            w.Element("text", "-" + NumberFormat.Value(maxAbs), ("class", "kn"), ("x", Num.F(x0)),
                ("y", Num.F(y + 11)));

            var bar = x0 + TextMetrics.Width("-" + NumberFormat.Value(maxAbs), 10.5) + 6;

            for (var i = 0; i < 11; i++)
            {
                w.Empty("rect", ("class", "cell d" + i), ("x", Num.F(bar + i * cell)), ("y", Num.F(y)),
                    ("width", Num.F(cell)), ("height", "14"));
            }

            w.Element("text", "+" + NumberFormat.Value(maxAbs), ("class", "kn"), ("x", Num.F(bar + 11 * cell + 6)),
                ("y", Num.F(y + 11)));
        }

        public static double KeyWidth(double maxAbs, string label)
        {
            var n = NumberFormat.Value(maxAbs);

            return TextMetrics.Width(label, 11) + 10 + 2 * (TextMetrics.Width("-" + n, 10.5) + 6) + 11 * 16 + 6;
        }
    }
}