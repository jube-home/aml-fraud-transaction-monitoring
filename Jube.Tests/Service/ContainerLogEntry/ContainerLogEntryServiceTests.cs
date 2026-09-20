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
using Jube.Service.ContainerLogEntry;
using Jube.Service.Exceptions.ContainerLogEntry;
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

namespace Jube.Test.Service.ContainerLogEntry
{
    using ContainerLogEntryService = ContainerLogEntryService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ContainerLogEntryServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.ContainerLogEntry.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<ContainerLogEntryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ContainerLogEntryService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateEntryAsync(DbContext dbContext, string containerName,
            ContainerLogStreamType streamType, string message, DateTime? occurredDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ContainerLogEntry
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                ContainerName = containerName,
                StreamTypeId = (int)streamType,
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

            await CreateEntryAsync(dbContext, "etcd", ContainerLogStreamType.Stdout, uniqueMessage);
            await CreateEntryAsync(dbContext, "patroni", ContainerLogStreamType.Stderr, uniqueMessage2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: DatabaseFixture.Prefix);

            var mine = result.Rows.Where(r => r.Message == uniqueMessage || r.Message == uniqueMessage2).ToList();
            mine.Should().HaveCount(2);
            mine[0].Message.Should().Be(uniqueMessage2);
            mine[0].ContainerName.Should().Be("patroni");
            mine[0].StreamTypeName.Should().Be("Stderr");
            mine[1].Message.Should().Be(uniqueMessage);
            mine[1].ContainerName.Should().Be("etcd");
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
            await CreateEntryAsync(dbContext, "redis", ContainerLogStreamType.Stdout, uniqueMessage, oldDate);
            await CreateEntryAsync(dbContext, "redis", ContainerLogStreamType.Stdout, uniqueMessage2, newDate);

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

            await CreateEntryAsync(dbContext, "redis-sentinel", ContainerLogStreamType.Stdout, uniqueMessage);
            await CreateEntryAsync(dbContext, "redis-sentinel", ContainerLogStreamType.Stdout, "other message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueMessage[..14].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Message == uniqueMessage).ToList();
            mine.Should().ContainSingle();
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstContainerNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueContainerName = $"{DatabaseFixture.Prefix}container{Guid.NewGuid():N}";
            var uniqueMessage = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, uniqueContainerName, ContainerLogStreamType.Stdout, uniqueMessage);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueContainerName);

            result.Rows.Should().Contain(r => r.Message == uniqueMessage);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "etcd", ContainerLogStreamType.Stdout, uniqueMessage);
            await CreateEntryAsync(dbContext, "etcd", ContainerLogStreamType.Stderr, uniqueMessage);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueMessage, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "etcd", ContainerLogStreamType.Stdout, uniqueMessage);
            await CreateEntryAsync(dbContext, "etcd", ContainerLogStreamType.Stderr, uniqueMessage);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueMessage, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}crosstenant{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "etcd", ContainerLogStreamType.Stdout, uniqueMessage);

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
            await CreateEntryAsync(dbContext, "redis", ContainerLogStreamType.Stdout, uniqueMessage, twoHoursAgo);
            await CreateEntryAsync(dbContext, "redis", ContainerLogStreamType.Stdout, uniqueMessage2, justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: DatabaseFixture.Prefix);

            var mine = result.Rows.Where(r => r.Message == uniqueMessage || r.Message == uniqueMessage2).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Message.Should().Be(uniqueMessage2);
        }

        [Fact]
        public async Task ListSortsByContainerNameAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}Cont{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{prefix}C", ContainerLogStreamType.Stdout, "message");
            await CreateEntryAsync(dbContext, $"{prefix}A", ContainerLogStreamType.Stdout, "message");
            await CreateEntryAsync(dbContext, $"{prefix}B", ContainerLogStreamType.Stdout, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: prefix, sortField: "containerName",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.ContainerName).Should().Equal($"{prefix}A", $"{prefix}B", $"{prefix}C");

            var descending = await service.ListAsync(search: prefix, sortField: "containerName",
                sortDirection: "desc");
            descending.Rows.Select(r => r.ContainerName).Should().Equal($"{prefix}C", $"{prefix}B", $"{prefix}A");
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}Cont{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{prefix}B", ContainerLogStreamType.Stdout, "message");
            await CreateEntryAsync(dbContext, $"{prefix}A", ContainerLogStreamType.Stdout, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: prefix, sortField: "containerName",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.ContainerName).Should().Equal($"{prefix}A", $"{prefix}B");
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}Cont{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, $"{prefix}A", ContainerLogStreamType.Stdout, "message");
            await CreateEntryAsync(dbContext, $"{prefix}B", ContainerLogStreamType.Stdout, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: prefix, sortField: "containerName",
                sortDirection: "banana");

            result.Rows.Select(r => r.ContainerName).Should().Equal($"{prefix}B", $"{prefix}A");
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMessage = $"{DatabaseFixture.Prefix}first{Guid.NewGuid():N}";
            var uniqueMessage2 = $"{DatabaseFixture.Prefix}second{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, "etcd", ContainerLogStreamType.Stdout, uniqueMessage,
                DateTime.UtcNow.AddMinutes(-30));
            await CreateEntryAsync(dbContext, "etcd", ContainerLogStreamType.Stdout, uniqueMessage2,
                DateTime.UtcNow);

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
            var prefix = $"{DatabaseFixture.Prefix}Cont{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, prefix, ContainerLogStreamType.Stdout, "message");
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
            var prefix = $"{DatabaseFixture.Prefix}Cont{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, prefix, ContainerLogStreamType.Stdout, "message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: prefix);

            result.Statistics.Columns.Should().BeEmpty(
                "every column on this DTO is a string, timestamp, or categorical stream-type code");
        }
    }
}