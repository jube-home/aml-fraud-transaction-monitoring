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

using Accord.Math;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class MahalanobisModel : IAnomalyModel
    {
        private readonly double[][] inverse;
        private readonly double[] mean;

        private MahalanobisModel(double[] mean, double[][] inverse)
        {
            this.mean = mean;
            this.inverse = inverse;
        }

        public string Summary =>
            "Mahalanobis distance: the distance from the training mean measured in units of the training covariance, so unusual combinations of correlated predictors stand out; higher is more anomalous.";

        public EvaluationNumber[] Facts()
        {
            return [ModelText.Fact("Variables", mean.Length)];
        }

        public double[] Score(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var d = rows[i].Select((v, j) => v - mean[j]).ToArray();
                var sum = 0d;

                for (var a = 0; a < d.Length; a++)
                {
                    for (var b = 0; b < d.Length; b++)
                    {
                        sum += d[a] * inverse[a][b] * d[b];
                    }
                }

                result[i] = Math.Sqrt(Math.Max(sum, 0d));
            }

            return result;
        }

        public static MahalanobisModel Fit(double[][] x)
        {
            var p = x[0].Length;
            var n = x.Length;
            var mean = new double[p];

            foreach (var row in x)
            {
                for (var j = 0; j < p; j++)
                {
                    mean[j] += row[j] / n;
                }
            }

            var covariance = new double[p][];

            for (var a = 0; a < p; a++)
            {
                covariance[a] = new double[p];
            }

            foreach (var row in x)
            {
                for (var a = 0; a < p; a++)
                {
                    for (var b = 0; b < p; b++)
                    {
                        covariance[a][b] += (row[a] - mean[a]) * (row[b] - mean[b]) / Math.Max(1, n - 1);
                    }
                }
            }

            for (var a = 0; a < p; a++)
            {
                covariance[a][a] += 1e-6 + 1e-6 * covariance[a][a];
            }

            return new MahalanobisModel(mean, covariance.Inverse());
        }
    }
}