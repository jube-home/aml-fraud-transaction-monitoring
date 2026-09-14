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
    public sealed class LevenshteinDistanceTests
    {
        private const string LongEntryName = "Zbigniew Brzezinski";

        private const string PaddedInputAroundLongEntryName =
            "The Man Called Zbigniew Brzezinski Wrote Books Today";

        private static readonly ConcurrentDictionary<string, byte> noStopTokens = StopTokens();

        private static ConcurrentDictionary<int, SanctionEntry> Entries(params (int Id, string[] Values)[] entries)
        {
            var dictionary = new ConcurrentDictionary<int, SanctionEntry>();
            foreach (var (id, values) in entries)
            {
                dictionary.TryAdd(id, new SanctionEntry
                {
                    SanctionEntryId = id,
                    SanctionEntrySourceId = 1,
                    SanctionEntryReference = $"REF{id}",
                    SanctionElementValue = values
                });
            }

            return dictionary;
        }

        private static ConcurrentDictionary<string, byte> StopTokens(params string[] tokens)
        {
            var dictionary = new ConcurrentDictionary<string, byte>();
            foreach (var token in tokens)
            {
                dictionary.TryAdd(token, 1);
            }

            return dictionary;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CheckMultipartStringWithNullOrBlankInputReturnsNoMatches(string input)
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Osama bin Laden"]));

            var result = levenshtein.CheckMultipartString(input, 2, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWithNoSanctionEntriesReturnsNoMatches()
        {
            var levenshtein = new LevenshteinDistance();
            var result = levenshtein.CheckMultipartString("Osama bin Laden", 2,
                new ConcurrentDictionary<int, SanctionEntry>(), noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWhereEveryInputTokenIsAStopTokenReturnsNoMatches()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Mohammed Hassan"]));
            var stopTokens = StopTokens("mohammed", "hassan");

            var result = levenshtein.CheckMultipartString("Mohammed Hassan", 2, entries, stopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWithEntryThatHasNoUsableTokensIsSkipped()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["-", "."]));
            var result = levenshtein.CheckMultipartString("John Smith", 2, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWithExactMatchReturnsZeroDistance()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Osama bin Laden"]));

            var result = levenshtein.CheckMultipartString("Osama bin Laden", 2, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].SanctionEntry.SanctionEntryId.Should().Be(1);
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringWithExactMatchIsCaseInsensitive()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["OSAMA BIN LADEN"]));

            var result = levenshtein.CheckMultipartString("osama bin laden", 0, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringNormalizesDiacriticsBeforeComparing()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["François Hollande"]));

            var result = levenshtein.CheckMultipartString("Francois Hollande", 0, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringStripsPunctuationBeforeComparing()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["D'Angelo, O'Brien-Smith."]));
            var result = levenshtein.CheckMultipartString("dangelo obrien smith", 0, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringWithOneCharacterEditWithinThresholdMatches()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Vladimir Putin"]));
            var result = levenshtein.CheckMultipartString("Vladimir Puttin", 1, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(1);
        }

        [Fact]
        public void CheckMultipartStringWithEditDistanceBeyondMaxDistanceIsRejected()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Vladimir Putin"]));

            var result = levenshtein.CheckMultipartString("Vladimir Puttinnn", 1, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWithEditDistanceExactlyAtMaxDistanceMatches()
        {
            var levenshtein = new LevenshteinDistance(1.0);
            var entries = Entries((1, ["Putin"]));
            var result = levenshtein.CheckMultipartString("Puttinn", 2, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(2);
        }

        [Fact]
        public void CheckMultipartStringWithCompletelyUnrelatedNameIsRejected()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Osama bin Laden"]));

            var result = levenshtein.CheckMultipartString("John Smith", 2, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringIgnoresConfiguredHonorificsOnBothSides()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Sheikh Ahmed Yassin"]));
            var stopTokens = StopTokens("sheikh", "imam");
            var result = levenshtein.CheckMultipartString("Imam Ahmed Yassin", 0, entries, stopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringSingleCharacterTokensAreAlwaysIgnoredRegardlessOfStopTokens()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["John Q Smith"]));

            var result = levenshtein.CheckMultipartString("John Smith", 0, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringWithCoverageRatioBelowHalfIsRejected()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Anders Nils Erik Olof Karlsson"]));
            var result = levenshtein.CheckMultipartString("Anders Karlsson", 0, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWithCoverageRatioExactlyAtHalfIsAccepted()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Anders Nils Erik Karlsson"]));
            var result = levenshtein.CheckMultipartString("Anders Karlsson", 0, entries, noStopTokens);

            result.Should().ContainSingle();
        }

        [Fact]
        public void CheckMultipartStringWithNoMaxCoverageRatioConfiguredStillRejectsAMuchLongerInputByDefault()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, [LongEntryName]));
            var result = levenshtein.CheckMultipartString(PaddedInputAroundLongEntryName, 0, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWithMaxCoverageRatioExplicitlyRaisedAllowsTheLongerInputThrough()
        {
            var levenshtein = new LevenshteinDistance(maxCoverageRatio: 10.0);
            var entries = Entries((1, [LongEntryName]));
            var result = levenshtein.CheckMultipartString(PaddedInputAroundLongEntryName, 0, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringWithMaxCoverageRatioRejectsMuchLongerInputThanEntry()
        {
            var levenshtein = new LevenshteinDistance(maxCoverageRatio: 2.0);
            var entries = Entries((1, [LongEntryName]));
            var result = levenshtein.CheckMultipartString(PaddedInputAroundLongEntryName, 0, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWithMaxCoverageRatioAllowsInputWithinTheConfiguredRatio()
        {
            var levenshtein = new LevenshteinDistance(maxCoverageRatio: 2.0);
            var entries = Entries((1, ["Alpha Bravo Charlie"]));
            var result = levenshtein.CheckMultipartString("xx yy zz Alpha Bravo Charlie", 0, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringWithMaxDistanceRatioRejectsProportionallyLargeEditOnShortToken()
        {
            var levenshtein = new LevenshteinDistance(0.3);
            var entries = Entries((1, ["Li"]));
            var result = levenshtein.CheckMultipartString("Wu", 2, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringWithMaxDistanceRatioStillAllowsProportionallySmallEditOnLongToken()
        {
            var levenshtein = new LevenshteinDistance(0.3);
            var entries = Entries((1, ["Alexanderson"]));
            var result = levenshtein.CheckMultipartString("Alexandersan", 2, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(1);
        }

        [Fact]
        public void CheckMultipartStringWithMaxDistanceRatioNeverAllowsBelowOneCharacterOfSlack()
        {
            var levenshtein = new LevenshteinDistance(0.01);
            var entries = Entries((1, ["Li"]));
            var result = levenshtein.CheckMultipartString("Lu", 5, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(1);
        }

        [Fact]
        public void CheckMultipartStringDoesNotLetTwoInputTokensClaimTheSameEntryToken()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Smith Jones"]));
            var result = levenshtein.CheckMultipartString("Smith Smyth", 2, entries, noStopTokens);

            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckMultipartStringReturnsWorstNotBestPairDistanceAcrossMultipleTokens()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["John Alexandria"]));
            var result = levenshtein.CheckMultipartString("John Alexandriaxyz", 3, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(3);
        }

        [Fact]
        public void CheckMultipartStringCombinesTokensFromMultipleElementValuesForOneEntry()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Osama bin Laden", "Usama Bin Ladin"]));
            var result = levenshtein.CheckMultipartString("Osama Bin Ladin", 0, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void CheckMultipartStringReturnsOnlyEntriesThatMatchFromALargerSet()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries(
                (1, ["Osama bin Laden"]),
                (2, ["John Smith"]),
                (3, ["Osama bin Ladin"]),
                (4, ["Completely Different Name"]));

            var result = levenshtein.CheckMultipartString("Osama bin Laden", 1, entries, noStopTokens);

            result.Select(r => r.SanctionEntry.SanctionEntryId).Should().BeEquivalentTo([1, 3]);
        }

        [Fact]
        public void CheckMultipartStringAcrossManyEntriesIsThreadSafeAndDeterministicInResultSet()
        {
            var levenshtein = new LevenshteinDistance();
            var entryTuples = Enumerable.Range(10, 200)
                .Select(i => (i, new[] { $"Person Number {i}" }))
                .ToArray();
            var entries = Entries(entryTuples);
            var result = levenshtein.CheckMultipartString("Person Number 57", 0, entries, noStopTokens);

            result.Should().ContainSingle();
            result[0].SanctionEntry.SanctionEntryId.Should().Be(57);
        }

        [Fact]
        public void CheckMultipartStringEntryWithFewerTokensThanInputIgnoresInputsExtraDistinguishingToken()
        {
            var levenshtein = new LevenshteinDistance();
            var entries = Entries((1, ["Person Number"]));

            var matchesCaseFiftySeven = levenshtein.CheckMultipartString("Person Number 57", 0, entries,
                noStopTokens);
            var matchesCaseNinetyNine = levenshtein.CheckMultipartString("Person Number 99", 0, entries,
                noStopTokens);

            matchesCaseFiftySeven.Should().ContainSingle();
            matchesCaseNinetyNine.Should().ContainSingle();
        }

        [Fact]
        public void DefaultMaxDistanceRatioConstantMatchesTheDocumentedServerWideDefault()
        {
            LevenshteinDistance.DefaultMaxDistanceRatio.Should().Be(0.3);
        }

        [Fact]
        public void DefaultMaxCoverageRatioConstantMatchesTheDocumentedServerWideDefault()
        {
            LevenshteinDistance.DefaultMaxCoverageRatio.Should().Be(2.0);
        }

        [Fact]
        public void DefaultConstructorAppliesTheDocumentedMaxDistanceRatioAutomatically()
        {
            var bareDefault = new LevenshteinDistance();
            var explicitDefault = new LevenshteinDistance(LevenshteinDistance.DefaultMaxDistanceRatio);
            var entries = Entries((1, ["Li"]));

            bareDefault.CheckMultipartString("Wu", 2, entries, noStopTokens).Should().BeEmpty();
            explicitDefault.CheckMultipartString("Wu", 2, entries, noStopTokens).Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-number")]
        public void ParseNullableDistanceRatioWithUnparsableInputReturnsNull(string raw)
        {
            LevenshteinDistance.ParseNullableDistanceRatio(raw).Should().BeNull();
        }

        [Fact]
        public void ParseNullableDistanceRatioParsesAValidValue()
        {
            LevenshteinDistance.ParseNullableDistanceRatio("0.4").Should().Be(0.4);
        }

        [Theory]
        [InlineData("-0.5", 0)]
        [InlineData("1.5", 1)]
        public void ParseNullableDistanceRatioClampsToUnitInterval(string raw, double expected)
        {
            LevenshteinDistance.ParseNullableDistanceRatio(raw).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-number")]
        public void ParseNullableCoverageRatioWithUnparsableInputReturnsNull(string raw)
        {
            LevenshteinDistance.ParseNullableCoverageRatio(raw).Should().BeNull();
        }

        [Fact]
        public void ParseNullableCoverageRatioParsesAValidValue()
        {
            LevenshteinDistance.ParseNullableCoverageRatio("3.5").Should().Be(3.5);
        }

        [Fact]
        public void ParseNullableCoverageRatioClampsNegativeToZeroButHasNoUpperBound()
        {
            LevenshteinDistance.ParseNullableCoverageRatio("-2").Should().Be(0);
            LevenshteinDistance.ParseNullableCoverageRatio("100").Should().Be(100);
        }
    }
}