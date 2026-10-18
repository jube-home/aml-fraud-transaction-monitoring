/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using FluentAssertions;
using Jube.AskJooby.DataCache;
using Jube.AskJooby.DataCache.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class CachedRowTextTests
    {
        private static readonly IReadOnlyList<CachedColumn> columns =
        [
            new("Dependent", "double"), new("IsChip", "double"), new("CountATM1Day", "double")
        ];

        [Fact]
        public void FormatTakesItsOrderFromTheColumnsAndNotFromTheRow()
        {
            var row = new Dictionary<string, object?>
            {
                ["IsChip"] = 1L, ["CountATM1Day"] = 4L, ["Dependent"] = 0L
            };

            CachedRowText.Format(columns, row)
                .Should().Be("Dependent=0, IsChip=1, CountATM1Day=4");
        }

        [Fact]
        public void FormatReportsAColumnTheRowDoesNotCarryAsNullRatherThanDroppingIt()
        {
            var row = new Dictionary<string, object?> { ["Dependent"] = 1L, ["CountATM1Day"] = 4L };

            CachedRowText.Format(columns, row)
                .Should().Be("Dependent=1, IsChip=null, CountATM1Day=4");
        }

        [Fact]
        public void FormatIgnoresAKeyThatIsNotAColumn()
        {
            var row = new Dictionary<string, object?>
            {
                ["Dependent"] = 1L, ["IsChip"] = 0L, ["CountATM1Day"] = 2L, ["Unlisted"] = 9L
            };

            CachedRowText.Format(columns, row).Should().NotContain("Unlisted");
        }

        [Fact]
        public void FormatAppliesTheCallersValueRenderer()
        {
            var row = new Dictionary<string, object?> { ["Dependent"] = 1L, ["IsChip"] = null, ["CountATM1Day"] = 2L };

            CachedRowText.Format(columns, row, value => value is null ? "-" : "#")
                .Should().Be("Dependent=#, IsChip=-, CountATM1Day=#");
        }

        [Fact]
        public void FormatFirstTakesTheLeadingColumnsInTableOrder()
        {
            var row = new Dictionary<string, object?>
            {
                ["IsChip"] = 1L, ["CountATM1Day"] = 4L, ["Dependent"] = 0L
            };

            CachedRowText.FormatFirst(columns, row, 2).Should().Be("Dependent=0, IsChip=1");
        }

        [Fact]
        public void FormatFirstReturnsEveryColumnWhenAskedForMoreThanThereAre()
        {
            var row = new Dictionary<string, object?>
            {
                ["IsChip"] = 1L, ["CountATM1Day"] = 4L, ["Dependent"] = 0L
            };

            CachedRowText.FormatFirst(columns, row, 50)
                .Should().Be("Dependent=0, IsChip=1, CountATM1Day=4");
        }

        [Fact]
        public void FormatReturnsAnEmptyStringForATableWithNoColumns()
        {
            CachedRowText.Format([], new Dictionary<string, object?> { ["Dependent"] = 1L })
                .Should().BeEmpty();
        }
    }
}