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

using Accord.Neuro;
using Accord.Neuro.Learning;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class NeuralMulticlassModel : IMulticlassModel
    {
        private readonly int epochs;
        private readonly object gate = new();
        private readonly int[] layers;
        private readonly ActivationNetwork network;

        private NeuralMulticlassModel(ActivationNetwork network, int[] layers, int epochs)
        {
            this.network = network;
            this.layers = layers;
            this.epochs = epochs;
        }

        public double[][] Probabilities(double[][] rows)
        {
            var result = new double[rows.Length][];

            lock (gate)
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    result[i] = MulticlassMath.Normalise(network.Compute(rows[i]).Select(v => Math.Max(v, 1e-9))
                        .ToArray());
                }
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            return new ModelDescription(
                $"A multilayer perceptron with hidden layers {string.Join("-", layers)} and one sigmoid output per class trained on one-hot targets; class probabilities are the normalised outputs.",
                [ModelText.Fact("EpochsRun", epochs)],
                [],
                null,
                []);
        }

        public static NeuralMulticlassModel Fit(MulticlassConfig c, double[][] x, int[] y, int classes, int seed,
            CancellationToken token)
        {
            var layers = c.Hidden.Length == 0 ? [8] : c.Hidden;
            var network = NeuralNetworkBuilder.Build(x[0].Length, layers, classes, seed);

            var targets = y.Select(v =>
            {
                var row = new double[classes];
                row[v] = 1d;

                return row;
            }).ToArray();

            var learner = new ResilientBackpropagationLearning(network);

            for (var epoch = 1; epoch <= c.Epochs; epoch++)
            {
                token.ThrowIfCancellationRequested();
                learner.RunEpoch(x, targets);
            }

            return new NeuralMulticlassModel(network, layers, c.Epochs);
        }
    }
}