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
using Jube.Service.Exceptions.RedisSlowOperation;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.RedisSlowOperation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.RedisSlowOperation
{
    using RedisSlowOperationService = RedisSlowOperationService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RedisSlowOperationServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.RedisSlowOperation.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<RedisSlowOperationService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return RedisSlowOperationService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateEntryAsync(DbContext dbContext, long redisSlowLogId, string command,
            DateTime? occurredDate = null, string? commandName = null, string? keyName = null,
            long durationMicroseconds = 15000)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RedisSlowOperation
            {
                RedisSlowLogId = redisSlowLogId,
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                DurationMicroseconds = durationMicroseconds,
                Command = command,
                CommandName = commandName,
                KeyName = keyName,
                ClientAddress = "127.0.0.1:6379",
                CreatedDate = DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, 1, command);
            await CreateEntryAsync(dbContext, 2, command);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Command == command).ToList();
            mine.Should().HaveCount(2);
            mine[0].RedisSlowLogId.Should().Be(2);
            mine[1].RedisSlowLogId.Should().Be(1);
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
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, 10, $"{command}Old", oldDate);
            await CreateEntryAsync(dbContext, 11, $"{command}New", newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Command.StartsWith(command)).ToList();
            mine.Should().ContainSingle();
            mine[0].Command.Should().Be($"{command}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueCommand = $"UniqueCommand{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, 20, uniqueCommand);
            await CreateEntryAsync(dbContext, 21, "OtherCommand");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueCommand[..14].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Command == uniqueCommand || r.Command == "OtherCommand").ToList();
            mine.Should().ContainSingle();
            mine[0].Command.Should().Be(uniqueCommand);
        }

        [Fact]
        public async Task ListProjectsAndFiltersByCommandNameAndKeyNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueCommandName = $"UNIQUECMD{Guid.NewGuid():N}";
            var uniqueKeyName = $"UniqueKey{Guid.NewGuid():N}";
            var fullCommand = $"{uniqueCommandName} {uniqueKeyName} value";

            await CreateEntryAsync(dbContext, 30, fullCommand, commandName: uniqueCommandName,
                keyName: uniqueKeyName);
            await CreateEntryAsync(dbContext, 31, "OtherCommand OtherKey", commandName: "OTHERCMD",
                keyName: "OtherKey");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueCommandName);

            var mine = result.Rows.Where(r => r.Command == fullCommand || r.Command == "OtherCommand OtherKey")
                .ToList();
            mine.Should().ContainSingle();
            mine[0].CommandName.Should().Be(uniqueCommandName);
            mine[0].KeyName.Should().Be(uniqueKeyName);

            var byKey = await service.ListAsync(search: uniqueKeyName);
            var mineByKey = byKey.Rows.Where(r => r.Command == fullCommand || r.Command == "OtherCommand OtherKey")
                .ToList();
            mineByKey.Should().ContainSingle();
            mineByKey[0].CommandName.Should().Be(uniqueCommandName);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 40, command);
            await CreateEntryAsync(dbContext, 41, command);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: command, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 42, command);
            await CreateEntryAsync(dbContext, 43, command);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: command, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListClampsSamplePercentageToZeroToOneHundredRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 44, command);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var negative = await service.ListAsync(search: command, samplePercentage: -10);
            negative.Rows.Should().BeEmpty();

            var overHundred = await service.ListAsync(search: command, samplePercentage: 150);
            overHundred.Rows.Should().ContainSingle();
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 3, command);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Command == command);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, 100, $"{command}Old", twoHoursAgo);
            await CreateEntryAsync(dbContext, 101, $"{command}New", justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: command);

            var mine = result.Rows.Where(r => r.Command.StartsWith(command)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Command.Should().Be($"{command}New");
        }

        [Fact]
        public async Task ListSortsByDurationMicrosecondsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 110, command, durationMicroseconds: 3000);
            await CreateEntryAsync(dbContext, 111, command, durationMicroseconds: 1000);
            await CreateEntryAsync(dbContext, 112, command, durationMicroseconds: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: command, sortField: "durationMicroseconds",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.DurationMicroseconds).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: command, sortField: "durationMicroseconds",
                sortDirection: "desc");
            descending.Rows.Select(r => r.DurationMicroseconds).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 120, command, durationMicroseconds: 2000);
            await CreateEntryAsync(dbContext, 121, command, durationMicroseconds: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: command, sortField: "durationMicroseconds",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.DurationMicroseconds).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 130, command, durationMicroseconds: 1000);
            await CreateEntryAsync(dbContext, 131, command, durationMicroseconds: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: command, sortField: "durationMicroseconds",
                sortDirection: "banana");

            result.Rows.Select(r => r.DurationMicroseconds).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 140, command, DateTime.UtcNow.AddMinutes(-30));
            await CreateEntryAsync(dbContext, 141, command, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: command, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Command == command).ToList();
            mine[0].RedisSlowLogId.Should().Be(141);
            mine[1].RedisSlowLogId.Should().Be(140);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, 150 + i, command);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, search: command);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForDurationMicrosecondsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";

            var logId = 160L;
            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, logId++, command, durationMicroseconds: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: command);

            var stats = result.Statistics.Columns["durationMicroseconds"];
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
            var command = $"{DatabaseFixture.Prefix}Command{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, 170, command);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: command);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo("durationMicroseconds");
        }
    }
}