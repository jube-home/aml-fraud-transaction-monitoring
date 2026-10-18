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
    public sealed class KsChartBuilder : ChartBuilder<KsChartBuilder>
    {
        private (double Min, double Max)? domain;
        private string negative = "Negative";
        private string positive = "Positive";
        private KsResult? result;
        private double? threshold;

        public KsChartBuilder()
        {
            Options = new ChartOptions { Width = 640, Height = 480 };
        }

        public KsChartBuilder Classes(string positiveLabel, string negativeLabel)
        {
            positive = positiveLabel;
            negative = negativeLabel;

            return this;
        }

        public KsChartBuilder Result(KsResult ksResult)
        {
            result = ksResult;

            return this;
        }

        public KsChartBuilder FromScores(IEnumerable<bool> actual, IEnumerable<double> scores,
            IEnumerable<double>? weights = null)
        {
            result = PerformanceMath.Ks([.. actual], [.. scores], weights: weights?.ToArray());

            return this;
        }

        public KsChartBuilder Threshold(double? thresholdValue)
        {
            threshold = thresholdValue;

            return this;
        }

        public KsChartBuilder Domain(double min, double max)
        {
            domain = (min, max);

            return this;
        }

        public KsChart Build()
        {
            return new KsChart(new KsChartSpec
            {
                Options = Options,
                Result = result,
                PositiveName = positive,
                NegativeName = negative,
                Threshold = threshold,
                XDomain = domain
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}