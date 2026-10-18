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
    public class TreeAugmentedNaiveBayesClassifierExplainToAdaptationTests
    {
        private static TreeAugmentedNaiveBayesClassifierExplain Explain(bool calibrated = false)
        {
            return new TreeAugmentedNaiveBayesClassifierExplain
            {
                RawLogOdds = 3.4,
                PriorLogOdds = 0.2,
                Contributions =
                [
                    new TreeAugmentedNaiveBayesClassifierContribution { Name = "Amount", Weight = 2.5 },
                    new TreeAugmentedNaiveBayesClassifierContribution { Name = "Country", Weight = 0.7 }
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
        public void ToAdaptation_CarriesTheValueAndModelNameAndFamilyAndChowLiuStructureLearning()
        {
            var adaptation = Explain().ToAdaptation(0.9, "MyTanModel");

            adaptation.Value.Should().Be(0.9);
            adaptation.Model!.Name.Should().Be("MyTanModel");
            adaptation.Model.Family.Should().Be(ProtocolConstants.Family.BayesianNetwork);
            adaptation.Model.StructureLearning.Should().Be(ProtocolConstants.StructureLearning.ChowLiu);
            adaptation.Model.FeatureCount.Should().Be(2);
        }

        [Fact]
        public void ToAdaptation_ContributionIsExactAndInLogOddsSpaceWithTheLogLikelihoodRatioMethod()
        {
            var adaptation = Explain().ToAdaptation(0.9, "MyTanModel");

            adaptation.Contribution!.Space.Should().Be(ProtocolConstants.ValueSpace.LogOdds);
            adaptation.Contribution.Method.Should().Be(ProtocolConstants.ContributionMethod.LogLikelihoodRatio);
            adaptation.Contribution.Exact.Should().BeTrue();
            adaptation.Contribution.BaseValue.Should().Be(0.2);
            adaptation.Contribution.Items.Sum(i => i.Weight!.Value).Should().Be(3.2);

            (adaptation.Contribution.BaseValue!.Value + adaptation.Contribution.Items.Sum(i => i.Weight!.Value))
                .Should().BeApproximately(3.4, 1e-12);
        }

        [Fact]
        public void ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesProbabilitySpaceEitherWay()
        {
            var uncalibrated = Explain().ToAdaptation(0.6, "M");
            var calibrated = Explain(true).ToAdaptation(0.7, "M");

            uncalibrated.Calibration!.Calibrated.Should().BeFalse();
            uncalibrated.Calibration.Method.Should().Be(ProtocolConstants.CalibrationMethod.None);
            uncalibrated.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Probability);

            calibrated.Calibration!.Calibrated.Should().BeTrue();
            calibrated.Calibration.Method.Should().Be(TreeAugmentedNaiveBayesClassifierRecall.CalibratorMethod);
            calibrated.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.Probability);
            calibrated.Calibration.Slope.Should().Be(1.1);
            calibrated.Calibration.Intercept.Should().Be(-0.2);
        }

        [Fact]
        public void ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven()
        {
            var adaptation = Explain().ToAdaptation(0.9, "M", [1, 3]);

            adaptation.Contribution!.Items[0].Source.Should().Be(ProtocolConstants.Source.Payload);
            adaptation.Contribution.Items[1].Source.Should().Be(ProtocolConstants.Source.TtlCounter);
        }

        [Fact]
        public void ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven()
        {
            var adaptation = Explain().ToAdaptation(0.9, "M");

            adaptation.Contribution!.Items.Should().OnlyContain(i => i.Source == null);
        }
    }
}