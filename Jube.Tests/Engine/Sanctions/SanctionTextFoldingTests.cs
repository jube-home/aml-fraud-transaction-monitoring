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
    public sealed class SanctionTextFoldingTests
    {
        private static ConcurrentDictionary<int, SanctionEntry> EntryFor(string normalisedValue)
        {
            var dictionary = new ConcurrentDictionary<int, SanctionEntry>();

            dictionary.TryAdd(1, new SanctionEntry
            {
                SanctionEntryId = 1,
                SanctionEntrySourceId = 1,
                SanctionEntryReference = "REF1",
                SanctionElementValue = [normalisedValue]
            });

            return dictionary;
        }

        [Theory]
        [InlineData("\uFF29\uFF56\uFF41\uFF4E", "ivan")]
        [InlineData("Iv\u200Ban", "ivan")]
        [InlineData("Iv\u00ADan", "ivan")]
        [InlineData("\u202EIvan\u202C", "ivan")]
        [InlineData("Ivan\tPetrov", "ivan petrov")]
        [InlineData("Fran\u00E7ois", "francois")]
        [InlineData("\uFB01nn", "finn")]
        public void FoldCollapsesCompatibilityFormsFormatCharactersAndWhitespace(string raw, string expected)
        {
            SanctionTextFolding.Fold(raw).Should().Be(expected);
        }

        [Fact]
        public void FoldRemovesArabicShortVowelMarksFromBothSides()
        {
            SanctionTextFolding.Fold("\u0645\u064F\u062D\u064E\u0645\u0651\u062F")
                .Should().Be(SanctionTextFolding.Fold("\u0645\u062D\u0645\u062F"));
        }

        [Fact]
        public void FoldKeepsGeorgianAndCyrillicLettersIntactAndLowersCyrillic()
        {
            SanctionTextFolding.Fold("\u10D2\u10D8\u10DD\u10E0\u10D2\u10D8").Should().Be("\u10D2\u10D8\u10DD\u10E0\u10D2\u10D8");
            SanctionTextFolding.Fold("\u0418\u0412\u0410\u041D").Should().Be("\u0438\u0432\u0430\u043D");
        }

        [Fact]
        public void AFullWidthQueryMatchesAnAsciiEntryAtDistanceZero()
        {
            var result = new LevenshteinDistance().CheckMultipartString("\uFF29\uFF56\uFF41\uFF4E", 0,
                EntryFor("ivan"), new ConcurrentDictionary<string, byte>());

            result.Should().ContainSingle().Which.LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void AQueryPaddedWithZeroWidthSpacesStillMatchesAtDistanceZero()
        {
            var result = new LevenshteinDistance().CheckMultipartString("I\u200Bv\u200Ba\u200Bn", 0,
                EntryFor("ivan"), new ConcurrentDictionary<string, byte>());

            result.Should().ContainSingle().Which.LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void NormalizeElementValueAppliesTheSameFolding()
        {
            SanctionEntryFileImporter.NormalizeElementValue("\uFF29\uFF56\uFF41\uFF4E-O'Brien")
                .Should().Be("ivan obrien");
        }

        [Theory]
        [InlineData("R\u043Eb\u0435rt", "robert")]
        [InlineData("M\u03C5g\u0430b\u0435", "mugabe")]
        [InlineData("\u0440\u0430\u0441\u0435", "pace")]
        [InlineData("\u041C\u0410RIA", "maria")]
        [InlineData("\u0391lex", "alex")]
        public void FoldMapsLookalikeCyrillicAndGreekLettersToLatinWhenTheWholeWordCanBeRead(string raw,
            string expected)
        {
            SanctionTextFolding.Fold(raw).Should().Be(expected);
        }

        [Theory]
        [InlineData("\u0421\u0435\u0440\u0433\u0435\u0439", "\u0441\u0435\u0440\u0433\u0435\u0438")]
        [InlineData("\u039D\u03AF\u03BA\u03BF\u03C2", "\u03BD\u03B9\u03BA\u03BF\u03C2")]
        [InlineData("\u10D2\u10D8\u10DD\u10E0\u10D2\u10D8", "\u10D2\u10D8\u10DD\u10E0\u10D2\u10D8")]
        [InlineData("\u0645\u062D\u0645\u062F", "\u0645\u062D\u0645\u062F")]
        [InlineData("\u0627\u062D\u0645\u062F", "\u0627\u062D\u0645\u062F")]
        [InlineData("\u0930\u093E\u092E", "\u0930\u093E\u092E")]
        public void FoldLeavesLegitimateNonLatinNamesUncorrupted(string raw, string expected)
        {
            SanctionTextFolding.Fold(raw).Should().Be(expected);
        }

        [Theory]
        [InlineData("R\u043Eb\u0435rt", true)]
        [InlineData("Robert", false)]
        [InlineData("\u0421\u0435\u0440\u0433\u0435\u0439", false)]
        [InlineData("Robert \u0421\u0435\u0440\u0433\u0435\u0439", false)]
        public void IsMixedScriptFlagsLatinWordsContainingCyrillicOrGreekLetters(string raw, bool expected)
        {
            SanctionTextFolding.IsMixedScript(raw).Should().Be(expected);
        }

        [Fact]
        public void ACyrillicLookalikeSpellingOfAListedLatinNameIsFoundAtDistanceZero()
        {
            var result = new LevenshteinDistance().CheckMultipartString("R\u043Eb\u0435rt M\u03C5g\u0430b\u0435", 0,
                EntryFor("robert mugabe"), new ConcurrentDictionary<string, byte>());

            result.Should().ContainSingle().Which.LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public void ACyrillicLookalikeSpellingInTheListIsFoundByTheLatinQueryBecauseTheLoaderFoldsIt()
        {
            var entry = SanctionEntryFileImporter.NormalizeElementValue("R\u043Eb\u0435rt");

            entry.Should().Be("robert");

            var result = new LevenshteinDistance().CheckMultipartString("Robert", 0, EntryFor(entry),
                new ConcurrentDictionary<string, byte>());

            result.Should().ContainSingle();
        }

        [Fact]
        public void FoldingIsIdempotent()
        {
            var once = SanctionTextFolding.Fold("R\u043Eb\u0435rt \u0421\u0435\u0440\u0433\u0435\u0439 \uFF29van");

            SanctionTextFolding.Fold(once).Should().Be(once);
        }

        [Fact]
        public void CachedEntryTokensGiveTheSameMatchesAsFreshEntriesAndAreRefreshedWhenTheValueChanges()
        {
            var stops = new ConcurrentDictionary<string, byte>();
            stops.TryAdd("bin", 1);

            string[][] values =
            [
                ["Osama bin Laden"], ["Mohammed Hassan"], ["Fran\u00E7ois Hollande"], ["D'Angelo O'Brien-Smith."],
                ["Robert", "Mugabe"], ["-", "."]
            ];

            var cached = new ConcurrentDictionary<int, SanctionEntry>();
            var fresh = new ConcurrentDictionary<int, SanctionEntry>();

            for (var i = 0; i < values.Length; i++)
            {
                cached.TryAdd(i, new SanctionEntry { SanctionEntryId = i, SanctionElementValue = values[i] });
                fresh.TryAdd(i, new SanctionEntry { SanctionEntryId = i, SanctionElementValue = values[i] });
            }

            foreach (var query in new[] { "osama laden", "Mohamed Hassan", "Francois Hollande", "Robert Mugabe", "x" })
            {
                var first = new LevenshteinDistance().CheckMultipartString(query, 2, cached, stops);
                var second = new LevenshteinDistance().CheckMultipartString(query, 2, cached, stops);
                var reference = new LevenshteinDistance().CheckMultipartString(query, 2, fresh, stops);

                first.Select(r => (r.SanctionEntry.SanctionEntryId, r.LevenshteinDistance)).OrderBy(r => r)
                    .Should().Equal(reference.Select(r => (r.SanctionEntry.SanctionEntryId, r.LevenshteinDistance))
                        .OrderBy(r => r));

                second.Count.Should().Be(first.Count);
            }

            var entry = cached[0];
            entry.Tokens.Should().Equal("osama", "bin", "laden");

            entry.SanctionElementValue = ["Someone Else"];

            entry.Tokens.Should().Equal("someone", "else");
        }
    }
}
