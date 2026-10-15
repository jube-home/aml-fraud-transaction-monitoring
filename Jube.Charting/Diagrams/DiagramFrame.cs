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
using Jube.Charting.Theme.Models;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    internal static class DiagramFrame
    {
        public static FrameFit Fit(ChartOptions options, string summary, IReadOnlyList<LegendEntry> legend,
            double contentWidth, double contentHeight, double bottomReserve, string what)
        {
            var width = Math.Max(options.Width, contentWidth + 2 * ChartCanvas.Pad + 8);

            if (width > DiagramLimits.MaxCanvas)
            {
                throw new DiagramTooLargeException(what + " width", (long)Math.Ceiling(width), DiagramLimits.MaxCanvas,
                    "Reduce the number of items or split the diagram.");
            }

            var probe = new ChartCanvas(options with { Width = width, Height = 400 }, "jc-0", summary, legend, 8, [],
                true);

            var plot = probe.Frame(0, bottomReserve);
            var height = plot.Top + contentHeight + bottomReserve + ChartCanvas.Pad;

            if (height > DiagramLimits.MaxCanvas)
            {
                throw new DiagramTooLargeException(what + " height", (long)Math.Ceiling(height),
                    DiagramLimits.MaxCanvas,
                    "Reduce the number of items or split the diagram.");
            }

            var sized = options with { Width = Math.Ceiling(width), Height = Math.Ceiling(Math.Max(height, 60)) };
            var x0 = plot.Left + (width - 2 * ChartCanvas.Pad - 4 - contentWidth) / 2;

            return new FrameFit(sized, x0, plot.Top);
        }

        public static Scene Begin(FrameFit fit, string id, string summary, IReadOnlyList<LegendEntry> legend,
            long marks)
        {
            var canvas = new ChartCanvas(fit.Options, id, summary, legend, 8, [], true)
                { Family = StyleFamily.Diagram };
            canvas.LimitInteractive(marks);
            canvas.Frame(0, 0);
            canvas.Begin();

            return new Scene(canvas);
        }
    }
}