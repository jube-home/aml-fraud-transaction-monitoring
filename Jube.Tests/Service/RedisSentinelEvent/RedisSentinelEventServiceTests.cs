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
using Jube.Service.Exceptions.RedisSentinelEvent;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.RedisSentinelEvent;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.RedisSentinelEvent
{
    using RedisSentinelEventService = RedisSentinelEventService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RedisSentinelEventServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.RedisSentinelEvent.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<RedisSentinelEventService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return RedisSentinelEventService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string channel, string message,
            DateTime? occurredDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RedisSentinelEvent
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                Channel = channel,
                Message = message,
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
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";

            var firstId = await CreateEntryAsync(dbContext, channel, "first message");
            var secondId = await CreateEntryAsync(dbContext, channel, "second message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Channel == channel).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].Message.Should().Be("second message");
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
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{channel}Old", "old message", oldDate);
            await CreateEntryAsync(dbContext, $"{channel}New", "new message", newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Channel.StartsWith(channel)).ToList();
            mine.Should().ContainSingle();
            mine[0].Channel.Should().Be($"{channel}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueChannel = $"+UniqueChannel{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, uniqueChannel, "some message");
            await CreateEntryAsync(dbContext, "+OtherChannel", "some message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueChannel[..14].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Channel == uniqueChannel || r.Channel == "+OtherChannel").ToList();
            mine.Should().ContainSingle();
            mine[0].Channel.Should().Be(uniqueChannel);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, channel, "first message");
            await CreateEntryAsync(dbContext, channel, "second message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: channel, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, channel, "first message");
            await CreateEntryAsync(dbContext, channel, "second message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: channel, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, channel, "message");

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Channel == channel);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{channel}Old", "old message", twoHoursAgo);
            await CreateEntryAsync(dbContext, $"{channel}New", "new message", justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: channel);

            var mine = result.Rows.Where(r => r.Channel.StartsWith(channel)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Channel.Should().Be($"{channel}New");
        }

        [Fact]
        public async Task ListSortsByChannelAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{channel}C", "message");
            await CreateEntryAsync(dbContext, $"{channel}A", "message");
            await CreateEntryAsync(dbContext, $"{channel}B", "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: channel, sortField: "channel", sortDirection: "asc");
            ascending.Rows.Select(r => r.Channel).Should().Equal($"{channel}A", $"{channel}B", $"{channel}C");

            var descending = await service.ListAsync(search: channel, sortField: "channel",
                sortDirection: "desc");
            descending.Rows.Select(r => r.Channel).Should().Equal($"{channel}C", $"{channel}B", $"{channel}A");
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{channel}B", "message");
            await CreateEntryAsync(dbContext, $"{channel}A", "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: channel, sortField: "channel",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.Channel).Should().Equal($"{channel}A", $"{channel}B");
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{channel}A", "message");
            await CreateEntryAsync(dbContext, $"{channel}B", "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: channel, sortField: "channel", sortDirection: "banana");

            result.Rows.Select(r => r.Channel).Should().Equal($"{channel}B", $"{channel}A");
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            var firstId = await CreateEntryAsync(dbContext, channel, "message",
                DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateEntryAsync(dbContext, channel, "message", DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: channel, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, channel, "message");
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, search: channel);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsIsEmptyForThisDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var channel = $"{DatabaseFixture.Prefix}Channel{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, channel, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: channel);

            result.Statistics.Columns.Should().BeEmpty(
                "every column on this DTO is a string, timestamp, or identifier");
        }
    }
}