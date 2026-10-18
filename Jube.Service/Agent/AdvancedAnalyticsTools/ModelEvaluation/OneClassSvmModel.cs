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
    internal sealed class OneClassSvmModel : IAnomalyModel
    {
        private readonly Func<double[], double> score;
        private readonly int supportVectors;

        private OneClassSvmModel(Func<double[], double> score, int supportVectors)
        {
            this.score = score;
            this.supportVectors = supportVectors;
        }

        public string Summary =>
            "One-class support vector machine: learns a boundary around the training rows in a Gaussian kernel space; the score is how far outside that boundary a row lies (higher is more anomalous).";

        public EvaluationNumber[] Facts()
        {
            return [ModelText.Fact("SupportVectors", supportVectors)];
        }

        public double[] Score(double[][] rows)
        {
            return rows.Select(r => -score(r)).ToArray();
        }

        public static OneClassSvmModel Fit(double[][] x, double nu, int seed, CancellationToken token)
        {
            var gamma = 1d / x[0].Length;

            var learner = new OneclassSupportVectorLearning<IKernel<double[]>>
            {
                Kernel = Gaussian.FromGamma(gamma),
                Nu = nu,
                Token = token
            };

            var model = SeededLibrary.Run(seed, () => learner.Learn(x));

            return new OneClassSvmModel(row => model.Score(row), model.SupportVectors?.Length ?? 0);
        }
    }
}