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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class NeuralNetworkBuilder
    {
        public static ActivationNetwork Build(int inputs, int[] hidden, int outputs, int seed)
        {
            var sizes = hidden.Append(outputs).ToArray();
            var network = new ActivationNetwork(new SigmoidFunction(1d), inputs, null, sizes);
            var random = new Random(seed);
            var fanIn = inputs;

            foreach (var layer in network.Layers)
            {
                var limit = Math.Sqrt(6d / (fanIn + layer.Neurons.Length));

                foreach (var neuron in layer.Neurons)
                {
                    for (var k = 0; k < neuron.Weights.Length; k++)
                    {
                        neuron.Weights[k] = (random.NextDouble() * 2d - 1d) * limit;
                    }

                    ((ActivationNeuron)neuron).Threshold = 0d;
                }

                fanIn = layer.Neurons.Length;
            }

            return network;
        }

        public static (double[][][] Weights, double[][] Thresholds) Snapshot(ActivationNetwork network)
        {
            var weights = network.Layers.Select(l => l.Neurons.Select(n => (double[])n.Weights.Clone()).ToArray())
                .ToArray();

            var thresholds = network.Layers
                .Select(l => l.Neurons.Select(n => ((ActivationNeuron)n).Threshold).ToArray())
                .ToArray();

            return (weights, thresholds);
        }

        public static void Restore(ActivationNetwork network, double[][][] weights, double[][] thresholds)
        {
            for (var l = 0; l < network.Layers.Length; l++)
            {
                for (var k = 0; k < network.Layers[l].Neurons.Length; k++)
                {
                    var neuron = network.Layers[l].Neurons[k];
                    Array.Copy(weights[l][k], neuron.Weights, weights[l][k].Length);
                    ((ActivationNeuron)neuron).Threshold = thresholds[l][k];
                }
            }
        }
    }
}