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
using Jube.Service.EntityAnalysisAsynchronousQueueBalance;
using Jube.Service.Exceptions.EntityAnalysisAsynchronousQueueBalance;
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

namespace Jube.Test.Service.EntityAnalysisAsynchronousQueueBalance
{
    using EntityAnalysisAsynchronousQueueBalanceService =
        EntityAnalysisAsynchronousQueueBalanceService;
    using EntityAnalysisAsynchronousQueueBalancePoco = Data.Poco.EntityAnalysisAsynchronousQueueBalance;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisAsynchronousQueueBalanceServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

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
                await dbContext.EntityAnalysisAsynchronousQueueBalance.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisAsynchronousQueueBalanceService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisAsynchronousQueueBalanceService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<string> CreateRowAsync(DbContext dbContext, int caseCreation = 0, int tagging = 0,
            int notification = 0, int asynchronousInvoke = 0, DateTime? createdDate = null, string? instance = null)
        {
            instance ??= $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            var id = await dbContext.InsertWithInt32IdentityAsync(
                new EntityAnalysisAsynchronousQueueBalancePoco
                {
                    Instance = instance,
                    CreatedDate = createdDate ?? DateTime.UtcNow,
                    CaseCreation = caseCreation,
                    Tagging = tagging,
                    Notification = notification,
                    AsynchronousInvoke = asynchronousInvoke
                }).ConfigureAwait(false);

            createdRowIds.Add(id);
            return instance;
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceOne = await CreateRowAsync(dbContext, createdDate: DateTime.UtcNow.AddMinutes(-30));
            var instanceTwo = await CreateRowAsync(dbContext, createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: DateTime.UtcNow.AddHours(-1));

            var mine = result.Rows.Where(r => r.Instance == instanceOne || r.Instance == instanceTwo).ToList();
            mine.Should().HaveCount(2);
            mine[0].Instance.Should().Be(instanceTwo);
            mine[1].Instance.Should().Be(instanceOne);
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
        public async Task CreateWithBlankUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, "   "));
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
            var instanceOne = await CreateRowAsync(dbContext, 1);
            var instanceTwo = await CreateRowAsync(dbContext, 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(samplePercentage: 0);

            result.Rows.Should().NotContain(r => r.Instance == instanceOne || r.Instance == instanceTwo);
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceOne = await CreateRowAsync(dbContext, 1);
            var instanceTwo = await CreateRowAsync(dbContext, 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(samplePercentage: 100);

            result.Rows.Where(r => r.Instance == instanceOne || r.Instance == instanceTwo).Should().HaveCount(2);
        }

        [Fact]
        public async Task ListFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            var oldInstance = await CreateRowAsync(dbContext, createdDate: oldDate);
            var newInstance = await CreateRowAsync(dbContext, createdDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Instance == oldInstance || r.Instance == newInstance).ToList();
            mine.Should().ContainSingle();
            mine[0].Instance.Should().Be(newInstance);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            var oldInstance = await CreateRowAsync(dbContext, createdDate: twoHoursAgo);
            var newInstance = await CreateRowAsync(dbContext, createdDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Instance == oldInstance || r.Instance == newInstance).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Instance.Should().Be(newInstance);
        }

        [Fact]
        public async Task ListSortsByCaseCreationAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceLow = await CreateRowAsync(dbContext, 10);
            var instanceMid = await CreateRowAsync(dbContext, 20);
            var instanceHigh = await CreateRowAsync(dbContext, 30);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(sortField: "caseCreation", sortDirection: "asc");
            var ascendingMine = ascending.Rows
                .Where(r => r.Instance == instanceLow || r.Instance == instanceMid || r.Instance == instanceHigh)
                .ToList();
            ascendingMine.Select(r => r.CaseCreation).Should().Equal(10, 20, 30);

            var descending = await service.ListAsync(sortField: "caseCreation", sortDirection: "desc");
            var descendingMine = descending.Rows
                .Where(r => r.Instance == instanceLow || r.Instance == instanceMid || r.Instance == instanceHigh)
                .ToList();
            descendingMine.Select(r => r.CaseCreation).Should().Equal(30, 20, 10);
        }

        [Fact]
        public async Task ListSortsByInstanceAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}SortInstance{Guid.NewGuid():N}";
            var instanceA = await CreateRowAsync(dbContext, instance: $"{prefix}A");
            var instanceB = await CreateRowAsync(dbContext, instance: $"{prefix}B");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(sortField: "instance", sortDirection: "asc");
            var ascendingMine = ascending.Rows.Where(r => r.Instance == instanceA || r.Instance == instanceB)
                .ToList();
            ascendingMine.Select(r => r.Instance).Should().Equal(instanceA, instanceB);

            var descending = await service.ListAsync(sortField: "instance", sortDirection: "desc");
            var descendingMine = descending.Rows.Where(r => r.Instance == instanceA || r.Instance == instanceB)
                .ToList();
            descendingMine.Select(r => r.Instance).Should().Equal(instanceB, instanceA);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var instanceHigh = await CreateRowAsync(dbContext, 20);
            var instanceLow = await CreateRowAsync(dbContext, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(sortField: "caseCreation", sortDirection: ascendingKeyword);

            var mine = result.Rows.Where(r => r.Instance == instanceHigh || r.Instance == instanceLow).ToList();
            mine.Select(r => r.CaseCreation).Should().Equal(10, 20);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceLow = await CreateRowAsync(dbContext, 10);
            var instanceHigh = await CreateRowAsync(dbContext, 20);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(sortField: "caseCreation", sortDirection: "banana");

            var mine = result.Rows.Where(r => r.Instance == instanceLow || r.Instance == instanceHigh).ToList();
            mine.Select(r => r.CaseCreation).Should().Equal(20, 10);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceOld = await CreateRowAsync(dbContext, createdDate: DateTime.UtcNow.AddMinutes(-30));
            var instanceNew = await CreateRowAsync(dbContext, createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(sortField: "notARealColumn", from: DateTime.UtcNow.AddHours(-1));

            var mine = result.Rows.Where(r => r.Instance == instanceOld || r.Instance == instanceNew).ToList();
            mine[0].Instance.Should().Be(instanceNew);
            mine[1].Instance.Should().Be(instanceOld);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var from = DateTime.UtcNow.AddMinutes(-1);
            for (var i = 0; i < 5; i++)
            {
                await CreateRowAsync(dbContext, 1);
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
            await CreateRowAsync(dbContext, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "caseCreation", "tagging", "notification", "asynchronousEntityInvoke");
            result.Statistics.Columns["asynchronousEntityInvoke"].Min.Should().Be(0,
                "AsynchronousEntityInvoke is always 0 in the DTO -- a pre-existing mapping gap");
            result.Statistics.Columns["asynchronousEntityInvoke"].Max.Should().Be(0);
        }

        [Fact]
        public async Task ListNameIsAlwaysNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = await CreateRowAsync(dbContext, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Single(r => r.Instance == instance);
            mine.Name.Should().BeNull(
                "the legacy DTO's Name property was never mapped from any source column -- preserved as-is");
        }

        [Fact]
        public async Task ListAsynchronousEntityInvokeIsAlwaysZeroEvenWhenUnderlyingColumnIsSetAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = await CreateRowAsync(dbContext, asynchronousInvoke: 42);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Single(r => r.Instance == instance);
            mine.AsynchronousEntityInvoke.Should().Be(0,
                "the legacy DTO's AsynchronousEntityInvoke never matched the underlying AsynchronousInvoke column name -- preserved as-is");
        }
    }
}