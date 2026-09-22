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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary.Fuzzy;
using Xunit;

namespace Jube.Test.Dictionary.Fuzzy
{
    [Trait("Category", "Unit")]
    public sealed class FuzzyListMatcherTests
    {
        private static List<string> Names() =>
            ["John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd", "José Müller"];

        private static IEnumerable<string> Streamed(params string[] values)
        {
            foreach (var value in values)
            {
                yield return value;
            }
        }

        [Fact]
        public void NothingMatchesForANullValueNullListOrInvalidOptions()
        {
            FuzzyListMatcher.Evaluate(null, Names(), FuzzyOptions.Default).Should().Be(FuzzyResult.None);
            FuzzyListMatcher.Evaluate("John Smith", null, FuzzyOptions.Default).Should().Be(FuzzyResult.None);
            FuzzyListMatcher.Evaluate("John Smith", Names(), FuzzyOptions.Parse("bogus")).Should().Be(FuzzyResult.None);
        }

        [Fact]
        public void AnEmptyListOrBlankValueNeverMatches()
        {
            FuzzyListMatcher.Any("John Smith", new List<string>(), FuzzyOptions.Default).Should().BeFalse();
            FuzzyListMatcher.Any("", Names(), FuzzyOptions.Default).Should().BeFalse();
            FuzzyListMatcher.Any("   ", Names(), FuzzyOptions.Default).Should().BeFalse();
            FuzzyListMatcher.Any("--", Names(), FuzzyOptions.Default).Should().BeFalse();
        }

        [Fact]
        public void BlankAndNullEntriesInTheListAreIgnored()
        {
            var list = new List<string> { "", "  ", null!, "John Smith" };

            FuzzyListMatcher.Evaluate("John Smith", list, FuzzyOptions.Parse("exact")).Count.Should().Be(1);
        }

        [Fact]
        public void AnExactMatchIsFoundAfterNormalisation()
        {
            FuzzyListMatcher.Any("  JOHN   smith ", Names(), FuzzyOptions.Parse("exact")).Should().BeTrue();
            FuzzyListMatcher.Any("Jon Smith", Names(), FuzzyOptions.Parse("exact")).Should().BeFalse();
        }

        [Fact]
        public void DiacriticsAreFoldedOnBothSides()
        {
            FuzzyListMatcher.Any("Jose Muller", Names(), FuzzyOptions.Parse("exact")).Should().BeTrue();
            FuzzyListMatcher.Any("JOSÉ MÜLLER", Names(), FuzzyOptions.Parse("exact")).Should().BeTrue();
        }

        [Fact]
        public void DiacriticsCanBeKeptSoThatTheyDiffer()
        {
            FuzzyListMatcher.Any("Jose Muller", Names(), FuzzyOptions.Parse("exact,diacritics")).Should().BeFalse();
        }

        [Fact]
        public void CaseCanBeMadeSignificant()
        {
            FuzzyListMatcher.Any("john smith", Names(), FuzzyOptions.Parse("exact,case")).Should().BeFalse();
            FuzzyListMatcher.Any("John Smith", Names(), FuzzyOptions.Parse("exact,case")).Should().BeTrue();
        }

        [Fact]
        public void PunctuationDoesNotPreventAMatchByDefault()
        {
            FuzzyListMatcher.Any("Ahmed Al Rashid", Names(), FuzzyOptions.Parse("exact")).Should().BeTrue();
            FuzzyListMatcher.Any("Ahmed Al Rashid", Names(), FuzzyOptions.Parse("exact,punct")).Should().BeFalse();
        }

        [Fact]
        public void TitlesAndCompanySuffixesCanBeIgnored()
        {
            FuzzyListMatcher.Any("Dr John Smith", Names(), FuzzyOptions.Parse("exact")).Should().BeFalse();
            FuzzyListMatcher.Any("Dr John Smith", Names(), FuzzyOptions.Parse("exact,notitles")).Should().BeTrue();
            FuzzyListMatcher.Any("Acme Trading Inc", Names(), FuzzyOptions.Parse("exact,nosuffixes")).Should().BeTrue();
        }

