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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Service.Charts;
using Jube.Service.Query.AdvancedAnalyticsChart;
using Xunit;

namespace Jube.Test.ModelDiagrams
{
    public class AnalyticsPageModelDiagramTests
    {
        public static IEnumerable<object[]> Kinds()
        {
            return ModelDiagramFixtures.TrainedSnapshotKinds.Select(k => new object[] { k });
        }

        [Theory]
        [MemberData(nameof(Kinds))]
        public void TheModelDiagramTabDrawsEveryKindInsteadOfTheEmptyState(string kind)
        {
            var json = ModelDiagramFixtures.Read(ModelDiagramFixtures.SnapshotFixtureName(kind));
            var source = new AdvancedAnalyticsChartSource("Fraud model", 3, json, null, null, []);

            var svg = AdvancedAnalyticsChartRenderer.ModelDiagram(source);

            svg.Should().StartWith("<svg").And.Contain("Fraud model version 3");
            svg.Should().NotContain("no diagram yet").And.NotContain(AdvancedAnalyticsChartRenderer.EmptyMessage);
            svg.Should().NotContain("<style");
        }

        [Fact]
        public void AMultipleLinearRegressionVersionPicksUpItsEvidenceSideCar()
        {
            var json = ModelDiagramFixtures.Read("snapshot-multiple-linear-regression");

            const string metrics =
                "{\"coefficients\":[{\"name\":\"(Intercept)\",\"kind\":\"Intercept\",\"estimate\":1.0}," +
                "{\"name\":\"amount\",\"kind\":\"Predictor\",\"estimate\":2.5,\"standardError\":0.5,\"pValue\":0.001}]}";

            var source = new AdvancedAnalyticsChartSource("Fraud model", 1, json, metrics, null, []);

            var svg = AdvancedAnalyticsChartRenderer.ModelDiagram(source);

            svg.Should().Contain("amount").And.NotContain("velocity");
        }

        private const string TransformedAmountOnlyVariablesJson =
            "[{\"name\":\"amount\",\"processingTypeId\":1,\"transformationId\":\"log\"}," +
            "{\"name\":\"velocity\",\"processingTypeId\":1,\"transformationId\":\"none\"}," +
            "{\"name\":\"noise\",\"processingTypeId\":1,\"transformationId\":\"none\"}]";

        [Fact]
        public void ATransformedVariableShowsItsDecoratedNameWhileAnUntransformedVariableIsUnchanged()
        {
            var json = ModelDiagramFixtures.Read("snapshot-logistic-pipeline");

            var source = new AdvancedAnalyticsChartSource("Fraud model", 1, json, null, null, [],
                TransformedAmountOnlyVariablesJson);

            var svg = AdvancedAnalyticsChartRenderer.ModelDiagram(source);

            svg.Should().Contain("amountLog");
            svg.Should().Contain("velocity").And.NotContain("velocityLog");
            svg.Should().Contain("noise").And.NotContain("noiseLog");
        }

        [Fact]
        public void ATransformedVariablesEmbeddedEvidenceCoefficientLabelIsAlsoDecorated()
        {
            var json = ModelDiagramFixtures.Read("snapshot-logistic-pipeline-evidence");

            var source = new AdvancedAnalyticsChartSource("Fraud model", 1, json, null, null, [],
                TransformedAmountOnlyVariablesJson);

            var svg = AdvancedAnalyticsChartRenderer.ModelDiagram(source);

            svg.Should().Contain("amountLog");
            svg.Should().Contain("velocity").And.NotContain("velocityLog");
        }

        [Fact]
        public void NoVariablesJsonLeavesEveryLabelExactlyAsStoredInTheModelJson()
        {
            var json = ModelDiagramFixtures.Read("snapshot-logistic-pipeline");
            var source = new AdvancedAnalyticsChartSource("Fraud model", 1, json, null, null, []);

            var svg = AdvancedAnalyticsChartRenderer.ModelDiagram(source);

            svg.Should().Contain("amount").And.NotContain("amountLog");
        }

        [Fact]
        public void AllVariablesUntransformedLeavesEveryLabelExactlyAsStoredInTheModelJson()
        {
            var json = ModelDiagramFixtures.Read("snapshot-logistic-pipeline");

            const string variablesJson =
                "[{\"name\":\"amount\",\"processingTypeId\":1,\"transformationId\":\"none\"}," +
                "{\"name\":\"velocity\",\"processingTypeId\":1,\"transformationId\":\"none\"}," +
                "{\"name\":\"noise\",\"processingTypeId\":1,\"transformationId\":\"none\"}]";

            var source = new AdvancedAnalyticsChartSource("Fraud model", 1, json, null, null, [], variablesJson);

            var svg = AdvancedAnalyticsChartRenderer.ModelDiagram(source);

            svg.Should().Contain("amount").And.NotContain("amountLog");
        }

        [Fact]
        public void MalformedVariablesJsonIsIgnoredAndTheDiagramStillRendersFromTheRawStoredNames()
        {
            var json = ModelDiagramFixtures.Read("snapshot-logistic-pipeline");

            var source = new AdvancedAnalyticsChartSource("Fraud model", 1, json, null, null, [],
                "{this is not valid json");

            var act = () => AdvancedAnalyticsChartRenderer.ModelDiagram(source);

            act.Should().NotThrow();
            act().Should().Contain("amount").And.NotContain("amountLog");
        }
    }
}