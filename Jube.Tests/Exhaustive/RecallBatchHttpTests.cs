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
using System.Text.Json;
using System.Threading.Tasks;
using Accord.Neuro;
using Jube.Engine.Exhaustive.Models;
using Jube.Test.Infrastructure.ModelScaffolding;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RecallBatchHttpTests(ModelScaffoldFixture fixture) : IClassFixture<ModelScaffoldFixture>
    {
        private const string Boundary = "----RecallBatchHttpTestsBoundary";

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

        private static ExhaustiveSearchInstancePromotedTrialInstanceVariable Variable(string name)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstanceVariable { Name = name };
        }

        private static byte[] MultipartCsv(string fileName, string csv)
        {
            var body = "--" + Boundary + "\r\n" +
                       "Content-Disposition: form-data; name=\"file\"; filename=\"" + fileName + "\"\r\n" +
                       "Content-Type: text/csv\r\n\r\n" +
                       csv + "\r\n" +
                       "--" + Boundary + "--\r\n";

            return Encoding.UTF8.GetBytes(body);
        }

        private async Task<InvokeResponse> BatchRecallAsync(ActivationNetwork network, string csv,
            params ExhaustiveSearchInstancePromotedTrialInstanceVariable[] variables)
        {
            var engineModel = fixture.Engine.FindActiveModel(fixture.Shared.ModelGuid) ??
                              throw new InvalidOperationException("The model is not synchronised.");

            var instance = new ExhaustiveSearchInstance
            {
                Name = "RecallBatchHttpExhaustive",
                Id = -1,
                Guid = Guid.NewGuid(),
                TopologyNetwork = network
            };

            instance.NetworkVariablesInOrder.AddRange(variables);

            engineModel.Snapshot.ExhaustiveModels.Add(instance);

            try
            {
                return await fixture.Api.SendAsync("POST",
                    "/api/Invoke/ExhaustiveSearchInstance/" + instance.Guid + "/Batch", fixture.Shared.UserName,
                    MultipartCsv("rows.csv", csv), "multipart/form-data; boundary=" + Boundary);
            }
            finally
            {
                engineModel.Snapshot.ExhaustiveModels.Remove(instance);
            }
        }

        [Fact]
        public async Task EveryRowIsScoredThroughTheSameRecallPathAsASingleRowAsync()
        {
            var response = await BatchRecallAsync(Network(0, 1), "X\n0.1\n2.5\n", Variable("X"));

            Assert.Equal(200, response.Status);
            var rows = response.Json.GetProperty("rows").EnumerateArray().ToArray();
            Assert.Equal(2, rows.Length);
            Assert.Equal(Math.Round(Sigmoid(0.1), 4), rows[0].GetProperty("score").GetDouble());
            Assert.Equal(Math.Round(Sigmoid(2.5), 4), rows[1].GetProperty("score").GetDouble());
        }

        [Fact]
        public async Task AnUnparseableValueIsReportedAsARowErrorNotABatchFailureAsync()
        {
            var response = await BatchRecallAsync(Network(0, 1), "X\n0.1\nabc\n", Variable("X"));

            Assert.Equal(200, response.Status);
            var rows = response.Json.GetProperty("rows").EnumerateArray().ToArray();
            Assert.Equal(2, rows.Length);
            Assert.Equal(Math.Round(Sigmoid(0.1), 4), rows[0].GetProperty("score").GetDouble());

            Assert.False(rows[1].TryGetProperty("score", out var secondScore) &&
                         secondScore.ValueKind != JsonValueKind.Null);

            Assert.True(rows[1].GetProperty("error").GetString()?.Length > 0);
        }

        [Fact]
        public async Task AColumnWithNoMatchingTrainedVariableIsReportedAsUnmappedAsync()
        {
            var response = await BatchRecallAsync(Network(0, 1), "X,Extra\n0.1,9\n", Variable("X"));

            Assert.Equal(200, response.Status);

            var mapped = response.Json.GetProperty("mappedVariables").EnumerateArray()
                .Select(e => e.GetString()).ToArray();
            Assert.Contains("X", mapped);
        }
    }
}