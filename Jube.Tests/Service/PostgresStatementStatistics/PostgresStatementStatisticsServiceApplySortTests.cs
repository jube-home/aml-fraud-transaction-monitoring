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
using Jube.Dto.PostgresStatementStatistics;
using Jube.Service.PostgresStatementStatistics;
using Xunit;

namespace Jube.Test.Service.PostgresStatementStatistics
{
    using PostgresStatementStatisticsService = PostgresStatementStatisticsService;

    [Trait("Category", "Unit")]
    public sealed class PostgresStatementStatisticsServiceApplySortTests
    {
        private static PostgresStatementStatisticsDto NewRow(long queryId, double meanExecTimeMilliseconds = 0)
        {
            return new PostgresStatementStatisticsDto(
                queryId, "postgres", "postgres", "select 1", 1, 1, meanExecTimeMilliseconds,
                meanExecTimeMilliseconds, meanExecTimeMilliseconds, meanExecTimeMilliseconds, 0, 0, 0, 0, 0, 0);
        }

        [Fact]
        public void UnrecognisedSortFieldReturnsRowsInTheirOriginalOrderUnchanged()
        {
            var rows = new List<PostgresStatementStatisticsDto> { NewRow(300), NewRow(100), NewRow(200) };

            var result = PostgresStatementStatisticsService.ApplySort(rows, "notARealColumn", null);

            result.Select(r => r.QueryId).Should().Equal(300, 100, 200);
        }

        [Fact]
        public void NullSortFieldReturnsRowsInTheirOriginalOrderUnchanged()
        {
            var rows = new List<PostgresStatementStatisticsDto> { NewRow(300), NewRow(100), NewRow(200) };

            var result = PostgresStatementStatisticsService.ApplySort(rows, null, null);

            result.Select(r => r.QueryId).Should().Equal(300, 100, 200);
        }

        [Fact]
        public void RecognisedSortFieldDefaultsToDescendingOrder()
        {
            var rows = new List<PostgresStatementStatisticsDto> { NewRow(100), NewRow(300), NewRow(200) };

            var result = PostgresStatementStatisticsService.ApplySort(rows, "queryId", null);

            result.Select(r => r.QueryId).Should().Equal(300, 200, 100);
        }

        [Fact]
        public void RecognisedSortFieldWithAscSortsAscending()
        {
            var rows = new List<PostgresStatementStatisticsDto> { NewRow(300), NewRow(100), NewRow(200) };

            var result = PostgresStatementStatisticsService.ApplySort(rows, "queryId", "asc");

            result.Select(r => r.QueryId).Should().Equal(100, 200, 300);
        }

        [Fact]
        public void SortsByMeanExecTimeMillisecondsAscendingAndDescending()
        {
            var rows = new List<PostgresStatementStatisticsDto>
            {
                NewRow(1, 30), NewRow(2, 10), NewRow(3, 20)
            };

            var ascending = PostgresStatementStatisticsService.ApplySort(rows, "meanExecTimeMilliseconds", "asc");
            ascending.Select(r => r.QueryId).Should().Equal(2, 3, 1);

            var descending = PostgresStatementStatisticsService.ApplySort(rows, "meanExecTimeMilliseconds", "desc");
            descending.Select(r => r.QueryId).Should().Equal(1, 3, 2);
        }
    }
}