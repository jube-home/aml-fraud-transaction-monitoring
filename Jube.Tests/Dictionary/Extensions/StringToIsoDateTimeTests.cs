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
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class StringToIsoDateTimeTests
    {
        [Theory]
        [InlineData("2026-01-02T03:04:05", 2026, 1, 2, 3, 4, 5)]
        [InlineData("2026-01-02T03:04:05Z", 2026, 1, 2, 3, 4, 5)]
        [InlineData("2026-01-02T05:04:05+02:00", 2026, 1, 2, 3, 4, 5)]
        [InlineData("2026-01-02", 2026, 1, 2, 0, 0, 0)]
        [InlineData(" 2026-01-02 03:04:05 ", 2026, 1, 2, 3, 4, 5)]
        public void IsoTextIsReadAsUniversalTime(string text, int year, int month, int day, int hour, int minute,
            int second)
        {
            var value = text.ToIsoDateTime();

            value.Should().Be(new DateTime(year, month, day, hour, minute, second));
            value.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Theory]
        [InlineData("")]
        [InlineData("yesterday")]
        [InlineData("2026-13-40")]
        public void TextThatIsNotADateThrows(string text)
        {
            var act = text.ToIsoDateTime;

            act.Should().Throw<FormatException>();
        }
    }
}