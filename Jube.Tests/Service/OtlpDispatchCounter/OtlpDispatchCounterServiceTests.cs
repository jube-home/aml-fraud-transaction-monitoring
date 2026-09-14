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
using Jube.Service.Exceptions.OtlpDispatchCounter;
using Jube.Service.OtlpDispatchCounter;
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

namespace Jube.Test.Service.OtlpDispatchCounter
{
    using OtlpDispatchCounterService = OtlpDispatchCounterService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class OtlpDispatchCounterServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.OtlpDispatchCounter.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<OtlpDispatchCounterService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return OtlpDispatchCounterService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateCounterAsync(DbContext dbContext, OtlpSignal signal, long count = 5,
            long successCount = 4, long failureCount = 1, long itemCount = 50, long droppedCount = 0,
            long totalMicroseconds = 5000, long minMicroseconds = 500, long maxMicroseconds = 1500,
            DateTime? createdDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.OtlpDispatchCounter
            {
                SignalId = (int)signal,
                Count = count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                ItemCount = itemCount,
                DroppedCount = droppedCount,
                TotalMicroseconds = totalMicroseconds,
                MinMicroseconds = minMicroseconds,
                MaxMicroseconds = maxMicroseconds,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
            return id;
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var firstId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 3, 2, 1, 30, 0, 3000, 800, 1200);
            var secondId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 7, 5, 2, 70, 4, 9000, 900, 1600);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].Count.Should().Be(7);
            mine[0].SuccessCount.Should().Be(5);
            mine[0].FailureCount.Should().Be(2);
            mine[0].ItemCount.Should().Be(70);
            mine[0].DroppedCount.Should().Be(4);
            mine[0].TotalMicroseconds.Should().Be(9000);
            mine[0].MinMicroseconds.Should().Be(900);
            mine[0].MaxMicroseconds.Should().Be(1600);
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
        public async Task ListFiltersBySignalIdAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var tracesId = await CreateCounterAsync(dbContext, OtlpSignal.Traces);
            var logsId = await CreateCounterAsync(dbContext, OtlpSignal.Logs);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(signalId: (int)OtlpSignal.Traces);

            var mine = result.Rows.Where(r => r.Id == tracesId || r.Id == logsId).ToList();
            mine.Should().ContainSingle();
            mine[0].Id.Should().Be(tracesId);
            mine[0].SignalName.Should().Be("Traces");
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            var oldId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, createdDate: twoHoursAgo);
            var newId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, createdDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Id == oldId || r.Id == newId).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Id.Should().Be(newId);
        }

        [Fact]
        public async Task ListSortsByCountAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var lowId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 10);
            var midId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 20);
            var highId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 30);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(sortField: "count", sortDirection: "asc");
            var ascendingMine = ascending.Rows
                .Where(r => r.Id == lowId || r.Id == midId || r.Id == highId).ToList();
            ascendingMine.Select(r => r.Count).Should().Equal(10, 20, 30);

            var descending = await service.ListAsync(sortField: "count", sortDirection: "desc");
            var descendingMine = descending.Rows
                .Where(r => r.Id == lowId || r.Id == midId || r.Id == highId).ToList();
            descendingMine.Select(r => r.Count).Should().Equal(30, 20, 10);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();

            var highId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 20);
            var lowId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(sortField: "count", sortDirection: ascendingKeyword);

            var mine = result.Rows.Where(r => r.Id == highId || r.Id == lowId).ToList();
            mine.Select(r => r.Count).Should().Equal(10, 20);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var lowId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 10);
            var highId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, 20);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(sortField: "count", sortDirection: "banana");

            var mine = result.Rows.Where(r => r.Id == lowId || r.Id == highId).ToList();
            mine.Select(r => r.Count).Should().Equal(20, 10);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var firstId = await CreateCounterAsync(dbContext, OtlpSignal.Traces,
                createdDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateCounterAsync(dbContext, OtlpSignal.Traces, createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var from = DateTime.UtcNow.AddMinutes(-1);
            for (var i = 0; i < 5; i++)
            {
                await CreateCounterAsync(dbContext, OtlpSignal.Traces);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, from);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().BeGreaterOrEqualTo(5);
        }

        [Fact]
        public async Task ListStatisticsIncludesEveryContinuousMeasuredColumnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await CreateCounterAsync(dbContext, OtlpSignal.Traces);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "count", "successCount", "failureCount", "itemCount", "droppedCount", "totalMicroseconds",
                "minMicroseconds", "maxMicroseconds");
        }
    }
}