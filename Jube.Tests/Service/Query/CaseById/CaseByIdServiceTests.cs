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
using Jube.Resources;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseById;
using Jube.Service.Query.CaseById;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.CaseById.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.CaseById
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseByIdServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private const string CaseJson =
            "{\"payload\":{\"Amount\":\"12.5\",\"Currency\":\"GBP\"},\"activation\":{\"RuleA\":{\"visible\":1},\"RuleB\":{\"visible\":0}}}";

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdCaseWorkflowXPathIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseEvent>()
                .Where(w => w.CaseId != null && createdCaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowXPathGuids = dbContext.CaseWorkflowXPath
                .Where(s => createdCaseWorkflowXPathIds.Contains(s.Id)).Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathRole>()
                .Where(w => caseWorkflowXPathGuids.Contains(w.CaseWorkflowXPathGuid)).DeleteAsync();
            await dbContext.CaseWorkflowXPath.Where(w => createdCaseWorkflowXPathIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowStatusGuids = dbContext.CaseWorkflowStatus
                .Where(s => createdCaseWorkflowStatusIds.Contains(s.Id)).Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => caseWorkflowStatusGuids.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            var caseWorkflowGuids = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => createdCaseWorkflowStatusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseByIdService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseByIdService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<Scenario> CreateScenarioAsync(DbContext dbContext, string userName,
            bool grantWorkflowRole = true, bool grantStatusRole = true, bool grantXPathRole = true,
            byte workflowDeleted = 0, string json = CaseJson, bool addXPaths = true)
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
            var visualisationGuid = Guid.NewGuid();

            var workflowGuid = Guid.NewGuid();
            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                VisualisationRegistryGuid = visualisationGuid,
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
                Deleted = 0,
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

            if (addXPaths)
            {
                await CreateXPathAsync(dbContext, userName, workflowId, "Amount", "$.payload.Amount",
                    grantXPathRole ? callerRoleGuid : unrelatedRoleGuid);
                await CreateXPathAsync(dbContext, userName, workflowId, "Missing", "$.payload.Nope",
                    callerRoleGuid);
            }

            var entryGuid = Guid.NewGuid();
            var created = DateTime.UtcNow.AddDays(-2);
            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = entryGuid,
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = "AccountId",
                CaseKeyValue = $"{DatabaseFixture.Prefix}Key",
                Json = json,
                CreatedDate = created,
                DiaryDate = created.AddDays(1),
                Diary = 1,
                DiaryUser = "diaryUser",
                Locked = 1,
                LockedUser = "lockUser",
                LockedDate = created.AddHours(1),
                ClosedStatusId = 2,
                ClosedUser = "closeUser",
                ClosedDate = created.AddHours(2),
                Rating = 7,
                LastClosedStatus = 3,
                ClosedStatusMigrationDate = created.AddHours(3),
            });
            createdCaseIds.Add(caseId);

            return new Scenario(caseId, workflowGuid, statusGuid, model.Id, entryGuid, visualisationGuid);
        }

        private async Task CreateXPathAsync(DbContext dbContext, string userName, int workflowId, string name,
            string xPath, Guid roleGuid)
        {
            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowXPath
            {
                CaseWorkflowId = workflowId,
                Guid = guid,
                Name = name,
                XPath = xPath,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                ConditionalRegularExpressionFormatting = 1,
                RegularExpression = "^12",
                ConditionalFormatForeColor = "#ff0000",
                ConditionalFormatBackColor = "#00ff00",
                ForeRowColorScope = 1,
                BackRowColorScope = 0,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowXPathIds.Add(id);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowXPathRole
            {
                CaseWorkflowXPathGuid = guid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });
        }

        private Task<List<Data.Poco.CaseEvent>> EventsForAsync(DbContext dbContext, int caseId) =>
            dbContext.GetTable<Data.Poco.CaseEvent>().Where(w => w.CaseId == caseId).ToListAsync();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseByIdService.CreateAsync(dbContext, userName, log, localizers, new NullServiceChangeBus(),
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
        public async Task GetThrowsForbiddenWhenPermissionMissingAndWritesNoEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(scenario.CaseId));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([1]);
            (await EventsForAsync(dbContext, scenario.CaseId)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetReturnsFullyMappedCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var poco = await dbContext.Case.FirstAsync(c => c.Id == scenario.CaseId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(scenario.CaseId);

            dto.Id.Should().Be(scenario.CaseId);
            dto.EntityAnalysisModelInstanceEntryGuid.Should().Be(scenario.EntryGuid);
            dto.CaseWorkflowGuid.Should().Be(scenario.WorkflowGuid);
            dto.CaseWorkflowStatusGuid.Should().Be(scenario.StatusGuid);
            dto.DiaryDate.UtcDateTime.Should().BeCloseTo(poco.DiaryDate.Required(), TimeSpan.FromSeconds(1));
            dto.CreatedDate.UtcDateTime.Should().BeCloseTo(poco.CreatedDate.Required(), TimeSpan.FromSeconds(1));
            dto.Locked.Should().BeTrue();
            dto.LockedUser.Should().Be("lockUser");
            dto.LockedDate.UtcDateTime.Should().BeCloseTo(poco.LockedDate.Required(), TimeSpan.FromSeconds(1));
            dto.ClosedStatusId.Should().Be(2);
            dto.ClosedUser.Should().Be("closeUser");
            dto.ClosedDate.UtcDateTime.Should().BeCloseTo(poco.ClosedDate.Required(), TimeSpan.FromSeconds(1));
            dto.CaseKey.Should().Be("AccountId");
            dto.CaseKeyValue.Should().Be($"{DatabaseFixture.Prefix}Key");
            dto.Diary.Should().BeTrue();
            dto.DiaryUser.Should().Be("diaryUser");
            dto.Rating.Should().Be(7);
            dto.LastClosedStatus.Should().Be(3);
            dto.ClosedStatusMigrationDate.UtcDateTime.Should()
                .BeCloseTo(poco.ClosedStatusMigrationDate.Required(), TimeSpan.FromSeconds(1));
            dto.ForeColor.Should().Be("#112233");
            dto.BackColor.Should().Be("#445566");
            Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JToken.Parse(dto.Json.Required()),
                Newtonsoft.Json.Linq.JToken.Parse(CaseJson)).Should().BeTrue();
            dto.EnableVisualisation.Should().BeTrue();
            dto.VisualisationRegistryGuid.Should().Be(scenario.VisualisationGuid);
            dto.EntityAnalysisModelId.Should().Be(scenario.ModelId);

            var field = dto.FormattedPayload.Should().ContainSingle().Subject;
            field.Name.Should().Be("Amount");
            field.Value.Should().Be("12.5");
            field.ConditionalRegularExpressionFormatting.Should().BeTrue();
            field.ExistsMatch.Should().BeTrue();
            field.CellFormatForeColor.Should().Be("#ff0000");
            field.CellFormatBackColor.Should().Be("#00ff00");
            field.CellFormatForeRow.Should().BeTrue();
            field.CellFormatBackRow.Should().BeFalse();

            dto.Activation.Should().ContainSingle().Which.Name.Should().Be("RuleA");
        }

        [Fact]
        public async Task GetRecordsExactlyOneCaseViewedEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.GetAsync(scenario.CaseId);

            var events = await EventsForAsync(dbContext, scenario.CaseId);
            var caseEvent = events.Should().ContainSingle().Subject;
            caseEvent.CaseEventTypeId.Should().Be(4);
            caseEvent.CaseKey.Should().Be("AccountId");
            caseEvent.CaseKeyValue.Should().Be($"{DatabaseFixture.Prefix}Key");
            caseEvent.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            caseEvent.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetWithNoXPathsReturnsEmptyFormattedPayloadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, addXPaths: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(scenario.CaseId);

            dto.FormattedPayload.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUnknownCaseThrowsNotFoundWritesNoEventAndLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);
            var before = await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync();

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(int.MaxValue));

            ex.Code.Should().Be("NotFound");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
            (await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync()).Should().Be(before);
        }

        [Fact]
        public async Task TenantBCannotSeeTenantACaseAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseA = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var caseB = await CreateScenarioAsync(dbContext, fx.Seed.UserTenantB);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<NotFoundException>(() => serviceB.GetAsync(caseA.CaseId));
            await Assert.ThrowsAsync<NotFoundException>(() => serviceA.GetAsync(caseB.CaseId));

            (await serviceA.GetAsync(caseA.CaseId)).Id.Should().Be(caseA.CaseId);
            (await serviceB.GetAsync(caseB.CaseId)).Id.Should().Be(caseB.CaseId);

            (await EventsForAsync(dbContext, caseA.CaseId)).Should().ContainSingle();
            (await EventsForAsync(dbContext, caseB.CaseId)).Should().ContainSingle();
        }

        [Fact]
        public async Task CaseWhoseWorkflowIsNotGrantedToCallersRoleIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                grantWorkflowRole: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(scenario.CaseId));
            (await EventsForAsync(dbContext, scenario.CaseId)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseWhoseStatusIsNotGrantedToCallersRoleIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                grantStatusRole: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(scenario.CaseId));
        }

        [Fact]
        public async Task CaseInDeletedWorkflowIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission, workflowDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(scenario.CaseId));
        }

        [Fact]
        public async Task XPathNotGrantedToCallersRoleIsOmittedFromFormattedPayloadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission,
                grantXPathRole: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(scenario.CaseId);

            dto.FormattedPayload.Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(scenario.CaseId, cts.Token));
            (await EventsForAsync(dbContext, scenario.CaseId)).Should().BeEmpty();
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

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

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync(1));

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var scenario = await CreateScenarioAsync(dbContext, fx.Seed.UserWithPermission);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(scenario.CaseId);

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
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

            await service.GetAsync(scenario.CaseId);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbiddenService.GetAsync(scenario.CaseId));
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(int.MaxValue));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("CaseByIdGet");
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

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

                var frenchLocalizer = localizers.Create(typeof(CaseByIdResources));
                ex.Message.Should().Be(frenchLocalizer[CaseByIdResources.PermissionDenied].Value);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}