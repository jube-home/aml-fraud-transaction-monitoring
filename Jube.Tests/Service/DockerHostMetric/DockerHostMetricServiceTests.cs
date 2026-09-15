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
using Jube.Service.DockerHostMetric;
using Jube.Service.Exceptions.DockerHostMetric;
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

namespace Jube.Test.Service.DockerHostMetric
{
    using DockerHostMetricService = DockerHostMetricService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DockerHostMetricServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.DockerHostMetric.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<DockerHostMetricService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return DockerHostMetricService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string instance,
            string operatingSystem = "Fedora Linux", DateTime? createdDate = null, long memTotalBytes = 67182252032)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.DockerHostMetric
            {
                ContainersTotal = 7,
                ContainersRunning = 2,
                ContainersPaused = 0,
                ContainersStopped = 5,
                ImagesCount = 6,
                NCpu = 24,
                MemTotalBytes = memTotalBytes,
                DockerVersion = "29.7.2",
                ApiVersion = "1.55",
                KernelVersion = "7.1.12-200.fc44.x86_64",
                OperatingSystem = operatingSystem,
                OsType = "linux",
                Architecture = "x86_64",
                LayersSizeBytes = 4114952039,
                ImagesSizeBytes = 4505306357,
                ReclaimableImagesBytes = 299305895,
                ContainersDiskBytes = 2617344,
                VolumesSizeBytes = 165585186,
                BuildCacheSizeBytes = 19760571507,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                Instance = instance
            }).ConfigureAwait(false);

            createdIds.Add(id);
            return id;
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            var firstId = await CreateEntryAsync(dbContext, instance);
            var secondId = await CreateEntryAsync(dbContext, instance);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Instance == instance).ToList();
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
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{instance}Old", createdDate: oldDate);
            await CreateEntryAsync(dbContext, $"{instance}New", createdDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Instance.StartsWith(instance)).ToList();
            mine.Should().ContainSingle();
            mine[0].Instance.Should().Be($"{instance}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueInstance = $"UniqueInstance{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, uniqueInstance);
            await CreateEntryAsync(dbContext, "OtherInstance");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueInstance[..14].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Instance == uniqueInstance || r.Instance == "OtherInstance").ToList();
            mine.Should().ContainSingle();
            mine[0].Instance.Should().Be(uniqueInstance);
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstOperatingSystemAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueOperatingSystem = $"UniqueOs{Guid.NewGuid():N}";
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, instance, uniqueOperatingSystem);
            await CreateEntryAsync(dbContext, $"{instance}Other", "Other OS");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueOperatingSystem);

            var mine = result.Rows.Where(r => r.Instance == instance || r.Instance == $"{instance}Other").ToList();
            mine.Should().ContainSingle();
            mine[0].OperatingSystem.Should().Be(uniqueOperatingSystem);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance);
            await CreateEntryAsync(dbContext, instance);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance);
            await CreateEntryAsync(dbContext, instance);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance);

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
            await CreateEntryAsync(dbContext, $"{instance}Old", createdDate: twoHoursAgo);
            await CreateEntryAsync(dbContext, $"{instance}New", createdDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance);

            var mine = result.Rows.Where(r => r.Instance.StartsWith(instance)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Instance.Should().Be($"{instance}New");
        }

        [Fact]
        public async Task ListSortsByMemTotalBytesAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance, memTotalBytes: 3000);
            await CreateEntryAsync(dbContext, instance, memTotalBytes: 1000);
            await CreateEntryAsync(dbContext, instance, memTotalBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: instance, sortField: "memTotalBytes",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.MemTotalBytes).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: instance, sortField: "memTotalBytes",
                sortDirection: "desc");
            descending.Rows.Select(r => r.MemTotalBytes).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance, memTotalBytes: 2000);
            await CreateEntryAsync(dbContext, instance, memTotalBytes: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, sortField: "memTotalBytes",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.MemTotalBytes).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance, memTotalBytes: 1000);
            await CreateEntryAsync(dbContext, instance, memTotalBytes: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, sortField: "memTotalBytes",
                sortDirection: "banana");

            result.Rows.Select(r => r.MemTotalBytes).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            var firstId = await CreateEntryAsync(dbContext, instance, createdDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateEntryAsync(dbContext, instance, createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, instance);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, search: instance);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForMemTotalBytesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, instance, memTotalBytes: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance);

            var stats = result.Statistics.Columns["memTotalBytes"];
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
            await CreateEntryAsync(dbContext, instance);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "containersTotal", "containersRunning", "containersPaused", "containersStopped", "imagesCount",
                "nCpu", "memTotalBytes", "layersSizeBytes", "imagesSizeBytes", "reclaimableImagesBytes",
                "containersDiskBytes", "volumesSizeBytes", "buildCacheSizeBytes");
        }
    }
}