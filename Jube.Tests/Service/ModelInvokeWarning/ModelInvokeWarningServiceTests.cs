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
using Jube.Service.Exceptions.ModelInvokeWarning;
using Jube.Service.ModelInvokeWarning;
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

namespace Jube.Test.Service.ModelInvokeWarning
{
    using ModelInvokeWarningService = ModelInvokeWarningService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ModelInvokeWarningServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.ModelInvokeWarning.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<ModelInvokeWarningService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ModelInvokeWarningService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateWarningAsync(DbContext dbContext, Guid entityAnalysisModelGuid,
            string message = "Slow trace point", long elapsedMicroseconds = 1000,
            long sinceLastEntryMicroseconds = 500, DateTime? occurredDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ModelInvokeWarning
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                EntityAnalysisModelGuid = entityAnalysisModelGuid,
                EntityAnalysisModelName = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}",
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                Message = message,
                ElapsedMicroseconds = elapsedMicroseconds,
                SinceLastEntryMicroseconds = sinceLastEntryMicroseconds,
                ThreadId = 1,
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
            var modelGuid = Guid.NewGuid();
            var firstId = await CreateWarningAsync(dbContext, modelGuid, "First", 1000, 500,
                DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateWarningAsync(dbContext, modelGuid, "Second", 2000, 900,
                DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid);

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[0].Message.Should().Be("Second");
            mine[0].ElapsedMicroseconds.Should().Be(2000);
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
            var modelGuid = Guid.NewGuid();
            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            var oldId = await CreateWarningAsync(dbContext, modelGuid, occurredDate: oldDate);
            var newId = await CreateWarningAsync(dbContext, modelGuid, occurredDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid, from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Id == oldId || r.Id == newId).ToList();
            mine.Should().ContainSingle();
            mine[0].Id.Should().Be(newId);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            var oldId = await CreateWarningAsync(dbContext, modelGuid, occurredDate: twoHoursAgo);
            var newId = await CreateWarningAsync(dbContext, modelGuid, occurredDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid);

            var mine = result.Rows.Where(r => r.Id == oldId || r.Id == newId).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Id.Should().Be(newId);
        }

        [Fact]
        public async Task ListFiltersByEntityAnalysisModelGuidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            var otherModelGuid = Guid.NewGuid();
            var matchingId = await CreateWarningAsync(dbContext, modelGuid);
            var otherId = await CreateWarningAsync(dbContext, otherModelGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid);

            var mine = result.Rows.Where(r => r.Id == matchingId || r.Id == otherId).ToList();
            mine.Should().ContainSingle();
            mine[0].Id.Should().Be(matchingId);
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstMessageCaseInsensitivelyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            var uniqueMessage = $"UniqueMessage{Guid.NewGuid():N}";
            var matchingId = await CreateWarningAsync(dbContext, modelGuid, uniqueMessage);
            var otherId = await CreateWarningAsync(dbContext, modelGuid, "Other message");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid,
                search: uniqueMessage.ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Id == matchingId || r.Id == otherId).ToList();
            mine.Should().ContainSingle();
            mine[0].Id.Should().Be(matchingId);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            await CreateWarningAsync(dbContext, modelGuid);
            await CreateWarningAsync(dbContext, modelGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            var id1 = await CreateWarningAsync(dbContext, modelGuid);
            var id2 = await CreateWarningAsync(dbContext, modelGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid, samplePercentage: 100);

            result.Rows.Where(r => r.Id == id1 || r.Id == id2).Should().HaveCount(2);
        }

        [Fact]
        public async Task ListSortsByElapsedMicrosecondsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            var id1 = await CreateWarningAsync(dbContext, modelGuid, elapsedMicroseconds: 3000);
            var id2 = await CreateWarningAsync(dbContext, modelGuid, elapsedMicroseconds: 1000);
            var id3 = await CreateWarningAsync(dbContext, modelGuid, elapsedMicroseconds: 2000);
            var ids = new HashSet<int> { id1, id2, id3 };

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(entityAnalysisModelGuid: modelGuid,
                sortField: "elapsedMicroseconds", sortDirection: "asc");
            ascending.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.ElapsedMicroseconds)
                .Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(entityAnalysisModelGuid: modelGuid,
                sortField: "elapsedMicroseconds", sortDirection: "desc");
            descending.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.ElapsedMicroseconds)
                .Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            var highId = await CreateWarningAsync(dbContext, modelGuid, elapsedMicroseconds: 2000);
            var lowId = await CreateWarningAsync(dbContext, modelGuid, elapsedMicroseconds: 1000);
            var ids = new HashSet<int> { highId, lowId };

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid,
                sortField: "elapsedMicroseconds", sortDirection: ascendingKeyword);

            result.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.ElapsedMicroseconds)
                .Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            var lowId = await CreateWarningAsync(dbContext, modelGuid, elapsedMicroseconds: 1000);
            var highId = await CreateWarningAsync(dbContext, modelGuid, elapsedMicroseconds: 2000);
            var ids = new HashSet<int> { lowId, highId };

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid,
                sortField: "elapsedMicroseconds", sortDirection: "banana");

            result.Rows.Where(r => ids.Contains(r.Id)).Select(r => r.ElapsedMicroseconds)
                .Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            var firstId = await CreateWarningAsync(dbContext, modelGuid,
                occurredDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateWarningAsync(dbContext, modelGuid, occurredDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.Id == firstId || r.Id == secondId).ToList();
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();
            for (var i = 0; i < 5; i++)
            {
                await CreateWarningAsync(dbContext, modelGuid);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(2, entityAnalysisModelGuid: modelGuid);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForElapsedMicrosecondsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelGuid = Guid.NewGuid();

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateWarningAsync(dbContext, modelGuid, elapsedMicroseconds: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid);

            var stats = result.Statistics.Columns["elapsedMicroseconds"];
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
            var modelGuid = Guid.NewGuid();
            await CreateWarningAsync(dbContext, modelGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(entityAnalysisModelGuid: modelGuid);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "elapsedMicroseconds", "sinceLastEntryMicroseconds");
        }
    }
}