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

using System;
using System.Linq;
using FluentAssertions;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Service.ModelDiagrams;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class NetworkTopologyAdapterTests
    {
        private const string Sigmoid = "Accord.Neuro.SigmoidFunction, Accord.Neuro";

        private static NetworkDiagramSpec Spec(string json, ModelDiagramOptions? options = null)
        {
            return ((NetworkDiagram)ModelDiagramRenderer.Build(json, options)).Spec;
        }

        public static string Network(double[][] hiddenWeights, double[] hiddenThresholds, double[][] outputWeights,
            double[] outputThresholds, string activation = Sigmoid, double? alpha = 2)
        {
            JObject Layer(double[][] weights, double[] thresholds)
            {
                return new JObject
                {
                    ["$type"] = "Accord.Neuro.ActivationLayer, Accord.Neuro",
                    ["neurons"] = new JArray(weights.Select((w, i) => new JObject
                    {
                        ["$type"] = "Accord.Neuro.ActivationNeuron, Accord.Neuro",
                        ["threshold"] = thresholds[i],
                        ["activationFunction"] = alpha is { } a
                            ? new JObject { ["$type"] = activation, ["alpha"] = a }
                            : new JObject { ["$type"] = activation },
                        ["weights"] = new JArray(w)
                    }))
                };
            }

            return new JObject
            {
                ["inputsCount"] = hiddenWeights[0].Length,
                ["layers"] = new JArray(Layer(hiddenWeights, hiddenThresholds), Layer(outputWeights, outputThresholds))
            }.ToString();
        }

        public static string TwoTwoOne()
        {
            return Network([[0.5, -1.5], [2.5, 3.5]], [0.1, -0.2], [[7.0, -8.0]], [0.3]);
        }

        [Fact]
        public void ATwoTwoOneNetworkPutsEachWeightOnItsEdge()
        {
            var spec = Spec(TwoTwoOne());

            spec.Layers.Select(l => l.Nodes.Count).Should().Equal(2, 2, 1);
            spec.Layers[0].Weights.Should().BeNull();
            spec.Layers[1].Weights![0].Should().Equal(0.5, -1.5);
            spec.Layers[1].Weights![1].Should().Equal(2.5, 3.5);
            spec.Layers[2].Weights![0].Should().Equal(7.0, -8.0);
        }

        [Fact]
        public void ThresholdsBecomeTheBiasesOfTheirNeurons()
        {
            var spec = Spec(TwoTwoOne());

            spec.Layers[0].Nodes.Should().OnlyContain(n => n.Bias == null);
            spec.Layers[1].Nodes.Select(n => n.Bias).Should().Equal(0.1, -0.2);
            spec.Layers[2].Nodes.Single().Bias.Should().Be(0.3);
        }

        [Fact]
        public void RolesAndLabelsFollowTheLayers()
        {
            var spec = Spec(TwoTwoOne());

            spec.Layers.Select(l => l.Role).Should().Equal(NetworkRole.Input, NetworkRole.Hidden, NetworkRole.Output);
            spec.Layers[0].Nodes.Select(n => n.Label).Should().Equal("x1", "x2");
            spec.Layers[1].Nodes.Select(n => n.Label).Should().Equal("h1.1", "h1.2");
            spec.Layers[2].Nodes.Select(n => n.Label).Should().Equal("y");
        }

        [Fact]
        public void InputNamesFromTheOptionsLabelTheInputLayer()
        {
            var spec = Spec(TwoTwoOne(), new ModelDiagramOptions { VariableNames = ["amount", "velocity"] });

            spec.Layers[0].Nodes.Select(n => n.Label).Should().Equal("amount", "velocity");
        }

        [Fact]
        public void TheOverrideBeatsTheStoredAnnotations()
        {
            var model = ModelDiagramFixtures.Parsed("topology:Sigmoid2");
            var names = Enumerable.Range(0, 26).Select(i => "n" + i).ToArray();

            var spec = Spec(model.ToString(), new ModelDiagramOptions { VariableNames = names });

            spec.Layers[0].Nodes.Select(n => n.Label).Should().Equal(names);
        }

        [Fact]
        public void AWrongLengthOverrideIsRejected()
        {
            Action act = () => Spec(TwoTwoOne(), new ModelDiagramOptions { VariableNames = ["one"] });

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.TopologyData), MemberType = typeof(ModelDiagramFixtures))]
        public void EveryRealTopologyMapsItsWeightsThresholdsAndInputNames(string name)
        {
            var json = ModelDiagramFixtures.Topology(name);
            var model = JObject.Parse(json);
            var spec = Spec(json);

            spec.Layers.Select(l => l.Nodes.Count).Should().Equal(26, 5, 1);
            var annotations = ((JArray)model["annotations"]!).Select(a => (string)a["name"]!).ToArray();
            spec.Layers[0].Nodes.Select(n => n.Label).Should().Equal(annotations);

            for (var l = 0; l < 2; l++)
            {
                var neurons = (JArray)model["layers"]![l]!["neurons"]!;

                for (var n = 0; n < neurons.Count; n++)
                {
                    spec.Layers[l + 1].Weights![n].Should().Equal(neurons[n]["weights"]!.Select(w => (double)w));
                    spec.Layers[l + 1].Nodes[n].Bias.Should().Be((double)neurons[n]["threshold"]!);
                }
            }

            spec.Layers[2].Role.Should().Be(NetworkRole.Output);
        }

        [Theory]
        [InlineData("Sigmoid2", "Sigmoid, alpha 2")]
        [InlineData("BipolarSigmoid1", "BipolarSigmoid, alpha 1")]
        [InlineData("Gaussian", "Gaussian, alpha 1")]
        [InlineData("RectifiedLinear", "RectifiedLinear")]
        [InlineData("Linear1", "Linear, alpha 1")]
        [InlineData("Identity", "Identity")]
        public void ActivationNamesAreReadFromTheStoredFunctionTypes(string name, string expected)
        {
            var spec = Spec(ModelDiagramFixtures.Topology(name));

            spec.Layers[1].Activation.Should().Be(expected);
            spec.Layers[2].Activation.Should().Be(expected);
            spec.Layers[0].Activation.Should().BeNull();
        }

        [Fact]
        public void MixedActivationsInALayerAreLabelledMixed()
        {
            var model = JObject.Parse(TwoTwoOne());

            model["layers"]![0]!["neurons"]![1]!["activationFunction"]!["$type"] =
                "Accord.Neuro.IdentityFunction, Accord.Neuro";

            Spec(model.ToString()).Layers[1].Activation.Should().Be("Mixed");
        }

        [Theory]
        [InlineData("Some.Namespace.<script>Function, x")]
        [InlineData("Some.Namespace.Very_Long_Function_Name_That_Is_Over_The_Limit_Function, x")]
        public void UnusualFunctionTypesFallBackToCustom(string type)
        {
            var model = JObject.Parse(TwoTwoOne());

            foreach (var neuron in model["layers"]![0]!["neurons"]!)
            {
                neuron["activationFunction"]!["$type"] = type;
            }

            Spec(model.ToString()).Layers[2].Activation.Should().Be("Sigmoid, alpha 2");
            Spec(model.ToString()).Layers[1].Activation.Should().Be("Custom");
        }

        [Fact]
        public void WeightsThatDoNotMatchThePreviousLayerAreMalformed()
        {
            var model = JObject.Parse(TwoTwoOne());
            model["layers"]![1]!["neurons"]![0]!["weights"] = new JArray(1.0, 2.0, 3.0);

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*layer 2*2 weights*");
        }

        [Fact]
        public void ADeclaredInputCountThatDisagreesIsMalformed()
        {
            var model = JObject.Parse(TwoTwoOne());
            model["inputsCount"] = 5;

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*declared input count*");
        }

        [Fact]
        public void ANetworkWithNoFiniteWeightsDoesNotVisualise()
        {
            var json = Network([[double.NaN, double.NaN]], [0.0], [[double.NaN]], [0.0]);
            var model = JObject.Parse(json);

            foreach (var w in model.SelectTokens("$.layers[*].neurons[*].weights").Cast<JArray>())
            {
                for (var i = 0; i < w.Count; i++)
                {
                    w[i] = "NaN";
                }
            }

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotSupportedException>();
        }

        [Fact]
        public void NeuronsWithoutAThresholdAreNotANetwork()
        {
            var model = JObject.Parse(TwoTwoOne());
            ((JObject)model["layers"]![0]!["neurons"]![0]!).Remove("threshold");

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>();
        }

        [Fact]
        public void ADeepNetworkBeyondTheChartLimitIsTooLarge()
        {
            var model = JObject.Parse(TwoTwoOne());
            var hidden = model["layers"]![0]!;
            var layers = new JArray { hidden.DeepClone() };

            for (var i = 0; i < 17; i++)
            {
                var square = new JObject
                {
                    ["neurons"] = new JArray(Enumerable.Range(0, 2).Select(_ => new JObject
                    {
                        ["threshold"] = 0.0,
                        ["weights"] = new JArray(1.0, 1.0),
                        ["activationFunction"] = new JObject { ["$type"] = Sigmoid }
                    }))
                };
                layers.Add(square);
            }

            model["layers"] = layers;

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*summary*");
        }

        [Fact]
        public void AWideRealNetworkIsElidedNotRejected()
        {
            var svg = ModelDiagramRenderer.Render(ModelDiagramFixtures.Topology("Sigmoid2"));

            svg.Should().Contain("<svg ");
        }
    }
}