        [Fact]
        public void SpacesCanBeIgnoredSoRunTogetherNamesMatch()
        {
            FuzzyListMatcher.Any("JohnSmith", Names(), FuzzyOptions.Parse("exact,nospaces")).Should().BeTrue();
            FuzzyListMatcher.Any("JohnSmith", Names(), FuzzyOptions.Parse("exact")).Should().BeFalse();
        }

        [Fact]
        public void CountReportsEveryDistinctEntryThatMatches()
        {
            var list = new List<string> { "John Smith", "john smith", "JOHN  SMITH", "Bob Marley", "Jon Smith" };

            FuzzyListMatcher.Evaluate("John Smith", list, FuzzyOptions.Parse("exact")).Count.Should().Be(3);
            FuzzyListMatcher.Evaluate("John Smith", list, FuzzyOptions.Parse("lev=1")).Count.Should().Be(4);
        }

        [Fact]
        public void StopAtFirstReturnsAsSoonAsOneEntryMatches()
        {
            var list = new List<string> { "John Smith", "john smith", "JOHN  SMITH" };

            FuzzyListMatcher.Evaluate("John Smith", list, FuzzyOptions.Parse("exact"), true).Count.Should().Be(1);
        }

        [Fact]
        public void BestEntryIsTheOriginalTextOfTheHighestScoringEntry()
        {
            var list = new List<string> { "Jon Smyth", "JOHN SMITH", "Bob Marley" };

            var result = FuzzyListMatcher.Evaluate("John Smith", list, FuzzyOptions.Default);

            result.BestEntry.Should().Be("JOHN SMITH");
            result.BestScore.Should().Be(1d);
        }

        [Fact]
        public void BestScoreIsReportedEvenWhenNothingPassesTheThreshold()
        {
            var result = FuzzyListMatcher.Evaluate("Jon Smith", Names(), FuzzyOptions.Parse("jw=1"));

            result.Count.Should().Be(0);
            result.BestScore.Should().BeGreaterThan(0.9);
            result.BestEntry.Should().Be("John Smith");
        }

        [Fact]
        public void ACompletelyUnrelatedValueHasNoBestEntry()
        {
            var result = FuzzyListMatcher.Evaluate("Qqqq", new List<string> { "Zzzz" }, FuzzyOptions.Parse("exact"));

            result.Count.Should().Be(0);
            result.BestScore.Should().Be(0d);
            result.BestEntry.Should().BeNull();
        }

        [Fact]
        public void MinLengthSkipsValuesThatAreTooShortToBeTrusted()
        {
            var list = new List<string> { "Li", "Wu" };

            FuzzyListMatcher.Any("Li", list, FuzzyOptions.Parse("exact")).Should().BeTrue();
            FuzzyListMatcher.Any("Li", list, FuzzyOptions.Parse("exact,minlength=3")).Should().BeFalse();
        }

        [Fact]
        public void MinLengthIsMeasuredAfterNormalisation()
        {
            FuzzyListMatcher.Any("L.i.", new List<string> { "Li" }, FuzzyOptions.Parse("exact,minlength=3"))
                .Should().BeFalse();
        }

        [Fact]
        public void FirstLetterBlockingExcludesEntriesStartingWithADifferentLetter()
        {
            var list = new List<string> { "john smith" };

            FuzzyListMatcher.Any("kohn smith", list, FuzzyOptions.Parse("lev=1")).Should().BeTrue();
            FuzzyListMatcher.Any("kohn smith", list, FuzzyOptions.Parse("lev=1,firstletter")).Should().BeFalse();
            FuzzyListMatcher.Any("jhon smith", list, FuzzyOptions.Parse("damerau=1,firstletter")).Should().BeTrue();
        }

