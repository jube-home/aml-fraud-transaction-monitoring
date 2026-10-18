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
using Jube.Service.Extensions.TextInspection;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class TextInspectionToolsTests
    {
        [Fact]
        [Trait("Covers", "DescribeText")]
        public void DescribeTextMeasuresEverything()
        {
            var measures = TextInspectionTools.DescribeText("aaaa1234\nxy");
            measures.Characters.Should().Be(11);
            measures.Words.Should().Be(2);
            measures.Lines.Should().Be(2);
            measures.Letters.Should().Be(6);
            measures.Digits.Should().Be(4);
            measures.LongestRepeatedCharacterRun.Should().Be(4);
            measures.IsAllAsciiDigits.Should().BeFalse();
            TextInspectionTools.DescribeText("0000").IsOneRepeatedCharacter.Should().BeTrue();
            TextInspectionTools.DescribeText("").IsAllAsciiLetters.Should().BeFalse();
            var act = () => TextInspectionTools.DescribeText(new string('x', 100_001));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CountOccurrences")]
        [Trait("Covers", "HasSequentialDigits")]
        public void CountsAndRuns()
        {
            TextInspectionTools.CountOccurrences("abababa", "aba").Should().Be(2);
            TextInspectionTools.HasSequentialDigits("pin 9876", 4).Should().BeTrue();
            TextInspectionTools.HasSequentialDigits("pin 9786", 4).Should().BeFalse();
            var act = () => TextInspectionTools.CountOccurrences("x", "");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "IsPalindrome")]
        [Trait("Covers", "AreAnagrams")]
        [Trait("Covers", "EqualsIgnoringCase")]
        public void Comparisons()
        {
            TextInspectionTools.IsPalindrome("Racecar").Should().BeTrue();
            TextInspectionTools.AreAnagrams("listen", "silent").Should().BeTrue();
            TextInspectionTools.EqualsIgnoringCase("ACME", "acme").Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "ContainsAllOf")]
        [Trait("Covers", "ContainsAnyOf")]
        public void Containment()
        {
            TextInspectionTools.ContainsAllOf("Gift card top-up", ["gift", "card"], true).Should().BeTrue();
            TextInspectionTools.ContainsAllOf("Gift card top-up", ["gift", "card"]).Should().BeFalse();
            TextInspectionTools.ContainsAnyOf("crypto exchange", ["casino", "crypto"]).Should().BeTrue();
        }
    }
}