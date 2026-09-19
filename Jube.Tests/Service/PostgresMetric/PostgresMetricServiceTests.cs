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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Exceptions.PostgresMetric;
using Jube.Service.PostgresMetric;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.PostgresMetric
{
    using PostgresMetricService = PostgresMetricService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PostgresMetricServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.PostgresMetric.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<PostgresMetricService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PostgresMetricService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateSampleAsync(DbContext dbContext, string instance, int activeConnections,
            DateTime? createdDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.PostgresMetric
            {
                CreatedDate = createdDate ?? DateTime.UtcNow,
                Instance = instance,
                ActiveConnections = activeConnections
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            await CreateSampleAsync(dbContext, instance, 5);
            await CreateSampleAsync(dbContext, instance, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Instance == instance).ToList();
            mine.Should().HaveCount(2);
            mine[0].ActiveConnections.Should().Be(10);
            mine[1].ActiveConnections.Should().Be(5);
        }

        [Fact]
        public async Task ListClampsTakeToOneHundredThousandAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var result = await service.ListAsync(500000);

            result.Rows.Count.Should().BeLessOrEqualTo(100000);
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }

        [Fact]
        public async Task NonLandlordHoldingEverySpecificationIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

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
        public async Task ListFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, instance, 5, oldDate);
            await CreateSampleAsync(dbContext, instance, 10, newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Instance == instance).ToList();
            mine.Should().ContainSingle();
            mine[0].ActiveConnections.Should().Be(10);
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueInstance = $"{DatabaseFixture.Prefix}UniqueInstance{Guid.NewGuid():N}";
            var otherInstance = $"{DatabaseFixture.Prefix}OtherInstance{Guid.NewGuid():N}";

            await CreateSampleAsync(dbContext, uniqueInstance, 5);
            await CreateSampleAsync(dbContext, otherInstance, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueInstance[..20].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Instance == uniqueInstance || r.Instance == otherInstance).ToList();
            mine.Should().ContainSingle();
            mine[0].Instance.Should().Be(uniqueInstance);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 5);
            await CreateSampleAsync(dbContext, instance, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 5);
            await CreateSampleAsync(dbContext, instance, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 20);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Instance == instance);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, $"{instance}Old", 5, twoHoursAgo);
            await CreateSampleAsync(dbContext, $"{instance}New", 10, justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance);

            var mine = result.Rows.Where(r => r.Instance.StartsWith(instance)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Instance.Should().Be($"{instance}New");
        }

        [Fact]
        public async Task ListSortsByActiveConnectionsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 3000);
            await CreateSampleAsync(dbContext, instance, 1000);
            await CreateSampleAsync(dbContext, instance, 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: instance, sortField: "activeConnections",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.ActiveConnections).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: instance, sortField: "activeConnections",
                sortDirection: "desc");
            descending.Rows.Select(r => r.ActiveConnections).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 2000);
            await CreateSampleAsync(dbContext, instance, 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance, sortField: "activeConnections",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.ActiveConnections).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 1000);
            await CreateSampleAsync(dbContext, instance, 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance, sortField: "activeConnections",
                sortDirection: "banana");

            result.Rows.Select(r => r.ActiveConnections).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 5, DateTime.UtcNow.AddMinutes(-30));
            await CreateSampleAsync(dbContext, instance, 10, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Instance == instance).ToList();
            mine[0].ActiveConnections.Should().Be(10);
            mine[1].ActiveConnections.Should().Be(5);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateSampleAsync(dbContext, instance, i);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: instance);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForActiveConnectionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            foreach (var value in new[] { 1, 2, 3, 4, 5 })
            {
                await CreateSampleAsync(dbContext, instance, value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance);

            var stats = result.Statistics.Columns["activeConnections"];
            stats.Min.Should().Be(1);
            stats.Max.Should().Be(5);
            stats.Mean.Should().Be(3);
            stats.Median.Should().Be(3);
            stats.StandardDeviation.Should().BeApproximately(1.5811, 0.001);
            stats.Histogram.Sum(b => b.Frequency).Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsIncludesEveryContinuousMeasuredColumnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "activeConnections", "transactionsCommitted", "transactionsRolledBack", "blocksRead",
                "blocksHit", "cacheHitRatioPercent", "rowsReturned", "rowsFetched", "rowsInserted",
                "rowsUpdated", "rowsDeleted", "deadlocks", "tempFilesCreated", "tempBytesWritten", "conflicts",
                "replicationLagSeconds", "replicaCount", "databaseSizeBytes", "longestRunningQuerySeconds",
                "waitingBackends", "walBytesGenerated");
        }
    }
}