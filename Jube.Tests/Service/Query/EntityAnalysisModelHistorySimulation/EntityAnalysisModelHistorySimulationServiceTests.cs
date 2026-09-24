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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dictionary;
using Jube.Dto.EntityAnalysisModelAbstractionRule;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Query.EntityAnalysisModelHistorySimulation;
using Jube.Service.Query.EntityAnalysisModelInvocationContext;
using Jube.Service.Reactivity;
using Jube.TaskCancellation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.EntityAnalysisModelHistorySimulation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelHistorySimulationServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string ReferenceDateName = "TxDate";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private CacheService cache = null!;
        private TaskCoordinator coordinator = null!;

        public Task InitializeAsync()
        {
            coordinator = new TaskCoordinator(new CancellationTokenProvider(), TestLog.NoOp);
            cache = new CacheService(
                Environment.GetEnvironmentVariable("JubeTestRedisConnectionString") ?? "localhost",
                Environment.GetEnvironmentVariable("JubeTestConnectionString") ?? string.Empty,
                new ConcurrentDictionary<Guid,
                    TaskCompletionSource<global::Jube.Cache.Redis.Callback.Callback>>(), 3000, false, false,
                50_000_000, false, false, false, false, TimeSpan.FromDays(1), false, TestLog.NoOp);
            return cache.StartAsync(coordinator);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            foreach (var id in createdModelIds)
            {
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelTtlCounter>()
                    .Where(w => w.EntityAnalysisModelId == id).DeleteAsync();
                await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == id)
                    .DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == id)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private async Task<(int Id, Guid Guid, int TenantId)> CreateModelAsync(DbContext dbContext)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}HsModel{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    ReferenceDateName = ReferenceDateName, CacheFetchLimit = 100, EnableTtlCounter = 1,
                    Active = 1, Locked = 0, Deleted = 0
                });
            createdModelIds.Add(model.Id);

            await dbContext.InsertAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = model.Id, Name = "AccountId", DataTypeId = 1, XPath = "$.AccountId",
                Active = 1, Cache = 1, CacheIndexId = 1, SearchKey = 1, SearchKeyCache = 0,
                SearchKeyTtlInterval = "d", SearchKeyTtlIntervalValue = 30, SearchKeyFetchLimit = 100, Deleted = 0,
                Guid = Guid.NewGuid()
            });
            await dbContext.InsertAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = model.Id, Name = "Amount", DataTypeId = 3, XPath = "$.Amount", Active = 1,
                Cache = 1, CacheIndexId = 2, Deleted = 0, Guid = Guid.NewGuid()
            });

            return (model.Id, model.Guid, model.TenantRegistryId!.Value);
        }

        private async Task SeedAsync(int tenantId, Guid modelGuid, string account, double amount, double hoursAgo)
        {
            var guid = Guid.NewGuid();
            var date = DateTime.UtcNow.AddHours(-hoursAgo);
            var payload = new DictionaryNoBoxing<int>();
            payload.TryAdd(-1, date);
            payload.TryAdd(1, account);
            payload.TryAdd(2, amount);

            await cache.CachePayloadRepository.InsertAsync(tenantId, modelGuid, payload, date, guid);
            await cache.CachePayloadRepository.InsertPayloadJournalAndLedgerAsync(tenantId, modelGuid, "AccountId",
                account, date, guid);
        }

        private async Task<InvocationContextDto> ContextAsync(DbContext dbContext, int modelId,
            Dictionary<string, string?> values)
        {
            var service = await EntityAnalysisModelInvocationContextService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());
            var context = await service.OverlayAsync(new InvocationContextOverlayDto
                { Context = await service.BlankAsync(modelId), Values = values });
            context.Errors.Should().BeEmpty();
            context.ReferenceDate = DateTime.UtcNow;
            return context;
        }

        private Task<EntityAnalysisModelHistorySimulationService> ServiceAsync(DbContext dbContext,
            CacheService? cacheService)
        {
            return EntityAnalysisModelHistorySimulationService.CreateAsync(dbContext, cacheService,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());
        }

        private static EntityAnalysisModelAbstractionRuleDto Rule(int modelId, int functionType)
        {
            return new EntityAnalysisModelAbstractionRuleDto
            {
                EntityAnalysisModelId = modelId, Name = "Velocity", RuleScriptTypeId = 2,
                CoderRuleScript = "If (Payload.Amount > 0) Then\n   Return True\nEnd If", BuilderRuleScript = "",
                Json = "", Search = true, SearchKey = "AccountId", SearchFunctionKey = "Amount",
                SearchFunctionTypeId = functionType, SearchInterval = "h", SearchValue = 24
            };
        }

        [Theory]
        [InlineData(1, "3")]
        [InlineData(3, "22")]
        public async Task TheAggregationOverCachedHistoryMatchesTheEngineAsync(int functionType, string expected)
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid, tenantId) = await CreateModelAsync(dbContext);
            await SeedAsync(tenantId, modelGuid, "A1", 5, 1);
            await SeedAsync(tenantId, modelGuid, "A1", 7, 2);
            await SeedAsync(tenantId, modelGuid, "A1", 100, 50);
            await SeedAsync(tenantId, modelGuid, "A2", 1000, 1);
            var context = await ContextAsync(dbContext, modelId,
                new() { ["Payload.AccountId"] = "A1", ["Payload.Amount"] = "10" });
            var service = await ServiceAsync(dbContext, cache);

            var result = await service.AbstractionAggregationAsync(Rule(modelId, functionType), context);

            result.Errors.Should().BeEmpty();
            result.Simulated.Should().BeTrue();
            result.SearchKeyValue.Should().Be("A1");
            result.DocumentsFetched.Should().Be(3);
            result.DocumentsEvaluated.Should().Be(4);
            result.InWindow.Should().Be(3);
            result.Value.Should().Be(expected);
            result.FetchLimit.Should().Be(100);
            result.Sample.Should().HaveCount(3);
        }

        [Fact]
        public async Task WithoutACacheTheSimulationSaysWhyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, _, _) = await CreateModelAsync(dbContext);
            var context = await ContextAsync(dbContext, modelId, new() { ["Payload.AccountId"] = "A1" });
            var service = await ServiceAsync(dbContext, null);

            var result = await service.AbstractionAggregationAsync(Rule(modelId, 1), context);

            result.Simulated.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorCode == "CacheUnavailable");
        }

        [Fact]
        public async Task ARuleThatDoesNotSearchAndAnUnsetSearchKeyAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, _, _) = await CreateModelAsync(dbContext);
            var context = await ContextAsync(dbContext, modelId, new());
            var service = await ServiceAsync(dbContext, cache);
            var notSearching = Rule(modelId, 1);
            notSearching.Search = false;

            (await service.AbstractionAggregationAsync(notSearching, context)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "NotASearchRule");
            (await service.AbstractionAggregationAsync(Rule(modelId, 1), context)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "SearchKeyValueUnset");
        }

        [Fact]
        public async Task ATtlCounterIsReadForTheContextsDataValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, _, _) = await CreateModelAsync(dbContext);
            var counterId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId, Name = "PerAccount", TtlCounterDataName = "AccountId",
                TtlCounterInterval = "d", TtlCounterValue = 1, OnlineAggregation = 0, Active = 1, Deleted = 0,
                Guid = Guid.NewGuid()
            });
            var service = await ServiceAsync(dbContext, cache);

            var unset = await service.TtlCounterValueAsync(counterId, await ContextAsync(dbContext, modelId, new()));
            var read = await service.TtlCounterValueAsync(counterId,
                await ContextAsync(dbContext, modelId, new() { ["Payload.AccountId"] = "Nobody" }));

            unset.Errors.Should().ContainSingle(e => e.ErrorCode == "DataValueUnset");
            read.Read.Should().BeTrue();
            read.Value.Should().Be("0");
            read.DataName.Should().Be("AccountId");
        }

        [Fact]
        public void TheCatalogueListsTheHistoryOperations()
        {
            ServiceToolCatalogue.All.Where(t => t.Name.StartsWith("EntityAnalysisModelHistorySimulation"))
                .Select(t => t.Name).Should().BeEquivalentTo([
                    "EntityAnalysisModelHistorySimulationAbstractionAggregation",
                    "EntityAnalysisModelHistorySimulationTtlCounterValue"
                ]);
        }
    }
}