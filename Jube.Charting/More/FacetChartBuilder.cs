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

using Jube.Charting.Charts;
using Jube.Charting.Core;
using Jube.Charting.Interfaces;
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public sealed class FacetChartBuilder : ChartBuilder<FacetChartBuilder>
    {
        private readonly List<FacetPanel> panels = [];
        private double aspect = 0.66;
        private int columns;
        private bool sharedX;
        private bool sharedY;

        public FacetChartBuilder Panel(string title, Func<FacetContext, ISvgChart> build,
            (double Min, double Max)? xExtent = null, (double Min, double Max)? yExtent = null)
        {
            panels.Add(new FacetPanel(title, build, xExtent, yExtent));

            return this;
        }

        public FacetChartBuilder LinePanel(string title, IEnumerable<double> xs, IEnumerable<double> ys)
        {
            var x = xs.ToArray();
            var y = ys.ToArray();

            return Panel(title, c => Domains(Chart.Line().WithOptions(c.Options).Series(title, x, y), c), Extent(x),
                Extent(y));
        }

        public FacetChartBuilder ScatterPanel(string title, IEnumerable<double> xs, IEnumerable<double> ys)
        {
            var x = xs.ToArray();
            var y = ys.ToArray();

            return Panel(title, c => Domains(Chart.Scatter().WithOptions(c.Options).Series(title, x, y), c), Extent(x),
                Extent(y));
        }

        public FacetChartBuilder ColumnPanel(string title, IEnumerable<string> categories, IEnumerable<double> values)
        {
            var cats = categories.ToArray();
            var vals = values.ToArray();

            return Panel(title,
                c => Chart.Column().WithOptions(c.Options).Categories(cats).Series(title, vals).Build());
        }

        public FacetChartBuilder HistogramPanel(string title, IEnumerable<double> values)
        {
            var vals = values.ToArray();

            return Panel(title, c => Chart.Histogram().WithOptions(c.Options).Values(title, vals).Build());
        }

        private static ISvgChart Domains<TBuilder>(TBuilder builder, FacetContext c)
            where TBuilder : XyBuilderBase<TBuilder>
        {
            if (c.XDomain is { } xd)
            {
                builder.XDomain(xd.Min, xd.Max);
            }

            if (c.YDomain is { } yd)
            {
                builder.YDomain(yd.Min, yd.Max);
            }

            return builder;
        }

        private static (double, double)? Extent(double[] values)
        {
            var e = new Extent();

            foreach (var v in values)
            {
                e.Add(v);
            }

            return e.Any ? (e.Min, e.Max) : null;
        }

        public FacetChartBuilder Columns(int columnCount)
        {
            columns = columnCount;

            return this;
        }

        public FacetChartBuilder SharedScales(bool x = true, bool y = true)
        {
            sharedX = x;
            sharedY = y;

            return this;
        }

        public FacetChartBuilder PanelAspect(double aspectRatio)
        {
            aspect = aspectRatio;

            return this;
        }

        public FacetChart Build()
        {
            return new FacetChart(new FacetChartSpec
            {
                Options = Options,
                Panels = [.. panels],
                Columns = columns,
                SharedX = sharedX,
                SharedY = sharedY,
                PanelAspect = aspect
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}