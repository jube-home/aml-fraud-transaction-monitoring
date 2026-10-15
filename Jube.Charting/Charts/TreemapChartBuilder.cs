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

using Jube.Charting.Charts.Models;
using Jube.Charting.Core;

namespace Jube.Charting.Charts
{
    public sealed class TreemapChartBuilder : ChartBuilder<TreemapChartBuilder>
    {
        private readonly List<TreemapNode> nodes = [];
        private Func<double, string>? format;

        public TreemapChartBuilder Item(string label, double value)
        {
            nodes.Add(new TreemapNode(label, value));

            return this;
        }

        public TreemapChartBuilder Group(string label, IEnumerable<(string Label, double Value)> children)
        {
            nodes.Add(new TreemapNode(label, 0, [.. children.Select(c => new TreemapNode(c.Label, c.Value))]));

            return this;
        }

        public TreemapChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public TreemapChart Build()
        {
            return new TreemapChart(new TreemapChartSpec
                { Options = Options, Nodes = [.. nodes], ValueFormat = format });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}