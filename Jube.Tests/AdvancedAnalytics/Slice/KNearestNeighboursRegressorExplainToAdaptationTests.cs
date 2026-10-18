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

using System.Linq;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.HttpAdaptationProtocol;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class KNearestNeighboursRegressorExplainToAdaptationTests
    {
        private static KNearestNeighboursRegressorExplain Explain()
        {
            return new KNearestNeighboursRegressorExplain
            {
                PredictedValue = 42d,
                BaseValue = 30d,
                Contributions =
                [
                    new KNearestNeighboursRegressorContribution { Name = "Amount", Weight = 0.8 },
                    new KNearestNeighboursRegressorContribution { Name = "Country", Weight = 0.2 }
                ],
                NeighbourTargets = [40d, 44d],
                NeighbourDistances = [0.1d, 0.2d]
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
            var adaptation = Explain().ToAdaptation(42d, "MyModel");

            adaptation.Value.Should().Be(42d);
            adaptation.Model!.Name.Should().Be("MyModel");
            adaptation.Model.Family.Should().Be(ProtocolConstants.Family.KNearestNeighbours);
            adaptation.Model.FeatureCount.Should().Be(2);
        }

        [Fact]
        public void ToAdaptation_ContributionIsInexactAndUsesTheTrainingMeanAsBaseValue()
        {
            var adaptation = Explain().ToAdaptation(42d, "MyModel");

            adaptation.Contribution!.Space.Should().Be(ProtocolConstants.ValueSpace.Score);
            adaptation.Contribution.Method.Should().Be(ProtocolConstants.ContributionMethod.NeighbourDistanceShare);
            adaptation.Contribution.Exact.Should().BeFalse();
            adaptation.Contribution.BaseValue.Should().Be(30d);
        }

        [Fact]
        public void ToAdaptation_IsNeverCalibrated()
        {
            var adaptation = Explain().ToAdaptation(42d, "M");

            adaptation.Calibration!.Calibrated.Should().BeFalse();
            adaptation.Calibration.Method.Should().Be(ProtocolConstants.CalibrationMethod.None);
            adaptation.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Score);
        }

        [Fact]
        public void ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven()
        {
            var adaptation = Explain().ToAdaptation(42d, "M", [1, 3]);

            adaptation.Contribution!.Items[0].Source.Should().Be(ProtocolConstants.Source.Payload);
            adaptation.Contribution.Items[1].Source.Should().Be(ProtocolConstants.Source.TtlCounter);
        }

        [Fact]
        public void ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven()
        {
            var adaptation = Explain().ToAdaptation(42d, "M");

            adaptation.Contribution!.Items.Should().OnlyContain(i => i.Source == null);
        }

        [Fact]
        public void ToAdaptation_NeverCarriesPerContributionSignificance()
        {
            var adaptation = Explain().ToAdaptation(42d, "M");

            adaptation.Contribution!.Items.Should().OnlyContain(i => i.Significance == null);
        }

        [Fact]
        public void ToAdaptation_ContributionWeightsSumToOne()
        {
            var adaptation = Explain().ToAdaptation(42d, "M");

            adaptation.Contribution!.Items.Sum(i => i.Weight!.Value).Should().BeApproximately(1d, 1e-9);
        }
    }
}