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
    public sealed class FuzzyNormaliserTests
    {
        private static readonly FuzzyNormalisation @default = FuzzyNormalisation.Default;

        [Theory]
        [InlineData("John Smith", "john smith")]
        [InlineData("  John   Smith  ", "john smith")]
        [InlineData("José O'Brien-Smith", "jose obrien smith")]
        [InlineData("O’Neil", "oneil")]
        [InlineData("Smith, John.", "smith john")]
        [InlineData("A/B & C", "a b c")]
        [InlineData("ÀÉÎÕÜ", "aeiou")]
        [InlineData("Straße", "strasse")]
        [InlineData("Ærøskøbing", "aeroskobing")]
        [InlineData("Łódź", "lodz")]
        [InlineData("İstanbul", "istanbul")]
        [InlineData("Đorđe", "dorde")]
        public void DefaultNormalisationFoldsCaseDiacriticsAndPunctuation(string input, string expected)
        {
            FuzzyNormaliser.Normalise(input, @default).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void NullAndBlankInputNormaliseToEmpty(string? input)
        {
            FuzzyNormaliser.Normalise(input, @default).Should().BeEmpty();
        }

        [Fact]
        public void PunctuationOnlyInputNormalisesToEmpty()
        {
            FuzzyNormaliser.Normalise("-- .. //", @default).Should().BeEmpty();
        }

        [Fact]
        public void CaseCanBePreserved()
        {
            FuzzyNormaliser.Normalise("John Smith", @default with { IgnoreCase = false }).Should().Be("John Smith");
        }

        [Fact]
        public void DiacriticsCanBePreserved()
        {
            FuzzyNormaliser.Normalise("José", @default with { RemoveDiacritics = false }).Should().Be("josé");
        }

        [Fact]
        public void PunctuationCanBePreserved()
        {
            FuzzyNormaliser.Normalise("a-b.c", @default with { StripPunctuation = false }).Should().Be("a-b.c");
        }

        [Fact]
        public void SpacesCanBeRemovedEntirely()
        {
            FuzzyNormaliser.Normalise("John  Smith", @default with { RemoveSpaces = true }).Should().Be("johnsmith");
        }

        [Theory]
        [InlineData("Dr. John Smith", "john smith")]
        [InlineData("Mr Mrs Smith", "smith")]
        [InlineData("Prof Sir John", "john")]
        [InlineData("Dr", "dr")]
        [InlineData("Mr Dr", "dr")]
        [InlineData("John Dr Smith", "john dr smith")]
        public void LeadingTitlesCanBeRemovedButNeverTheWholeName(string input, string expected)
        {
            FuzzyNormaliser.Normalise(input, @default with { RemoveTitles = true }).Should().Be(expected);
        }

        [Theory]
        [InlineData("Acme Trading Ltd.", "acme trading")]
        [InlineData("Acme Trading Limited", "acme trading")]
        [InlineData("Acme Co Ltd", "acme")]
        [InlineData("Ltd", "ltd")]
        [InlineData("Ltd Acme", "ltd acme")]
        [InlineData("Acme GmbH", "acme")]
        public void TrailingCompanySuffixesCanBeRemovedButNeverTheWholeName(string input, string expected)
        {
            FuzzyNormaliser.Normalise(input, @default with { RemoveCompanySuffixes = true }).Should().Be(expected);
        }

        [Fact]
        public void TitlesAndSuffixesAreNotRemovedUnlessAsked()
        {
            FuzzyNormaliser.Normalise("Dr John Ltd", @default).Should().Be("dr john ltd");
        }

        [Fact]
        public void TitleRemovalIsCaseInsensitiveEvenWhenCaseIsPreserved()
        {
            FuzzyNormaliser.Normalise("DR John", @default with { IgnoreCase = false, RemoveTitles = true })
                .Should().Be("John");
        }

        [Fact]
        public void EverySignatureDiffersWhenAnyFlagDiffers()
        {
            var signatures = new[]
            {
                @default.Signature,
                (@default with { IgnoreCase = false }).Signature,
                (@default with { RemoveDiacritics = false }).Signature,
                (@default with { StripPunctuation = false }).Signature,
                (@default with { RemoveSpaces = true }).Signature,
                (@default with { RemoveTitles = true }).Signature,
                (@default with { RemoveCompanySuffixes = true }).Signature
            };

            signatures.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void FoldDiacriticsLeavesPlainAsciiAlone()
        {
            FuzzyNormaliser.FoldDiacritics("Plain ASCII 123").Should().Be("Plain ASCII 123");
        }

        [Fact]
        public void FoldDiacriticsKeepsTheCaseOfTheFoldedLetters()
        {
            FuzzyNormaliser.FoldDiacritics("ÆØŁĐŒ").Should().Be("AEOLDOE");
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("john", 1)]
        [InlineData("john smith", 2)]
        [InlineData("a b c d", 4)]
        public void TokensSplitOnSingleSpaces(string normalised, int expected)
        {
            FuzzyNormaliser.Tokens(normalised).Should().HaveCount(expected);
        }

        [Fact]
        public void NormalisationIsIdempotent()
        {
            var once = FuzzyNormaliser.Normalise("  Dr. José O'Brien-Smith Ltd. ", @default with
            {
                RemoveTitles = true, RemoveCompanySuffixes = true
            });
            var twice = FuzzyNormaliser.Normalise(once, @default with
            {
                RemoveTitles = true, RemoveCompanySuffixes = true
            });

            twice.Should().Be(once);
        }
    }
}