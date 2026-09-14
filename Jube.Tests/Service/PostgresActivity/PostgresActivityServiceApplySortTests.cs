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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Dto.PostgresActivity;
using Jube.Service.PostgresActivity;
using Xunit;

namespace Jube.Test.Service.PostgresActivity
{
    using PostgresActivityService = PostgresActivityService;

    [Trait("Category", "Unit")]
    public sealed class PostgresActivityServiceApplySortTests
    {
        private static PostgresActivityDto NewRow(int pid)
        {
            return new PostgresActivityDto(
                pid, "postgres", "postgres", null, null, "client backend", "active", null, null, [],
                null, null, null, null, null, null);
        }

        [Fact]
        public void UnrecognisedSortFieldReturnsRowsInTheirOriginalOrderUnchanged()
        {
            var rows = new List<PostgresActivityDto> { NewRow(300), NewRow(100), NewRow(200) };

            var result = PostgresActivityService.ApplySort(rows, "notARealColumn", null);

            result.Select(r => r.Pid).Should().Equal(300, 100, 200);
        }

        [Fact]
        public void NullSortFieldReturnsRowsInTheirOriginalOrderUnchanged()
        {
            var rows = new List<PostgresActivityDto> { NewRow(300), NewRow(100), NewRow(200) };

            var result = PostgresActivityService.ApplySort(rows, null, null);

            result.Select(r => r.Pid).Should().Equal(300, 100, 200);
        }

        [Fact]
        public void RecognisedSortFieldDefaultsToDescendingOrder()
        {
            var rows = new List<PostgresActivityDto> { NewRow(100), NewRow(300), NewRow(200) };

            var result = PostgresActivityService.ApplySort(rows, "pid", null);

            result.Select(r => r.Pid).Should().Equal(300, 200, 100);
        }

        [Fact]
        public void RecognisedSortFieldWithAscSortsAscending()
        {
            var rows = new List<PostgresActivityDto> { NewRow(300), NewRow(100), NewRow(200) };

            var result = PostgresActivityService.ApplySort(rows, "pid", "asc");

            result.Select(r => r.Pid).Should().Equal(100, 200, 300);
        }
    }
}