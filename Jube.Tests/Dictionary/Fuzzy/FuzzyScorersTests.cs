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

using FluentAssertions;
using Jube.Dictionary.Fuzzy;
using Xunit;

namespace Jube.Test.Dictionary.Fuzzy
{
    [Trait("Category", "Unit")]
    public sealed class FuzzyScorersTests
    {
        [Theory]
        [InlineData("abc", "abc", 1d)]
        [InlineData("abc", "abd", 0d)]
        [InlineData("", "", 1d)]
        [InlineData("abc", "ABC", 0d)]
        public void ExactIsAllOrNothing(string a, string b, double expected)
        {
            FuzzyScorers.Exact(a, b).Should().Be(expected);
        }

        [Theory]
        [InlineData("kitten", "sitting", 3)]
        [InlineData("", "abc", 3)]
        [InlineData("abc", "", 3)]
        [InlineData("abc", "abc", 0)]
        [InlineData("ab", "ba", 2)]
        public void LevenshteinCountsInsertionsDeletionsAndSubstitutions(string a, string b, int expected)
        {
            FuzzyScorers.LevenshteinDistance(a, b).Should().Be(expected);
        }

        [Theory]
        [InlineData("ab", "ba", 1)]
        [InlineData("abcd", "acbd", 1)]
        [InlineData("john", "jhon", 1)]
        [InlineData("kitten", "sitting", 3)]
        [InlineData("ca", "abc", 3)]
        [InlineData("", "", 0)]
        [InlineData("abc", "", 3)]
        [InlineData("", "abc", 3)]
        [InlineData("abc", "abc", 0)]
        public void DamerauCountsAnAdjacentTranspositionAsOneEdit(string a, string b, int expected)
        {
            FuzzyScorers.DamerauDistance(a, b).Should().Be(expected);
        }

        [Fact]
        public void DamerauIsNeverWorseThanLevenshtein()
        {
            var words = new[] { "smith", "smyth", "simth", "john", "jhon", "jonh", "garcia", "gracia" };

            foreach (var a in words)
            {
                foreach (var b in words)
                {
                    FuzzyScorers.DamerauDistance(a, b).Should()
                        .BeLessThanOrEqualTo(FuzzyScorers.LevenshteinDistance(a, b));
                }
            }
        }

        [Theory]
        [InlineData(0, "abc", "abc", 1d)]
        [InlineData(1, "abc", "abd", 0.6667)]
        [InlineData(3, "abc", "xyz", 0d)]
        [InlineData(0, "", "", 1d)]
        [InlineData(5, "ab", "cd", 0d)]
        public void DistanceScoreIsOneMinusTheDistanceOverTheLongerLength(int distance, string a, string b,
            double expected)
        {
            FuzzyScorers.DistanceScore(distance, a, b).Should().BeApproximately(expected, 0.0001);
        }

        [Theory]
        [InlineData("abc", "abc", 1d)]
        [InlineData("abc", "xyz", 0d)]
        [InlineData("", "", 1d)]
        [InlineData("jon smith", "john smith", 0.9)]
        public void SimilarityIsTheLevenshteinRatio(string a, string b, double expected)
        {
            FuzzyScorers.Similarity(a, b).Should().BeApproximately(expected, 0.0001);
        }

        [Fact]
        public void JaroWinklerRewardsACommonPrefix()
        {
            FuzzyScorers.JaroWinkler("MARTHA", "MARHTA").Should().BeApproximately(0.9611, 0.0001);
            FuzzyScorers.JaroWinkler("abc", "abc").Should().Be(1d);
            FuzzyScorers.JaroWinkler("abc", "xyz").Should().Be(0d);
        }

        [Theory]
        [InlineData("night", "nacht", 0.25)]
        [InlineData("abc", "abc", 1d)]
        [InlineData("a", "a", 1d)]
        [InlineData("a", "b", 0d)]
        [InlineData("", "", 1d)]
        [InlineData("ab", "", 0d)]
        [InlineData("abab", "abab", 1d)]
        public void DiceComparesBigramMultisets(string a, string b, double expected)
        {
            FuzzyScorers.Dice(a, b).Should().BeApproximately(expected, 0.0001);
        }

        [Fact]
        public void DiceIsSymmetricAndCountsRepeatedBigramsOnce()
        {
            FuzzyScorers.Dice("aaaa", "aa").Should().BeApproximately(FuzzyScorers.Dice("aa", "aaaa"), 0.0001);
            FuzzyScorers.Dice("aaaa", "aa").Should().BeApproximately(2d / 4d, 0.0001);
        }

        [Theory]
        [InlineData("robert", "rupert", 1d)]
        [InlineData("john smith", "jon smyth", 1d)]
        [InlineData("john smith", "smith john", 0d)]
        [InlineData("john", "john smith", 0d)]
        [InlineData("", "", 0d)]
        [InlineData("123", "123", 1d)]
        [InlineData("123", "124", 0d)]
        [InlineData("robert", "smith", 0d)]
        public void SoundexRequiresEveryTokenToSoundTheSameInOrder(string a, string b, double expected)
        {
            FuzzyScorers.Soundex(a, b).Should().Be(expected);
        }

