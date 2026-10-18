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
    public sealed class RidgelineChartBuilder : ChartBuilder<RidgelineChartBuilder>
    {
        private readonly List<DistributionGroup> groups = [];
        private double adjust = 1;
        private double? bandwidth;
        private Func<double, string>? format;
        private double overlap = 1.6;
        private DensityScale scale = DensityScale.Area;

        public RidgelineChartBuilder Values(string name, IEnumerable<double> values, string? color = null)
        {
            groups.Add(new DistributionGroup(name, [.. values], color));

            return this;
        }

        public RidgelineChartBuilder Bandwidth(double bandwidthValue)
        {
            bandwidth = bandwidthValue;

            return this;
        }

        public RidgelineChartBuilder BandwidthAdjust(double factor)
        {
            adjust = factor;

            return this;
        }

        public RidgelineChartBuilder Scale(DensityScale scaleMode)
        {
            scale = scaleMode;

            return this;
        }

        public RidgelineChartBuilder Overlap(double overlapValue)
        {
            overlap = overlapValue;

            return this;
        }

        public RidgelineChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public RidgelineChart Build()
        {
            return new RidgelineChart(new RidgelineChartSpec
            {
                Options = Options,
                Groups = [.. groups],
                Bandwidth = bandwidth,
                BandwidthAdjust = adjust,
                Scale = scale,
                Overlap = overlap,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}