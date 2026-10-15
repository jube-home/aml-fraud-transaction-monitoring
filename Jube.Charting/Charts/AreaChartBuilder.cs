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

namespace Jube.Charting.Charts
{
    public sealed class AreaChartBuilder : XyBuilderBase<AreaChartBuilder>
    {
        private bool crosshair;
        private bool percent;
        private bool smooth;
        private bool stacked;

        public AreaChartBuilder Stacked(bool enabled = true)
        {
            stacked = enabled;

            return this;
        }

        public AreaChartBuilder Crosshair(bool enabled = true)
        {
            crosshair = enabled;

            return this;
        }

        public AreaChartBuilder PercentStacked(bool enabled = true)
        {
            percent = enabled;

            return this;
        }

        public AreaChartBuilder Smooth(bool enabled = true)
        {
            smooth = enabled;

            return this;
        }

        public AreaChart Build()
        {
            return new AreaChart(new AreaChartSpec
            {
                Options = Options,
                Series = [.. SeriesList],
                DataLabels = ShowDataLabels,
                Bands = [.. BandList],
                Annotations = [.. AnnotationList],
                XFormat = XFormatter,
                YFormat = YFormatter,
                Y2Format = Y2Formatter,
                XAxis = XKind,
                Categories = CategoryLabels,
                YScale = YKind,
                Stacked = stacked,
                Percent = percent,
                Smooth = smooth,
                Crosshair = crosshair
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}