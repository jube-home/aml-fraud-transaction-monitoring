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
using System.Linq;
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

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ExhaustiveAdaptationsExtensionsTests
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

        private static ActivationNetwork AlwaysHalfNetwork()
        {
            var network = new ActivationNetwork(new SigmoidFunction(), 1, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];
            neuron.Weights[0] = 0;
            neuron.Threshold = 0;

            return network;
        }

        private static ActivationNetwork IdentityWeightedSigmoidNetwork()
        {
            var network = new ActivationNetwork(new SigmoidFunction(), 1, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];
            neuron.Weights[0] = 1;
            neuron.Threshold = 0;

            return network;
        }

        private static ExhaustiveSearchInstancePromotedTrialInstanceVariable NewVariable(string name,
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

        private static ExhaustiveSearchInstance NewModel(string name, ActivationNetwork network,
            params ExhaustiveSearchInstancePromotedTrialInstanceVariable[] variables)
        {
            var model = new ExhaustiveSearchInstance
            {
                Id = 1,
                Name = name,
                TopologyNetwork = network
            };
            model.NetworkVariablesInOrder.AddRange(variables);

            return model;
        }

        [Fact]
        public void AddsTheRecalledScoreToTheExhaustiveAdaptationDictionaryUnderTheModelsName()
        {
            var model = NewModel("Model1", AlwaysHalfNetwork(), NewVariable("Field", 1));
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 1.0);

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Model1"].Should().Be(0.5);
        }

        [Fact]
        public void PayloadValuesAreExtractedAndPassedThroughTheNetworkWithZScoreApplied()
        {
            var model = NewModel("Model1", IdentityWeightedSigmoidNetwork(), NewVariable("Field", 1));
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 2.0);

            context.ExecuteExhaustiveAdaptation();

            var expected = 1 / (1 + Math.Exp(-2 * 2.0));

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Model1"].Should()
                .BeApproximately(expected, 1e-9);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void EachNonPayloadProcessingTypeIsExtractedFromItsOwnDictionaryAndRecalledCorrectly(
            int processingTypeId)
        {
            var model = NewModel("Model1", IdentityWeightedSigmoidNetwork(),
                NewVariable("Field", processingTypeId));

            var context = NewContext(model);

            var payload = context.EntityAnalysisModelInstanceEntryPayload;

            switch (processingTypeId)
            {
                case 2:
                    payload.Dictionary["Field"] = 3.0;
                    break;
                case 3:
                    payload.TtlCounter["Field"] = 3.0;
                    break;
                case 4:
                    payload.Sanction["Field"] = 3.0;
                    break;
                case 5:
                    payload.Abstraction["Field"] = 3.0;
                    break;
                case 6:
                    payload.AbstractionCalculation["Field"] = 3.0;
                    break;
            }

            context.ExecuteExhaustiveAdaptation();

            var expected = 1 / (1 + Math.Exp(-2 * 3.0));

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Model1"].Should()
                .BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void AnUnrecognisedProcessingTypeFallsBackToAbstraction()
        {
            var model = NewModel("Model1", IdentityWeightedSigmoidNetwork(),
                NewVariable("Field", 99));

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Field"] = 4.0;

            context.ExecuteExhaustiveAdaptation();

            var expected = 1 / (1 + Math.Exp(-2 * 4.0));

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Model1"].Should()
                .BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void AMissingValueFallsBackToTheVariablesConfiguredMean()
        {
            var model = NewModel("Model1", IdentityWeightedSigmoidNetwork(),
                NewVariable("Field", 1, 5.0));

            var context = NewContext(model);

            context.ExecuteExhaustiveAdaptation();

            var expected = 1 / (1 + Math.Exp(-2 * 5.0));

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Model1"].Should()
                .BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void ANameContainingADotUsesOnlyTheSegmentAfterTheFirstDotAsTheLookupKey()
        {
            var model = NewModel("Model1", IdentityWeightedSigmoidNetwork(),
                NewVariable("Prefix.Field", 1));

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 1.5);

            context.ExecuteExhaustiveAdaptation();

            var expected = 1 / (1 + Math.Exp(-2 * 1.5));

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Model1"].Should()
                .BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void NormalisationTypeTwoAppliesTheZScoreFormulaBeforeRecall()
        {
            var model = NewModel("Model1", IdentityWeightedSigmoidNetwork(),
                NewVariable("Field", 1, 10, 2, 2));

            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 14.0);

            context.ExecuteExhaustiveAdaptation();

            var expected = 1 / (1 + Math.Exp(-2 * 2.0));

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Model1"].Should()
                .BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void AnExceptionDuringOneModelsRecallIsCaughtAndSubsequentModelsStillRun()
        {
            var throwingModel = NewModel("Throws", null!, NewVariable("Field", 1));
            var second = NewModel("Second", AlwaysHalfNetwork(), NewVariable("Field", 1));
            var context = NewContext(throwingModel, second);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 1.0);

            var act = context.ExecuteExhaustiveAdaptation;

            act.Should().NotThrow();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.ContainsKey("Throws")
                .Should().BeFalse();

            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Second"].Should().Be(0.5);
        }

        [Fact]
        public void WhenSampledTheStageTimingRecordsOneItemPerModel()
        {
            var model = NewModel("Model1", AlwaysHalfNetwork(), NewVariable("Field", 1));
            var context = NewContext(model);

            context.ExecuteExhaustiveAdaptation();

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages;
            stages.Should().NotBeNull();
            stages!.ExhaustiveAdaptation.Should().NotBeNull();
            stages.ExhaustiveAdaptation!.Items.Should().ContainKey("Model1");
        }

        [Fact]
        public void WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns()
        {
            var model = NewModel("Model1", AlwaysHalfNetwork(), NewVariable("Field", 1));
            var context = NewContext(model);
            context.LogSampled = false;

            context.ExecuteExhaustiveAdaptation();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation["Model1"].Should().Be(0.5);
        }

        [Fact]
        public void WithNoModelsConfiguredNothingThrowsAndTheDictionaryStaysEmpty()
        {
            var context = NewContext();

            var act = context.ExecuteExhaustiveAdaptation;

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.Should().BeEmpty();
        }

        [Fact]
        public void TheExplainabilityDetailCarriesTheSameValueAsTheScoreAndReconstructsTheRawOutput()
        {
            var model = NewModel("Model1", IdentityWeightedSigmoidNetwork(), NewVariable("Field", 1));
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 2.0);

            context.ExecuteExhaustiveAdaptation();

            var payload = context.EntityAnalysisModelInstanceEntryPayload;
            var detail = payload.ExhaustiveAdaptationDetail["Model1"];

            detail.Value.Should().Be(payload.ExhaustiveAdaptation["Model1"]);
            detail.Model.Name.Should().Be("Model1");
            detail.Model.Family.Should().Be(ProtocolConstants.Family.NeuralNetwork);
            detail.Model.HiddenLayers.Should().Be(0);
            detail.Model.FeatureCount.Should().Be(1);
            detail.Contribution.Method.Should().Be(ProtocolConstants.ContributionMethod.LayerwiseRelevance);
            detail.Contribution.Exact.Should().BeTrue();
            detail.Contribution.Items.Should().ContainSingle().Which.Name.Should().Be("Field");
            var rawScore = 1 / (1 + Math.Exp(-2 * 2.0));

            (detail.Contribution.Items.Sum(i => i.Weight!.Value) + detail.Contribution.BaseValue!.Value).Should()
                .BeApproximately(rawScore, 1e-6);
        }

        [Fact]
        public void AnUncalibratedModelReportsNotCalibratedAndAScoreSpaceContribution()
        {
            var model = NewModel("Model1", AlwaysHalfNetwork(), NewVariable("Field", 1));
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 1.0);

            context.ExecuteExhaustiveAdaptation();

            var detail = context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptationDetail["Model1"];

            detail.Calibration.Calibrated.Should().BeFalse();
            detail.Calibration.Method.Should().Be(ProtocolConstants.CalibrationMethod.None);
            detail.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Score);
            detail.Calibration.Slope.Should().BeNull();
            detail.Calibration.Intercept.Should().BeNull();
        }

        [Fact]
        public void ContributionSourceIsMappedFromTheVariablesProcessingTypeId()
        {
            var network = new ActivationNetwork(new SigmoidFunction(), 2, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];
            neuron.Weights[0] = 1;
            neuron.Weights[1] = 1;
            var model = NewModel("Model1", network, NewVariable("Field", 1), NewVariable("Counter", 3));
            var context = NewContext(model);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Field", 1.0);
            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Add("Counter", 1.0);

            context.ExecuteExhaustiveAdaptation();

            var items = context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptationDetail["Model1"]
                .Contribution.Items;

            items.Single(i => i.Name == "Field").Source.Should().Be(ProtocolConstants.Source.Payload);
            items.Single(i => i.Name == "Counter").Source.Should().Be(ProtocolConstants.Source.TtlCounter);
        }
    }
}