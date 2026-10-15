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

using Accord.MachineLearning.Bayes;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class TreeAugmentedBayesModel : IBinaryModel
    {
        private readonly TreeAugmentedNaiveBayes model;

        private TreeAugmentedBayesModel(TreeAugmentedNaiveBayes model)
        {
            this.model = model;
        }

        public bool OutputsProbability => false;

        public double[] Score(double[][] rows)
        {
            return rows.Select(r => model.LogLikelihood(r, 1) - model.LogLikelihood(r, 0)).ToArray();
        }

        public ModelDescription Describe(string[] names)
        {
            var parents = model.Parents;

            var rules = names.Select((n, j) =>
                    parents[j] >= 0 ? $"{n} depends on {names[parents[j]]}" : $"{n} is the root")
                .ToArray();

            var linked = Enumerable.Range(0, names.Length).Select(j => parents[j] >= 0 ? 1d : 0d).ToArray();

            return new ModelDescription(
                "Tree augmented naive Bayes: every predictor depends on the class and on one other predictor chosen by a maximum spanning tree of conditional mutual information, relaxing the independence assumption of naive Bayes.",
                [ModelText.Fact("LinkedVariables", linked.Sum())],
                rules,
                null,
                []);
        }

        public static TreeAugmentedBayesModel Fit(LearnerConfig c, double[][] x, int[] y, double[]? w, FitContext ctx)
        {
            var learner = new TreeAugmentedNaiveBayesLearning
            {
                Smoothing = c.ResolvedRegularisation(1d),
                Token = ctx.Token,
                ParallelOptions = { MaxDegreeOfParallelism = 1 }
            };

            return new TreeAugmentedBayesModel(learner.Learn(x, y, w));
        }
    }
}