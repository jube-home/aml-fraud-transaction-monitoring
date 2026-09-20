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
using Jube.Service.EtcdMemberStatus;
using Jube.Service.Exceptions.EtcdMemberStatus;
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

namespace Jube.Test.Service.EtcdMemberStatus
{
    using EtcdMemberStatusService = EtcdMemberStatusService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EtcdMemberStatusServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.EtcdMemberStatus.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<EtcdMemberStatusService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EtcdMemberStatusService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string endpoint, DateTime? occurredDate = null,
            string name = "etcd1", string memberId = "1", string leaderId = "1", int alarmCount = 0,
            string? alarms = null, long dbSizeBytes = 20480)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EtcdMemberStatus
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                Endpoint = endpoint,
                MemberId = memberId,
                Name = name,
                Version = "3.5.9",
                HealthOk = true,
                LeaderId = leaderId,
                IsLeader = memberId == leaderId,
                HasLeader = true,
                DbSizeBytes = dbSizeBytes,
                AlarmCount = alarmCount,
                Alarms = alarms,
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
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";

            var firstId = await CreateEntryAsync(dbContext, endpoint);
            var secondId = await CreateEntryAsync(dbContext, endpoint);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Endpoint == endpoint).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].IsLeader.Should().BeTrue();
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
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{endpoint}Old", oldDate);
            await CreateEntryAsync(dbContext, $"{endpoint}New", newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Endpoint.StartsWith(endpoint)).ToList();
            mine.Should().ContainSingle();
            mine[0].Endpoint.Should().Be($"{endpoint}New");
        }

        [Fact]
        public async Task ListFiltersBySearchOnAlarmsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueEndpoint = $"UniqueHost{Guid.NewGuid():N}:2379";

            await CreateEntryAsync(dbContext, uniqueEndpoint, alarmCount: 1, alarms: "NOSPACE");
            await CreateEntryAsync(dbContext, "OtherHost:2379");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: "NOSPACE");

            var mine = result.Rows.Where(r => r.Endpoint == uniqueEndpoint || r.Endpoint == "OtherHost:2379").ToList();
            mine.Should().ContainSingle();
            mine[0].Endpoint.Should().Be(uniqueEndpoint);
            mine[0].AlarmCount.Should().Be(1);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            await CreateEntryAsync(dbContext, endpoint);
            await CreateEntryAsync(dbContext, endpoint);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: endpoint, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            await CreateEntryAsync(dbContext, endpoint);
            await CreateEntryAsync(dbContext, endpoint);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: endpoint, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            await CreateEntryAsync(dbContext, endpoint);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Endpoint == endpoint);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{endpoint}Old", twoHoursAgo);
            await CreateEntryAsync(dbContext, $"{endpoint}New", justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: endpoint);

            var mine = result.Rows.Where(r => r.Endpoint.StartsWith(endpoint)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Endpoint.Should().Be($"{endpoint}New");
        }

        [Fact]
        public async Task ListSortsByDbSizeBytesAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            await CreateEntryAsync(dbContext, endpoint, dbSizeBytes: 3000);
            await CreateEntryAsync(dbContext, endpoint, dbSizeBytes: 1000);
            await CreateEntryAsync(dbContext, endpoint, dbSizeBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: endpoint, sortField: "dbSizeBytes",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.DbSizeBytes).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: endpoint, sortField: "dbSizeBytes",
                sortDirection: "desc");
            descending.Rows.Select(r => r.DbSizeBytes).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            await CreateEntryAsync(dbContext, endpoint, dbSizeBytes: 2000);
            await CreateEntryAsync(dbContext, endpoint, dbSizeBytes: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: endpoint, sortField: "dbSizeBytes",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.DbSizeBytes).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            await CreateEntryAsync(dbContext, endpoint, dbSizeBytes: 1000);
            await CreateEntryAsync(dbContext, endpoint, dbSizeBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: endpoint, sortField: "dbSizeBytes",
                sortDirection: "banana");

            result.Rows.Select(r => r.DbSizeBytes).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            var firstId = await CreateEntryAsync(dbContext, endpoint, DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateEntryAsync(dbContext, endpoint, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: endpoint, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, endpoint);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: endpoint);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForDbSizeBytesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, endpoint, dbSizeBytes: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: endpoint);

            var stats = result.Statistics.Columns["dbSizeBytes"];
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
            var endpoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:2379";
            await CreateEntryAsync(dbContext, endpoint);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: endpoint);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "leaderChangesTotal", "dbSizeBytes", "dbSizeInUseBytes", "proposalsCommittedTotal",
                "proposalsAppliedTotal", "proposalsPendingCount", "proposalsFailedTotal",
                "walFsyncAvgMicroseconds", "backendCommitAvgMicroseconds", "slowApplyTotal",
                "slowReadIndexesTotal", "alarmCount");
        }
    }
}