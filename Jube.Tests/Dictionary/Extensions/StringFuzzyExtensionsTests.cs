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

using System.Collections.Generic;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class StringFuzzyExtensionsTests
    {
        private static readonly List<string> names =
            ["John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd"];

        [Theory]
        [InlineData("John Smith", true)]
        [InlineData("john smith", true)]
        [InlineData("Jon Smith", true)]
        [InlineData("Smith John", true)]
        [InlineData("J. Smith", false)]
        [InlineData("Bob Marley", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSomehowInListUsesTheDefaultProfile(string? value, bool expected)
        {
            value.IsSomehowInList(names).Should().Be(expected);
        }

        [Fact]
        public void IsSomehowInListIsFalseForANullList()
        {
            "John Smith".IsSomehowInList(null).Should().BeFalse();
            "John Smith".IsSomehowInList(null, "jw").Should().BeFalse();
        }

        [Fact]
        public void IsSomehowInListAcceptsAnOptionsString()
        {
            "Jon Smyth".IsSomehowInList(names, "lev=2").Should().BeTrue();
            "Jon Smyth".IsSomehowInList(names, "lev=1").Should().BeFalse();
            "J Smith".IsSomehowInList(names, "initials").Should().BeTrue();
            "Jon Smyth".IsSomehowInList(names, "soundex").Should().BeTrue();
        }

        [Fact]
        public void AnInvalidOptionsStringFailsClosed()
        {
            "John Smith".IsSomehowInList(names, "nonsense").Should().BeFalse();
            "John Smith".IsSomehowInList(names, "jw=5").Should().BeFalse();
        }

        [Fact]
        public void ANullOrBlankOptionsStringMeansTheDefaultProfile()
        {
            "Jon Smith".IsSomehowInList(names, null).Should().BeTrue();
            "Jon Smith".IsSomehowInList(names, "").Should().BeTrue();
        }

        [Fact]
        public void IsSomehowLikeComparesToASingleValue()
        {
            "Jon Smith".IsSomehowLike("John Smith").Should().BeTrue();
            "Bob".IsSomehowLike("John Smith").Should().BeFalse();
            "Jon Smyth".IsSomehowLike("John Smith", "lev=2").Should().BeTrue();
            "Jon Smyth".IsSomehowLike("John Smith", "lev=1").Should().BeFalse();
            "Jon".IsSomehowLike(null).Should().BeFalse();
            ((string?)null).IsSomehowLike("John").Should().BeFalse();
        }

        [Fact]
        public void ContainsSomehowInListFindsEntriesInsideLongerText()
        {
            "Payment to Jon Smith today".ContainsSomehowInList(names).Should().BeTrue();
            "Payment to Bob Marley".ContainsSomehowInList(names).Should().BeFalse();
            "Payment to Jon Smyth".ContainsSomehowInList(names, "lev=2").Should().BeTrue();
            "Payment to Jon Smyth".ContainsSomehowInList(names, "lev=1").Should().BeFalse();
        }

        [Fact]
        public void ContainsSomehowInListForcesWithinScopeWhateverTheOptionsSay()
        {
            "Payment to John Smith".ContainsSomehowInList(names, "whole,exact").Should().BeTrue();
            "Payment to John Smith".ContainsSomehowInList(names, "exact").Should().BeTrue();
            "Payment to John Smith".IsSomehowInList(names, "exact").Should().BeFalse();
        }

        [Fact]
        public void FuzzyBestScoreIsTheHighestScoreAcrossTheList()
        {
            "John Smith".FuzzyBestScore(names).Should().Be(1d);
            "Jon Smith".FuzzyBestScore(names).Should().BeGreaterThan(0.9).And.BeLessThan(1d);
            "Jon Smith".FuzzyBestScore(names, "similarity").Should().BeApproximately(0.9, 0.0001);
            "Qqqqq".FuzzyBestScore(names, "exact").Should().Be(0d);
            ((string?)null).FuzzyBestScore(names).Should().Be(0d);
            "John".FuzzyBestScore(null).Should().Be(0d);
        }

        [Fact]
        public void FuzzyBestScoreCanBeComparedInARule()
        {
            ("Jon Smith".FuzzyBestScore(names, "jw") > 0.9).Should().BeTrue();
            ("Bob Marley".FuzzyBestScore(names, "jw") > 0.9).Should().BeFalse();
        }

        [Fact]
        public void FuzzyMatchCountCountsEntriesThatPass()
        {
            var list = new List<string> { "John Smith", "john smith", "Jon Smith", "Bob" };

            "John Smith".FuzzyMatchCount(list, "exact").Should().Be(2);
            "John Smith".FuzzyMatchCount(list, "lev=1").Should().Be(3);
            "John Smith".FuzzyMatchCount(list).Should().Be(3);
            ((string?)null).FuzzyMatchCount(list).Should().Be(0);
        }

        [Fact]
        public void FuzzyBestMatchReturnsTheOriginalEntryOrNull()
        {
            "Jon Smith".FuzzyBestMatch(names).Should().Be("John Smith");
            "Maria Garcya".FuzzyBestMatch(names, "lev=2").Should().Be("Maria Garcia");
            "Zzzz".FuzzyBestMatch(names, "exact").Should().BeNull();
            ((string?)null).FuzzyBestMatch(names).Should().BeNull();
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("jw=0.9,notitles,within", true)]
        [InlineData("jw=9", false)]
        [InlineData("wibble", false)]
        public void IsValidFuzzyOptionsChecksAnOptionsStringWithoutRunningIt(string? options, bool expected)
        {
            options.IsValidFuzzyOptions().Should().Be(expected);
        }

        [Fact]
        public void FuzzyOptionsErrorExplainsWhatIsWrong()
        {
            "wibble".FuzzyOptionsError().Should().Contain("wibble");
            "jw=0.9".FuzzyOptionsError().Should().BeEmpty();
            ((string?)null).FuzzyOptionsError().Should().BeEmpty();
        }

        [Fact]
        public void IsNormalisedInListIsAnExactMatchAfterNormalisation()
        {
            "  JOHN  smith ".IsNormalisedInList(names).Should().BeTrue();
            "Jon Smith".IsNormalisedInList(names).Should().BeFalse();
        }

        [Fact]
        public void IsLevenshteinInListHonoursTheEditBudget()
        {
            "Jon Smith".IsLevenshteinInList(names, 1).Should().BeTrue();
            "Jon Smyth".IsLevenshteinInList(names, 1).Should().BeFalse();
            "Jon Smyth".IsLevenshteinInList(names, 2).Should().BeTrue();
            "John Smith".IsLevenshteinInList(names, 0).Should().BeTrue();
        }

        [Fact]
        public void AnOutOfRangeDistanceFailsClosed()
        {
            "John Smith".IsLevenshteinInList(names, 11).Should().BeFalse();
            "John Smith".IsLevenshteinInList(names, -1).Should().BeFalse();
        }

        [Fact]
        public void IsDamerauInListTreatsATranspositionAsOneEdit()
        {
            "Jhon Smith".IsDamerauInList(names, 1).Should().BeTrue();
            "Jhon Smith".IsLevenshteinInList(names, 1).Should().BeFalse();
        }

        [Fact]
        public void IsSimilarInListUsesTheLevenshteinRatio()
        {
            "Jon Smith".IsSimilarInList(names, 0.85).Should().BeTrue();
            "Jon Smith".IsSimilarInList(names, 0.95).Should().BeFalse();
            "John Smith".IsSimilarInList(names, 1).Should().BeTrue();
            "John Smith".IsSimilarInList(names, 0).Should().BeFalse();
        }

        [Fact]
        public void IsJaroWinklerInListUsesThePrefixWeightedScore()
        {
            "Jon Smith".IsJaroWinklerInList(names, 0.95).Should().BeTrue();
            "Bob Marley".IsJaroWinklerInList(names, 0.95).Should().BeFalse();
        }

        [Fact]
        public void IsDiceInListUsesBigramOverlap()
        {
            "Jon Smith".IsDiceInList(names, 0.8).Should().BeTrue();
            "Jon Smith".IsDiceInList(names, 0.9).Should().BeFalse();
        }

        [Fact]
        public void IsSoundexInListMatchesNamesThatSoundAlike()
        {
            "Jon Smyth".IsSoundexInList(names).Should().BeTrue();
            "Bob Marley".IsSoundexInList(names).Should().BeFalse();
        }

        [Fact]
        public void IsTokenSortInListIgnoresWordOrder()
        {
            "Smith John".IsTokenSortInList(names, 0.95).Should().BeTrue();
            "Smith Bob".IsTokenSortInList(names, 0.95).Should().BeFalse();
            "Garcia Maria".IsTokenSortInList(names, 1).Should().BeTrue();
        }

        [Fact]
        public void IsTokenSetInListToleratesMissingWords()
        {
            "John".IsTokenSetInList(names, 0.5).Should().BeTrue();
            "John".IsTokenSetInList(names, 0.6).Should().BeFalse();
        }

        [Fact]
        public void IsInitialsInListMatchesAbbreviatedFirstNames()
        {
            "J Smith".IsInitialsInList(names).Should().BeTrue();
            "J. Smith".IsInitialsInList(names).Should().BeTrue();
            "M Garcia".IsInitialsInList(names).Should().BeTrue();
            "J S".IsInitialsInList(names).Should().BeFalse();
            "K Smith".IsInitialsInList(names).Should().BeFalse();
        }

        [Fact]
        public void EveryNamedAlgorithmIsFalseForNullValuesAndNullLists()
        {
            ((string?)null).IsNormalisedInList(names).Should().BeFalse();
            ((string?)null).IsLevenshteinInList(names, 1).Should().BeFalse();
            ((string?)null).IsDamerauInList(names, 1).Should().BeFalse();
            ((string?)null).IsSimilarInList(names, 0.5).Should().BeFalse();
            ((string?)null).IsJaroWinklerInList(names, 0.5).Should().BeFalse();
            ((string?)null).IsDiceInList(names, 0.5).Should().BeFalse();
            ((string?)null).IsSoundexInList(names).Should().BeFalse();
            ((string?)null).IsTokenSortInList(names, 0.5).Should().BeFalse();
            ((string?)null).IsTokenSetInList(names, 0.5).Should().BeFalse();
            ((string?)null).IsInitialsInList(names).Should().BeFalse();

            "John".IsNormalisedInList(null).Should().BeFalse();
            "John".IsSoundexInList(null).Should().BeFalse();
            "John".IsInitialsInList(null).Should().BeFalse();
        }

        [Fact]
        public void TheUserFacingExampleReadsNaturally()
        {
            var name = "Jon Smith";

            name.IsSomehowInList(names).Should().BeTrue();
        }

        [Fact]
        public void MiddleInitialsAndMissingMiddleNamesAreHandledByTheRightAlgorithm()
        {
            var list = new List<string> { "John Quincy Smith" };

            "John Q Smith".IsInitialsInList(list).Should().BeTrue();
            "John Smith".IsTokenSetInList(list, 0.6).Should().BeTrue();
            "John Smith".IsTokenSetInList(list, 0.9).Should().BeFalse();
        }

        [Fact]
        public void CompanyNamesMatchAcrossLegalSuffixes()
        {
            "Acme Trading Limited".IsSomehowInList(names, "nosuffixes,exact").Should().BeTrue();
            "ACME TRADING, INC.".IsSomehowInList(names, "nosuffixes,exact").Should().BeTrue();
            "Acme Trading".IsSomehowInList(names, "nosuffixes,exact").Should().BeTrue();
            "Acme Holdings".IsSomehowInList(names, "nosuffixes,exact").Should().BeFalse();
        }

        [Fact]
        public void ATransliteratedNameMatchesAcrossDiacritics()
        {
            var list = new List<string> { "Zoë Müller", "Søren Kierkegaard" };

            "Zoe Muller".IsSomehowInList(list, "exact").Should().BeTrue();
            "Soren Kierkegaard".IsSomehowInList(list, "exact").Should().BeTrue();
        }

        [Fact]
        public void HyphenatedAndApostropheNamesMatchTheirPlainForms()
        {
            var list = new List<string> { "Mary Smith-Jones", "Sean O'Brien" };

            "Mary Smith Jones".IsSomehowInList(list, "exact").Should().BeTrue();
            "Sean OBrien".IsSomehowInList(list, "exact").Should().BeTrue();
            "Sean O’Brien".IsSomehowInList(list, "exact").Should().BeTrue();
        }
    }
}
