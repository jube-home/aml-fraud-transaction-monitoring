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
using Jube.Engine.Integrity;
using Jube.Service.Exceptions.Query.EntityAnalysisModelIntegrity;
using Jube.Service.Query.EntityAnalysisModelIntegrity;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;
using Jube.Test.Service.Query.EntityAnalysisModelIntegrity.Models;

namespace Jube.Test.Service.Query.EntityAnalysisModelIntegrity
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelIntegrityServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly string instance = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}"[..30];
        private int accountIdField;
        private int activationRuleId;
        private Guid modelGuid;
        private int modelId;
        private int tenantRegistryId;
        private int ttlCounterId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Int{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0, EnableTtlCounter = 0
                });
            (modelId, modelGuid, tenantRegistryId) = (model.Id, model.Guid, model.TenantRegistryId!.Value);

            accountIdField = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId, Name = "AccountId", DataTypeId = 1, XPath = "$.AccountId",
                Active = 1, Cache = 1, Deleted = 0, Guid = Guid.NewGuid(), SearchKey = 1,
                SearchKeyTtlInterval = "h", SearchKeyTtlIntervalValue = 12
            });
            ttlCounterId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId, Name = "PerAccount", TtlCounterDataName = "AccountId",
                TtlCounterInterval = "d", TtlCounterValue = 1, Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
            activationRuleId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelActivationRule
                {
                    EntityAnalysisModelId = modelId, Name = "Many", RuleScriptTypeId = 2,
                    CoderRuleScript = "If (TTLCounter.PerAccount > 3 AND TTLCounter.Missing > 1) Then\n" +
                                      "   Return True\nEnd If",
                    Active = 1, Deleted = 0, Guid = Guid.NewGuid()
                });
            await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelGatewayRule
            {
                EntityAnalysisModelId = modelId, Name = "Broken", RuleScriptTypeId = 2,
                CoderRuleScript = "If (Payload.AccountId = \"1\") Then\n   Return True\nEnd If", Active = 1,
                Deleted = 0, Guid = Guid.NewGuid(), Compiled = 0, CompileError = "BC30451: boom"
            });
            await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelAbstractionRule
            {
                EntityAnalysisModelId = modelId, Name = "CountWeek", Search = 1, SearchKey = "AccountId",
                SearchInterval = "d", SearchValue = 7, SearchFunctionTypeId = 1, RuleScriptTypeId = 2,
                CoderRuleScript = "Return True", Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
            await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelList
            {
                EntityAnalysisModelGuid = modelGuid, Name = "Unused", Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
            await dbContext.InsertAsync(new EntityAnalysisModelSynchronisationNodeStatusEntry
            {
                Instance = instance, TenantRegistryId = tenantRegistryId, HeartbeatDate = DateTime.UtcNow,
                SynchronisedDate = DateTime.UtcNow
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelEngineSnapshot.Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry.Where(w => w.Instance == instance)
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuid).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelAbstractionRule>()
                .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelGatewayRule>()
                .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
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

        private Task<EntityAnalysisModelIntegrityService> ServiceAsync(Data.Context.DbContext dbContext,
            IEngineStateSource? source = null, string? userName = null)
        {
            return EntityAnalysisModelIntegrityService.CreateAsync(dbContext, source,
                userName ?? fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());
        }

        private EngineModelState State(string node, bool started, params EngineLoadedEntity[] loaded)
        {
            return new EngineModelState(node, modelId, tenantRegistryId, started, DateTime.UtcNow, loaded);
        }

        [Fact]
        public async Task TheCheckFindsTheModelsProblemsErrorsFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var report = await service.CheckAsync(modelId);

            report.EntityAnalysisModelId.Should().Be(modelId);
            report.EngineSource.Should().Be("Unavailable");
            var codes = report.Checks.Select(c => c.Code).ToList();
            codes.Should().Contain([
                nameof(IntegrityCode.DependencyDangling), nameof(IntegrityCode.EngineCompileFailed),
                nameof(IntegrityCode.TtlCountersDisabled), nameof(IntegrityCode.SearchKeyTtlShortensWindow),
                nameof(IntegrityCode.EntityUnreferenced), nameof(IntegrityCode.EngineStateUnavailable)
            ]);
            report.Checks.Single(c => c.Code == nameof(IntegrityCode.EngineCompileFailed)).EntityName.Should()
                .Be("Broken");
            report.Checks.Select(c => c.Severity == "Error" ? 0 : c.Severity == "Warning" ? 1 : 2).Should()
                .BeInAscendingOrder();
            report.Errors.Should().Be(report.Checks.Count(c => c.Severity == "Error"));
        }

        [Fact]
        public async Task ARecordedSnapshotIsComparedWithTheActiveEntitiesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await new EntityAnalysisModelEngineSnapshotRepository(dbContext).UpsertAsync(instance, tenantRegistryId,
                modelId, JsonConvert.SerializeObject(State(instance, true,
                    new EngineLoadedEntity("RequestXPath", accountIdField, "AccountId"),
                    new EngineLoadedEntity("TtlCounter", ttlCounterId, "PerAccount"),
                    new EngineLoadedEntity("GatewayRule", -1, "Removed"))));
            var service = await ServiceAsync(dbContext);

            var report = await service.CheckAsync(modelId);
            var engine = await service.EngineStateAsync(modelId);

            report.EngineSource.Should().Be("Snapshot");
            report.Checks.Should().Contain(c => c.Code == nameof(IntegrityCode.EngineEntityNotLoaded) &&
                                                c.EntityId == activationRuleId);
            report.Checks.Should().Contain(c => c.Code == nameof(IntegrityCode.EngineEntityStale) &&
                                                c.EntityName == "Removed");
            engine.Source.Should().Be("Snapshot");
            engine.Nodes.Should().Contain(n => n.Instance == instance);
            engine.Instances.Should().ContainSingle(i => i.Instance == instance && i.Started).Which.Loaded
                .Select(l => (l.Kind, l.Count)).Should().BeEquivalentTo(new[]
                    { ("GatewayRule", 1), ("RequestXPath", 1), ("TtlCounter", 1) });
        }

        [Fact]
        public async Task UpsertingASnapshotReplacesTheInstancesPreviousOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelEngineSnapshotRepository(dbContext);

            await repository.UpsertAsync(instance, tenantRegistryId, modelId, JsonConvert.SerializeObject(
                State(instance, false)));
            await repository.UpsertAsync(instance, tenantRegistryId, modelId, JsonConvert.SerializeObject(
                State(instance, true)));

            var stored = await repository.GetByEntityAnalysisModelIdAsync(tenantRegistryId, modelId);
            stored.Should().ContainSingle();
            JsonConvert.DeserializeObject<EngineModelState>(stored[0].Json)!.Started.Should().BeTrue();
        }

        [Fact]
        public async Task AnInProcessEngineIsPreferredToItsOwnSnapshotAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await new EntityAnalysisModelEngineSnapshotRepository(dbContext).UpsertAsync(instance, tenantRegistryId,
                modelId, JsonConvert.SerializeObject(State(instance, false)));
            var source = new FakeEngineStateSource(instance, State(instance, true));
            var service = await ServiceAsync(dbContext, source);

            var engine = await service.EngineStateAsync(modelId);
            var report = await service.CheckAsync(modelId);

            engine.Source.Should().Be("InProcess");
            engine.Instances.Should().ContainSingle().Which.Started.Should().BeTrue();
            report.Checks.Should().NotContain(c => c.Code == nameof(IntegrityCode.EngineModelNotStarted));
        }

        [Fact]
        public async Task TheGraphCanBeCentredOnAnEntityAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var whole = await service.GraphAsync(modelId);
            var focused = await service.GraphAsync(modelId, "TtlCounter", ttlCounterId, 1);

            whole.Nodes.Should().Contain(n => n.Id == "Missing:TTLCounter.Missing");
            whole.MaxNodes.Should().Be(150);
            focused.Focus.Should().Be($"TtlCounter:{ttlCounterId}");
            focused.Nodes.Select(n => n.Id).Should().BeEquivalentTo($"TtlCounter:{ttlCounterId}",
                $"RequestXPath:{accountIdField}", $"ActivationRule:{activationRuleId}");
            await Assert.ThrowsAsync<NotFoundException>(() => service.GraphAsync(modelId, "TtlCounter", -5));
        }

        [Fact]
        public async Task ModelsListTheTenantsModelsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            (await service.ModelsAsync()).Should().Contain(m => m.Id == modelId && m.Active);
        }

        [Fact]
        public async Task AnotherTenantsModelIsNotFoundAndNoPermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await (await ServiceAsync(dbContext, userName: fx.Seed.UserTenantB)).CheckAsync(modelId));
            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await (await ServiceAsync(dbContext, userName: fx.Seed.UserWithoutPermission)).ModelsAsync());
        }
    }
}