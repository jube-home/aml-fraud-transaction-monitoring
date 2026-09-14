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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Exceptions.PostgresTableStatistics;
using Jube.Service.PostgresTableStatistics;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.PostgresTableStatistics
{
    using PostgresTableStatisticsService = PostgresTableStatisticsService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PostgresTableStatisticsServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static Task<PostgresTableStatisticsService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PostgresTableStatisticsService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        [Fact]
        public async Task ListReturnsRealTablesFromTheSchemaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ListAsync();

            result.Rows.Should().NotBeEmpty();
            result.Rows.Should().Contain(r => r.TableName == "EntityAnalysisModel");
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }

        [Fact]
        public async Task CreateWithNullUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, null));
        }

        [Fact]
        public async Task CreateWithUnknownTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var result = await otherTenantService.ListAsync();

            result.Rows.Should().Contain(r => r.TableName == "EntityAnalysisModel");
        }

        [Fact]
        public async Task ListSortsByTotalSizeBytesAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync("totalSizeBytes", "asc");
            ascending.Rows.Select(r => r.TotalSizeBytes).Should().BeInAscendingOrder();

            var descending = await service.ListAsync("totalSizeBytes", "desc");
            descending.Rows.Select(r => r.TotalSizeBytes).Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldPreservesQueryOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var unsorted = await service.ListAsync();
            var withGarbageSort = await service.ListAsync("notARealColumn");

            withGarbageSort.Rows.Select(r => r.TableName).Should().Equal(unsorted.Rows.Select(r => r.TableName));
        }

        [Fact]
        public async Task ListTotalReflectsRowCountAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ListAsync();

            result.Total.Should().Be(result.Rows.Count);
        }

        [Fact]
        public async Task ListStatisticsIncludesEveryContinuousMeasuredColumnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ListAsync();

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "sequentialScans", "sequentialTuplesRead", "indexScans", "indexTuplesFetched", "liveTupleCount",
                "deadTupleCount", "tableSizeBytes", "indexesSizeBytes", "totalSizeBytes");
        }
    }
}