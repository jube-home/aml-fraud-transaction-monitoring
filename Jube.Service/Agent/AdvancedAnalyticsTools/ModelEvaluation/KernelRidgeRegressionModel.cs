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
using Accord.Math.Decompositions;
using Accord.Statistics.Kernels;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class KernelRidgeRegressionModel : IRegressionModel
    {
        private readonly double[] alpha;
        private readonly IKernel<double[]> kernel;
        private readonly double lambda;
        private readonly double mean;
        private readonly double[][] support;

        private KernelRidgeRegressionModel(double[][] support, double[] alpha, IKernel<double[]> kernel, double mean,
            double lambda)
        {
            this.support = support;
            this.alpha = alpha;
            this.kernel = kernel;
            this.mean = mean;
            this.lambda = lambda;
        }

        public double[] Predict(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = mean;

                for (var s = 0; s < support.Length; s++)
                {
                    sum += alpha[s] * kernel.Function(support[s], rows[i]);
                }

                result[i] = sum;
            }

            return result;
        }

        public EvaluationNumber[] Coefficients(string[] names)
        {
            return [];
        }

        public ModelDescription Describe(string[] names)
        {
            return new ModelDescription(
                "Kernel ridge regression on standardised predictors: a ridge penalty of 1/Complexity on a kernel expansion over every training row. Small Complexity gives a smoother fit, large Complexity a closer one.",
                [ModelText.Fact("TrainingRowsInExpansion", support.Length), ModelText.Fact("RidgePenalty", lambda)],
                [],
                null,
                []);
        }

        public static KernelRidgeRegressionModel Fit(RegressorConfig c, double[][] x, double[] y)
        {
            var n = x.Length;
            var gamma = c.Gamma > 0 ? c.Gamma : 1d / x[0].Length;

            IKernel<double[]> kernel = c.Kernel switch
            {
                SvmKernel.Linear => new Linear(0d),
                SvmKernel.Polynomial => new Polynomial(3, 1d),
                SvmKernel.Sigmoid => new Sigmoid(gamma, 0d),
                SvmKernel.Laplacian => new Laplacian(Math.Sqrt(1d / (2d * gamma))),
                _ => Gaussian.FromGamma(gamma)
            };

            var lambda = 1d / c.Complexity;
            var matrix = new double[n][];

            for (var i = 0; i < n; i++)
            {
                matrix[i] = new double[n];
            }

            for (var i = 0; i < n; i++)
            {
                for (var j = i; j < n; j++)
                {
                    var value = kernel.Function(x[i], x[j]);
                    matrix[i][j] = value;
                    matrix[j][i] = value;
                }

                matrix[i][i] += lambda;
            }

            var mean = y.Average();
            var centred = y.Select(v => v - mean).ToArray();
            var decomposition = new JaggedCholeskyDecomposition(matrix);

            double[] alpha;

            if (decomposition.IsPositiveDefinite)
            {
                alpha = decomposition.Solve(centred);
            }
            else
            {
                alpha = matrix.Solve(centred);
            }

            if (alpha.Any(v => !double.IsFinite(v)))
            {
                throw new ArgumentException(
                    "The kernel system could not be solved; try a smaller Complexity or a different kernel width.");
            }

            return new KernelRidgeRegressionModel(x.Select(r => (double[])r.Clone()).ToArray(), alpha, kernel, mean,
                lambda);
        }
    }
}