        [Fact]
        public void AnyPassesWhenOneAlgorithmPassesButAllNeedsEvery()
        {
            var list = new List<string> { "John Smith" };

            FuzzyListMatcher.Any("Jon Smith", list, FuzzyOptions.Parse("jw=0.9,exact")).Should().BeTrue();
            FuzzyListMatcher.Any("Jon Smith", list, FuzzyOptions.Parse("jw=0.9,exact,all")).Should().BeFalse();
            FuzzyListMatcher.Any("John Smith", list, FuzzyOptions.Parse("jw=0.9,exact,all")).Should().BeTrue();
        }

        [Fact]
        public void AllScoresAsTheWeakestAlgorithmAndAnyAsTheStrongest()
        {
            var list = new List<string> { "John Smith" };

            var any = FuzzyListMatcher.Evaluate("Jon Smith", list, FuzzyOptions.Parse("jw,exact"));
            var all = FuzzyListMatcher.Evaluate("Jon Smith", list, FuzzyOptions.Parse("jw,exact,all"));

            any.BestScore.Should().BeGreaterThan(0.9);
            all.BestScore.Should().Be(0d);
        }

        [Fact]
        public void WithinFindsAListEntryInsideLongerText()
        {
            var options = FuzzyOptions.Parse("within,exact");

            FuzzyListMatcher.Any("Payment to John Smith today", Names(), options).Should().BeTrue();
            FuzzyListMatcher.Any("Payment to Bob Marley today", Names(), options).Should().BeFalse();
        }

        [Fact]
        public void WithinToleratesATypoInsideLongerText()
        {
            FuzzyListMatcher.Any("Payment to Jon Smyth today", Names(), FuzzyOptions.Parse("within,lev=2"))
                .Should().BeTrue();
            FuzzyListMatcher.Any("Payment to Jon Smyth today", Names(), FuzzyOptions.Parse("within,lev=1"))
                .Should().BeFalse();
        }

        [Fact]
        public void WithinNeverMatchesWhenTheTextIsShorterThanTheEntry()
        {
            FuzzyListMatcher.Any("John", Names(), FuzzyOptions.Parse("within,exact")).Should().BeFalse();
        }

        [Fact]
        public void WithinAtTheStartAndEndOfTheTextMatches()
        {
            var options = FuzzyOptions.Parse("within,exact");

            FuzzyListMatcher.Any("John Smith paid", Names(), options).Should().BeTrue();
            FuzzyListMatcher.Any("paid John Smith", Names(), options).Should().BeTrue();
            FuzzyListMatcher.Any("John Smith", Names(), options).Should().BeTrue();
        }

        [Fact]
        public void WithinDoesNotMatchInsideAWord()
        {
            FuzzyListMatcher.Any("Johnson Smithers", new List<string> { "John Smith" },
                FuzzyOptions.Parse("within,exact")).Should().BeFalse();
        }

        [Fact]
        public void WithinWithSpacesRemovedSearchesCharactersInsideTheText()
        {
            FuzzyListMatcher.Any("Payment to John Smith today", Names(), FuzzyOptions.Parse("within,exact,nospaces"))
                .Should().BeTrue();
            FuzzyListMatcher.Any("Pay", Names(), FuzzyOptions.Parse("within,exact,nospaces")).Should().BeFalse();
        }

        [Fact]
        public void WithinReportsTheBestEntryFound()
        {
            var result = FuzzyListMatcher.Evaluate("Wire from Maria Garcia", Names(), FuzzyOptions.Parse("within,exact"));

            result.BestEntry.Should().Be("Maria Garcia");
            result.Count.Should().Be(1);
        }

        [Fact]
        public void AStreamedNonListEnumerableWorks()
        {
            var streamed = Streamed("John Smith", "Maria Garcia");

            FuzzyListMatcher.Any("Jon Smith", streamed, FuzzyOptions.Default).Should().BeTrue();
            FuzzyListMatcher.Any("Jon Smith", Streamed("John Smith"), FuzzyOptions.Default).Should().BeTrue();
        }

