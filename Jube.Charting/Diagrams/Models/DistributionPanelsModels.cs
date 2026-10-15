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

using Jube.Charting.Core.Models;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Diagrams.Models
{
    public sealed record DistributionSeries(string Name, IReadOnlyList<double> X, IReadOnlyList<double> Y)
    {
        public bool Bars { get; init; }

        public static DistributionSeries Normal(string name, double mean, double standardDeviation, int points = 61)
        {
            if (!double.IsFinite(mean) || !(standardDeviation > 0) || !double.IsFinite(standardDeviation) || points < 3)
            {
                throw new ChartSpecificationException(
                    "A normal density needs a finite mean, a positive deviation and at least 3 points.");
            }

            var xs = new double[points];
            var ys = new double[points];

            for (var i = 0; i < points; i++)
            {
                xs[i] = mean + standardDeviation * (-4 + 8.0 * i / (points - 1));
                var z = (xs[i] - mean) / standardDeviation;
                ys[i] = Math.Exp(-0.5 * z * z) / (standardDeviation * Math.Sqrt(2 * Math.PI));
            }

            return new DistributionSeries(name, xs, ys);
        }

        public static DistributionSeries Histogram(string name, IReadOnlyList<double> edges,
            IReadOnlyList<double> heights)
        {
            return new DistributionSeries(name, edges, heights) { Bars = true };
        }
    }

    public sealed record DistributionPanel(string Title, IReadOnlyList<DistributionSeries> Series);

    public sealed record DistributionPanelsSpec
    {
        public ChartOptions Options { get; init; } = new();
        public IReadOnlyList<DistributionPanel> Panels { get; init; } = [];
        public int MaxColumns { get; init; } = 3;
    }
}