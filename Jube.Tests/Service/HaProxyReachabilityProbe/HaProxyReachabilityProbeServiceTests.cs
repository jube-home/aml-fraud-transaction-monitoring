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
using Jube.Service.Exceptions.HaProxyReachabilityProbe;
using Jube.Service.HaProxyReachabilityProbe;
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

namespace Jube.Test.Service.HaProxyReachabilityProbe
{
    using HaProxyReachabilityProbeService = HaProxyReachabilityProbeService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class HaProxyReachabilityProbeServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.HaProxyReachabilityProbe.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<HaProxyReachabilityProbeService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return HaProxyReachabilityProbeService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string instance, string target = "JubeUi",
            bool success = true, DateTime? createdDate = null, long connectMicroseconds = 1500)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.HaProxyReachabilityProbe
            {
                OccurredDate = createdDate ?? DateTime.UtcNow,
                Target = target,
                HaProxyAddress = instance,
                Success = success,
                ConnectMicroseconds = connectMicroseconds,
                HttpStatusCode = success ? 200 : null,
                ErrorMessage = success ? null : "Connection refused",
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

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
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
        public async Task ListFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{instance}Old", createdDate: oldDate);
            await CreateEntryAsync(dbContext, $"{instance}New", createdDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Instance.StartsWith(instance)).ToList();
            mine.Should().ContainSingle();
            mine[0].Instance.Should().Be($"{instance}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAgainstTargetAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, instance, "PostgresPrimary");
            await CreateEntryAsync(dbContext, $"{instance}Other", "JubeApi");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: "PostgresPrimary");

            var mine = result.Rows.Where(r => r.Instance == instance || r.Instance == $"{instance}Other").ToList();
            mine.Should().ContainSingle();
            mine[0].Target.Should().Be("PostgresPrimary");
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
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

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance);

            var mine = result.Rows.Where(r => r.Instance.StartsWith(instance)).ToList();
            mine.Should().ContainSingle("the default from/to window is the last hour");
            mine[0].Instance.Should().Be($"{instance}New");
        }

        [Fact]
        public async Task ListSortsByConnectMicrosecondsAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance, connectMicroseconds: 3000);
            await CreateEntryAsync(dbContext, instance, connectMicroseconds: 1000);
            await CreateEntryAsync(dbContext, instance, connectMicroseconds: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: instance, sortField: "connectMicroseconds",
                sortDirection: "asc");
            ascending.Rows.Select(r => r.ConnectMicroseconds).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: instance, sortField: "connectMicroseconds",
                sortDirection: "desc");
            descending.Rows.Select(r => r.ConnectMicroseconds).Should().Equal(3000, 2000, 1000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance, connectMicroseconds: 1000);
            await CreateEntryAsync(dbContext, instance, connectMicroseconds: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance, sortField: "connectMicroseconds",
                sortDirection: "banana");

            result.Rows.Select(r => r.ConnectMicroseconds).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            var firstId = await CreateEntryAsync(dbContext, instance, createdDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateEntryAsync(dbContext, instance, createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
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

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: instance);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForConnectMicrosecondsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, instance, connectMicroseconds: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance);

            var stats = result.Statistics.Columns["connectMicroseconds"];
            stats.Min.Should().Be(1);
            stats.Max.Should().Be(5);
            stats.Mean.Should().Be(3);
            stats.Median.Should().Be(3);
            stats.StandardDeviation.Should().BeApproximately(1.5811, 0.001);
            stats.Histogram.Sum(b => b.Frequency).Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsIncludesOnlyConnectMicrosecondsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo("connectMicroseconds");
        }

        [Fact]
        public async Task ListCapturesAFailedProbeWithNoHttpStatusCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instance = $"{DatabaseFixture.Prefix}Instance{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, instance, success: false);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: instance);

            var row = result.Rows.Should().ContainSingle().Subject;
            row.Success.Should().BeFalse();
            row.HttpStatusCode.Should().BeNull();
            row.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }
}