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
using Jube.Engine.AdvancedAnalytics.Density;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.HttpAdaptationProtocol;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Density.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class GaussianMixtureDensityExplainToAdaptationTests
    {
        private static GaussianMixtureDensityExplain Explain()
        {
            return new GaussianMixtureDensityExplain
            {
                LogDensity = -4.1,
                DominantComponent = 1,
                BaseValue = -1.5,
                Contributions =
                [
                    new GaussianMixtureDensityContribution { Name = "Amount", Weight = -0.9 },
                    new GaussianMixtureDensityContribution { Name = "Country", Weight = -1.7 }
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
            var adaptation = Explain().ToAdaptation(-4.1, "MyDensityModel");

            adaptation.Value.Should().Be(-4.1);
            adaptation.Model!.Name.Should().Be("MyDensityModel");
            adaptation.Model.Family.Should().Be(ProtocolConstants.Family.Gaussian);
            adaptation.Model.FeatureCount.Should().Be(2);
        }

        [Fact]
        public void ToAdaptation_ContributionIsInexactAndInTheLogLikelihoodSpaceWithTheMahalanobisTermMethod()
        {
            var adaptation = Explain().ToAdaptation(-4.1, "MyDensityModel");

            adaptation.Contribution!.Space.Should().Be(ProtocolConstants.ValueSpace.LogLikelihood);
            adaptation.Contribution.Method.Should().Be(ProtocolConstants.ContributionMethod.MahalanobisTerm);
            adaptation.Contribution.Exact.Should().BeFalse();
            adaptation.Contribution.BaseValue.Should().Be(-1.5);
            adaptation.Contribution.Items.Sum(i => i.Weight!.Value).Should().Be(-2.6);
        }

        [Fact]
        public void ToAdaptation_IsNeverCalibratedAndUsesLogLikelihoodSpace()
        {
            var adaptation = Explain().ToAdaptation(-4.1, "M");

            adaptation.Calibration!.Calibrated.Should().BeFalse();
            adaptation.Calibration.Method.Should().Be(ProtocolConstants.CalibrationMethod.None);
            adaptation.Calibration.Space.Should().Be(ProtocolConstants.ValueSpace.LogLikelihood);
            adaptation.Calibration.Slope.Should().BeNull();
            adaptation.Calibration.Intercept.Should().BeNull();
        }

        [Fact]
        public void ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven()
        {
            var adaptation = Explain().ToAdaptation(-4.1, "M", [1, 3]);

            adaptation.Contribution!.Items[0].Source.Should().Be(ProtocolConstants.Source.Payload);
            adaptation.Contribution.Items[1].Source.Should().Be(ProtocolConstants.Source.TtlCounter);
        }

        [Fact]
        public void ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven()
        {
            var adaptation = Explain().ToAdaptation(-4.1, "M");

            adaptation.Contribution!.Items.Should().OnlyContain(i => i.Source == null);
        }
    }
}