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
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsSyncBuildModelTests
    {
        private static readonly TrainedFixture fixture = EngineModelFactory.Trained(31,
            ["Payload.Z", "Abstraction.A", "TtlCounter.M"],
            [1, 5, 3]);

        private static AdvancedAnalyticsInstance Instance(byte responsePayload = 0, byte reportTable = 0)
        {
            return new AdvancedAnalyticsInstance
            {
                Id = 4,
                Guid = Guid.NewGuid(),
                Name = "Instance",
                Active = 1,
                ResponsePayload = responsePayload,
                ReportTable = reportTable
            };
        }

        private static AdvancedAnalyticsTrainedModel Trained(string? name = "Fraud Model")
        {
            return new AdvancedAnalyticsTrainedModel
            {
                Id = 12,
                Guid = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                Name = name!,
                Active = 1,
                ActiveVersionId = 99
            };
        }

        private static AdvancedAnalyticsTrainedModelVersion Version(string? modelJson = null,
            string? variablesJson = null)
        {
            return new AdvancedAnalyticsTrainedModelVersion
            {
                Id = 99,
                VersionNumber = 1,
                ModelJson = modelJson ?? fixture.SnapshotJson,
                VariablesJson = variablesJson ?? fixture.VariablesJson
            };
        }

        [Fact]
        public void IdentityGuidKindAndVersionComeFromTheRecords()
        {
            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                Version());

            model.Should().NotBeNull();
            model!.Id.Should().Be(12);
            model.Guid.Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));
            model.AdvancedAnalyticsInstanceId.Should().Be(4);
            model.VersionId.Should().Be(99);
            model.Kind.Should().Be("LogisticRegressionPipeline");
            model.Recall.Should().NotBeNull();
        }

        [Fact]
        public void SpacesInTheNameBecomeUnderscoresAndANullNameDefaults()
        {
            SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained("A B  C"),
                    Version())!
                .Name.Should().Be("A_B__C");

            SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(null), Version())!
                .Name.Should().Be("AdvancedAnalytics_12");
        }

        [Theory]
        [InlineData((byte)0, false)]
        [InlineData((byte)1, true)]
        [InlineData((byte)2, false)]
        public void ResponsePayloadAndReportTableAreTrueOnlyForOne(byte value, bool expected)
        {
            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(value, value),
                Trained(), Version())!;

            model.ResponsePayload.Should().Be(expected);
            model.ReportTable.Should().Be(expected);
        }

        [Fact]
        public void VariablesKeepTheStoredOrderWithTheirProcessingTypes()
        {
            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                Version())!;

            model.VariablesInOrder.Select(s => s.Name).Should().Equal("Payload.Z", "Abstraction.A", "TtlCounter.M");
            model.VariablesInOrder.Select(s => s.ProcessingTypeId).Should().Equal(1, 5, 3);
        }

        [Fact]
        public void TheBuiltRecallScoresLikeTheSnapshot()
        {
            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                Version())!;

            double[] row = [0.1, 0.2, 0.3];
            model.Recall(row).Should().Equal(fixture.Recall.Predict(row));
        }

        [Fact]
        public void ExplicitVariableRowsAreAcceptedByTheSeam()
        {
            var rows = new List<AdvancedAnalyticsVariableRow>
            {
                new() { Name = "Payload.Z", ProcessingTypeId = 2 },
                new() { Name = "Abstraction.A", ProcessingTypeId = 2 },
                new() { Name = "TtlCounter.M", ProcessingTypeId = 2 }
            };

            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                Version(), rows)!;

            model.VariablesInOrder.Should().OnlyContain(o => o.ProcessingTypeId == 2);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{\"kind\":\"Nope\"}")]
        [InlineData(
            "{\"kind\":\"LogisticRegressionPipeline\",\"schemaVersion\":1,\"inputCount\":3,\"variablesInOrder\":[\"a\",\"b\",\"c\"],\"payload\":{},\"calibrator\":null}")]
        public void ACorruptOrUnknownSnapshotReportsTheErrorAndYieldsNoModel(string modelJson)
        {
            var errors = new List<Exception>();

            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                Version(modelJson), errors.Add);

            model.Should().BeNull();
            errors.Should().ContainSingle();
        }

        [Fact]
        public void ACorruptSnapshotDoesNotStopOtherModelsFromBuilding()
        {
            var errors = new List<Exception>();

            var models = new[] { Version("garbage"), Version(), Version("{\"kind\":\"Nope\"}") }
                .Select(v => SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                    v,
                    errors.Add))
                .ToList();

            models.Count(c => c != null).Should().Be(1);
            errors.Should().HaveCount(2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("garbage")]
        [InlineData("{\"name\":\"x\"}")]
        [InlineData("[{\"name\":\"Payload.Z\",\"processingTypeId\":1}]")]
        [InlineData(
            "[{\"name\":\"Payload.Z\",\"processingTypeId\":1},{\"name\":\"Other\",\"processingTypeId\":5},{\"name\":\"TtlCounter.M\",\"processingTypeId\":3}]")]
        public void MissingMalformedMismatchedOrMisnamedVariablesYieldNoModel(string? variablesJson)
        {
            var errors = new List<Exception>();
            var version = Version();
            version.VariablesJson = variablesJson!;

            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                version, errors.Add);

            model.Should().BeNull();
            errors.Should().ContainSingle();
        }

        [Fact]
        public void VariablePropertyNamesAreCaseInsensitive()
        {
            var version = Version(variablesJson:
                "[{\"Name\":\"Payload.Z\",\"ProcessingTypeId\":1},{\"NAME\":\"Abstraction.A\",\"processingtypeid\":5},{\"name\":\"TtlCounter.M\",\"processingTypeId\":3}]");

            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                version);

            model.Should().NotBeNull();
        }

        [Fact]
        public void AVariablesJsonWithNoTransformationIdPropertyDefaultsToNoneAndRecallsIdentically()
        {
            var withTransformProperty = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(
                Instance(), Trained(), Version())!;

            withTransformProperty.VariablesInOrder.Should()
                .OnlyContain(v => v.TransformationId == VariableTransformationKind.None);

            withTransformProperty.VariablesInOrder.Should().OnlyContain(v => v.TransformationParametersJson == null);

            double[] row = [0.1, 0.2, 0.3];
            withTransformProperty.Recall(row).Should().Equal(fixture.Recall.Predict(row));
        }

        [Fact]
        public void TransformationIdAndParametersRoundTripThroughVariablesJsonLosslessly()
        {
            var variablesJson = AdvancedAnalyticsVariableRowsJson.Build(
                ["Payload.Z", "Abstraction.A", "TtlCounter.M"],
                ["Payload.Z", "Abstraction.A", "TtlCounter.M"],
                [1, 5, 3],
                [
                    VariableTransformationKind.Log, VariableTransformationKind.None,
                    VariableTransformationKind.ZScoreStandard
                ],
                ["{\"offset\":0}", null, "{\"center\":1,\"scale\":2}"]);

            var model = SyncAdvancedAnalyticsInstancesExtensions.BuildAdvancedAnalyticsModel(Instance(), Trained(),
                Version(variablesJson: variablesJson))!;

            model.VariablesInOrder[0].TransformationId.Should().Be(VariableTransformationKind.Log);
            model.VariablesInOrder[0].TransformationParametersJson.Should().Be("{\"offset\":0}");
            model.VariablesInOrder[1].TransformationId.Should().Be(VariableTransformationKind.None);
            model.VariablesInOrder[1].TransformationParametersJson.Should().BeNull();
            model.VariablesInOrder[2].TransformationId.Should().Be(VariableTransformationKind.ZScoreStandard);
            model.VariablesInOrder[2].TransformationParametersJson.Should().Be("{\"center\":1,\"scale\":2}");
        }
    }
}