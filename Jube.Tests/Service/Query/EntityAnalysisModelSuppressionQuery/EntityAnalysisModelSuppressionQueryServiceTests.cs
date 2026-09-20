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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.EntityAnalysisModelSuppressionQuery;
using Jube.Service.Query.EntityAnalysisModelSuppressionQuery;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.EntityAnalysisModelSuppressionQuery
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelSuppressionQueryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdXpathIds = [];
        private readonly List<int> createdSuppressionIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelSuppression.Where(w => createdSuppressionIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => createdXpathIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<EntityAnalysisModelSuppressionQueryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelSuppressionQueryService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static Task<int> TenantOfAsync(DbContext dbContext, string userName) =>
            dbContext.UserInTenant.Where(w => w.User == userName).Select(w => w.TenantRegistryId).FirstAsync();

        private async Task<Data.Poco.EntityAnalysisModel> InsertModelAsync(DbContext dbContext, int tenantRegistryId,
            string key, bool enableSuppression = true, byte deletedModel = 0, byte deletedXpath = 0)
        {
            var model = new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Sq{Guid.NewGuid():N}"[..30],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = deletedModel,
                TenantRegistryId = tenantRegistryId,
            };
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model);
            createdModelIds.Add(model.Id);

            createdXpathIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = model.Id,
                    Name = key,
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = deletedXpath,
                    EnableSuppression = (byte)(enableSuppression ? 1 : 0),
                }));
            return model;
        }

        private async Task InsertSuppressionAsync(DbContext dbContext, Guid modelGuid, string key, string value,
            DateTime? expiry = null, byte? deleted = 0)
        {
            createdSuppressionIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelSuppression
                {
                    EntityAnalysisModelGuid = modelGuid,
                    SuppressionKey = key,
                    SuppressionKeyValue = value,
                    DeleteExpiryDate = expiry,
                    Deleted = deleted,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix,
                    Version = 1,
                }));
        }

        private static string NewKey() => $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..24];

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                EntityAnalysisModelSuppressionQueryService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task GetAsyncThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("k", "v"));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([2]);
        }

        [Fact]
        public async Task GetAsyncMapsSuppressedModelFieldByFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);
            var expiry = DateTime.UtcNow.AddHours(3);
            await InsertSuppressionAsync(dbContext, model.Guid, key, "value1", expiry);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(key, "value1");

            var dto = result.Should().ContainSingle().Subject;
            dto.Name.Should().Be(model.Name);
            dto.EntityAnalysisModelGuid.Should().Be(model.Guid);
            dto.Suppression.Should().BeTrue();
            dto.DeleteExpiryDate.Should().NotBeNull();
            dto.DeleteExpiryDate.Required().UtcDateTime.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetAsyncReportsSuppressionWithNullExpiryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);
            await InsertSuppressionAsync(dbContext, model.Guid, key, "value1", null, null);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(key, "value1")).Should().ContainSingle().Subject;

            dto.Suppression.Should().BeTrue();
            dto.DeleteExpiryDate.Should().BeNull();
        }

        [Fact]
        public async Task GetAsyncReturnsModelWithoutSuppressionWhenNoneMatchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);
            await InsertSuppressionAsync(dbContext, model.Guid, key, "other");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(key, "value1")).Should().ContainSingle().Subject;

            dto.EntityAnalysisModelGuid.Should().Be(model.Guid);
            dto.Suppression.Should().BeFalse();
            dto.DeleteExpiryDate.Should().BeNull();
        }

        [Fact]
        public async Task GetAsyncExpiredOrDeletedSuppressionsAreNotActiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var expiredModel = await InsertModelAsync(dbContext, tenantA, key);
            var deletedModel = await InsertModelAsync(dbContext, tenantA, key);
            await InsertSuppressionAsync(dbContext, expiredModel.Guid, key, "v", DateTime.UtcNow.AddHours(-1));
            await InsertSuppressionAsync(dbContext, deletedModel.Guid, key, "v", null, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(key, "v");

            result.Should().HaveCount(2);
            result.Should().OnlyContain(d => !d.Suppression);
        }

        [Fact]
        public async Task GetAsyncExcludesDeletedModelsDeletedXpathsAndSuppressionDisabledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var live = await InsertModelAsync(dbContext, tenantA, key);
            await InsertModelAsync(dbContext, tenantA, key, deletedModel: 1);
            await InsertModelAsync(dbContext, tenantA, key, deletedXpath: 1);
            await InsertModelAsync(dbContext, tenantA, key, enableSuppression: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(key, "v");

            result.Should().ContainSingle().Which.EntityAnalysisModelGuid.Should().Be(live.Guid);
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyWhenNoModelSupportsTheKeyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(NewKey(), "v");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBDataIsInvisibleToTenantAAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            tenantA.Should().NotBe(tenantB);
            var key = NewKey();
            var modelA = await InsertModelAsync(dbContext, tenantA, key);
            var modelB = await InsertModelAsync(dbContext, tenantB, key);
            await InsertSuppressionAsync(dbContext, modelA.Guid, key, "v");
            await InsertSuppressionAsync(dbContext, modelB.Guid, key, "v");

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var fromA = await serviceA.GetAsync(key, "v");
            var fromB = await serviceB.GetAsync(key, "v");

            fromA.Should().ContainSingle().Which.EntityAnalysisModelGuid.Should().Be(modelA.Guid);
            fromB.Should().ContainSingle().Which.EntityAnalysisModelGuid.Should().Be(modelB.Guid);
        }

        [Fact]
        public async Task SuppressionOnOtherTenantsModelDoesNotLeakIntoOwnModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var key = NewKey();
            var modelA = await InsertModelAsync(dbContext, tenantA, key);
            var modelB = await InsertModelAsync(dbContext, tenantB, key);
            await InsertSuppressionAsync(dbContext, modelB.Guid, key, "v");
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var fromA = await serviceA.GetAsync(key, "v");

            var dto = fromA.Should().ContainSingle().Subject;
            dto.EntityAnalysisModelGuid.Should().Be(modelA.Guid);
            dto.Suppression.Should().BeFalse();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync("k", "v", cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("k", "v"));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(NewKey(), "v");

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetAsync(NewKey(), "v");
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync("k", "v"));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("EntityAnalysisModelSuppressionQueryGet");
        }
    }
}