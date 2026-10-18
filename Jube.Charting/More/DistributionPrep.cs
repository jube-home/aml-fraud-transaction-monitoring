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

using Jube.Charting.Exceptions;
using Jube.Charting.More.Models;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    internal static class DistributionPrep
    {
        public static (DistributionSummary Summary, DensityEstimate Estimate)[] Prepare(
            IReadOnlyList<DistributionGroup> groups,
            double? bandwidth, double adjust, double cut)
        {
            if (!(adjust > 0 && double.IsFinite(adjust)))
            {
                throw new ChartSpecificationException("The bandwidth adjustment must be a positive finite number.");
            }

            ChartLimits.Series(groups.Count);
            ChartLimits.Observations(groups.Sum(g => (long)g.Values.Count));

            return
            [
                .. groups.Select(g =>
                {
                    var finite = DensityMath.Finite(g.Values);
                    var h = bandwidth ?? DensityMath.SilvermanBandwidth(finite) * adjust;

                    var estimate = finite.Length >= 2
                        ? DensityMath.Estimate(finite, h, 96, cut)
                        : new DensityEstimate([], [], h, finite.Length);

                    return (DensityMath.Summarise(finite), estimate);
                })
            ];
        }

        public static List<TipRow> Rows(string name, int series, DistributionSummary sm, DensityEstimate e,
            Func<double, string> f)
        {
            var rows = new List<TipRow> { new(name, true, series), new("n: " + sm.Count) };

            if (sm.Count > 0)
            {
                rows.Add(new TipRow("Median: " + f(sm.Median) + ", mean: " + f(sm.Mean)));
                rows.Add(new TipRow("Q1 to Q3: " + f(sm.Q1) + " to " + f(sm.Q3)));
                rows.Add(new TipRow("Min to max: " + f(sm.Min) + " to " + f(sm.Max)));

                if (e.Density.Count > 0)
                {
                    rows.Add(new TipRow("Bandwidth: " + NumberFormat.Value(Math.Round(e.Bandwidth, 4))));
                }
            }

            return rows;
        }
    }
}