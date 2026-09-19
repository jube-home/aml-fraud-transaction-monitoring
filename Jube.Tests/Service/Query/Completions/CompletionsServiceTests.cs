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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.Completions;
using Jube.Resources;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.Completions;
using Jube.Service.Query.Completions;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.Completions.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.Completions
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CompletionsServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdWorkflowIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.EntityAnalysisModelRequestXpath.Where(w =>
                    w.EntityAnalysisModelId != null && createdModelIds.Contains(w.EntityAnalysisModelId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CompletionsService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CompletionsService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<Scenario> CreateScenarioAsync(DbContext dbContext, string userName,
            string[] xpathNames, byte workflowDeleted = 0)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, userName).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                }).ConfigureAwait(false);
            createdModelIds.Add(model.Id);

            var dataTypes = new[] { 1, 2, 3, 4, 5 };
            for (var i = 0; i < xpathNames.Length; i++)
            {
                await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = model.Id,
                    Name = xpathNames[i],
                    DataTypeId = dataTypes[i % dataTypes.Length],
                    XPath = $"$.{xpathNames[i]}",
                    Active = 1, Locked = 0, Deleted = 0, Version = 1,
                    Guid = Guid.NewGuid(),
                    CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
                });
            }

            var workflowGuid = Guid.NewGuid();
            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = workflowDeleted,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            });
            createdWorkflowIds.Add(workflowId);

            return new Scenario(model.Id, workflowId, workflowGuid);
        }

        private static readonly string[] names = ["bInt", "aString"];

        private static void AssertMapped(List<CompletionDto> dtos)
        {
            dtos.Should().HaveCount(2);

            var first = dtos[0];
            first.Name.Should().Be("Payload.aString");
            first.Value.Should().Be("Payload.aString");
            first.Group.Should().Be("Payload");
            first.Score.Should().Be(1000);
            first.DataType.Should().Be("integer");
            first.Meta.Should().Be("Payload.aString:integer");
            first.Field.Should().Be("(\"Json\"-> 'payload' ->> 'aString')::int");
            first.XPath.Should().Be("payload.aString");

            var second = dtos[1];
            second.Name.Should().Be("Payload.bInt");
            second.DataType.Should().Be("string");
            second.Meta.Should().Be("Payload.bInt:string");
            second.Field.Should().Be("(\"Json\"-> 'payload' ->> 'bInt')");
            second.XPath.Should().Be("payload.bInt");
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
                CompletionsService.CreateAsync(dbContext, userName, log, localizers, new NullServiceChangeBus(),
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
        public async Task EveryOperationThrowsForbiddenWithItsOwnSpecsWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var byId = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByCaseWorkflowIdAsync(1));
            byId.Code.Should().Be("PermissionDenied");
            byId.RequiredSpecifications.Should().BeEquivalentTo([25, 17, 20]);

            var byGuid = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByCaseWorkflowGuidIncludingDeletedAsync(Guid.NewGuid()));
            byGuid.RequiredSpecifications.Should().BeEquivalentTo([1]);

            var byIdDeleted = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByCaseWorkflowIdIncludingDeletedAsync(1));
            byIdDeleted.RequiredSpecifications.Should().BeEquivalentTo([1]);

            var byModel = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByEntityAnalysisModelIdAsync(1));
            byModel.RequiredSpecifications.Should().BeEquivalentTo([16]);

            var byParse = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByEntityAnalysisModelIdParseTypeIdAsync(1, 1));
            byParse.RequiredSpecifications.Should().BeEquivalentTo([8, 10, 13, 14, 17, 20, 26]);
        }

        [Fact]
        public async Task GetByCaseWorkflowIdReturnsMappedCompletionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dtos = await service.GetByCaseWorkflowIdAsync(scenario.WorkflowId);

            AssertMapped(dtos);
        }

        [Fact]
        public async Task GetByCaseWorkflowGuidIncludingDeletedReturnsMappedCompletionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            AssertMapped(await service.GetByCaseWorkflowGuidIncludingDeletedAsync(scenario.WorkflowGuid));
        }

        [Fact]
        public async Task GetByCaseWorkflowIdIncludingDeletedReturnsMappedCompletionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            AssertMapped(await service.GetByCaseWorkflowIdIncludingDeletedAsync(scenario.WorkflowId));
        }

        [Fact]
        public async Task GetByEntityAnalysisModelIdReturnsMappedCompletionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            AssertMapped(await service.GetByEntityAnalysisModelIdAsync(scenario.ModelId));
        }

        [Fact]
        public async Task GetByEntityAnalysisModelIdParseTypeIdReturnsMappedCompletionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            AssertMapped(await service.GetByEntityAnalysisModelIdParseTypeIdAsync(scenario.ModelId, 1));
        }

        [Fact]
        public async Task ParseTypeIdZeroOrLessReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetByEntityAnalysisModelIdParseTypeIdAsync(scenario.ModelId, 0)).Should().BeEmpty();
        }

        [Fact]
        public async Task ModelWithNoXPathsReturnsEmptyListAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, []);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetByEntityAnalysisModelIdAsync(scenario.ModelId)).Should().BeEmpty();
            (await service.GetByCaseWorkflowIdAsync(scenario.WorkflowId)).Should().BeEmpty();
        }

        [Fact]
        public async Task UnknownCaseWorkflowThrowsCaseWorkflowNotFoundOnEveryWorkflowRouteAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var ex = await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                service.GetByCaseWorkflowIdAsync(int.MaxValue));
            ex.Code.Should().Be("CaseWorkflowNotFound");
            await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                service.GetByCaseWorkflowIdIncludingDeletedAsync(int.MaxValue));
            await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                service.GetByCaseWorkflowGuidIncludingDeletedAsync(Guid.NewGuid()));

            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task DeletedWorkflowIsHiddenByIdButVisibleIncludingDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names,
                workflowDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                service.GetByCaseWorkflowIdAsync(scenario.WorkflowId));

            AssertMapped(await service.GetByCaseWorkflowIdIncludingDeletedAsync(scenario.WorkflowId));
            AssertMapped(await service.GetByCaseWorkflowGuidIncludingDeletedAsync(scenario.WorkflowGuid));
        }

        [Fact]
        public async Task TenantBCannotSeeTenantACompletionsAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var a = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var b = await CreateScenarioAsync(dbContext, fx.Seed.UserTenantB, ["cOnlyB"]);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                serviceB.GetByCaseWorkflowIdAsync(a.WorkflowId));
            await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                serviceB.GetByCaseWorkflowIdIncludingDeletedAsync(a.WorkflowId));
            await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                serviceB.GetByCaseWorkflowGuidIncludingDeletedAsync(a.WorkflowGuid));
            await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                serviceA.GetByCaseWorkflowIdAsync(b.WorkflowId));

            (await serviceB.GetByEntityAnalysisModelIdAsync(a.ModelId)).Should().BeEmpty();
            (await serviceB.GetByEntityAnalysisModelIdParseTypeIdAsync(a.ModelId, 1)).Should().BeEmpty();
            (await serviceA.GetByEntityAnalysisModelIdAsync(b.ModelId)).Should().BeEmpty();

            (await serviceB.GetByEntityAnalysisModelIdAsync(b.ModelId)).Should().ContainSingle()
                .Which.Name.Should().Be("Payload.cOnlyB");
            (await serviceA.GetByEntityAnalysisModelIdAsync(a.ModelId)).Should().HaveCount(2);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.GetByEntityAnalysisModelIdAsync(scenario.ModelId, cts.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.GetByCaseWorkflowIdAsync(scenario.WorkflowId, cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByEntityAnalysisModelIdAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetByEntityAnalysisModelIdAsync(1));

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLinePerCallAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetByCaseWorkflowIdAsync(scenario.WorkflowId);

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=GetByCaseWorkflowId");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, names);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);
            var forbiddenService = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetByEntityAnalysisModelIdAsync(scenario.ModelId);
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                forbiddenService.GetByEntityAnalysisModelIdAsync(scenario.ModelId));
            await Assert.ThrowsAsync<CaseWorkflowNotFoundException>(() =>
                service.GetByCaseWorkflowIdAsync(int.MaxValue));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var toolNames = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            toolNames.Should().OnlyHaveUniqueItems();
            toolNames.Should().OnlyContain(n => !n.Contains('_'));
            toolNames.Should().Contain([
                "CompletionsGetByCaseWorkflowId", "CompletionsGetByCaseWorkflowGuidIncludingDeleted",
                "CompletionsGetByCaseWorkflowIdIncludingDeleted", "CompletionsGetByEntityAnalysisModelId",
                "CompletionsGetByEntityAnalysisModelIdParseTypeId"
            ]);
        }

        [Fact]
        public async Task PermissionDeniedMessageResolvesForFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByEntityAnalysisModelIdAsync(1));

                var frenchLocalizer = localizers.Create(typeof(CompletionsResources));
                ex.Message.Should().Be(frenchLocalizer[CompletionsResources.PermissionDenied].Value);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}