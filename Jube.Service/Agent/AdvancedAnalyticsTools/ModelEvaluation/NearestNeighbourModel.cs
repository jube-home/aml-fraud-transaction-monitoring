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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class NearestNeighbourModel : IBinaryModel
    {
        private readonly int k;
        private readonly KNearestNeighbors model;

        private NearestNeighbourModel(KNearestNeighbors model, int k)
        {
            this.model = model;
            this.k = k;
        }

        public bool OutputsProbability => true;

        public double[] Score(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                model.GetNearestNeighbors(rows[i], out var labels);
                result[i] = labels.Length == 0 ? 0d : labels.Count(l => l == 1) / (double)labels.Length;
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            return new ModelDescription(
                "k nearest neighbours on standardised predictors with Euclidean distance; the score is the share of positives among the k nearest training rows, calibrated afterwards. There is no fitted structure to inspect.",
                [ModelText.Fact("NeighbourCount", k)],
                [],
                null,
                []);
        }

        public static NearestNeighbourModel Fit(LearnerConfig c, double[][] x, int[] y)
        {
            var k = Math.Max(1, Math.Min(c.K, x.Length - 1));
            var model = new KNearestNeighbors(k);
            model.Learn(x, y);

            return new NearestNeighbourModel(model, k);
        }
    }
}