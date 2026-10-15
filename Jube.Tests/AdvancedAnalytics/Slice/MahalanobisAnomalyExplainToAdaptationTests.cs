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
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.HttpAdaptationProtocol;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class MahalanobisAnomalyExplainToAdaptationTests
    {
        private static MahalanobisAnomalyExplain Explain(bool calibrated = false)
        {
            return new MahalanobisAnomalyExplain
            {
                Distance = 3.2,
                Contributions =
                [
                    new MahalanobisAnomalyContribution { Name = "Amount", Weight = 2.5 },
                    new MahalanobisAnomalyContribution { Name = "Country", Weight = 0.9 }
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
            var adaptation = Explain().ToAdaptation(4.1, "MyAnomalyModel");

            adaptation.Value.Should().Be(4.1);
            adaptation.Model!.Name.Should().Be("MyAnomalyModel");
            adaptation.Model.Family.Should().Be(ProtocolConstants.Family.Gaussian);
            adaptation.Model.FeatureCount.Should().Be(2);
        }

        [Fact]
        public void ToAdaptation_ContributionIsInexactAndInTheDistanceSpaceWithTheMahalanobisTermMethod()
        {
            var adaptation = Explain().ToAdaptation(4.1, "MyAnomalyModel");

            adaptation.Contribution!.Space.Should().Be(ProtocolConstants.ValueSpace.Distance);
            adaptation.Contribution.Method.Should().Be(ProtocolConstants.ContributionMethod.MahalanobisTerm);
            adaptation.Contribution.Exact.Should().BeFalse();
            adaptation.Contribution.BaseValue.Should().Be(0d);
            adaptation.Contribution.Items.Sum(i => i.Weight!.Value).Should().Be(3.4);
        }

        [Fact]
        public void ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesDistanceSpaceOtherwise()
        {
            var uncalibrated = Explain().ToAdaptation(3.2, "M");
            var calibrated = Explain(true).ToAdaptation(0.7, "M");

            uncalibrated.Calibration!.Calibrated.Should().BeFalse();
            uncalibrated.Calibration.Method.Should().Be(ProtocolConstants.CalibrationMethod.None);
            uncalibrated.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Distance);

            calibrated.Calibration!.Calibrated.Should().BeTrue();
            calibrated.Calibration.Method.Should().Be(MahalanobisAnomalyRecall.CalibratorMethod);
            calibrated.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Probability);
            calibrated.Calibration.Slope.Should().Be(1.1);
            calibrated.Calibration.Intercept.Should().Be(-0.2);
        }

        [Fact]
        public void ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven()
        {
            var adaptation = Explain().ToAdaptation(3.2, "M", [1, 3]);

            adaptation.Contribution!.Items[0].Source.Should().Be(ProtocolConstants.Source.Payload);
            adaptation.Contribution.Items[1].Source.Should().Be(ProtocolConstants.Source.TtlCounter);
        }

        [Fact]
        public void ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven()
        {
            var adaptation = Explain().ToAdaptation(3.2, "M");

            adaptation.Contribution!.Items.Should().OnlyContain(i => i.Source == null);
        }
    }
}