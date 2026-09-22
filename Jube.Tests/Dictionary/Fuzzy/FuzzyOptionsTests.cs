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
using System.Linq;
using FluentAssertions;
using Jube.Dictionary.Fuzzy;
using Xunit;

namespace Jube.Test.Dictionary.Fuzzy
{
    [Trait("Category", "Unit")]
    public sealed class FuzzyOptionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ABlankOptionStringMeansTheDefaultProfile(string? text)
        {
            FuzzyOptions.Parse(text).Should().BeSameAs(FuzzyOptions.Default);
        }

        [Fact]
        public void TheDefaultProfileIsJaroWinklerOrTokenSortWithFoldedNormalisation()
        {
            var options = FuzzyOptions.Default;

            options.IsValid.Should().BeTrue();
            options.Algorithms.Select(a => a.Algorithm).Should()
                .BeEquivalentTo(new[] { FuzzyAlgorithm.JaroWinkler, FuzzyAlgorithm.TokenSort });
            options.Algorithms.Should().OnlyContain(a => Math.Abs(a.Threshold - 0.92) < 0.0001);
            options.RequireAll.Should().BeFalse();
            options.Within.Should().BeFalse();
            options.MinLength.Should().Be(0);
            options.Normalisation.Should().Be(FuzzyNormalisation.Default);
        }

        [Theory]
        [InlineData("exact", FuzzyAlgorithm.Exact, 1d)]
        [InlineData("levenshtein=2", FuzzyAlgorithm.Levenshtein, 2d)]
        [InlineData("lev=3", FuzzyAlgorithm.Levenshtein, 3d)]
        [InlineData("lev", FuzzyAlgorithm.Levenshtein, 1d)]
        [InlineData("damerau=1", FuzzyAlgorithm.Damerau, 1d)]
        [InlineData("osa=2", FuzzyAlgorithm.Damerau, 2d)]
        [InlineData("similarity=0.8", FuzzyAlgorithm.Similarity, 0.8)]
        [InlineData("sim", FuzzyAlgorithm.Similarity, 0.85)]
        [InlineData("jarowinkler=0.9", FuzzyAlgorithm.JaroWinkler, 0.9)]
        [InlineData("jw", FuzzyAlgorithm.JaroWinkler, 0.92)]
        [InlineData("dice=0.7", FuzzyAlgorithm.Dice, 0.7)]
        [InlineData("bigram", FuzzyAlgorithm.Dice, 0.85)]
        [InlineData("soundex", FuzzyAlgorithm.Soundex, 1d)]
        [InlineData("tokensort=0.95", FuzzyAlgorithm.TokenSort, 0.95)]
        [InlineData("sort", FuzzyAlgorithm.TokenSort, 0.92)]
        [InlineData("tokenset=0.6", FuzzyAlgorithm.TokenSet, 0.6)]
        [InlineData("set", FuzzyAlgorithm.TokenSet, 0.85)]
        [InlineData("initials", FuzzyAlgorithm.Initials, 1d)]
        [InlineData("JW=0.9", FuzzyAlgorithm.JaroWinkler, 0.9)]
        [InlineData(" jw = 0.9 ", FuzzyAlgorithm.JaroWinkler, 0.9)]
        public void EveryAlgorithmAndAliasParsesWithItsThreshold(string text, FuzzyAlgorithm algorithm,
            double threshold)
        {
            var options = FuzzyOptions.Parse(text);

            options.Error.Should().BeNull();
            options.Algorithms.Should().ContainSingle().Which.Should().Be(new FuzzyAlgorithmSpec(algorithm, threshold));
        }

        [Fact]
        public void SeveralAlgorithmsCanBeCombinedWithCommasOrSemicolons()
        {
            FuzzyOptions.Parse("exact,soundex,jw=0.9").Algorithms.Should().HaveCount(3);
            FuzzyOptions.Parse("exact;soundex;jw=0.9").Algorithms.Should().HaveCount(3);
        }

        [Fact]
        public void OptionsWithNoAlgorithmFallBackToTheDefaultAlgorithms()
        {
            var options = FuzzyOptions.Parse("notitles,within");

            options.Algorithms.Select(a => a.Algorithm).Should()
                .BeEquivalentTo(new[] { FuzzyAlgorithm.JaroWinkler, FuzzyAlgorithm.TokenSort });
        }

        [Fact]
        public void NormalisationFlagsAreParsed()
        {
            var options = FuzzyOptions.Parse("exact,case,diacritics,punct,nospaces,notitles,nosuffixes");

            options.Normalisation.Should().Be(new FuzzyNormalisation(false, false, false, true, true, true));
        }

        [Fact]
        public void NormalisationFlagsCanBeSwitchedBackByTheirOpposites()
        {
            var options = FuzzyOptions.Parse("exact,case,nocase,diacritics,nodiacritics,punct,nopunct,nospaces,keepspaces");

            options.Normalisation.Should().Be(FuzzyNormalisation.Default);
        }

