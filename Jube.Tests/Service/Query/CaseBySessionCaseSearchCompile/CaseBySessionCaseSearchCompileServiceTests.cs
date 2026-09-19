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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseBySessionCaseSearchCompile;
using Jube.Service.Observability;
using Jube.Service.Query.CaseBySessionCaseSearchCompile;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.CaseBySessionCaseSearchCompile.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using log4net;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Query.CaseBySessionCaseSearchCompile
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseBySessionCaseSearchCompileServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                                          ?? Environment.GetEnvironmentVariable("ConnectionString")
                                                          ??
                                                          "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;Pooling=true;MinPoolSize=1;MaxPoolSize=50;";

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] = connectionString,
                ["ParserAssertSelectOnly"] = "True",
            });

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdCompiledIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<Guid> createdXPathGuids = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseEvent>()
                .Where(w => w.CaseId != null && createdCaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .Where(w => createdCompiledIds.Contains(w.SessionCaseSearchCompiledSqlId)).DeleteAsync();
            await dbContext.SessionCaseSearchCompiledSql.Where(w => createdCompiledIds.Contains(w.Id)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathRole>()
                .Where(w => createdXPathGuids.Contains(w.CaseWorkflowXPathGuid)).DeleteAsync();
            await dbContext.CaseWorkflowXPath.Where(w => createdCaseWorkflowIds.Contains(w.CaseWorkflowId ?? 0))
                .DeleteAsync();
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

        private static Task<CaseBySessionCaseSearchCompileService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseBySessionCaseSearchCompileService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new NullServiceChangeBus(), dynamicEnvironment,
                auditLog ?? TestLog.NoOp);
        }

        private async Task<Seeded> SeedAsync(DbContext dbContext, string ownerUser, string? lockedUser = null,
            bool withXPath = false)
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

            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == ownerUser).Select(u => u.RoleRegistryGuid).FirstAsync();

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
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = ownerUser,
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
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(statusId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = statusGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            if (withXPath)
            {
                var xPathGuid = Guid.NewGuid();
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowXPath
                {
                    Guid = xPathGuid,
                    CaseWorkflowId = workflowId,
                    Name = "Amount",
                    XPath = "$.payload.Amount",
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    ConditionalRegularExpressionFormatting = 1,
                    ConditionalFormatForeColor = "#AA0000",
                    ConditionalFormatBackColor = "#00BB00",
                    RegularExpression = "^12",
                    ForeRowColorScope = 1,
                    BackRowColorScope = 0,
                    CreatedUser = ownerUser,
                    CreatedDate = DateTime.UtcNow,
                });
                createdXPathGuids.Add(xPathGuid);
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowXPathRole
                {
                    CaseWorkflowXPathGuid = xPathGuid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = roleRegistryGuid,
                    CreatedUser = ownerUser,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                });
            }

            var caseKeyValue = $"{DatabaseFixture.Prefix}Ckv{Guid.NewGuid():N}";
            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CreatedDate = DateTime.UtcNow,
                Locked = (byte)(lockedUser == null ? 0 : 1),
                LockedUser = lockedUser,
                LockedDate = lockedUser == null ? null : DateTime.UtcNow,
                ClosedStatusId = 0,
                CaseKey = "AccountId",
                CaseKeyValue = caseKeyValue,
                Diary = 0,
                Rating = 3,
                LastClosedStatus = 0,
                Json = new JObject
                {
                    ["payload"] = new JObject { ["Amount"] = "1234", ["AccountId"] = caseKeyValue },
                    ["activation"] = new JObject
                    {
                        ["RuleVisible"] = new JObject { ["visible"] = 1 },
                        ["RuleHidden"] = new JObject { ["visible"] = 0 },
                    },
                }.ToString(Formatting.None),
            });
            createdCaseIds.Add(caseId);

            var tokens = new List<object> { caseKeyValue, workflowGuid, ownerUser };
            var whereSql = "from \"Case\",\"CaseWorkflow\",\"EntityAnalysisModel\",\"TenantRegistry\"," +
                           "\"CaseWorkflowStatus\",\"UserInTenant\"" +
                           " where \"EntityAnalysisModel\".\"Id\" = \"CaseWorkflow\".\"EntityAnalysisModelId\"" +
                           " and \"EntityAnalysisModel\".\"TenantRegistryId\" = \"TenantRegistry\".\"Id\"" +
                           " and \"UserInTenant\".\"TenantRegistryId\" = \"TenantRegistry\".\"Id\"" +
                           " and \"Case\".\"CaseWorkflowGuid\" = \"CaseWorkflow\".\"Guid\"" +
                           " and \"Case\".\"CaseWorkflowStatusGuid\" = \"CaseWorkflowStatus\".\"Guid\"" +
                           " and \"Case\".\"CaseKeyValue\" = (@1)" +
                           " and \"CaseWorkflow\".\"Guid\" = uuid(@2)" +
                           " and \"UserInTenant\".\"User\" = (@3)";

            var compiledGuid = Guid.NewGuid();
            var compiledId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.SessionCaseSearchCompiledSql
            {
                Guid = compiledGuid,
                FilterTokens = JsonConvert.SerializeObject(tokens),
                WhereSql = whereSql,
                OrderSql = "order by \"Case\".\"Id\" ASC",
                CaseWorkflowGuid = workflowGuid,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
                Prepared = 1,
            });
            createdCompiledIds.Add(compiledId);

            return new Seeded(workflowGuid, statusGuid, model.Id, caseKeyValue, compiledGuid, compiledId, caseId);
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
                CaseBySessionCaseSearchCompileService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp));

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
        public async Task GetWithoutPermissionThrowsForbiddenAndChangesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(seeded.CompiledGuid));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([1]);
            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
            (await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId)).Locked.Should().Be(0);
            (await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync(e => e.CaseId == seeded.CaseId))
                .Should().Be(0);
        }

        [Fact]
        public async Task GetReturnsTheCaseWithEveryFieldMappedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user, withXPath: true);
            var before = await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId);
            var service = await BuildServiceAsync(dbContext, user);

            var dto = await service.GetAsync(seeded.CompiledGuid);

            dto.Id.Should().Be(seeded.CaseId);
            dto.EntityAnalysisModelInstanceEntryGuid.Should().Be(before.EntityAnalysisModelInstanceEntryGuid);
            dto.CaseWorkflowGuid.Should().Be(seeded.WorkflowGuid);
            dto.CaseWorkflowStatusGuid.Should().Be(seeded.StatusGuid);
            dto.CreatedDate.UtcDateTime.Should().BeCloseTo(before.CreatedDate.Required(), TimeSpan.FromSeconds(1));
            dto.Locked.Should().BeTrue();
            dto.LockedUser.Should().Be(user);
            dto.ClosedStatusId.Should().Be(0);
            dto.ClosedUser.Should().BeEmpty();
            dto.CaseKey.Should().Be("AccountId");
            dto.CaseKeyValue.Should().Be(seeded.CaseKeyValue);
            dto.Diary.Should().BeFalse();
            dto.DiaryUser.Should().BeEmpty();
            dto.Rating.Should().Be(3);
            dto.LastClosedStatus.Should().Be(0);
            dto.ForeColor.Should().Be("#112233");
            dto.BackColor.Should().Be("#445566");
            dto.Json.Should().Contain(seeded.CaseKeyValue);
            dto.EnableVisualisation.Should().BeTrue();
            dto.VisualisationRegistryGuid.Should().NotBeEmpty();
            dto.EntityAnalysisModelId.Should().Be(seeded.ModelId);

            dto.Activation.Should().ContainSingle().Which.Name.Should().Be("RuleVisible");
            var field = dto.FormattedPayload.Should().ContainSingle().Subject;
            field.Name.Should().Be("Amount");
            field.Value.Should().Be("1234");
            field.ConditionalRegularExpressionFormatting.Should().BeTrue();
            field.ExistsMatch.Should().BeTrue();
            field.CellFormatForeColor.Should().Be("#AA0000");
            field.CellFormatBackColor.Should().Be("#00BB00");
            field.CellFormatForeRow.Should().BeTrue();
            field.CellFormatBackRow.Should().BeFalse();
        }

        [Fact]
        public async Task GetRecordsACaseEventLocksTheRetrievedCaseAndRecordsAnExecutionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var decoy = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);

            await service.GetAsync(seeded.CompiledGuid);

            var caseEvent = (await dbContext.GetTable<Data.Poco.CaseEvent>()
                .Where(e => e.CaseId == seeded.CaseId).ToListAsync()).Should().ContainSingle().Subject;
            caseEvent.CaseEventTypeId.Should().Be(2);
            caseEvent.CaseKey.Should().Be("AccountId");
            caseEvent.CaseKeyValue.Should().Be(seeded.CaseKeyValue);
            caseEvent.CreatedUser.Should().Be(user);

            var locked = await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId);
            locked.Locked.Should().Be(1);
            locked.LockedUser.Should().Be(user);
            locked.LockedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

            (await dbContext.Case.FirstAsync(c => c.Id == decoy.CaseId)).Locked.Should().Be(0);

            var execution = (await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                    .Where(e => e.SessionCaseSearchCompiledSqlId == seeded.CompiledId).ToListAsync())
                .Should().ContainSingle().Subject;
            execution.Records.Should().Be(1);
            execution.CreatedUser.Should().Be(user);
        }

        [Fact]
        public async Task GetReturnsACaseAlreadyLockedToTheCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user, lockedUser: user);
            var service = await BuildServiceAsync(dbContext, user);

            var dto = await service.GetAsync(seeded.CompiledGuid);

            dto.Id.Should().Be(seeded.CaseId);
            dto.LockedUser.Should().Be(user);
        }

        [Fact]
        public async Task GetThrowsNotFoundWhenTheCaseIsLockedToAnotherUserAndStillRecordsTheExecutionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user, lockedUser: "someone-else");
            var service = await BuildServiceAsync(dbContext, user);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(seeded.CompiledGuid));

            ex.Code.Should().Be("NotFound");
            ex.Message.Should().Be(localizers.Create(typeof(CaseBySessionCaseSearchCompileResources))
                [CaseBySessionCaseSearchCompileResources.NotFound].Value);
            (await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId)).LockedUser.Should().Be("someone-else");
            (await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync(e => e.CaseId == seeded.CaseId))
                .Should().Be(0);
            (await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .CountAsync(e => e.SessionCaseSearchCompiledSqlId == seeded.CompiledId)).Should().Be(1);
        }

        [Fact]
        public async Task GetThrowsNotFoundWhenNoCaseMatchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            await dbContext.Case.Where(c => c.Id == seeded.CaseId).DeleteAsync();
            var service = await BuildServiceAsync(dbContext, user);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(seeded.CompiledGuid));
        }

        [Fact]
        public async Task GetWithUnknownGuidFailsAsTheLegacyNullReferenceOutcomeAndLogsErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await Assert.ThrowsAsync<NullReferenceException>(() => service.GetAsync(Guid.NewGuid()));

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Which.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task SameTenantUserCannotConsumeAnotherUsersCompiledSearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);

            await Assert.ThrowsAsync<NullReferenceException>(() => service.GetAsync(seeded.CompiledGuid));

            (await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId)).Locked.Should().Be(0);
            (await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .CountAsync(e => e.SessionCaseSearchCompiledSqlId == seeded.CompiledId)).Should().Be(0);
        }

        [Fact]
        public async Task RepositoryGetByGuidIsScopedToTheOwningUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);

            new SessionCaseSearchCompiledSqlRepository(dbContext, fx.Seed.UserWithPermission)
                .GetByGuid(seeded.CompiledGuid).Should().NotBeNull();
            new SessionCaseSearchCompiledSqlRepository(dbContext, fx.Seed.UserWithPermissionNoApproveByReview)
                .GetByGuid(seeded.CompiledGuid).Should().BeNull();
            new SessionCaseSearchCompiledSqlRepository(dbContext, fx.Seed.UserTenantB)
                .GetByGuid(seeded.CompiledGuid).Should().BeNull();
        }

        [Fact]
        public async Task TenantBUserCannotRetrieveOrLockATenantACaseThroughATenantACompiledSearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var act = () => service.GetAsync(seeded.CompiledGuid);

            (await act.Should().ThrowAsync<Exception>()).Which.Should().NotBeOfType<ForbiddenException>();
            (await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId)).Locked.Should().Be(0);
            (await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync(e => e.CaseId == seeded.CaseId))
                .Should().Be(0);
        }

        [Fact]
        public async Task TenantAUserCannotRetrieveOrLockATenantBCaseThroughATenantBCompiledSearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.GetAsync(seeded.CompiledGuid);

            (await act.Should().ThrowAsync<Exception>()).Which.Should().NotBeOfType<ForbiddenException>();
            (await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId)).Locked.Should().Be(0);
            (await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync(e => e.CaseId == seeded.CaseId))
                .Should().Be(0);
        }

        [Fact]
        public async Task EachTenantRetrievesOnlyItsOwnCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seededA = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var seededB = await SeedAsync(dbContext, fx.Seed.UserTenantB);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(seededA.CompiledGuid)).Id.Should().Be(seededA.CaseId);
            (await serviceB.GetAsync(seededB.CompiledGuid)).Id.Should().Be(seededB.CaseId);
        }

        [Fact]
        public async Task UserWithoutTheWorkflowRoleCannotRetrieveTheCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == fx.Seed.UserWithPermission).Select(u => u.RoleRegistryGuid).FirstAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(r => r.CaseWorkflowGuid == seeded.WorkflowGuid && r.RoleRegistryGuid == roleRegistryGuid)
                .DeleteAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.GetAsync(seeded.CompiledGuid);

            await act.Should().ThrowAsync<Exception>();
            (await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId)).Locked.Should().Be(0);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAndChangesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.GetAsync(seeded.CompiledGuid, cts.Token));

            (await dbContext.Case.FirstAsync(c => c.Id == seeded.CaseId, token: cts.Token)).Locked.Should().Be(0);
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

            await service.GetAsync(seeded.CompiledGuid);

            auditLog.Entries.Should().ContainSingle().Which.Message.Should()
                .Contain("area=CaseBySessionCaseSearchCompile").And.Contain("op=Get").And.Contain("outcome=ok");
            activities.Should().ContainSingle(a => a.OperationName == "CaseBySessionCaseSearchCompile.Get")
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
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                auditLog: auditForbidden, serviceChangeBus: bus);
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, lockedUser: "someone-else");
            var notFound = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditNotFound,
                serviceChangeBus: bus);

            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(seeded.CompiledGuid));
            await Assert.ThrowsAsync<NotFoundException>(() => notFound.GetAsync(seeded.CompiledGuid));

            auditForbidden.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=forbidden");
            auditNotFound.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=not_found");
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue()
        {
            var attribute = typeof(CaseBySessionCaseSearchCompileService)
                .GetMethod(nameof(CaseBySessionCaseSearchCompileService.GetAsync)).Required()
                .GetCustomAttribute<ServiceOperationAttribute>().Required();
            attribute.Name.Should().Be("CaseBySessionCaseSearchCompileGet");
            attribute.Kind.Should().Be(OperationKind.Write);
            attribute.Idempotent.Should().BeFalse();

            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("CaseBySessionCaseSearchCompileGet");
        }
    }
}