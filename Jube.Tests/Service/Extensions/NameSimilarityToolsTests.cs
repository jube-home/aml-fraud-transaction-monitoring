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
using FluentAssertions;
using Jube.Service.Extensions.NameSimilarity;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class NameSimilarityToolsTests
    {
        [Fact]
        [Trait("Covers", "CompareNames")]
        public void CompareNamesScoresEveryAlgorithm()
        {
            var comparison = NameSimilarityTools.CompareNames("John Smith", "Smith John");
            comparison.TokenSort.Should().Be(1);
            comparison.TokenSet.Should().Be(1);
            comparison.LevenshteinDistance.Should().BeGreaterThan(0);
            comparison.SoundsAlike.Should().BeFalse();
            var act = () => NameSimilarityTools.CompareNames(new string('x', 1001), "x");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "LevenshteinDistance")]
        [Trait("Covers", "DamerauDistance")]
        [Trait("Covers", "JaroWinklerSimilarity")]
        public void EditDistances()
        {
            NameSimilarityTools.LevenshteinDistance("kitten", "sitting").Should().Be(3);
            NameSimilarityTools.DamerauDistance("abcd", "abdc").Should().Be(1);
            NameSimilarityTools.JaroWinklerSimilarity("MARTHA", "MARHTA").Should().BeApproximately(0.9611, 1e-4);
        }

        [Fact]
        [Trait("Covers", "SoundexCode")]
        [Trait("Covers", "NamesSoundAlike")]
        [Trait("Covers", "InitialsOf")]
        [Trait("Covers", "NormaliseNameForMatching")]
        public void PhoneticAndNormalisation()
        {
            NameSimilarityTools.SoundexCode("Robert").Should().Be("R163");
            NameSimilarityTools.SoundexCode("Rupert").Should().Be("R163");
            NameSimilarityTools.NamesSoundAlike("Robert Smith", "Rupert Smyth").Should().BeTrue();
            NameSimilarityTools.InitialsOf("john ronald reuel tolkien").Should().Be("JRRT");
            NameSimilarityTools.NormaliseNameForMatching("Dr. José Núñez").Should().Be("jose nunez");
        }

        [Fact]
        [Trait("Covers", "NamesMatchWithOptions")]
        public void NamesMatchWithOptionsUsesTheRuleLanguage()
        {
            NameSimilarityTools.NamesMatchWithOptions("Jon Smith", "John Smith").Should().BeTrue();
            NameSimilarityTools.NamesMatchWithOptions("Jon Smith", "John Smith", "exact").Should().BeFalse();
            var act = () => NameSimilarityTools.NamesMatchWithOptions("a", "b", "nonsense=1");
            act.Should().Throw<ArgumentException>();
        }
    }
}