        [Theory]
        [InlineData("smith john", "john smith", 1d)]
        [InlineData("john smith", "john smith", 1d)]
        [InlineData("garcia maria jose", "jose maria garcia", 1d)]
        [InlineData("", "", 1d)]
        public void TokenSortIgnoresTheOrderOfWords(string a, string b, double expected)
        {
            FuzzyScorers.TokenSort(a, b).Should().Be(expected);
        }

        [Fact]
        public void TokenSortStillPenalisesDifferentWords()
        {
            FuzzyScorers.TokenSort("smith bob", "john smith").Should().BeLessThan(0.9);
        }

        [Theory]
        [InlineData("john", "john smith", 0.5)]
        [InlineData("john smith", "john", 0.5)]
        [InlineData("john smith", "john smith", 1d)]
        [InlineData("smith john", "john smith", 1d)]
        [InlineData("", "", 1d)]
        [InlineData("john", "", 0d)]
        public void TokenSetAveragesTheBestMatchOfEachTokenOverTheLargerSet(string a, string b, double expected)
        {
            FuzzyScorers.TokenSet(a, b).Should().BeApproximately(expected, 0.0001);
        }

        [Fact]
        public void TokenSetUsesEachTokenOnlyOnce()
        {
            FuzzyScorers.TokenSet("john john", "john smith").Should().BeLessThan(0.75);
        }

        [Theory]
        [InlineData("j smith", "john smith", 1d)]
        [InlineData("john smith", "j smith", 1d)]
        [InlineData("john smith", "john smith", 1d)]
        [InlineData("j s", "john smith", 0d)]
        [InlineData("k smith", "john smith", 0d)]
        [InlineData("j smith", "john", 0d)]
        [InlineData("j", "john", 0d)]
        [InlineData("", "", 0d)]
        [InlineData("john q smith", "john quincy smith", 1d)]
        public void InitialsMatchAnAbbreviatedTokenButNeedAFullTokenToAnchorIt(string a, string b, double expected)
        {
            FuzzyScorers.Initials(a, b).Should().Be(expected);
        }

        [Theory]
        [InlineData(FuzzyAlgorithm.Exact)]
        [InlineData(FuzzyAlgorithm.Levenshtein)]
        [InlineData(FuzzyAlgorithm.Damerau)]
        [InlineData(FuzzyAlgorithm.Similarity)]
        [InlineData(FuzzyAlgorithm.JaroWinkler)]
        [InlineData(FuzzyAlgorithm.Dice)]
        [InlineData(FuzzyAlgorithm.Soundex)]
        [InlineData(FuzzyAlgorithm.TokenSort)]
        [InlineData(FuzzyAlgorithm.TokenSet)]
        [InlineData(FuzzyAlgorithm.Initials)]
        public void EveryAlgorithmPassesAnIdenticalMultiWordName(FuzzyAlgorithm algorithm)
        {
            var spec = new FuzzyAlgorithmSpec(algorithm, FuzzyOptions.DefaultThreshold(algorithm));

            FuzzyListMatcher.Score(spec, "john smith", "john smith").Passed.Should().BeTrue();
        }

        [Theory]
        [InlineData(FuzzyAlgorithm.Exact)]
        [InlineData(FuzzyAlgorithm.Levenshtein)]
        [InlineData(FuzzyAlgorithm.Damerau)]
        [InlineData(FuzzyAlgorithm.Similarity)]
        [InlineData(FuzzyAlgorithm.JaroWinkler)]
        [InlineData(FuzzyAlgorithm.Dice)]
        [InlineData(FuzzyAlgorithm.Soundex)]
        [InlineData(FuzzyAlgorithm.TokenSort)]
        [InlineData(FuzzyAlgorithm.TokenSet)]
        [InlineData(FuzzyAlgorithm.Initials)]
        public void NoAlgorithmPassesCompletelyUnrelatedNames(FuzzyAlgorithm algorithm)
        {
            var spec = new FuzzyAlgorithmSpec(algorithm, FuzzyOptions.DefaultThreshold(algorithm));

            FuzzyListMatcher.Score(spec, "john smith", "maria garcia").Passed.Should().BeFalse();
        }

        [Fact]
        public void LevenshteinPassesOnTheEditBudgetAndScoresByRatio()
        {
            var spec = new FuzzyAlgorithmSpec(FuzzyAlgorithm.Levenshtein, 2);

            FuzzyListMatcher.Score(spec, "jon smyth", "john smith").Passed.Should().BeTrue();
            FuzzyListMatcher.Score(new FuzzyAlgorithmSpec(FuzzyAlgorithm.Levenshtein, 1), "jon smyth", "john smith")
                .Passed.Should().BeFalse();
        }

        [Fact]
        public void ADamerauTranspositionPassesOneEditWhereLevenshteinNeedsTwo()
        {
            FuzzyListMatcher.Score(new FuzzyAlgorithmSpec(FuzzyAlgorithm.Damerau, 1), "jhon", "john").Passed
                .Should().BeTrue();
            FuzzyListMatcher.Score(new FuzzyAlgorithmSpec(FuzzyAlgorithm.Levenshtein, 1), "jhon", "john").Passed
                .Should().BeFalse();
        }
    }
}
