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
using Jube.Service.Exceptions.RedisConnectionEvent;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.RedisConnectionEvent;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Service.RedisConnectionEvent
{
    using RedisConnectionEventService = RedisConnectionEventService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RedisConnectionEventServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.RedisConnectionEvent.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<RedisConnectionEventService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return RedisConnectionEventService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, RedisConnectionEventType eventType,
            DateTime? occurredDate = null, string endPoint = "localhost:6379",
            ConnectionType connectionType = ConnectionType.Interactive, ConnectionFailureType? failureType = null,
            string? origin = null, string? message = null, string? exception = null, int? retryCount = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RedisConnectionEvent
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                EventTypeId = (int)eventType,
                EndPoint = endPoint,
                ConnectionTypeId = (int)connectionType,
                FailureTypeId = (int?)failureType,
                Origin = origin,
                Message = message,
                Exception = exception,
                RetryCount = retryCount,
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
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";

            var firstId = await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed,
                endPoint: endPoint,
                failureType: ConnectionFailureType.SocketClosed);
            var secondId = await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionRestored,
                endPoint: endPoint,
                failureType: ConnectionFailureType.SocketClosed);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.EndPoint == endPoint).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].EventTypeName.Should().Be("ConnectionRestored");
            mine[1].FailureTypeName.Should().Be("SocketClosed");
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
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed, oldDate, $"{endPoint}Old");
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed, newDate, $"{endPoint}New");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.EndPoint.StartsWith(endPoint)).ToList();
            mine.Should().ContainSingle();
            mine[0].EndPoint.Should().Be($"{endPoint}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueEndPoint = $"UniqueHost{Guid.NewGuid():N}:6379";

            await CreateEntryAsync(dbContext, RedisConnectionEventType.InternalError, endPoint: uniqueEndPoint,
                origin: "ReadFromPipe");
            await CreateEntryAsync(dbContext, RedisConnectionEventType.InternalError, endPoint: "OtherHost:6379",
                origin: "ReadFromPipe");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueEndPoint[..12].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.EndPoint == uniqueEndPoint || r.EndPoint == "OtherHost:6379").ToList();
            mine.Should().ContainSingle();
            mine[0].EndPoint.Should().Be(uniqueEndPoint);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed, endPoint: endPoint);
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionRestored, endPoint: endPoint);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: endPoint, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed, endPoint: endPoint);
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionRestored, endPoint: endPoint);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: endPoint, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed, endPoint: endPoint);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.EndPoint == endPoint);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed, twoHoursAgo,
                $"{endPoint}Old");
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed, justNow,
                $"{endPoint}New");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: endPoint);

            var mine = result.Rows.Where(r => r.EndPoint.StartsWith(endPoint)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].EndPoint.Should().Be($"{endPoint}New");
        }

        [Fact]
        public async Task ListSortsByRetryCountAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                retryCount: 3);
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                retryCount: 1);
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                retryCount: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: endPoint, sortField: "retryCount",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.RetryCount).Should().Equal(1, 2, 3);

            var descending = await service.ListAsync(search: endPoint, sortField: "retryCount",
                sortDirection: "desc");
            descending.Rows.Select(r => r.RetryCount).Should().Equal(3, 2, 1);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                retryCount: 2);
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                retryCount: 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: endPoint, sortField: "retryCount",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.RetryCount).Should().Equal(1, 2);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                retryCount: 1);
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                retryCount: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: endPoint, sortField: "retryCount",
                sortDirection: "banana");

            result.Rows.Select(r => r.RetryCount).Should().Equal(2, 1);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            var firstId = await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed,
                DateTime.UtcNow.AddMinutes(-30), endPoint);
            var secondId = await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed,
                DateTime.UtcNow, endPoint);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: endPoint, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, RedisConnectionEventType.ConnectionFailed, endPoint: endPoint);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, search: endPoint);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForRetryCountAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";

            foreach (var value in new[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                    retryCount: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: endPoint);

            var stats = result.Statistics.Columns["retryCount"];
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
            var endPoint = $"{DatabaseFixture.Prefix}host{Guid.NewGuid():N}:6379";
            await CreateEntryAsync(dbContext, RedisConnectionEventType.ReconnectRetry, endPoint: endPoint,
                retryCount: 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: endPoint);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "retryCount", "backoffMilliseconds", "transactionsImpacted");
        }
    }
}