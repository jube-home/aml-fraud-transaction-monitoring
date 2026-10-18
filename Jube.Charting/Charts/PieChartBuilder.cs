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
using Jube.Charting.Exceptions;

namespace Jube.Charting.Charts
{
    public sealed class PieChartBuilder : ChartBuilder<PieChartBuilder>
    {
        private readonly List<PieSlice> slices = [];
        private string? center;
        private Func<double, string>? format;
        private double inner;
        private int max = 8;

        public PieChartBuilder Slice(string label, double value, string? color = null)
        {
            slices.Add(new PieSlice(label, value, color));

            return this;
        }

        public PieChartBuilder Slices(IEnumerable<string> labels, IEnumerable<double> values)
        {
            var l = labels.ToArray();
            var v = values.ToArray();

            if (l.Length != v.Length)
            {
                throw new ChartSpecificationException("Labels and values must have the same length.");
            }

            for (var i = 0; i < l.Length; i++)
            {
                slices.Add(new PieSlice(l[i], v[i]));
            }

            return this;
        }

        public PieChartBuilder Donut(double innerRadiusRatio = 0.6)
        {
            inner = Math.Clamp(innerRadiusRatio, 0.2, 0.9);

            return this;
        }

        public PieChartBuilder MaxSlices(int maxSlices)
        {
            max = Math.Clamp(maxSlices, 2, 8);

            return this;
        }

        public PieChartBuilder CenterLabel(string? label)
        {
            center = label;

            return this;
        }

        public PieChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public PieChart Build()
        {
            return new PieChart(new PieChartSpec
            {
                Options = Options,
                Slices = [.. slices],
                InnerRadiusRatio = inner,
                MaxSlices = max,
                CenterLabel = center,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}