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

using FluentAssertions;
using Jube.Data.Poco;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.Repository.AskJoobyAnalyticsModel;
using Xunit;
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Unit")]
    public sealed class AskJoobyAnalyticsModelMapperTests
    {
        private static Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AdvancedAnalyticsLogisticEvidence
            LogisticEvidence()
        {
            return new Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AdvancedAnalyticsLogisticEvidence
            {
                Kind = LogisticRegressionRecall.ModelKind,
                Seed = 7,
                Variables = ["Amount", "RiskScore"],
                SplitSizes = new LogisticSplitSizes(60, 20, 20, 30, 10, 10),
                Calibration = new LogisticCalibrationInfo(true, 1.0, 0.0, 20, null),
                Priors = new LogisticPriors(0.5, 0.5, 0.5, "TrainAndValidationRate"),
                Fit = new LogisticFitInfo(true, 5, 50, 0.0001, false, 0, 2, 15, "basis"),
                Coefficients = [],
                Warnings = []
            };
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{}")]
        public void ReadEvidence_ReturnsNullForMissingOrUnusableJson(string? json)
        {
            AskJoobyAnalyticsModelMapper.ReadEvidence(json).Should().BeNull();
        }

        [Fact]
        public void ReadEvidence_ReturnsTheLogisticEvidenceForALogisticKind()
        {
            var json = AdvancedAnalyticsEvidenceJson.Serialise(LogisticEvidence());

            var evidence = AskJoobyAnalyticsModelMapper.ReadEvidence(json);

            evidence.Should()
                .BeOfType<Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AdvancedAnalyticsLogisticEvidence>();
            evidence!.Kind.Should().Be(LogisticRegressionRecall.ModelKind);
            evidence.Seed.Should().Be(7);
        }

        [Fact]
        public void ReadEvidence_SaysNothingRatherThanOverreachForAnUnknownKind()
        {
            var json = AdvancedAnalyticsEvidenceJson.Serialise(new { kind = "SomeFutureModelKind", seed = 1 });

            AskJoobyAnalyticsModelMapper.ReadEvidence(json).Should().BeNull();
        }

        [Fact]
        public void ToPromotionEvidence_DispatchesLogisticEvidenceToTheLogisticDto()
        {
            var model = new AskJoobyAnalyticsTrainedModel { Id = 1, Name = "M", ActiveVersionId = 9 };
            var version = new AskJoobyAnalyticsTrainedModelVersion { Id = 9, VersionNumber = 1 };

            var result = AskJoobyAnalyticsModelMapper.ToPromotionEvidence(model, version, LogisticEvidence(), null);

            result.Should().BeOfType<AdvancedAnalyticsPromotionEvidenceDto>();
            ((AdvancedAnalyticsPromotionEvidenceDto)result).Seed.Should().Be(7);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not json")]
        [InlineData("{}")]
        [InlineData("[]")]
        public void TransformedVariableCountLabel_ReturnsNullWhenThereIsNothingUsableToCount(string? variablesJson)
        {
            AskJoobyAnalyticsModelMapper.TransformedVariableCountLabel(variablesJson).Should().BeNull();
        }

        [Fact]
        public void TransformedVariableCountLabel_CountsOnlyRowsWithATransformationOtherThanNone()
        {
            var json =
                "[{\"name\":\"A\",\"processingTypeId\":1,\"transformationId\":\"log\"}," +
                "{\"name\":\"B\",\"processingTypeId\":1,\"transformationId\":\"none\"}," +
                "{\"name\":\"C\",\"processingTypeId\":1,\"transformationId\":\"zScoreStandard\"}]";

            AskJoobyAnalyticsModelMapper.TransformedVariableCountLabel(json).Should()
                .Be("2 of 3 variables transformed");
        }

        [Fact]
        public void TransformedVariableCountLabel_TreatsAnAbsentTransformationIdPropertyAsNone()
        {
            var json = "[{\"name\":\"A\",\"processingTypeId\":1},{\"name\":\"B\",\"processingTypeId\":1}]";

            AskJoobyAnalyticsModelMapper.TransformedVariableCountLabel(json).Should()
                .Be("0 of 2 variables transformed");
        }

        [Fact]
        public void TransformedVariableCountLabel_AllVariablesTransformedIsReportedInFull()
        {
            var json =
                "[{\"name\":\"A\",\"processingTypeId\":1,\"transformationId\":\"log\"}," +
                "{\"name\":\"B\",\"processingTypeId\":1,\"transformationId\":\"minMax\"}]";

            AskJoobyAnalyticsModelMapper.TransformedVariableCountLabel(json).Should()
                .Be("2 of 2 variables transformed");
        }
    }
}