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

namespace Jube.Charting.Charts
{
    public sealed class LearningCurveChartBuilder : ChartBuilder<LearningCurveChartBuilder>
    {
        private double[]? iterations;
        private bool lowerIsBetter = true;
        private bool markBest = true;
        private string metric = "Loss";
        private double[] train = [];
        private double[] validation = [];

        public LearningCurveChartBuilder Iterations(IEnumerable<double> iterationValues)
        {
            iterations = [.. iterationValues];

            return this;
        }

        public LearningCurveChartBuilder Train(IEnumerable<double> values)
        {
            train = [.. values];

            return this;
        }

        public LearningCurveChartBuilder Validation(IEnumerable<double> values)
        {
            validation = [.. values];

            return this;
        }

        public LearningCurveChartBuilder Metric(string metricName, bool lowerBetter = true)
        {
            metric = metricName;
            lowerIsBetter = lowerBetter;

            return this;
        }

        public LearningCurveChartBuilder MarkBest(bool mark = true)
        {
            markBest = mark;

            return this;
        }

        public LearningCurveChart Build()
        {
            return new LearningCurveChart(new LearningCurveChartSpec
            {
                Options = Options,
                Iterations = iterations,
                Train = train,
                Validation = validation,
                Metric = metric,
                LowerIsBetter = lowerIsBetter,
                MarkBest = markBest
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}