        [Fact]
        public void TheLastOfConflictingFlagsWins()
        {
            FuzzyOptions.Parse("nocase,case").Normalisation.IgnoreCase.Should().BeFalse();
            FuzzyOptions.Parse("case,nocase").Normalisation.IgnoreCase.Should().BeTrue();
        }

        [Fact]
        public void ScopeCombinationAndBlockingOptionsAreParsed()
        {
            var options = FuzzyOptions.Parse("within,all,firstletter,minlength=4");

            options.Within.Should().BeTrue();
            options.RequireAll.Should().BeTrue();
            options.FirstLetter.Should().BeTrue();
            options.MinLength.Should().Be(4);
        }

        [Fact]
        public void WholeAndAnyReverseWithinAndAll()
        {
            var options = FuzzyOptions.Parse("within,whole,all,any");

            options.Within.Should().BeFalse();
            options.RequireAll.Should().BeFalse();
        }

        [Theory]
        [InlineData("bogus", "Unknown fuzzy option 'bogus'")]
        [InlineData("jw=2", "threshold")]
        [InlineData("jw=0", "threshold")]
        [InlineData("jw=-0.5", "threshold")]
        [InlineData("jw=abc", "not a number")]
        [InlineData("jw=NaN", "threshold")]
        [InlineData("dice=1.5", "threshold")]
        [InlineData("lev=1.5", "whole number")]
        [InlineData("lev=-1", "whole number")]
        [InlineData("lev=11", "whole number")]
        [InlineData("damerau=abc", "not a number")]
        [InlineData("exact=1", "takes no value")]
        [InlineData("soundex=1", "takes no value")]
        [InlineData("initials=1", "takes no value")]
        [InlineData("nocase=1", "takes no value")]
        [InlineData("within=yes", "takes no value")]
        [InlineData("minlength", "minlength")]
        [InlineData("minlength=-1", "minlength")]
        [InlineData("minlength=abc", "minlength")]
        [InlineData("jw,bogus", "bogus")]
        public void InvalidOptionsAreReportedNotThrown(string text, string expectedFragment)
        {
            var options = FuzzyOptions.Parse(text);

            options.IsValid.Should().BeFalse();
            options.Error.Should().Contain(expectedFragment);
        }

        [Fact]
        public void ParsingIsCachedPerString()
        {
            var first = FuzzyOptions.Parse("jw=0.91,notitles");
            var second = FuzzyOptions.Parse("jw=0.91,notitles");

            second.Should().BeSameAs(first);
        }

        [Fact]
        public void ADifferentStringGivesADifferentOptionsObject()
        {
            FuzzyOptions.Parse("jw=0.91").Should().NotBeSameAs(FuzzyOptions.Parse("jw=0.93"));
        }

        [Fact]
        public void CanonicalFormIsTrimmedAndLowerCase()
        {
            FuzzyOptions.Parse("  JW=0.9 , NoCase ").Canonical.Should().Be("jw=0.9 , nocase");
        }

        [Theory]
        [InlineData(FuzzyAlgorithm.Levenshtein, 2, true)]
        [InlineData(FuzzyAlgorithm.Levenshtein, 2.5, false)]
        [InlineData(FuzzyAlgorithm.Levenshtein, 11, false)]
        [InlineData(FuzzyAlgorithm.Damerau, 0, true)]
        [InlineData(FuzzyAlgorithm.JaroWinkler, 0.9, true)]
        [InlineData(FuzzyAlgorithm.JaroWinkler, 0, false)]
        [InlineData(FuzzyAlgorithm.JaroWinkler, 1.1, false)]
        [InlineData(FuzzyAlgorithm.Dice, double.NaN, false)]
        [InlineData(FuzzyAlgorithm.Exact, 1, true)]
        public void ForBuildsASingleAlgorithmProfileAndValidatesItsThreshold(FuzzyAlgorithm algorithm, double value,
            bool valid)
        {
            var options = FuzzyOptions.For(algorithm, value);

            options.IsValid.Should().Be(valid);
            options.Algorithms.Should().ContainSingle().Which.Algorithm.Should().Be(algorithm);
        }

        [Fact]
        public void ForCanBeScopedWithin()
        {
            FuzzyOptions.For(FuzzyAlgorithm.Exact, 1, true).Within.Should().BeTrue();
            FuzzyOptions.For(FuzzyAlgorithm.Exact, 1).Within.Should().BeFalse();
        }

        [Fact]
        public void DefaultThresholdsAreSensiblePerAlgorithm()
        {
            FuzzyOptions.DefaultThreshold(FuzzyAlgorithm.Levenshtein).Should().Be(1);
            FuzzyOptions.DefaultThreshold(FuzzyAlgorithm.Damerau).Should().Be(1);
            FuzzyOptions.DefaultThreshold(FuzzyAlgorithm.JaroWinkler).Should().Be(0.92);
            FuzzyOptions.DefaultThreshold(FuzzyAlgorithm.Exact).Should().Be(1);
        }
    }
}
