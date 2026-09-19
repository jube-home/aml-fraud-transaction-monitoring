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
        private const string RenderedHtml = "<b>12.5 GBP [@Payload.Missing@]</b>";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly JsonSerializationHelper jsonSerializationHelper = new();

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdDisplayIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var displayGuids = dbContext.CaseWorkflowDisplay.Where(s => createdDisplayIds.Contains(s.Id))
                .Select(s => s.Guid);
            var statusGuids = dbContext.CaseWorkflowStatus.Where(s => createdCaseWorkflowStatusIds.Contains(s.Id))
                .Select(s => s.Guid);
            var workflowGuids = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
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
                serviceChangeBus ?? new NullServiceChangeBus(), jsonSerializationHelper,
                auditLog ?? TestLog.NoOp);
        }

        private async Task<Scenario> CreateScenarioAsync(DbContext dbContext, string userName,
            bool grantWorkflowRole = true, bool grantStatusRole = true, bool grantDisplayRole = true,
            byte displayActive = 1, byte displayDeleted = 0, byte workflowDeleted = 0, byte statusDeleted = 0,
            string json = CaseJson)
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
                Html = Html,
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
                    new NullServiceChangeBus(),
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
        public async Task ExecuteReplacesPayloadTokensAndLeavesUnknownTokensAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var html = await service.ExecuteAsync(Req(scenario));

            html.Should().Be(RenderedHtml);
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