        [Fact]
        public void AnArrayIsTreatedAsAList()
        {
            FuzzyListMatcher.Any("Jon Smith", new[] { "John Smith" }, FuzzyOptions.Default).Should().BeTrue();
        }

        [Fact]
        public void AListChangedAfterAFirstEvaluationIsReRead()
        {
            var list = new List<string> { "John Smith" };
            var options = FuzzyOptions.Parse("exact");

            FuzzyListMatcher.Any("Maria Garcia", list, options).Should().BeFalse();

            list.Add("Maria Garcia");

            FuzzyListMatcher.Any("Maria Garcia", list, options).Should().BeTrue();
        }

        [Fact]
        public void ARemovedEntryStopsMatching()
        {
            var list = new List<string> { "John Smith", "Maria Garcia" };
            var options = FuzzyOptions.Parse("exact");

            FuzzyListMatcher.Any("Maria Garcia", list, options).Should().BeTrue();

            list.RemoveAt(1);

            FuzzyListMatcher.Any("Maria Garcia", list, options).Should().BeFalse();
        }

        [Fact]
        public void ReplacingAFirstEntryInPlaceIsDetected()
        {
            var list = new List<string> { "John Smith", "Maria Garcia", "Bob Marley" };
            var options = FuzzyOptions.Parse("exact");

            FuzzyListMatcher.Any("John Smith", list, options).Should().BeTrue();

            list[0] = "Zed Zebra";

            FuzzyListMatcher.Any("John Smith", list, options).Should().BeFalse();
            FuzzyListMatcher.Any("Zed Zebra", list, options).Should().BeTrue();
        }

        [Fact]
        public void TheSameListEvaluatedUnderDifferentNormalisationsGivesEachItsOwnAnswer()
        {
            var list = new List<string> { "John Smith" };

            FuzzyListMatcher.Any("john smith", list, FuzzyOptions.Parse("exact,case")).Should().BeFalse();
            FuzzyListMatcher.Any("john smith", list, FuzzyOptions.Parse("exact")).Should().BeTrue();
            FuzzyListMatcher.Any("john smith", list, FuzzyOptions.Parse("exact,case")).Should().BeFalse();
        }

        [Fact]
        public void TwoDifferentListsNeverShareCachedEntries()
        {
            var first = new List<string> { "John Smith" };
            var second = new List<string> { "Maria Garcia" };
            var options = FuzzyOptions.Parse("exact");

            FuzzyListMatcher.Any("John Smith", first, options).Should().BeTrue();
            FuzzyListMatcher.Any("John Smith", second, options).Should().BeFalse();
        }

        [Fact]
        public void ConcurrentEvaluationOfOneListIsConsistent()
        {
            var list = Names();
            var results = new bool[400];

            Parallel.For(0, results.Length, i =>
            {
                results[i] = FuzzyListMatcher.Any(i % 2 == 0 ? "Jon Smith" : "Bob Marley", list, FuzzyOptions.Default);
            });

            results.Where((_, i) => i % 2 == 0).Should().OnlyContain(x => x);
            results.Where((_, i) => i % 2 == 1).Should().OnlyContain(x => !x);
        }

        [Fact]
        public void MatchAlgorithmsCombinesScoresPerTheRequireAllSetting()
        {
            var any = FuzzyListMatcher.MatchAlgorithms("jon smith", "john smith", FuzzyOptions.Parse("jw,exact"));
            var all = FuzzyListMatcher.MatchAlgorithms("jon smith", "john smith", FuzzyOptions.Parse("jw,exact,all"));

            any.Passed.Should().BeTrue();
            all.Passed.Should().BeFalse();
        }

        [Fact]
        public void ALargeListIsScannedAndTheMatchFound()
        {
            var list = Enumerable.Range(0, 20000).Select(i => "Person Number" + i).ToList();
            list.Add("Jon Smith");

            FuzzyListMatcher.Any("John Smith", list, FuzzyOptions.Default).Should().BeTrue();
        }
    }
}
