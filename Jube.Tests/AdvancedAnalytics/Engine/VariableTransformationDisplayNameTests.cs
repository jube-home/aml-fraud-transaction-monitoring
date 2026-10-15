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
using Jube.Engine.AdvancedAnalytics;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Models;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    [Trait("Category", "Unit")]
    public sealed class VariableTransformationDisplayNameTests
    {
        [Theory]
        [InlineData(VariableTransformationKind.Log, "AmountLog")]
        [InlineData(VariableTransformationKind.Log10, "AmountLog10")]
        [InlineData(VariableTransformationKind.Log2, "AmountLog2")]
        [InlineData(VariableTransformationKind.Log1P, "AmountLog1P")]
        [InlineData(VariableTransformationKind.SignedLog1P, "AmountSignedLog1P")]
        [InlineData(VariableTransformationKind.Sqrt, "AmountSqrt")]
        [InlineData(VariableTransformationKind.SignedSqrt, "AmountSignedSqrt")]
        [InlineData(VariableTransformationKind.CubeRoot, "AmountCubeRoot")]
        [InlineData(VariableTransformationKind.Reciprocal, "AmountReciprocal")]
        [InlineData(VariableTransformationKind.Square, "AmountSquare")]
        [InlineData(VariableTransformationKind.AbsoluteValue, "AmountAbs")]
        [InlineData(VariableTransformationKind.BoxCox, "AmountBoxCox")]
        [InlineData(VariableTransformationKind.YeoJohnson, "AmountYeoJohnson")]
        [InlineData(VariableTransformationKind.ZScoreStandard, "AmountZScore")]
        [InlineData(VariableTransformationKind.MinMax, "AmountMinMax")]
        [InlineData(VariableTransformationKind.RobustMad, "AmountRobustMad")]
        [InlineData(VariableTransformationKind.RobustIqr, "AmountRobustIqr")]
        [InlineData(VariableTransformationKind.MaxAbs, "AmountMaxAbs")]
        [InlineData(VariableTransformationKind.QuantileUniform, "AmountQuantileUniform")]
        [InlineData(VariableTransformationKind.QuantileNormal, "AmountQuantileNormal")]
        [InlineData(VariableTransformationKind.Winsorise, "AmountWinsor")]
        [InlineData(VariableTransformationKind.DiscretiseEqualWidth, "AmountBinEW")]
        [InlineData(VariableTransformationKind.DiscretiseEqualFrequency, "AmountBinEF")]
        [InlineData(VariableTransformationKind.DiscretiseKMeans, "AmountBinKMeans")]
        [InlineData(VariableTransformationKind.DiscretiseSupervised, "AmountBinSupervised")]
        [InlineData(VariableTransformationKind.DiscretiseCustomEdges, "AmountBinCustom")]
        [InlineData(VariableTransformationKind.LabelOrdinal, "AmountOrdinal")]
        [InlineData(VariableTransformationKind.TargetMean, "AmountTargetMean")]
        [InlineData(VariableTransformationKind.WeightOfEvidence, "AmountWoE")]
        [InlineData(VariableTransformationKind.Frequency, "AmountFreq")]
        public void EachTransformationKindAppendsItsOwnFixedAbbreviation(VariableTransformationKind kind,
            string expected)
        {
            VariableTransformationDisplayName.DisplayName("Amount", kind).Should().Be(expected);
        }

        [Fact]
        public void NoneLeavesTheNameUnchanged()
        {
            VariableTransformationDisplayName.DisplayName("Amount", VariableTransformationKind.None)
                .Should().Be("Amount");
        }

        [Fact]
        public void CyclicalSinAndCosProduceASinCosPairFromTheSameBaseName()
        {
            VariableTransformationDisplayName.DisplayName("Hour", VariableTransformationKind.CyclicalSin)
                .Should().Be("HourSin");
            VariableTransformationDisplayName.DisplayName("Hour", VariableTransformationKind.CyclicalCos)
                .Should().Be("HourCos");
        }

        [Fact]
        public void OneHotIndicatorLeavesTheNameUnchangedBecauseItAlreadyCarriesItsOwnCategorySuffix()
        {
            VariableTransformationDisplayName.DisplayName("Country=US", VariableTransformationKind.OneHotIndicator)
                .Should().Be("Country=US");
        }

        [Fact]
        public void DifferentSourceVariablesWithTheSameTransformationNeverCollide()
        {
            var first = VariableTransformationDisplayName.DisplayName("Amount", VariableTransformationKind.Log);
            var second = VariableTransformationDisplayName.DisplayName("Balance", VariableTransformationKind.Log);

            first.Should().NotBe(second);
        }
    }
}