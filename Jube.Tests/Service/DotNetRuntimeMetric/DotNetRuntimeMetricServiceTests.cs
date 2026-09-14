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
using Jube.Service.DotNetRuntimeMetric;
using Jube.Service.Exceptions.DotNetRuntimeMetric;
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

namespace Jube.Test.Service.DotNetRuntimeMetric
{
    using DotNetRuntimeMetricService = DotNetRuntimeMetricService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DotNetRuntimeMetricServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.DotNetRuntimeMetric.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<DotNetRuntimeMetricService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return DotNetRuntimeMetricService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateSampleAsync(DbContext dbContext, string instance, int processorCount,
            DateTime? createdDate = null, long? heapSizeBytes = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.DotNetRuntimeMetric
            {
                CreatedDate = createdDate ?? DateTime.UtcNow,
                Instance = instance,
                ProcessorCount = processorCount,
                HeapSizeBytes = heapSizeBytes
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            await CreateSampleAsync(dbContext, instance, 4);
            await CreateSampleAsync(dbContext, instance, 8);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Instance == instance).ToList();
            mine.Should().HaveCount(2);
            mine[0].ProcessorCount.Should().Be(8);
            mine[1].ProcessorCount.Should().Be(4);
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
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, instance, 4, oldDate);
            await CreateSampleAsync(dbContext, instance, 8, newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Instance == instance).ToList();
            mine.Should().ContainSingle();
            mine[0].ProcessorCount.Should().Be(8);
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueInstance = $"{DatabaseFixture.Prefix}UniqueInstance{Guid.NewGuid():N}";
            var otherInstance = $"{DatabaseFixture.Prefix}OtherInstance{Guid.NewGuid():N}";

            await CreateSampleAsync(dbContext, uniqueInstance, 4);
            await CreateSampleAsync(dbContext, otherInstance, 8);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueInstance[..20].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Instance == uniqueInstance || r.Instance == otherInstance).ToList();
            mine.Should().ContainSingle();
            mine[0].Instance.Should().Be(uniqueInstance);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 4);
            await CreateSampleAsync(dbContext, instance, 8);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 4);
            await CreateSampleAsync(dbContext, instance, 8);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 16);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Instance == instance);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, $"{instance}Old", 4, twoHoursAgo);
            await CreateSampleAsync(dbContext, $"{instance}New", 8, justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance);

            var mine = result.Rows.Where(r => r.Instance.StartsWith(instance)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Instance.Should().Be($"{instance}New");
        }

        [Fact]
        public async Task ListSortsByHeapSizeBytesAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: 3000);
            await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: 1000);
            await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: instance, sortField: "heapSizeBytes",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.HeapSizeBytes).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: instance, sortField: "heapSizeBytes",
                sortDirection: "desc");
            descending.Rows.Select(r => r.HeapSizeBytes).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: 2000);
            await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, sortField: "heapSizeBytes",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.HeapSizeBytes).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: 1000);
            await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, sortField: "heapSizeBytes",
                sortDirection: "banana");

            result.Rows.Select(r => r.HeapSizeBytes).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 4, DateTime.UtcNow.AddMinutes(-30));
            await CreateSampleAsync(dbContext, instance, 8, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Instance == instance).ToList();
            mine[0].ProcessorCount.Should().Be(8);
            mine[1].ProcessorCount.Should().Be(4);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateSampleAsync(dbContext, instance, 4);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, search: instance);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForHeapSizeBytesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance);

            var stats = result.Statistics.Columns["heapSizeBytes"];
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
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, instance, 4, heapSizeBytes: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "processorCount", "gen0CollectionCount", "gen1CollectionCount", "gen2CollectionCount",
                "totalAllocatedBytes", "heapSizeBytes", "fragmentedBytes", "memoryLoadBytes",
                "highMemoryLoadThresholdBytes", "workingSetBytes", "privateMemoryBytes", "threadCount",
                "threadPoolWorkerThreadsAvailable", "threadPoolWorkerThreadsMax",
                "threadPoolCompletionPortThreadsAvailable", "threadPoolCompletionPortThreadsMax",
                "threadPoolQueueLength", "cpuTimeMicroseconds", "runtimeAvailableMemoryBytes",
                "runtimeCommittedMemoryBytes", "containerCpuLimitCores", "containerCpuUsageMicroseconds",
                "containerCpuThrottledPeriods", "containerCpuThrottledMicroseconds", "containerMemoryLimitBytes",
                "containerMemoryUsageBytes", "gcPauseTimeMicroseconds", "lockContentionCount");
        }
    }
}