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

using Accord.Collections;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class NearestNeighbourRegressionModel : IRegressionModel
    {
        private readonly int k;
        private readonly KDTree<double> tree;

        public NearestNeighbourRegressionModel(KDTree<double> tree, int k)
        {
            this.tree = tree;
            this.k = k;
        }

        public double[] Predict(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = 0d;
                var count = 0;

                foreach (var neighbour in tree.Nearest(rows[i], k))
                {
                    sum += neighbour.Node.Value;
                    count++;
                }

                result[i] = count == 0 ? 0d : sum / count;
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
                "k nearest neighbours on standardised predictors: the prediction is the mean target of the k closest training rows. There is no fitted structure to inspect.",
                [ModelText.Fact("NeighbourCount", k)],
                [],
                null,
                []);
        }

        public static NearestNeighbourRegressionModel Fit(RegressorConfig c, double[][] x, double[] y)
        {
            var k = Math.Max(1, Math.Min(c.K, x.Length - 1));

            return new NearestNeighbourRegressionModel(KDTree.FromData(x.Select(r => (double[])r.Clone()).ToArray(), y),
                k);
        }
    }
}