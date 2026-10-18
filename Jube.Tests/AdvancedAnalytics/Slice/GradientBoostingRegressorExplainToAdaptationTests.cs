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
using System.Linq;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.HttpAdaptationProtocol;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class GradientBoostingRegressorExplainToAdaptationTests
    {
        private static GradientBoostingRegressorExplain Explain()
        {
            return new GradientBoostingRegressorExplain
            {
                PredictedValue = 1.9,
                BaseValue = -0.4,
                Contributions =
                [
                    new GradientBoostingRegressorContribution { Name = "Amount", Weight = 1.6 },
                    new GradientBoostingRegressorContribution { Name = "Country", Weight = 0.7 }
                ]
            };
        }

        [Fact]
        public void ToAdaptation_IsAssignableToTheSharedExplanationInterface()
        {
            IAdvancedAnalyticsExplanation explanation = Explain();

            explanation.Should().NotBeNull();
        }

        [Fact]
        public void ToAdaptation_CarriesTheValueAndModelNameAndFamily()
        {
            var adaptation = Explain().ToAdaptation(1.9, "MyGradientBoostingRegressorModel");

            adaptation.Value.Should().Be(1.9);
            adaptation.Model!.Name.Should().Be("MyGradientBoostingRegressorModel");
            adaptation.Model.Family.Should().Be(ProtocolConstants.Family.XgBoost);
            adaptation.Model.FeatureCount.Should().Be(2);
        }

        [Fact]
        public void ToAdaptation_ContributionIsExactAndInScoreSpaceWithTheTreePathContributionMethod()
        {
            var adaptation = Explain().ToAdaptation(1.9, "MyGradientBoostingRegressorModel");

            adaptation.Contribution!.Space.Should().Be(ProtocolConstants.ValueSpace.Score);
            adaptation.Contribution.Method.Should().Be(ProtocolConstants.ContributionMethod.TreePathContribution);
            adaptation.Contribution.Exact.Should().BeTrue();
            adaptation.Contribution.BaseValue.Should().Be(-0.4);
            adaptation.Contribution.Items.Sum(i => i.Weight!.Value).Should().Be(2.3);

            (adaptation.Contribution.BaseValue!.Value + adaptation.Contribution.Items.Sum(i => i.Weight!.Value))
                .Should().BeApproximately(1.9, 1e-12);
        }

        [Fact]
        public void ToAdaptation_IsNeverCalibrated()
        {
            var adaptation = Explain().ToAdaptation(1.9, "M");

            adaptation.Calibration!.Calibrated.Should().BeFalse();
            adaptation.Calibration.Method.Should().Be(ProtocolConstants.CalibrationMethod.None);
            adaptation.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Score);
        }

        [Fact]
        public void ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven()
        {
            var adaptation = Explain().ToAdaptation(1.9, "M", [1, 3]);

            adaptation.Contribution!.Items[0].Source.Should().Be(ProtocolConstants.Source.Payload);
            adaptation.Contribution.Items[1].Source.Should().Be(ProtocolConstants.Source.TtlCounter);
        }

        [Fact]
        public void ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven()
        {
            var adaptation = Explain().ToAdaptation(1.9, "M");

            adaptation.Contribution!.Items.Should().OnlyContain(i => i.Source == null);
        }

        [Fact]
        [Trait("Covers", "GradientBoostingRegressorExactness")]
        public void ToAdaptation_ProvesExactnessAcrossManyRealFitsThroughTheRecallAndAdaptationRoundTrip()
        {
            var data = LinearSliceData.Continuous(400, 909, 0.1, [1.2, -0.9, 0.4], 1);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new GradientBoostingRegressorOptions
                    { Rounds = 50, MaxDepth = 3, LearningRate = 0.12, MinimumLeafSize = 5, Seed = 909 });

            var recall = GradientBoostingRegressorRecall.FromSnapshot(result.SnapshotJson);
            var processingTypeIds = data.Names.Select(_ => 1).ToArray();

            var random = new Random(4041);

            for (var i = 0; i < 100; i++)
            {
                var row = new double[data.Names.Length];

                for (var j = 0; j < row.Length; j++)
                {
                    row[j] = (random.NextDouble() - 0.5) * 8d;
                }

                var explain = recall.Explain(row);
                var adaptation = explain.ToAdaptation(recall.Predict(row)[0], "Probe", processingTypeIds);

                adaptation.Contribution!.Exact.Should().BeTrue();

                var reconstructed = adaptation.Contribution.BaseValue!.Value +
                                    adaptation.Contribution.Items.Sum(item => item.Weight!.Value);

                reconstructed.Should().BeApproximately(explain.PredictedValue, 1e-9);
            }
        }
    }
}