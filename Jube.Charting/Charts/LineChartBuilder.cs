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
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class LineChartBuilder : XyBuilderBase<LineChartBuilder>
    {
        private bool crosshair;
        private bool fromZero;
        private MarkerMode markers;
        private bool smooth;
        private bool step;

        public LineChartBuilder Crosshair(bool enabled = true)
        {
            crosshair = enabled;

            return this;
        }

        public LineChartBuilder Markers(MarkerMode mode)
        {
            markers = mode;

            return this;
        }

        public LineChartBuilder FromZero(bool enabled = true)
        {
            fromZero = enabled;

            return this;
        }

        public LineChartBuilder Stepped(bool enabled = true)
        {
            step = enabled;

            return this;
        }

        public LineChartBuilder Smooth(bool enabled = true)
        {
            smooth = enabled;

            return this;
        }

        public LineChart Build()
        {
            return new LineChart(new LineChartSpec
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
                Crosshair = crosshair,
                Markers = markers,
                YFromZero = fromZero,
                Step = step,
                Smooth = smooth,
                References = [.. ReferenceList],
                XDomain = XDomainFixed,
                YDomain = YDomainFixed
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}