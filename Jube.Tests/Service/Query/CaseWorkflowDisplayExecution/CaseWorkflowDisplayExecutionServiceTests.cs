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
using Jube.Dto.Query.CaseWorkflowDisplayExecution;
using Jube.Engine.Helpers;
using Jube.Resources;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseWorkflowDisplayExecution;
using Jube.Service.Query.CaseWorkflowDisplayExecution;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.CaseWorkflowDisplayExecution.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.CaseWorkflowDisplayExecution
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowDisplayExecutionServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string CaseJson = "{\"payload\":{\"Amount\":\"12.5\",\"Currency\":\"GBP\"}}";
        private const string Html = "<b>[@Payload.Amount@] [@Payload.Currency@] [@Payload.Missing@]</b>";
        private const string RenderedHtml = "<b>12.5 GBP </b>";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly JsonSerializationHelper jsonSerializationHelper = new();

        private static readonly Jube.DynamicEnvironment.DynamicEnvironment dynamicEnvironment =
            TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                       ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty,
                ["ReportConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                             ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty
            });
        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdDisplayIds = [];
        private readonly List<int> createdFormIds = [];
        private readonly List<int> createdEntryIds = [];
        private readonly List<int> createdModelIds = [];
        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseWorkflowDispatch>()
                .Where(w => w.CaseId != null && createdCaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowFormEntryValue>()
                .Where(w => w.CaseWorkflowFormEntryId != null &&
                            createdEntryIds.Contains(w.CaseWorkflowFormEntryId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowFormEntry.Where(w => createdEntryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();

            var displayGuids = dbContext.CaseWorkflowDisplay.Where(s => createdDisplayIds.Contains(s.Id))
                .Select(s => s.Guid);
            var statusGuids = dbContext.CaseWorkflowStatus.Where(s => createdCaseWorkflowStatusIds.Contains(s.Id))
                .Select(s => s.Guid);
            var workflowGuids = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);

            var formGuids = dbContext.CaseWorkflowForm.Where(s => createdFormIds.Contains(s.Id)).Select(s => s.Guid);

            await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>()
                .Where(w => formGuids.Contains(w.CaseWorkflowFormGuid)).DeleteAsync();
            await dbContext.CaseWorkflowForm.Where(w => createdFormIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowDisplayRole>()
                .Where(w => displayGuids.Contains(w.CaseWorkflowDisplayGuid)).DeleteAsync();

            await dbContext.CaseWorkflowDisplay.Where(w => createdDisplayIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => statusGuids.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => workflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();

            await dbContext.CaseWorkflowStatus.Where(w => createdCaseWorkflowStatusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();

            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowDisplayExecutionService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowDisplayExecutionService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), dynamicEnvironment, jsonSerializationHelper,
                auditLog ?? TestLog.NoOp);
        }

        private async Task<Scenario> CreateScenarioAsync(DbContext dbContext, string userName,
            bool grantWorkflowRole = true, bool grantStatusRole = true, bool grantDisplayRole = true,
            byte displayActive = 1, byte displayDeleted = 0, byte workflowDeleted = 0, byte statusDeleted = 0,
            string json = CaseJson, string html = Html, string? displaySql = null)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, userName).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                }).ConfigureAwait(false);
            createdModelIds.Add(model.Id);

            var callerRoleGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            var unrelatedRoleGuid = Guid.NewGuid();

            var workflowGuid = Guid.NewGuid();

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                VisualisationRegistryGuid = Guid.NewGuid(),
                EnableVisualisation = 1,
                Active = 1,
                Locked = 0,
                Deleted = workflowDeleted,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowIds.Add(workflowId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = workflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = grantWorkflowRole ? callerRoleGuid : unrelatedRoleGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            var statusGuid = Guid.NewGuid();

            var statusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                Guid = statusGuid,
                CaseWorkflowId = workflowId,
                ForeColor = "#112233",
                BackColor = "#445566",
                Active = 1,
                Locked = 0,
                Deleted = statusDeleted,
                Priority = 1,
                EnableNotification = 0,
                EnableHttpEndpoint = 0,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(statusId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = statusGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = grantStatusRole ? callerRoleGuid : unrelatedRoleGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            var displayGuid = Guid.NewGuid();

            var displayId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowDisplay
            {
                Name = $"{DatabaseFixture.Prefix}Display{Guid.NewGuid():N}"[..40],
                Guid = displayGuid,
                CaseWorkflowId = workflowId,
                Html = html,
                Sql = displaySql,
                Active = displayActive,
                Locked = 0,
                Deleted = displayDeleted,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdDisplayIds.Add(displayId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowDisplayRole
            {
                CaseWorkflowDisplayGuid = displayGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = grantDisplayRole ? callerRoleGuid : unrelatedRoleGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = "AccountId",
                CaseKeyValue = $"{DatabaseFixture.Prefix}Key",
                Json = json,
                CreatedDate = DateTime.UtcNow,
                DiaryDate = DateTime.UtcNow,
                Diary = 0,
                Locked = 0,
                ClosedStatusId = 0,
                Rating = 1,
                LastClosedStatus = 0,
                ClosedStatusMigrationDate = DateTime.UtcNow,
            });
            createdCaseIds.Add(caseId);

            return new Scenario(caseId, displayId);
        }

        private async Task<int> CreateFormAsync(DbContext dbContext, string userName, int caseId, string html,
            bool grantRole = true, string? sql = null)
        {
            var workflowGuid = await dbContext.Case.Where(c => c.Id == caseId).Select(c => c.CaseWorkflowGuid)
                .FirstAsync();
            var workflowId = await dbContext.CaseWorkflow.Where(w => w.Guid == workflowGuid).Select(w => w.Id)
                .FirstAsync();
            var callerRoleGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();
            var formGuid = Guid.NewGuid();

            var formId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowForm
            {
                Name = $"{DatabaseFixture.Prefix}Form{Guid.NewGuid():N}"[..40],
                Guid = formGuid,
                CaseWorkflowId = workflowId,
                Html = html,
                Sql = sql,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdFormIds.Add(formId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowFormRole
            {
                CaseWorkflowFormGuid = formGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = grantRole ? callerRoleGuid : Guid.NewGuid(),
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return formId;
        }

        private async Task<int> CreateEntryAsync(DbContext dbContext, string userName, int caseId, int formId,
            params (string Name, string Value)[] values)
        {
            return await CreateSnapshotEntryAsync(dbContext, userName, caseId, formId, null, null, null, values);
        }

        private async Task<int> CreateSnapshotEntryAsync(DbContext dbContext, string userName, int caseId, int formId,
            string? sql, string? template, string? rows, params (string Name, string Value)[] values)
        {
            var found = await dbContext.Case.FirstAsync(c => c.Id == caseId);

            var entryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowFormEntry
            {
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = found.CaseKey,
                CaseKeyValue = found.CaseKeyValue,
                Sql = sql,
                Template = template,
                Rows = rows,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            });
            createdEntryIds.Add(entryId);

            foreach (var (name, value) in values)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowFormEntryValue
                {
                    CaseWorkflowFormEntryId = entryId,
                    Name = name,
                    Value = value
                });
            }

            return entryId;
        }

        private static CaseWorkflowDisplayExecutionDto Req(Scenario s) =>
            new() { CaseId = s.CaseId, CaseWorkflowDisplayId = s.DisplayId };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowDisplayExecutionService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), dynamicEnvironment,
                    jsonSerializationHelper, TestLog.NoOp));

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
        public async Task ExecuteThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteAsync(Req(scenario)));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([1]);
        }

        [Fact]
        public async Task ExecuteReplacesPayloadTokensAndRendersUnknownOnesEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var html = await service.ExecuteAsync(Req(scenario));

            html.Should().Be(RenderedHtml);
        }

        [Fact]
        public async Task ExecuteReadsTheCaseRootAndRendersAFieldReadOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                html: "<p>[@ Case.CaseKey @] [@ Payload.Amount | number(1) @]</p><input name=\"a\">");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var html = await service.ExecuteAsync(Req(scenario));

            html.Should().StartWith("<p>AccountId 12.5</p>").And.Contain("disabled");
        }

        [Fact]
        public async Task ExecuteWithATemplateThatDoesNotCompileThrowsTemplateInvalidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                html: "<script>alert(1)</script>");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.ExecuteAsync(Req(scenario));

            var ex = (await act.Should().ThrowAsync<TemplateInvalidException>()).Which;

            ex.Code.Should().Be("TemplateInvalid");
            ex.Message.Should().Contain("script");
        }

        [Fact]
        public async Task RenderFormFillsTheValuesAndNamesTheFieldsShownAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var formId = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId,
                "<select name=\"reason\"><option value=\"other\">O</option><option value=\"fraud\">F</option></select><input v-if=\"Form.reason == 'other'\" name=\"detail\">");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var shown = await service.RenderFormAsync(new CaseWorkflowFormRenderDto
            {
                CaseId = scenario.CaseId,
                CaseWorkflowFormId = formId,
                Values = new Dictionary<string, string?> { ["reason"] = "other" }
            });

            shown.VisibleFields.Should().Equal("reason", "detail");
            shown.Html.Should().Contain("<option value=\"other\" selected>");
            shown.Fields.Select(f => f.Kind).Should().Equal("select", "text");
            shown.Fields[0].Options.Should().Equal("other", "fraud");

            var hidden = await service.RenderFormAsync(new CaseWorkflowFormRenderDto
            {
                CaseId = scenario.CaseId,
                CaseWorkflowFormId = formId,
                Values = new Dictionary<string, string?> { ["reason"] = "fraud" }
            });

            hidden.VisibleFields.Should().Equal("reason");
        }

        [Fact]
        public async Task RenderFormForAFormNotGrantedToCallersRoleThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var formId = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId,
                "<input name=\"a\">", false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.RenderFormAsync(new CaseWorkflowFormRenderDto
            {
                CaseId = scenario.CaseId,
                CaseWorkflowFormId = formId
            });

            (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("NotFound");
        }

        [Fact]
        public async Task RenderFormWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.RenderFormAsync(new CaseWorkflowFormRenderDto { CaseId = 1, CaseWorkflowFormId = 1 });

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.Code.Should().Be("PermissionDenied");
        }

        [Fact]
        public async Task RenderFormEntriesDrawsEachEntryReadOnlyWithItsValuesAndTheValuesItHasNoFieldForAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var formId = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId,
                "<select name=\"reason\"><option value=\"other\">O</option><option value=\"fraud\">F</option></select><input name=\"detail\">");
            var entryId = await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId, formId,
                ("reason", "fraud"), ("detail", "<b>odd</b>"), ("retired", "old value"));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var views = await service.RenderFormEntriesAsync(new CaseWorkflowFormEntriesRenderDto
            {
                CaseId = scenario.CaseId
            });

            var view = views.Should().ContainSingle().Which;

            view.Id.Should().Be(entryId);
            view.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            view.Html.Should().Contain("<option value=\"fraud\" selected>")
                .And.Contain("value=\"&lt;b&gt;odd&lt;/b&gt;\"").And.Contain("disabled");
            view.OtherValues.Should().ContainSingle().Which.Name.Should().Be("retired");
            view.Error.Should().BeNull();
        }

        [Fact]
        public async Task RenderFormEntriesListsTheNewestEntryFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var formId = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId,
                "<input name=\"detail\">");
            var first = await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId, formId, ("detail", "one"));
            var second = await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId, formId, ("detail", "two"));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var views = await service.RenderFormEntriesAsync(new CaseWorkflowFormEntriesRenderDto
            {
                CaseId = scenario.CaseId
            });

            views.Select(v => v.Id).Should().Equal(second, first);
        }

        [Fact]
        public async Task RenderFormEntriesReportsATemplateThatNoLongerCompilesAndStillListsTheValuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var formId = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId,
                "<script>alert(1)</script><input name=\"detail\">");
            await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId, formId, ("detail", "kept"));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var views = await service.RenderFormEntriesAsync(new CaseWorkflowFormEntriesRenderDto
            {
                CaseId = scenario.CaseId
            });

            var view = views.Should().ContainSingle().Which;

            view.Html.Should().BeEmpty();
            view.Error.Should().Contain("script");
            view.OtherValues.Should().ContainSingle().Which.Value.Should().Be("kept");
        }

        [Fact]
        public async Task RenderFormEntriesSkipsAnEntryOfAFormTheCallersRoleDoesNotHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var formId = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId,
                "<input name=\"detail\">", false);
            await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId, formId, ("detail", "hidden"));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.RenderFormEntriesAsync(new CaseWorkflowFormEntriesRenderDto { CaseId = scenario.CaseId }))
                .Should().BeEmpty();
        }

        [Fact]
        public async Task RenderFormEntriesWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.RenderFormEntriesAsync(new CaseWorkflowFormEntriesRenderDto { CaseId = 1 });

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.Code.Should().Be("PermissionDenied");
        }

        [Fact]
        public async Task ExecuteDrawsADisplayFromTheRowsOfItsSqlAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                html: "<p>[@ rows[0].ckv @] [@ rowCount @]</p>", displaySql: "select \"CaseKeyValue\" as ckv, \"Id\" as id from \"Case\" where \"Id\" = @Id");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var html = await service.ExecuteAsync(Req(scenario));

            html.Should().Be($"<p>{DatabaseFixture.Prefix}Key 1</p>");
        }

        [Fact]
        public async Task ExecuteRefusesTheEarlierRootsInADisplayThatHasSqlAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                html: "<p>[@ Case.Id @]</p>", displaySql: "select \"CaseKeyValue\" as ckv, \"Id\" as id from \"Case\" where \"Id\" = @Id");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.ExecuteAsync(Req(scenario));

            await act.Should().ThrowAsync<TemplateInvalidException>();
        }

        [Fact]
        public async Task ExecuteReportsADisplaySqlThatFailsAsTemplateInvalidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                html: "<p>x</p>", displaySql: "select count(*) as n from \"UserRegistry\"");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.ExecuteAsync(Req(scenario));

            (await act.Should().ThrowAsync<TemplateInvalidException>()).Which.Message.Should()
                .Contain("could not be loaded");
        }

        [Fact]
        public async Task RenderFormDrawsAFormFromTheRowsOfItsSqlAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var formId = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId,
                "<input name=\"ref\" value=\"[@ rows[0].ckv @]\">", sql: "select \"CaseKeyValue\" as ckv, \"Id\" as id from \"Case\" where \"Id\" = @Id");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var drawn = await service.RenderFormAsync(new CaseWorkflowFormRenderDto
            {
                CaseId = scenario.CaseId, CaseWorkflowFormId = formId
            });

            drawn.Html.Should().Contain($"value=\"{DatabaseFixture.Prefix}Key\"");
            drawn.VisibleFields.Should().Equal("ref");
        }

        [Fact]
        public async Task RenderFormEntriesDrawsAnEntryFromItsSnapshotWhateverTheFormBecomesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var formId = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId,
                "<p>The form was redesigned</p><input name=\"other\">");
            await CreateSnapshotEntryAsync(dbContext, fx.Seed.UserWithPermission, scenario.CaseId, formId,
                "select 1 as n", "<p>Balance [@ rows[0].balance | number(2) @]</p><input name=\"detail\">",
                "[{\"balance\":1234.5}]", ("detail", "as completed"));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var view = (await service.RenderFormEntriesAsync(new CaseWorkflowFormEntriesRenderDto
            {
                CaseId = scenario.CaseId
            })).Should().ContainSingle().Which;

            view.Html.Should().Contain("Balance 1,234.50").And.Contain("value=\"as completed\"")
                .And.NotContain("redesigned");
            view.OtherValues.Should().BeEmpty();
        }

        [Fact]
        public async Task ListDispatchesReturnsTheCaseKeysCallsNewestFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var found = await dbContext.Case.FirstAsync(c => c.Id == scenario.CaseId);

            foreach (var name in new[] { "first", "second" })
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowDispatch
                {
                    CaseId = scenario.CaseId,
                    CaseKey = found.CaseKey,
                    CaseKeyValue = found.CaseKeyValue,
                    SourceKindId = 2,
                    SourceName = name,
                    ChannelId = 3,
                    ModeId = 2,
                    OutcomeId = 1,
                    CreatedUser = fx.Seed.UserWithPermission,
                    CreatedDate = DateTime.UtcNow
                });
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var listed = await service.ListDispatchesAsync(new CaseWorkflowDispatchesDto { CaseId = scenario.CaseId });

            listed.Select(d => d.SourceName).Should().Equal("second", "first");

            var stranger = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var act = () => stranger.ListDispatchesAsync(new CaseWorkflowDispatchesDto { CaseId = scenario.CaseId });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task TestSqlRunsForTheCaseAndReturnsTheColumnsAndRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.TestSqlAsync(new CaseWorkflowSqlTestDto
            {
                CaseId = scenario.CaseId,
                Sql = "select \"CaseKeyValue\" as ckv, @Id as id from \"Case\" where \"Id\" = @Id"
            });

            result.Ok.Should().BeTrue();
            result.Columns.Select(c => c.Name).Should().Equal("ckv", "id");
            result.RowCount.Should().Be(1);
            result.Rows.Single()["ckv"].Should().Be($"{DatabaseFixture.Prefix}Key");
        }

        [Fact]
        public async Task TestSqlRefusesBlockedTablesUnknownParametersAndWritesWithoutRaisingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            async Task<CaseWorkflowSqlTestResultDto> Try(string sql) =>
                await service.TestSqlAsync(new CaseWorkflowSqlTestDto { CaseId = scenario.CaseId, Sql = sql });

            (await Try("select count(*) from \"UserRegistry\"")).Ok.Should().BeFalse();
            (await Try("select @nothing")).Error.Should().Contain("@nothing");
            (await Try("delete from \"Case\" returning 1")).Ok.Should().BeFalse();
            (await Try("select 1/0 as boom from \"Case\" where \"Id\" = @Id")).Ok.Should().BeFalse();
        }

        [Fact]
        public async Task TestSqlNeedsTheSqlAuthoringPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var act = () => service.TestSqlAsync(new CaseWorkflowSqlTestDto { CaseId = scenario.CaseId, Sql = "select 1" });

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(67);
        }

        [Fact]
        public async Task TestSqlForACaseOutsideTheCallersTenantThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var stranger = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var act = () => stranger.TestSqlAsync(new CaseWorkflowSqlTestDto { CaseId = scenario.CaseId, Sql = "select 1" });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ExecuteUnknownCaseThrowsNotFoundAndLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(
                new CaseWorkflowDisplayExecutionDto
                    { CaseId = int.MaxValue, CaseWorkflowDisplayId = scenario.DisplayId }));

            ex.Code.Should().Be("NotFound");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task ExecuteUnknownDisplayThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(
                new CaseWorkflowDisplayExecutionDto
                    { CaseId = scenario.CaseId, CaseWorkflowDisplayId = int.MaxValue }));
        }

        [Fact]
        public async Task ExecuteWithSoftDeletedStatusThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, statusDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Req(scenario)));
        }

        [Fact]
        public async Task TenantBCannotRenderTenantACaseOrDisplayAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenarioA = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var scenarioB = await CreateScenarioAsync(dbContext, fx.Seed.UserTenantB);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<NotFoundException>(() => serviceB.ExecuteAsync(Req(scenarioA)));
            await Assert.ThrowsAsync<NotFoundException>(() => serviceA.ExecuteAsync(Req(scenarioB)));

            await Assert.ThrowsAsync<NotFoundException>(() => serviceB.ExecuteAsync(
                new CaseWorkflowDisplayExecutionDto
                    { CaseId = scenarioB.CaseId, CaseWorkflowDisplayId = scenarioA.DisplayId }));
            await Assert.ThrowsAsync<NotFoundException>(() => serviceA.ExecuteAsync(
                new CaseWorkflowDisplayExecutionDto
                    { CaseId = scenarioA.CaseId, CaseWorkflowDisplayId = scenarioB.DisplayId }));

            (await serviceA.ExecuteAsync(Req(scenarioA))).Should().Be(RenderedHtml);
            (await serviceB.ExecuteAsync(Req(scenarioB))).Should().Be(RenderedHtml);
        }

        [Fact]
        public async Task CaseWhoseWorkflowIsNotGrantedToCallersRoleIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                grantWorkflowRole: false);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Req(scenario)));
        }

        [Fact]
        public async Task CaseWhoseStatusIsNotGrantedToCallersRoleIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                grantStatusRole: false);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Req(scenario)));
        }

        [Fact]
        public async Task DisplayNotGrantedToCallersRoleIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                grantDisplayRole: false);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Req(scenario)));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        public async Task InactiveOrDeletedDisplayIsNotFoundAsync(byte active, byte deleted)
        {
            await using var dbContext = fx.GetDbContext();

            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                displayActive: active, displayDeleted: deleted);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Req(scenario)));
        }

        [Fact]
        public async Task CaseInDeletedWorkflowIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, workflowDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Req(scenario)));
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ExecuteAsync(Req(scenario), cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.ExecuteAsync(new CaseWorkflowDisplayExecutionDto()));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.ExecuteAsync(Req(scenario));

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Execute");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);
            var forbiddenService = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                serviceChangeBus: serviceChangeBus);

            await service.ExecuteAsync(Req(scenario));
            await Assert.ThrowsAsync<ForbiddenException>(() => forbiddenService.ExecuteAsync(Req(scenario)));

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(
                new CaseWorkflowDisplayExecutionDto
                    { CaseId = int.MaxValue, CaseWorkflowDisplayId = scenario.DisplayId }));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("CaseWorkflowDisplayExecutionExecute");
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

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                    service.ExecuteAsync(new CaseWorkflowDisplayExecutionDto()));

                var frenchLocalizer = localizers.Create(typeof(CaseWorkflowDisplayExecutionResources));
                ex.Message.Should().Be(frenchLocalizer[CaseWorkflowDisplayExecutionResources.PermissionDenied].Value);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}