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
using Jube.Service.Exceptions.PostgresLogEntry;
using Jube.Service.PostgresLogEntry;
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

namespace Jube.Test.Service.PostgresLogEntry
{
    using PostgresLogEntryService = PostgresLogEntryService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PostgresLogEntryServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.PostgresLogEntry.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<PostgresLogEntryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PostgresLogEntryService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateEntryAsync(DbContext dbContext, string level, string message,
            DateTime? occurredDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.PostgresLogEntry
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                Level = level,
                Pid = 1,
                Message = message,
                CreatedDate = DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsAllFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}first{Guid.NewGuid():N}";
            var uniqueMessage2 = $"{DatabaseFixture.Prefix}second{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, "LOG", uniqueMessage);
            await CreateEntryAsync(dbContext, "ERROR", uniqueMessage2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: DatabaseFixture.Prefix);

            var mine = result.Rows.Where(r => r.Message == uniqueMessage || r.Message == uniqueMessage2).ToList();
            mine.Should().HaveCount(2);
            mine[0].Message.Should().Be(uniqueMessage2);
            mine[0].Level.Should().Be("ERROR");
            mine[1].Message.Should().Be(uniqueMessage);
            mine[1].Level.Should().Be("LOG");
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
            var uniqueMessage = $"{DatabaseFixture.Prefix}old{Guid.NewGuid():N}";
            var uniqueMessage2 = $"{DatabaseFixture.Prefix}new{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, "LOG", uniqueMessage, oldDate);
            await CreateEntryAsync(dbContext, "ERROR", uniqueMessage2, newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1), search: DatabaseFixture.Prefix);

            var mine = result.Rows.Where(r => r.Message == uniqueMessage || r.Message == uniqueMessage2).ToList();
            mine.Should().ContainSingle();
            mine[0].Message.Should().Be(uniqueMessage2);
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"UniqueMessage{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, "LOG", uniqueMessage);
            await CreateEntryAsync(dbContext, "LOG", "other message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueMessage[..14].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Message == uniqueMessage).ToList();
            mine.Should().ContainSingle();
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "LOG", uniqueMessage);
            await CreateEntryAsync(dbContext, "ERROR", uniqueMessage);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueMessage, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "LOG", uniqueMessage);
            await CreateEntryAsync(dbContext, "ERROR", uniqueMessage);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueMessage, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}crosstenant{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "ERROR", uniqueMessage);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var otherTenantResult = await otherTenantService.ListAsync(search: uniqueMessage);

            otherTenantResult.Rows.Should().Contain(r => r.Message == uniqueMessage);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}old{Guid.NewGuid():N}";
            var uniqueMessage2 = $"{DatabaseFixture.Prefix}new{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, "LOG", uniqueMessage, twoHoursAgo);
            await CreateEntryAsync(dbContext, "ERROR", uniqueMessage2, justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: DatabaseFixture.Prefix);

            var mine = result.Rows.Where(r => r.Message == uniqueMessage || r.Message == uniqueMessage2).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Message.Should().Be(uniqueMessage2);
        }

        [Fact]
        public async Task ListSortsByLevelAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "WARNING", prefix);
            await CreateEntryAsync(dbContext, "ERROR", prefix);
            await CreateEntryAsync(dbContext, "LOG", prefix);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: prefix, sortField: "level", sortDirection: "asc");
            ascending.Rows.Select(r => r.Level).Should().Equal("ERROR", "LOG", "WARNING");

            var descending = await service.ListAsync(search: prefix, sortField: "level",
                sortDirection: "desc");
            descending.Rows.Select(r => r.Level).Should().Equal("WARNING", "LOG", "ERROR");
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "LOG", prefix);
            await CreateEntryAsync(dbContext, "ERROR", prefix);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: prefix, sortField: "level",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.Level).Should().Equal("ERROR", "LOG");
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "ERROR", prefix);
            await CreateEntryAsync(dbContext, "LOG", prefix);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: prefix, sortField: "level", sortDirection: "banana");

            result.Rows.Select(r => r.Level).Should().Equal("LOG", "ERROR");
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}first{Guid.NewGuid():N}";
            var uniqueMessage2 = $"{DatabaseFixture.Prefix}second{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "LOG", uniqueMessage, DateTime.UtcNow.AddMinutes(-30));
            await CreateEntryAsync(dbContext, "LOG", uniqueMessage2, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: DatabaseFixture.Prefix, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Message == uniqueMessage || r.Message == uniqueMessage2).ToList();
            mine[0].Message.Should().Be(uniqueMessage2);
            mine[1].Message.Should().Be(uniqueMessage);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, "LOG", prefix);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: prefix);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsIsEmptyForThisDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "LOG", prefix);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: prefix);

            result.Statistics.Columns.Should().BeEmpty(
                "Pid is a process identifier, not a measurement, and every other column is a string or timestamp");
        }
    }
}