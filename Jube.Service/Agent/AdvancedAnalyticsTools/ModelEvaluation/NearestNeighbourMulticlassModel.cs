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
    internal sealed class NearestNeighbourMulticlassModel : IMulticlassModel
    {
        private readonly int classes;
        private readonly int k;
        private readonly KNearestNeighbors model;

        private NearestNeighbourMulticlassModel(KNearestNeighbors model, int classes, int k)
        {
            this.model = model;
            this.classes = classes;
            this.k = k;
        }

        public double[][] Probabilities(double[][] rows)
        {
            var result = new double[rows.Length][];

            for (var i = 0; i < rows.Length; i++)
            {
                model.GetNearestNeighbors(rows[i], out var labels);
                var counts = new double[classes];

                foreach (var label in labels)
                {
                    counts[label]++;
                }

                result[i] = MulticlassMath.Normalise(counts);
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            return new ModelDescription(
                "k nearest neighbours on standardised predictors; class probabilities are the class shares among the k nearest training rows.",
                [ModelText.Fact("NeighbourCount", k)],
                [],
                null,
                []);
        }

        public static NearestNeighbourMulticlassModel Fit(MulticlassConfig c, double[][] x, int[] y, int classes)
        {
            var k = Math.Max(1, Math.Min(c.K, x.Length - 1));
            var model = new KNearestNeighbors(k);
            model.Learn(x, y);

            return new NearestNeighbourMulticlassModel(model, classes, k);
        }
    }
}