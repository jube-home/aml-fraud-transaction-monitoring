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
using System.Text;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class StringExtensionsPart1Tests
    {
        [Fact]
        public void Br2NlReplacesSelfClosingAndOpenBreakTagsWithCrlf()
        {
            "a<br />b<br>c".Br2Nl().Should().Be("a\r\nb\r\nc");
        }

        [Fact]
        public void Br2NlLeavesUnmatchedCasingUntouchedBecauseTheReplaceIsCaseSensitive()
        {
            "a<BR>b".Br2Nl().Should().Be("a<BR>b", "String.Replace is ordinal/case-sensitive by default");
        }

        [Theory]
        [InlineData("4111111111111111", "Visa")]
        [InlineData("4012888888881881", "Visa")]
        [InlineData("5500000000000004", "Mastercard")]
        [InlineData("2221000000000009", "Mastercard")]
        [InlineData("2720000000000005", "Mastercard")]
        [InlineData("340000000000009", "AmericanExpress")]
        [InlineData("370000000000002", "AmericanExpress")]
        [InlineData("6011000000000004", "Discover")]
        [InlineData("6500000000000002", "Discover")]
        [InlineData("30000000000004", "DinersClub")]
        [InlineData("36000000000008", "DinersClub")]
        [InlineData("3528000000000009", "Jcb")]
        [InlineData("1234567890123456", "Unknown")]
        [InlineData("abc", "Unknown")]
        [InlineData("", "Unknown")]
        [InlineData(null, "Unknown")]
        public void CardNetworkClassifiesByIinPrefixRanges(string? input, string expected)
        {
            input.CardNetwork().Should().Be(expected);
        }

        [Fact]
        public void CardNetworkIgnoresSpacesAndDashes()
        {
            "4111 1111 1111 1111".CardNetwork().Should().Be("Visa");
            "4111-1111-1111-1111".CardNetwork().Should().Be("Visa");
        }

        [Fact]
        public void CompareOrdinalTwoArgOverloadOrdersLexicallyByOrdinalValue()
        {
            "a".CompareOrdinal("b").Should().BeLessThan(0);
            "b".CompareOrdinal("a").Should().BeGreaterThan(0);
            "a".CompareOrdinal("a").Should().Be(0);
        }

        [Fact]
        public void CompareOrdinalFiveArgOverloadComparesOnlyTheSpecifiedSubstrings()
        {
            var result = "xxabcxx".CompareOrdinal(2, "xxabdxx", 2, 2);

            result.Should().Be(0,
                "the compared substrings 'ab' == 'ab' are equal even though the full strings differ later");
        }

        [Fact]
        public void ConcatTwoArgJoinsTheStringsWithNoSeparator()
        {
            "foo".Concat("bar").Should().Be("foobar");
        }

        [Fact]
        public void ConcatThreeArgJoinsAllThreeStrings()
        {
            "a".Concat("b", "c").Should().Be("abc");
        }

        [Fact]
        public void ConcatFourArgJoinsAllFourStrings()
        {
            "a".Concat("b", "c", "d").Should().Be("abcd");
        }

        [Fact]
        public void ConcatTreatsANullArgumentAsAnEmptyStringJustLikeStringConcat()
        {
            "a".Concat(null!).Should().Be("a");
        }

        [Fact]
        public void ConcatenateJoinsAnEnumerableOfStringsWithNoSeparator()
        {
            new[] { "a", "b", "c" }.Concatenate().Should().Be("abc");
        }

        [Fact]
        public void ConcatenateOfAnEmptySequenceProducesAnEmptyString()
        {
            Array.Empty<string>().Concatenate().Should().Be(string.Empty);
        }

        [Fact]
        public void ConcatenateWithASelectorAppliesTheProjectionBeforeJoining()
        {
            new[] { 1, 2, 3 }.Concatenate(i => i.ToString()).Should().Be("123");
        }

        [Fact]
        public void ConcatWithAppendsEachVarargValueInOrder()
        {
            "start-".ConcatWith("a", "b", "c").Should().Be("start-abc");
        }

        [Fact]
        public void ConcatWithNoValuesReturnsTheOriginalStringUnchanged()
        {
            "start".ConcatWith().Should().Be("start");
        }

        [Fact]
        public void ContainsAllReturnsTrueOnlyWhenEveryValueIsPresent()
        {
            "the quick brown fox".ContainsAll("quick", "fox").Should().BeTrue();
            "the quick brown fox".ContainsAll("quick", "slow").Should().BeFalse();
        }

        [Fact]
        public void ContainsAllWithComparisonTypeRespectsCaseInsensitivity()
        {
            "The Quick Fox".ContainsAll(StringComparison.OrdinalIgnoreCase, "quick", "fox").Should().BeTrue();
            "The Quick Fox".ContainsAll(StringComparison.Ordinal, "quick", "fox").Should().BeFalse();
        }

        [Fact]
        public void ContainsAnyReturnsTrueWhenAtLeastOneValueIsPresent()
        {
            "the quick brown fox".ContainsAny("slow", "fox").Should().BeTrue();
            "the quick brown fox".ContainsAny("slow", "lazy").Should().BeFalse();
        }

        [Fact]
        public void ContainsAnyWithComparisonTypeRespectsCaseInsensitivity()
        {
            "The Quick Fox".ContainsAny(StringComparison.OrdinalIgnoreCase, "quick").Should().BeTrue();
            "The Quick Fox".ContainsAny(StringComparison.Ordinal, "quick").Should().BeFalse();
        }

        [Fact]
        public void TheSingleArgContainsExtensionIsShadowedByStringsOwnInstanceContainsMethod()
        {
            var viaInstanceSyntax = "the fox".Contains("fox");
            var viaExtensionDirectly = Jube.Dictionary.Extensions.Extensions.Contains("the fox", "fox");

            viaInstanceSyntax.Should().BeTrue();
            viaExtensionDirectly.Should().BeTrue();
        }

        [Fact]
        public void TheContainsWithComparisonTypeOverloadIsAlsoShadowedByAnIdenticalBclInstanceOverload()
        {
            var viaExtensionDirectly =
                Jube.Dictionary.Extensions.Extensions.Contains("The Fox", "fox", StringComparison.OrdinalIgnoreCase);

            viaExtensionDirectly.Should().BeTrue();
        }

        [Fact]
        public void ConvertToUtf32CombinesTheSurrogatePairAtTheGivenIndexIntoItsCodePoint()
        {
            const string emoji = "😀";

            emoji.ConvertToUtf32(0).Should().Be(char.ConvertToUtf32(emoji[0], emoji[1]));
        }

        [Fact]
        public void DecodeBase64RecoversTheOriginalAsciiText()
        {
            "aGVsbG8=".DecodeBase64().Should().Be("hello");
        }

        [Fact]
        public void DecodeBase64ThrowsForMalformedBase64Input()
        {
            var act = () => "not-valid-base64!!".DecodeBase64();

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void EncodeBase64ThenDecodeBase64RoundTripsTheOriginalText()
        {
            const string original = "Round trip me!";

            original.EncodeBase64().DecodeBase64().Should().Be(original);
        }

        [Fact]
        public void DeserializeJsonParsesAMatchingJsonObjectIntoThePocoType()
        {
            var result = """{"A":42,"B":"hello"}""".DeserializeJson<Sample>();

            result.Should().NotBeNull();
            result!.A.Should().Be(42);
            result.B.Should().Be("hello");
        }

        [Fact]
        public void DeserializeJsonReturnsDefaultRatherThanThrowingForMalformedJson()
        {
            var result = "{not valid json".DeserializeJson<Sample>();

            result.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void DeserializeJsonReturnsDefaultForNullEmptyOrWhitespaceInputWithoutAttemptingToParse(string? input)
        {
            var result = input.DeserializeJson<Sample>();

            result.Should().BeNull();
        }

        [Fact]
        public void DeserializeJsonWithAnExplicitEncodingAlsoParsesCorrectly()
        {
            var result = """{"A":7,"B":"x"}""".DeserializeJson<Sample>(Encoding.UTF8);

            result.Should().NotBeNull();
            result!.A.Should().Be(7);
        }

        [Fact]
        public void DeserializeJsonWithANullEncodingFallsBackToUtf8()
        {
            var result = """{"A":9,"B":"y"}""".DeserializeJson<Sample>(null);

            result.Should().NotBeNull();
            result!.A.Should().Be(9);
        }

        [Theory]
        [InlineData("abc123", 3)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        [InlineData("no digits here", 0)]
        public void DigitCountCountsDigitCharacters(string? input, int expected)
        {
            input.DigitCount().Should().Be(expected);
        }

        [Theory]
        [InlineData("abc123", 3)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        [InlineData("12345", 0)]
        public void LetterCountCountsLetterCharacters(string? input, int expected)
        {
            input.LetterCount().Should().Be(expected);
        }

        [Theory]
        [InlineData("user@example.com", "example.com")]
        [InlineData("notanemail", "")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void EmailDomainReturnsEverythingAfterTheAtSign(string? input, string expected)
        {
            input.EmailDomain().Should().Be(expected);
        }

        [Theory]
        [InlineData("user@example.com", "user")]
        [InlineData("notanemail", "notanemail")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void EmailLocalPartReturnsEverythingBeforeTheAtSign(string? input, string expected)
        {
            input.EmailLocalPart().Should().Be(expected);
        }

        [Fact]
        public void EqualsIgnoreCaseComparesOrdinallyIgnoringCase()
        {
            "Hello".EqualsIgnoreCase("HELLO").Should().BeTrue();
            "Hello".EqualsIgnoreCase("World").Should().BeFalse();
        }

        [Fact]
        public void EscapeXmlEscapesAllFiveReservedXmlCharacters()
        {
            "<a href=\"x\">Tom & Jerry's</a>".EscapeXml()
                .Should().Be("&lt;a href=&quot;x&quot;&gt;Tom &amp; Jerry&apos;s&lt;/a&gt;");
        }

        [Fact]
        public void EscapeXmlOnPlainTextIsAnIdentityOperation()
        {
            "plain text".EscapeXml().Should().Be("plain text");
        }

        [Fact]
        public void ExtractKeepsOnlyCharactersMatchingThePredicate()
        {
            "a1b2c3".Extract(char.IsDigit).Should().Be("123");
        }

        [Fact]
        public void ExtractWithAPredicateThatMatchesNothingReturnsAnEmptyString()
        {
            "abcdef".Extract(char.IsDigit).Should().Be(string.Empty);
        }

        [Fact]
        public void ExtractDecimalConcatenatesEveryDigitAndDotInTheStringRatherThanFindingOneNumber()
        {
            "price: -12 and 34.5".ExtractDecimal().Should().Be(-1234.5m);
        }

        [Fact]
        public void ExtractDecimalThrowsWhenTheStringContainsNoDigits()
        {
            var act = () => "no digits here".ExtractDecimal();

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void ExtractDoubleHasTheSameWholeStringConcatenationBehaviourAsExtractDecimal()
        {
            "-12 and 34.5".ExtractDouble().Should().Be(-1234.5d);
        }

        [Theory]
        [InlineData("id-42", (short)-42)]
        [InlineData("-7 apples", (short)-7)]
        [InlineData("id42", (short)42)]
        public void ExtractInt16ConcatenatesAllDigitsHonouringOnlyALeadingMinusBeforeTheFirstDigit(string input,
            short expected)
        {
            input.ExtractInt16().Should().Be(expected);
        }

        [Fact]
        public void ExtractInt16ThrowsForAnEmptyDigitAccumulation()
        {
            var act = () => "no digits".ExtractInt16();

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void ExtractInt32MergesMultipleSeparateNumbersInTheStringIntoOne()
        {
            "abc-12-34".ExtractInt32().Should().Be(-1234);
        }

        [Fact]
        public void ExtractInt64BehavesLikeExtractInt32ButForA64BitResult()
        {
            "id:9999999999".ExtractInt64().Should().Be(9999999999L);
        }

        [Fact]
        public void ExtractLetterKeepsOnlyLetterCharacters()
        {
            "a1! b2? c3.".ExtractLetter().Should().Be("abc");
        }

        [Fact]
        public void ExtractManyDecimalFindsEachSeparateSignedDecimalNumberIndependently()
        {
            "-12 and 34.5 and -6.25".ExtractManyDecimal().Should().Equal(-12m, 34.5m, -6.25m);
        }

        [Fact]
        public void ExtractManyDecimalOnAStringWithNoNumbersReturnsAnEmptyArray()
        {
            "no numbers here".ExtractManyDecimal().Should().BeEmpty();
        }

        [Fact]
        public void ExtractManyDoubleFindsEachSeparateSignedNumberIndependentlyUnlikeExtractDouble()
        {
            "-12 and 34.5".ExtractManyDouble().Should().Equal(-12d, 34.5d);
        }

        [Fact]
        public void ExtractManyInt16FindsEachSeparateSignedIntegerIndependently()
        {
            "a-1b22c-333".ExtractManyInt16().Should().Equal(-1, 22, -333);
        }

        [Fact]
        public void ExtractManyInt32FindsEachSeparateSignedIntegerIndependently()
        {
            "x=10, y=-20".ExtractManyInt32().Should().Equal(10, -20);
        }

        [Fact]
        public void ExtractManyInt64FindsEachSeparateSignedIntegerIndependently()
        {
            "a=9999999999, b=-1".ExtractManyInt64().Should().Equal(9999999999L, -1L);
        }

        [Fact]
        public void ExtractManyUInt16IgnoresTheMinusSignAndOnlyMatchesTheDigitsThemselves()
        {
            "-42".ExtractManyUInt16().Should().Equal(42);
        }

        [Fact]
        public void ExtractManyUInt32IgnoresTheMinusSignJustLikeExtractManyUInt16()
        {
            "-4200000".ExtractManyUInt32().Should().Equal(4200000);
        }

        [Fact]
        public void ExtractManyUInt64FindsEachUnsignedNumberInTheString()
        {
            "a10 b20".ExtractManyUInt64().Should().Equal(10UL, 20UL);
        }

        [Fact]
        public void ExtractNumberKeepsOnlyCharactersThatCharIsNumberRecognises()
        {
            "a1 b2 c3".ExtractNumber().Should().Be("123");
        }

        [Fact]
        public void ExtractUInt16ConcatenatesAllDigitsWithNoNegativeSignSupportAtAll()
        {
            "-42".ExtractUInt16().Should().Be(42);
        }

        [Fact]
        public void ExtractUInt32ConcatenatesAllDigitsAcrossTheWholeString()
        {
            "id-1-2-3".ExtractUInt32().Should().Be(123);
        }

        [Fact]
        public void ExtractUInt64ThrowsWhenNoDigitsArePresent()
        {
            var act = () => "no digits".ExtractUInt64();

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void FormatOneArgDelegatesToStringFormat()
        {
            "Hello {0}".Format("World").Should().Be("Hello World");
        }

        [Fact]
        public void FormatTwoArgDelegatesToStringFormat()
        {
            "{0}-{1}".Format("a", "b").Should().Be("a-b");
        }

        [Fact]
        public void FormatThreeArgDelegatesToStringFormat()
        {
            "{0}-{1}-{2}".Format("a", "b", "c").Should().Be("a-b-c");
        }

        [Fact]
        public void FormatObjectArrayOverloadDelegatesToStringFormat()
        {
            "{0}-{1}-{2}-{3}".Format(["a", "b", "c", "d"]).Should().Be("a-b-c-d");
        }

        [Fact]
        public void FormatThrowsAFormatExceptionForAnUnmatchedPlaceholder()
        {
            var act = () => "{0}-{1}".Format("only-one");

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void FormatWithOneArgBehavesTheSameAsFormat()
        {
            "Hi {0}".FormatWith("there").Should().Be("Hi there");
        }

        [Fact]
        public void FormatWithTwoArgBehavesTheSameAsFormat()
        {
            "{0}{1}".FormatWith("a", "b").Should().Be("ab");
        }

        [Fact]
        public void FormatWithThreeArgBehavesTheSameAsFormat()
        {
            "{0}{1}{2}".FormatWith("a", "b", "c").Should().Be("abc");
        }

        [Fact]
        public void FormatWithParamsArrayOverloadAcceptsAnyNumberOfValues()
        {
            "{0}{1}{2}{3}{4}".FormatWith("a", "b", "c", "d", "e").Should().Be("abcde");
        }

        [Fact]
        public void GetAfterReturnsEverythingFollowingTheFirstOccurrenceOfTheMarker()
        {
            "key=value=extra".GetAfter("=").Should().Be("value=extra");
        }

        [Fact]
        public void GetAfterReturnsEmptyStringWhenTheMarkerIsNotFound()
        {
            "no marker here".GetAfter("=").Should().Be(string.Empty);
        }

        [Fact]
        public void GetBeforeReturnsEverythingPrecedingTheFirstOccurrenceOfTheMarker()
        {
            "key=value=extra".GetBefore("=").Should().Be("key");
        }

        [Fact]
        public void GetBeforeReturnsEmptyStringWhenTheMarkerIsNotFound()
        {
            "no marker here".GetBefore("=").Should().Be(string.Empty);
        }

        [Fact]
        public void GetBetweenReturnsTheTextBetweenTheFirstBeforeMarkerAndTheNextAfterMarker()
        {
            "<tag>content</tag>".GetBetween("<tag>", "</tag>").Should().Be("content");
        }

        [Fact]
        public void GetBetweenReturnsEmptyStringWhenTheBeforeMarkerIsAbsentButShortEnoughNotToOverrun()
        {
            "no markers".GetBetween("x", "y").Should().Be(string.Empty);
        }

        [Fact]
        public void GetBetweenReturnsEmptyStringWhenTheBeforeMarkerIsAbsentAndLongerThanTheRemainingStringLength()
        {
            "ab".GetBetween("this-marker-is-too-long", "y").Should().Be(string.Empty);
        }

        [Fact]
        public void GetBetweenReturnsEmptyStringWhenTheAfterMarkerIsMissing()
        {
            "<tag>content".GetBetween("<tag>", "</tag>").Should().Be(string.Empty);
        }

        [Theory]
        [InlineData("a5b", 1, 5d)]
        [InlineData("ab", 0, -1d)]
        public void GetNumericValueReturnsTheDigitsValueOrMinusOneForNonDigitsAtTheGivenIndex(string input, int index,
            double expected)
        {
            input.GetNumericValue(index).Should().Be(expected);
        }

        public sealed class Sample
        {
            public int A { get; set; }
            public string? B { get; set; }
        }
    }
}