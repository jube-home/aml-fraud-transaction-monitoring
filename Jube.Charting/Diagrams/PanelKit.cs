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
using Jube.Charting.Interfaces;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Ticks;

namespace Jube.Charting.Diagrams
{
    internal readonly record struct PanelPlan(
        int Columns,
        // ReSharper disable once NotAccessedPositionalProperty.Global
        int Rows,
        double CellWidth,
        double CellHeight,
        double Gap,
        double Width,
        double Height);

    internal static class PanelKit
    {
        public static PanelPlan Plan(int panels, double optionsWidth, double cellHeight, double minCell = 210,
            int maxColumns = 3)
        {
            var columns = Math.Min(panels, maxColumns);
            var gap = 12.0;
            var available = Math.Max(optionsWidth - 2 * ChartCanvas.Pad - 8, columns * minCell + (columns - 1) * gap);
            var cell = (available - (columns - 1) * gap) / columns;
            var rows = (panels + columns - 1) / columns;

            return new PanelPlan(columns, rows, cell, cellHeight, gap, columns * cell + (columns - 1) * gap,
                rows * cellHeight + (rows - 1) * gap);
        }

        public static (double X, double Y) Origin(PanelPlan plan, int index)
        {
            return (index % plan.Columns * (plan.CellWidth + plan.Gap),
                (double)index / plan.Columns * (plan.CellHeight + plan.Gap));
        }

        public static void Frame(SvgWriter w, double x, double y, PanelPlan plan, string title)
        {
            w.Empty("rect", ("class", "pf"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(plan.CellWidth)),
                ("height", Num.F(plan.CellHeight)), ("rx", "6"));

            w.Element("text", TextMetrics.Truncate(title, plan.CellWidth - 20, 12, true), ("class", "ph"),
                ("x", Num.F(x + 10)), ("y", Num.F(y + 18)));
        }

        public static IReadOnlyList<double> Ticks(double min, double max, int target)
        {
            return [.. ExtendedTicks.Generate(min, max, target).Where(v => v >= min - 1e-9 && v <= max + 1e-9)];
        }

        public static void XAxis(SvgWriter w, IScale scale, double left, double right, double baseline, int target)
        {
            var ticks = Ticks(Math.Min(scale.DomainMin, scale.DomainMax), Math.Max(scale.DomainMin, scale.DomainMax),
                target);

            var labels = NumberFormat.Axis(ticks);

            w.Empty("line", ("class", "ax"), ("x1", Num.F(left)), ("y1", Num.F(baseline)), ("x2", Num.F(right)),
                ("y2", Num.F(baseline)));

            var lastEnd = double.NegativeInfinity;

            for (var i = 0; i < ticks.Count; i++)
            {
                var px = scale.Map(ticks[i]);

                w.Empty("line", ("class", "ax"), ("x1", Num.F(px)), ("y1", Num.F(baseline)), ("x2", Num.F(px)),
                    ("y2", Num.F(baseline + 3)));

                var half = TextMetrics.Width(labels[i], 10.5) / 2;

                if (px - half < lastEnd + 4 || px + half > right + 14)
                {
                    continue;
                }

                lastEnd = px + half;
                w.Element("text", labels[i], ("class", "kn ctr"), ("x", Num.F(px)), ("y", Num.F(baseline + 15)));
            }
        }
    }
}