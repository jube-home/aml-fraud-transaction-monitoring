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
using System.Linq;
using FluentAssertions;
using Jube.Analytics.Categorical;
using Xunit;

namespace Jube.Test.Analytics.Categorical
{
    public class CategoricalNormaliserTests
    {
        [Theory]
        [InlineData("GB", "gb")]
        [InlineData("  Gb  ", "gb")]
        [InlineData("g\tb", "g b")]
        [InlineData("United   Kingdom", "united kingdom")]
        [InlineData("United Kingdom", "united kingdom")]
        [InlineData("ＧＢ", "gb")]
        [InlineData("ﬁne", "fine")]
        [InlineData("GB​", "gb")]
        [InlineData("﻿GB", "gb")]
        [InlineData("é", "é")]
        public void Key_NormalisesCaseWhitespaceAndUnicode(string raw, string expected)
        {
            CategoricalNormaliser.Key(raw).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\r\n")]
        [InlineData("​‌")]
        public void Key_MissingShapesReturnNull(string? raw)
        {
            CategoricalNormaliser.Key(raw).Should().BeNull();
        }

        [Fact]
        public void Key_IsCultureInvariant()
        {
            var original = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CategoricalNormaliser.Key("ISTANBUL").Should().Be("istanbul");
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                CategoricalNormaliser.Key("ISTANBUL").Should().Be("istanbul");
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Key_HostileStrings_NeverThrowAndAreBounded()
        {
            var hostile = new[]
            {
                new string('a', 1_000_000),
                "\uD800",
                "ab\uDC00cd",
                "😀\uD83D",
                new string('\uD83D', 500) + new string('\uDE00', 500),
                "'; DROP TABLE Archive; --",
                "<script>alert(1)</script>",
                "\0\0\0",
                "a\u0000b",
                string.Concat(Enumerable.Repeat("😀", 200))
            };

            foreach (var value in hostile)
            {
                var key = CategoricalNormaliser.Key(value);
                (key?.Length ?? 0).Should().BeLessThanOrEqualTo(CategoricalNormaliser.MaxKeyLength);

                CategoricalNormaliser.Label(value).Length.Should()
                    .BeLessThanOrEqualTo(CategoricalNormaliser.MaxLabelLength);

                if (key != null)
                {
                    key.Should().NotContain("\0");

                    for (var i = 0; i < key.Length; i++)
                    {
                        if (char.IsHighSurrogate(key[i]))
                        {
                            (i + 1 < key.Length && char.IsLowSurrogate(key[i + 1])).Should().BeTrue();
                            i++;
                        }
                        else
                        {
                            char.IsLowSurrogate(key[i]).Should().BeFalse();
                        }
                    }
                }
            }
        }

        [Fact]
        public void Key_LongStringsSharingAPrefixCollideByDesign()
        {
            var a = new string('a', 300) + "1";
            var b = new string('a', 300) + "2";
            CategoricalNormaliser.Key(a).Should().Be(CategoricalNormaliser.Key(b));
        }

        [Fact]
        public void Key_IsIdempotentOnRandomStrings()
        {
            var random = new Random(7);

            for (var i = 0; i < 5000; i++)
            {
                var chars = new char[random.Next(0, 40)];

                for (var j = 0; j < chars.Length; j++)
                {
                    chars[j] = (char)random.Next(0, 0x3000);
                }

                var raw = new string(chars);
                var key = CategoricalNormaliser.Key(raw);

                if (key != null)
                {
                    CategoricalNormaliser.Key(key).Should().Be(key);
                }
            }
        }

        [Fact]
        public void Label_KeepsCaseButRemovesControlCharacters()
        {
            CategoricalNormaliser.Label("  Great\u0000  Britain​ ").Should().Be("Great Britain");
        }

        [Fact]
        public void ToRaw_UsesTheInvariantCulture()
        {
            CategoricalNormaliser.ToRaw(1.5d).Should().Be("1.5");
            CategoricalNormaliser.ToRaw(42L).Should().Be("42");
            CategoricalNormaliser.ToRaw(true).Should().Be("true");
            CategoricalNormaliser.ToRaw(double.NaN).Should().BeEmpty();
        }
    }
}