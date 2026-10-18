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
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Jube.Charting.More.Models;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme.Models;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class FacetChart(FacetChartSpec spec) : IStyleModeChart
    {
        private const double StripHeight = 22;
        private const double Gap = 10;
        private const double MinPanelHeight = 150;

        public FacetChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new FacetChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var panels = s.Panels;
            ChartLimits.Series(panels.Count);

            if (s.Columns is < 0 or > 12)
            {
                throw new ChartSpecificationException("Facet columns must be between 0 (automatic) and 12.");
            }

            if (!(s.PanelAspect is >= 0.2 and <= 3))
            {
                throw new ChartSpecificationException("Panel aspect must be between 0.2 and 3.");
            }

            var summary = "Small multiples of " + panels.Count + " panels: " +
                          MoreKit.Names([.. panels.Select(p => p.Title)], 8) + ".";

            var n = panels.Count;

            var columns = n == 0 ? 1 :
                s.Columns > 0 ? Math.Min(s.Columns, n) :
                Math.Clamp((int)Math.Ceiling(Math.Sqrt(n * s.Options.Width / Math.Max(s.Options.Height, 1))), 1,
                    Math.Min(n, 6));

            var rows = n == 0 ? 1 : (int)Math.Ceiling((double)n / columns);

            var probe = new ChartCanvas(s.Options with { Legend = LegendPosition.None }, id, summary, [], 0, []);
            var frame = probe.Frame(0, 0);
            var cellWidth = (frame.Width - Gap * (columns - 1)) / columns;
            var neededHeight = frame.Height;
            var cellHeight = (frame.Height - Gap * (rows - 1)) / rows;
            var height = s.Options.Height;

            if (cellHeight < MinPanelHeight)
            {
                cellHeight = Math.Max(MinPanelHeight, StripHeight + cellWidth * s.PanelAspect);
                height = Math.Min(4000, s.Options.Height + (cellHeight * rows + Gap * (rows - 1) - neededHeight));
            }

            var options = s.Options with { Legend = LegendPosition.None, Height = height };
            var canvas = new ChartCanvas(options, id, summary, [], 0, []);
            canvas.LimitInteractive(0);
            canvas.Family = StyleFamily.Facet;
            var plot = canvas.Frame(0, 0);
            canvas.Begin();

            (double, double)? sharedX = null;
            (double, double)? sharedY = null;

            if (s.SharedX)
            {
                sharedX = Union(panels.Select(p => p.XExtent));
            }

            if (s.SharedY)
            {
                sharedY = Union(panels.Select(p => p.YExtent));
            }

            for (var i = 0; i < n; i++)
            {
                var col = i % columns;
                var row = i / columns;
                var x = plot.Left + col * (cellWidth + Gap);
                var y = plot.Top + row * (cellHeight + Gap);

                canvas.W.Empty("rect", ("class", "fst"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(cellWidth)),
                    ("height", Num.F(StripHeight)), ("rx", "4"));

                canvas.W.Element("text", TextMetrics.Truncate(panels[i].Title, cellWidth - 12, 12, true),
                    ("class", "fsl"), ("x", Num.F(x + 8)), ("y", Num.F(y + 15)));

                var childOptions = new ChartOptions
                {
                    Id = id + "-p" + i,
                    Width = Math.Max(cellWidth, 40),
                    Height = Math.Max(cellHeight - StripHeight, 20),
                    Sizing = ChartSizing.Fixed,
                    Theme = s.Options.Theme,
                    Legend = LegendPosition.None,
                    TransparentBackground = true,
                    Interactive = s.Options.Interactive,
                    StyleMode = s.Options.StyleMode
                };

                var panel = panels[i].Build(new FacetContext(childOptions, sharedX, sharedY));

                if (panel.GetType().Assembly != typeof(ISvgChart).Assembly)
                {
                    throw new ChartSpecificationException("A facet panel must be a chart from this library.");
                }

                var child = panel.Render();

                if (!child.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"", StringComparison.Ordinal))
                {
                    throw new ChartSpecificationException("A facet panel must render a chart svg document.");
                }

                canvas.W.Raw("<svg x=\"" + Num.F(x) + "\" y=\"" + Num.F(y + StripHeight) + "\" " + child[5..]);
            }

            if (n == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles(0);

            return canvas.End();
        }

        private static (double, double)? Union(IEnumerable<(double Min, double Max)?> extents)
        {
            var e = new Extent();

            foreach (var x in extents)
            {
                if (x is { } v)
                {
                    e.Add(v.Min);
                    e.Add(v.Max);
                }
            }

            return e.Any ? MoreKit.NiceDomain(e.Min, e.Max) : null;
        }
    }
}