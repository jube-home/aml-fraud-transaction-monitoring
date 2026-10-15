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
using System.Linq;
using Accord.Neuro;
using FluentAssertions;
using Jube.Engine.BackgroundTasks.Context;
using Jube.Engine.EntityAnalysisModelManager;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Exhaustive.Models;
using Jube.Test.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Exhaustive
{
    using EntityAnalysisModel = EntityAnalysisModel;

    public class TrainRecallExhaustiveTests
    {
        private static ActivationNetwork SigmoidOfSum(int inputs)
        {
            var network = new ActivationNetwork(new SigmoidFunction(), inputs, [],
                1);

            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];

            for (var i = 0; i < inputs; i++)
            {
                neuron.Weights[i] = 1;
            }

            neuron.Threshold = 0;

            return network;
        }

        private static Context NewContext(params (int ModelId, ExhaustiveSearchInstance[] Instances)[] models)
        {
            var context = new Context();
            context.Services.Log = TestLog.NoOp;
            context.Tasks.EntityAnalysisModelManager = new EntityAnalysisModelManager(context);

            foreach (var (modelId, instances) in models)
            {
                var model = new EntityAnalysisModel();
                model.Snapshot.ExhaustiveModels.AddRange(instances);

                context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                        .ActiveEntityAnalysisModels[modelId] =
                    model;
            }

            return context;
        }

        private static ExhaustiveSearchInstance NewInstance(Guid guid, int inputs, params (string Name, double Mean,
            double Sd, byte Type)[] variables)
        {
            var instance = new ExhaustiveSearchInstance { Guid = guid, TopologyNetwork = SigmoidOfSum(inputs) };

            foreach (var (name, mean, sd, type) in variables)
            {
                instance.NetworkVariablesInOrder.Add(new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                {
                    Name = name, Mean = mean, Sd = sd, NormalisationTypeId = type
                });
            }

            return instance;
        }

        private static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-2 * x));
        }

        [Fact]
        public void RecallExhaustive_ScoresTheMatchingInstanceWithZScoredInputsInVariableOrder()
        {
            var guid = Guid.NewGuid();
            var context = NewContext((1, [NewInstance(guid, 2, ("a", 10, 2, 2), ("b", 0, 1, 1))]));

            var value = context.RecallExhaustive(guid, JObject.Parse("{\"a\": 14, \"b\": 1}"));

            value.Should().BeApproximately(Sigmoid((14 - 10) / 2d + 1), 1e-12);
        }

        [Fact]
        public void RecallExhaustive_ChoosesTheInstanceByGuidAcrossModels()
        {
            var wanted = Guid.NewGuid();
            var other = Guid.NewGuid();

            var context = NewContext(
                (1, [NewInstance(other, 1, ("a", 0, 1, 1))]),
                (2, [NewInstance(Guid.NewGuid(), 1, ("a", 0, 1, 1)), NewInstance(wanted, 1, ("a", 0, 1, 1))]));

            context.RecallExhaustive(wanted, JObject.Parse("{\"a\": 2}")).Should()
                .BeApproximately(Sigmoid(2), 1e-12);
        }

        [Fact]
        public void RecallExhaustive_AMissingOrNullOrUnparseableValue_IsImputedWithTheTrainingMean()
        {
            var guid = Guid.NewGuid();
            var context = NewContext((1, [NewInstance(guid, 1, ("a", 6, 3, 2))]));

            var missing = context.RecallExhaustive(guid, JObject.Parse("{}"));
            var nullValue = context.RecallExhaustive(guid, JObject.Parse("{\"a\": null}"));
            var text = context.RecallExhaustive(guid, JObject.Parse("{\"a\": \"abc\"}"));

            missing.Should().BeApproximately(Sigmoid(0), 1e-12);
            nullValue.Should().Be(missing);
            text.Should().Be(missing);
        }

        [Fact]
        public void RecallExhaustive_UnknownGuid_ReturnsZeroBecauseTheNotFoundErrorIsSwallowed()
        {
            var context = NewContext((1, [NewInstance(Guid.NewGuid(), 1, ("a", 0, 1, 1))]));

            context.RecallExhaustive(Guid.NewGuid(), JObject.Parse("{\"a\": 1}")).Should().Be(0);
        }

        [Fact]
        public void RecallExhaustive_NoActiveModels_ReturnsZero()
        {
            NewContext().RecallExhaustive(Guid.NewGuid(), JObject.Parse("{}")).Should().Be(0);
        }

        [Fact]
        public void RecallExhaustive_ANetworkWhoseInputCountDisagreesWithTheVariables_ReturnsZeroNotAnException()
        {
            var guid = Guid.NewGuid();
            var context = NewContext((1, [NewInstance(guid, 3, ("a", 0, 1, 1))]));

            var act = () => context.RecallExhaustive(guid, JObject.Parse("{\"a\": 1}"));

            act.Should().NotThrow();
            act().Should().Be(0);
        }

        [Fact]
        public void RecallExhaustive_AnInstanceWithNoNetwork_ReturnsZero()
        {
            var guid = Guid.NewGuid();
            var instance = new ExhaustiveSearchInstance { Guid = guid };
            var context = NewContext((1, [instance]));

            context.RecallExhaustive(guid, JObject.Parse("{}")).Should().Be(0);
        }

        [Fact]
        public void RecallExhaustiveWithExplainability_MatchesTheScoreOfRecallExhaustive()
        {
            var guid = Guid.NewGuid();
            var context = NewContext((1, [NewInstance(guid, 2, ("a", 10, 2, 2), ("b", 0, 1, 1))]));
            var body = JObject.Parse("{\"a\": 14, \"b\": 1}");

            var (value, relevance) = context.RecallExhaustiveWithExplainability(guid, body);

            value.Should().Be(context.RecallExhaustive(guid, body));
            relevance.Should().NotBeNull();
            relevance!.Variables.Select(v => v.Name).Should().Equal("a", "b");
        }

        [Fact]
        public void RecallExhaustiveWithExplainability_LrpRelevancePlusBaseValueReconstructsTheRawOutput()
        {
            var guid = Guid.NewGuid();
            var context = NewContext((1, [NewInstance(guid, 2, ("a", 10, 2, 2), ("b", 0, 1, 1))]));

            var (_, relevance) = context.RecallExhaustiveWithExplainability(guid,
                JObject.Parse("{\"a\": 14, \"b\": 1}"));

            var rawOutput = Sigmoid((14 - 10) / 2d + 1);

            (relevance!.Variables.Sum(v => v.LrpRelevance) + relevance.BaseValue).Should()
                .BeApproximately(rawOutput, 1e-6);
        }

        [Fact]
        public void RecallExhaustiveWithExplainability_UnknownGuid_ReturnsZeroAndNoRelevance()
        {
            var context = NewContext((1, [NewInstance(Guid.NewGuid(), 1, ("a", 0, 1, 1))]));

            var (value, relevance) = context.RecallExhaustiveWithExplainability(Guid.NewGuid(),
                JObject.Parse("{\"a\": 1}"));

            value.Should().Be(0);
            relevance.Should().BeNull();
        }
    }
}