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
    public sealed class ViolinChartBuilder : ChartBuilder<ViolinChartBuilder>
    {
        private readonly List<DistributionGroup> groups = [];
        private double adjust = 1;
        private double? bandwidth;
        private bool box = true;
        private Func<double, string>? format;
        private bool horizontal;
        private bool points;
        private DensityScale scale = DensityScale.Area;

        public ViolinChartBuilder Values(string name, IEnumerable<double> values, string? color = null)
        {
            groups.Add(new DistributionGroup(name, [.. values], color));

            return this;
        }

        public ViolinChartBuilder Horizontal(bool enabled = true)
        {
            horizontal = enabled;

            return this;
        }

        public ViolinChartBuilder Bandwidth(double bandwidthValue)
        {
            bandwidth = bandwidthValue;

            return this;
        }

        public ViolinChartBuilder BandwidthAdjust(double factor)
        {
            adjust = factor;

            return this;
        }

        public ViolinChartBuilder Scale(DensityScale scaleMode)
        {
            scale = scaleMode;

            return this;
        }

        public ViolinChartBuilder Box(bool show = true)
        {
            box = show;

            return this;
        }

        public ViolinChartBuilder Points(bool show = true)
        {
            points = show;

            return this;
        }

        public ViolinChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public ViolinChart Build()
        {
            return new ViolinChart(new ViolinChartSpec
            {
                Options = Options,
                Groups = [.. groups],
                Horizontal = horizontal,
                Bandwidth = bandwidth,
                BandwidthAdjust = adjust,
                Scale = scale,
                ShowBox = box,
                ShowPoints = points,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}