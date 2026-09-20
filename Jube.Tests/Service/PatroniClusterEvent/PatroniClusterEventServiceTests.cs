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
using Jube.Service.Exceptions.PatroniClusterEvent;
using Jube.Service.PatroniClusterEvent;
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

namespace Jube.Test.Service.PatroniClusterEvent
{
    using PatroniClusterEventService = PatroniClusterEventService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PatroniClusterEventServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.PatroniClusterEvent.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<PatroniClusterEventService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PatroniClusterEventService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string name,
            PatroniClusterEventType eventType, DateTime? occurredDate = null, string? reason = null,
            int? timelineId = null, long? lsnBytes = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.PatroniClusterEvent
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                Scope = "batman",
                Name = name,
                EventTypeId = (int)eventType,
                Reason = reason,
                TimelineId = timelineId,
                LsnBytes = lsnBytes,
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

            var firstId = await CreateEntryAsync(dbContext, name, PatroniClusterEventType.RoleChanged);
            var secondId = await CreateEntryAsync(dbContext, name, PatroniClusterEventType.Failover,
                reason: "manual failover",
                timelineId: 2, lsnBytes: 12345);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Name == name).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].EventTypeName.Should().Be("Failover");
            mine[0].Reason.Should().Be("manual failover");
            mine[0].TimelineId.Should().Be(2);
            mine[0].LsnBytes.Should().Be(12345);
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
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{name}Old", PatroniClusterEventType.RoleChanged, oldDate);
            await CreateEntryAsync(dbContext, $"{name}New", PatroniClusterEventType.RoleChanged, newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Name.StartsWith(name)).ToList();
            mine.Should().ContainSingle();
            mine[0].Name.Should().Be($"{name}New");
        }

        [Fact]
        public async Task ListFiltersBySearchOnReasonAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueName = $"UniqueNode{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, uniqueName, PatroniClusterEventType.Failover,
                reason: "no recovery target specified");
            await CreateEntryAsync(dbContext, "OtherNode", PatroniClusterEventType.RoleChanged);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: "recovery target");

            var mine = result.Rows.Where(r => r.Name == uniqueName || r.Name == "OtherNode").ToList();
            mine.Should().ContainSingle();
            mine[0].Name.Should().Be(uniqueName);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, PatroniClusterEventType.RoleChanged);
            await CreateEntryAsync(dbContext, name, PatroniClusterEventType.StateChanged);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: name, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, PatroniClusterEventType.RoleChanged);
            await CreateEntryAsync(dbContext, name, PatroniClusterEventType.StateChanged);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: name, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, PatroniClusterEventType.RoleChanged);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
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
            await CreateEntryAsync(dbContext, $"{name}Old", PatroniClusterEventType.RoleChanged, twoHoursAgo);
            await CreateEntryAsync(dbContext, $"{name}New", PatroniClusterEventType.RoleChanged, justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: name);

            var mine = result.Rows.Where(r => r.Name.StartsWith(name)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Name.Should().Be($"{name}New");
        }

        [Fact]
        public async Task ListSortsByNameAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{name}C", PatroniClusterEventType.RoleChanged);
            await CreateEntryAsync(dbContext, $"{name}A", PatroniClusterEventType.RoleChanged);
            await CreateEntryAsync(dbContext, $"{name}B", PatroniClusterEventType.RoleChanged);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: name, sortField: "name", sortDirection: "asc");
            ascending.Rows.Select(r => r.Name).Should().Equal($"{name}A", $"{name}B", $"{name}C");

            var descending = await service.ListAsync(search: name, sortField: "name", sortDirection: "desc");
            descending.Rows.Select(r => r.Name).Should().Equal($"{name}C", $"{name}B", $"{name}A");
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{name}B", PatroniClusterEventType.RoleChanged);
            await CreateEntryAsync(dbContext, $"{name}A", PatroniClusterEventType.RoleChanged);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: name, sortField: "name",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.Name).Should().Equal($"{name}A", $"{name}B");
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{name}A", PatroniClusterEventType.RoleChanged);
            await CreateEntryAsync(dbContext, $"{name}B", PatroniClusterEventType.RoleChanged);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: name, sortField: "name", sortDirection: "banana");

            result.Rows.Select(r => r.Name).Should().Equal($"{name}B", $"{name}A");
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            var firstId = await CreateEntryAsync(dbContext, name, PatroniClusterEventType.RoleChanged,
                DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateEntryAsync(dbContext, name, PatroniClusterEventType.RoleChanged,
                DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
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
                await CreateEntryAsync(dbContext, name, PatroniClusterEventType.RoleChanged);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: name);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsIsEmptyForThisDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, PatroniClusterEventType.Failover, timelineId: 2,
                lsnBytes: 12345);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: name);

            result.Statistics.Columns.Should().BeEmpty(
                "TimelineId and LsnBytes identify a timeline/WAL position rather than measure a quantity, " +
                "and every other column is a string, timestamp or categorical event-type code");
        }
    }
}