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
using Jube.Data.Repository;
using Jube.Service.EntityAnalysisModelList;
using Jube.Service.EntityAnalysisModelRequestXPath;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ListValidationException = Jube.Service.Exceptions.EntityAnalysisModelList.DtoValidationException;
using XPathValidationException = Jube.Service.Exceptions.EntityAnalysisModelRequestXPath.DtoValidationException;

namespace Jube.Test.Service.Dependency
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ModelEntityDeleteGuardTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<(int Id, Guid Guid)> createdModels = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var (id, guid) in createdModels)
            {
                var listIds = await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                    .Where(w => w.EntityAnalysisModelGuid == guid).Select(s => s.Id).ToListAsync();
                await dbContext.GetTable<EntityAnalysisModelListVersion>()
                    .Where(w => listIds.Contains(w.EntityAnalysisModelListId)).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                    .Where(w => w.EntityAnalysisModelGuid == guid).DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelGatewayRuleVersion>()
                    .Where(w => w.EntityAnalysisModelId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelGatewayRule>()
                    .Where(w => w.EntityAnalysisModelId == id).DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelTtlCounterVersion>()
                    .Where(w => w.EntityAnalysisModelId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelTtlCounter>()
                    .Where(w => w.EntityAnalysisModelId == id).DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                    .Where(w => w.EntityAnalysisModelId == id).DeleteAsync();
                await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == id)
                    .DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == id)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private async Task<(int Id, Guid Guid)> CreateModelAsync(DbContext dbContext)
        {
            var saved = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });

            createdModels.Add((saved.Id, saved.Guid));
            return (saved.Id, saved.Guid);
        }

        private static Task<int> AddFieldAsync(DbContext dbContext, int modelId, string name)
        {
            return dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId, Name = name, DataTypeId = 1, XPath = $"$.{name}", Active = 1,
                Cache = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
        }

        private static Task<int> AddListAsync(DbContext dbContext, Guid modelGuid, string name)
        {
            return dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelList
            {
                EntityAnalysisModelGuid = modelGuid, Name = name, Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
        }

        private static Task<int> AddGatewayRuleAsync(DbContext dbContext, int modelId, string name, string text)
        {
            return dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelGatewayRule
            {
                EntityAnalysisModelId = modelId, Name = name, BuilderRuleScript = text, RuleScriptTypeId = 1,
                Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });
        }

        private Task<EntityAnalysisModelListService> ListServiceAsync(DbContext dbContext)
        {
            return EntityAnalysisModelListService.CreateAsync(dbContext, fx.Seed.UserWithPermission, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), TestLog.NoOp);
        }

        private Task<EntityAnalysisModelRequestXPathService> XPathServiceAsync(DbContext dbContext)
        {
            return EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp);
        }

        [Fact]
        public async Task AListUsedInRuleTextCannotBeDeletedAndTheRuleIsNamedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateModelAsync(dbContext);
            await AddFieldAsync(dbContext, modelId, "Country");
            var listId = await AddListAsync(dbContext, modelGuid, "HighRisk");
            await AddGatewayRuleAsync(dbContext, modelId, "UsesList",
                "If (List.HighRisk.Contains(Payload.Country)) Then\n   Return True\nEnd If");
            var service = await ListServiceAsync(dbContext);

            var ex = await Assert.ThrowsAsync<ListValidationException>(() => service.DeleteAsync(listId));

            ex.Result.Errors.Should().ContainSingle().Which.Should()
                .Match<FluentValidation.Results.ValidationFailure>(e =>
                    e.ErrorCode == "HasDependents" && e.ErrorMessage.Contains("UsesList") &&
                    e.ErrorMessage.Contains("List.HighRisk"));
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>().SingleAsync(w => w.Id == listId)).Deleted
                .Should().NotBe(1);
        }

        [Fact]
        public async Task AListNothingUsesIsDeletedAsBeforeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateModelAsync(dbContext);
            var listId = await AddListAsync(dbContext, modelGuid, "Unused");
            var service = await ListServiceAsync(dbContext);

            await service.DeleteAsync(listId);

            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>().SingleAsync(w => w.Id == listId)).Deleted
                .Should().Be(1);
        }

        [Fact]
        public async Task ADeletedDependentNoLongerBlocksAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateModelAsync(dbContext);
            await AddFieldAsync(dbContext, modelId, "Country");
            var listId = await AddListAsync(dbContext, modelGuid, "HighRisk");
            var ruleId = await AddGatewayRuleAsync(dbContext, modelId, "UsesList",
                "If (List.HighRisk.Contains(Payload.Country)) Then\n   Return True\nEnd If");
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelGatewayRule>().Where(w => w.Id == ruleId)
                .Set(s => s.Deleted, (byte)1).UpdateAsync();
            var service = await ListServiceAsync(dbContext);

            await service.DeleteAsync(listId);
        }

        [Fact]
        public async Task AFieldATtlCounterAggregatesOnCannotBeDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, _) = await CreateModelAsync(dbContext);
            var fieldId = await AddFieldAsync(dbContext, modelId, "AccountId");
            await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId, Name = "PerAccount", TtlCounterDataName = "AccountId", Active = 1,
                Deleted = 0, Guid = Guid.NewGuid()
            });
            var service = await XPathServiceAsync(dbContext);

            var ex = await Assert.ThrowsAsync<XPathValidationException>(() => service.DeleteAsync(fieldId));

            ex.Result.Errors.Should().ContainSingle(e =>
                e.ErrorCode == "HasDependents" && e.ErrorMessage.Contains("PerAccount") &&
                e.ErrorMessage.Contains("TtlCounterDataName"));
        }
    }
}