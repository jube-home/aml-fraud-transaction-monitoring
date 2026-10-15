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

using Accord.MachineLearning;
using Accord.Statistics.Distributions.Fitting;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class GaussianMixtureDensityModel : IAnomalyModel
    {
        private readonly int components;
        private readonly Func<double[], double> logDensity;

        private GaussianMixtureDensityModel(Func<double[], double> logDensity, int components)
        {
            this.logDensity = logDensity;
            this.components = components;
        }

        public string Summary =>
            "Gaussian mixture density: rows are modelled as a mixture of multivariate normal clusters, and the score is the negative log density; rows in low-density regions are anomalous.";

        public EvaluationNumber[] Facts()
        {
            return [ModelText.Fact("Components", components)];
        }

        public double[] Score(double[][] rows)
        {
            return rows.Select(r => -logDensity(r)).ToArray();
        }

        public static GaussianMixtureDensityModel Fit(double[][] x, int components, int seed, CancellationToken token)
        {
            var k = Math.Max(1, Math.Min(components, x.Length / 5));

            return SeededLibrary.Run(seed, () =>
            {
                var mixture = new GaussianMixtureModel(k)
                {
                    Options = new NormalOptions { Regularization = 1e-4 },
                    MaxIterations = 100,
                    ParallelOptions = { MaxDegreeOfParallelism = 1 }
                };

                var fitted = mixture.Learn(x);
                var distribution = fitted.Model;

                return new GaussianMixtureDensityModel(row => distribution.LogProbabilityDensityFunction(row), k);
            });
        }
    }
}