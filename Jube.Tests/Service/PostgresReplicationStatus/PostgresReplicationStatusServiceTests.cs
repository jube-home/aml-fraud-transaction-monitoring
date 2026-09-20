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
using Jube.Service.Exceptions.PostgresReplicationStatus;
using Jube.Service.PostgresReplicationStatus;
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

namespace Jube.Test.Service.PostgresReplicationStatus
{
    using PostgresReplicationStatusService = PostgresReplicationStatusService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PostgresReplicationStatusServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.PostgresReplicationStatus.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<PostgresReplicationStatusService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PostgresReplicationStatusService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, int pid, string applicationName,
            DateTime? occurredDate = null, string state = "streaming", double replayLagSeconds = 0.03)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.PostgresReplicationStatus
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                Pid = pid,
                UserName = "replicator",
                ApplicationName = applicationName,
                ClientAddress = "10.0.0.5",
                State = state,
                SentLsn = "0/1000000",
                WriteLsn = "0/1000000",
                FlushLsn = "0/1000000",
                ReplayLsn = "0/1000000",
                WriteLagSeconds = 0.01,
                FlushLagSeconds = 0.02,
                ReplayLagSeconds = replayLagSeconds,
                SyncState = "async",
                SyncPriority = 0,
                CreatedDate = DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
            return id;
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";

            var firstId = await CreateEntryAsync(dbContext, 1001, applicationName);
            var secondId = await CreateEntryAsync(dbContext, 1002, applicationName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.ApplicationName == applicationName).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].SyncState.Should().Be("async");
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
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, 2001, $"{applicationName}Old", oldDate);
            await CreateEntryAsync(dbContext, 2002, $"{applicationName}New", newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.ApplicationName.StartsWith(applicationName)).ToList();
            mine.Should().ContainSingle();
            mine[0].ApplicationName.Should().Be($"{applicationName}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueApplicationName = $"UniqueApp{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, 3001, uniqueApplicationName);
            await CreateEntryAsync(dbContext, 3002, "OtherApp");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueApplicationName[..12].ToUpperInvariant());

            var mine = result.Rows
                .Where(r => r.ApplicationName == uniqueApplicationName || r.ApplicationName == "OtherApp")
                .ToList();
            mine.Should().ContainSingle();
            mine[0].ApplicationName.Should().Be(uniqueApplicationName);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 5001, applicationName);
            await CreateEntryAsync(dbContext, 5002, applicationName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: applicationName, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 5003, applicationName);
            await CreateEntryAsync(dbContext, 5004, applicationName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: applicationName, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 4001, applicationName);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.ApplicationName == applicationName);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, 6001, $"{applicationName}Old", twoHoursAgo);
            await CreateEntryAsync(dbContext, 6002, $"{applicationName}New", justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: applicationName);

            var mine = result.Rows.Where(r => r.ApplicationName.StartsWith(applicationName)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].ApplicationName.Should().Be($"{applicationName}New");
        }

        [Fact]
        public async Task ListSortsByReplayLagSecondsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 7001, applicationName, replayLagSeconds: 3);
            await CreateEntryAsync(dbContext, 7002, applicationName, replayLagSeconds: 1);
            await CreateEntryAsync(dbContext, 7003, applicationName, replayLagSeconds: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: applicationName, sortField: "replayLagSeconds",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.ReplayLagSeconds).Should().Equal(1, 2, 3);

            var descending = await service.ListAsync(search: applicationName, sortField: "replayLagSeconds",
                sortDirection: "desc");
            descending.Rows.Select(r => r.ReplayLagSeconds).Should().Equal(3, 2, 1);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 7004, applicationName, replayLagSeconds: 2);
            await CreateEntryAsync(dbContext, 7005, applicationName, replayLagSeconds: 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: applicationName, sortField: "replayLagSeconds",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.ReplayLagSeconds).Should().Equal(1, 2);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 7006, applicationName, replayLagSeconds: 1);
            await CreateEntryAsync(dbContext, 7007, applicationName, replayLagSeconds: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: applicationName, sortField: "replayLagSeconds",
                sortDirection: "banana");

            result.Rows.Select(r => r.ReplayLagSeconds).Should().Equal(2, 1);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            var firstId = await CreateEntryAsync(dbContext, 7008, applicationName,
                DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateEntryAsync(dbContext, 7009, applicationName, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: applicationName, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, 8000 + i, applicationName);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: applicationName);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForReplayLagSecondsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";

            var pid = 9000;
            foreach (var value in new double[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, pid++, applicationName, replayLagSeconds: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: applicationName);

            var stats = result.Statistics.Columns["replayLagSeconds"];
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
            var applicationName = $"{DatabaseFixture.Prefix}App{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 9500, applicationName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: applicationName);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "writeLagSeconds", "flushLagSeconds", "replayLagSeconds", "syncPriority");
        }
    }
}