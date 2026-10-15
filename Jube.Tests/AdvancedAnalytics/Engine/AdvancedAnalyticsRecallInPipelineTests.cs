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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
    public sealed class AdvancedAnalyticsRecallInPipelineTests
    {
        private static readonly TrainedFixture fixture = EngineModelFactory.Trained(11, ["A", "B", "C"]);
        private static readonly TrainedFixture single = EngineModelFactory.Trained(12, ["Field"]);

        private static Context NewContext(Jube.DynamicEnvironment.DynamicEnvironment? environment,
            params AdvancedAnalyticsModel[] models)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.PublishSnapshot(new ModelSnapshot { AdvancedAnalyticsModels = models });

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                Environment = environment!,
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

        private static Context NewContext(params AdvancedAnalyticsModel[] models)
        {
            return NewContext(null, models);
        }

        private static void Put(Context context, int processingTypeId, string key, double value)
        {
            var payload = context.EntityAnalysisModelInstanceEntryPayload;

            switch (processingTypeId)
            {
                case 1:
                    payload.Payload.Add(key, value);
                    break;
                case 2:
                    payload.Dictionary[key] = value;
                    break;
                case 3:
                    payload.TtlCounter[key] = value;
                    break;
                case 4:
                    payload.Sanction[key] = value;
                    break;
                case 5:
                    payload.Abstraction[key] = value;
                    break;
                case 6:
                    payload.AbstractionCalculation[key] = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(processingTypeId));
            }
        }

        private static double Expected(TrainedFixture trained, params double[] row)
        {
            return trained.Recall.Predict(row)[0];
        }

        private static Dictionary<string, double> Scores(Context context)
        {
            return context.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation
                .ToDictionary(k => k.Key, v => v.Value);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(99)]
        [InlineData(0)]
        public void AbsentValueIsPassedAsNaNSoTheRecallImputesAsTrainedForEveryProcessingType(int processingTypeId)
        {
            var context = NewContext(EngineModelFactory.Model("M", single, processingTypeId));

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context)["M"].Should().Be(Expected(single, double.NaN));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void APresentZeroIsUsedAsZeroAndNotTreatedAsAbsent(int processingTypeId)
        {
            var context = NewContext(EngineModelFactory.Model("M", single, processingTypeId));
            Put(context, processingTypeId, "Field", 0);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Expected(single, 0).Should().NotBe(Expected(single, double.NaN));
            Scores(context)["M"].Should().Be(Expected(single, 0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void APresentValueIsPassedRawFromItsOwnProcessingTypeSource(int processingTypeId)
        {
            var context = NewContext(EngineModelFactory.Model("M", single, processingTypeId));
            Put(context, processingTypeId, "Field", 1.75);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context)["M"].Should().Be(Expected(single, 1.75));
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        [InlineData(3, 4)]
        [InlineData(4, 5)]
        [InlineData(5, 6)]
        [InlineData(6, 1)]
        public void AValueHeldInADifferentSourceThanTheProcessingTypeIsNotSeen(int processingTypeId, int otherSource)
        {
            var context = NewContext(EngineModelFactory.Model("M", single, processingTypeId));
            Put(context, otherSource, "Field", 9);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context)["M"].Should().Be(Expected(single, double.NaN));
        }

        [Fact]
        public void ThePayloadValueIsReadAsADoubleWhenTheStoredValueIsAnInteger()
        {
            var context = NewContext(EngineModelFactory.Model("M", single, 1));
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 3);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context)["M"].Should().Be(Expected(single, 3));
        }

        [Fact]
        public void InputVectorFollowsTheStoredVariableOrderAndNotTheNameOrder()
        {
            var context = NewContext(EngineModelFactory.Model("M", fixture));
            Put(context, 5, "A", 0.3);
            Put(context, 5, "B", -0.4);
            Put(context, 5, "C", 1.2);

            context.ExecuteAdvancedAnalyticsAdaptation();

            var forward = Expected(fixture, 0.3, -0.4, 1.2);
            Scores(context)["M"].Should().Be(forward);
            Expected(fixture, 1.2, -0.4, 0.3).Should().NotBe(forward);
        }

        [Fact]
        public void EachVariableIsResolvedFromItsOwnProcessingTypeInOneVector()
        {
            var context = NewContext(EngineModelFactory.Model("M", fixture, [1, 3, 6]));
            Put(context, 1, "A", 0.5);
            Put(context, 3, "B", 0.25);
            Put(context, 6, "C", -0.75);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context)["M"].Should().Be(Expected(fixture, 0.5, 0.25, -0.75));
        }

        [Fact]
        public void OnlySomeVariablesAbsentPassesNaNAtExactlyThosePositions()
        {
            var context = NewContext(EngineModelFactory.Model("M", fixture));
            Put(context, 5, "A", 0.5);
            Put(context, 5, "C", -0.75);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context)["M"].Should().Be(Expected(fixture, 0.5, double.NaN, -0.75));
        }

        [Theory]
        [InlineData("Abstraction.Field", "Field")]
        [InlineData("Payload.Field", "Field")]
        [InlineData("A.Field.Extra", "Field")]
        public void OnlyTheSecondDotSegmentIsUsedAsTheLookupKey(string storedName, string key)
        {
            var trained = EngineModelFactory.Trained(13, [storedName]);
            var context = NewContext(EngineModelFactory.Model("M", trained));
            Put(context, 5, key, 0.7);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context)["M"].Should().Be(Expected(trained, 0.7));
        }

        [Fact]
        public void LookupIsCaseSensitive()
        {
            var context = NewContext(EngineModelFactory.Model("M", single));
            Put(context, 5, "field", 0.9);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context)["M"].Should().Be(Expected(single, double.NaN));
        }

        [Fact]
        public void ARecallThatThrowsOmitsOnlyThatModelAndDoesNotRaise()
        {
            var broken = EngineModelFactory.Model("Broken", single, 5, 1);
            broken.Recall = _ => throw new InvalidOperationException("boom");
            var good = EngineModelFactory.Model("Good", single, 5, 2);
            var context = NewContext(broken, good);
            Put(context, 5, "Field", 0.4);

            var act = () => context.ExecuteAdvancedAnalyticsAdaptation();

            act.Should().NotThrow();
            var scores = Scores(context);
            scores.Should().NotContainKey("Broken");
            scores["Good"].Should().Be(Expected(single, 0.4));
        }

        [Fact]
        public void AModelTrainedToRejectMissingValuesOmitsTheEntryWhenAValueIsAbsent()
        {
            var model = EngineModelFactory.Model("Strict", single);
            model.Recall = row => double.IsNaN(row[0]) ? throw new ArgumentException("rejects missing") : [0.5];
            var context = NewContext(model);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context).Should().BeEmpty();
        }

        [Fact]
        public void ANullRecallOmitsTheEntryButStillRecordsTiming()
        {
            var model = EngineModelFactory.Model("NoRecall", single);
            model.Recall = null!;
            var context = NewContext(model);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context).Should().NotContainKey("NoRecall");

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.AdvancedAnalyticsAdaptation
                .Items.Should().ContainKey("NoRecall");
        }

        [Fact]
        public void ModelsAreKeyedByNameAndEachScoredIndependently()
        {
            var other = EngineModelFactory.Trained(14, ["Field"]);

            var context = NewContext(EngineModelFactory.Model("One", single, 5, 1),
                EngineModelFactory.Model("Two", other, 5, 2));

            Put(context, 5, "Field", 0.5);

            context.ExecuteAdvancedAnalyticsAdaptation();

            var scores = Scores(context);
            scores["One"].Should().Be(Expected(single, 0.5));
            scores["Two"].Should().Be(Expected(other, 0.5));
            scores["One"].Should().NotBe(scores["Two"]);
        }

        [Fact]
        public void TimingIsRecordedPerModelWithDurationAndAllocationAndAStageTotal()
        {
            var context = NewContext(EngineModelFactory.Model("One", single, 5, 1),
                EngineModelFactory.Model("Two", single, 5, 2));

            Put(context, 5, "Field", 0.5);

            context.ExecuteAdvancedAnalyticsAdaptation();

            var stage = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages
                .AdvancedAnalyticsAdaptation;

            stage.Items.Keys.Should().BeEquivalentTo("One", "Two");
            stage.Items.Values.Should().OnlyContain(i => i.ComputeTimeMicroseconds >= 0 && i.Memory >= 0);
            stage.DurationMicroseconds.Should().BeGreaterThanOrEqualTo(0);

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.EnumerateStages()
                .Select(s => s.Name).Should().Contain("AdvancedAnalyticsAdaptation");
        }

        [Fact]
        public void AnUnsampledInvocationRecordsNoStageTiming()
        {
            var context = NewContext(EngineModelFactory.Model("One", single));
            context.LogSampled = false;
            Put(context, 5, "Field", 0.5);

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context).Should().ContainKey("One");
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
        }

        [Fact]
        public void NoModelsLeavesTheScoresEmpty()
        {
            var context = NewContext();

            context.ExecuteAdvancedAnalyticsAdaptation();

            Scores(context).Should().BeEmpty();
        }

        [Fact]
        public void ExhaustedBudgetSkipsEveryModelAndLeavesNoScore()
        {
            var context = NewContext(EngineModelFactory.Model("First", single, 5, 1),
                EngineModelFactory.Model("Second", single, 5, 2));

            Put(context, 5, "Field", 1);

            AdvancedAnalyticsAdaptationExtensions.ExecuteAdvancedAnalyticsAdaptation(context, -1);

            Scores(context).Should().BeEmpty();
        }

        [Fact]
        public void ExhaustedBudgetDoesNotRecordTimingForSkippedModels()
        {
            var context = NewContext(EngineModelFactory.Model("First", single));
            Put(context, 5, "Field", 1);

            AdvancedAnalyticsAdaptationExtensions.ExecuteAdvancedAnalyticsAdaptation(context, -1);

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.AdvancedAnalyticsAdaptation
                .Items.Should().BeEmpty();
        }

        [Fact]
        public void GenerousBudgetScoresEveryModel()
        {
            var context = NewContext(EngineModelFactory.Model("First", single, 5, 1),
                EngineModelFactory.Model("Second", single, 5, 2));

            Put(context, 5, "Field", 1);

            AdvancedAnalyticsAdaptationExtensions.ExecuteAdvancedAnalyticsAdaptation(context, long.MaxValue);

            Scores(context).Keys.Should().BeEquivalentTo("First", "Second");
        }

        [Fact]
        public void TheBudgetIsCheckedBetweenModelsSoALaterModelIsSkippedOnceTheBudgetIsSpent()
        {
            var slow = EngineModelFactory.Model("Slow", single, 5, 1);
            var inner = slow.Recall;

            slow.Recall = row =>
            {
                Thread.Sleep(40);

                return inner(row);
            };

            var context = NewContext(slow, EngineModelFactory.Model("Late", single, 5, 2));
            Put(context, 5, "Field", 1);

            AdvancedAnalyticsAdaptationExtensions.ExecuteAdvancedAnalyticsAdaptation(context,
                (long)(10 * (Stopwatch.Frequency / 1000.0)));

            Scores(context).Keys.Should().BeEquivalentTo("Slow");

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.AdvancedAnalyticsAdaptation
                .Items.Keys.Should().BeEquivalentTo("Slow");
        }

        [Fact]
        public void TheBudgetIsCheckedBetweenInputExtractionsSoAnOverrunModelLeavesNoScoreButIsTimed()
        {
            var context = NewContext(EngineModelFactory.Model("Wide", fixture));
            Put(context, 5, "A", 1);
            var stageStopwatchTicks = (long)(1 * (Stopwatch.Frequency / 1000.0));
            var model = context.EntityAnalysisModel.Snapshot.AdvancedAnalyticsModels[0];

            model.VariablesInOrder.Insert(0,
                new AdvancedAnalyticsModelVariable { Name = "Spin", ProcessingTypeId = 5 });

            var spin = Stopwatch.StartNew();

            while (spin.ElapsedMilliseconds < 5)
            {
            }

            AdvancedAnalyticsAdaptationExtensions.ExecuteAdvancedAnalyticsAdaptation(context, stageStopwatchTicks);

            Scores(context).Should().BeEmpty();
        }

        [Fact]
        public void WithoutEnvironmentTheBudgetIsTheDefault()
        {
            var context = NewContext();

            var ticks = AdvancedAnalyticsAdaptationExtensions.ResolveBudgetTicks(context);

            ticks.Should().Be((long)(1000 * (Stopwatch.Frequency / 1000.0)));
        }

        [Fact]
        public void TheEnvironmentDefaultIsOneSecond()
        {
            var context = NewContext(TestDynamicEnvironment.Create());

            AdvancedAnalyticsAdaptationExtensions.ResolveBudgetTicks(context).Should()
                .Be((long)(1000 * (Stopwatch.Frequency / 1000.0)));
        }

        [Fact]
        public void TheConfiguredStageTimeoutIsUsedAndZeroDisablesTheBudget()
        {
            var configured = NewContext(TestDynamicEnvironment.Create(
                new Dictionary<string, string> { ["AdvancedAnalyticsStageTimeout"] = "250" }));
            var disabled = NewContext(TestDynamicEnvironment.Create(
                new Dictionary<string, string> { ["AdvancedAnalyticsStageTimeout"] = "0" }));

            AdvancedAnalyticsAdaptationExtensions.ResolveBudgetTicks(configured).Should()
                .Be((long)(250 * (Stopwatch.Frequency / 1000.0)));

            AdvancedAnalyticsAdaptationExtensions.ResolveBudgetTicks(disabled).Should().Be(long.MaxValue);
        }

        [Fact]
        public void TheStageOutputSitsBesideTheExhaustiveAdaptationAndDoesNotTouchIt()
        {
            var context = NewContext(EngineModelFactory.Model("M", single));
            Put(context, 5, "Field", 0.5);
            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"] = 0.123;

            context.ExecuteAdvancedAnalyticsAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should().Be(0.123);
            Scores(context)["M"].Should().Be(Expected(single, 0.5));
        }

        [Fact]
        public void TheStageRunsAfterExhaustiveAndBeforeHttpAdaptationInTheRegisteredStageSequence()
        {
            var sequence = ResponseTimePipelineExtensions.StageSequence.ToList();

            sequence.IndexOf("AdvancedAnalyticsAdaptation").Should().Be(sequence.IndexOf("ExhaustiveAdaptation") + 1);
            sequence.IndexOf("HttpAdaptation").Should().Be(sequence.IndexOf("AdvancedAnalyticsAdaptation") + 1);
            Enum.TryParse<InvokeStage>("AdvancedAnalyticsAdaptation", out _).Should().BeTrue();
            sequence.Should().OnlyContain(s => Enum.IsDefined(typeof(InvokeStage), s));
        }
    }
}