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
using Jube.Charting.Performance.Models;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Performance
{
    public sealed class MetricsPanelChartBuilder : ChartBuilder<MetricsPanelChartBuilder>
    {
        private readonly List<MetricGroup> groups = [];
        private int columns = 4;

        public MetricsPanelChartBuilder()
        {
            Options = new ChartOptions { Width = 720, Height = 400 };
        }

        public MetricsPanelChartBuilder Group(string title, IEnumerable<MetricTile> tiles)
        {
            groups.Add(new MetricGroup(title, [.. tiles]));

            return this;
        }

        public MetricsPanelChartBuilder Columns(int columnCount)
        {
            columns = columnCount;

            return this;
        }

        public MetricsPanelChart Build()
        {
            return new MetricsPanelChart(new MetricsPanelChartSpec
            {
                Options = Options,
                Groups = [.. groups],
                Columns = columns
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}