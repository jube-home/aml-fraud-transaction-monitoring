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
using Jube.Service.Extensions.TextCleaning;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class TextCleaningToolsTests
    {
        [Fact]
        [Trait("Covers", "RemoveAccents")]
        [Trait("Covers", "CollapseWhitespace")]
        [Trait("Covers", "RemoveControlCharacters")]
        [Trait("Covers", "NormaliseUnicode")]
        public void Normalisation()
        {
            TextCleaningTools.RemoveAccents("Crème Brûlée").Should().Be("Creme Brulee");
            TextCleaningTools.CollapseWhitespace("  a \t b ").Should().Be("a b");
            TextCleaningTools.RemoveControlCharacters("a\u0000b").Should().Be("ab");
            TextCleaningTools.NormaliseUnicode("\uFF21\uFF22\uFF23", "NFKC").Should().Be("ABC");
            var act = () => TextCleaningTools.NormaliseUnicode("x", "NFZ");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "KeepOnlyDigits")]
        [Trait("Covers", "KeepOnlyLetters")]
        [Trait("Covers", "RemoveDigits")]
        [Trait("Covers", "RemoveLetters")]
        [Trait("Covers", "ToSlug")]
        public void Filters()
        {
            TextCleaningTools.KeepOnlyDigits("+44 (0) 7911-123456").Should().Be("4407911123456");
            TextCleaningTools.KeepOnlyLetters("AB-12 cd").Should().Be("ABcd");
            TextCleaningTools.RemoveDigits("AB-12").Should().Be("AB-");
            TextCleaningTools.RemoveLetters("AB-12").Should().Be("-12");
            TextCleaningTools.ToSlug("Crème Brûlée & Co.!").Should().Be("creme-brulee-co");
        }
    }
}