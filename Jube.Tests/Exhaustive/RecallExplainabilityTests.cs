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
using System.Linq;
using Accord.Neuro;
using FluentAssertions;
using Jube.Engine.Exhaustive.Utilities;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class RecallExplainabilityTests
    {
        private static ActivationNetwork OneLayerIdentityNetwork(double[] weights, double threshold)
        {
            var network = new ActivationNetwork(new IdentityFunction(), weights.Length,
                [], 1);

            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];

            for (var i = 0; i < weights.Length; i++)
            {
                neuron.Weights[i] = weights[i];
            }

            neuron.Threshold = threshold;

            return network;
        }

        private static ActivationNetwork TwoLayerSigmoidNetwork()
        {
            return TwoLayerNetwork(new SigmoidFunction());
        }

        private static ActivationNetwork TwoLayerNetwork(IActivationFunction hiddenFunction)
        {
            var network = new ActivationNetwork(hiddenFunction, 3, [], 2, 1);

            var hidden = network.Layers[0].Neurons;
            ((ActivationNeuron)hidden[0]).Weights[0] = 0.5;
            ((ActivationNeuron)hidden[0]).Weights[1] = -0.3;
            ((ActivationNeuron)hidden[0]).Weights[2] = 0.2;
            ((ActivationNeuron)hidden[0]).Threshold = 0.1;
            ((ActivationNeuron)hidden[1]).Weights[0] = -0.4;
            ((ActivationNeuron)hidden[1]).Weights[1] = 0.6;
            ((ActivationNeuron)hidden[1]).Weights[2] = 0.1;
            ((ActivationNeuron)hidden[1]).Threshold = -0.2;

            var output = (ActivationNeuron)network.Layers[1].Neurons[0];
            output.Weights[0] = 0.7;
            output.Weights[1] = -0.5;
            output.Threshold = 0.05;

            return network;
        }

        [Fact]
        public void Explain_OneLayerIdentityNetwork_ConnectionWeightIsWeightTimesInput()
        {
            var network = OneLayerIdentityNetwork([2, -3, 0.5], 1);
            double[] input = [4, 1, -2];

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);

            result.Variables.Select(v => v.ConnectionWeight).Should()
                .Equal(2 * 4, -3 * 1, 0.5 * -2);
        }

        [Fact]
        public void Explain_OneLayerIdentityNetwork_LrpRelevanceAndBaseValueReconstructTheOutputExactly()
        {
            var network = OneLayerIdentityNetwork([2, -3, 0.5], 1);
            double[] input = [4, 1, -2];

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);
            var output = network.Compute(input)[0];

            result.Variables.Select(v => v.LrpRelevance).Sum().Should()
                .BeApproximately(output - result.BaseValue, 1e-6);

            result.Variables[0].LrpRelevance.Should().BeApproximately(2 * 4, 1e-5);
            result.Variables[1].LrpRelevance.Should().BeApproximately(-3 * 1, 1e-5);
            result.Variables[2].LrpRelevance.Should().BeApproximately(0.5 * -2, 1e-5);
            result.BaseValue.Should().BeApproximately(1, 1e-5);
        }

        [Fact]
        public void Explain_AnIgnoredInputWithZeroWeightContributesZeroToBothMeasures()
        {
            var network = OneLayerIdentityNetwork([0, 5], 2);
            double[] input = [100, 3];

            var result = RecallExplainability.Explain(network, input, ["Ignored", "Used"]);

            result.Variables[0].ConnectionWeight.Should().Be(0);
            result.Variables[0].LrpRelevance.Should().Be(0);
            result.Variables[1].ConnectionWeight.Should().Be(15);
        }

        [Fact]
        public void Explain_TwoLayerSigmoidNetwork_LrpRelevancePlusBaseValueReconstructsTheOutput()
        {
            var network = TwoLayerSigmoidNetwork();
            double[] input = [1, -2, 0.5];

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);
            var output = network.Compute(input)[0];

            (result.Variables.Sum(v => v.LrpRelevance) + result.BaseValue).Should().BeApproximately(output, 1e-4);
        }

        [Fact]
        public void Explain_TwoLayerSigmoidNetwork_ConnectionWeightIsSignedByThePathWeightProduct()
        {
            var network = TwoLayerSigmoidNetwork();
            double[] input = [1, -2, 0.5];

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);

            var expectedPathWeightA = 0.5 * 0.7 + -0.4 * -0.5;
            result.Variables[0].ConnectionWeight.Should().BeApproximately(expectedPathWeightA * input[0], 1e-9);
        }

        [Theory]
        [InlineData(1, -2, 0.5)]
        [InlineData(-1, -2, 0.5)]
        [InlineData(3, 5, -4)]
        public void Explain_ReconstructionHoldsRegardlessOfWhichActivationFunctionTheTopologySearchPicked(
            double a, double b, double c)
        {
            double[] input = [a, b, c];

            IActivationFunction[] candidates =
            [
                new SigmoidFunction(), new SigmoidFunction(2), new BipolarSigmoidFunction(),
                new RectifiedLinearFunction(), new IdentityFunction()
            ];

            foreach (var hiddenFunction in candidates)
            {
                var network = TwoLayerNetwork(hiddenFunction);

                var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);
                var output = network.Compute(input)[0];

                (result.Variables.Sum(v => v.LrpRelevance) + result.BaseValue).Should().BeApproximately(output, 1e-4);
            }
        }

        [Fact]
        public void Explain_ADeadRectifiedLinearHiddenNeuronPropagatesZeroRelevanceThroughItAndStillReconstructs()
        {
            var network = TwoLayerNetwork(new RectifiedLinearFunction());
            double[] input = [-10, -10, -10];

            var hiddenOutputs = network.Layers[0].Compute(input);
            hiddenOutputs[0].Should().Be(0);
            hiddenOutputs[1].Should().Be(0);

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);
            var output = network.Compute(input)[0];

            result.Variables.Should().OnlyContain(v => v.LrpRelevance == 0);
            result.BaseValue.Should().BeApproximately(output, 1e-6);
        }

        [Fact]
        public void Explain_NodeRelevanceAtTheInputBoundaryMatchesEachVariablesLrpRelevance()
        {
            var network = TwoLayerSigmoidNetwork();
            double[] input = [1, -2, 0.5];

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);

            result.NodeRelevance[0].Should().Equal(result.Variables.Select(v => v.LrpRelevance));
        }

        [Fact]
        public void Explain_NodeRelevanceAtTheOutputBoundaryIsTheRawNetworkOutput()
        {
            var network = TwoLayerSigmoidNetwork();
            double[] input = [1, -2, 0.5];

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);
            var output = network.Compute(input)[0];

            result.NodeRelevance[^1].Should().ContainSingle().Which.Should().Be(output);
        }

        [Fact]
        public void Explain_EdgeRelevanceHasTheSameShapeAsEachLayersWeightMatrix()
        {
            var network = TwoLayerSigmoidNetwork();
            double[] input = [1, -2, 0.5];

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);

            result.EdgeRelevance.Should().HaveCount(2);
            result.EdgeRelevance[0].Should().HaveCount(2).And.OnlyContain(row => row.Count == 3);
            result.EdgeRelevance[1].Should().HaveCount(1).And.OnlyContain(row => row.Count == 2);
        }

        [Fact]
        public void Explain_WithZeroThresholdsEachNeuronsIncomingEdgeRelevanceSumsToItsOwnNodeRelevance()
        {
            var network = TwoLayerNetwork(new SigmoidFunction());

            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    ((ActivationNeuron)neuron).Threshold = 0;
                }
            }

            double[] input = [1, -2, 0.5];

            var result = RecallExplainability.Explain(network, input, ["A", "B", "C"]);

            for (var l = 0; l < network.Layers.Length; l++)
            {
                for (var n = 0; n < result.EdgeRelevance[l].Count; n++)
                {
                    result.EdgeRelevance[l][n].Sum().Should().BeApproximately(result.NodeRelevance[l + 1][n], 1e-5);
                }
            }
        }
    }
}