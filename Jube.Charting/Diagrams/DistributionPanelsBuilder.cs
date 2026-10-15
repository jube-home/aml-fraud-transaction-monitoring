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
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    public sealed class DistributionPanelsBuilder : ChartBuilder<DistributionPanelsBuilder>
    {
        private readonly List<DistributionPanel> panels = [];
        private int columns = 3;

        public DistributionPanelsBuilder Panel(string title, params DistributionSeries[] series)
        {
            panels.Add(new DistributionPanel(title, series));

            return this;
        }

        public DistributionPanelsBuilder Columns(int columnCount)
        {
            columns = columnCount;

            return this;
        }

        public DistributionPanels Build()
        {
            return new DistributionPanels(new DistributionPanelsSpec
                { Options = Options, Panels = [.. panels], MaxColumns = columns });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}