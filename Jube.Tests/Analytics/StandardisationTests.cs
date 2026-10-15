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
using Jube.Analytics;
using Xunit;

namespace Jube.Test.Analytics
{
    public class StandardisationTests
    {
        [Theory]
        [InlineData(12, 10, 2, 1)]
        [InlineData(8, 10, 2, -1)]
        [InlineData(10, 10, 2, 0)]
        [InlineData(5, 5, 0, 0)]
        [InlineData(6, 5, 0, 0)]
        [InlineData(6, 5, double.NaN, 0)]
        [InlineData(6, 5, double.PositiveInfinity, 0)]
        [InlineData(double.NaN, 5, 2, 0)]
        [InlineData(double.PositiveInfinity, 5, 2, 0)]
        public void ZScore_KnownAnswers(double value, double mean, double sd, double expected)
        {
            Standardisation.ZScore(value, mean, sd).Should().Be(expected);
        }

        [Fact]
        public void Resolve_PresentValue_IsStandardisedAndNotImputed()
        {
            Standardisation.Resolve(14, 10, 2, true, out var imputed).Should().Be(2);
            imputed.Should().BeFalse();
        }

        [Fact]
        public void Resolve_PresentValue_IsUntouchedWhenNotStandardised()
        {
            Standardisation.Resolve(0.75, 0.5, 0.2, false, out var imputed).Should().Be(0.75);
            imputed.Should().BeFalse();
        }

        [Fact]
        public void Resolve_Missing_UsesTheMeanSoAStandardisedInputIsZero()
        {
            Standardisation.Resolve(null, 10, 2, true, out var imputed).Should().Be(0);
            imputed.Should().BeTrue();
        }

        [Fact]
        public void Resolve_Missing_UsesTheRawMeanWhenNotStandardised()
        {
            Standardisation.Resolve(null, 0.4, 0.2, false, out var imputed).Should().Be(0.4);
            imputed.Should().BeTrue();
        }

        [Fact]
        public void Resolve_NanIsTreatedAsMissing()
        {
            Standardisation.Resolve(double.NaN, 10, 2, true, out var imputed).Should().Be(0);
            imputed.Should().BeTrue();
        }
    }
}