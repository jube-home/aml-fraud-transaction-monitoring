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
using System.Text;
using System.Threading.Tasks;
using Accord.Neuro;
using Jube.Engine.Exhaustive.Models;
using Jube.Test.Analytics.Categorical;
using Jube.Test.Infrastructure.ModelScaffolding;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CategoricalRecallHttpTests(ModelScaffoldFixture fixture) : IClassFixture<ModelScaffoldFixture>
    {
        private const string Json = "application/json";
        private static readonly double[] weights = [0.5, 2, 3, 5, 7, 11];

        private static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-2 * x));
        }

        private static ActivationNetwork Network()
        {
            var network = new ActivationNetwork(new SigmoidFunction(), weights.Length, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];

            for (var i = 0; i < weights.Length; i++)
            {
                neuron.Weights[i] = weights[i];
            }

            neuron.Threshold = 0;

            return network;
        }

        private static ExhaustiveSearchInstancePromotedTrialInstanceVariable[] Variables()
        {
            var encoder = CategoricalTestData.Country();

            var variables = new[]
            {
                new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                    { Name = "Abstraction.A", NormalisationTypeId = 0 }
            };

            return
            [
                .. variables, .. encoder.Indicators.Select((indicator, index) =>
                    new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                    {
                        Name = indicator.Name,
                        ProcessingTypeId = 1,
                        NormalisationTypeId = 1,
                        CategoricalEncoder = encoder,
                        CategoricalIndicatorIndex = index
                    })
            ];
        }

        private async Task<InvokeResponse> RecallAsync(string body)
        {
            var engineModel = fixture.Engine.FindActiveModel(fixture.Shared.ModelGuid) ??
                              throw new InvalidOperationException("The model is not synchronised.");

            var instance = new ExhaustiveSearchInstance
            {
                Name = "CategoricalRecallHttpExhaustive",
                Id = -1,
                Guid = Guid.NewGuid(),
                TopologyNetwork = Network()
            };
            instance.NetworkVariablesInOrder.AddRange(Variables());

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

        [Theory]
        [InlineData("{\"Abstraction.A\":1,\"Payload.Country\":\"US\"}", 2.5)]
        [InlineData("{\"Abstraction.A\":1,\"Payload.Country\":\" ng \"}", 3.5)]
        [InlineData("{\"Abstraction.A\":1,\"Payload.Country\":\"GB\"}", 0.5)]
        [InlineData("{\"Abstraction.A\":1,\"Payload.Country\":\"Atlantis\"}", 7.5)]
        [InlineData("{\"Abstraction.A\":1,\"Payload.Country\":\"\"}", 11.5)]
        [InlineData("{\"Abstraction.A\":1,\"Payload.Country\":null}", 11.5)]
        [InlineData("{\"Abstraction.A\":1}", 11.5)]
        public async Task ACategoricalStringIsAcceptedAndScoredThroughTheEncoderAsync(string body, double sum)
        {
            var response = await RecallAsync(body);

            Assert.Equal(200, response.Status);
            Assert.Equal(Math.Round(Sigmoid(sum), 4), response.Json.GetProperty("score").GetDouble());
        }

        [Theory]
        [InlineData("{\"Payload.Country\":{\"a\":1}}")]
        [InlineData("{\"Payload.Country\":[\"GB\"]}")]
        [InlineData("{\"Abstraction.A\":\"not a number\",\"Payload.Country\":\"GB\"}")]
        public async Task WrongTypesAreStillRejectedWith400Async(string body)
        {
            var response = await RecallAsync(body);

            Assert.Equal(400, response.Status);
        }
    }
}