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
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class HashingEncodingAndTextExtensionsTests
    {
        private const string Fox = "The quick brown fox jumps over the lazy dog";

        [Fact]
        public void DigestsMatchTheStandardTestVectors()
        {
            "abc".Sha256Hex().Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
            "abc".Sha384Hex().Should()
                .Be("cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7");
            "abc".Sha512Hex().Should()
                .Be(
                    "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f");
            "abc".Sha1Hex().Should().Be("a9993e364706816aba3e25717850c26c9cd0d89d");
            "abc".Md5Hex().Should().Be("900150983cd24fb0d6963f7d28e17f72");
            "123456789".Crc32Hex().Should().Be("cbf43926");
        }

        [Fact]
        public void HmacsMatchTheStandardTestVectors()
        {
            Fox.HmacSha256Hex("key").Should().Be("f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8");
            Fox.HmacSha512Hex("key").Should()
                .Be(
                    "b42af09057bac1e2d41708e48a902e09b5ff7f12ab428a4fe86653c73dd248fb82f948a549f7b791a5b41915ee4d1ec3935357e4e2317250d0372afa2ebeeb3a");
        }

        [Fact]
        public void HexAndBase64UrlRoundTrip()
        {
            "Jübe".ToHexUtf8().Should().Be("4ac3bc6265");
            "0x4AC3BC6265".FromHexUtf8().Should().Be("Jübe");
            "hello?>".EncodeBase64Url().Should().Be("aGVsbG8_Pg");
            "aGVsbG8_Pg".DecodeBase64Url().Should().Be("hello?>");
        }

        [Fact]
        public void UnicodeNormalisedAppliesTheRequestedForm()
        {
            "e\u0301".UnicodeNormalised("NFC").Should().Be("\u00e9");
            "\uFB01".UnicodeNormalised("nfkc").Should().Be("fi");
            var act = () => "x".UnicodeNormalised("NFX");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RemoveControlCharactersKeepsLineBreaksAndTabs()
        {
            "a\u0000b\u0007c\td\ne".RemoveControlCharacters().Should().Be("abc\td\ne");
        }

        [Fact]
        public void CountingHelpersCountWhatTheyName()
        {
            "  one two\tthree\n".WordCount().Should().Be(3);
            "a\r\nb\nc".LineCount().Should().Be(3);
            "".LineCount().Should().Be(0);
            "abababa".CountOccurrences("aba").Should().Be(2);
            "aabbcc".DistinctCharacterCount().Should().Be(3);
            "ab111122".LongestCharacterRun().Should().Be(4);
        }

        [Fact]
        public void ShapingHelpersProduceTheExpectedText()
        {
            "  a \t b\n c ".CollapseWhitespace().Should().Be("a b c");
            "Crème Brûlée & Co.!".ToSlug().Should().Be("creme-brulee-co");
        }

        [Theory]
        [InlineData("a,b,c", ",", 1, "b")]
        [InlineData("a,b,c", ",", -1, "c")]
        [InlineData("a,b,c", ",", 5, "")]
        [InlineData("a b", null, 0, "a")]
        public void TokenAtSupportsNegativeIndexes(string text, string? separator, int index, string expected)
        {
            text.TokenAt(separator, index).Should().Be(expected);
        }

        [Fact]
        public void TokenCountCountsEmptyTokens()
        {
            "a,,c".TokenCount(",").Should().Be(3);
            "".TokenCount(",").Should().Be(0);
        }

        [Fact]
        public void RegexHelpersReplaceCaptureAndCount()
        {
            "a1b22c333".RegexReplace("[0-9]+", "#").Should().Be("a#b#c#");
            "order-12345-x".RegexCapture("order-([0-9]+)", 1).Should().Be("12345");
            "order-12345-x".RegexCapture("missing-([0-9]+)", 1).Should().BeEmpty();
            "a1b22c333".RegexMatchCount("[0-9]+").Should().Be(3);
            "1.5*2".EscapedForRegex().Should().Be("1\\.5\\*2");
        }

        [Fact]
        public void UserSuppliedPatternsAreBoundedByATimeout()
        {
            var input = new string('a', 5000) + "!";

            var act = () => input.IsMatch("^(a+)+$");

            act.Should().Throw<RegexMatchTimeoutException>();
        }

        [Fact]
        public void FuzzyScalarHelpersScoreNamePairs()
        {
            "abcd".DamerauDistance("abdc").Should().Be(1);
            "abcd".LevenshteinDistance("abdc").Should().Be(2);
            "Night".DiceSimilarity("Nacht").Should().BeApproximately(0.25, 1e-9);
            "Smith John".TokenSortSimilarity("john smith").Should().Be(1);
            "John Smith".TokenSetSimilarity("Smith John Paul").Should().BeApproximately(2d / 3d, 1e-9);
            "Robert".SoundsLike("Rupert").Should().BeTrue();
            "Robert".SoundsLike("Rubin").Should().BeFalse();
        }

        [Fact]
        public void NormalisedForMatchingStripsTitlesSuffixesAndDiacritics()
        {
            "Dr. José Núñez".NormalisedForMatching().Should().Be("jose nunez");
            "Acme Holdings Ltd.".NormalisedForMatching().Should().Be("acme holdings");
        }
    }
}