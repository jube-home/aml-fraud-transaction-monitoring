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
using System.Globalization;
using Accord.Neuro;
using FluentAssertions;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Exhaustive.Models;
using Jube.Service.Invoke;
using Jube.Test.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;
using BackgroundContext = Jube.Engine.BackgroundTasks.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using Manager = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModelManager;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class RecallScoreTests
    {
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

        private static ExhaustiveSearchInstancePromotedTrialInstanceVariable Variable(string name, double mean = 0,
            double sd = 1, byte normalisation = 0)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstanceVariable
            {
                Name = name,
                Mean = mean,
                Sd = sd,
                NormalisationTypeId = normalisation
            };
        }

        [Fact]
        public void Score_AppliesTheNetworkToTheStandardisedInputsInVariableOrder()
        {
            var variables = new List<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            {
                Variable("Abstraction.A", 10, 2, 2), Variable("Abstraction.B")
            };

            var score = ExhaustiveExtensions.Score(Network(0.1, 1, 10), variables,
                JObject.Parse("{\"Abstraction.A\": 14, \"Abstraction.B\": 0.5}"));

            score.Should().BeApproximately(Sigmoid(2 * 1 + 0.5 * 10 + 0.1), 1e-12);
        }

        [Fact]
        public void Score_ReturnsTheFirstOutputOnly()
        {
            var network = new ActivationNetwork(new SigmoidFunction(), 1, null, 3);

            for (var i = 0; i < 3; i++)
            {
                var neuron = (ActivationNeuron)network.Layers[0].Neurons[i];
                neuron.Weights[0] = i + 1;
                neuron.Threshold = 0;
            }

            var score = ExhaustiveExtensions.Score(network, [Variable("X")], JObject.Parse("{\"X\": 0.3}"));

            score.Should().BeApproximately(Sigmoid(0.3), 1e-12);
        }

        [Fact]
        public void Score_WithAllValuesMissing_UsesTheImputedMeans()
        {
            var variables = new List<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            {
                Variable("A", 4, 2, 2), Variable("B", 0.5)
            };

            var score = ExhaustiveExtensions.Score(Network(0, 1, 1), variables, new JObject());

            score.Should().BeApproximately(Sigmoid(0 + 0.5), 1e-12);
        }

        [Fact]
        public void Score_WhenTheNetworkCannotBeComputed_Throws()
        {
            var act = () => ExhaustiveExtensions.Score(Network(0, 1, 1), [Variable("X")], new JObject());

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Score_WithANullNetwork_Throws()
        {
            var act = () => ExhaustiveExtensions.Score(null, [Variable("X")], new JObject());

            act.Should().Throw<NullReferenceException>();
        }

        [Fact]
        public void Score_WithFewerVariablesThanNetworkInputs_Throws()
        {
            var network = new ActivationNetwork(new SigmoidFunction(), 1, null, 1);
            ((ActivationNeuron)network.Layers[0].Neurons[0]).Threshold = 0;

            var act = () => ExhaustiveExtensions.Score(network, [], new JObject());

            act.Should().Throw<Exception>();
        }

        [Theory]
        [InlineData("true", 1)]
        [InlineData("false", 0)]
        [InlineData("\"2.5\"", 2.5)]
        [InlineData("\" 2.5 \"", 2.5)]
        [InlineData("7", 7)]
        [InlineData("1e2", 100)]
        [InlineData("-0.5", -0.5)]
        public void BuildScoreInputs_ConvertsEveryAcceptedTokenShape(string json, double expected)
        {
            var body = JObject.Parse("{\"X\": " + json + "}");

            ExhaustiveExtensions.BuildScoreInputs([Variable("X")], body).Should().Equal(expected);
        }

        [Theory]
        [InlineData("[1]")]
        [InlineData("{\"a\":1}")]
        [InlineData("\"abc\"")]
        [InlineData("\"\"")]
        public void BuildScoreInputs_UnconvertibleTokenIsMissingAndImputesTheMean(string json)
        {
            var body = JObject.Parse("{\"X\": " + json + "}");

            ExhaustiveExtensions.BuildScoreInputs([Variable("X", 3)], body).Should().Equal(3);
        }

        [Fact]
        public void BuildScoreInputs_NestedObjectsAreNotSearched()
        {
            var body = JObject.Parse("{\"Outer\": {\"X\": 5}}");

            ExhaustiveExtensions.BuildScoreInputs([Variable("X", 3)], body).Should().Equal(3);
        }

        [Fact]
        public void BuildScoreInputs_TraceReportsImputationAndConversionFailure()
        {
            var messages = new List<string>();

            ExhaustiveExtensions.BuildScoreInputs([Variable("X", 3), Variable("Y", 1)],
                JObject.Parse("{\"X\": \"abc\"}"), messages.Add);

            messages.Should().HaveCount(3);
            messages[0].Should().Contain("could not convert X");
            messages[1].Should().Contain("imputed").And.Contain("X");
            messages[2].Should().Contain("imputed").And.Contain("Y");
        }

        [Fact]
        public void BuildScoreInputs_WithNoVariables_ReturnsAnEmptyVector()
        {
            ExhaustiveExtensions.BuildScoreInputs([], JObject.Parse("{\"X\": 1}")).Should().BeEmpty();
        }

        [Fact]
        public void BuildScoreInputs_StringNumbersAreReadInvariantlyWhateverTheCurrentCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            try
            {
                ExhaustiveExtensions.BuildScoreInputs([Variable("X")], JObject.Parse("{\"X\": \"2.5\"}"))
                    .Should().Equal(2.5);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        private static ExhaustiveSearchInstance Instance(Guid guid, ActivationNetwork network,
            params ExhaustiveSearchInstancePromotedTrialInstanceVariable[] variables)
        {
            var instance = new ExhaustiveSearchInstance { Id = 1, Name = "M", Guid = guid, TopologyNetwork = network };
            instance.NetworkVariablesInOrder.AddRange(variables);

            return instance;
        }

        private static BackgroundContext ContextWith(TestLog log, params ExhaustiveSearchInstance[] instances)
        {
            var manager = new Manager(new BackgroundContext());
            var model = new EntityAnalysisModel();
            model.Snapshot.ExhaustiveModels.AddRange(instances);
            manager.Context.EntityAnalysisModels.ActiveEntityAnalysisModels.TryAdd(1, model);

            var context = new BackgroundContext();
            context.Services.Log = log;
            context.Tasks.EntityAnalysisModelManager = manager;

            return context;
        }

        [Fact]
        public void RecallExhaustive_ForAKnownGuid_ReturnsTheScore()
        {
            var guid = Guid.NewGuid();
            var context = ContextWith(new TestLog(), Instance(guid, Network(0, 1), Variable("X")));

            context.RecallExhaustive(guid, JObject.Parse("{\"X\": 0.4}")).Should()
                .BeApproximately(Sigmoid(0.4), 1e-12);
        }

        [Fact]
        public void RecallExhaustive_ForAnUnknownGuid_ReturnsZeroAndLogsTheError()
        {
            var log = new TestLog();
            var context = ContextWith(log, Instance(Guid.NewGuid(), Network(0, 1), Variable("X")));

            context.RecallExhaustive(Guid.NewGuid(), new JObject()).Should().Be(0);

            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("KeyNotFoundException"));
        }

        [Fact]
        public void RecallExhaustive_WhenTheNetworkThrows_SwallowsTheExceptionAndReturnsZero()
        {
            var guid = Guid.NewGuid();
            var log = new TestLog();
            var context = ContextWith(log, Instance(guid, Network(0, 1, 1), Variable("X")));

            context.RecallExhaustive(guid, JObject.Parse("{\"X\": 0.4}")).Should().Be(0);

            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Fact]
        public void RecallExhaustive_WhenTheNetworkIsNull_ReturnsZero()
        {
            var guid = Guid.NewGuid();
            var context = ContextWith(new TestLog(), Instance(guid, null!, Variable("X")));

            context.RecallExhaustive(guid, new JObject()).Should().Be(0);
        }

        [Fact]
        public void RecallExhaustive_ANonFiniteScoreIsReturnedUnchanged()
        {
            var guid = Guid.NewGuid();
            var context = ContextWith(new TestLog(), Instance(guid, Network(0, double.NaN), Variable("X")));

            double.IsNaN(context.RecallExhaustive(guid, JObject.Parse("{\"X\": 1}"))).Should().BeTrue();
        }

        [Fact]
        public void RecallExhaustive_FindsTheInstanceAcrossModels()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var context = ContextWith(new TestLog(), Instance(first, Network(0, 1), Variable("X")));
            var otherModel = new EntityAnalysisModel();
            otherModel.Snapshot.ExhaustiveModels.Add(Instance(second, Network(1, 1), Variable("X")));

            context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels.ActiveEntityAnalysisModels
                .TryAdd(2, otherModel);

            context.RecallExhaustive(second, JObject.Parse("{\"X\": 0.5}")).Should()
                .BeApproximately(Sigmoid(1.5), 1e-12);
        }

        private static ExhaustiveSearchInstance GateInstance()
        {
            return Instance(Guid.NewGuid(), null!, Variable("Amount"), Variable("Abstraction.Count"));
        }

        [Theory]
        [InlineData("{\"Amount\": 0}", true)]
        [InlineData("{\"Amount\": -1}", true)]
        [InlineData("{\"Amount\": 9223372036854775807}", true)]
        [InlineData("{\"Amount\": 1.7976931348623157e308}", true)]
        [InlineData("{\"Amount\": 5e-324}", true)]
        [InlineData("{\"Amount\": true}", true)]
        [InlineData("{\"Amount\": false}", true)]
        [InlineData("{\"Amount\": null}", true)]
        [InlineData("{\"Amount\": \"12\"}", true)]
        [InlineData("{\"Amount\": \" 12 \"}", true)]
        [InlineData("{\"Amount\": \"1.5e3\"}", true)]
        [InlineData("{\"Amount\": \"-0\"}", true)]
        [InlineData("{\"Amount\": \"+1\"}", true)]
        [InlineData("{\"Amount\": \".5\"}", true)]
        [InlineData("{\"Amount\": \"1,5\"}", false)]
        [InlineData("{\"Amount\": \"1,000\"}", false)]
        [InlineData("{\"Amount\": \"0x10\"}", false)]
        [InlineData("{\"Amount\": \"\"}", false)]
        [InlineData("{\"Amount\": \" \"}", false)]
        [InlineData("{\"Amount\": \"abc\"}", false)]
        [InlineData("{\"Amount\": \"NaN\"}", false)]
        [InlineData("{\"Amount\": \"Infinity\"}", false)]
        [InlineData("{\"Amount\": \"-Infinity\"}", false)]
        [InlineData("{\"Amount\": \"1e309\"}", false)]
        [InlineData("{\"Amount\": 1e309}", false)]
        [InlineData("{\"Amount\": -1e309}", false)]
        [InlineData("{\"Amount\": []}", false)]
        [InlineData("{\"Amount\": [1]}", false)]
        [InlineData("{\"Amount\": {}}", false)]
        [InlineData("{\"Amount\": {\"a\": 1}}", false)]
        [InlineData("{\"Amount\": \"2018-01-01T00:00:00Z\"}", false)]
        [InlineData("{}", true)]
        [InlineData("{\"Unrelated\": [1, {\"a\": 2}]}", true)]
        [InlineData("{\"amount\": [1]}", true)]
        [InlineData("{\"Amount\": 1, \"Abstraction.Count\": {}}", false)]
        [InlineData("{\"Amount\": 1, \"Abstraction.Count\": \"2\"}", true)]
        [InlineData("{\"Count\": [1]}", true)]
        public void RecallBodyHasValidTypes_Boundaries(string json, bool expected)
        {
            InvokeService.RecallBodyHasValidTypes(GateInstance(), JObject.Parse(json)).Should().Be(expected);
        }
    }
}