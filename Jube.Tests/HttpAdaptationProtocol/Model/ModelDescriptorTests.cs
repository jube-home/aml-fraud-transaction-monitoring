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
using FluentAssertions;
using Jube.HttpAdaptationProtocol;
using Jube.HttpAdaptationProtocol.Model;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Model
{
    [Trait("Category", "Unit")]
    public sealed class ModelDescriptorTests
    {
        [Fact]
        public void HoldsAssignedFieldsForAGlmModel()
        {
            var trainedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var validation = new ValidationDescriptor { Auc = 0.8 };

            var descriptor = new ModelDescriptor
            {
                Name = "fraud-glm-v3",
                Family = ProtocolConstants.Family.Glm,
                Version = "3.0.1",
                ArtifactHash = "sha256:abc123",
                TrainedDate = trainedDate,
                FeatureCount = 42,
                Validation = validation
            };

            descriptor.Name.Should().Be("fraud-glm-v3");
            descriptor.Family.Should().Be("GLM");
            descriptor.Version.Should().Be("3.0.1");
            descriptor.ArtifactHash.Should().Be("sha256:abc123");
            descriptor.TrainedDate.Should().Be(trainedDate);
            descriptor.FeatureCount.Should().Be(42);
            descriptor.Validation.Should().BeSameAs(validation);
        }

        [Fact]
        public void HoldsAssignedFieldsForARandomForestModel()
        {
            var descriptor = new ModelDescriptor
            {
                Name = "fraud-rf-v1",
                Family = ProtocolConstants.Family.RandomForest,
                BootstrapReplicates = 500
            };

            descriptor.Family.Should().Be("RandomForest");
            descriptor.BootstrapReplicates.Should().Be(500);
        }

        [Fact]
        public void HoldsAssignedFieldsForAc5Model()
        {
            var descriptor = new ModelDescriptor
            {
                Name = "fraud-c5-v2",
                Family = ProtocolConstants.Family.C5,
                LabelsVersion = "2.0",
                LabelsHash = "sha256:def456"
            };

            descriptor.LabelsVersion.Should().Be("2.0");
            descriptor.LabelsHash.Should().Be("sha256:def456");
        }

        [Fact]
        public void HoldsAssignedFieldsForABayesianNetworkModel()
        {
            var topologyDate = new DateTime(2023, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            var weightsDate = new DateTime(2023, 8, 1, 0, 0, 0, DateTimeKind.Utc);

            var descriptor = new ModelDescriptor
            {
                Name = "fraud-bn-v1",
                Family = ProtocolConstants.Family.BayesianNetwork,
                TopologyVersion = "1.0",
                TopologyHash = "sha256:topo",
                TopologyDate = topologyDate,
                WeightsVersion = "1.1",
                WeightsHash = "sha256:weights",
                WeightsDate = weightsDate,
                StructureLearning = ProtocolConstants.StructureLearning.HillClimbing,
                WhitelistedArcs = 3,
                BlacklistedArcs = 1
            };

            descriptor.TopologyVersion.Should().Be("1.0");
            descriptor.TopologyHash.Should().Be("sha256:topo");
            descriptor.TopologyDate.Should().Be(topologyDate);
            descriptor.WeightsVersion.Should().Be("1.1");
            descriptor.WeightsHash.Should().Be("sha256:weights");
            descriptor.WeightsDate.Should().Be(weightsDate);
            descriptor.StructureLearning.Should().Be("HillClimbing");
            descriptor.WhitelistedArcs.Should().Be(3);
            descriptor.BlacklistedArcs.Should().Be(1);
        }

        [Fact]
        public void HoldsAssignedFieldsForANeuralNetworkModel()
        {
            var descriptor = new ModelDescriptor
            {
                Name = "fraud-nn-v1",
                Family = ProtocolConstants.Family.NeuralNetwork,
                HiddenLayers = 2,
                ProcessingElements = 64
            };

            descriptor.HiddenLayers.Should().Be(2);
            descriptor.ProcessingElements.Should().Be(64);
        }

        [Fact]
        public void AllFieldsDefaultToNullWhenNotAssigned()
        {
            var descriptor = new ModelDescriptor();

            descriptor.Name.Should().BeNull();
            descriptor.Family.Should().BeNull();
            descriptor.Version.Should().BeNull();
            descriptor.ArtifactHash.Should().BeNull();
            descriptor.TrainedDate.Should().BeNull();
            descriptor.FeatureCount.Should().BeNull();
            descriptor.Validation.Should().BeNull();
            descriptor.BootstrapReplicates.Should().BeNull();
            descriptor.LabelsVersion.Should().BeNull();
            descriptor.LabelsHash.Should().BeNull();
            descriptor.TopologyVersion.Should().BeNull();
            descriptor.TopologyHash.Should().BeNull();
            descriptor.TopologyDate.Should().BeNull();
            descriptor.WeightsVersion.Should().BeNull();
            descriptor.WeightsHash.Should().BeNull();
            descriptor.WeightsDate.Should().BeNull();
            descriptor.StructureLearning.Should().BeNull();
            descriptor.WhitelistedArcs.Should().BeNull();
            descriptor.BlacklistedArcs.Should().BeNull();
            descriptor.HiddenLayers.Should().BeNull();
            descriptor.ProcessingElements.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJsonIncludingNestedValidation()
        {
            var original = new ModelDescriptor
            {
                Name = "fraud-glm-v3",
                Family = ProtocolConstants.Family.Glm,
                Version = "3.0.1",
                ArtifactHash = "sha256:abc123",
                TrainedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FeatureCount = 42,
                Validation = new ValidationDescriptor
                {
                    Date = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 1000,
                    Auc = 0.8,
                    Stale = false
                }
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ModelDescriptor>(json);

            deserialized.Should().NotBeNull();
            deserialized!.Name.Should().Be(original.Name);
            deserialized.Family.Should().Be(original.Family);
            deserialized.Version.Should().Be(original.Version);
            deserialized.ArtifactHash.Should().Be(original.ArtifactHash);
            deserialized.TrainedDate.Should().Be(original.TrainedDate);
            deserialized.FeatureCount.Should().Be(original.FeatureCount);
            deserialized.Validation.Should().Be(original.Validation);
        }
    }
}