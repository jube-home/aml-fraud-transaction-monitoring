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
using Jube.Service.Exceptions.Repository.ActivationWatcher;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.ActivationWatcher;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.ActivationWatcher
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ActivationWatcherServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.ActivationWatcher.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static async Task<int> ResolveTenantRegistryIdAsync(DbContext dbContext, string userName)
        {
            var tenantRegistryId = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName);

            return tenantRegistryId.Required();
        }

        private static Task<ActivationWatcherService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, IServiceChangeBus? serviceChangeBus = null)
        {
            return ActivationWatcherService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus());
        }

        private async Task CreateSampleAsync(DbContext dbContext, int tenantRegistryId, string key,
            string keyValue, DateTime? createdDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ActivationWatcher
            {
                TenantRegistryId = tenantRegistryId,
                Key = key,
                KeyValue = keyValue,
                Longitude = 1.23,
                Latitude = 4.56,
                ActivationRuleSummary = "summary",
                ResponseElevationContent = "content",
                ResponseElevation = 7.89,
                BackColor = "#ff0000",
                ForeColor = "#000000",
                CreatedDate = createdDate ?? DateTime.UtcNow
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        [Fact]
        public async Task ReplayReturnsRowsInTenantAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}";
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);

            await CreateSampleAsync(dbContext, tenantRegistryId, key, "KeyValueA");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result =
                await service.ReplayAsync(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));

            var mine = result.Where(r => r.Key == key).ToList();
            mine.Should().ContainSingle();
            mine[0].KeyValue.Should().Be("KeyValueA");
            mine[0].Longitude.Should().Be(1.23);
            mine[0].Latitude.Should().Be(4.56);
            mine[0].ActivationRuleSummary.Should().Be("summary");
            mine[0].ResponseElevationContent.Should().Be("content");
            mine[0].ResponseElevation.Should().Be(7.89);
            mine[0].BackColor.Should().Be("#ff0000");
            mine[0].ForeColor.Should().Be("#000000");
        }

        [Fact]
        public async Task ReplayIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var keyInOtherTenant = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}";
            var tenantARegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);

            await CreateSampleAsync(dbContext, tenantARegistryId, keyInOtherTenant, "KeyValueA");

            var tenantBService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var result = await tenantBService.ReplayAsync(DateTimeOffset.UtcNow.AddHours(-1),
                DateTimeOffset.UtcNow.AddHours(1));

            result.Should().NotContain(r => r.Key == keyInOtherTenant);
        }

        [Fact]
        public async Task ReplayFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}";
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, tenantRegistryId, key, "Old", oldDate);
            await CreateSampleAsync(dbContext, tenantRegistryId, key, "New", newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ReplayAsync(newDate.AddMinutes(-1), newDate.AddMinutes(1));

            var mine = result.Where(r => r.Key == key).ToList();
            mine.Should().ContainSingle();
            mine[0].KeyValue.Should().Be("New");
        }

        [Fact]
        public async Task ReplayReturnsOldestFirstAndCarriesThePreviousLocationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}";
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var earlier = DateTime.UtcNow.AddMinutes(-30);

            await CreateSampleAsync(dbContext, tenantRegistryId, key, "Entity", earlier);

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ActivationWatcher
            {
                TenantRegistryId = tenantRegistryId,
                Key = key,
                KeyValue = "Entity",
                Longitude = 2.35,
                Latitude = 48.86,
                PreviousLongitude = 1.23,
                PreviousLatitude = 4.56,
                PreviousCreatedDate = earlier,
                CreatedDate = DateTime.UtcNow
            });

            createdIds.Add(id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var mine = (await service.ReplayAsync(null, null, key, "Entity")).ToList();

            mine.Should().HaveCount(2);
            mine[0].CreatedDate.Should().BeBefore(mine[1].CreatedDate!.Value);
            mine[1].PreviousLatitude.Should().Be(4.56);
            mine[1].PreviousLongitude.Should().Be(1.23);
            mine[1].PreviousCreatedDate.Should().BeCloseTo(earlier, TimeSpan.FromSeconds(1));
            mine[0].PreviousLatitude.Should().BeNull();
        }

        [Fact]
        public async Task ReplayCanBeNarrowedToOneKeyAndValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}";
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);

            await CreateSampleAsync(dbContext, tenantRegistryId, key, "Mine");
            await CreateSampleAsync(dbContext, tenantRegistryId, key, "Mine");
            await CreateSampleAsync(dbContext, tenantRegistryId, key, "Theirs");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var onlyMine = await service.ReplayAsync(null, null, key, "Mine");
            var onlyTheKey = await service.ReplayAsync(null, null, key);
            var wrongKey = await service.ReplayAsync(null, null, key + "x", "Mine");

            onlyMine.Should().HaveCount(2).And.OnlyContain(r => r.KeyValue == "Mine" && r.Key == key);
            onlyTheKey.Where(r => r.Key == key).Should().HaveCount(3);
            wrongKey.Should().BeEmpty();
        }

        [Fact]
        public async Task ReplayNarrowedToAnEntityNeverReachesAnotherTenantsRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}";
            var tenantARegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);

            await CreateSampleAsync(dbContext, tenantARegistryId, key, "Shared");

            var tenantBService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await tenantBService.ReplayAsync(null, null, key, "Shared")).Should().BeEmpty();
        }

        [Fact]
        public async Task ReplayWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ReplayAsync(null, null));
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
        public async Task ReplayDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}";
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, tenantRegistryId, key, "KeyValueA");

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await service.ReplayAsync(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ReplayWithNoMatchingRowsReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ReplayAsync(DateTimeOffset.UtcNow.AddYears(-10),
                DateTimeOffset.UtcNow.AddYears(-9));

            result.Should().BeEmpty();
        }
    }
}