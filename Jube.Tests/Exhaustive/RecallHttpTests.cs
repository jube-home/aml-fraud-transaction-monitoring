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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Accord.Neuro;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Models;
using Jube.Test.Infrastructure.ModelScaffolding;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RecallHttpTests(ModelScaffoldFixture fixture) : IClassFixture<ModelScaffoldFixture>
    {
        private const string Json = "application/json";

        private static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-2 * x));
        }

        private static ActivationNetwork Network(double threshold, params double[] weights)
        {
            var network = new ActivationNetwork(new SigmoidFunction(), weights.Length, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];

            for (var i = 0; i < weights.Length; i++)
            {
                neuron.Weights[i] = weights[i];
            }

            neuron.Threshold = threshold;

            return network;
        }

        private Task<InvokeResponse> RecallAsync(ActivationNetwork network, string body,
            params ExhaustiveSearchInstancePromotedTrialInstanceVariable[] variables)
        {
            return RecallAsync(network, body, null, variables);
        }

        private async Task<InvokeResponse> RecallAsync(ActivationNetwork network, string body,
            ExhaustiveCalibration? calibration,
            params ExhaustiveSearchInstancePromotedTrialInstanceVariable[] variables)
        {
            var engineModel = fixture.Engine.FindActiveModel(fixture.Shared.ModelGuid) ??
                              throw new InvalidOperationException("The model is not synchronised.");

            var instance = new ExhaustiveSearchInstance
            {
                Name = "RecallHttpExhaustive",
                Id = -1,
                Guid = Guid.NewGuid(),
                TopologyNetwork = network
            };

            if (calibration != null)
            {
                instance.Calibration = calibration;
            }

            instance.NetworkVariablesInOrder.AddRange(variables);

            engineModel.Snapshot.ExhaustiveModels.Add(instance);

            try
            {
                return await fixture.Api.SendAsync("POST", "/api/Invoke/ExhaustiveSearchInstance/" + instance.Guid,
                    fixture.Shared.UserName, Encoding.UTF8.GetBytes(body), Json);
            }
            finally
            {
                engineModel.Snapshot.ExhaustiveModels.Remove(instance);
            }
        }

        private static ExhaustiveSearchInstancePromotedTrialInstanceVariable Variable(string name)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstanceVariable { Name = name };
        }

        [Theory]
        [InlineData(0.1)]
        [InlineData(0.2)]
        [InlineData(0.5)]
        [InlineData(-0.37)]
        [InlineData(1.234)]
        [InlineData(-3)]
        [InlineData(4)]
        public async Task ResponseIsTheScoreRoundedToFourDecimalPlacesAsync(double input)
        {
            var response = await RecallAsync(Network(0, 1), "{\"X\": " + input.ToString("R",
                CultureInfo.InvariantCulture) + "}", Variable("X"));

            Assert.Equal(200, response.Status);
            Assert.Equal(Math.Round(Sigmoid(input), 4), response.Json.GetProperty("score").GetDouble());
        }

        [Theory]
        [InlineData(-4.0)]
        [InlineData(0.1)]
        [InlineData(2.5)]
        public async Task ACalibratedModelReturnsTheCalibratedProbabilityRoundedToFourDecimalsAsync(double input)
        {
            var calibration = ExhaustiveCalibration.FromColumns("platt", "logit",
                1.9, -0.3, 1e-6, 0.5, 0.02, "PopulationCount");

            var raw = Network(0, 1).Compute([input])[0];

            var response = await RecallAsync(Network(0, 1), "{\"X\": " + input.ToString("R",
                CultureInfo.InvariantCulture) + "}", calibration, Variable("X"));

            Assert.Equal(200, response.Status);
            Assert.Equal(Math.Round(calibration.Apply(raw), 4), response.Json.GetProperty("score").GetDouble());
            Assert.NotEqual(Math.Round(raw, 4), response.Json.GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task ASmallProbabilityKeepsItsResolutionAtFourDecimalsAsync()
        {
            var calibration = ExhaustiveCalibration.FromColumns("platt", "logit",
                1.9, -0.3, 1e-6, 0.5, 0.02, "Override");

            var response = await RecallAsync(Network(0, 1), "{\"X\": 0}", calibration, Variable("X"));

            Assert.InRange(response.Json.GetProperty("score").GetDouble(), 0.014, 0.016);

            Assert.NotEqual(Math.Round(response.Json.GetProperty("score").GetDouble(), 2),
                response.Json.GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task ResponseIsNotAnUnroundedScoreAsync()
        {
            var response = await RecallAsync(Network(0, 1), "{\"X\": 0.1}", Variable("X"));

            Assert.NotEqual(Sigmoid(0.1), response.Json.GetProperty("score").GetDouble());
            Assert.Equal(0.5498, response.Json.GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task ANonFiniteScoreIsA500WithNoBodyAsync()
        {
            var response = await RecallAsync(Network(0, double.NaN), "{\"X\": 1}", Variable("X"));

            Assert.Equal(500, response.Status);
            Assert.Empty(response.BodyBytes);
        }

        [Fact]
        public async Task ANetworkThatThrowsIsSwallowedToAZeroScoreWith200Async()
        {
            var response = await RecallAsync(Network(0, 1, 1), "{\"X\": 1}", Variable("X"));

            Assert.Equal(200, response.Status);
            Assert.Equal(JsonValueKind.Object, response.Json.ValueKind);
            Assert.Equal(0, response.Json.GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task AMissingKeyIsImputedWithTheMeanAsInThePipelineAsync()
        {
            var variable = new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                { Name = "X", Mean = 0.3, Sd = 2, NormalisationTypeId = 1 };

            var response = await RecallAsync(Network(0, 1), "{}", variable);

            Assert.Equal(Math.Round(Sigmoid(0.3), 4), response.Json.GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task AStandardisedVariableUsesTheStoredMeanAndSdAsync()
        {
            var variable = new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                { Name = "X", Mean = 10, Sd = 4, NormalisationTypeId = 2 };

            var response = await RecallAsync(Network(0, 1), "{\"X\": 12}", variable);

            Assert.Equal(Math.Round(Sigmoid(0.5), 4), response.Json.GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task VariablesAreConsumedInStoredOrderNotBodyOrderAsync()
        {
            var response = await RecallAsync(Network(0, 1, 10), "{\"B\": 0.05, \"A\": 0.3}",
                Variable("A"), Variable("B"));

            Assert.Equal(Math.Round(Sigmoid(0.3 + 0.5), 4), response.Json.GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task ExtraKeysInTheBodyAreIgnoredAsync()
        {
            var response = await RecallAsync(Network(0, 1), "{\"X\": 0.1, \"Y\": 99, \"Z\": \"anything\"}",
                Variable("X"));

            Assert.Equal(0.5498, response.Json.GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task TheResponseCarriesPerVariableRelevanceThatReconstructsTheRawScoreAsync()
        {
            var response = await RecallAsync(Network(0, 1, 10), "{\"A\": 0.3, \"B\": 0.05}",
                Variable("A"), Variable("B"));

            var relevance = response.Json.GetProperty("relevance").EnumerateArray().ToArray();
            Assert.Equal(2, relevance.Length);
            Assert.Equal("A", relevance[0].GetProperty("name").GetString());
            Assert.Equal("B", relevance[1].GetProperty("name").GetString());

            var raw = Network(0, 1, 10).Compute([0.3, 0.05])[0];

            var reconstructed = relevance.Sum(r => r.GetProperty("lrpRelevance").GetDouble()) +
                                response.Json.GetProperty("baseValue").GetDouble();

            Assert.Equal(Math.Round(raw, 4), Math.Round(reconstructed, 4));
        }

        [Theory]
        [InlineData("{\"X\": [1]}")]
        [InlineData("{\"X\": \"NaN\"}")]
        [InlineData("{\"X\": 1e999}")]
        public async Task TypeGateRejectionsAre400WithAPlainMessageAsync(string body)
        {
            var response = await RecallAsync(Network(0, 1), body, Variable("X"));

            Assert.Equal(400, response.Status);
            Assert.Equal("\"Exhaustive recall input has an invalid shape or types.\"", response.Body);
        }

        [Fact]
        public async Task AnUnknownGuidIs404Async()
        {
            var response = await fixture.Api.SendAsync("POST",
                "/api/Invoke/ExhaustiveSearchInstance/" + Guid.NewGuid(), fixture.Shared.UserName,
                Encoding.UTF8.GetBytes("{}"), Json);

            Assert.Equal(404, response.Status);
        }
    }
}