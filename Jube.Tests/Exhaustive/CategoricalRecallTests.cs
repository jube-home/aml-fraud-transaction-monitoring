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
using System.Diagnostics;
using System.Linq;
using Accord.Neuro;
using FluentAssertions;
using Jube.Analytics.Categorical;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Exhaustive.Models;
using Jube.Engine.Helpers;
using Jube.HttpAdaptationProtocol;
using Jube.Service.Invoke;
using Jube.Test.Analytics.Categorical;
using Jube.Test.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using Poco = Jube.Data.Poco;
using Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class CategoricalRecallTests
    {
        private static readonly JsonSerializationHelper helper = new();
        private static readonly double[] weights = [0.5, 2, 3, 5, 7, 11];

        private static CategoricalFieldEncoder Encoder()
        {
            return CategoricalTestData.Country();
        }

        private static ActivationNetwork Network(params double[] networkWeights)
        {
            var network = new ActivationNetwork(new SigmoidFunction(), networkWeights.Length, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];

            for (var i = 0; i < networkWeights.Length; i++)
            {
                neuron.Weights[i] = networkWeights[i];
            }

            neuron.Threshold = 0;

            return network;
        }

        private static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-2 * x));
        }

        private static List<GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto> Rows(
            CategoricalFieldEncoder encoder, params int[] keepIndicators)
        {
            var rows = new List<GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto>
            {
                new()
                {
                    Id = 1, Name = "Abstraction.A", ProcessingTypeId = 5, Mean = 0, StandardDeviation = 1,
                    NormalisationTypeId = 0
                }
            };

            var json = encoder.ToJson();

            foreach (var i in keepIndicators.Length == 0
                         ? [.. Enumerable.Range(0, encoder.Indicators.Count)]
                         : keepIndicators)
            {
                rows.Add(new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto
                {
                    Id = 10 + i,
                    Name = encoder.Indicators[i].Name,
                    ProcessingTypeId = 1,
                    NormalisationTypeId = 1,
                    Mean = 0.2,
                    StandardDeviation = 0.4,
                    CategoricalFieldId = 5,
                    CategoricalKind = (byte)encoder.Indicators[i].Kind,
                    CategoricalLevel = encoder.Indicators[i].Label,
                    CategoricalIndicatorIndex = i,
                    CategoricalSourceName = encoder.SourceName,
                    CategoricalVocabularyJson = json
                });
            }

            return rows;
        }

        private static ExhaustiveSearchInstance Sync(
            IEnumerable<GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto> rows,
            ActivationNetwork network, Action<Exception>? onError = null)
        {
            return SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(
                new Poco.ExhaustiveSearchInstance
                {
                    Id = 3, Guid = Guid.NewGuid(), Name = "Categorical"
                },
                new Poco.ExhaustiveSearchInstancePromotedTrialInstance
                {
                    Json = JsonConvert.SerializeObject(network, helper.TopologyNetworkJsonSerializerSettings)
                },
                rows, helper.DeserializeTopologyNetworkJsonSerializerSettings, onError);
        }

        private static double[] Inputs(ExhaustiveSearchInstance model, string json)
        {
            return ExhaustiveExtensions.BuildScoreInputs(model.NetworkVariablesInOrder, JObject.Parse(json));
        }

        private static Context NewContext(ExhaustiveSearchInstance model)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Snapshot.ExhaustiveModels.Add(model);

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Dictionary = new PooledDictionary<string, double>(),
                    TtlCounter = new PooledDictionary<string, double>(),
                    Sanction = new PooledDictionary<string, double>(),
                    Abstraction = new PooledDictionary<string, double>(),
                    AbstractionCalculation = new PooledDictionary<string, double>(),
                    ExhaustiveAdaptation = new PooledDictionary<string, double>(),
                    ExhaustiveAdaptationDetail = new PooledDictionary<string, Adaptation>(),
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        [Fact]
        public void Sync_BuildsTheEncoderFromTheStoredVocabularyAndSharesItAcrossIndicators()
        {
            var model = Sync(Rows(Encoder()), Network(weights));

            model.NetworkVariablesInOrder.Should().HaveCount(6);
            model.NetworkVariablesInOrder[0].IsCategorical.Should().BeFalse();
            var categorical = model.NetworkVariablesInOrder.Skip(1).ToList();
            categorical.Should().OnlyContain(v => v.IsCategorical);
            categorical.Select(s => s.CategoricalIndicatorIndex).Should().Equal(0, 1, 2, 3, 4);
            categorical.Select(s => s.CategoricalEncoder).Distinct().Should().HaveCount(1);
            categorical[0].CategoricalSourceName.Should().Be("Payload.Country");
            categorical[0].CategoricalEncoder!.ToJson().Should().Be(Encoder().ToJson());
        }

        [Fact]
        public void Sync_AnInvalidVocabularyReportsALoadError()
        {
            var rows = Rows(Encoder());
            rows[1].CategoricalVocabularyJson = "{\"version\":1,\"sourceName\":\"x\",\"levels\":[]}";
            Exception? captured = null;

            Sync(rows, Network(weights), e => captured = e);

            captured.Should().NotBeNull();
        }

        [Fact]
        public void Sync_ModelsWithoutCategoricalMetadataAreUnchanged()
        {
            var model = Sync(
            [
                new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto
                {
                    Name = "Abstraction.A", ProcessingTypeId = 5, Mean = 4, StandardDeviation = 2,
                    NormalisationTypeId = 2
                }
            ], Network(1));

            var variable = model.NetworkVariablesInOrder.Single();
            variable.IsCategorical.Should().BeFalse();
            variable.CategoricalEncoder.Should().BeNull();
            variable.CategoricalSourceName.Should().BeNull();
            Inputs(model, "{\"Abstraction.A\": 8}").Should().Equal(2);
        }

        [Theory]
        [InlineData("{\"Payload.Country\":\"GB\"}", new[] { 3d, 0, 0, 0, 0, 0 })]
        [InlineData("{\"Payload.Country\":\"US\"}", new[] { 3d, 1, 0, 0, 0, 0 })]
        [InlineData("{\"Payload.Country\":\"  nG \"}", new[] { 3d, 0, 1, 0, 0, 0 })]
        [InlineData("{\"Payload.Country\":\"\\uFF26\\uFF32\"}", new[] { 3d, 0, 0, 1, 0, 0 })]
        [InlineData("{\"Payload.Country\":\"XX\"}", new[] { 3d, 0, 0, 0, 1, 0 })]
        [InlineData("{\"Payload.Country\":\"Atlantis\"}", new[] { 3d, 0, 0, 0, 1, 0 })]
        [InlineData("{\"Payload.Country\":\"\"}", new[] { 3d, 0, 0, 0, 0, 1 })]
        [InlineData("{\"Payload.Country\":null}", new[] { 3d, 0, 0, 0, 0, 1 })]
        [InlineData("{}", new[] { 3d, 0, 0, 0, 0, 1 })]
        [InlineData("{\"Payload.Country\":{\"a\":1}}", new[] { 3d, 0, 0, 0, 0, 1 })]
        [InlineData("{\"Payload.Country\":12345}", new[] { 3d, 0, 0, 0, 1, 0 })]
        [InlineData("{\"Payload.Country\":true}", new[] { 3d, 0, 0, 0, 1, 0 })]
        public void BuildScoreInputs_ComputesTheIndicatorsFromTheIncomingString(string json, double[] expected)
        {
            var model = Sync(Rows(Encoder()), Network(weights));

            Inputs(model, json.Replace("{", "{\"Abstraction.A\":3,").Replace(",}", "}")).Should().Equal(expected);
        }

        [Fact]
        public void Score_AppliesTheNetworkToTheIndicators()
        {
            var model = Sync(Rows(Encoder()), Network(weights));

            var score = ExhaustiveExtensions.Score(model.TopologyNetwork, model.NetworkVariablesInOrder,
                JObject.Parse("{\"Abstraction.A\":1,\"Payload.Country\":\"ng\"}"));

            score.Should().BeApproximately(Sigmoid(0.5 * 1 + 3), 1e-12);
        }

        [Fact]
        public void Score_ANormalisedIndicatorIsStandardisedLikeTraining()
        {
            var rows = Rows(Encoder());
            rows[2].NormalisationTypeId = 2;
            var model = Sync(rows, Network(weights));

            var inputs = Inputs(model, "{\"Abstraction.A\":0,\"Payload.Country\":\"US\"}");

            inputs[1].Should().Be(1);
            inputs[2].Should().BeApproximately((0 - 0.2) / 0.4, 1e-12);
        }

        [Fact]
        public void EliminatedIndicatorsFoldTheLevelIntoTheReference()
        {
            var encoder = Encoder();
            var model = Sync(Rows(encoder, 0, 2, 3), Network(0.5, 2, 5, 7));

            Inputs(model, "{\"Abstraction.A\":0,\"Payload.Country\":\"NG\"}").Should().Equal(0, 0, 0, 0);
            Inputs(model, "{\"Abstraction.A\":0,\"Payload.Country\":\"US\"}").Should().Equal(0, 1, 0, 0);
            Inputs(model, "{\"Abstraction.A\":0,\"Payload.Country\":\"XX\"}").Should().Equal(0, 0, 0, 1);
        }

        [Theory]
        [InlineData("GB")]
        [InlineData("US")]
        [InlineData("ng")]
        [InlineData(" Fr ")]
        [InlineData("XX")]
        [InlineData("never seen")]
        [InlineData("")]
        [InlineData("​us")]
        public void PipelineAndHttpRecallComputeIdenticalInputsForTheSameString(string country)
        {
            var model = Sync(Rows(Encoder()), Network(weights));
            var http = Inputs(model, "{\"Abstraction.A\":2,\"Payload.Country\":" + JsonConvert.ToString(country) + "}");

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["A"] = 2;
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("Country", country);
            context.ExecuteExhaustiveAdaptation();

            var expected = ExhaustiveExtensions.Score(model.TopologyNetwork, model.NetworkVariablesInOrder,
                JObject.Parse("{\"Abstraction.A\":2,\"Payload.Country\":" + JsonConvert.ToString(country) + "}"));

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Categorical"].Should()
                .BeApproximately(expected, 1e-12);

            model.TopologyNetwork.Compute(http)[0].Should().BeApproximately(expected, 1e-12);
        }

        [Fact]
        public void Pipeline_AbsentPayloadValueIsMissing()
        {
            var model = Sync(Rows(Encoder()), Network(weights));
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["A"] = 1;

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Categorical"].Should()
                .BeApproximately(Sigmoid(0.5 + 11), 1e-12);
        }

        [Fact]
        public void Pipeline_NumericPayloadValueBecomesItsInvariantString()
        {
            var counter = CategoricalTestData.Count(("5411", 100), ("5812", 60), ("7995", 30), ("42", 25));
            var encoder = CategoricalFieldEncoder.Fit("Payload.Mcc", counter, CategoricalTestData.Options())!;
            var model = Sync(Rows(encoder), Network(0.5, 2, 3, 5));
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["A"] = 0;
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("Mcc", 7995);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Categorical"].Should()
                .BeApproximately(Sigmoid(3), 1e-12, "7995 is indicator index 1 of Mcc");
        }

        [Fact]
        public void TypeGate_AcceptsStringsForACategoricalSourceAndStillRejectsWrongTypesElsewhere()
        {
            var model = Sync(Rows(Encoder()), Network(weights));

            foreach (var accepted in new[]
                     {
                         "{\"Payload.Country\":\"GB\"}", "{\"Payload.Country\":\"not a number\"}",
                         "{\"Payload.Country\":\"\"}", "{\"Payload.Country\":null}", "{\"Payload.Country\":12}",
                         "{\"Payload.Country\":1.5}", "{\"Payload.Country\":true}", "{}",
                         "{\"Abstraction.A\":\"7\",\"Payload.Country\":\"GB\"}"
                     })
            {
                InvokeService.RecallBodyHasValidTypes(model, JObject.Parse(accepted)).Should().BeTrue(accepted);
            }

            foreach (var rejected in new[]
                     {
                         "{\"Payload.Country\":{\"a\":1}}", "{\"Payload.Country\":[\"GB\"]}",
                         "{\"Abstraction.A\":\"abc\",\"Payload.Country\":\"GB\"}",
                         "{\"Abstraction.A\":[1],\"Payload.Country\":\"GB\"}",
                         "{\"Abstraction.A\":{\"a\":1}}"
                     })
            {
                InvokeService.RecallBodyHasValidTypes(model, JObject.Parse(rejected)).Should().BeFalse(rejected);
            }
        }

        [Fact]
        public void TypeGate_TheIndicatorNameIsNotARequestKey()
        {
            var model = Sync(Rows(Encoder()), Network(weights));

            InvokeService.RecallBodyHasValidTypes(model,
                JObject.Parse("{\"Payload.Country=US\":{\"a\":1}}")).Should().BeTrue();

            Inputs(model, "{\"Abstraction.A\":0,\"Payload.Country=US\":1}").Skip(1).Should()
                .OnlyContain(v => v == 0 || v == 1);
        }

        [Fact]
        public void TypeGate_ModelsWithoutCategoricalMetadataRejectStringsAsBefore()
        {
            var model = Sync(
            [
                new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto
                {
                    Name = "Payload.Country", ProcessingTypeId = 1, NormalisationTypeId = 0
                }
            ], Network(1));

            InvokeService.RecallBodyHasValidTypes(model, JObject.Parse("{\"Payload.Country\":\"GB\"}")).Should()
                .BeFalse();
            InvokeService.RecallBodyHasValidTypes(model, JObject.Parse("{\"Payload.Country\":\"3.5\"}")).Should()
                .BeTrue();
        }

        [Fact]
        public void HostileStringsNeverThrowOrProduceNonFiniteInputs()
        {
            var model = Sync(Rows(Encoder()), Network(weights));

            var hostile = new[]
            {
                new string('x', 200_000), "\uD800", "'; DROP TABLE Archive;--", "<script>alert(1)</script>",
                "\0", "‮evil", string.Concat(Enumerable.Repeat("😀", 5000))
            };

            foreach (var value in hostile)
            {
                var inputs = Inputs(model,
                    "{\"Abstraction.A\":0,\"Payload.Country\":" + JsonConvert.ToString(value) + "}");

                inputs.Should().OnlyContain(v => double.IsFinite(v));
                inputs.Skip(1).Sum().Should().BeLessThanOrEqualTo(1);
            }
        }
    }
}