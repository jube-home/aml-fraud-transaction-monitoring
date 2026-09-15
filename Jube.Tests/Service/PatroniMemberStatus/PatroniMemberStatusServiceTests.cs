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
using Jube.Service.Exceptions.PatroniMemberStatus;
using Jube.Service.PatroniMemberStatus;
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

namespace Jube.Test.Service.PatroniMemberStatus
{
    using PatroniMemberStatusService = PatroniMemberStatusService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PatroniMemberStatusServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.PatroniMemberStatus.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<PatroniMemberStatusService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PatroniMemberStatusService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string name, DateTime? occurredDate = null,
            string role = "leader", string state = "running", long? lagBytes = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.PatroniMemberStatus
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                Name = name,
                Host = name,
                Port = 5432,
                ApiUrl = $"http://{name}:8008/patroni",
                Role = role,
                State = state,
                TimelineId = 1,
                LagBytes = lagBytes,
                Scope = "batman",
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
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";

            var firstId = await CreateEntryAsync(dbContext, name);
            var secondId = await CreateEntryAsync(dbContext, name);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Name == name).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].Role.Should().Be("leader");
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
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";

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
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueName = $"UniqueReplica{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, uniqueName, role: "replica", lagBytes: 128);
            await CreateEntryAsync(dbContext, "OtherReplica", role: "replica");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueName[..12].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Name == uniqueName || r.Name == "OtherReplica").ToList();
            mine.Should().ContainSingle();
            mine[0].Name.Should().Be(uniqueName);
            mine[0].LagBytes.Should().Be(128);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
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
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
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
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Name == name);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";

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
        public async Task ListSortsByLagBytesAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, lagBytes: 3000);
            await CreateEntryAsync(dbContext, name, lagBytes: 1000);
            await CreateEntryAsync(dbContext, name, lagBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: name, sortField: "lagBytes", sortDirection: "asc");
            ascending.Rows.Select(r => r.LagBytes).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: name, sortField: "lagBytes", sortDirection: "desc");
            descending.Rows.Select(r => r.LagBytes).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, lagBytes: 2000);
            await CreateEntryAsync(dbContext, name, lagBytes: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, sortField: "lagBytes",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.LagBytes).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, lagBytes: 1000);
            await CreateEntryAsync(dbContext, name, lagBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, sortField: "lagBytes", sortDirection: "banana");

            result.Rows.Select(r => r.LagBytes).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
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
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
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
        public async Task ListStatisticsComputesExactValuesForLagBytesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, name, lagBytes: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name);

            var stats = result.Statistics.Columns["lagBytes"];
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
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, lagBytes: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo("lagBytes");
        }
    }
}