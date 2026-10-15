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

using System.Collections.Generic;
using Accord.Neuro;

namespace Jube.Engine.Exhaustive.Utilities
{
    public static class RecallExplainability
    {
        private const double LrpEpsilon = 1e-6;

        public static RecallRelevance Explain(ActivationNetwork network, double[] input, IReadOnlyList<string> names)
        {
            var forward = ForwardActivations(network, input);
            var connectionWeight = ConnectionWeight(network, input);
            var propagation = LayerwiseRelevancePropagation(network, forward);

            var variables = new VariableRelevance[input.Length];

            for (var i = 0; i < input.Length; i++)
            {
                variables[i] = new VariableRelevance
                {
                    Name = names[i],
                    ConnectionWeight = connectionWeight[i],
                    LrpRelevance = propagation.NodeRelevance[0][i]
                };
            }

            return new RecallRelevance
            {
                Variables = variables,
                BaseValue = propagation.BaseValue,
                NodeRelevance = propagation.NodeRelevance,
                EdgeRelevance = propagation.EdgeRelevance
            };
        }

        internal static double[][] ForwardActivations(Network network, double[] input)
        {
            var activations = new double[network.Layers.Length + 1][];
            activations[0] = input;

            var current = input;

            for (var l = 0; l < network.Layers.Length; l++)
            {
                current = network.Layers[l].Compute(current);
                activations[l + 1] = current;
            }

            return activations;
        }

        internal static double[] ConnectionWeight(ActivationNetwork network, double[] input)
        {
            var grad = new double[network.Layers[^1].Neurons.Length];

            for (var i = 0; i < grad.Length; i++)
            {
                grad[i] = 1;
            }

            for (var l = network.Layers.Length - 1; l >= 0; l--)
            {
                var layer = network.Layers[l];
                var prevGrad = new double[layer.InputsCount];

                for (var n = 0; n < layer.Neurons.Length; n++)
                {
                    var weights = layer.Neurons[n].Weights;

                    for (var j = 0; j < weights.Length; j++)
                    {
                        prevGrad[j] += grad[n] * weights[j];
                    }
                }

                grad = prevGrad;
            }

            var contributions = new double[input.Length];

            for (var i = 0; i < input.Length; i++)
            {
                contributions[i] = grad[i] * input[i];
            }

            return contributions;
        }

        internal static (IReadOnlyList<IReadOnlyList<double>> NodeRelevance,
            IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> EdgeRelevance, double BaseValue)
            LayerwiseRelevancePropagation(ActivationNetwork network, double[][] forward)
        {
            var nodeRelevance = new double[network.Layers.Length + 1][];
            var edgeRelevance = new double[network.Layers.Length][][];
            nodeRelevance[^1] = [forward[^1][0]];
            var relevance = nodeRelevance[^1];
            var baseValue = 0d;

            for (var l = network.Layers.Length - 1; l >= 0; l--)
            {
                var layer = network.Layers[l];
                var layerInput = forward[l];
                var prevRelevance = new double[layer.InputsCount];
                var layerEdgeRelevance = new double[layer.Neurons.Length][];

                for (var n = 0; n < layer.Neurons.Length; n++)
                {
                    var neuron = (ActivationNeuron)layer.Neurons[n];
                    var weights = neuron.Weights;
                    var neuronEdgeRelevance = new double[weights.Length];
                    layerEdgeRelevance[n] = neuronEdgeRelevance;

                    var z = neuron.Threshold;

                    for (var j = 0; j < weights.Length; j++)
                    {
                        z += weights[j] * layerInput[j];
                    }

                    var denominator = z + (z >= 0 ? LrpEpsilon : -LrpEpsilon);

                    if (denominator == 0)
                    {
                        continue;
                    }

                    for (var j = 0; j < weights.Length; j++)
                    {
                        var share = weights[j] * layerInput[j] / denominator * relevance[n];
                        neuronEdgeRelevance[j] = share;
                        prevRelevance[j] += share;
                    }

                    baseValue += neuron.Threshold / denominator * relevance[n];
                }

                edgeRelevance[l] = layerEdgeRelevance;
                nodeRelevance[l] = prevRelevance;
                relevance = prevRelevance;
            }

            return (nodeRelevance, edgeRelevance, baseValue);
        }
    }
}