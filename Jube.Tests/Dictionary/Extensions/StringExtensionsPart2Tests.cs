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

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class StringExtensionsPart2Tests
    {
        [Theory]
        [InlineData("A1", 0, UnicodeCategory.UppercaseLetter)]
        [InlineData("A1", 1, UnicodeCategory.DecimalDigitNumber)]
        public void GetUnicodeCategoryLooksAtTheCharacterAtTheGivenIndex(string input, int index,
            UnicodeCategory expected)
        {
            input.GetUnicodeCategory(index).Should().Be(expected);
        }

        [Fact]
        public void HtmlAttributeEncodeEscapesQuotesButNotAllHtmlMetacharacters()
        {
            "he said \"hi\"".HtmlAttributeEncode().Should().Be("he said &quot;hi&quot;");
        }

        [Fact]
        public void HtmlAttributeEncodeWithATextWriterWritesToTheStreamInsteadOfReturning()
        {
            using var writer = new StringWriter();

            "he said \"hi\"".HtmlAttributeEncode(writer);

            writer.ToString().Should().Be("he said &quot;hi&quot;");
        }

        [Fact]
        public void HtmlEncodeEscapesAngleBracketsAndAmpersands()
        {
            "<b>a & b</b>".HtmlEncode().Should().Be("&lt;b&gt;a &amp; b&lt;/b&gt;");
        }

        [Fact]
        public void HtmlEncodeWithATextWriterWritesToTheStreamInsteadOfReturning()
        {
            using var writer = new StringWriter();

            "<b>x</b>".HtmlEncode(writer);

            writer.ToString().Should().Be("&lt;b&gt;x&lt;/b&gt;");
        }

        [Fact]
        public void HtmlDecodeReversesHtmlEncode()
        {
            "&lt;b&gt;a &amp; b&lt;/b&gt;".HtmlDecode().Should().Be("<b>a & b</b>");
        }

        [Fact]
        public void HtmlDecodeWithATextWriterWritesToTheStreamInsteadOfReturning()
        {
            using var writer = new StringWriter();

            "&lt;b&gt;x&lt;/b&gt;".HtmlDecode(writer);

            writer.ToString().Should().Be("<b>x</b>");
        }

        [Fact]
        public void IfEmptyReturnsTheOriginalValueWhenItIsNotEmpty()
        {
            "value".IfEmpty("default").Should().Be("value");
        }

        [Fact]
        public void IfEmptyReturnsTheDefaultWhenTheOriginalIsEmpty()
        {
            "".IfEmpty("default").Should().Be("default");
        }

        [Fact]
        public void IfEmptyTreatsNullAsNotEmptyBecauseItOnlyChecksAgainstTheEmptyString()
        {
            string? nullValue = null;

            nullValue!.IfEmpty("default").Should().BeNull();
        }

        [Theory]
        [InlineData("b", new[] { "a", "b", "c" }, true)]
        [InlineData("z", new[] { "a", "b", "c" }, false)]
        public void InChecksMembershipAgainstTheProvidedValues(string input, string[] values, bool expected)
        {
            input.In(values).Should().Be(expected);
        }

        [Theory]
        [InlineData("International Business Machines", "IBM")]
        [InlineData("  hello   world  ", "HW")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("solo", "S")]
        public void InitialsOfReturnsTheUppercasedFirstLetterOfEachWord(string? input, string expected)
        {
            input.InitialsOf().Should().Be(expected);
        }

        [Fact]
        public void InternReturnsTheSameReferenceForEqualStringsOnceInterned()
        {
            var built = new string(['I', 'n', 't', 'e', 'r', 'n', 'e', 'd']);

            var interned = built.Intern();

            ReferenceEquals(interned, string.Intern(built)).Should().BeTrue();
        }

        [Theory]
        [InlineData("HelloWorld", true)]
        [InlineData("Hello World", false)]
        [InlineData("Hello123", false)]
        [InlineData("", true)]
        public void IsAlphaAcceptsOnlyAsciiLettersAndTreatsEmptyAsTrue(string input, bool expected)
        {
            input.IsAlpha().Should().Be(expected);
        }

        [Theory]
        [InlineData("Hello123", true)]
        [InlineData("Hello 123", false)]
        [InlineData("", true)]
        public void IsAlphaNumericAcceptsOnlyAsciiLettersAndDigitsAndTreatsEmptyAsTrue(string input, bool expected)
        {
            input.IsAlphaNumeric().Should().Be(expected);
        }

        [Theory]
        [InlineData("listen", "silent", true)]
        [InlineData("Listen", "silent", false)]
        [InlineData("abc", "abcd", false)]
        public void IsAnagramComparesCharacterMultisetsCaseSensitively(string first, string second, bool expected)
        {
            first.IsAnagram(second).Should().Be(expected);
        }

        [Theory]
        [InlineData("ab", 1, true)]
        [InlineData("abc", 1, false)]
        public void IsControlLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsControl(index).Should().Be(expected);
        }

        [Theory]
        [InlineData("a5c", 1, true)]
        [InlineData("abc", 1, false)]
        public void IsDigitLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsDigit(index).Should().Be(expected);
        }

        [Fact]
        public void IsEmptyIsTrueOnlyForTheExactEmptyString()
        {
            "".IsEmpty().Should().BeTrue();
            " ".IsEmpty().Should().BeFalse();
        }

        [Fact]
        public void IsEmptyThrowsOnNullBecauseItUsesReferenceEqualityViaTheEqualityOperator()
        {
            string? nullValue = null;

            var act = () => nullValue!.IsEmpty();

            act.Should().NotThrow();
            nullValue!.IsEmpty().Should().BeFalse();
        }

        [Fact]
        public void IsHighSurrogateLooksAtTheCharacterAtTheGivenIndex()
        {
            var emoji = "😀";

            emoji.IsHighSurrogate(0).Should().BeTrue();
            emoji.IsHighSurrogate(1).Should().BeFalse();
        }

        [Fact]
        public void IsInternedReturnsTheStringWhenInternedAndNullWhenNot()
        {
            "a-string-literal-is-interned-by-the-compiler".IsInterned().Should().NotBeNull();

            var neverInterned = new string([
                'n', 'e', 'v', 'e', 'r', '-', 'i', 'n', 't', 'e', 'r', 'n', 'e', 'd', '-',
                'x', 'y', 'z'
            ]);
            neverInterned.IsInterned().Should().BeNull();
        }

        [Theory]
        [InlineData("aAc", 1, true)]
        [InlineData("a1c", 1, false)]
        public void IsLetterLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsLetter(index).Should().Be(expected);
        }

        [Theory]
        [InlineData("a!c", 1, false)]
        [InlineData("a1c", 1, true)]
        [InlineData("aAc", 1, true)]
        public void IsLetterOrDigitLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsLetterOrDigit(index).Should().Be(expected);
        }

        [Theory]
        [InlineData("hello.txt", "*.txt", true)]
        [InlineData("hello.csv", "*.txt", false)]
        [InlineData("a1b", "a#b", true)]
        [InlineData("aXb", "a#b", false)]
        [InlineData("cat", "c?t", true)]
        public void IsLikeSupportsStarWildcardAndHashDigitPlaceholder(string input, string pattern, bool expected)
        {
            input.IsLike(pattern).Should().Be(expected);
        }

        [Theory]
        [InlineData("aAc", 1, false)]
        [InlineData("aac", 1, true)]
        public void IsLowerLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsLower(index).Should().Be(expected);
        }

        [Fact]
        public void IsLowSurrogateLooksAtTheCharacterAtTheGivenIndex()
        {
            var emoji = "😀";

            emoji.IsLowSurrogate(1).Should().BeTrue();
            emoji.IsLowSurrogate(0).Should().BeFalse();
        }

        [Fact]
        public void IsMatchDelegatesToRegexIsMatch()
        {
            "abc123".IsMatch(@"^\w+$").Should().BeTrue();
            "abc 123".IsMatch(@"^\w+$").Should().BeFalse();
        }

        [Fact]
        public void IsMatchWithOptionsHonoursTheGivenRegexOptions()
        {
            "ABC".IsMatch("abc", RegexOptions.IgnoreCase).Should().BeTrue();
            "ABC".IsMatch("abc", RegexOptions.None).Should().BeFalse();
        }

        [Fact]
        public void IsNotEmptyIsTheInverseOfIsEmpty()
        {
            "x".IsNotEmpty().Should().BeTrue();
            "".IsNotEmpty().Should().BeFalse();
        }

        [Fact]
        public void IsNotNullDistinguishesNullFromNonNull()
        {
            "x".IsNotNull().Should().BeTrue();
            ((string?)null)!.IsNotNull().Should().BeFalse();
        }

        [Fact]
        public void IsNotNullOrEmptyDelegatesToStringIsNullOrEmptyNegated()
        {
            "x".IsNotNullOrEmpty().Should().BeTrue();
            "".IsNotNullOrEmpty().Should().BeFalse();
            ((string?)null)!.IsNotNullOrEmpty().Should().BeFalse();
        }

        [Fact]
        public void IsNotNullOrWhiteSpaceDelegatesToStringIsNullOrWhiteSpaceNegated()
        {
            "x".IsNotNullOrWhiteSpace().Should().BeTrue();
            "   ".IsNotNullOrWhiteSpace().Should().BeFalse();
            ((string?)null)!.IsNotNullOrWhiteSpace().Should().BeFalse();
        }

        [Fact]
        public void IsNullDistinguishesNullFromNonNull()
        {
            ((string?)null)!.IsNull().Should().BeTrue();
            "x".IsNull().Should().BeFalse();
        }

        [Fact]
        public void IsNullOrEmptyDelegatesToTheBcl()
        {
            ((string?)null)!.IsNullOrEmpty().Should().BeTrue();
            "".IsNullOrEmpty().Should().BeTrue();
            "x".IsNullOrEmpty().Should().BeFalse();
        }

        [Fact]
        public void IsNullOrWhiteSpaceDelegatesToTheBcl()
        {
            ((string?)null)!.IsNullOrWhiteSpace().Should().BeTrue();
            "   ".IsNullOrWhiteSpace().Should().BeTrue();
            "x".IsNullOrWhiteSpace().Should().BeFalse();
        }

        [Theory]
        [InlineData("a5c", 1, true)]
        [InlineData("abc", 1, false)]
        public void IsNumberLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsNumber(index).Should().Be(expected);
        }

        [Theory]
        [InlineData("12345", true)]
        [InlineData("123.45", false)]
        [InlineData("-123", false)]
        [InlineData("", true)]
        public void IsNumericOnlyAcceptsDigitsZeroToNineAndTreatsEmptyAsTrue(string input, bool expected)
        {
            input.IsNumeric().Should().Be(expected);
        }

        [Theory]
        [InlineData("racecar", true)]
        [InlineData("A man a plan a canal Panama", true)]
        [InlineData("Racecar", true)]
        [InlineData("hello", false)]
        [InlineData("12321", true)]
        public void IsPalindromeStripsNonAlphanumericsAndIsCaseInsensitive(string input, bool expected)
        {
            input.IsPalindrome().Should().Be(expected);
        }

        [Theory]
        [InlineData("a!c", 1, true)]
        [InlineData("abc", 1, false)]
        public void IsPunctuationLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsPunctuation(index).Should().Be(expected);
        }

        [Theory]
        [InlineData("a c", 1, true)]
        [InlineData("abc", 1, false)]
        public void IsSeparatorLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsSeparator(index).Should().Be(expected);
        }

        [Fact]
        public void IsSurrogateLooksAtTheCharacterAtTheGivenIndex()
        {
            var emoji = "😀";

            emoji.IsSurrogate(0).Should().BeTrue();
            emoji.IsSurrogate(1).Should().BeTrue();
            "abc".IsSurrogate(0).Should().BeFalse();
        }

        [Fact]
        public void IsSurrogatePairLooksAtTheCharacterPairStartingAtTheGivenIndex()
        {
            var emoji = "😀";

            emoji.IsSurrogatePair(0).Should().BeTrue();
            "abc".IsSurrogatePair(0).Should().BeFalse();
        }

        [Theory]
        [InlineData("a$c", 1, true)]
        [InlineData("abc", 1, false)]
        public void IsSymbolLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsSymbol(index).Should().Be(expected);
        }

        [Theory]
        [InlineData("aAc", 1, true)]
        [InlineData("aac", 1, false)]
        public void IsUpperLooksAtTheCharacterAtTheGivenIndex(string input, int index, bool expected)
        {
            input.IsUpper(index).Should().Be(expected);
        }

        [Theory]
        [InlineData("DEUTDEFF", true)]
        [InlineData("DEUTDEFF500", true)]
        [InlineData("deutdeff", true)]
        [InlineData("DEUTDEFF5", false)]
        [InlineData("12345678", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidBicChecksTheEightOrElevenCharacterSwiftFormat(string? input, bool expected)
        {
            input.IsValidBic().Should().Be(expected);
        }

        [Theory]
        [InlineData("01/2099", true)]
        [InlineData("1/2099", true)]
        [InlineData("01/2000", false)]
        [InlineData("12/30", true)]
        [InlineData("13/2099", false)]
        [InlineData("00/2099", false)]
        [InlineData("notadate", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidCardExpiryAcceptsMmSlashYyOrMmSlashYyyyNotYetElapsed(string? input, bool expected)
        {
            input.IsValidCardExpiry().Should().Be(expected);
        }

        [Theory]
        [InlineData("user@example.com", true)]
        [InlineData("user.name@sub.example.com", true)]
        [InlineData("not-an-email", false)]
        [InlineData("@example.com", false)]
        [InlineData("user@", false)]
        public void IsValidEmailAcceptsCommonFormsAndRejectsObviouslyMalformedInput(string input, bool expected)
        {
            input.IsValidEmail().Should().Be(expected);
        }

        [Theory]
        [InlineData("192.168.1.1", true)]
        [InlineData("255.255.255.255", true)]
        [InlineData("256.1.1.1", false)]
        [InlineData("1.2.3", false)]
        [InlineData("not-an-ip", false)]
        [InlineData("::1", false)]
        public void IsValidIpAcceptsWellFormedIpv4AndRejectsOutOfRangeOrNonIpv4Input(string input, bool expected)
        {
            input.IsValidIP().Should().Be(expected);
        }

        [Fact]
        public void IsValidIpRejectsALeadingZeroAsTheEntireFirstOctetEvenThoughZeroIsAValidOctetValue()
        {
            "0.0.0.0".IsValidIP().Should().BeFalse();
            "1.0.0.0".IsValidIP().Should().BeTrue();
        }

        [Fact]
        public void IsValidIpAcceptsThreeDigitLeadingZeroFormsBecauseItMatchesTextNotNumericValue()
        {
            "001.002.003.004".IsValidIP().Should().BeTrue();
        }

        [Theory]
        [InlineData("12345", 4, true)]
        [InlineData("11112111", 4, false)]
        [InlineData("abc123456xyz", 4, true)]
        [InlineData("abc1234xyz", 5, false)]
        [InlineData("987654", 3, true)]
        [InlineData("112233", 3, false)]
        [InlineData("", 3, false)]
        public void HasSequentialDigitsDetectsAscendingOrDescendingRunsOfAtLeastTheGivenLength(string input,
            int minRunLength, bool expected)
        {
            input.HasSequentialDigits(minRunLength).Should().Be(expected);
        }

        [Fact]
        public void HasSequentialDigitsTreatsAMinimumRunLengthBelowTwoAsTwo()
        {
            "abc12xyz".HasSequentialDigits(1).Should().BeTrue();
            "abc12xyz".HasSequentialDigits(0).Should().BeTrue();
        }

        [Theory]
        [InlineData("1111111", true)]
        [InlineData("1111112", false)]
        [InlineData("5", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsAllSameCharacterChecksEveryCharacterMatchesTheFirst(string? input, bool expected)
        {
            input.IsAllSameCharacter().Should().Be(expected);
        }

        [Theory]
        [InlineData("79927398713", true)]
        [InlineData("79927398714", false)]
        [InlineData("4111111111111111", true)]
        [InlineData("4111111111111112", false)]
        [InlineData("4111-1111-1111-1111", false)]
        [InlineData("5", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidLuhnValidatesTheChecksumDigit(string? input, bool expected)
        {
            input.IsValidLuhn().Should().Be(expected);
        }

        [Theory]
        [InlineData("GB29NWBK60161331926819", true)]
        [InlineData("gb29 nwbk 6016 1331 9268 19", true)]
        [InlineData("DE89370400440532013000", true)]
        [InlineData("DE89370400440532013001", false)]
        [InlineData("GB29", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidIbanValidatesTheMod97Checksum(string? input, bool expected)
        {
            input.IsValidIban().Should().Be(expected);
        }
    }
}