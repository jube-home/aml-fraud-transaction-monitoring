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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.EntityAnalysisModelStagePerformanceCounter;
using Jube.Service.Exceptions.EntityAnalysisModelStagePerformanceCounter;
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

namespace Jube.Test.Service.EntityAnalysisModelStagePerformanceCounter
{
    using EntityAnalysisModelStagePerformanceCounterService =
        EntityAnalysisModelStagePerformanceCounterService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelStagePerformanceCounterServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdCounterIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdCounterIds)
            {
                await dbContext.EntityAnalysisModelStagePerformanceCounter.Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelStagePerformanceCounterService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelStagePerformanceCounterService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<(int Id, Guid Guid)> CreateModelAsync(DbContext dbContext, string createdUser)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return (saved.Id, saved.Guid);
        }

        private async Task CreateCounterAsync(DbContext dbContext, Guid entityAnalysisModelGuid,
            InvokeStage stageId, long totalMicroseconds, int invokeCount, DateTime? createdDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelStagePerformanceCounter
                {
                    EntityAnalysisModelGuid = entityAnalysisModelGuid,
                    StageId = (int)stageId,
                    TotalMicroseconds = totalMicroseconds,
                    MinMicroseconds = totalMicroseconds,
                    MaxMicroseconds = totalMicroseconds,
                    InvokeCount = invokeCount,
                    CreatedDate = createdDate ?? DateTime.UtcNow,
                    Instance = $"{DatabaseFixture.Prefix}Instance"
                }).ConfigureAwait(false);

            createdCounterIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsModelNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 10);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 2000, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Should().HaveCount(2);
            mine[0].StageName.Should().Be("Gateway");
            mine[1].StageName.Should().Be("Parse");
            mine[0].EntityAnalysisModelName.Should().NotBeNullOrEmpty();
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
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 10);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 2000, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 10);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 2000, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(samplePercentage: 100);

            result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsForbiddenForNonLandlordUsersOfAnyTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var user in new[] { fx.Seed.UserWithPermission, fx.Seed.UserTenantB })
            {
                var service = await BuildServiceAsync(dbContext, user);
                await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
            }
        }

        [Fact]
        public async Task ListFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 10, oldDate);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 2000, 10, newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Should().ContainSingle();
            mine[0].StageName.Should().Be("Gateway");
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 10, twoHoursAgo);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 2000, 10, justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].StageName.Should().Be("Gateway");
        }

        [Fact]
        public async Task ListFiltersByStageIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 10);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 2000, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(stageId: (int)InvokeStage.Parse);

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Should().ContainSingle();
            mine[0].StageName.Should().Be("Parse");
        }

        [Fact]
        public async Task ListIncludesEveryTenantForLandlordUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 10);

            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var landlordResult = await landlordService.ListAsync();

            landlordResult.Rows.Should().Contain(r => r.EntityAnalysisModelGuid == modelGuid);
        }

        [Fact]
        public async Task ListSortsByTotalMicrosecondsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 3000, 1);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 1000, 1);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Activation, 2000, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(sortField: "totalMicroseconds", sortDirection: "asc");
            var ascendingMine = ascending.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            ascendingMine.Select(r => r.TotalMicroseconds).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(sortField: "totalMicroseconds", sortDirection: "desc");
            var descendingMine = descending.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            descendingMine.Select(r => r.TotalMicroseconds).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 2000, 1);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 1000, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result =
                await service.ListAsync(sortField: "totalMicroseconds", sortDirection: ascendingKeyword);

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Select(r => r.TotalMicroseconds).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 1);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 2000, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(sortField: "totalMicroseconds", sortDirection: "banana");

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Select(r => r.TotalMicroseconds).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 1);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Gateway, 2000, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine[0].StageName.Should().Be("Gateway");
            mine[1].StageName.Should().Be("Parse");
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            for (var i = 0; i < 5; i++)
            {
                await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000 + i, 1);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, stageId: (int)InvokeStage.Parse,
                from: DateTime.UtcNow.AddMinutes(-1));

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForTotalMicrosecondsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, value, 1);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: DateTime.UtcNow.AddMinutes(-1));

            var stats = result.Statistics.Columns["totalMicroseconds"];
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
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            await CreateCounterAsync(dbContext, modelGuid, InvokeStage.Parse, 1000, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "totalMicroseconds", "minMicroseconds", "maxMicroseconds", "invokeCount");
        }
    }
}