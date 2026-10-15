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
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using Jube.Engine.AdvancedAnalytics.Models;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsVariableTransformationRecallTests
    {
        private static Context NewContext(params Jube.Engine.AdvancedAnalytics.Models.AdvancedAnalyticsModel[] models)
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
        public void NoneTransformationLeavesThePayloadValueUnchanged()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.None, null);
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 7.0);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(7.0);
        }

        [Fact]
        public void NoneTransformationLeavesAMissingValueAsNaN()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.None, null);
            var context = NewContext(model);

            context.ExecuteAdvancedAnalyticsAdaptation();

            double.IsNaN(Recalled(context, "M")).Should().BeTrue();
        }

        [Fact]
        public void LogTransformationIsAppliedBeforeReachingRecall()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.Log, "{\"offset\":0}");
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", Math.E);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().BeApproximately(1.0, 1e-9);
        }

        [Fact]
        public void ZScoreStandardTransformationIsAppliedBeforeReachingRecall()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.ZScoreStandard,
                "{\"center\":10,\"scale\":2}");

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 14.0);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(2.0);
        }

        [Fact]
        public void WinsoriseClipsAboveTheUpperBoundBeforeReachingRecall()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.Winsorise,
                "{\"lowerBound\":0,\"upperBound\":100}");

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 999.0);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(100.0);
        }

        [Fact]
        public void DiscretiseCustomEdgesReturnsTheBinIndexBeforeReachingRecall()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.DiscretiseCustomEdges,
                "{\"edges\":[10,20,30]}");

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 25.0);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(2.0);
        }

        [Fact]
        public void CyclicalSinAndCosAreComputedFromTheSamePayloadField()
        {
            var sinModel = EngineModelFactory.IdentityModel("Sin", VariableTransformationKind.CyclicalSin,
                "{\"period\":24,\"sourceName\":\"Hour\"}");
            var cosModel = EngineModelFactory.IdentityModel("Cos", VariableTransformationKind.CyclicalCos,
                "{\"period\":24,\"sourceName\":\"Hour\"}");

            var context = NewContext(sinModel, cosModel);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Hour", 6.0);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "Sin").Should().BeApproximately(1.0, 1e-9);
            Recalled(context, "Cos").Should().BeApproximately(0.0, 1e-9);
        }

        [Fact]
        public void OneHotIndicatorIsOneOnlyWhenTheTextMatchesTheStoredCategory()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.OneHotIndicator,
                "{\"category\":\"US\",\"sourceName\":\"Country\"}");

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "US");

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(1.0);
        }

        [Fact]
        public void OneHotIndicatorIsZeroWhenTheTextDoesNotMatch()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.OneHotIndicator,
                "{\"category\":\"US\",\"sourceName\":\"Country\"}");

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "CA");

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(0.0);
        }

        [Fact]
        public void WeightOfEvidenceLooksUpTheStoredCategoryValue()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.WeightOfEvidence,
                "{\"categories\":[\"CA\",\"US\"],\"values\":[-0.5,0.8],\"defaultValue\":0}");

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", "US");

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(0.8);
        }

        [Fact]
        public void WeightOfEvidenceFallsBackToTheDefaultValueForAnUnseenCategory()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.WeightOfEvidence,
                "{\"categories\":[\"CA\",\"US\"],\"values\":[-0.5,0.8],\"defaultValue\":0.1}");

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", "FR");

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(0.1);
        }

        [Fact]
        public void ACategoricalTransformationWithAMissingPayloadFieldFallsBackToTheDefaultValue()
        {
            var model = EngineModelFactory.IdentityModel("M", VariableTransformationKind.WeightOfEvidence,
                "{\"categories\":[\"CA\",\"US\"],\"values\":[-0.5,0.8],\"defaultValue\":0.25}");

            var context = NewContext(model);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Recalled(context, "M").Should().Be(0.25);
        }
    }
}