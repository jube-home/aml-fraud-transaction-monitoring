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
using Jube.Dto.Overrides;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.EntityAnalysisModelOverrideQuery;
using Jube.Service.Query.EntityAnalysisModelOverrideQuery;
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

namespace Jube.Test.Service.Query.EntityAnalysisModelOverrideQuery
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelOverrideQueryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdXpathIds = [];
        private readonly List<int> createdOverrideIds = [];
        private readonly List<int> createdActivationRuleOverrideIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelOverride.Where(w => createdOverrideIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => createdActivationRuleOverrideIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => createdXpathIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<EntityAnalysisModelOverrideQueryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelOverrideQueryService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static Task<int> TenantOfAsync(DbContext dbContext, string userName) =>
            dbContext.UserInTenant.Where(w => w.User == userName).Select(w => w.TenantRegistryId).FirstAsync();

        private async Task<Data.Poco.EntityAnalysisModel> InsertModelAsync(DbContext dbContext, int tenantRegistryId,
            string key, bool enableOverride = true, byte deletedModel = 0, byte deletedXpath = 0)
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
                    EnableOverride = (byte)(enableOverride ? 1 : 0),
                }));
            return model;
        }

        private async Task InsertOverrideAsync(DbContext dbContext, Guid modelGuid, string key, string value,
            DateTime? expiry = null, byte? deleted = 0)
        {
            createdOverrideIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelOverride
                {
                    EntityAnalysisModelGuid = modelGuid,
                    OverrideKey = key,
                    OverrideKeyValue = value,
                    DeleteExpiryDate = expiry,
                    Deleted = deleted,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix,
                    Version = 1,
                }));
        }

        private async Task InsertActivationRuleOverrideAsync(DbContext dbContext, Guid modelGuid, string key,
            string value, string activationRuleName, byte overrideKind = 0, DateTime? expiry = null,
            byte? deleted = 0)
        {
            createdActivationRuleOverrideIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelActivationRuleOverride
                {
                    EntityAnalysisModelGuid = modelGuid,
                    OverrideKey = key,
                    OverrideKeyValue = value,
                    EntityAnalysisModelActivationRuleName = activationRuleName,
                    OverrideKind = overrideKind,
                    DeleteExpiryDate = expiry,
                    Deleted = deleted,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix,
                    Version = 1
                }));
        }

        [Fact]
        public async Task ValuesListsAValueHeldOnlyByAnActivationRuleOverrideAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);

            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "ruleOnly", "AnyRule");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var values = await service.GetValuesAsync(key);

            values.Select(v => v.OverrideKeyValue).Should().Contain("ruleOnly",
                "the rule level table is now the primary override surface, so a value held only there must still be listed");
        }

        [Fact]
        public async Task ValuesListsAValueOncePerModelWhenSeveralActivationRulesOverrideItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);

            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "shared", "RuleOne");
            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "shared", "RuleTwo");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var values = await service.GetValuesAsync(key);

            values.Count(v => v.OverrideKeyValue == "shared").Should().Be(1,
                "the listing is of values, so several rules bound to one value must not repeat that value");
        }

        [Fact]
        public async Task ValuesReportsForceWhereAnyOverrideOnTheValueIsForceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);

            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "mixed", "RuleOne");
            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "mixed", "RuleTwo", 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var values = await service.GetValuesAsync(key);

            values.Single(v => v.OverrideKeyValue == "mixed").OverrideKind.Should()
                .Be(EntityAnalysisModelOverrideKind.Force,
                    "Force wins in the engine, so the listing must not report the value as merely suppressed");
        }

        [Fact]
        public async Task ValuesExcludesDeletedAndExpiredActivationRuleOverridesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);

            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "gone", "RuleOne", 0, null, 1);
            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "stale", "RuleTwo", 0,
                DateTime.UtcNow.AddHours(-1));

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var values = await service.GetValuesAsync(key);

            values.Select(v => v.OverrideKeyValue).Should().NotContain("gone");
            values.Select(v => v.OverrideKeyValue).Should().NotContain("stale");
        }

        [Fact]
        public async Task KeysListsAnOverrideEnabledKeyThatHoldsNothingYetAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            await InsertModelAsync(dbContext, tenantA, key);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var keys = await service.GetKeysAsync();

            var row = keys.Single(w => w.OverrideKey == key);
            row.Values.Should().Be(0);
            row.Overrides.Should().Be(0);
            row.EnabledOnModels.Should().Be(1,
                "the summary is of the keys available, so a key that holds nothing yet must still be offered");
        }

        [Fact]
        public async Task KeysAggregatesValuesOverridesForcedAndRulesAcrossBothTablesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);

            await InsertOverrideAsync(dbContext, model.Guid, key, "cardOne");
            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "cardOne", "RuleOne");
            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "cardTwo", "RuleTwo", 1);
            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "cardThree", "RuleTwo", 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var row = (await service.GetKeysAsync()).Single(w => w.OverrideKey == key);

            row.Values.Should().Be(3, "three distinct values are overridden against the key");
            row.Overrides.Should().Be(4, "every live binding counts, whichever table it is held in");
            row.Forced.Should().Be(2, "two of the bindings force their rule rather than suppress it");
            row.ActivationRules.Should().Be(2, "RuleOne and RuleTwo are the distinct rules bound");
            row.AllActivationRules.Should().Be(1, "one binding is the model level all activation rules override");
        }

        [Fact]
        public async Task KeysExcludesDeletedAndExpiredOverridesFromItsCountsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);

            await InsertOverrideAsync(dbContext, model.Guid, key, "live");
            await InsertOverrideAsync(dbContext, model.Guid, key, "removed", null, 1);
            await InsertActivationRuleOverrideAsync(dbContext, model.Guid, key, "stale", "RuleOne", 0,
                DateTime.UtcNow.AddHours(-1));

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var row = (await service.GetKeysAsync()).Single(w => w.OverrideKey == key);

            row.Values.Should().Be(1, "a deleted and an expired override are not held against the key any more");
            row.Overrides.Should().Be(1);
        }

        [Fact]
        public async Task KeysNeverReportsAnotherTenantsOverridesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var key = NewKey();
            var modelA = await InsertModelAsync(dbContext, tenantA, key);
            var modelB = await InsertModelAsync(dbContext, tenantB, key);

            await InsertOverrideAsync(dbContext, modelA.Guid, key, "mine");
            await InsertOverrideAsync(dbContext, modelB.Guid, key, "theirs");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var row = (await service.GetKeysAsync()).Single(w => w.OverrideKey == key);

            row.Values.Should().Be(1, "the other tenant's value must not be counted");
            row.EnabledOnModels.Should().Be(1, "nor its model");
        }

        [Fact]
        public async Task KeysReportsTheEarliestExpiryAndTheLatestCreationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);

            var soon = DateTime.UtcNow.AddHours(2);
            var later = DateTime.UtcNow.AddHours(8);
            await InsertOverrideAsync(dbContext, model.Guid, key, "first", later);
            await InsertOverrideAsync(dbContext, model.Guid, key, "second", soon);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var row = (await service.GetKeysAsync()).Single(w => w.OverrideKey == key);

            row.NextExpiryDate.Should().NotBeNull();
            row.NextExpiryDate!.Value.UtcDateTime.Should().BeCloseTo(soon, TimeSpan.FromSeconds(5),
                "the next expiry is the soonest one, being what an operator needs to notice");
            row.LastCreatedDate.Should().NotBeNull();
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
                EntityAnalysisModelOverrideQueryService.CreateAsync(dbContext, userName, log, localizers,
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
            await InsertOverrideAsync(dbContext, model.Guid, key, "value1", expiry);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(key, "value1");

            var dto = result.Should().ContainSingle().Subject;
            dto.Name.Should().Be(model.Name);
            dto.EntityAnalysisModelGuid.Should().Be(model.Guid);
            dto.HasOverride.Should().BeTrue();
            dto.DeleteExpiryDate.Should().NotBeNull();
            dto.DeleteExpiryDate.Required().UtcDateTime.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetAsyncReportsOverrideWithNullExpiryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);
            await InsertOverrideAsync(dbContext, model.Guid, key, "value1", null, null);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(key, "value1")).Should().ContainSingle().Subject;

            dto.HasOverride.Should().BeTrue();
            dto.DeleteExpiryDate.Should().BeNull();
        }

        [Fact]
        public async Task GetAsyncReturnsModelWithoutOverrideWhenNoneMatchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var model = await InsertModelAsync(dbContext, tenantA, key);
            await InsertOverrideAsync(dbContext, model.Guid, key, "other");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(key, "value1")).Should().ContainSingle().Subject;

            dto.EntityAnalysisModelGuid.Should().Be(model.Guid);
            dto.HasOverride.Should().BeFalse();
            dto.DeleteExpiryDate.Should().BeNull();
        }

        [Fact]
        public async Task GetAsyncExpiredOrDeletedOverridesAreNotActiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var expiredModel = await InsertModelAsync(dbContext, tenantA, key);
            var deletedModel = await InsertModelAsync(dbContext, tenantA, key);
            await InsertOverrideAsync(dbContext, expiredModel.Guid, key, "v", DateTime.UtcNow.AddHours(-1));
            await InsertOverrideAsync(dbContext, deletedModel.Guid, key, "v", null, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(key, "v");

            result.Should().HaveCount(2);
            result.Should().OnlyContain(d => !d.HasOverride);
        }

        [Fact]
        public async Task GetAsyncExcludesDeletedModelsDeletedXpathsAndOverrideDisabledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();
            var live = await InsertModelAsync(dbContext, tenantA, key);
            await InsertModelAsync(dbContext, tenantA, key, deletedModel: 1);
            await InsertModelAsync(dbContext, tenantA, key, deletedXpath: 1);
            await InsertModelAsync(dbContext, tenantA, key, enableOverride: false);
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
            await InsertOverrideAsync(dbContext, modelA.Guid, key, "v");
            await InsertOverrideAsync(dbContext, modelB.Guid, key, "v");

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var fromA = await serviceA.GetAsync(key, "v");
            var fromB = await serviceB.GetAsync(key, "v");

            fromA.Should().ContainSingle().Which.EntityAnalysisModelGuid.Should().Be(modelA.Guid);
            fromB.Should().ContainSingle().Which.EntityAnalysisModelGuid.Should().Be(modelB.Guid);
        }

        [Fact]
        public async Task OverrideOnOtherTenantsModelDoesNotLeakIntoOwnModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var key = NewKey();
            var modelA = await InsertModelAsync(dbContext, tenantA, key);
            var modelB = await InsertModelAsync(dbContext, tenantB, key);
            await InsertOverrideAsync(dbContext, modelB.Guid, key, "v");
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var fromA = await serviceA.GetAsync(key, "v");

            var dto = fromA.Should().ContainSingle().Subject;
            dto.EntityAnalysisModelGuid.Should().Be(modelA.Guid);
            dto.HasOverride.Should().BeFalse();
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
            names.Should().Contain("EntityAnalysisModelOverrideQueryGet");
        }

        [Fact]
        public async Task ValuesListsEachOverrideOnceWhereAModelGuidIsSharedWithADeletedTwinAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();

            var live = await InsertModelAsync(dbContext, tenantA, key);

            var deletedTwin = new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Sq{Guid.NewGuid():N}"[..30],
                Guid = live.Guid,
                Active = 1,
                Locked = 0,
                Deleted = 1,
                TenantRegistryId = tenantA
            };
            deletedTwin.Id = await dbContext.InsertWithInt32IdentityAsync(deletedTwin);
            createdModelIds.Add(deletedTwin.Id);

            await InsertOverrideAsync(dbContext, live.Guid, key, "Test1");
            await InsertOverrideAsync(dbContext, live.Guid, key, "ddd");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var values = await service.GetValuesAsync(key);

            values.Select(v => v.OverrideKeyValue).Should().BeEquivalentTo(new[] { "Test1", "ddd" },
                "a model guid shared with a soft deleted row must not multiply the override through the join");
        }

        [Fact]
        public async Task ValuesExcludesOverridesOfDeletedModelsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();

            var live = await InsertModelAsync(dbContext, tenantA, key);
            var gone = await InsertModelAsync(dbContext, tenantA, key, deletedModel: 1);

            await InsertOverrideAsync(dbContext, live.Guid, key, "kept");
            await InsertOverrideAsync(dbContext, gone.Guid, key, "dropped");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var values = await service.GetValuesAsync(key);

            values.Select(v => v.OverrideKeyValue).Should().BeEquivalentTo(new[] { "kept" },
                "an override belonging to a deleted model is not a live override");
        }

        [Fact]
        public async Task ValuesKeepsTheSameValueOnceForEachModelThatOverridesItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var key = NewKey();

            var first = await InsertModelAsync(dbContext, tenantA, key);
            var second = await InsertModelAsync(dbContext, tenantA, key);

            await InsertOverrideAsync(dbContext, first.Guid, key, "shared");
            await InsertOverrideAsync(dbContext, second.Guid, key, "shared");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var values = await service.GetValuesAsync(key);

            values.Should().HaveCount(2,
                "one value genuinely overridden on two models is two rows, one per model, not a duplicate");
            values.Select(v => v.Name).Should().BeEquivalentTo(new[] { first.Name, second.Name });
        }
    }
}