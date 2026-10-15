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
using System.Globalization;
using FluentAssertions;
using Jube.Service.Extensions.TextExtraction;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class TextExtractionToolsTests
    {
        [Fact]
        [Trait("Covers", "TextAfter")]
        [Trait("Covers", "TextBefore")]
        [Trait("Covers", "TextBetween")]
        public void Markers()
        {
            TextExtractionTools.TextAfter("ann@example.com", "@").Should().Be("example.com");
            TextExtractionTools.TextBefore("ann@example.com", "@").Should().Be("ann");
            TextExtractionTools.TextBetween("x ref:AB12; y", "ref:", ";").Should().Be("AB12");
            TextExtractionTools.TextAfter("none", "@").Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "TokenAt")]
        [Trait("Covers", "SplitText")]
        public void Tokens()
        {
            TextExtractionTools.TokenAt("a|b|c", "|", -1).Should().Be("c");
            TextExtractionTools.SplitText(" a , b ,, c ", ",").Should().Equal("a", "b", "", "c");
            TextExtractionTools.SplitText(" a , b ,, c ", ",", removeEmpty: true).Should().Equal("a", "b", "c");
        }

        [Fact]
        [Trait("Covers", "ExtractNumbers")]
        [Trait("Covers", "ExtractWholeNumbers")]
        [Trait("Covers", "QueryStringValue")]
        public void NumbersAndQueries()
        {
            TextExtractionTools.ExtractNumbers("paid 12.50 then -3 twice").Should().Equal(12.5, -3);
            TextExtractionTools.ExtractWholeNumbers("invoices 1001 and 1002").Should().Equal(1001, 1002);
            TextExtractionTools.QueryStringValue("https://x.test/p?id=7&name=a%20b", "name").Should().Be("a b");
            var act = () => TextExtractionTools.ExtractWholeNumbers(new string('9', 30));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ExtractNumbersIgnoresTheServerCulture()
        {
            var original = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                TextExtractionTools.ExtractNumbers("amount 1.5").Should().Equal(1.5);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}