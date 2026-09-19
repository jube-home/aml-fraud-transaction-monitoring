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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseNoteByCaseKeyValue;
using Jube.Service.Query.CaseNoteByCaseKeyValue;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.CaseNoteByCaseKeyValue.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.CaseNoteByCaseKeyValue
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseNoteByCaseKeyValueServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseNoteIds = [];
        private readonly List<int> createdCaseWorkflowActionIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseNote.Where(w => createdCaseNoteIds.Contains(w.Id)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowActionGuids = dbContext.CaseWorkflowAction
                .Where(a => createdCaseWorkflowActionIds.Contains(a.Id)).Select(a => a.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowActionRole>()
                .Where(w => caseWorkflowActionGuids.Contains(w.CaseWorkflowActionGuid)).DeleteAsync();
            await dbContext.CaseWorkflowAction.Where(w => createdCaseWorkflowActionIds.Contains(w.Id)).DeleteAsync();
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

        private static Task<CaseNoteByCaseKeyValueService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseNoteByCaseKeyValueService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<Fixture> CreateCaseGraphAsync(DbContext dbContext, string ownerUser,
            string grantRoleOfUser, string key, string value, bool grantWorkflow = true, bool grantStatus = true,
            bool grantAction = true, byte workflowDeleted = 0, byte statusDeleted = 0, byte roleDeleted = 0)
        {
            var repository = new Data.Repository.EntityAnalysisModelRepository(dbContext, ownerUser);
            var model = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
            });
            createdModelIds.Add(model.Id);

            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == grantRoleOfUser).Select(u => u.RoleRegistryGuid).FirstAsync();

            var workflowGuid = Guid.NewGuid();
            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = workflowDeleted,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowIds.Add(workflowId);

            if (grantWorkflow)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
                {
                    CaseWorkflowGuid = workflowGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleRegistryGuid,
                    CreatedUser = ownerUser, CreatedDate = DateTime.UtcNow, Deleted = roleDeleted,
                });
            }

            var statusGuid = Guid.NewGuid();
            var statusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                Guid = statusGuid, CaseWorkflowId = workflowId, Active = 1, Locked = 0, Deleted = statusDeleted,
                Priority = 1, CreatedUser = ownerUser, CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(statusId);

            if (grantStatus)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
                {
                    CaseWorkflowStatusGuid = statusGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleRegistryGuid,
                    CreatedUser = ownerUser, CreatedDate = DateTime.UtcNow, Deleted = 0,
                });
            }

            var actionGuid = Guid.NewGuid();
            var actionName = $"{DatabaseFixture.Prefix}Action{Guid.NewGuid():N}"[..40];
            var actionId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowAction
            {
                Name = actionName, Guid = actionGuid, CaseWorkflowId = workflowId, Active = 1, Locked = 0,
                Deleted = 0, CreatedUser = ownerUser, CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowActionIds.Add(actionId);

            if (grantAction)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowActionRole
                {
                    CaseWorkflowActionGuid = actionGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleRegistryGuid,
                    CreatedUser = ownerUser, CreatedDate = DateTime.UtcNow, Deleted = 0,
                });
            }

            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                CaseWorkflowGuid = workflowGuid, CaseWorkflowStatusGuid = statusGuid, CaseKey = key,
                CaseKeyValue = value, Json = "{}", CreatedDate = DateTime.UtcNow,
            });
            createdCaseIds.Add(caseId);

            return new Fixture(actionId, actionName, caseId, key, value);
        }

        private async Task<int> AddNoteAsync(DbContext dbContext, Fixture graph, string note, int priorityId,
            string createdUser, DateTime createdDate)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseNote
            {
                Note = note, ActionId = graph.ActionId, PriorityId = priorityId, CreatedDate = createdDate,
                CreatedUser = createdUser, CaseId = graph.CaseId, CaseKey = graph.Key, CaseKeyValue = graph.Value,
            });
            createdCaseNoteIds.Add(id);
            return id;
        }

        private static string Unique(string label) => $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..32];

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseNoteByCaseKeyValueService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task GetWithoutPermissionThrowsForbiddenWithCodeAndSpecsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("k", "v"));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([1]);
        }

        [Fact]
        public async Task ForbiddenLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("k", "v"));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task GetReturnsSeededNotesMappedFieldByFieldNewestFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, user, user, key, value);
            var created = DateTime.UtcNow.AddMinutes(-5);
            var firstId = await AddNoteAsync(dbContext, graph, "first note", 1, "author-one", created);
            var secondId = await AddNoteAsync(dbContext, graph, "second note", 3, "author-two", created.AddMinutes(1));
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.GetAsync(key, value);

            result.Select(r => r.Id).Should().Equal(secondId, firstId);
            var second = result[0];
            second.CaseId.Should().Be(graph.CaseId);
            second.CreatedDate.Should().BeCloseTo(created.AddMinutes(1), TimeSpan.FromSeconds(2));
            second.CreatedUser.Should().Be("author-two");
            second.Note.Should().Be("second note");
            second.ActionId.Should().Be(graph.ActionId);
            second.Action.Should().Be(graph.ActionName);
            second.PriorityId.Should().Be(3);
            second.Priority.Should().Be("Low");
            result[1].Priority.Should().Be("High");
            result[1].PriorityId.Should().Be(1);
        }

        [Theory]
        [InlineData("<img src=x onerror=alert(1)>harmless", "onerror")]
        [InlineData("plain <script>alert(1)</script> text", "<script")]
        [InlineData("<a href=\"javascript:alert(1)\">click</a>", "javascript:")]
        public async Task NotesStoredBeforeTheSanitiserFixAreSanitisedOnReadAsync(string stored, string forbidden)
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, user, user, key, value);
            await AddNoteAsync(dbContext, graph, stored, 2, user, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.GetAsync(key, value);

            var note = result.Should().ContainSingle().Subject.Note;
            note.Should().NotBeNull();
            note.Required().ToLowerInvariant().Should().NotContain(forbidden);
            note.Should().NotContain("onerror");
        }

        [Fact]
        public async Task PlainNotesAreReturnedUnchangedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, user, user, key, value);
            await AddNoteAsync(dbContext, graph, "Called the customer, will re-check tomorrow.", 2, user,
                DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.GetAsync(key, value);

            result.Should().ContainSingle().Which.Note.Should().Be("Called the customer, will re-check tomorrow.");
        }

        [Theory]
        [InlineData(1, "High")]
        [InlineData(2, "Medium")]
        [InlineData(3, "Low")]
        [InlineData(9, "Medium")]
        public async Task PriorityIdIsConvertedToTextAsync(int priorityId, string expected)
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, user, user, key, value);
            await AddNoteAsync(dbContext, graph, "n", priorityId, user, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.GetAsync(key, value);

            result.Should().ContainSingle().Which.Priority.Should().Be(expected);
        }

        [Fact]
        public async Task GetReturnsEmptyWhenNoCaseMatchesKeyValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var graph = await CreateCaseGraphAsync(dbContext, user, user, Unique("Key"), Unique("Val"));
            await AddNoteAsync(dbContext, graph, "n", 1, user, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(graph.Key, Unique("Other"))).Should().BeEmpty();
            (await service.GetAsync(Unique("OtherKey"), graph.Value)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetOnlyReturnsNotesForTheMatchingCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var graphA = await CreateCaseGraphAsync(dbContext, user, user, Unique("Key"), Unique("Val"));
            var graphB = await CreateCaseGraphAsync(dbContext, user, user, Unique("Key"), Unique("Val"));
            var noteA = await AddNoteAsync(dbContext, graphA, "a", 1, user, DateTime.UtcNow);
            await AddNoteAsync(dbContext, graphB, "b", 1, user, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.GetAsync(graphA.Key, graphA.Value);

            result.Should().ContainSingle().Which.Id.Should().Be(noteA);
        }

        [Fact]
        public async Task TenantBNotesAreInvisibleToTenantAAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userA = fx.Seed.UserWithPermission;
            var userB = fx.Seed.UserTenantB;
            var key = Unique("Key");
            var value = Unique("Val");
            var graphA = await CreateCaseGraphAsync(dbContext, userA, userA, key, value);
            var graphB = await CreateCaseGraphAsync(dbContext, userB, userB, key, value);
            var noteA = await AddNoteAsync(dbContext, graphA, "tenant a", 1, userA, DateTime.UtcNow);
            var noteB = await AddNoteAsync(dbContext, graphB, "tenant b", 1, userB, DateTime.UtcNow);

            var serviceA = await BuildServiceAsync(dbContext, userA);
            var serviceB = await BuildServiceAsync(dbContext, userB);

            (await serviceA.GetAsync(key, value)).Select(r => r.Id).Should().Equal(noteA);
            (await serviceB.GetAsync(key, value)).Select(r => r.Id).Should().Equal(noteB);
        }

        [Fact]
        public async Task TenantAUserCannotSeeTenantBNotesEvenWhenRoleGrantIsForATenantAUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userA = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, fx.Seed.UserTenantB, userA, key, value);
            await AddNoteAsync(dbContext, graph, "cross tenant", 1, userA, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, userA);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task UserWithoutTheGrantedRoleSeesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, fx.Seed.UserWithPermission,
                fx.Seed.UserWithPermission, key, value);
            await AddNoteAsync(dbContext, graph, "n", 1, "x", DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Theory]
        [InlineData("workflow")]
        [InlineData("status")]
        [InlineData("action")]
        public async Task MissingRoleGrantOnAnyOfWorkflowStatusOrActionHidesNotesAsync(string missing)
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, user, user, key, value,
                grantWorkflow: missing != "workflow", grantStatus: missing != "status",
                grantAction: missing != "action");
            await AddNoteAsync(dbContext, graph, "n", 1, user, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task DeletedWorkflowRoleGrantHidesNotesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, user, user, key, value, roleDeleted: 1);
            await AddNoteAsync(dbContext, graph, "n", 1, user, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task DeletedWorkflowHidesNotesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, user, user, key, value, workflowDeleted: 1);
            await AddNoteAsync(dbContext, graph, "n", 1, user, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task DeletedStatusHidesNotesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var key = Unique("Key");
            var value = Unique("Val");
            var graph = await CreateCaseGraphAsync(dbContext, user, user, key, value, statusDeleted: 1);
            await AddNoteAsync(dbContext, graph, "n", 1, user, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAndAuditsCancelledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync("k", "v", cts.Token));

            auditLog.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=cancelled");
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAndPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);
            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync("k", "v"));

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Which.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task ExactlyOneAuditRecordPerCallAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(Unique("Key"), Unique("Val"));

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("area=CaseNoteByCaseKeyValue op=Get")
                .And.Contain("outcome=ok").And.Contain("rows=0");
        }

        [Fact]
        public async Task ForbiddenCallIsAuditedOnceWithForbiddenOutcomeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, auditLog: auditLog);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("k", "v"));

            auditLog.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=forbidden");
        }

        [Fact]
        public async Task ReadsPublishNoChangeEventAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await service.GetAsync(Unique("Key"), Unique("Val"));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniqueToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("CaseNoteByCaseKeyValueGet");
        }
    }
}