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
using System.Diagnostics;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    [Trait("Category", "Unit")]
    [Trait("Covers", "EdgeCases")]
    public sealed class AdvancedAnalyticsVariableTransformationRecallEdgeCaseTests
    {
        private static Context NewContext(params AdvancedAnalyticsModel[] models)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.PublishSnapshot(new ModelSnapshot { AdvancedAnalyticsModels = models });

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
                    AdvancedAnalyticsAdaptation = new PooledDictionary<string, double>(),
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        private static double Recalled(Context context, string name)
        {
            return context.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation[name];
        }

        [Fact]
        public void AModelMixingUntransformedAndTransformedVariablesResolvesEverySlotIndependently()
        {
            double[]? captured = null;

            var model = EngineModelFactory.IdentityModel("Mixed",
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Raw", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.None
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Amount", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.Log,
                    TransformationParametersJson = "{\"offset\":0}"
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Country", ProcessingTypeId = 1,
                    TransformationId = VariableTransformationKind.WeightOfEvidence,
                    TransformationParametersJson =
                        "{\"categories\":[\"CA\",\"US\"],\"values\":[-0.5,0.8],\"defaultValue\":0}"
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Limit", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.None
                });

            model.Recall = row =>
            {
                captured = row;

                return row;
            };

            var context = NewContext(model);
            var payload = context.EntityAnalysisModelInstanceEntryPayload.Payload;
            payload.Add("Raw", 3.0);
            payload.Add("Amount", Math.E);
            payload.Add("Country", "US");
            payload.Add("Limit", 250.0);

            context.ExecuteAdvancedAnalyticsAdaptation();

            captured.Should().NotBeNull();
            captured![0].Should().Be(3.0);
            captured[1].Should().BeApproximately(1.0, 1e-9);
            captured[2].Should().Be(0.8);
            captured[3].Should().Be(250.0);
        }

        [Fact]
        public void AOneHotExpandedVariableAlongsideOrdinaryVariablesKeepsEverySlotAtTheCorrectIndex()
        {
            double[]? captured = null;

            var model = EngineModelFactory.IdentityModel("OneHot",
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Amount", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.None
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Country=CA", ProcessingTypeId = 1,
                    TransformationId = VariableTransformationKind.OneHotIndicator,
                    TransformationParametersJson = "{\"category\":\"CA\",\"sourceName\":\"Country\"}"
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Country=US", ProcessingTypeId = 1,
                    TransformationId = VariableTransformationKind.OneHotIndicator,
                    TransformationParametersJson = "{\"category\":\"US\",\"sourceName\":\"Country\"}"
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Country=FR", ProcessingTypeId = 1,
                    TransformationId = VariableTransformationKind.OneHotIndicator,
                    TransformationParametersJson = "{\"category\":\"FR\",\"sourceName\":\"Country\"}"
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Limit", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.None
                });

            model.Recall = row =>
            {
                captured = row;

                return row;
            };

            var context = NewContext(model);
            var payload = context.EntityAnalysisModelInstanceEntryPayload.Payload;
            payload.Add("Amount", 42.0);
            payload.Add("Country", "US");
            payload.Add("Limit", 900.0);

            context.ExecuteAdvancedAnalyticsAdaptation();

            captured.Should().NotBeNull();
            captured!.Length.Should().Be(5);
            captured[0].Should().Be(42.0);
            captured[1].Should().Be(0.0);
            captured[2].Should().Be(1.0);
            captured[3].Should().Be(0.0);
            captured[4].Should().Be(900.0);
        }

        [Fact]
        public void AMalformedTransformationParametersJsonOnOneVariableDoesNotStopTheOtherVariablesOfTheSameModel()
        {
            double[]? captured = null;

            var model = EngineModelFactory.IdentityModel("Resilient",
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Good", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.None
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "Broken", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.ZScoreStandard,
                    TransformationParametersJson = "{this is not json"
                },
                new AdvancedAnalyticsModelVariable
                {
                    Name = "AlsoGood", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.Log,
                    TransformationParametersJson = "{\"offset\":0}"
                });

            model.Recall = row =>
            {
                captured = row;

                return row;
            };

            var context = NewContext(model);
            var payload = context.EntityAnalysisModelInstanceEntryPayload.Payload;
            payload.Add("Good", 7.0);
            payload.Add("Broken", 99.0);
            payload.Add("AlsoGood", Math.E);

            var act = () => context.ExecuteAdvancedAnalyticsAdaptation();

            act.Should().NotThrow();
            captured.Should().NotBeNull();
            captured![0].Should().Be(7.0);
            captured[1].Should().Be(99.0);
            captured[2].Should().BeApproximately(1.0, 1e-9);

            context.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation.ContainsKey("Resilient")
                .Should().BeTrue();
        }

        [Fact]
        public void ATransformedVariableWithNoPayloadFieldAtAllReachesRecallAsNaNJustLikeUntransformedToday()
        {
            var none = EngineModelFactory.IdentityModel("None", new AdvancedAnalyticsModelVariable
            {
                Name = "Field", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.None
            });

            var log = EngineModelFactory.IdentityModel("Log", new AdvancedAnalyticsModelVariable
            {
                Name = "Field", ProcessingTypeId = 1, TransformationId = VariableTransformationKind.Log,
                TransformationParametersJson = "{\"offset\":0}"
            });

            var context = NewContext(none, log);

            context.ExecuteAdvancedAnalyticsAdaptation();

            double.IsNaN(Recalled(context, "None")).Should().BeTrue();
            double.IsNaN(Recalled(context, "Log")).Should().BeTrue();
        }

        [Theory]
        [InlineData(VariableTransformationKind.LabelOrdinal)]
        [InlineData(VariableTransformationKind.OneHotIndicator)]
        [InlineData(VariableTransformationKind.TargetMean)]
        [InlineData(VariableTransformationKind.WeightOfEvidence)]
        [InlineData(VariableTransformationKind.Frequency)]
        public void ACategoricalTransformationWithAnUnseenLiveCategoryNeverThrowsAndStillProducesAScore(
            VariableTransformationKind kind)
        {
            var parametersJson = kind switch
            {
                VariableTransformationKind.LabelOrdinal =>
                    "{\"categories\":[\"CA\",\"US\"],\"unknownCode\":-1}",
                VariableTransformationKind.OneHotIndicator => "{\"category\":\"US\"}",
                _ => "{\"categories\":[\"CA\",\"US\"],\"values\":[-0.5,0.8],\"defaultValue\":0}"
            };

            var model = EngineModelFactory.IdentityModel("M", new AdvancedAnalyticsModelVariable
            {
                Name = "Country", ProcessingTypeId = 1, TransformationId = kind,
                TransformationParametersJson = parametersJson
            });

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "NeverSeenAtTrainingTime");

            var act = () => context.ExecuteAdvancedAnalyticsAdaptation();

            act.Should().NotThrow();

            context.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation.ContainsKey("M")
                .Should().BeTrue();
        }
    }
}