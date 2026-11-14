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

namespace Jube.Test.Exhaustive
{
    using System.Collections.Generic;
    using FluentAssertions;
    using Jube.Engine.Exhaustive;
    using Jube.Engine.Exhaustive.Preparation;
    using Jube.Test.Infrastructure;
    using Xunit;

    public class ExhaustiveZScoreClipBoundResolverTests
    {
        [Fact]
        public void Resolve_NullEnvironment_FallsBackToTheDefaultBound()
        {
            ExhaustiveZScoreClipBoundResolver.Resolve(null).Should().Be(5);
            ExhaustiveZScoreClipBoundResolver.Resolve(null).Should().Be(ExhaustiveZScoreClipBoundResolver.DefaultBound);
        }

        [Fact]
        public void Resolve_UsesTheCodeDefaultFromANewlyConstructedEnvironment()
        {
            var environment = TestDynamicEnvironment.Create();

            ExhaustiveZScoreClipBoundResolver.Resolve(environment).Should().Be(5);
        }

        [Fact]
        public void Resolve_HonoursAConfiguredOverride()
        {
            var environment = TestDynamicEnvironment.Create(
                new Dictionary<string, string> { ["ExhaustiveZScoreClipBound"] = "3.5" });

            ExhaustiveZScoreClipBoundResolver.Resolve(environment).Should().Be(3.5);
        }

        [Fact]
        public void Resolve_UnparsableOverride_FallsBackToTheDefaultBound()
        {
            var environment = TestDynamicEnvironment.Create(
                new Dictionary<string, string> { ["ExhaustiveZScoreClipBound"] = "not-a-number" });

            ExhaustiveZScoreClipBoundResolver.Resolve(environment).Should().Be(5);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-3")]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        public void Resolve_ANonPositiveOrNonFiniteOverride_FallsBackToTheDefaultBound(string configured)
        {
            var environment = TestDynamicEnvironment.Create(
                new Dictionary<string, string> { ["ExhaustiveZScoreClipBound"] = configured });

            ExhaustiveZScoreClipBoundResolver.Resolve(environment).Should().Be(5);
        }

        [Fact]
        public void ZScore_AnIndicatorWithinTheBoundIsNotMovedByTheClamp()
        {
            var variable = new Jube.Engine.Exhaustive.Models.ExhaustiveSearchInstancePromotedTrialInstanceVariable
            {
                Mean = 0.5, Sd = 0.5, NormalisationTypeId = 2, ZScoreClipBound = 5
            };

            variable.ZScore(1).Should().Be(1);
            variable.ZScore(0).Should().Be(-1);
        }

        [Theory]
        [InlineData(3, 5, 3)]
        [InlineData(-3, 5, -3)]
        [InlineData(0, 5, 0)]
        [InlineData(5, 5, 5)]
        [InlineData(-5, 5, -5)]
        [InlineData(50, 5, 5)]
        [InlineData(-50, 5, -5)]
        public void Clamp_BoundsToTheSymmetricRange(double value, double bound, double expected)
        {
            ExhaustiveZScoreClipBoundResolver.Clamp(value, bound).Should().Be(expected);
        }

        [Theory]
        [InlineData(12, 10, 2, 2, 1)]
        [InlineData(8, 10, 2, 2, -1)]
        [InlineData(110, 10, 2, 2, 5)]
        [InlineData(-90, 10, 2, 2, -5)]
        [InlineData(110, 10, 2, 0, 110)]
        [InlineData(110, 10, 2, 1, 110)]
        public void StaticZScore_AppliesTheClampOnlyWhenStandardised(double value, double mean, double sd,
            int normalisationTypeId, double expected)
        {
            Normalisation.ZScore(value, mean, sd, normalisationTypeId).Should().Be(expected);
        }

        [Fact]
        public void StaticZScore_HonoursAnExplicitClipBound()
        {
            Normalisation.ZScore(110, 10, 2, 2, 3).Should().Be(3);
            Normalisation.ZScore(-90, 10, 2, 2, 3).Should().Be(-3);
        }
    }
}
