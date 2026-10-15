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
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public sealed class SankeyChartBuilder : ChartBuilder<SankeyChartBuilder>
    {
        private readonly List<SankeyLink> links = [];
        private readonly List<SankeyNode> nodes = [];
        private Func<double, string>? format;
        private bool justify = true;
        private double nodeWidth = 14;
        private double padding = 10;

        public SankeyChartBuilder Node(string name, string? color = null)
        {
            nodes.Add(new SankeyNode(name, color));

            return this;
        }

        public SankeyChartBuilder Link(string source, string target, double value)
        {
            links.Add(new SankeyLink(source, target, value));

            return this;
        }

        public SankeyChartBuilder NodeWidth(double width)
        {
            nodeWidth = width;

            return this;
        }

        public SankeyChartBuilder NodePadding(double paddingValue)
        {
            padding = paddingValue;

            return this;
        }

        public SankeyChartBuilder Justify(bool enabled = true)
        {
            justify = enabled;

            return this;
        }

        public SankeyChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public SankeyChart Build()
        {
            return new SankeyChart(new SankeyChartSpec
            {
                Options = Options,
                Nodes = [.. nodes],
                Links = [.. links],
                NodeWidth = nodeWidth,
                NodePadding = padding,
                Justify = justify,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}