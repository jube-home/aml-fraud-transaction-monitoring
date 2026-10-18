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
using Jube.Service.Extensions.TextEditing;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class TextEditingToolsTests
    {
        [Fact]
        [Trait("Covers", "ToTitleCase")]
        [Trait("Covers", "ReverseText")]
        [Trait("Covers", "RepeatText")]
        public void CaseReverseRepeat()
        {
            TextEditingTools.ToTitleCase("john SMITH").Should().Be("John SMITH");
            TextEditingTools.ReverseText("abc").Should().Be("cba");
            TextEditingTools.RepeatText("ab", 3).Should().Be("ababab");
            var huge = () => TextEditingTools.RepeatText("ab", 60_000);
            huge.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "TruncateText")]
        [Trait("Covers", "TakeFirstCharacters")]
        [Trait("Covers", "TakeLastCharacters")]
        [Trait("Covers", "MaskAllButLast")]
        public void Slicing()
        {
            TextEditingTools.TruncateText("Hello world", 8).Should().Be("Hello...");
            TextEditingTools.TruncateText("Hi", 8).Should().Be("Hi");
            TextEditingTools.TakeFirstCharacters("GB82WEST", 4).Should().Be("GB82");
            TextEditingTools.TakeLastCharacters("4111111111111111", 4).Should().Be("1111");
            TextEditingTools.TakeLastCharacters("12", 4).Should().Be("12");
            TextEditingTools.MaskAllButLast("4111111111111111").Should().Be("************1111");
            var tooShort = () => TextEditingTools.TruncateText("Hello", 2);
            tooShort.Should().Throw<ArgumentException>();
            var badMask = () => TextEditingTools.MaskAllButLast("x", 1, "##");
            badMask.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ReplaceFirstOccurrence")]
        [Trait("Covers", "ReplaceLastOccurrence")]
        public void Replacement()
        {
            TextEditingTools.ReplaceFirstOccurrence("a-b-c", "-", "+").Should().Be("a+b-c");
            TextEditingTools.ReplaceLastOccurrence("a-b-c", "-", "+").Should().Be("a-b+c");
            var act = () => TextEditingTools.ReplaceFirstOccurrence("abc", "", "x");
            act.Should().Throw<ArgumentException>();
        }
    }
}