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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseWorkflowMacroExecution;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseWorkflowMacroExecution;
using Jube.Service.Observability;
using Jube.Service.Query.CaseWorkflowMacroExecution;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.CaseWorkflowMacroExecution.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using log4net;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Query.CaseWorkflowMacroExecution
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowMacroExecutionServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                                          ?? Environment.GetEnvironmentVariable("ConnectionString")
                                                          ??
                                                          "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;Pooling=true;MinPoolSize=1;MaxPoolSize=50;";

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string> { ["ConnectionString"] = connectionString });

        private static readonly EngineJsonSerializationHelper jsonSerializationHelper = new();

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdMacroIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var macroGuids = dbContext.CaseWorkflowMacro.Where(s => createdMacroIds.Contains(s.Id))
                .Select(s => s.Guid);
            var statusGuids = dbContext.CaseWorkflowStatus.Where(s => createdCaseWorkflowStatusIds.Contains(s.Id))
                .Select(s => s.Guid);
            var workflowGuids = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowMacroRole>()
                .Where(w => macroGuids.Contains(w.CaseWorkflowMacroGuid)).DeleteAsync();
            await dbContext.CaseWorkflowMacro.Where(w => createdMacroIds.Contains(w.Id)).DeleteAsync();
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

        private static Task<CaseWorkflowMacroExecutionService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowMacroExecutionService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new NullServiceChangeBus(), dynamicEnvironment,
                jsonSerializationHelper, auditLog ?? TestLog.NoOp);
        }

        private async Task<Seeded> SeedAsync(DbContext dbContext, string ownerUser, string? endpoint = null,
            byte httpEndpointTypeId = 1, string? workflowRoleUser = null, string? statusRoleUser = null,
            string? macroRoleUser = null, bool macroActive = true, string? caseJson = null)
        {
            var modelRepository = new EntityAnalysisModelRepository(dbContext, ownerUser);
            var model = await modelRepository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
            });
            createdModelIds.Add(model.Id);

            var workflowGuid = Guid.NewGuid();
            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                EnableVisualisation = 1,
                VisualisationRegistryGuid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowIds.Add(workflowId);
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = workflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleGuidAsync(dbContext, workflowRoleUser ?? ownerUser),
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            var statusGuid = Guid.NewGuid();
            var statusName = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40];
            var statusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = statusName,
                Guid = statusGuid,
                CaseWorkflowId = workflowId,
                ForeColor = "#112233",
                BackColor = "#445566",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Priority = 1,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(statusId);
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = statusGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleGuidAsync(dbContext, statusRoleUser ?? ownerUser),
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            var macroGuid = Guid.NewGuid();
            var macroName = $"{DatabaseFixture.Prefix}Macro{Guid.NewGuid():N}"[..40];
            var macroId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowMacro
            {
                Name = macroName,
                Guid = macroGuid,
                CaseWorkflowId = workflowId,
                Active = (byte)(macroActive ? 1 : 0),
                Locked = 0,
                Deleted = 0,
                Version = 1,
                EnableHttpEndpoint = (byte)(endpoint == null ? 0 : 1),
                HttpEndpoint = endpoint,
                HttpEndpointTypeId = httpEndpointTypeId,
                EnableNotification = 0,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdMacroIds.Add(macroId);
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowMacroRole
            {
                CaseWorkflowMacroGuid = macroGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleGuidAsync(dbContext, macroRoleUser ?? ownerUser),
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CreatedDate = DateTime.UtcNow,
                Locked = 0,
                ClosedStatusId = 0,
                CaseKey = "AccountId",
                CaseKeyValue = $"{DatabaseFixture.Prefix}Ckv{Guid.NewGuid():N}",
                Diary = 0,
                Rating = 3,
                LastClosedStatus = 0,
                Json = caseJson ?? new JObject
                {
                    ["payload"] = new JObject { ["Ref"] = "abc123" },
                }.ToString(Formatting.None),
            });
            createdCaseIds.Add(caseId);

            return new Seeded(caseId, macroId, statusName, macroName);
        }

        private static Task<Guid> RoleGuidAsync(DbContext dbContext, string userName) =>
            dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

        private static CaseWorkflowMacroExecutionDto Request(Seeded seeded) =>
            new() { CaseId = seeded.CaseId, CaseWorkflowMacroId = seeded.MacroId };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowMacroExecutionService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), dynamicEnvironment, jsonSerializationHelper, TestLog.NoOp));

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
        public async Task ExecuteWithoutPermissionThrowsForbiddenAndCallsNothingAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, hook.BaseUrl + "hook");
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteAsync(Request(seeded)));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([1]);
            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
            hook.Snapshot().Should().BeEmpty();
        }

        [Fact]
        public async Task ExecutePostsTheCaseStatusAndPayloadToTheHttpEndpointAndEchoesTheRequestAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user, hook.BaseUrl + "hook/[@Payload.Ref@]");
            var service = await BuildServiceAsync(dbContext, user);

            var dto = await service.ExecuteAsync(Request(seeded));

            dto.CaseId.Should().Be(seeded.CaseId);
            dto.CaseWorkflowMacroId.Should().Be(seeded.MacroId);

            var received = hook.Snapshot().Should().ContainSingle().Subject;
            received.Method.Should().Be("POST");
            received.Path.Should().Be("/hook/abc123");
            var body = JObject.Parse(received.Body);
            body["caseId"].Required().Value<int>().Should().Be(seeded.CaseId);
            body["caseWorkflowMacroId"].Required().Value<int>().Should().Be(seeded.MacroId);
            body["caseWorkflowMacroName"].Required().Value<string>().Should().Be(seeded.MacroName);
            var caseBody = (JObject)body["case"].Required();
            caseBody["id"].Required().Value<int>().Should().Be(seeded.CaseId);
            caseBody["caseWorkflowStatus"].Required().Value<string>().Should().Be(seeded.StatusName);
            caseBody.ContainsKey("json").Should().BeFalse();
            caseBody["payload"].Required()["payload"].Required()["Ref"].Required().Value<string>().Should().Be("abc123");
        }

        [Fact]
        public async Task ExecuteWithGetEndpointTypeIssuesAGetWithNoBodyAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user, hook.BaseUrl + "ping", httpEndpointTypeId: 2);
            var service = await BuildServiceAsync(dbContext, user);

            await service.ExecuteAsync(Request(seeded));

            var received = hook.Snapshot().Should().ContainSingle().Subject;
            received.Method.Should().Be("GET");
            received.Path.Should().Be("/ping");
            received.Body.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteWithNothingEnabledSucceedsAndCallsNothingAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);

            var dto = await service.ExecuteAsync(Request(seeded));

            dto.CaseId.Should().Be(seeded.CaseId);
            hook.Snapshot().Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteDoesNotModifyTheCaseMacroOrStatusAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user, hook.BaseUrl + "hook");
            var caseBefore = JsonConvert.SerializeObject(await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId));
            var macroBefore = JsonConvert.SerializeObject(
                await dbContext.CaseWorkflowMacro.FirstAsync(c => c.Id == seeded.MacroId));
            var eventsBefore =
                await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync(e => e.CaseId == seeded.CaseId);
            var service = await BuildServiceAsync(dbContext, user);

            await service.ExecuteAsync(Request(seeded));

            JsonConvert.SerializeObject(await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId))
                .Should().Be(caseBefore);
            JsonConvert.SerializeObject(await dbContext.CaseWorkflowMacro.FirstAsync(c => c.Id == seeded.MacroId))
                .Should().Be(macroBefore);
            (await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync(e => e.CaseId == seeded.CaseId))
                .Should().Be(eventsBefore);
        }

        [Fact]
        public async Task ExecuteUnknownCaseOrMacroThrowsNotFoundAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user, hook.BaseUrl + "hook");
            var service = await BuildServiceAsync(dbContext, user);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(
                new CaseWorkflowMacroExecutionDto { CaseId = -1, CaseWorkflowMacroId = seeded.MacroId }));
            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(
                new CaseWorkflowMacroExecutionDto { CaseId = seeded.CaseId, CaseWorkflowMacroId = -1 }));

            hook.Snapshot().Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteInactiveMacroThrowsNotFoundAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user, hook.BaseUrl + "hook", macroActive: false);
            var service = await BuildServiceAsync(dbContext, user);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Request(seeded)));
            hook.Snapshot().Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAcrossTenantsBehavesAsNotFoundInBothDirectionsAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var tenantA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, hook.BaseUrl + "a");
            var tenantB = await SeedAsync(dbContext, fx.Seed.UserTenantB, hook.BaseUrl + "b");
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<NotFoundException>(() => serviceA.ExecuteAsync(Request(tenantB)));
            await Assert.ThrowsAsync<NotFoundException>(() => serviceA.ExecuteAsync(
                new CaseWorkflowMacroExecutionDto { CaseId = tenantA.CaseId, CaseWorkflowMacroId = tenantB.MacroId }));
            await Assert.ThrowsAsync<NotFoundException>(() => serviceA.ExecuteAsync(
                new CaseWorkflowMacroExecutionDto { CaseId = tenantB.CaseId, CaseWorkflowMacroId = tenantA.MacroId }));

            await Assert.ThrowsAsync<NotFoundException>(() => serviceB.ExecuteAsync(Request(tenantA)));
            await Assert.ThrowsAsync<NotFoundException>(() => serviceB.ExecuteAsync(
                new CaseWorkflowMacroExecutionDto { CaseId = tenantB.CaseId, CaseWorkflowMacroId = tenantA.MacroId }));

            hook.Snapshot().Should().BeEmpty();

            await serviceA.ExecuteAsync(Request(tenantA));
            await serviceB.ExecuteAsync(Request(tenantB));
            hook.Snapshot().Select(r => r.Path).Should().BeEquivalentTo(["/a", "/b"]);
        }

        [Fact]
        public async Task ExecuteRequiresTheCallersRoleOnTheWorkflowStatusAndMacroAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var noWorkflowRole = await SeedAsync(dbContext, user, hook.BaseUrl + "hook",
                workflowRoleUser: fx.Seed.UserTenantB);
            var noStatusRole = await SeedAsync(dbContext, user, hook.BaseUrl + "hook",
                statusRoleUser: fx.Seed.UserTenantB);
            var noMacroRole = await SeedAsync(dbContext, user, hook.BaseUrl + "hook",
                macroRoleUser: fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, user);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Request(noWorkflowRole)));
            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Request(noStatusRole)));
            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Request(noMacroRole)));

            hook.Snapshot().Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteEndpointFailureIsSwallowedAsLegacyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            var seeded = await SeedAsync(dbContext, user, $"http://127.0.0.1:{port}/nobody-home");
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, user, log);

            var dto = await service.ExecuteAsync(Request(seeded));

            dto.CaseId.Should().Be(seeded.CaseId);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAndCallsNothingAsync()
        {
            using var hook = new Hook();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, hook.BaseUrl + "hook");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ExecuteAsync(Request(seeded), cts.Token));

            hook.Snapshot().Should().BeEmpty();
        }

        [Fact]
        public async Task SuccessEmitsOneAuditLineOneSpanAndNoChangeEventAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var auditLog = new TestLog();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, user, auditLog: auditLog, serviceChangeBus: bus);

            await service.ExecuteAsync(Request(seeded));

            auditLog.Entries.Should().ContainSingle().Which.Message.Should()
                .Contain("area=CaseWorkflowMacroExecution").And.Contain("op=Execute").And.Contain("outcome=ok");
            activities.Should().ContainSingle(a => a.OperationName == "CaseWorkflowMacroExecution.Execute")
                .Which.GetTagItem("jube.outcome").Should().Be("ok");
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ForbiddenAndNotFoundPublishNothingAndAuditTheirOutcomeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var auditForbidden = new TestLog();
            var auditNotFound = new TestLog();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                auditLog: auditForbidden, serviceChangeBus: bus);
            var notFound = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditNotFound,
                serviceChangeBus: bus);

            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.ExecuteAsync(Request(seeded)));
            await Assert.ThrowsAsync<NotFoundException>(() => notFound.ExecuteAsync(
                new CaseWorkflowMacroExecutionDto { CaseId = -1, CaseWorkflowMacroId = seeded.MacroId }));

            auditForbidden.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=forbidden");
            auditNotFound.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=not_found");
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue()
        {
            var attribute = typeof(CaseWorkflowMacroExecutionService)
                .GetMethod(nameof(CaseWorkflowMacroExecutionService.ExecuteAsync)).Required()
                .GetCustomAttribute<ServiceOperationAttribute>().Required();
            attribute.Name.Should().Be("CaseWorkflowMacroExecutionExecute");
            attribute.Kind.Should().Be(OperationKind.Write);
            attribute.Idempotent.Should().BeFalse();

            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("CaseWorkflowMacroExecutionExecute");
        }
    }
}