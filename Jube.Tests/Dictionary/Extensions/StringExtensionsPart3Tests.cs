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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class StringExtensionsPart3Tests
    {
        [Fact]
        public void IsWhiteSpaceChecksTheCharacterAtTheGivenIndex()
        {
            "a b".IsWhiteSpace(1).Should().BeTrue();
            "a b".IsWhiteSpace(0).Should().BeFalse();
        }

        [Fact]
        public void IsWhiteSpaceThrowsForAnOutOfRangeIndex()
        {
            var act = () => "abc".IsWhiteSpace(10);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void JavaScriptStringEncodeEscapesSpecialCharacters()
        {
            "he said \"hi\"".JavaScriptStringEncode().Should().Be("he said \\\"hi\\\"");
        }

        [Fact]
        public void JavaScriptStringEncodeWithDoubleQuotesWrapsTheResultInQuotes()
        {
            var encoded = "a\"b".JavaScriptStringEncode(true);

            encoded.Should().StartWith("\"").And.EndWith("\"");
        }

        [Fact]
        public void JoinWithAStringArrayConcatenatesUsingTheSeparator()
        {
            ",".Join(["a", "b", "c"]).Should().Be("a,b,c");
        }

        [Fact]
        public void JoinWithAnObjectArrayConcatenatesTheirStringRepresentations()
        {
            ",".Join([1, true, "x"]).Should().Be("1,True,x");
        }

        [Fact]
        public void JoinWithAGenericEnumerableConcatenatesUsingTheSeparator()
        {
            ",".Join(Enumerable.Range(1, 3)).Should().Be("1,2,3");
        }

        [Fact]
        public void JoinWithAnEnumerableOfStringConcatenatesUsingTheSeparator()
        {
            IEnumerable<string> Values()
            {
                yield return "x";
                yield return "y";
            }

            ",".Join(Values()).Should().Be("x,y");
        }

        [Fact]
        public void JoinWithAStartIndexAndCountOnlyJoinsTheSelectedRange()
        {
            ",".Join(["a", "b", "c", "d"], 1, 2).Should().Be("b,c");
        }

        [Fact]
        public void JoinOnAnEmptyArrayReturnsAnEmptyString()
        {
            ",".Join([]).Should().Be(string.Empty);
        }

        [Fact]
        public void LeftReturnsTheRequestedNumberOfLeadingCharacters()
        {
            "abcdef".Left(3).Should().Be("abc");
        }

        [Fact]
        public void LeftWithZeroReturnsAnEmptyString()
        {
            "abcdef".Left(0).Should().Be(string.Empty);
        }

        [Fact]
        public void LeftThrowsWhenTheRequestedLengthExceedsTheStringsLength()
        {
            var act = () => "abc".Left(10);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void LeftSafeClampsToTheStringsActualLengthInsteadOfThrowing()
        {
            "abc".LeftSafe(10).Should().Be("abc");
        }

        [Fact]
        public void LeftSafeBehavesLikeLeftWhenTheLengthIsWithinBounds()
        {
            "abcdef".LeftSafe(3).Should().Be("abc");
        }

        [Fact]
        public void MatchWithoutOptionsFindsTheFirstOccurrence()
        {
            var match = "foo123bar456".Match(@"\d+");

            match.Success.Should().BeTrue();
            match.Value.Should().Be("123");
        }

        [Fact]
        public void MatchWithOptionsRespectsThoseOptions()
        {
            var match = "FOO".Match("foo", RegexOptions.IgnoreCase);

            match.Success.Should().BeTrue();
        }

        [Fact]
        public void MatchReturnsAnUnsuccessfulMatchWhenThereIsNoOccurrence()
        {
            "abc".Match(@"\d+").Success.Should().BeFalse();
        }

        [Fact]
        public void MatchesWithoutOptionsFindsEveryOccurrence()
        {
            var matches = "1 2 3".Matches(@"\d+");

            matches.Select(m => m.Value).Should().Equal("1", "2", "3");
        }

        [Fact]
        public void MatchesWithOptionsRespectsThoseOptions()
        {
            var matches = "FOO foo".Matches("foo", RegexOptions.IgnoreCase);

            matches.Count.Should().Be(2);
        }

        [Fact]
        public void MatchesReturnsAnEmptyCollectionWhenThereAreNoOccurrences()
        {
            "abc".Matches(@"\d+").Should().BeEmpty();
        }

        [Fact]
        public void Nl2BrConvertsWindowsAndUnixNewlinesToHtmlBreaks()
        {
            "a\r\nb\nc".Nl2Br().Should().Be("a<br />b<br />c");
        }

        [Fact]
        public void Nl2BrLeavesTextWithoutNewlinesUnchanged()
        {
            "abc".Nl2Br().Should().Be("abc");
        }

        [Theory]
        [InlineData("b", false)]
        [InlineData("z", true)]
        public void NotInIsTheInverseOfMembership(string input, bool expected)
        {
            input.NotIn("a", "b", "c").Should().Be(expected);
        }

        [Fact]
        public void NullIfEmptyReturnsNullForNullOrEmptyAndTheOriginalValueOtherwise()
        {
            ((string?)null).NullIfEmpty().Should().BeNull();
            "".NullIfEmpty().Should().BeNull();
            "abc".NullIfEmpty().Should().Be("abc");
        }

        [Fact]
        public void NullIfEmptyDoesNotTreatWhitespaceAsEmpty()
        {
            " ".NullIfEmpty().Should().Be(" ");
        }

        [Fact]
        public void ParseQueryStringParsesKeyValuePairs()
        {
            var parsed = "a=1&b=2".ParseQueryString();

            parsed["a"].Should().Be("1");
            parsed["b"].Should().Be("2");
        }

        [Fact]
        public void ParseQueryStringWithAnExplicitEncodingStillParsesKeyValuePairs()
        {
            var parsed = "a=1&b=hello".ParseQueryString(Encoding.UTF8);

            parsed["b"].Should().Be("hello");
        }

        [Fact]
        public void ParseQueryStringDecodesUrlEncodedValues()
        {
            var parsed = "name=John%20Doe".ParseQueryString();

            parsed["name"].Should().Be("John Doe");
        }

        [Fact]
        public void PathCombineJoinsThisAndTheSuppliedSegments()
        {
            "a".PathCombine("b", "c").Should().Be(Path.Combine("a", "b", "c"));
        }

        [Fact]
        public void PathCombineWithNoAdditionalSegmentsReturnsTheOriginalValue()
        {
            "a".PathCombine().Should().Be("a");
        }

        [Fact]
        public void RemoveDiacriticsStripsAccentsButKeepsTheBaseLetters()
        {
            "café résumé".RemoveDiacritics().Should().Be("cafe resume");
        }

        [Fact]
        public void RemoveDiacriticsLeavesPlainAsciiUnchanged()
        {
            "hello".RemoveDiacritics().Should().Be("hello");
        }

        [Fact]
        public void RemoveLetterStripsAllLettersButKeepsEverythingElse()
        {
            "abc123 def!".RemoveLetter().Should().Be("123 !");
        }

        [Fact]
        public void RemoveNumberStripsAllDigitsButKeepsEverythingElse()
        {
            "abc123 def!".RemoveNumber().Should().Be("abc def!");
        }

        [Fact]
        public void RemoveWhereStripsCharactersMatchingTheGivenPredicate()
        {
            "a1b2c3".RemoveWhere(char.IsDigit).Should().Be("abc");
        }

        [Fact]
        public void RemoveWhereWithAPredicateThatNeverMatchesLeavesTheStringUnchanged()
        {
            "abc".RemoveWhere(_ => false).Should().Be("abc");
        }

        [Fact]
        public void RepeatWithASingleCharacterStringRepeatsItNTimes()
        {
            "x".Repeat(3).Should().Be("xxx");
        }

        [Fact]
        public void RepeatWithAMultiCharacterStringRepeatsTheWholeStringNTimes()
        {
            "ab".Repeat(3).Should().Be("ababab");
        }

        [Fact]
        public void RepeatWithZeroProducesAnEmptyString()
        {
            "ab".Repeat(0).Should().Be(string.Empty);
        }

        [Fact]
        public void ReplaceByEmptyRemovesEveryOccurrenceOfEachGivenValue()
        {
            "a-b-c".ReplaceByEmpty("-", "b").Should().Be("ac");
        }

        [Fact]
        public void ReplaceByEmptyWithNoValuesLeavesTheStringUnchanged()
        {
            "abc".ReplaceByEmpty().Should().Be("abc");
        }

        [Fact]
        public void ReplaceWithStartIndexLengthAndValueSplicesInTheReplacement()
        {
            "hello world".Replace(0, 5, "goodbye").Should().Be("goodbye world");
        }

        [Fact]
        public void ReplaceFirstReplacesOnlyTheFirstOccurrence()
        {
            "a-a-a".ReplaceFirst("a", "X").Should().Be("X-a-a");
        }

        [Fact]
        public void ReplaceFirstLeavesTheStringUnchangedWhenThereIsNoMatch()
        {
            "abc".ReplaceFirst("z", "X").Should().Be("abc");
        }

        [Theory]
        [InlineData(0, "a-a-a-a")]
        [InlineData(1, "X-a-a-a")]
        [InlineData(2, "X-X-a-a")]
        public void ReplaceFirstWithANumberReplacesExactlyThatManyLeadingOccurrences(int number, string expected)
        {
            "a-a-a-a".ReplaceFirst(number, "a", "X").Should().Be(expected);
        }

        [Fact]
        public void ReplaceLastReplacesOnlyTheLastOccurrence()
        {
            "a-a-a".ReplaceLast("a", "X").Should().Be("a-a-X");
        }

        [Fact]
        public void ReplaceLastLeavesTheStringUnchangedWhenThereIsNoMatch()
        {
            "abc".ReplaceLast("z", "X").Should().Be("abc");
        }

        [Theory]
        [InlineData(0, "a-a-a-a")]
        [InlineData(1, "a-a-a-X")]
        [InlineData(2, "a-a-X-X")]
        public void ReplaceLastWithANumberReplacesExactlyThatManyTrailingOccurrences(int number, string expected)
        {
            "a-a-a-a".ReplaceLast(number, "a", "X").Should().Be(expected);
        }

        [Fact]
        public void ReplaceWhenEqualsSwapsTheEntireStringWhenItExactlyMatches()
        {
            "old".ReplaceWhenEquals("old", "new").Should().Be("new");
        }

        [Fact]
        public void ReplaceWhenEqualsLeavesTheStringUnchangedOnAPartialMatch()
        {
            "oldish".ReplaceWhenEquals("old", "new").Should().Be("oldish");
        }

        [Fact]
        public void ReverseFlipsTheCharacterOrder()
        {
            "abcde".Reverse().Should().Be("edcba");
        }

        [Fact]
        public void ReverseOfAnEmptyOrSingleCharacterStringReturnsItUnchanged()
        {
            "".Reverse().Should().Be("");
            "a".Reverse().Should().Be("a");
        }

        [Fact]
        public void RightReturnsTheRequestedNumberOfTrailingCharacters()
        {
            "abcdef".Right(3).Should().Be("def");
        }

        [Fact]
        public void RightThrowsWhenTheRequestedLengthExceedsTheStringsLength()
        {
            var act = () => "abc".Right(10);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void RightSafeClampsToTheStringsActualLengthInsteadOfThrowing()
        {
            "abc".RightSafe(10).Should().Be("abc");
        }

        [Fact]
        public void RightSafeBehavesLikeRightWhenTheLengthIsWithinBounds()
        {
            "abcdef".RightSafe(3).Should().Be("def");
        }

        [Fact]
        public void SaveAsWritesTheStringToTheGivenFileNameAndOverwritesByDefault()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                "first".SaveAs(path);
                "second".SaveAs(path);

                File.ReadAllText(path).Should().Be("second");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SaveAsWithAppendTrueAddsToExistingContentRatherThanOverwriting()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                "first".SaveAs(path);
                "second".SaveAs(path, true);

                File.ReadAllText(path).Should().Be("firstsecond");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SaveAsWithAFileInfoWritesTheStringToThatFile()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                "content".SaveAs(new FileInfo(path));

                File.ReadAllText(path).Should().Be("content");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SplitByAMultiCharacterSeparatorSplitsCorrectly()
        {
            "a::b::c".Split("::").Should().Equal("a", "b", "c");
        }

        [Fact]
        public void SplitWithRemoveEmptyEntriesOmitsEmptySegments()
        {
            "a,,b".Split(",", StringSplitOptions.RemoveEmptyEntries).Should().Equal("a", "b");
        }

        [Fact]
        public void SplitDefaultsToKeepingEmptySegments()
        {
            "a,,b".Split(",").Should().Equal("a", "", "b");
        }

        [Fact]
        public void ToByteArrayEncodesUsingAscii()
        {
            "abc".ToByteArray().Should().Equal(Encoding.ASCII.GetBytes("abc"));
        }

        [Fact]
        public void ToByteArrayReplacesNonAsciiCharactersWithQuestionMarks()
        {
            "café".ToByteArray().Should().Equal(Encoding.ASCII.GetBytes("café"));
            "café".ToByteArray().Last().Should().Be((byte)'?');
        }

        [Fact]
        public void ToEnumParsesAMatchingMemberName()
        {
            "Second".ToEnum<SampleEnum>().Should().Be(SampleEnum.Second);
        }

        [Fact]
        public void ToEnumIsCaseSensitiveBecauseItCallsEnumParseWithoutIgnoreCase()
        {
            var act = () => "second".ToEnum<SampleEnum>();

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToEnumThrowsForAMemberNameThatDoesNotExist()
        {
            var act = () => "NotAMember".ToEnum<SampleEnum>();

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToTitleCaseWithoutAnExplicitCultureUsesEnUs()
        {
            "hello world".ToTitleCase().Should().Be("Hello World");
        }

        [Fact]
        public void ToTitleCaseWithAnExplicitCultureUsesThatCulture()
        {
            "hello world".ToTitleCase(CultureInfo.InvariantCulture).Should().Be("Hello World");
        }

        [Fact]
        public void ToTitleCaseDoesNotLowercaseAlreadyUppercaseWords()
        {
            "HELLO world".ToTitleCase().Should().Be("HELLO World");
        }

        [Fact]
        public void ToValidDateTimeOrNullParsesAWellFormedDate()
        {
            var result = "2024-06-15".ToValidDateTimeOrNull();

            result.Should().Be(new DateTime(2024, 6, 15));
        }

        [Fact]
        public void ToValidDateTimeOrNullReturnsNullForUnparsableText()
        {
            "not a date".ToValidDateTimeOrNull().Should().BeNull();
        }

        [Fact]
        public void ToValidDateTimeOrNullReturnsNullForAnEmptyString()
        {
            "".ToValidDateTimeOrNull().Should().BeNull();
        }

        [Fact]
        public void ToXDocumentParsesWellFormedXmlIntoAnXDocument()
        {
            var document = "<root><child>value</child></root>".ToXDocument();

            document.Root!.Name.LocalName.Should().Be("root");
            document.Root.Element("child")!.Value.Should().Be("value");
        }

        [Fact]
        public void ToXDocumentThrowsForMalformedXml()
        {
            var act = () => "<root><unclosed></root>".ToXDocument();

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void ToXmlDocumentParsesWellFormedXmlIntoAnXmlDocument()
        {
            var document = "<root><child>value</child></root>".ToXmlDocument();

            document.DocumentElement!.Name.Should().Be("root");
            document.DocumentElement.SelectSingleNode("child")!.InnerText.Should().Be("value");
        }

        [Fact]
        public void ToXmlDocumentThrowsForMalformedXml()
        {
            var act = () => "<root><unclosed></root>".ToXmlDocument();

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void TruncateLeavesShortStringsUnchanged()
        {
            "short".Truncate(10).Should().Be("short");
        }

        [Fact]
        public void TruncateAppendsAnEllipsisWhenTheStringExceedsTheMaxLength()
        {
            "abcdefghij".Truncate(5).Should().Be("ab...");
        }

        [Fact]
        public void TruncateWithACustomSuffixUsesThatSuffixInstead()
        {
            "abcdefghij".Truncate(6, "~").Should().Be("abcde~");
        }

        [Fact]
        public void TruncateOnANullStringReturnsNull()
        {
            ((string?)null).Truncate(5).Should().BeNull();
        }

        [Fact]
        public void TruncateThrowsWhenTheMaxLengthIsShorterThanTheSuffixItself()
        {
            var act = () => "abcdefghij".Truncate(2);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void UrlEncodeThenUrlDecodeRoundTripsSpecialCharacters()
        {
            const string original = "a b&c=d?e";

            var roundTripped = original.UrlEncode().UrlDecode();

            roundTripped.Should().Be(original);
        }

        [Fact]
        public void UrlEncodeConvertsSpacesToPlusSigns()
        {
            "a b".UrlEncode().Should().Be("a+b");
        }

        [Fact]
        public void UrlEncodeWithAnExplicitEncodingStillEncodesSpaces()
        {
            "a b".UrlEncode(Encoding.UTF8).Should().Be("a+b");
        }

        [Fact]
        public void UrlDecodeWithAnExplicitEncodingDecodesBackToTheOriginalText()
        {
            "a+b".UrlDecode(Encoding.UTF8).Should().Be("a b");
        }

        [Fact]
        public void UrlEncodeToBytesThenUrlDecodeToBytesRoundTrips()
        {
            const string original = "hello world!";

            var bytes = original.UrlEncodeToBytes();
            var decodedBytes = Encoding.UTF8.GetString(bytes).UrlDecodeToBytes();

            Encoding.UTF8.GetString(decodedBytes).Should().Be(original);
        }

        [Fact]
        public void UrlEncodeToBytesWithAnExplicitEncodingProducesTheSameTextWhenDecoded()
        {
            var bytes = "hello".UrlEncodeToBytes(Encoding.UTF8);

            Encoding.UTF8.GetString(bytes).Should().Be("hello");
        }

        [Fact]
        public void UrlDecodeToBytesWithAnExplicitEncodingDecodesCorrectly()
        {
            var bytes = "hello".UrlDecodeToBytes(Encoding.UTF8);

            Encoding.UTF8.GetString(bytes).Should().Be("hello");
        }

        [Fact]
        public void UrlPathEncodeEncodesSpacesAsPercentTwentyRatherThanAPlusSign()
        {
            "a b".UrlPathEncode().Should().Be("a%20b");
        }

        [Theory]
        [InlineData("kitten", "sitting", 3)]
        [InlineData("abc", "abc", 0)]
        [InlineData("", "abc", 3)]
        [InlineData("abc", "", 3)]
        [InlineData(null, "abc", 3)]
        [InlineData("abc", null, 3)]
        public void LevenshteinDistanceComputesTheEditDistance(string? source, string? target, int expected)
        {
            source.LevenshteinDistance(target).Should().Be(expected);
        }

        [Fact]
        public void SimilarityToOfIdenticalStringsIsOne()
        {
            "hello".SimilarityTo("hello").Should().Be(1.0);
            "".SimilarityTo("").Should().Be(1.0);
        }

        [Fact]
        public void SimilarityToNormalisesTheEditDistanceByTheLongerStringLength()
        {
            "kitten".SimilarityTo("sitting").Should().BeApproximately(1.0 - 3.0 / 7.0, 0.0001);
        }

        [Fact]
        public void JaroWinklerSimilarityOfIdenticalStringsIsOne()
        {
            "hello".JaroWinklerSimilarity("hello").Should().Be(1.0);
            "".JaroWinklerSimilarity("").Should().Be(1.0);
        }

        [Fact]
        public void JaroWinklerSimilarityOfAnEmptyAndNonEmptyStringIsZero()
        {
            "".JaroWinklerSimilarity("abc").Should().Be(0.0);
            "abc".JaroWinklerSimilarity("").Should().Be(0.0);
        }

        [Fact]
        public void JaroWinklerSimilarityMatchesTheWellKnownMarthaMarhtaExample()
        {
            "MARTHA".JaroWinklerSimilarity("MARHTA").Should().BeApproximately(0.9611, 0.0001);
        }

        [Fact]
        public void JaroWinklerSimilarityMatchesTheWellKnownDixonDicksonxExample()
        {
            "DIXON".JaroWinklerSimilarity("DICKSONX").Should().BeApproximately(0.8133, 0.0001);
        }

        [Theory]
        [InlineData("Robert", "R163")]
        [InlineData("Rupert", "R163")]
        [InlineData("robert", "R163")]
        [InlineData("A", "A000")]
        public void SoundexEncodesNamesToTheirPhoneticCode(string input, string expected)
        {
            input.Soundex().Should().Be(expected);
        }

        [Fact]
        public void SoundexOfSimilarSoundingNamesIsTheSame()
        {
            "Robert".Soundex().Should().Be("Rupert".Soundex());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123")]
        public void SoundexOfBlankOrLetterlessInputIsEmpty(string? input)
        {
            input.Soundex().Should().Be("");
        }

        [Fact]
        public void ShannonEntropyOfAllSameCharacterIsZero()
        {
            "aaaa".ShannonEntropy().Should().Be(0.0);
        }

        [Fact]
        public void ShannonEntropyOfTwoEquallyLikelyCharactersIsOneBit()
        {
            "abab".ShannonEntropy().Should().BeApproximately(1.0, 0.0001);
        }

        [Fact]
        public void ShannonEntropyOfFourEquallyLikelyDistinctCharactersIsTwoBits()
        {
            "abcd".ShannonEntropy().Should().BeApproximately(2.0, 0.0001);
        }

        [Fact]
        public void ShannonEntropyOfAnEmptyOrNullStringIsZero()
        {
            "".ShannonEntropy().Should().Be(0.0);
            ((string?)null).ShannonEntropy().Should().Be(0.0);
        }

        [Theory]
        [InlineData("4111111111111111", 4, "************1111")]
        [InlineData("123", 5, "123")]
        [InlineData("12345", 0, "*****")]
        [InlineData("12345", -1, "*****")]
        public void MaskExceptLastMasksEverythingButTheTrailingVisibleCount(string input, int visibleCount,
            string expected)
        {
            input.MaskExceptLast(visibleCount).Should().Be(expected);
        }

        [Fact]
        public void MaskExceptLastAcceptsACustomMaskCharacter()
        {
            "4111111111111111".MaskExceptLast(4, '#').Should().Be("############1111");
        }

        [Theory]
        [InlineData("user+promo1@gmail.com", "user@gmail.com")]
        [InlineData("User+Promo2@Gmail.com", "user@gmail.com")]
        [InlineData("plainuser@gmail.com", "plainuser@gmail.com")]
        [InlineData("  User@Gmail.com  ", "user@gmail.com")]
        [InlineData("notanemail", "notanemail")]
        public void NormalizeEmailAliasStripsPlusTaggingAndLowercases(string input, string expected)
        {
            input.NormalizeEmailAlias().Should().Be(expected);
        }

        private enum SampleEnum
        {
            Second
        }
    }
}