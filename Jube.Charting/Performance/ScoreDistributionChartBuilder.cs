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
    public sealed class ScoreDistributionChartBuilder : ChartBuilder<ScoreDistributionChartBuilder>
    {
        private DistributionResult? distribution;
        private string negative = "Negative";
        private string positive = "Positive";
        private double? threshold;

        public ScoreDistributionChartBuilder()
        {
            Options = new ChartOptions { Width = 640, Height = 480 };
        }

        public ScoreDistributionChartBuilder Classes(string positiveLabel, string negativeLabel)
        {
            positive = positiveLabel;
            negative = negativeLabel;

            return this;
        }

        public ScoreDistributionChartBuilder Distribution(DistributionResult distributionResult)
        {
            distribution = distributionResult;

            return this;
        }

        public ScoreDistributionChartBuilder FromScores(IEnumerable<bool> actual, IEnumerable<double> scores,
            double from = 0, double to = 1, int bins = 20, IEnumerable<double>? weights = null)
        {
            distribution = PerformanceMath.Distribution([.. actual], [.. scores], from, to, bins,
                weights?.ToArray());

            return this;
        }

        public ScoreDistributionChartBuilder Threshold(double? thresholdValue)
        {
            threshold = thresholdValue;

            return this;
        }

        public ScoreDistributionChart Build()
        {
            return new ScoreDistributionChart(new ScoreDistributionChartSpec
            {
                Options = Options,
                Distribution = distribution,
                PositiveName = positive,
                NegativeName = negative,
                Threshold = threshold
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}