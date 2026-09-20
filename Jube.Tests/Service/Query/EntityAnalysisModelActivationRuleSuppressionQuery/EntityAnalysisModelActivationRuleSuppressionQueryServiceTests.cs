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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.EntityAnalysisModelActivationRuleSuppressionQuery;
using Jube.Service.Query.EntityAnalysisModelActivationRuleSuppressionQuery;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.EntityAnalysisModelActivationRuleSuppressionQuery.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.EntityAnalysisModelActivationRuleSuppressionQuery
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelActivationRuleSuppressionQueryServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdRuleIds = [];
        private readonly List<int> createdSuppressionIds = [];
        private readonly List<int> createdXpathIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRuleSuppression>()
                .Where(w => createdSuppressionIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelRequestXpath>()
                .Where(w => createdXpathIds.Contains(w.Id)).DeleteAsync();

            foreach (var id in createdRuleIds)
            {
                await dbContext.GetTable<EntityAnalysisModelActivationRuleVersion>()
                    .Where(w => w.EntityAnalysisModelActivationRuleId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelActivationRuleSuppressionQueryService> BuildServiceAsync(
            DbContext dbContext, string? userName,
            ILog? log = null, ILog? auditLog = null, IServiceChangeBus? serviceChangeBus = null) =>
            EntityAnalysisModelActivationRuleSuppressionQueryService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);

        private static string Unique(string label) => $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];

        private async Task<Seeded> SeedAsync(DbContext dbContext, string ownerUser, bool suppressed,
            bool ruleEnableSuppression = true, bool xpathEnableSuppression = true, DateTime? expiry = null,
            byte? suppressionDeleted = 0)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, ownerUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = Unique("Model"),
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            createdModelIds.Add(model.Id);

            var ruleName = Unique("Rule");
            var rule = await new EntityAnalysisModelActivationRuleRepository(dbContext, ownerUser).InsertAsync(
                new Data.Poco.EntityAnalysisModelActivationRule
                {
                    EntityAnalysisModelId = model.Id,
                    Name = ruleName,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    EnableSuppression = (byte)(ruleEnableSuppression ? 1 : 0)
                });
            createdRuleIds.Add(rule.Id);

            var key = Unique("Key");
            var xpathId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = model.Id,
                Name = key,
                XPath = "$.a",
                DataTypeId = 1,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableSuppression = (byte)(xpathEnableSuppression ? 1 : 0),
                Guid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedUser = ownerUser,
                Version = 1
            });
            createdXpathIds.Add(xpathId);

            var keyValue = Unique("Value");
            if (!suppressed)
            {
                return new Seeded(model.Guid, key, keyValue, ruleName, rule.Id);
            }

            var suppressionId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelActivationRuleSuppression
                {
                    EntityAnalysisModelGuid = model.Guid,
                    SuppressionKey = key,
                    SuppressionKeyValue = keyValue,
                    EntityAnalysisModelActivationRuleName = ruleName,
                    DeleteExpiryDate = expiry,
                    Deleted = suppressionDeleted,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = ownerUser,
                    Version = 1
                });
            createdSuppressionIds.Add(suppressionId);

            return new Seeded(model.Guid, key, keyValue, ruleName, rule.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                EntityAnalysisModelActivationRuleSuppressionQueryService.CreateAsync(dbContext, userName, log,
                    localizers, new NullServiceChangeBus(),
                    TestLog.NoOp));

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
        public async Task GetThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetAsync(seeded.ModelGuid, seeded.Key, seeded.KeyValue));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([2]);
        }

        [Fact]
        public async Task GetReturnsSuppressedRuleWithExactFieldMappingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var expiry = DateTime.UtcNow.AddHours(3);
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, true, expiry: expiry);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(seeded.ModelGuid, seeded.Key, seeded.KeyValue);

            var dto = result.Should().ContainSingle().Subject;
            dto.Name.Should().Be(seeded.RuleName);
            dto.Suppression.Should().BeTrue();
            dto.EntityAnalysisModelGuid.Should().Be(seeded.ModelGuid);
            dto.EntityAnalysisModelActivationRuleSuppressionId.Should().Be(seeded.RuleId);
            dto.DeleteExpiryDate.Should().NotBeNull();
            dto.DeleteExpiryDate.Required().Offset.Should().Be(TimeSpan.Zero);
            dto.DeleteExpiryDate.Required().UtcDateTime.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GetReturnsRuleUnsuppressedWhenNoSuppressionRowExistsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(seeded.ModelGuid, seeded.Key, seeded.KeyValue);

            var dto = result.Should().ContainSingle().Subject;
            dto.Name.Should().Be(seeded.RuleName);
            dto.Suppression.Should().BeFalse();
            dto.DeleteExpiryDate.Should().BeNull();
        }

        [Fact]
        public async Task GetSuppressionWithNoExpiryIsActiveWithNullExpiryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, true, expiry: null);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(seeded.ModelGuid, seeded.Key, seeded.KeyValue)).Single();

            dto.Suppression.Should().BeTrue();
            dto.DeleteExpiryDate.Should().BeNull();
        }

        [Fact]
        public async Task GetIgnoresExpiredSuppressionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, true,
                expiry: DateTime.UtcNow.AddHours(-1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(seeded.ModelGuid, seeded.Key, seeded.KeyValue)).Single();

            dto.Suppression.Should().BeFalse();
        }

        [Fact]
        public async Task GetIgnoresDeletedSuppressionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, true, suppressionDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(seeded.ModelGuid, seeded.Key, seeded.KeyValue)).Single();

            dto.Suppression.Should().BeFalse();
        }

        [Fact]
        public async Task GetIgnoresSuppressionForADifferentKeyValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(seeded.ModelGuid, seeded.Key, "some-other-value")).Single();

            dto.Suppression.Should().BeFalse();
        }

        [Fact]
        public async Task GetReturnsEmptyForUnknownModelGuidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(Guid.NewGuid(), "k", "v");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetReturnsEmptyForNullKeyAndValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(Guid.Empty, null, null);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetReturnsEmptyWhenKeyIsNotASuppressionEnabledXpathAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, false,
                xpathEnableSuppression: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(seeded.ModelGuid, seeded.Key, seeded.KeyValue);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetReturnsEmptyWhenRuleHasSuppressionDisabledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, false,
                ruleEnableSuppression: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(seeded.ModelGuid, seeded.Key, seeded.KeyValue);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBDataIsInvisibleToTenantAAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seededB = await SeedAsync(dbContext, fx.Seed.UserTenantB, true);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await serviceA.GetAsync(seededB.ModelGuid, seededB.Key, seededB.KeyValue);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task TenantADataIsInvisibleToTenantBAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seededA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, true);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var result = await serviceB.GetAsync(seededA.ModelGuid, seededA.Key, seededA.KeyValue);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task EachTenantSeesOnlyItsOwnModelWhenBothExistAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seededA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, true);
            var seededB = await SeedAsync(dbContext, fx.Seed.UserTenantB, true);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(seededA.ModelGuid, seededA.Key, seededA.KeyValue)).Should().ContainSingle();
            (await serviceB.GetAsync(seededB.ModelGuid, seededB.Key, seededB.KeyValue)).Should().ContainSingle();
            (await serviceA.GetAsync(seededB.ModelGuid, seededB.Key, seededB.KeyValue)).Should().BeEmpty();
            (await serviceB.GetAsync(seededA.ModelGuid, seededA.Key, seededA.KeyValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.GetAsync(Guid.NewGuid(), "k", "v", cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(Guid.NewGuid(), "k", "v"));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(Guid.NewGuid(), "k", "v");

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

            await service.GetAsync(Guid.NewGuid(), "k", "v");
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(Guid.NewGuid(), "k", "v"));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("EntityAnalysisModelActivationRuleSuppressionQueryGet");
        }
    }
}