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
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class BooleanExtensionsTests
    {
        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void ToBinaryConvertsToOneOrZero(bool input, byte expected)
        {
            input.ToBinary().Should().Be(expected);
        }

        [Theory]
        [InlineData(true, "Yes", "No", "Yes")]
        [InlineData(false, "Yes", "No", "No")]
        public void ToStringWithTrueAndFalseValuesSelectsTheMatchingOne(bool input, string trueValue,
            string falseValue, string expected)
        {
            input.ToString(trueValue, falseValue).Should().Be(expected);
        }
    }
}