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
using Jube.Service.EntityAnalysisModelProcessingCounter;
using Jube.Service.Exceptions.EntityAnalysisModelProcessingCounter;
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

namespace Jube.Test.Service.EntityAnalysisModelProcessingCounter
{
    using EntityAnalysisModelProcessingCounterService =
        EntityAnalysisModelProcessingCounterService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelProcessingCounterServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];

        private readonly List<int> createdRowIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdRowIds)
            {
                await dbContext.EntityAnalysisModelProcessingCounter.Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelProcessingCounterService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelProcessingCounterService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<(int Id, Guid Guid)> CreateModelAsync(DbContext dbContext, string createdUser,
            string? namePrefix = null)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{namePrefix ?? DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return (saved.Id, saved.Guid);
        }

        private async Task CreateRowAsync(DbContext dbContext, Guid entityAnalysisModelGuid, int modelInvoke = 0,
            double? responseElevationSum = null, double? activationWatcher = null,
            long? minResponseTimeMicroseconds = null, long? maxResponseTimeMicroseconds = null,
            int? archiveWalPendingCount = null, DateTime? createdDate = null, string? instance = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelProcessingCounter
                {
                    EntityAnalysisModelGuid = entityAnalysisModelGuid,
                    ModelInvoke = modelInvoke,
                    ResponseElevationSum = responseElevationSum,
                    ActivationWatcher = activationWatcher,
                    MinResponseTimeMicroseconds = minResponseTimeMicroseconds,
                    MaxResponseTimeMicroseconds = maxResponseTimeMicroseconds,
                    ArchiveWalPendingCount = archiveWalPendingCount,
                    CreatedDate = createdDate ?? DateTime.UtcNow,
                    Instance = instance ?? $"{DatabaseFixture.Prefix}Instance"
                }).ConfigureAwait(false);

            createdRowIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsModelNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateRowAsync(dbContext, modelGuid, 1, createdDate: DateTime.UtcNow.AddMinutes(-30));
            await CreateRowAsync(dbContext, modelGuid, 2, createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Should().HaveCount(2);
            mine[0].ModelInvoke.Should().Be(2);
            mine[1].ModelInvoke.Should().Be(1);
            mine[0].Name.Should().NotBeNullOrEmpty();
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

            await CreateRowAsync(dbContext, modelGuid, 1);
            await CreateRowAsync(dbContext, modelGuid, 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateRowAsync(dbContext, modelGuid, 1);
            await CreateRowAsync(dbContext, modelGuid, 2);

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
        public async Task ListIncludesEveryTenantForLandlordUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            await CreateRowAsync(dbContext, modelGuid, 1);

            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var landlordResult = await landlordService.ListAsync();

            landlordResult.Rows.Should().Contain(r => r.EntityAnalysisModelGuid == modelGuid);
        }

        [Fact]
        public async Task ListFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateRowAsync(dbContext, modelGuid, 1, createdDate: oldDate);
            await CreateRowAsync(dbContext, modelGuid, 2, createdDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Should().ContainSingle();
            mine[0].ModelInvoke.Should().Be(2);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateRowAsync(dbContext, modelGuid, 1, createdDate: twoHoursAgo);
            await CreateRowAsync(dbContext, modelGuid, 2, createdDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].ModelInvoke.Should().Be(2);
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstModelNameCaseInsensitivelyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchToken = $"Findme{Guid.NewGuid():N}";
            var (_, matchingGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser, searchToken);
            var (_, otherGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            await CreateRowAsync(dbContext, matchingGuid, 1);
            await CreateRowAsync(dbContext, otherGuid, 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: searchToken.ToLower());

            var mine = result.Rows
                .Where(r => r.EntityAnalysisModelGuid == matchingGuid || r.EntityAnalysisModelGuid == otherGuid)
                .ToList();
            mine.Should().ContainSingle();
            mine[0].EntityAnalysisModelGuid.Should().Be(matchingGuid);
        }

        [Fact]
        public async Task ListSortsByModelInvokeAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateRowAsync(dbContext, modelGuid, 30);
            await CreateRowAsync(dbContext, modelGuid, 10);
            await CreateRowAsync(dbContext, modelGuid, 20);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(sortField: "modelInvoke", sortDirection: "asc");
            var ascendingMine = ascending.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            ascendingMine.Select(r => r.ModelInvoke).Should().Equal(10, 20, 30);

            var descending = await service.ListAsync(sortField: "modelInvoke", sortDirection: "desc");
            var descendingMine = descending.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            descendingMine.Select(r => r.ModelInvoke).Should().Equal(30, 20, 10);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateRowAsync(dbContext, modelGuid, 20);
            await CreateRowAsync(dbContext, modelGuid, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(sortField: "modelInvoke", sortDirection: ascendingKeyword);

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Select(r => r.ModelInvoke).Should().Equal(10, 20);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateRowAsync(dbContext, modelGuid, 10);
            await CreateRowAsync(dbContext, modelGuid, 20);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(sortField: "modelInvoke", sortDirection: "banana");

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Select(r => r.ModelInvoke).Should().Equal(20, 10);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateRowAsync(dbContext, modelGuid, 1, createdDate: DateTime.UtcNow.AddMinutes(-30));
            await CreateRowAsync(dbContext, modelGuid, 2, createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine[0].ModelInvoke.Should().Be(2);
            mine[1].ModelInvoke.Should().Be(1);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var modelName = await dbContext.EntityAnalysisModel.Where(m => m.Guid == modelGuid)
                .Select(m => m.Name).SingleAsync();

            for (var i = 0; i < 5; i++)
            {
                await CreateRowAsync(dbContext, modelGuid, i);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, DateTime.UtcNow.AddMinutes(-1), search: modelName);

            var mine = result.Rows.Where(r => r.EntityAnalysisModelGuid == modelGuid).ToList();
            mine.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListProjectsMinAndMaxResponseTimeMicrosecondsAndArchiveWalPendingCountAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            await CreateRowAsync(dbContext, modelGuid, minResponseTimeMicroseconds: 1200,
                maxResponseTimeMicroseconds: 9800, archiveWalPendingCount: 42);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Single(r => r.EntityAnalysisModelGuid == modelGuid);
            mine.MinResponseTimeMicroseconds.Should().Be(1200);
            mine.MaxResponseTimeMicroseconds.Should().Be(9800);
            mine.ArchiveWalPendingCount.Should().Be(42);
        }

        [Fact]
        public async Task ListIdIsAlwaysZeroAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            await CreateRowAsync(dbContext, modelGuid, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Single(r => r.EntityAnalysisModelGuid == modelGuid);
            mine.Id.Should().Be(0,
                "the legacy read path never selected a row identifier -- preserved as-is");
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForResponseElevationSumAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            foreach (var value in new double[] { 1, 2, 3, 4, 5 })
            {
                await CreateRowAsync(dbContext, modelGuid, responseElevationSum: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: DateTime.UtcNow.AddMinutes(-1));

            var stats = result.Statistics.Columns["responseElevationSum"];
            stats.Min.Should().Be(1);
            stats.Max.Should().Be(5);
            stats.Mean.Should().Be(3);
            stats.Median.Should().Be(3);
            stats.StandardDeviation.Should().BeApproximately(1.5811, 0.001);
            stats.Histogram.Sum(b => b.Frequency).Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsWithSingleValueHasZeroStandardDeviationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var modelName = await dbContext.EntityAnalysisModel.Where(m => m.Guid == modelGuid)
                .Select(m => m.Name).SingleAsync();
            await CreateRowAsync(dbContext, modelGuid, activationWatcher: 7);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: DateTime.UtcNow.AddMinutes(-1), search: modelName);

            var stats = result.Statistics.Columns["activationWatcher"];
            stats.Min.Should().Be(7);
            stats.Max.Should().Be(7);
            stats.StandardDeviation.Should().Be(0, "a single-point sample has no spread, and must not be NaN");
        }

        [Fact]
        public async Task ListStatisticsOverZeroMatchingRowsDoesNotThrowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var result =
                await service.ListAsync(from: DateTime.UtcNow.AddSeconds(1), to: DateTime.UtcNow.AddSeconds(2));

            result.Statistics.Columns["responseElevationSum"].Min.Should().Be(0);
            result.Statistics.Columns["responseElevationSum"].Histogram.Should().BeEmpty();
        }

        [Fact]
        public async Task ListStatisticsIncludesEveryContinuousMeasuredColumnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            await CreateRowAsync(dbContext, modelGuid, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "modelInvoke", "gatewayMatch", "responseElevation", "responseElevationSum",
                "activationWatcher", "responseElevationLimit", "modelTotalResponseTime",
                "minResponseTimeMicroseconds", "maxResponseTimeMicroseconds", "archiveWalPendingCount");
        }
    }
}