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
using System.Collections.Generic;
using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using FluentAssertions;
using Jube.Engine.Helpers;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class NetworkRoundTripTests(ITestOutputHelper output)
    {
        private static readonly JsonSerializationHelper helper = new();

        public static IEnumerable<object[]> Functions()
        {
            yield return [nameof(GaussianFunction), new GaussianFunction()];
            yield return ["BipolarSigmoid1", new BipolarSigmoidFunction(1)];
            yield return ["BipolarSigmoid2", new BipolarSigmoidFunction(2)];
            yield return [nameof(ThresholdFunction), new ThresholdFunction()];
            yield return ["Sigmoid1", new SigmoidFunction(1)];
            yield return ["Sigmoid2", new SigmoidFunction(2)];
            yield return ["Linear1", new LinearFunction(1)];
            yield return ["Linear2", new LinearFunction(2)];
            yield return [nameof(RectifiedLinearFunction), new RectifiedLinearFunction()];
            yield return [nameof(IdentityFunction), new IdentityFunction()];
            yield return [nameof(BernoulliFunction), new BernoulliFunction()];
        }

        [Theory]
        [MemberData(nameof(Functions))]
        public void ATrainedNetworkOfEveryActivationFunctionSurvivesTheEngineRoundTrip(string label,
            IActivationFunction function)
        {
            var network = new ActivationNetwork(function, 5, null, 6, 3, 1);
            var random = new Random(7);

            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    for (var w = 0; w < neuron.Weights.Length; w++)
                    {
                        neuron.Weights[w] = random.NextDouble() - 0.5;
                    }

                    ((ActivationNeuron)neuron).Threshold = random.NextDouble() - 0.5;
                }
            }

            var json = JsonConvert.SerializeObject(network, helper.TopologyNetworkJsonSerializerSettings);
            var errors = new List<string>();
            var settings = helper.DeserializeTopologyNetworkJsonSerializerSettings;
            settings.Error += (_, args) => errors.Add(args.ErrorContext.Error.Message);

            var loaded = JsonConvert.DeserializeObject<ActivationNetwork>(json, settings);

            output.WriteLine($"{label}: {json.Length} chars, {errors.Count} deserialisation errors");
            errors.Should().BeEmpty();
            loaded.Should().NotBeNull();

            for (var trial = 0; trial < 5; trial++)
            {
                var input = new double[5];

                for (var i = 0; i < 5; i++)
                {
                    input[i] = random.NextDouble() * 4 - 2;
                }

                loaded!.Compute(input)[0].Should().Be(network.Compute(input)[0], label);
            }
        }
    }
}