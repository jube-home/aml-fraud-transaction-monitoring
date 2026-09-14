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
using Jube.Service.CaseCreationStagePerformanceCounter;
using Jube.Service.Exceptions.CaseCreationStagePerformanceCounter;
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

namespace Jube.Test.Service.CaseCreationStagePerformanceCounter
{
    using CaseCreationStagePerformanceCounterService =
        CaseCreationStagePerformanceCounterService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseCreationStagePerformanceCounterServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.CaseCreationStagePerformanceCounter.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<CaseCreationStagePerformanceCounterService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseCreationStagePerformanceCounterService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateCounterAsync(DbContext dbContext, CaseCreationStage stage,
            long totalMicroseconds = 1000, int invokeCount = 1, DateTime? createdDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseCreationStagePerformanceCounter
            {
                StageId = (int)stage,
                TotalMicroseconds = totalMicroseconds,
                MinMicroseconds = totalMicroseconds,
                MaxMicroseconds = totalMicroseconds,
                InvokeCount = invokeCount,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
            return id;
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsStageNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var firstId = await CreateCounterAsync(dbContext, CaseCreationStage.ExistingCasePriorityLookup,
                createdDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateCounterAsync(dbContext, CaseCreationStage.HttpEndpoint,
                createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[0].StageName.Should().Be("HttpEndpoint");
            mine[1].Id.Should().Be(firstId);
            mine[1].StageName.Should().Be("ExistingCasePriorityLookup");
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
            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            var oldId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification, createdDate: oldDate);
            var newId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification, createdDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Id == oldId || r.Id == newId).ToList();
            mine.Should().ContainSingle();
            mine[0].Id.Should().Be(newId);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            var oldId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification,
                createdDate: twoHoursAgo);
            var newId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification, createdDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Id == oldId || r.Id == newId).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Id.Should().Be(newId);
        }

        [Fact]
        public async Task ListFiltersByStageIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var priorityId = await CreateCounterAsync(dbContext, CaseCreationStage.ExistingCasePriorityLookup);
            var httpId = await CreateCounterAsync(dbContext, CaseCreationStage.HttpEndpoint);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(stageId: (int)CaseCreationStage.ExistingCasePriorityLookup);

            var mine = result.Rows.Where(r => r.Id == priorityId || r.Id == httpId).ToList();
            mine.Should().ContainSingle();
            mine[0].Id.Should().Be(priorityId);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await CreateCounterAsync(dbContext, CaseCreationStage.Notification);
            await CreateCounterAsync(dbContext, CaseCreationStage.Notification);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(stageId: (int)CaseCreationStage.Notification, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var firstId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification);
            var secondId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(stageId: (int)CaseCreationStage.Notification,
                samplePercentage: 100);

            result.Rows.Where(r => r.Id == firstId || r.Id == secondId).Should().HaveCount(2);
        }

        [Fact]
        public async Task ListSortsByTotalMicrosecondsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id1 = await CreateCounterAsync(dbContext, CaseCreationStage.Notification, 3000);
            var id2 = await CreateCounterAsync(dbContext, CaseCreationStage.Notification);
            var id3 = await CreateCounterAsync(dbContext, CaseCreationStage.Notification, 2000);
            var ids = new HashSet<int> { id1, id2, id3 };

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(stageId: (int)CaseCreationStage.Notification,
                sortField: "totalMicroseconds", sortDirection: "asc");
            ascending.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.TotalMicroseconds)
                .Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(stageId: (int)CaseCreationStage.Notification,
                sortField: "totalMicroseconds", sortDirection: "desc");
            descending.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.TotalMicroseconds)
                .Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var highId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification, 2000);
            var lowId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification);
            var ids = new HashSet<int> { highId, lowId };

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(stageId: (int)CaseCreationStage.Notification,
                sortField: "totalMicroseconds", sortDirection: ascendingKeyword);

            result.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.TotalMicroseconds).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var lowId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification);
            var highId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification, 2000);
            var ids = new HashSet<int> { lowId, highId };

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(stageId: (int)CaseCreationStage.Notification,
                sortField: "totalMicroseconds", sortDirection: "banana");

            result.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.TotalMicroseconds).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var firstId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification,
                createdDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateCounterAsync(dbContext, CaseCreationStage.Notification,
                createdDate: DateTime.UtcNow);

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
                await CreateCounterAsync(dbContext, CaseCreationStage.WorkflowStatusLookupAndPersist);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, stageId: (int)CaseCreationStage.WorkflowStatusLookupAndPersist,
                from: from);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().BeGreaterOrEqualTo(5);
        }

        [Fact]
        public async Task ListStatisticsIncludesEveryContinuousMeasuredColumnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await CreateCounterAsync(dbContext, CaseCreationStage.Notification);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "totalMicroseconds", "minMicroseconds", "maxMicroseconds", "invokeCount");
        }
    }
}