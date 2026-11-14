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

using System.Collections.Concurrent;
using System.Linq;
using FluentAssertions;
using Jube.Engine.Sanctions;
using Jube.Engine.Sanctions.Models;
using Xunit;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Unit")]
    public sealed class SanctionMatchAlgorithmTests
    {
        private static readonly ConcurrentDictionary<string, byte> noStopTokens = new();

        private static ConcurrentDictionary<int, SanctionEntry> Entries(params string[] names)
        {
            var entries = new ConcurrentDictionary<int, SanctionEntry>();

            for (var i = 0; i < names.Length; i++)
            {
                entries[i + 1] = new SanctionEntry
                {
                    SanctionEntryId = i + 1, SanctionElementValue = [names[i]], SanctionEntrySourceId = 1,
                    SanctionEntryReference = "R" + i
                };
            }

            return entries;
        }

        private static string[] Match(string input, string[] entries, SanctionMatchOptions? options, int distance = 3)
        {
            var dictionary = Entries(entries);

            return new LevenshteinDistance(null, null, options)
                .CheckMultipartString(input, distance, dictionary, noStopTokens)
                .Select(r => r.SanctionEntry.SanctionElementValue[0]).ToArray();
        }

        [Fact]
        public void WithoutOptionsTheMatcherBehavesExactlyAsItAlwaysDid()
        {
            Match("jon smith", ["john smith", "maria garcia"], null).Should().Equal("john smith");
            Match("jon smith", ["john smith", "maria garcia"], new SanctionMatchOptions()).Should().Equal("john smith");
        }

        [Fact]
        public void JaroWinklerAcceptsAPartOnlyAtOrAboveTheMinimumSimilarity()
        {
            var entries = new[] { "john smith" };

            Match("jon smith", entries, new SanctionMatchOptions(SanctionMatchOptions.JaroWinkler, 0.9))
                .Should().Equal("john smith");
            Match("jon smith", entries, new SanctionMatchOptions(SanctionMatchOptions.JaroWinkler, 0.99))
                .Should().BeEmpty();
        }

        [Fact]
        public void JaroWinklerStillHonoursTheMaximumEditDistance()
        {
            var entries = new[] { "john smith" };
            var options = new SanctionMatchOptions(SanctionMatchOptions.JaroWinkler, 0.5);

            Match("jon smith", entries, options, distance: 0).Should().BeEmpty();
            Match("jon smith", entries, options, distance: 1).Should().Equal("john smith");
        }

        [Fact]
        public void JaroWinklerRewardsAMatchingPrefixWhereEditDistanceDoesNot()
        {
            var entries = new[] { "christopher johnson" };

            Match("christofer jonson", entries, new SanctionMatchOptions(SanctionMatchOptions.JaroWinkler, 0.9))
                .Should().Equal("christopher johnson");
        }

        [Fact]
        public void TokenSetIgnoresTheOrderOfTheParts()
        {
            var entries = new[] { "john smith", "maria garcia" };

            Match("smith john", entries, new SanctionMatchOptions(SanctionMatchOptions.TokenSet, 0.9))
                .Should().Equal("john smith");
        }

        [Fact]
        public void TokenSetLowersTheScoreForEveryPartLeftUnmatched()
        {
            var entries = new[] { "john michael smith" };

            Match("smith john", entries, new SanctionMatchOptions(SanctionMatchOptions.TokenSet, 0.9)).Should().BeEmpty();
            Match("smith john", entries, new SanctionMatchOptions(SanctionMatchOptions.TokenSet, 0.6))
                .Should().Equal("john michael smith");
        }

        [Fact]
        public void TokenSetRejectsUnrelatedNamesAndEmptyInput()
        {
            var options = new SanctionMatchOptions(SanctionMatchOptions.TokenSet, 0.9);

            Match("completely different", ["john smith"], options).Should().BeEmpty();
            Match("   ", ["john smith"], options).Should().BeEmpty();
        }

        [Fact]
        public void TransliterationLetsACyrillicNameMatchItsLatinSpellingBothWays()
        {
            var transliterate = new SanctionMatchOptions(SanctionMatchOptions.Levenshtein, 0.9, true);
            var plain = new SanctionMatchOptions();

            Match("Ivan Petrov", ["Иван Петров"], transliterate, distance: 1).Should().Equal("Иван Петров");
            Match("Иван Петров", ["Ivan Petrov"], transliterate, distance: 1).Should().Equal("Ivan Petrov");
            Match("Ivan Petrov", ["Иван Петров"], plain, distance: 1).Should().BeEmpty();
        }

        [Fact]
        public void TransliterationDoesNotChangeLatinMatches()
        {
            var transliterate = new SanctionMatchOptions(SanctionMatchOptions.Levenshtein, 0.9, true);

            Match("jon smith", ["john smith", "maria garcia"], transliterate).Should().Equal("john smith");
        }

        [Fact]
        public void ALatinTokenViewIsCachedAndRefreshedWhenTheValueChanges()
        {
            var entry = new SanctionEntry { SanctionElementValue = ["Иван"] };

            entry.LatinTokens.Should().Equal("ivan");
            entry.SanctionElementValue = ["Пётр"];
            entry.LatinTokens.Should().NotContain("ivan");
        }
    }
}
