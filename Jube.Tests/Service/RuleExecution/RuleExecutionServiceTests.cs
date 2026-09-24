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
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelAbstractionCalculation;
using Jube.Dto.EntityAnalysisModelGatewayRule;
using Jube.Dto.EntityAnalysisModelInlineFunction;
using Jube.Dto.EntityAnalysisModelInlineScript;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Dto.RuleExecution;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.EntityAnalysisModelAbstractionCalculation;
using Jube.Service.EntityAnalysisModelGatewayRule;
using Jube.Service.EntityAnalysisModelInlineFunction;
using Jube.Service.EntityAnalysisModelInlineScript;
using Jube.Service.Query.EntityAnalysisModelInvocationContext;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.RuleExecution
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RuleExecutionServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                    .Where(w => listIds.Contains(w.EntityAnalysisModelListId ?? 0)).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                    .Where(w => w.EntityAnalysisModelGuid == guid).DeleteAsync();
                await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == id)
                    .DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == id)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private async Task<int> CreateModelAsync(DbContext dbContext)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}ExModel{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0
                });
            createdModels.Add((model.Id, model.Guid));

            foreach (var (name, dataTypeId) in new[] { ("Amount", 3), ("Country", 1) })
            {
                await dbContext.InsertAsync(new EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = model.Id, Name = name, DataTypeId = dataTypeId, XPath = $"$.{name}",
                    Active = 1, Cache = 1, Deleted = 0, Guid = Guid.NewGuid()
                });
            }

            var listId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelList
            {
                EntityAnalysisModelGuid = model.Guid, Name = "HighRisk", Active = 1, Deleted = 0,
                Guid = Guid.NewGuid()
            });
            await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelListValue
            {
                EntityAnalysisModelListId = listId, ListValue = "IR", Deleted = 0, Guid = Guid.NewGuid()
            });

            return model.Id;
        }

        private async Task<InvocationContextDto> ContextAsync(DbContext dbContext, int modelId,
            Dictionary<string, string?> values)
        {
            var service = await EntityAnalysisModelInvocationContextService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());
            var context = await service.OverlayAsync(new InvocationContextOverlayDto
                { Context = await service.BlankAsync(modelId), Values = values });
            context.Errors.Should().BeEmpty();
            return context;
        }

        private Task<EntityAnalysisModelGatewayRuleService> GatewayServiceAsync(DbContext dbContext)
        {
            return EntityAnalysisModelGatewayRuleService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus());
        }

        private static EntityAnalysisModelGatewayRuleDto Gateway(int modelId, string text)
        {
            return new EntityAnalysisModelGatewayRuleDto
            {
                EntityAnalysisModelId = modelId, Name = "Probe", RuleScriptTypeId = 2, CoderRuleScript = text,
                BuilderRuleScript = "", Json = ""
            };
        }

        [Theory]
        [InlineData("150", "true")]
        [InlineData("50", "false")]
        public async Task AGatewayRuleRunsAgainstAContextBuiltByTheContextServiceAsync(string amount,
            string expected)
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var context = await ContextAsync(dbContext, modelId, new() { ["Payload.Amount"] = amount });
            var service = await GatewayServiceAsync(dbContext);

            var result = await service.ExecuteAsync(
                Gateway(modelId, "If (Payload.Amount > 100) Then\n   Return True\nEnd If"), context);

            result.Compiled.Should().BeTrue();
            result.Result.Should().Be(expected);
            result.ResultType.Should().Be("Boolean");
            result.NamesRead.Should().Equal("Payload.Amount");
            result.UnsetNamesRead.Should().BeEmpty();
        }

        [Fact]
        public async Task ListsAreReadFromTheModelAndUnsetNamesAreFlaggedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var context = await ContextAsync(dbContext, modelId, new() { ["Payload.Country"] = "IR" });
            var service = await GatewayServiceAsync(dbContext);

            var result = await service.ExecuteAsync(Gateway(modelId,
                    "If (List.HighRisk.Contains(Payload.Country) Or Payload.Amount > 1) Then\n   Return True\nEnd If"),
                context);

            result.Result.Should().Be("true");
            result.UnsetNamesRead.Should().Equal("Payload.Amount");
        }

        [Fact]
        public async Task ARuleThatDoesNotCompileReportsLocatedErrorsAndRunsNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var context = await ContextAsync(dbContext, modelId, new());
            var service = await GatewayServiceAsync(dbContext);

            var result = await service.ExecuteAsync(
                Gateway(modelId, "If (Payload.Missing > 1) Then\n   Return True\nEnd If"), context);

            result.Compiled.Should().BeFalse();
            result.Result.Should().BeNull();
            result.Errors.Should().NotBeEmpty().And.OnlyContain(e =>
                e.PropertyName == "CoderRuleScript" && e.ErrorCode == "RuleScriptInvalid");
        }

        [Fact]
        public async Task AContextForAnotherModelIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var otherModelId = await CreateModelAsync(dbContext);
            var context = await ContextAsync(dbContext, otherModelId, new());
            var service = await GatewayServiceAsync(dbContext);

            var result = await service.ExecuteAsync(Gateway(modelId, "Return True"), context);

            result.Errors.Should().ContainSingle(e => e.ErrorCode == "ContextModelMismatch");
        }

        [Fact]
        public async Task CalculationsAndInlineFunctionsReturnTheirValuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var context = await ContextAsync(dbContext, modelId,
                new() { ["Payload.Amount"] = "21", ["Payload.Country"] = "FR" });
            var calculations = await EntityAnalysisModelAbstractionCalculationService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());
            var functions = await EntityAnalysisModelInlineFunctionService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var calculation = await calculations.ExecuteAsync(new EntityAnalysisModelAbstractionCalculationDto
                    { EntityAnalysisModelId = modelId, Name = "Double", FunctionScript = "Return Payload.Amount * 2" },
                context);
            var function = await functions.ExecuteAsync(new EntityAnalysisModelInlineFunctionDto
            {
                EntityAnalysisModelId = modelId, Name = "Where", ReturnDataTypeId = 1,
                FunctionScript = "Return Payload.Country"
            }, context);

            calculation.Result.Should().Be("42");
            function.Result.Should().Be("FR");
        }

        [Theory]
        [InlineData("EntityAnalysisModelGatewayRule")]
        [InlineData("EntityAnalysisModelAbstractionRule")]
        [InlineData("EntityAnalysisModelAbstractionCalculation")]
        [InlineData("EntityAnalysisModelActivationRule")]
        [InlineData("EntityAnalysisModelInlineFunction")]
        [InlineData("EntityAnalysisModelReprocessingRule")]
        public void EveryVbNetRuleServiceExposesACataloguedExecute(string area)
        {
            var service = typeof(ServiceOperationAttribute).Assembly.GetTypes().Single(t => t.Name == area + "Service");
            var execute = service.GetMethod("ExecuteAsync");

            execute.Should().NotBeNull();
            execute!.ReturnType.Should().Be(typeof(Task<RuleExecutionResultDto>));
            var attribute = execute.GetCustomAttribute<ServiceOperationAttribute>()!;
            attribute.Name.Should().Be(area + "Execute");
            attribute.Kind.Should().Be(OperationKind.Read);
            ServiceToolCatalogue.All.Where(t => t.Name == attribute.Name).Should().ContainSingle();
        }

        [Fact]
        public async Task InlineScriptExecutionIsRefusedUnlessEnabledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var context = await ContextAsync(dbContext, modelId, new());
            var service = await EntityAnalysisModelInlineScriptService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var result = await service.ExecuteAsync(new EntityAnalysisModelInlineScriptDto
                { EntityAnalysisModelId = modelId, EntityAnalysisInlineScriptId = 1 }, context);

            result.Compiled.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorCode == "InlineScriptExecutionDisabled");
            ServiceToolCatalogue.All.Should().ContainSingle(t => t.Name == "EntityAnalysisModelInlineScriptExecute");
        }

        [Fact]
        public async Task BuilderJsonBecomesARuleThatSavesAndItsProblemsCarryTheirJsonPathAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var service = await GatewayServiceAsync(dbContext);
            const string json =
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"greater\"," +
                "\"value\":100},{\"id\":\"List.HighRisk\",\"operator\":\"has\",\"value\":\"Payload.Country\"}]}";

            var built = await service.BuildRuleFromBuilderJsonAsync(modelId, json);
            var bad = await service.BuildRuleFromBuilderJsonAsync(modelId,
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"begins_with\",\"value\":\"1\"}]}");

            built.Valid.Should().BeTrue(string.Join("; ", built.Errors.Select(e => e.Message)));
            built.RuleText.Should().Be(
                "If (Payload.Amount > 100 AND List.HighRisk.contains(Payload.Country)) Then\n  Return True\nEnd If");
            var saved = await service.InsertAsync(new EntityAnalysisModelGatewayRuleDto
            {
                EntityAnalysisModelId = modelId, Name = "Built", RuleScriptTypeId = 1,
                BuilderRuleScript = built.RuleText!,
                Json = json, Active = true, GatewaySample = 1
            });
            saved.Id.Should().BeGreaterThan(0);
            await dbContext.GetTable<EntityAnalysisModelGatewayRuleVersion>()
                .Where(w => w.EntityAnalysisModelGatewayRuleId == saved.Id).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelGatewayRule>().Where(w => w.Id == saved.Id)
                .DeleteAsync();
            bad.Valid.Should().BeFalse();
            bad.Errors.Should().ContainSingle(e =>
                e.ErrorCode == "OperatorInvalid" && e.PropertyName == "$.rules[0].operator");
        }
    }
}