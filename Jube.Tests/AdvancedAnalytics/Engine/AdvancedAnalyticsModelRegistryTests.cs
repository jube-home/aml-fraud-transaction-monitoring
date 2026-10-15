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
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Recall.Models;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsModelRegistryTests
    {
        [Fact]
        public void LogisticRegressionPipelineIsRegisteredAndBuildsARecallWithTheVariablesInOrder()
        {
            var fixture = EngineModelFactory.Trained(1, ["A", "B", "C"]);

            var model = AdvancedAnalyticsModelRegistry.Build(fixture.SnapshotJson);

            AdvancedAnalyticsModelRegistry.Kinds.Should().Contain("LogisticRegressionPipeline");
            model.Kind.Should().Be("LogisticRegressionPipeline");
            model.Variables.Should().Equal("A", "B", "C");
            model.Predict([0.1, 0.2, 0.3]).Should().ContainSingle().Which.Should().BeInRange(0d, 1d);
        }

        [Fact]
        public void AnUnknownKindIsRefusedWithAClearMessageNamingTheRegisteredKinds()
        {
            var json =
                "{\"kind\":\"NoSuchFamily\",\"schemaVersion\":1,\"inputCount\":1,\"variablesInOrder\":[\"A\"],\"payload\":{},\"calibrator\":null}";

            var act = () => AdvancedAnalyticsModelRegistry.Build(json);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>()
                .WithMessage("*NoSuchFamily*LogisticRegressionPipeline*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("[1,2]")]
        [InlineData("{\"schemaVersion\":1}")]
        public void ACorruptDocumentIsRefused(string json)
        {
            var act = () => AdvancedAnalyticsModelRegistry.Build(json);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void AKnownKindWithACorruptPayloadIsRefused()
        {
            var json =
                "{\"kind\":\"LogisticRegressionPipeline\",\"schemaVersion\":1,\"inputCount\":1,\"variablesInOrder\":[\"A\"],\"payload\":{\"weights\":1},\"calibrator\":null}";

            var act = () => AdvancedAnalyticsModelRegistry.Build(json);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void ANewFamilyRegistersInOnePlaceAndIsThenBuilt()
        {
            var kind = "TestFamily" + Guid.NewGuid().ToString("N");

            AdvancedAnalyticsModelRegistry.Register(kind,
                _ => new AdvancedAnalyticsRecallModel(kind, ["X"], row => [row[0] * 2]));

            var json = "{\"kind\":\"" + kind + "\"}";

            var model = AdvancedAnalyticsModelRegistry.Build(json);

            AdvancedAnalyticsModelRegistry.IsRegistered(kind).Should().BeTrue();
            model.Predict([4])[0].Should().Be(8);
        }

        [Fact]
        public void ARecallWithoutVariablesIsRefused()
        {
            var kind = "EmptyFamily" + Guid.NewGuid().ToString("N");

            AdvancedAnalyticsModelRegistry.Register(kind,
                _ => new AdvancedAnalyticsRecallModel(kind, [], row => row));

            var act = () => AdvancedAnalyticsModelRegistry.Build("{\"kind\":\"" + kind + "\"}");

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*no variables*");
        }
    }
}