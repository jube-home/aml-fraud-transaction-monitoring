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

using System;
using FluentAssertions;
using Jube.Service.Extensions.ListScreening;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class ListScreeningToolsTests
    {
        private static readonly string[] watchList = ["Ivan Petrov", "Acme Trading LLC", "Maria Garcia"];

        [Fact]
        [Trait("Covers", "ScreenAgainstList")]
        public void ScreenAgainstListReportsTheBestMatch()
        {
            var result = ListScreeningTools.ScreenAgainstList("Ivan Petrof", watchList);
            result.Matched.Should().BeTrue();
            result.MatchCount.Should().Be(1);
            result.BestMatch.Should().Be("Ivan Petrov");
            result.BestScore.Should().BeGreaterThan(0.9);
            ListScreeningTools.ScreenAgainstList("Nobody Known", watchList).Matched.Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "IsFuzzilyInList")]
        [Trait("Covers", "ContainsFuzzilyAnyInList")]
        [Trait("Covers", "IsExactlyInList")]
        public void MembershipChecks()
        {
            ListScreeningTools.IsFuzzilyInList("ACME TRADING", watchList, "tokenset,nosuffixes").Should().BeTrue();

            ListScreeningTools.ContainsFuzzilyAnyInList("payment to maria garcia for invoice 7", watchList).Should()
                .BeTrue();

            ListScreeningTools.IsExactlyInList("maria garcia", watchList, true).Should().BeTrue();
            ListScreeningTools.IsExactlyInList("maria garcia", watchList).Should().BeFalse();
            var tooMany = () => ListScreeningTools.IsExactlyInList("x", new string[10_001]);
            tooMany.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "InitialsMatchInList")]
        [Trait("Covers", "SoundsLikeAnyInList")]
        [Trait("Covers", "CheckFuzzyOptions")]
        public void InitialsPhoneticsAndOptions()
        {
            ListScreeningTools.InitialsMatchInList("I Petrov", watchList).Should().BeTrue();
            ListScreeningTools.SoundsLikeAnyInList("Mariah Garsia", watchList).Should().BeTrue();
            ListScreeningTools.CheckFuzzyOptions("jarowinkler=0.9,notitles").Should().BeEmpty();
            ListScreeningTools.CheckFuzzyOptions("bogus").Should().NotBeEmpty();
        }
    }
}