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
using Jube.Charting.Interfaces;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class AreaChart(AreaChartSpec spec) : IStyleModeChart
    {
        public AreaChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new AreaChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;

            return ChartRun.Execute(s.Options, id => XyRenderer.Render(new XyModel
            {
                Options = s.Options,
                Kind = s.Percent ? "100% stacked area chart" : s.Stacked ? "Stacked area chart" : "Area chart",
                Mode = XyMode.Area,
                Series = s.Series,
                DataLabels = s.DataLabels,
                Bands = s.Bands,
                Annotations = s.Annotations,
                XFormat = s.XFormat,
                YFormat = s.YFormat,
                Y2Format = s.Y2Format,
                XAxis = s.XAxis,
                Categories = s.Categories,
                YScale = s.YScale,
                Stacked = s.Stacked || s.Percent,
                PercentStack = s.Percent,
                Smooth = s.Smooth,
                Crosshair = s.Crosshair,
                Markers = MarkerMode.Off,
                YFromZero = true
            }, id));
        }
    }
}