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
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class DoubleCalculationCollisionTests
    {
        private static readonly string[] calculations =
        [
            "RatioOf", "PercentOf", "ShareOfSumWith", "Complement", "LogRatioOf", "RatioOfRatios", "InverseRatioOf",
            "DifferenceFrom", "AbsoluteDifferenceFrom", "PercentDifferenceFrom", "RelativeChangeFrom", "GrowthFactorFrom",
            "SlopeFrom", "CompoundGrowthRate", "PercentDeviationFromMean", "ModifiedZScore", "CoefficientOfVariation",
            "MinMaxNormalise", "PercentOfRange", "SumWith", "MeanWith", "MaxWith", "MinWith", "SpreadWith",
            "WeightedMeanWith", "GeometricMeanWith", "HarmonicMeanWith", "ShareOfMax", "HerfindahlWith",
            "EntropyOfSharesWith", "HeadroomTo", "ExcessOver", "ShortfallBelow", "DistanceToThreshold",
            "PercentBelowThreshold", "ImbalanceWith", "RetentionAfter"
        ];

        [Fact]
        public void NoCalculationNameIsAnInstanceOrStaticMemberOfDoubleWhichVbWouldBindFirst()
        {
            var collisions = calculations
                .Where(n => typeof(double).GetMember(n, BindingFlags.Public | BindingFlags.Instance |
                                                        BindingFlags.Static).Length > 0)
                .ToList();

            collisions.Should().BeEmpty();
        }

        [Fact]
        public void EveryCalculationIsAnExtensionOnDoubleReturningADouble()
        {
            var extension = typeof(Jube.Dictionary.Extensions.Extensions);

            foreach (var name in calculations)
            {
                var method = extension.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == name && m.GetParameters()[0].ParameterType == typeof(double));

                method.ReturnType.Should().Be(typeof(double), name);
            }
        }

        [Fact]
        public void EveryCalculationHasAboveBelowInRangeAndOutsideRangeComparisonSteps()
        {
            var names = typeof(Jube.Dictionary.Extensions.Extensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(m => m.Name).ToHashSet();

            foreach (var calculation in calculations)
            {
                foreach (var prefix in new[] { "Match", "Reject", "Break", "Require", "Ensure" })
                {
                    foreach (var suffix in new[] { "Above", "Below", "InRange", "OutsideRange" })
                    {
                        names.Should().Contain(prefix + calculation + suffix);
                    }
                }
            }
        }
    }
}
