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
using Accord.Neuro;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.Exhaustive.Models;
using Jube.HttpAdaptationProtocol;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class RecallInPipelineTests
    {
        private static Context NewContext(params ExhaustiveSearchInstance[] models)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Snapshot.ExhaustiveModels.AddRange(models);

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

        private static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-2 * x));
        }

        private static ExhaustiveSearchInstancePromotedTrialInstanceVariable Variable(string name,
            int processingTypeId, double mean = 0, byte normalisationTypeId = 0, double sd = 1)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstanceVariable
            {
                Name = name,
                ProcessingTypeId = processingTypeId,
                Mean = mean,
                Sd = sd,
                NormalisationTypeId = normalisationTypeId
            };
        }

        private static ExhaustiveSearchInstance Model(string name, ActivationNetwork network,
            params ExhaustiveSearchInstancePromotedTrialInstanceVariable[] variables)
        {
            var model = new ExhaustiveSearchInstance { Id = 7, Name = name, TopologyNetwork = network };
            model.NetworkVariablesInOrder.AddRange(variables);

            return model;
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
        public void AbsentValue_WithoutStandardisation_FallsBackToTheRawMeanForEveryProcessingType(
            int processingTypeId)
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", processingTypeId, 1.5)));

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(1.5), 1e-12);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void AbsentValue_WithStandardisation_ImputesTheMeanAndStandardisesToExactlyZero(int processingTypeId)
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", processingTypeId, 42, 2, 3)));

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should().Be(0.5);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(2, 1)]
        [InlineData(3, 4)]
        [InlineData(4, 5)]
        [InlineData(5, 6)]
        [InlineData(6, 1)]
        public void AValueHeldInADifferentSourceThanTheProcessingTypeIsNotSeenAndTheMeanIsImputed(
            int processingTypeId, int otherSource)
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", processingTypeId, 0.25)));
            Put(context, otherSource, "Field", 9);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(0.25), 1e-12);
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
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", processingTypeId, 5)));
            Put(context, processingTypeId, "Field", 0);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should().Be(0.5);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void PresentValue_WithStandardisation_IsStandardisedAgainstTheStoredMeanAndSd(int processingTypeId)
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", processingTypeId, 10, 2, 4)));
            Put(context, processingTypeId, "Field", 18);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(2), 1e-12);
        }

        [Fact]
        public void PresentValue_WithConstantColumnSd_StandardisesToZeroRatherThanNonFinite()
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", 5, 10, 2, 0)));
            Put(context, 5, "Field", 18);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should().Be(0.5);
        }

        [Fact]
        public void InputVectorFollowsTheStoredVariableOrderAndNotTheNameOrder()
        {
            var network = Network(0, 1, 10);
            var ab = NewContext(Model("M", network, Variable("A", 5), Variable("B", 5)));
            Put(ab, 5, "A", 0.3);
            Put(ab, 5, "B", 0.05);
            var ba = NewContext(Model("M", network, Variable("B", 5), Variable("A", 5)));
            Put(ba, 5, "A", 0.3);
            Put(ba, 5, "B", 0.05);

            ab.ExecuteExhaustiveAdaptation();
            ba.ExecuteExhaustiveAdaptation();

            ab.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(0.3 * 1 + 0.05 * 10), 1e-12);
            ba.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(0.05 * 1 + 0.3 * 10), 1e-12);
        }

        [Fact]
        public void EachVariableIsResolvedFromItsOwnProcessingTypeInOneVector()
        {
            var context = NewContext(Model("M", Network(0, 1, 2, 3, 4, 5, 6),
                Variable("P", 1), Variable("D", 2), Variable("T", 3), Variable("S", 4), Variable("A", 5),
                Variable("C", 6)));

            Put(context, 1, "P", 0.01);
            Put(context, 2, "D", 0.02);
            Put(context, 3, "T", 0.03);
            Put(context, 4, "S", 0.04);
            Put(context, 5, "A", 0.05);
            Put(context, 6, "C", 0.06);

            context.ExecuteExhaustiveAdaptation();

            var x = 0.01 * 1 + 0.02 * 2 + 0.03 * 3 + 0.04 * 4 + 0.05 * 5 + 0.06 * 6;

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(x), 1e-12);
        }

        [Theory]
        [InlineData("Abstraction.Field", "Field")]
        [InlineData("Payload.Field", "Field")]
        [InlineData("Field", "Field")]
        [InlineData("A.Field.Extra", "Field")]
        public void OnlyTheSecondDotSegmentIsUsedAsTheLookupKey(string storedName, string key)
        {
            var context = NewContext(Model("M", Network(0, 1), Variable(storedName, 5)));
            Put(context, 5, key, 0.7);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(0.7), 1e-12);
        }

        [Fact]
        public void ADottedNameIsNotFoundUnderItsFullPrefixedKey()
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Abstraction.Field", 5, 0.5)));
            Put(context, 5, "Abstraction.Field", 0.9);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(0.5), 1e-12);
        }

        [Fact]
        public void LookupIsCaseSensitive()
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", 5, 0.5)));
            Put(context, 5, "field", 0.9);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(0.5), 1e-12);
        }

        [Fact]
        public void ANetworkThatThrowsOmitsTheEntryAndDoesNotRaise()
        {
            var context = NewContext(Model("Broken", Network(0, 1, 1), Variable("Field", 5)));
            Put(context, 5, "Field", 1);

            var act = () => context.ExecuteExhaustiveAdaptation();

            act.Should().NotThrow();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.ContainsKey("Broken").Should()
                .BeFalse();
        }

        [Fact]
        public void AModelWithoutANetworkOmitsTheEntryButStillRecordsTiming()
        {
            var context = NewContext(Model("NoNetwork", null!, Variable("Field", 5)));

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.ContainsKey("NoNetwork").Should()
                .BeFalse();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.ExhaustiveAdaptation.Items
                .Should().ContainKey("NoNetwork");
        }

        [Fact]
        public void AFailingModelDoesNotStopTheNextOneFromBeingRecalled()
        {
            var context = NewContext(
                Model("Broken", Network(0, 1, 1), Variable("Field", 5)),
                Model("Good", Network(0, 1), Variable("Field", 5)));

            Put(context, 5, "Field", 0.4);

            context.ExecuteExhaustiveAdaptation();

            var adaptation = context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation;
            adaptation.ContainsKey("Broken").Should().BeFalse();
            adaptation["Good"].Should().BeApproximately(Sigmoid(0.4), 1e-12);
        }

        [Fact]
        public void ANonFiniteScoreIsStoredUnchangedWithoutAnyFinitenessCheck()
        {
            var context = NewContext(Model("Nan", Network(0, double.NaN), Variable("Field", 5)));
            Put(context, 5, "Field", 1);

            context.ExecuteExhaustiveAdaptation();

            double.IsNaN(context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Nan"]).Should()
                .BeTrue();
        }

        [Fact]
        public void TheScoreIsNotRoundedInThePipeline()
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", 5)));
            Put(context, 5, "Field", 0.123456789);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .Be(Sigmoid(0.123456789));
        }

        [Fact]
        public void ThePayloadValueIsReadAsADoubleWhenTheStoredValueIsAnInteger()
        {
            var context = NewContext(Model("M", Network(0, 1), Variable("Field", 1)));
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 3);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["M"].Should()
                .BeApproximately(Sigmoid(3), 1e-12);
        }

        [Fact]
        public void ModelsAreKeyedByNameAndEachScoredIndependently()
        {
            var context = NewContext(
                Model("One", Network(0, 1), Variable("Field", 5)),
                Model("Two", Network(1, 1), Variable("Field", 5)));

            Put(context, 5, "Field", 0.5);

            context.ExecuteExhaustiveAdaptation();

            var adaptation = context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation;
            adaptation["One"].Should().BeApproximately(Sigmoid(0.5), 1e-12);
            adaptation["Two"].Should().BeApproximately(Sigmoid(0.5 + 1), 1e-12);
        }

        [Fact]
        public void ExhaustedBudget_SkipsEveryModelAndLeavesNoScore()
        {
            var context = NewContext(
                Model("First", Network(0, 1), Variable("Amount", 1)),
                Model("Second", Network(0, 1), Variable("Amount", 1)));

            Put(context, 1, "Amount", 1);

            ExhaustiveAdaptationsExtensions.ExecuteExhaustiveAdaptation(context, -1);

            Assert.Empty(context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation);
        }

        [Fact]
        public void GenerousBudget_ScoresEveryModel()
        {
            var context = NewContext(
                Model("First", Network(0, 1), Variable("Amount", 1)),
                Model("Second", Network(0, 1), Variable("Amount", 1)));

            Put(context, 1, "Amount", 1);

            ExhaustiveAdaptationsExtensions.ExecuteExhaustiveAdaptation(context, long.MaxValue);

            var scores = context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation;
            Assert.Equal(Sigmoid(1), scores["First"], 12);
            Assert.Equal(Sigmoid(1), scores["Second"], 12);
        }

        [Fact]
        public void ExhaustedBudget_DoesNotRecordTimingForSkippedModels()
        {
            var context = NewContext(Model("First", Network(0, 1), Variable("Amount", 1)));
            Put(context, 1, "Amount", 1);

            ExhaustiveAdaptationsExtensions.ExecuteExhaustiveAdaptation(context, -1);

            var stage = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages
                .ExhaustiveAdaptation;
            Assert.Empty(stage.Items);
        }

        [Fact]
        public void WithoutEnvironment_TheBudgetIsTheDefault()
        {
            var context = NewContext();

            var ticks = ExhaustiveAdaptationsExtensions.ResolveBudgetTicks(context);

            Assert.Equal((long)(ExhaustiveAdaptationsExtensions.DefaultBudgetMilliseconds *
                                (Stopwatch.Frequency / 1000.0)), ticks);
        }
    }
}