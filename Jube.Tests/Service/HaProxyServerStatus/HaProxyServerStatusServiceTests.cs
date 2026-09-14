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
using Jube.Service.Exceptions.HaProxyServerStatus;
using Jube.Service.HaProxyServerStatus;
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

namespace Jube.Test.Service.HaProxyServerStatus
{
    using HaProxyServerStatusService = HaProxyServerStatusService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class HaProxyServerStatusServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.HaProxyServerStatus.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<HaProxyServerStatusService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return HaProxyServerStatusService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string instance, string pxName = "jube_ui",
            string? svName = null, string status = "UP", DateTime? createdDate = null, int chkFail = 0)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new HAProxyServerStatus
            {
                OccurredDate = createdDate ?? DateTime.UtcNow,
                PxName = pxName,
                SvName = svName ?? instance,
                Status = status,
                Addr = "10.0.1.5:5001",
                CheckStatus = "L7OK",
                CheckCode = 200,
                ChkFail = chkFail,
                ChkDown = 0,
                LastChg = 120,
                Scur = 3,
                Qcur = 0,
                Weight = 1,
                Act = 1,
                Bck = 0,
                Hrsp2Xx = 100,
                Hrsp5Xx = 0,
                Mode = "http",
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
            var result = await service.ListAsync(search: instance);

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
        public async Task ListFiltersBySearchAgainstPxNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            var uniquePxName = $"UniquePx{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, instance, uniquePxName);
            await CreateEntryAsync(dbContext, $"{instance}Other", "jube_api");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniquePxName);

            var mine = result.Rows.Where(r => r.Instance == instance || r.Instance == $"{instance}Other").ToList();
            mine.Should().ContainSingle();
            mine[0].PxName.Should().Be(uniquePxName);
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, instance, status: "DOWN");
            await CreateEntryAsync(dbContext, $"{instance}Other", status: "UP");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: "DOWN");

            var mine = result.Rows.Where(r => r.Instance == instance || r.Instance == $"{instance}Other").ToList();
            mine.Should().ContainSingle();
            mine[0].Status.Should().Be("DOWN");
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
            mine.Should().ContainSingle("the default from/to window is the last hour");
            mine[0].Instance.Should().Be($"{instance}New");
        }

        [Fact]
        public async Task ListSortsByChkFailAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance, chkFail: 3);
            await CreateEntryAsync(dbContext, instance, chkFail: 1);
            await CreateEntryAsync(dbContext, instance, chkFail: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: instance, sortField: "chkFail", sortDirection: "asc");
            ascending.Rows.Select(r => r.ChkFail).Should().Equal(1, 2, 3);

            var descending = await service.ListAsync(search: instance, sortField: "chkFail", sortDirection: "desc");
            descending.Rows.Select(r => r.ChkFail).Should().Equal(3, 2, 1);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance, chkFail: 1);
            await CreateEntryAsync(dbContext, instance, chkFail: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance, sortField: "chkFail", sortDirection: "banana");

            result.Rows.Select(r => r.ChkFail).Should().Equal(2, 1);
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
        public async Task ListStatisticsComputesExactValuesForChkFailAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            foreach (var value in new[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, instance, chkFail: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: instance);

            var stats = result.Statistics.Columns["chkFail"];
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
                "chkFail", "chkDown", "lastChg", "scur", "qcur", "weight", "hrsp2Xx", "hrsp5Xx");
        }
    }
}