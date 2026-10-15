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
    public sealed class ThresholdSweepChartBuilder : ChartBuilder<ThresholdSweepChartBuilder>
    {
        private double? current = 0.5;
        private bool mcc = true;
        private double? risk;
        private SweepResult? sweep;

        public ThresholdSweepChartBuilder()
        {
            Options = new ChartOptions { Width = 640, Height = 480 };
        }

        public ThresholdSweepChartBuilder Sweep(SweepResult sweepResult)
        {
            sweep = sweepResult;

            return this;
        }

        public ThresholdSweepChartBuilder FromScores(IEnumerable<bool> actual, IEnumerable<double> scores,
            double from = 0, double to = 1, int steps = 100)
        {
            sweep = PerformanceMath.Sweep([.. actual], [.. scores], from, to, steps);

            return this;
        }

        public ThresholdSweepChartBuilder CurrentThreshold(double? threshold)
        {
            current = threshold;

            return this;
        }

        public ThresholdSweepChartBuilder MinimumRiskThreshold(double? threshold)
        {
            risk = threshold;

            return this;
        }

        public ThresholdSweepChartBuilder ShowMcc(bool show = true)
        {
            mcc = show;

            return this;
        }

        public ThresholdSweepChart Build()
        {
            return new ThresholdSweepChart(new ThresholdSweepChartSpec
            {
                Options = Options,
                Sweep = sweep,
                CurrentThreshold = current,
                MinimumRiskThreshold = risk,
                ShowMcc = mcc
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}