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
using Jube.Data.Repository;
using Jube.Service.Exceptions.RedisSentinelStatus;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.RedisSentinelStatus;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.RedisSentinelStatus
{
    using RedisSentinelStatusService = RedisSentinelStatusService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RedisSentinelStatusServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.RedisSentinelStatus.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<RedisSentinelStatusService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return RedisSentinelStatusService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string name, DateTime? occurredDate = null,
            RedisSentinelEntityType entityType = RedisSentinelEntityType.Master, string flags = "master",
            double? replicationLagSeconds = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RedisSentinelStatus
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                EntityTypeId = (int)entityType,
                Name = name,
                Ip = "10.0.0.10",
                Port = 6379,
                Flags = flags,
                ReplicationLagSeconds = replicationLagSeconds,
                RunId = Guid.NewGuid().ToString("N"),
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
            var name = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";

            var firstId = await CreateEntryAsync(dbContext, name);
            var secondId = await CreateEntryAsync(dbContext, name);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Name == name).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].EntityTypeName.Should().Be("Master");
        }

        [Fact]
        public async Task ListClampsTakeToOneHundredThousandAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

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
            var name = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{name}Old", oldDate);
            await CreateEntryAsync(dbContext, $"{name}New", newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Name.StartsWith(name)).ToList();
            mine.Should().ContainSingle();
            mine[0].Name.Should().Be($"{name}New");
        }

        [Fact]
        public async Task ListProjectsReplicationLagSecondsOnlyForSlaveRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var slaveName = $"{DatabaseFixture.Prefix}Slave{Guid.NewGuid():N}";
            var masterName = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, slaveName, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 2.5);
            await CreateEntryAsync(dbContext, masterName, entityType: RedisSentinelEntityType.Master,
                flags: "master");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var slaveRow = result.Rows.Should().ContainSingle(r => r.Name == slaveName).Subject;
            slaveRow.ReplicationLagSeconds.Should().Be(2.5);

            var masterRow = result.Rows.Should().ContainSingle(r => r.Name == masterName).Subject;
            masterRow.ReplicationLagSeconds.Should().BeNull();
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueName = $"UniqueMaster{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, uniqueName, flags: "s_down,master");
            await CreateEntryAsync(dbContext, "OtherMaster");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: "s_down");

            var mine = result.Rows.Where(r => r.Name == uniqueName || r.Name == "OtherMaster").ToList();
            mine.Should().ContainSingle();
            mine[0].Name.Should().Be(uniqueName);
            mine[0].Flags.Should().Contain("s_down");
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name);
            await CreateEntryAsync(dbContext, name);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name);
            await CreateEntryAsync(dbContext, name);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Name == name);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{name}Old", twoHoursAgo);
            await CreateEntryAsync(dbContext, $"{name}New", justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name);

            var mine = result.Rows.Where(r => r.Name.StartsWith(name)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Name.Should().Be($"{name}New");
        }

        [Fact]
        public async Task ListSortsByReplicationLagSecondsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Slave{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 3);
            await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 1);
            await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: name, sortField: "replicationLagSeconds",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.ReplicationLagSeconds).Should().Equal(1, 2, 3);

            var descending = await service.ListAsync(search: name, sortField: "replicationLagSeconds",
                sortDirection: "desc");
            descending.Rows.Select(r => r.ReplicationLagSeconds).Should().Equal(3, 2, 1);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Slave{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 2);
            await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, sortField: "replicationLagSeconds",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.ReplicationLagSeconds).Should().Equal(1, 2);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Slave{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 1);
            await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, sortField: "replicationLagSeconds",
                sortDirection: "banana");

            result.Rows.Select(r => r.ReplicationLagSeconds).Should().Equal(2, 1);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";
            var firstId = await CreateEntryAsync(dbContext, name, DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateEntryAsync(dbContext, name, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Master{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, name);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, search: name);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForReplicationLagSecondsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Slave{Guid.NewGuid():N}";

            foreach (var value in new double[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                    replicationLagSeconds: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name);

            var stats = result.Statistics.Columns["replicationLagSeconds"];
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
            var name = $"{DatabaseFixture.Prefix}Slave{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, entityType: RedisSentinelEntityType.Slave, flags: "slave",
                replicationLagSeconds: 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "replicationLagSeconds", "numSlaves", "numOtherSentinels", "quorum", "downAfterMilliseconds");
        }
    }
}