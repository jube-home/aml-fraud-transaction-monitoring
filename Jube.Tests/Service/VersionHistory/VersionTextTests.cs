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

namespace Jube.Test.Service.VersionHistory
{
    using FluentAssertions;
    using Jube.Data.Query;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class VersionTextTests
    {
        [Theory]
        [InlineData("Return a / b", "Return a / b")]
        [InlineData("Return a / b\r\n(c _\r\n+ d)", "Return a / b\n(c _\n+ d)")]
        [InlineData("Return a / b\n(c _  \n+ d) _ \n", "Return a / b\n(c _\n+ d) _")]
        [InlineData("   Return a\n\n\n    (b)   ", "Return a\n(b)")]
        [InlineData("﻿Return a", "Return a")]
        [InlineData("Return a ", "Return a")]
        public void NormaliseStripsLineEndingsIndentationAndBlankLines(string text, string expected)
        {
            VersionText.Normalise(text).Should().Be(expected);
        }

        [Fact]
        public void NormaliseKeepsSpacingInsideALine()
        {
            VersionText.Normalise("Return \"a  b\"").Should().Be("Return \"a  b\"");
        }

        [Theory]
        [InlineData("Return a\r\n(b)", "Return a\n(b)")]
        [InlineData("Return a / \n (b _  \n+ c) _ \n", "Return a /\n(b _\n+ c) _")]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void TextThatDiffersOnlyInLayoutMatches(string? from, string? to)
        {
            VersionText.Matches(from, to).Should().BeTrue();
        }

        [Fact]
        public void NullAndEmptyTextMatch()
        {
            VersionText.Matches(null, "").Should().BeTrue();
        }

        [Fact]
        public void ATokenChangeDoesNotMatch()
        {
            VersionText.Matches("Return a / b", "Return a * b").Should().BeFalse();
        }

        [Fact]
        public void TextInsideAStringLiteralStillCountsAsAChange()
        {
            VersionText.Matches("Return \"a b\"", "Return \"a  b\"").Should().BeFalse();
        }

        [Fact]
        public void NonTextValuesMatchOnEquality()
        {
            VersionText.Matches(1, 1).Should().BeTrue();
            VersionText.Matches(1, 2).Should().BeFalse();
            VersionText.Matches(null, 0).Should().BeFalse();
        }

        [Fact]
        public void ChangeKindIsAddedRemovedOrChanged()
        {
            VersionText.ChangeKind(null, "x").Should().Be("Added");
            VersionText.ChangeKind("x", null).Should().Be("Removed");
            VersionText.ChangeKind("x", "y").Should().Be("Changed");
        }

        [Fact]
        public void PreviewKeepsTheFirstTwoHundredCharactersThenAnEllipsis()
        {
            var text = new string('a', 200) + "tail";

            VersionText.Preview(text).Should().Be(new string('a', 200) + "...");
        }

        [Fact]
        public void PreviewLeavesTextOfTwoHundredCharactersOrFewerWhole()
        {
            var text = new string('b', 200);

            VersionText.Preview(text).Should().Be(text);
        }

        [Fact]
        public void PreviewRendersNonTextValues()
        {
            VersionText.Preview(true).Should().Be("true");
            VersionText.Preview(false).Should().Be("false");
            VersionText.Preview(42).Should().Be("42");
            VersionText.Preview(null).Should().Be("");
        }

        [Fact]
        public void ShortenLeavesNonTextValuesAlone()
        {
            VersionText.Shorten(7).Should().Be(7);
        }
    }
}