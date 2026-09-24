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
using Jube.Dto.EntityAnalysisModelAbstractionRule;
using Jube.Service.EntityAnalysisModelAbstractionRule;
using Jube.Service.Exceptions.EntityAnalysisModelAbstractionRule;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.EntityAnalysisModelAbstractionRule
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelAbstractionRuleValidateTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.GetTable<EntityAnalysisModelAbstractionRuleVersion>()
                    .Where(w => w.EntityAnalysisModelAbstractionRuleId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelAbstractionRule>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelAbstractionRuleService> BuildServiceAsync(DbContext dbContext,
            string userName)
        {
            return EntityAnalysisModelAbstractionRuleService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), TestLog.NoOp);
        }

        private async Task<int> CreateParentModelAsync(DbContext dbContext)
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

            createdModelIds.Add(saved.Id);
            return saved.Id;
        }

        private static Task<int> AddFieldAsync(DbContext dbContext, int entityAnalysisModelId, string name,
            bool cache)
        {
            return dbContext.InsertAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Name = name,
                DataTypeId = 3,
                XPath = $"$.{name}",
                Active = 1,
                Cache = (byte)(cache ? 1 : 0),
                Deleted = 0,
                Guid = Guid.NewGuid()
            });
        }

        private static EntityAnalysisModelAbstractionRuleDto NewDto(int entityAnalysisModelId, string name)
        {
            return new EntityAnalysisModelAbstractionRuleDto
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Name = name,
                BuilderRuleScript = "If (Payload.CurrencyAmount > 0) Then\n   Return True\nEnd If",
                Json = "{\"valid\":true,\"condition\":\"AND\",\"rules\":[]}",
                CoderRuleScript = "Return True",
                RuleScriptTypeId = 1,
                Search = true,
                SearchKey = "IP",
                SearchValue = 1,
                SearchInterval = "h",
                SearchFunctionTypeId = 1,
                Offset = false,
                OffsetTypeId = 1,
                OffsetValue = 0
            };
        }

        private static string UniqueName(string label)
        {
            return $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];
        }

        [Fact]
        public async Task AValidRuleIsReportedValidAndNothingIsStoredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            await AddFieldAsync(dbContext, modelId, "CurrencyAmount", true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = UniqueName("Valid");

            var result = await service.ValidateAsync(NewDto(modelId, name));

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelAbstractionRule>().CountAsync(w => w.Name == name))
                .Should().Be(0);
        }

        [Fact]
        public async Task EveryFailureIsReturnedWithItsPropertyAndCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, "");
            dto.SearchInterval = "x";
            dto.RuleScriptTypeId = 9;

            var result = await service.ValidateAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => (e.PropertyName, e.ErrorCode)).Should().Contain([
                ("Name", "NameNotEmpty"), ("SearchInterval", "SearchIntervalInvalid"),
                ("RuleScriptTypeId", "RuleScriptTypeIdInvalid")
            ]);
            result.Errors.Should().OnlyContain(e => e.Message.Length > 0 && e.Line == null);
        }

        [Fact]
        public async Task AnIdOfZeroValidatesAsACreateSoAnExistingNameIsADuplicateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            await AddFieldAsync(dbContext, modelId, "CurrencyAmount", true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Dup")));
            createdIds.Add(saved.Id);

            var asCreate = await service.ValidateAsync(NewDto(modelId, saved.Name!));
            var asUpdate = NewDto(modelId, saved.Name!);
            asUpdate.Id = saved.Id;

            asCreate.Errors.Should().ContainSingle(e => e.ErrorCode == "NameDuplicate");
            (await service.ValidateAsync(asUpdate)).IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task AUserWithoutPermissionIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.ValidateAsync(NewDto(modelId, UniqueName("Forbidden"))));
        }

        [Fact]
        public async Task ANullModelIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.ValidateAsync(null));
        }

        [Fact]
        public async Task ParseRuleAcceptsARuleOverTheModelsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            await AddFieldAsync(dbContext, modelId, "CurrencyAmount", true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ParseRuleAsync(NewDto(modelId, UniqueName("Parse")));

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ParseRuleLocatesAnUnknownFieldInTheBuilderTextAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ParseRuleAsync(NewDto(modelId, UniqueName("Unknown")));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty().And.OnlyContain(e =>
                e.PropertyName == "BuilderRuleScript" && e.ErrorCode == "RuleScriptInvalid" && e.Line != null);
        }

        [Fact]
        public async Task ParseRuleIgnoresFailuresOutsideTheRuleTextAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            await AddFieldAsync(dbContext, modelId, "CurrencyAmount", true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, "");
            dto.SearchInterval = "x";

            (await service.ParseRuleAsync(dto)).IsValid.Should().BeTrue();
            (await service.ValidateAsync(dto)).Errors.Select(e => e.ErrorCode).Should()
                .Contain("NameNotEmpty").And.Contain("SearchIntervalInvalid").And.NotContain("RuleScriptInvalid");
        }

        [Fact]
        public async Task ValidateReportsRuleTextFailuresAlongsideTheOthersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, "");

            var result = await service.ValidateAsync(dto);

            result.Errors.Select(e => e.ErrorCode).Should().Contain(["NameNotEmpty", "RuleScriptInvalid"]);
        }

        [Fact]
        public async Task OnlyBuilderAbstractionRulesAreRestrictedToCachedFieldsLikeTheEngineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            await AddFieldAsync(dbContext, modelId, "CurrencyAmount", false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var builder = NewDto(modelId, UniqueName("Builder"));
            var coder = NewDto(modelId, UniqueName("Coder"));
            coder.RuleScriptTypeId = 2;
            coder.CoderRuleScript = builder.BuilderRuleScript;

            (await service.ParseRuleAsync(builder)).IsValid.Should().BeFalse();
            (await service.ParseRuleAsync(coder)).IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task SavingARuleWhoseTextDoesNotParseIsRefusedAndNothingIsStoredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = UniqueName("Gated");

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(NewDto(modelId, name)));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "RuleScriptInvalid" &&
                                                   e.PropertyName == "BuilderRuleScript");
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelAbstractionRule>().CountAsync(w => w.Name == name))
                .Should().Be(0);
        }

        [Fact]
        public async Task UpdatingARuleSoItsTextNoLongerParsesIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            await AddFieldAsync(dbContext, modelId, "CurrencyAmount", true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Upd")));
            createdIds.Add(saved.Id);
            var dto = NewDto(modelId, saved.Name!);
            dto.Id = saved.Id;
            dto.BuilderRuleScript = "If (Payload.Nope > 0) Then\n   Return True\nEnd If";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.UpdateAsync(dto));
        }
    }
}