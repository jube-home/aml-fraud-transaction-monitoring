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
using Jube.Service.DockerContainerMetric;
using Jube.Service.Exceptions.DockerContainerMetric;
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

namespace Jube.Test.Service.DockerContainerMetric
{
    using DockerContainerMetricService = DockerContainerMetricService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DockerContainerMetricServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.DockerContainerMetric.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<DockerContainerMetricService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return DockerContainerMetricService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string name, string image = "redis:latest",
            string state = "running", string status = "Up 3 hours", DateTime? occurredDate = null,
            long memoryUsageBytes = 1024)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.DockerContainerMetric
            {
                ContainerId = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}",
                Name = name,
                Image = image,
                State = state,
                Status = status,
                RestartCount = 0,
                OomKilled = false,
                ExitCode = 0,
                HealthStatus = "none",
                CpuUsagePercent = 1.5,
                OnlineCpus = 4,
                MemoryUsageBytes = memoryUsageBytes,
                MemoryLimitBytes = 2048,
                MemoryPercent = 50,
                NetworkRxBytes = 100,
                NetworkTxBytes = 200,
                BlockReadBytes = 300,
                BlockWriteBytes = 400,
                PidsCurrent = 5,
                PidsLimit = 100,
                OccurredDate = occurredDate ?? DateTime.UtcNow,
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
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";

            var firstId = await CreateEntryAsync(dbContext, name);
            var secondId = await CreateEntryAsync(dbContext, name);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Name == name).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
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
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{name}Old", occurredDate: oldDate);
            await CreateEntryAsync(dbContext, $"{name}New", occurredDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Name.StartsWith(name)).ToList();
            mine.Should().ContainSingle();
            mine[0].Name.Should().Be($"{name}New");
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{name}Old", occurredDate: twoHoursAgo);
            await CreateEntryAsync(dbContext, $"{name}New", occurredDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name);

            var mine = result.Rows.Where(r => r.Name.StartsWith(name)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Name.Should().Be($"{name}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueName = $"UniqueContainer{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, uniqueName);
            await CreateEntryAsync(dbContext, "OtherContainer");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueName[..14].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Name == uniqueName || r.Name == "OtherContainer").ToList();
            mine.Should().ContainSingle();
            mine[0].Name.Should().Be(uniqueName);
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstImageStateAndStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueImage = $"uniqueimage{Guid.NewGuid():N}";
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, name, uniqueImage);
            await CreateEntryAsync(dbContext, $"{name}Other", "otherimage:latest");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueImage);

            var mine = result.Rows.Where(r => r.Name == name || r.Name == $"{name}Other").ToList();
            mine.Should().ContainSingle();
            mine[0].Image.Should().Be(uniqueImage);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name);
            await CreateEntryAsync(dbContext, name);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name);
            await CreateEntryAsync(dbContext, name);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Name == name);
        }

        [Fact]
        public async Task ListSortsByMemoryUsageBytesAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, memoryUsageBytes: 3000);
            await CreateEntryAsync(dbContext, name, memoryUsageBytes: 1000);
            await CreateEntryAsync(dbContext, name, memoryUsageBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: name, sortField: "memoryUsageBytes",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.MemoryUsageBytes).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: name, sortField: "memoryUsageBytes",
                sortDirection: "desc");
            descending.Rows.Select(r => r.MemoryUsageBytes).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, memoryUsageBytes: 2000);
            await CreateEntryAsync(dbContext, name, memoryUsageBytes: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, sortField: "memoryUsageBytes",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.MemoryUsageBytes).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name, memoryUsageBytes: 1000);
            await CreateEntryAsync(dbContext, name, memoryUsageBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, sortField: "memoryUsageBytes",
                sortDirection: "banana");

            result.Rows.Select(r => r.MemoryUsageBytes).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            var firstId = await CreateEntryAsync(dbContext, name, occurredDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateEntryAsync(dbContext, name, occurredDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, name);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, search: name);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForMemoryUsageBytesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, name, memoryUsageBytes: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name);

            var stats = result.Statistics.Columns["memoryUsageBytes"];
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
            var name = $"{DatabaseFixture.Prefix}Container{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, name);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: name);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "restartCount", "cpuUsagePercent", "onlineCpus", "memoryUsageBytes", "memoryLimitBytes",
                "memoryPercent", "networkRxBytes", "networkTxBytes", "blockReadBytes", "blockWriteBytes",
                "pidsCurrent", "pidsLimit");
        }
    }
}