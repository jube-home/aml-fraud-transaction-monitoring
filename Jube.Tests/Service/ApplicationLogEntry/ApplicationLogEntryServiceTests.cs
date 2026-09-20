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
using Jube.Service.ApplicationLogEntry;
using Jube.Service.Exceptions.ApplicationLogEntry;
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

namespace Jube.Test.Service.ApplicationLogEntry
{
    using ApplicationLogEntryService = ApplicationLogEntryService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ApplicationLogEntryServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.ApplicationLogEntry.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<ApplicationLogEntryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ApplicationLogEntryService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateEntryAsync(DbContext dbContext, string level, string loggerName,
            string message, DateTime? occurredDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ApplicationLogEntry
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                Level = level,
                LoggerName = loggerName,
                ThreadContext = "1",
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
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, "WARN", loggerName, "first message");
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "second message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.LoggerName == loggerName).ToList();
            mine.Should().HaveCount(2);
            mine[0].Message.Should().Be("second message");
            mine[0].Level.Should().Be("ERROR");
            mine[1].Message.Should().Be("first message");
            mine[1].Level.Should().Be("WARN");
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
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, "WARN", loggerName, "old message", oldDate);
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "new message", newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.LoggerName == loggerName).ToList();
            mine.Should().ContainSingle();
            mine[0].Message.Should().Be("new message");
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            var uniqueMessage = $"UniqueMessage{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, "WARN", loggerName, uniqueMessage);
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "other message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueMessage[..14].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.LoggerName == loggerName).ToList();
            mine.Should().ContainSingle();
            mine[0].Message.Should().Be(uniqueMessage);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "WARN", loggerName, "first message");
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "second message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: loggerName, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "WARN", loggerName, "first message");
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "second message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: loggerName, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "cross-tenant visibility check");

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.LoggerName == loggerName);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, "WARN", loggerName, "old message", twoHoursAgo);
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "new message", justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: loggerName);

            var mine = result.Rows.Where(r => r.LoggerName == loggerName).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Message.Should().Be("new message");
        }

        [Fact]
        public async Task ListSortsByLevelAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "WARN", loggerName, "message");
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "message");
            await CreateEntryAsync(dbContext, "FATAL", loggerName, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: loggerName, sortField: "level",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.Level).Should().Equal("ERROR", "FATAL", "WARN");

            var descending = await service.ListAsync(search: loggerName, sortField: "level",
                sortDirection: "desc");
            descending.Rows.Select(r => r.Level).Should().Equal("WARN", "FATAL", "ERROR");
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "WARN", loggerName, "message");
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: loggerName, sortField: "level",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.Level).Should().Equal("ERROR", "WARN");
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "message");
            await CreateEntryAsync(dbContext, "WARN", loggerName, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: loggerName, sortField: "level",
                sortDirection: "banana");

            result.Rows.Select(r => r.Level).Should().Equal("WARN", "ERROR");
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "WARN", loggerName, "first message",
                DateTime.UtcNow.AddMinutes(-30));
            await CreateEntryAsync(dbContext, "ERROR", loggerName, "second message", DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: loggerName, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.LoggerName == loggerName).ToList();
            mine[0].Message.Should().Be("second message");
            mine[1].Message.Should().Be("first message");
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, "WARN", loggerName, "message");
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: loggerName);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsIsEmptyForThisDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var loggerName = $"{DatabaseFixture.Prefix}Some.Class{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "WARN", loggerName, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: loggerName);

            result.Statistics.Columns.Should().BeEmpty(
                "every column on this DTO is a string, timestamp, or identifier");
        }
    }
}