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

using System.Globalization;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class CharExtensionsTests
    {
        [Fact]
        public void ConvertToUtf32CombinesASurrogatePairIntoItsCodePoint()
        {
            const string emoji = "😀";

            var codePoint = emoji[0].ConvertToUtf32(emoji[1]);

            codePoint.Should().Be(char.ConvertToUtf32(emoji[0], emoji[1]));
        }

        [Theory]
        [InlineData('5', 5d)]
        [InlineData('0', 0d)]
        [InlineData('a', -1d)]
        public void GetNumericValueReturnsTheDigitsValueOrMinusOneForNonDigits(char input, double expected)
        {
            input.GetNumericValue().Should().Be(expected);
        }

        [Theory]
        [InlineData('A', UnicodeCategory.UppercaseLetter)]
        [InlineData('5', UnicodeCategory.DecimalDigitNumber)]
        [InlineData(' ', UnicodeCategory.SpaceSeparator)]
        public void GetUnicodeCategoryDelegatesToTheBclImplementation(char input,
            UnicodeCategory expected)
        {
            input.GetUnicodeCategory().Should().Be(expected);
        }

        [Theory]
        [InlineData('b', new[] { 'a', 'b', 'c' }, true)]
        [InlineData('z', new[] { 'a', 'b', 'c' }, false)]
        public void InChecksMembershipAgainstTheProvidedValues(char input, char[] values, bool expected)
        {
            input.In(values).Should().Be(expected);
        }

        [Theory]
        [InlineData('b', new[] { 'a', 'b', 'c' }, false)]
        [InlineData('z', new[] { 'a', 'b', 'c' }, true)]
        public void NotInIsTheInverseOfIn(char input, char[] values, bool expected)
        {
            input.NotIn(values).Should().Be(expected);
        }

        [Theory]
        [InlineData('', true)]
        [InlineData('A', false)]
        public void IsControlDelegatesToTheBcl(char input, bool expected)
        {
            input.IsControl().Should().Be(expected);
        }

        [Theory]
        [InlineData('5', true)]
        [InlineData('a', false)]
        public void IsDigitDelegatesToTheBcl(char input, bool expected)
        {
            input.IsDigit().Should().Be(expected);
        }

        [Theory]
        [InlineData('\uD800', true)]
        [InlineData('A', false)]
        public void IsHighSurrogateDelegatesToTheBcl(char input, bool expected)
        {
            input.IsHighSurrogate().Should().Be(expected);
        }

        [Theory]
        [InlineData('A', true)]
        [InlineData('5', false)]
        public void IsLetterDelegatesToTheBcl(char input, bool expected)
        {
            input.IsLetter().Should().Be(expected);
        }

        [Theory]
        [InlineData('A', true)]
        [InlineData('5', true)]
        [InlineData('!', false)]
        public void IsLetterOrDigitDelegatesToTheBcl(char input, bool expected)
        {
            input.IsLetterOrDigit().Should().Be(expected);
        }

        [Theory]
        [InlineData('a', true)]
        [InlineData('A', false)]
        public void IsLowerDelegatesToTheBcl(char input, bool expected)
        {
            input.IsLower().Should().Be(expected);
        }

        [Theory]
        [InlineData('\uDC00', true)]
        [InlineData('A', false)]
        public void IsLowSurrogateDelegatesToTheBcl(char input, bool expected)
        {
            input.IsLowSurrogate().Should().Be(expected);
        }

        [Theory]
        [InlineData('5', true)]
        [InlineData('a', false)]
        public void IsNumberDelegatesToTheBcl(char input, bool expected)
        {
            input.IsNumber().Should().Be(expected);
        }

        [Theory]
        [InlineData('!', true)]
        [InlineData('a', false)]
        public void IsPunctuationDelegatesToTheBcl(char input, bool expected)
        {
            input.IsPunctuation().Should().Be(expected);
        }

        [Theory]
        [InlineData(' ', true)]
        [InlineData('a', false)]
        public void IsSeparatorDelegatesToTheBcl(char input, bool expected)
        {
            input.IsSeparator().Should().Be(expected);
        }

        [Theory]
        [InlineData('\uD800', true)]
        [InlineData('\uDC00', true)]
        [InlineData('A', false)]
        public void IsSurrogateDelegatesToTheBcl(char input, bool expected)
        {
            input.IsSurrogate().Should().Be(expected);
        }

        [Fact]
        public void IsSurrogatePairRecognisesAValidHighLowPairAndRejectsAnInvalidOne()
        {
            const string emoji = "😀";

            emoji[0].IsSurrogatePair(emoji[1]).Should().BeTrue();
            'A'.IsSurrogatePair('B').Should().BeFalse();
        }

        [Theory]
        [InlineData('$', true)]
        [InlineData('a', false)]
        public void IsSymbolDelegatesToTheBcl(char input, bool expected)
        {
            input.IsSymbol().Should().Be(expected);
        }

        [Theory]
        [InlineData('A', true)]
        [InlineData('a', false)]
        public void IsUpperDelegatesToTheBcl(char input, bool expected)
        {
            input.IsUpper().Should().Be(expected);
        }

        [Theory]
        [InlineData(' ', true)]
        [InlineData('\t', true)]
        [InlineData('a', false)]
        public void IsWhiteSpaceDelegatesToTheBcl(char input, bool expected)
        {
            input.IsWhiteSpace().Should().Be(expected);
        }

        [Fact]
        public void RepeatBuildsAStringOfTheCharacterRepeatedNTimes()
        {
            'x'.Repeat(3).Should().Be("xxx");
        }

        [Fact]
        public void RepeatWithZeroProducesAnEmptyString()
        {
            'x'.Repeat(0).Should().Be(string.Empty);
        }

        [Fact]
        public void ToEnumeratesInclusivelyFromThisCharacterToTheTarget()
        {
            'a'.To('e').Should().Equal('a', 'b', 'c', 'd', 'e');
        }

        [Fact]
        public void ToReversesTheSequenceWhenTheTargetPrecedesThisCharacter()
        {
            'e'.To('a').Should().Equal('e', 'd', 'c', 'b', 'a');
        }

        [Fact]
        public void ToWithTheSameStartAndEndYieldsASingleCharacter()
        {
            'a'.To('a').Should().Equal('a');
        }

        [Fact]
        public void ToLowerWithoutCultureConvertsToLowercase()
        {
            'A'.ToLower().Should().Be('a');
        }

        [Fact]
        public void ToLowerWithAnExplicitCultureUsesThatCulturesRules()
        {
            'A'.ToLower(CultureInfo.InvariantCulture).Should().Be('a');
        }

        [Fact]
        public void ToLowerInvariantIgnoresCurrentCulture()
        {
            'A'.ToLowerInvariant().Should().Be('a');
        }

        [Fact]
        public void ToStringConvertsACharacterToASingleCharacterString()
        {
            'x'.ToString().Should().Be("x");
        }

        [Fact]
        public void ToUpperWithoutCultureConvertsToUppercase()
        {
            'a'.ToUpper().Should().Be('A');
        }

        [Fact]
        public void ToUpperWithAnExplicitCultureUsesThatCulturesRules()
        {
            'a'.ToUpper(CultureInfo.InvariantCulture).Should().Be('A');
        }

        [Fact]
        public void ToUpperInvariantIgnoresCurrentCulture()
        {
            'a'.ToUpperInvariant().Should().Be('A');
        }

        [Fact]
        public void NonAlphabeticCharactersAreUnchangedByCasingConversions()
        {
            '5'.ToUpper().Should().Be('5');
            '5'.ToLower().Should().Be('5');
        }
    }
}