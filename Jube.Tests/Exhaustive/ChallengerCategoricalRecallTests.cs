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
using FluentAssertions;
using Jube.Analytics.Categorical;
using Jube.Data.Repository.Models;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Exhaustive.Models;
using Jube.Engine.Helpers;
using Jube.HttpAdaptationProtocol;
using Jube.Test.Analytics.Categorical;
using Jube.Test.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using Poco = Jube.Data.Poco;
using Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class ChallengerCategoricalRecallTests
    {
        private static CategoricalFieldEncoder Encoder()
        {
            return CategoricalTestData.Country();
        }

        private static List<GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto> Rows(
            CategoricalFieldEncoder encoder, int keepIndicator)
        {
            return
            [
                new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto
                {
                    Id = 1, Name = "Abstraction.A", ProcessingTypeId = 5, Mean = 4, StandardDeviation = 2,
                    NormalisationTypeId = 2
                },
                new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto
                {
                    Id = 10 + keepIndicator,
                    Name = encoder.Indicators[keepIndicator].Name,
                    ProcessingTypeId = 1,
                    NormalisationTypeId = 1,
                    Mean = 0.2,
                    StandardDeviation = 0.4,
                    CategoricalFieldId = 5,
                    CategoricalKind = (byte)encoder.Indicators[keepIndicator].Kind,
                    CategoricalLevel = encoder.Indicators[keepIndicator].Label,
                    CategoricalIndicatorIndex = keepIndicator,
                    CategoricalSourceName = encoder.SourceName,
                    CategoricalVocabularyJson = encoder.ToJson()
                }
            ];
        }

        private static (double[][] Inputs, int[] Labels, string[] Names) SyntheticData()
        {
            var random = new Random(97);
            var names = new[] { "Abstraction.A", "Payload.Country=US" };
            var inputs = new double[300][];
            var labels = new int[300];

            for (var i = 0; i < inputs.Length; i++)
            {
                var continuous = 4 + Normal(random) * 2;
                var indicator = random.NextDouble() < 0.4 ? 1d : 0d;
                inputs[i] = [continuous, indicator];
                var z = 0.6 * (continuous - 4) + 2.5 * indicator - 1;
                labels[i] = random.NextDouble() < 1 / (1 + Math.Exp(-z)) ? 1 : 0;
            }

            return (inputs, labels, names);
        }

        private static double Normal(Random random)
        {
            var u = 1 - random.NextDouble();
            var v = random.NextDouble();

            return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
        }

        private static ExhaustiveSearchInstance Sync(
            IEnumerable<GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto> rows, string kind,
            string modelJson)
        {
            return SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(
                new Poco.ExhaustiveSearchInstance
                {
                    Id = 4, Guid = Guid.NewGuid(), Name = "ChallengerCategorical"
                },
                new Poco.ExhaustiveSearchInstancePromotedTrialInstance
                {
                    KindId = Enum.Parse<AdvancedAnalyticsModelKind>(kind), Json = modelJson
                },
                rows, new JsonSerializationHelper().DeserializeTopologyNetworkJsonSerializerSettings);
        }

        private static ExhaustiveSearchInstance BuildChallengerModel(int keepIndicator = 0)
        {
            var (inputs, labels, names) = SyntheticData();

            var result = ChallengerModelTrainer.Train(AdvancedAnalyticsModelKind.LogisticRegressionPipeline,
                new Random(13), inputs,
                labels, names);

            return Sync(Rows(Encoder(), keepIndicator), result.Kind, result.ModelJson);
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
        public void Sync_AChallengerKindBuildsChallengerRecallNotATopologyNetwork()
        {
            var model = BuildChallengerModel();

            model.TopologyNetwork.Should().BeNull();
            model.ChallengerRecall.Should().NotBeNull();
            model.NetworkVariablesInOrder.Should().HaveCount(2);
            model.NetworkVariablesInOrder[1].IsCategorical.Should().BeTrue();
        }

        [Theory]
        [InlineData("GB")]
        [InlineData("US")]
        [InlineData("ng")]
        [InlineData(" Fr ")]
        [InlineData("XX")]
        [InlineData("never seen")]
        [InlineData("")]
        public void PipelineAndExternalRecallComputeIdenticalScoresForAChallengerModel(string country)
        {
            var model = BuildChallengerModel();
            var json = "{\"Abstraction.A\":6,\"Payload.Country\":" + JsonConvert(country) + "}";

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["A"] = 6;
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("Country", country);
            context.ExecuteExhaustiveAdaptation();

            var pipelineScore =
                context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["ChallengerCategorical"];

            var externalScore = ExhaustiveExtensions.Score(model, JObject.Parse(json));

            var rawInputs = ExhaustiveExtensions.BuildScoreInputs(model.NetworkVariablesInOrder, JObject.Parse(json),
                standardise: false);

            var directScore = model.ChallengerRecall.Predict(rawInputs)[0];

            pipelineScore.Should().BeApproximately(externalScore, 1e-9);
            pipelineScore.Should().BeApproximately(directScore, 1e-9);
        }

        [Fact]
        public void ChallengerCategoricalIndicatorIsNeverZScored()
        {
            var model = BuildChallengerModel();

            var us = ExhaustiveExtensions.BuildScoreInputs(model.NetworkVariablesInOrder,
                JObject.Parse("{\"Abstraction.A\":6,\"Payload.Country\":\"US\"}"), standardise: false);
            var gb = ExhaustiveExtensions.BuildScoreInputs(model.NetworkVariablesInOrder,
                JObject.Parse("{\"Abstraction.A\":6,\"Payload.Country\":\"GB\"}"), standardise: false);

            us[1].Should().Be(1);
            gb[1].Should().Be(0);
        }

        [Fact]
        public void ChallengerContinuousVariableIsNotStandardisedEither()
        {
            var model = BuildChallengerModel();

            var inputs = ExhaustiveExtensions.BuildScoreInputs(model.NetworkVariablesInOrder,
                JObject.Parse("{\"Abstraction.A\":9,\"Payload.Country\":\"GB\"}"), standardise: false);

            inputs[0].Should().Be(9);
        }

        [Fact]
        public void Pipeline_AbsentPayloadValueFallsBackToTheReferenceIndicatorAndTrainingMean()
        {
            var model = BuildChallengerModel();
            var context = NewContext(model);

            context.ExecuteExhaustiveAdaptation();

            var pipelineScore =
                context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["ChallengerCategorical"];

            var directScore = model.ChallengerRecall.Predict([4, 0])[0];

            pipelineScore.Should().BeApproximately(directScore, 1e-9);
        }

        [Fact]
        public void Pipeline_NumericPayloadValueBecomesItsInvariantStringForAChallengerModel()
        {
            var counter = CategoricalTestData.Count(("5411", 100), ("5812", 60), ("7995", 30), ("42", 25));
            var encoder = CategoricalFieldEncoder.Fit("Payload.Mcc", counter, CategoricalTestData.Options())!;

            var (inputs, labels, names) = SyntheticData();

            var result = ChallengerModelTrainer.Train(AdvancedAnalyticsModelKind.LogisticRegressionPipeline,
                new Random(13), inputs,
                labels, names);

            var rows = Rows(encoder, 0);
            var model = Sync(rows, result.Kind, result.ModelJson);

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["A"] = 5;
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("Mcc", 5812);

            context.ExecuteExhaustiveAdaptation();

            var pipelineScore =
                context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["ChallengerCategorical"];

            var directScore = model.ChallengerRecall.Predict([5, 1])[0];

            pipelineScore.Should().BeApproximately(directScore, 1e-9, "5812 is indicator index 0 of Mcc");
        }

        [Fact]
        public void HostileStringsNeverThrowOrProduceNonFiniteInputsForAChallengerModel()
        {
            var model = BuildChallengerModel();

            var hostile = new[]
            {
                new string('x', 200_000), "\uD800", "'; DROP TABLE Archive;--", "<script>alert(1)</script>",
                "\0", "‮evil", string.Concat(Enumerable.Repeat("😀", 5000))
            };

            foreach (var value in hostile)
            {
                var json = "{\"Abstraction.A\":6,\"Payload.Country\":" + JsonConvert(value) + "}";

                var inputs = ExhaustiveExtensions.BuildScoreInputs(model.NetworkVariablesInOrder,
                    JObject.Parse(json), standardise: false);

                inputs.Should().OnlyContain(v => double.IsFinite(v));
                inputs[1].Should().Be(0);

                var context = NewContext(model);
                context.EntityAnalysisModelInstanceEntryPayload.Abstraction["A"] = 6;
                context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("Country", value);
                context.ExecuteExhaustiveAdaptation();

                context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["ChallengerCategorical"]
                    .Should().Match(v => double.IsFinite(v));
            }
        }

        private static string JsonConvert(string value)
        {
            return Newtonsoft.Json.JsonConvert.ToString(value);
        }
    }
}