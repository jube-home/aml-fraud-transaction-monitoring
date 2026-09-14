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
using Jube.HttpAdaptationProtocol;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol
{
    [Trait("Category", "Unit")]
    public sealed class ProtocolConstantsTests
    {
        [Fact]
        public void CurrentProtocolVersionIsOneDotOne()
        {
            ProtocolConstants.CurrentProtocolVersion.Should().Be("1.1");
        }

        [Fact]
        public void FamilyConstantsHaveExpectedValues()
        {
            ProtocolConstants.Family.Glm.Should().Be("GLM");
            ProtocolConstants.Family.RandomForest.Should().Be("RandomForest");
            ProtocolConstants.Family.C5.Should().Be("C5");
            ProtocolConstants.Family.XgBoost.Should().Be("XGBoost");
            ProtocolConstants.Family.Svm.Should().Be("SVM");
            ProtocolConstants.Family.BayesianNetwork.Should().Be("BayesianNetwork");
            ProtocolConstants.Family.NeuralNetwork.Should().Be("NeuralNetwork");
            ProtocolConstants.Family.ExpertRule.Should().Be("ExpertRule");
        }

        [Fact]
        public void ContributionSpaceConstantsHaveExpectedValues()
        {
            ProtocolConstants.ContributionSpace.Relative.Should().Be("Relative");
        }

        [Fact]
        public void ContributionMethodConstantsHaveExpectedValues()
        {
            ProtocolConstants.ContributionMethod.Coefficient.Should().Be("Coefficient");
            ProtocolConstants.ContributionMethod.BootstrapStrength.Should().Be("BootstrapStrength");
            ProtocolConstants.ContributionMethod.ArcStrength.Should().Be("ArcStrength");
            ProtocolConstants.ContributionMethod.ConnectionWeight.Should().Be("ConnectionWeight");
        }

        [Fact]
        public void ValueSpaceConstantsHaveExpectedValues()
        {
            ProtocolConstants.ValueSpace.Probability.Should().Be("Probability");
            ProtocolConstants.ValueSpace.LogOdds.Should().Be("LogOdds");
            ProtocolConstants.ValueSpace.DecisionFunction.Should().Be("DecisionFunction");
            ProtocolConstants.ValueSpace.VoteFraction.Should().Be("VoteFraction");
            ProtocolConstants.ValueSpace.Score.Should().Be("Score");
        }

        [Fact]
        public void CalibrationMethodConstantsHaveExpectedValues()
        {
            ProtocolConstants.CalibrationMethod.None.Should().Be("None");
            ProtocolConstants.CalibrationMethod.Native.Should().Be("Native");
            ProtocolConstants.CalibrationMethod.Platt.Should().Be("Platt");
            ProtocolConstants.CalibrationMethod.Isotonic.Should().Be("Isotonic");
            ProtocolConstants.CalibrationMethod.Beta.Should().Be("Beta");
        }

        [Fact]
        public void StructureLearningConstantsHaveExpectedValues()
        {
            ProtocolConstants.StructureLearning.HillClimbing.Should().Be("HillClimbing");
            ProtocolConstants.StructureLearning.Mmhc.Should().Be("MMHC");
            ProtocolConstants.StructureLearning.TabuSearch.Should().Be("TabuSearch");
            ProtocolConstants.StructureLearning.Expert.Should().Be("Expert");
            ProtocolConstants.StructureLearning.Constrained.Should().Be("Constrained");
            ProtocolConstants.StructureLearning.None.Should().Be("None");
        }

        [Fact]
        public void SourceConstantsHaveExpectedValues()
        {
            ProtocolConstants.Source.Payload.Should().Be("Payload");
            ProtocolConstants.Source.Abstraction.Should().Be("Abstraction");
            ProtocolConstants.Source.AbstractionCalculation.Should().Be("AbstractionCalculation");
            ProtocolConstants.Source.TtlCounter.Should().Be("TtlCounter");
            ProtocolConstants.Source.Dictionary.Should().Be("Dictionary");
            ProtocolConstants.Source.Sanction.Should().Be("Sanction");
        }
    }
}