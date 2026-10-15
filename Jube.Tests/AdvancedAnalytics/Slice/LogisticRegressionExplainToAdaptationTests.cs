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
    public class LogisticRegressionExplainToAdaptationTests
    {
        private static LogisticRegressionExplain Explain(bool calibrated = false)
        {
            return new LogisticRegressionExplain
            {
                RawLogit = 1.5,
                Intercept = 0.5,
                Contributions =
                [
                    new LogisticRegressionContribution { Name = "Amount", Weight = 0.8, Significance = 0.03 },
                    new LogisticRegressionContribution { Name = "Country", Weight = 0.2 }
                ],
                Calibrated = calibrated,
                CalibrationSlope = calibrated ? 1.2 : null,
                CalibrationIntercept = calibrated ? -0.1 : null
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
            var adaptation = Explain().ToAdaptation(0.73, "MyModel");

            adaptation.Value.Should().Be(0.73);
            adaptation.Model!.Name.Should().Be("MyModel");
            adaptation.Model.Family.Should().Be(ProtocolConstants.Family.Glm);
            adaptation.Model.FeatureCount.Should().Be(2);
        }

        [Fact]
        public void ToAdaptation_ContributionIsExactAndReconstructsTheRawLogit()
        {
            var adaptation = Explain().ToAdaptation(0.73, "MyModel");

            adaptation.Contribution!.Space.Should().Be(ProtocolConstants.ValueSpace.LogOdds);
            adaptation.Contribution.Method.Should().Be(ProtocolConstants.ContributionMethod.Coefficient);
            adaptation.Contribution.Exact.Should().BeTrue();
            adaptation.Contribution.BaseValue.Should().Be(0.5);

            (adaptation.Contribution.Items.Sum(i => i.Weight!.Value) + adaptation.Contribution.BaseValue!.Value)
                .Should().Be(1.5);
        }

        [Fact]
        public void ToAdaptation_DescribesCalibrationOnlyWhenCalibrated()
        {
            var uncalibrated = Explain().ToAdaptation(0.5, "M");
            var calibrated = Explain(true).ToAdaptation(0.5, "M");

            uncalibrated.Calibration!.Calibrated.Should().BeFalse();
            uncalibrated.Calibration.Method.Should().Be(ProtocolConstants.CalibrationMethod.None);
            uncalibrated.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Probability);

            calibrated.Calibration!.Calibrated.Should().BeTrue();
            calibrated.Calibration.Method.Should().Be(LogisticRegressionRecall.CalibratorMethod);
            calibrated.Calibration.Slope.Should().Be(1.2);
            calibrated.Calibration.Intercept.Should().Be(-0.1);
        }

        [Fact]
        public void ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven()
        {
            var adaptation = Explain().ToAdaptation(0.5, "M", [1, 3]);

            adaptation.Contribution!.Items[0].Source.Should().Be(ProtocolConstants.Source.Payload);
            adaptation.Contribution.Items[1].Source.Should().Be(ProtocolConstants.Source.TtlCounter);
        }

        [Fact]
        public void ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven()
        {
            var adaptation = Explain().ToAdaptation(0.5, "M");

            adaptation.Contribution!.Items.Should().OnlyContain(i => i.Source == null);
        }

        [Fact]
        public void ToAdaptation_CarriesPerCoefficientSignificanceWhenPresentAndLeavesItNullOtherwise()
        {
            var adaptation = Explain().ToAdaptation(0.5, "M");

            adaptation.Contribution!.Items[0].Significance.Should().Be(0.03);
            adaptation.Contribution.Items[1].Significance.Should().BeNull();
        }
    }
}