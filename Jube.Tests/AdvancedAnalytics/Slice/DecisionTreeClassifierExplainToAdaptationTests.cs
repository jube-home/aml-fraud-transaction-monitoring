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
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.HttpAdaptationProtocol;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class DecisionTreeClassifierExplainToAdaptationTests
    {
        private static DecisionTreeClassifierExplain Explain(bool calibrated = false)
        {
            return new DecisionTreeClassifierExplain
            {
                RootProbability = 0.3,
                LeafProbability = 0.8,
                Contributions =
                [
                    new DecisionTreeContribution
                    {
                        Name = "Amount", Weight = 0.35, Depth = 0, Threshold = 100d, InputValue = 250d, WentRight = true
                    },
                    new DecisionTreeContribution
                    {
                        Name = "Country", Weight = 0.15, Depth = 1, Threshold = 0.5, InputValue = 1d, WentRight = true
                    }
                ],
                Calibrated = calibrated,
                CalibrationSlope = calibrated ? 1.1 : null,
                CalibrationIntercept = calibrated ? -0.2 : null
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
            var adaptation = Explain().ToAdaptation(0.8, "MyTreeModel");

            adaptation.Value.Should().Be(0.8);
            adaptation.Model!.Name.Should().Be("MyTreeModel");
            adaptation.Model.Family.Should().Be(ProtocolConstants.Family.C5);
            adaptation.Model.FeatureCount.Should().Be(2);
        }

        [Fact]
        public void ToAdaptation_ContributionIsInexactAndInProbabilitySpaceWithTheDecisionPathMethod()
        {
            var adaptation = Explain().ToAdaptation(0.8, "MyTreeModel");

            adaptation.Contribution!.Space.Should().Be(ProtocolConstants.ValueSpace.Probability);
            adaptation.Contribution.Method.Should().Be(ProtocolConstants.ContributionMethod.DecisionPath);
            adaptation.Contribution.Exact.Should().BeFalse();
            adaptation.Contribution.BaseValue.Should().Be(0.3);
            adaptation.Contribution.Items.Sum(i => i.Weight!.Value).Should().BeApproximately(0.5, 1e-9);
        }

        [Fact]
        public void ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndAlwaysUsesProbabilitySpace()
        {
            var uncalibrated = Explain().ToAdaptation(0.8, "M");
            var calibrated = Explain(true).ToAdaptation(0.7, "M");

            uncalibrated.Calibration!.Calibrated.Should().BeFalse();
            uncalibrated.Calibration.Method.Should().Be(ProtocolConstants.CalibrationMethod.None);
            uncalibrated.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Probability);

            calibrated.Calibration!.Calibrated.Should().BeTrue();
            calibrated.Calibration.Method.Should().Be(DecisionTreeClassifierRecall.CalibratorMethod);
            calibrated.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Probability);
            calibrated.Calibration.Slope.Should().Be(1.1);
            calibrated.Calibration.Intercept.Should().Be(-0.2);
        }

        [Fact]
        public void ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven()
        {
            var adaptation = Explain().ToAdaptation(0.8, "M", [1, 3]);

            adaptation.Contribution!.Items[0].Source.Should().Be(ProtocolConstants.Source.Payload);
            adaptation.Contribution.Items[1].Source.Should().Be(ProtocolConstants.Source.TtlCounter);
        }

        [Fact]
        public void ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven()
        {
            var adaptation = Explain().ToAdaptation(0.8, "M");

            adaptation.Contribution!.Items.Should().OnlyContain(i => i.Source == null);
        }
    }
}