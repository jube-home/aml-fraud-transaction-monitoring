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
using Jube.Service.DockerEvent;
using Jube.Service.Exceptions.DockerEvent;
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

namespace Jube.Test.Service.DockerEvent
{
    using DockerEventService = DockerEventService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DockerEventServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.DockerEvent.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<DockerEventService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return DockerEventService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateEntryAsync(DbContext dbContext, string eventType, string action, string actorName,
            DateTime? occurredDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.DockerEvent
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                EventType = eventType,
                Action = action,
                ActorId = $"{DatabaseFixture.Prefix}actor{Guid.NewGuid():N}",
                ActorName = actorName,
                Scope = "local",
                CreatedDate = DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsAllFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}first{Guid.NewGuid():N}";
            var uniqueActorName2 = $"{DatabaseFixture.Prefix}second{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, "container", "start", uniqueActorName);
            await CreateEntryAsync(dbContext, "container", "die", uniqueActorName2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: DatabaseFixture.Prefix);

            var mine = result.Rows.Where(r => r.ActorName == uniqueActorName || r.ActorName == uniqueActorName2)
                .ToList();
            mine.Should().HaveCount(2);
            mine[0].ActorName.Should().Be(uniqueActorName2);
            mine[0].Action.Should().Be("die");
            mine[1].ActorName.Should().Be(uniqueActorName);
            mine[1].Action.Should().Be("start");
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
            var uniqueActorName = $"{DatabaseFixture.Prefix}old{Guid.NewGuid():N}";
            var uniqueActorName2 = $"{DatabaseFixture.Prefix}new{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, "container", "kill", uniqueActorName, oldDate);
            await CreateEntryAsync(dbContext, "container", "kill", uniqueActorName2, newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1), search: DatabaseFixture.Prefix);

            var mine = result.Rows.Where(r => r.ActorName == uniqueActorName || r.ActorName == uniqueActorName2)
                .ToList();
            mine.Should().ContainSingle();
            mine[0].ActorName.Should().Be(uniqueActorName2);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}old{Guid.NewGuid():N}";
            var uniqueActorName2 = $"{DatabaseFixture.Prefix}new{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, "container", "kill", uniqueActorName, twoHoursAgo);
            await CreateEntryAsync(dbContext, "container", "kill", uniqueActorName2, justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: DatabaseFixture.Prefix);

            var mine = result.Rows.Where(r => r.ActorName == uniqueActorName || r.ActorName == uniqueActorName2)
                .ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].ActorName.Should().Be(uniqueActorName2);
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstActionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueAction = $"UniqueAction{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, "container", uniqueAction, "some-container");
            await CreateEntryAsync(dbContext, "container", "other-action", "some-container");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueAction[..12].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Action == uniqueAction).ToList();
            mine.Should().ContainSingle();
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstActorNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}actor{Guid.NewGuid():N}";
            var uniqueAction = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, "container", uniqueAction, uniqueActorName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueActorName);

            result.Rows.Should().Contain(r => r.Action == uniqueAction);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueAction = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "container", uniqueAction, "a");
            await CreateEntryAsync(dbContext, "container", uniqueAction, "b");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueAction, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueAction = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "container", uniqueAction, "a");
            await CreateEntryAsync(dbContext, "container", uniqueAction, "b");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueAction, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueAction = $"{DatabaseFixture.Prefix}crosstenant{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "container", uniqueAction, "a");

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantResult = await otherTenantService.ListAsync(search: uniqueAction);

            otherTenantResult.Rows.Should().Contain(r => r.Action == uniqueAction);
        }

        [Fact]
        public async Task ListSortsByActionAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}sort{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "container", "c-action", uniqueActorName);
            await CreateEntryAsync(dbContext, "container", "a-action", uniqueActorName);
            await CreateEntryAsync(dbContext, "container", "b-action", uniqueActorName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: uniqueActorName, sortField: "action",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.Action).Should().Equal("a-action", "b-action", "c-action");

            var descending = await service.ListAsync(search: uniqueActorName, sortField: "action",
                sortDirection: "desc");
            descending.Rows.Select(r => r.Action).Should().Equal("c-action", "b-action", "a-action");
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}sort{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "container", "b-action", uniqueActorName);
            await CreateEntryAsync(dbContext, "container", "a-action", uniqueActorName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueActorName, sortField: "action",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.Action).Should().Equal("a-action", "b-action");
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}sort{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "container", "a-action", uniqueActorName);
            await CreateEntryAsync(dbContext, "container", "b-action", uniqueActorName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueActorName, sortField: "action",
                sortDirection: "banana");

            result.Rows.Select(r => r.Action).Should().Equal("b-action", "a-action");
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}sort{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "container", "first", uniqueActorName,
                DateTime.UtcNow.AddMinutes(-30));
            await CreateEntryAsync(dbContext, "container", "second", uniqueActorName, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueActorName, sortField: "notARealColumn");

            result.Rows.Select(r => r.Action).Should().Equal("second", "first");
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}total{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, "container", "action", uniqueActorName);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, search: uniqueActorName);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsIsEmptyForThisDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueActorName = $"{DatabaseFixture.Prefix}stats{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "container", "action", uniqueActorName);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueActorName);

            result.Statistics.Should().NotBeNull();
            result.Statistics.Columns.Should().BeEmpty(
                "DockerEventDto has no continuous measured columns -- every column is a string, date, or identifier");
        }
    }
}