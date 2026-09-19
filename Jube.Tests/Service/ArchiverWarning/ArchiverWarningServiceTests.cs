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
using Jube.Service.ArchiverWarning;
using Jube.Service.Exceptions.ArchiverWarning;
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

namespace Jube.Test.Service.ArchiverWarning
{
    using ArchiverWarningService = ArchiverWarningService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ArchiverWarningServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.ArchiverWarning.Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ArchiverWarningService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ArchiverWarningService.CreateAsync(
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

        private async Task<int> CreateWarningAsync(DbContext dbContext, Guid entityAnalysisModelGuid,
            string entityAnalysisModelName, ArchiverStage stage = ArchiverStage.BuildArchiveJson,
            long durationMicroseconds = 1000, DateTime? occurredDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ArchiverWarning
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                EntityAnalysisModelGuid = entityAnalysisModelGuid,
                EntityAnalysisModelName = entityAnalysisModelName,
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                StageId = (int)stage,
                DurationMicroseconds = durationMicroseconds,
                CreatedDate = DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
            return id;
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsModelNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var modelName = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}";
            var firstId = await CreateWarningAsync(dbContext, modelGuid, modelName,
                occurredDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateWarningAsync(dbContext, modelGuid, modelName,
                ArchiverStage.RdbmsArchiveWrite, 2000, DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[0].StageName.Should().Be("RdbmsArchiveWrite");
            mine[0].EntityAnalysisModelName.Should().Be(modelName);
            mine[1].Id.Should().Be(firstId);
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
            var id = await CreateWarningAsync(dbContext, modelGuid, "Model");

            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var landlordResult = await landlordService.ListAsync();

            landlordResult.Rows.Should().Contain(r => r.Id == id);
        }

        [Fact]
        public async Task ListFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            var oldId = await CreateWarningAsync(dbContext, modelGuid, "Model", occurredDate: oldDate);
            var newId = await CreateWarningAsync(dbContext, modelGuid, "Model", occurredDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Id == oldId || r.Id == newId).ToList();
            mine.Should().ContainSingle();
            mine[0].Id.Should().Be(newId);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            var oldId = await CreateWarningAsync(dbContext, modelGuid, "Model", occurredDate: twoHoursAgo);
            var newId = await CreateWarningAsync(dbContext, modelGuid, "Model", occurredDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Id == oldId || r.Id == newId).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Id.Should().Be(newId);
        }

        [Fact]
        public async Task ListFiltersByStageIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var buildId = await CreateWarningAsync(dbContext, modelGuid, "Model");
            var rdbmsId = await CreateWarningAsync(dbContext, modelGuid, "Model",
                ArchiverStage.RdbmsArchiveWrite);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(stageId: (int)ArchiverStage.BuildArchiveJson);

            var mine = result.Rows.Where(r => r.Id == buildId || r.Id == rdbmsId).ToList();
            mine.Should().ContainSingle();
            mine[0].Id.Should().Be(buildId);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            await CreateWarningAsync(dbContext, modelGuid, "Model");
            await CreateWarningAsync(dbContext, modelGuid, "Model");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var id1 = await CreateWarningAsync(dbContext, modelGuid, "Model");
            var id2 = await CreateWarningAsync(dbContext, modelGuid, "Model");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(samplePercentage: 100);

            result.Rows.Where(r => r.Id == id1 || r.Id == id2).Should().HaveCount(2);
        }

        [Fact]
        public async Task ListSortsByDurationMicrosecondsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var id1 = await CreateWarningAsync(dbContext, modelGuid, "Model", durationMicroseconds: 3000);
            var id2 = await CreateWarningAsync(dbContext, modelGuid, "Model", durationMicroseconds: 1000);
            var id3 = await CreateWarningAsync(dbContext, modelGuid, "Model", durationMicroseconds: 2000);
            var ids = new HashSet<int> { id1, id2, id3 };

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending =
                await service.ListAsync(sortField: "durationMicroseconds", sortDirection: "asc");
            ascending.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.DurationMicroseconds)
                .Should().Equal(1000, 2000, 3000);

            var descending =
                await service.ListAsync(sortField: "durationMicroseconds", sortDirection: "desc");
            descending.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.DurationMicroseconds)
                .Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var highId = await CreateWarningAsync(dbContext, modelGuid, "Model", durationMicroseconds: 2000);
            var lowId = await CreateWarningAsync(dbContext, modelGuid, "Model", durationMicroseconds: 1000);
            var ids = new HashSet<int> { highId, lowId };

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(sortField: "durationMicroseconds",
                sortDirection: ascendingKeyword);

            result.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.DurationMicroseconds)
                .Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var lowId = await CreateWarningAsync(dbContext, modelGuid, "Model", durationMicroseconds: 1000);
            var highId = await CreateWarningAsync(dbContext, modelGuid, "Model", durationMicroseconds: 2000);
            var ids = new HashSet<int> { lowId, highId };

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(sortField: "durationMicroseconds", sortDirection: "banana");

            result.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.DurationMicroseconds)
                .Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            var firstId = await CreateWarningAsync(dbContext, modelGuid, "Model",
                occurredDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateWarningAsync(dbContext, modelGuid, "Model",
                occurredDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);
            for (var i = 0; i < 5; i++)
            {
                await CreateWarningAsync(dbContext, modelGuid, "Model");
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, DateTime.UtcNow.AddMinutes(-1));

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForDurationMicrosecondsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext, fx.Seed.LandlordUser);

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateWarningAsync(dbContext, modelGuid, "Model", durationMicroseconds: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: DateTime.UtcNow.AddMinutes(-1));

            var stats = result.Statistics.Columns["durationMicroseconds"];
            stats.Min.Should().Be(1);
            stats.Max.Should().Be(5);
            stats.Mean.Should().Be(3);
            stats.Median.Should().Be(3);
            stats.StandardDeviation.Should().BeApproximately(1.5811, 0.001);
            stats.Histogram.Sum(b => b.Frequency).Should().Be(5);
        }
    }
}