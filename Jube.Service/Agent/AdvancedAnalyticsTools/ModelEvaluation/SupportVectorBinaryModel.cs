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

using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class SupportVectorBinaryModel : IBinaryModel
    {
        private readonly string kernel;
        private readonly Func<double[], double> score;
        private readonly int supportVectors;

        private SupportVectorBinaryModel(Func<double[], double> score, int supportVectors, string kernel)
        {
            this.score = score;
            this.supportVectors = supportVectors;
            this.kernel = kernel;
        }

        public bool OutputsProbability => false;

        public double[] Score(double[][] rows)
        {
            return rows.Select(score).ToArray();
        }

        public ModelDescription Describe(string[] names)
        {
            return new ModelDescription(
                $"A {kernel} support vector machine on standardised predictors; the score is the signed distance to the decision boundary, calibrated afterwards.",
                [ModelText.Fact("SupportVectors", supportVectors)],
                [],
                null,
                []);
        }

        public static SupportVectorBinaryModel Fit(LearnerConfig c, double[][] x, int[] y, double[]? w, FitContext ctx)
        {
            var labels = y.Select(v => v == 1).ToArray();

            return SeededLibrary.Run(ctx.Seed, () =>
            {
                if (c.Family == EvaluationFamily.LinearSupportVector || c.Kernel == SvmKernel.Linear)
                {
                    var linear = new LinearDualCoordinateDescent { Complexity = c.Complexity, Token = ctx.Token };
                    var machine = linear.Learn(x, labels, w);

                    return new SupportVectorBinaryModel(row => machine.Score(row), machine.SupportVectors?.Length ?? 0,
                        "linear");
                }

                var gamma = c.Gamma > 0 ? c.Gamma : 1d / x[0].Length;

                IKernel<double[]> kernel = c.Kernel switch
                {
                    SvmKernel.Polynomial => new Polynomial(c.Degree, 1d),
                    SvmKernel.Sigmoid => new Sigmoid(gamma, 0d),
                    SvmKernel.Laplacian => new Laplacian(Math.Sqrt(1d / (2d * gamma))),
                    _ => Gaussian.FromGamma(gamma)
                };

                var smo = new SequentialMinimalOptimization<IKernel<double[]>>
                {
                    Complexity = c.Complexity,
                    Kernel = kernel,
                    Token = ctx.Token
                };

                var model = smo.Learn(x, labels, w);

                return new SupportVectorBinaryModel(row => model.Score(row), model.SupportVectors?.Length ?? 0,
                    c.Kernel.ToString().ToLowerInvariant() + " kernel");
            });
        }
    }
}