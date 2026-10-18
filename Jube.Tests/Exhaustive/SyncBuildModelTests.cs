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
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Helpers;
using Newtonsoft.Json;
using Xunit;
using Poco = Jube.Data.Poco;
using Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class SyncBuildModelTests
    {
        private static readonly JsonSerializationHelper helper = new();

        private static string Serialise(ActivationNetwork network)
        {
            return JsonConvert.SerializeObject(network, helper.TopologyNetworkJsonSerializerSettings);
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

        private static Poco.ExhaustiveSearchInstance Record(string name = "Fraud Model", byte? responsePayload = null,
            byte? reportTable = null)
        {
            return new Poco.ExhaustiveSearchInstance
            {
                Id = 12,
                Guid = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                Name = name,
                ResponsePayload = responsePayload,
                ReportTable = reportTable
            };
        }

        private static Poco.ExhaustiveSearchInstancePromotedTrialInstance Row(string json)
        {
            return new Poco.ExhaustiveSearchInstancePromotedTrialInstance { Json = json };
        }

        private static GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto Var(string name,
            int processingTypeId, double mean, double sd, byte normalisation)
        {
            return new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto
            {
                Name = name,
                ProcessingTypeId = processingTypeId,
                Mean = mean,
                StandardDeviation = sd,
                NormalisationTypeId = normalisation,
                Maximum = 99,
                Minimum = -99
            };
        }

        [Fact]
        public void IdentityAndGuidComeFromTheRecord()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(),
                Row(Serialise(Network(0, 1))), [], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.Id.Should().Be(12);
            model.Guid.Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        }

        [Fact]
        public void SpacesInTheNameBecomeUnderscores()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record("A B  C"),
                Row(Serialise(Network(0, 1))), [], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.Name.Should().Be("A_B__C");
        }

        [Fact]
        public void ANullNameDefaultsToExhaustiveUnderscoreId()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(null!),
                Row(Serialise(Network(0, 1))), [], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.Name.Should().Be("Exhaustive_12");
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData((byte)0, false)]
        [InlineData((byte)1, true)]
        [InlineData((byte)2, false)]
        public void ResponsePayloadAndReportTableAreTrueOnlyForOne(byte? value, bool expected)
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record("M", value, value),
                Row(Serialise(Network(0, 1))), [], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.ResponsePayload.Should().Be(expected);
            model.ReportTable.Should().Be(expected);
        }

        [Fact]
        public void TheNetworkJsonIsDeserialisedToAnEquivalentNetwork()
        {
            var original = Network(0.25, 0.5, -1.5, 2);

            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(),
                Row(Serialise(original)), [], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            double[] input = [0.1, 0.2, 0.3];
            model.TopologyNetwork.Compute(input).Should().Equal(original.Compute(input));
            model.TopologyNetwork.InputsCount.Should().Be(3);
        }

        [Fact]
        public void VariablesKeepTheOrderSuppliedAndCarryTheStatisticsPerVariable()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(),
                Row(Serialise(Network(0, 1, 1, 1))),
                [
                    Var("Payload.Z", 1, 10.5, 2.5, 2),
                    Var("Abstraction.A", 5, -3, 0, 1),
                    Var("TtlCounter.M", 3, 7, 1, 0)
                ], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.NetworkVariablesInOrder.Should().HaveCount(3);

            model.NetworkVariablesInOrder.Select(s => s.Name).Should().Equal("Payload.Z", "Abstraction.A",
                "TtlCounter.M");

            var first = model.NetworkVariablesInOrder[0];
            first.ProcessingTypeId.Should().Be(1);
            first.Mean.Should().Be(10.5);
            first.Sd.Should().Be(2.5);
            first.NormalisationTypeId.Should().Be(2);

            var second = model.NetworkVariablesInOrder[1];
            second.ProcessingTypeId.Should().Be(5);
            second.Mean.Should().Be(-3);
            second.Sd.Should().Be(0);
            second.NormalisationTypeId.Should().Be(1);

            var third = model.NetworkVariablesInOrder[2];
            third.ProcessingTypeId.Should().Be(3);
            third.NormalisationTypeId.Should().Be(0);
        }

        [Fact]
        public void TheLoadedVariablesStandardiseWithTheStoredStatistics()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(),
                Row(Serialise(Network(0, 1))), [Var("X", 5, 10, 4, 2)],
                helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.NetworkVariablesInOrder[0].ZScore(18).Should().Be(2);
        }

        [Fact]
        public void NoVariableRowsLeavesTheNetworkLoadedWithAnEmptyVariableList()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(),
                Row(Serialise(Network(0, 1))), null, helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.TopologyNetwork.Should().NotBeNull();
            model.NetworkVariablesInOrder.Should().BeEmpty();
        }

        [Fact]
        public void ANullJsonReportsTheLoadErrorAndStillReturnsTheModelWithoutNetworkOrVariables()
        {
            var errors = new List<Exception>();

            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(), Row(null!),
                [Var("X", 5, 0, 1, 0)], helper.DeserializeTopologyNetworkJsonSerializerSettings, errors.Add);

            errors.Should().ContainSingle();
            model.Should().NotBeNull();
            model.TopologyNetwork.Should().BeNull();
            model.NetworkVariablesInOrder.Should().BeEmpty();
            model.Name.Should().Be("Fraud_Model");
        }

        [Fact]
        public void AnEmptyJsonStringGivesANullNetworkAndNoError()
        {
            var errors = new List<Exception>();

            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(), Row(string.Empty),
                [], helper.DeserializeTopologyNetworkJsonSerializerSettings, errors.Add);

            model.TopologyNetwork.Should().BeNull();
        }

        [Fact]
        public void GarbageJsonNeverEscapesAsAnException()
        {
            var act = () => SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(),
                Row("{ this is not json"), [Var("X", 5, 0, 1, 0)],
                helper.DeserializeTopologyNetworkJsonSerializerSettings);

            act.Should().NotThrow();
        }

        [Fact]
        public void AnOnErrorCallbackIsOptional()
        {
            var act = () => SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(), Row(null!), [],
                helper.DeserializeTopologyNetworkJsonSerializerSettings);

            act.Should().NotThrow();
        }
    }
}