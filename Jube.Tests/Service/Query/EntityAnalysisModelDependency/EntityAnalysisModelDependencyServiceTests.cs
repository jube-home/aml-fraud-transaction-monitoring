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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Exceptions.Query.EntityAnalysisModelDependency;
using Jube.Service.Query.EntityAnalysisModelDependency;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.EntityAnalysisModelDependency
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelDependencyServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private int accountIdField;
        private int activationRuleId;
        private Guid modelGuid;
        private int modelId;
        private int ttlCounterId;
        private int unusedListId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Dep{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0
                });
            (modelId, modelGuid) = (model.Id, model.Guid);

            accountIdField = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId, Name = "AccountId", DataTypeId = 1, XPath = "$.AccountId",
                Active = 1, Cache = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
            ttlCounterId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId, Name = "PerAccount", TtlCounterDataName = "AccountId",
                TtlCounterInterval = "d", TtlCounterValue = 1, Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
            activationRuleId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelActivationRule
                {
                    EntityAnalysisModelId = modelId, Name = "ManyPerAccount", RuleScriptTypeId = 2,
                    CoderRuleScript = "If (TTLCounter.PerAccount > 3) Then\n   Return True\nEnd If",
                    Active = 1, Deleted = 0, Guid = Guid.NewGuid()
                });
            await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelActivationRule
            {
                EntityAnalysisModelId = modelId, Name = "Dangling", RuleScriptTypeId = 2,
                CoderRuleScript = "If (TTLCounter.Missing > 3) Then\n   Return True\nEnd If",
                Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
            unusedListId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelList
            {
                EntityAnalysisModelGuid = modelGuid, Name = "Unused", Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuid)
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>()
                .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelTtlCounter>()
                .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
        }

        private Task<EntityAnalysisModelDependencyService> ServiceAsync(Data.Context.DbContext dbContext,
            string? userName = null)
        {
            return EntityAnalysisModelDependencyService.CreateAsync(dbContext,
                userName ?? fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());
        }

        [Fact]
        public async Task AFieldsDependentsIncludeWhatUsesItThroughAnotherEntityAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var impact = await service.DependentsAsync(modelId, "RequestXPath", accountIdField);

            impact.Errors.Should().BeEmpty();
            impact.Entity!.Name.Should().Be("AccountId");
            impact.DirectDependents.Should().Be(1);
            impact.DeleteBlocked.Should().BeTrue();
            impact.Dependents.Should().ContainSingle(d => d.Depth == 1 && d.Dependent.Kind == "TtlCounter" &&
                                                          d.DependencyKind == "TtlCounterDataName" && d.Via == null);
            impact.Dependents.Should().ContainSingle(d => d.Depth == 2 && d.Dependent.Name == "ManyPerAccount" &&
                                                          d.DependencyKind == "RuleText" &&
                                                          d.Name == "TTLCounter.PerAccount" &&
                                                          d.Via == "TtlCounter:PerAccount");
        }

        [Fact]
        public async Task AnEntityNothingUsesHasNoDependentsAndCanBeDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var impact = await service.DependentsAsync(modelId, "list", unusedListId);

            impact.Dependents.Should().BeEmpty();
            impact.DeleteBlocked.Should().BeFalse();
        }

        [Fact]
        public async Task AnActivationRulesDependenciesResolveToTheEntitiesItNamesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var uses = await service.DependenciesAsync(modelId, "ActivationRule", activationRuleId);

            uses.Dependencies.Should().Contain(d => d.Name == "TTLCounter.PerAccount" && d.Target!.Id == ttlCounterId &&
                                                    d.Line == 0);
        }

        [Fact]
        public async Task IssuesFindDanglingNamesAndUnusedEntitiesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var issues = await service.IssuesAsync(modelId);

            issues.Dangling.Should().ContainSingle(d => d.Dependent.Name == "Dangling" &&
                                                        d.Name == "TTLCounter.Missing" && d.Target == null);
            issues.Unreferenced.Should().ContainSingle(e => e.Kind == "List" && e.Name == "Unused");
            issues.Unreferenced.Should().NotContain(e => e.Name == "PerAccount");
        }

        [Fact]
        public async Task AnUnknownKindOrIdComesBackAsAnErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            (await service.DependentsAsync(modelId, "Widget", 1)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "KindInvalid");
            (await service.DependentsAsync(modelId, "7", 1)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "KindInvalid");
            (await service.DependenciesAsync(modelId, "List", int.MaxValue)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "EntityNotFound");
        }

        [Fact]
        public async Task AnotherTenantsModelIsNotFoundAndNoPermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await (await ServiceAsync(dbContext, fx.Seed.UserTenantB)).IssuesAsync(modelId));
            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await (await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission)).IssuesAsync(modelId));
        }
    }
}