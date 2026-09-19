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
using Jube.Dto.Repository.SessionCaseSearchCompiledSql;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.SessionCaseSearchCompiledSql;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.SessionCaseSearchCompiledSql;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Repository.SessionCaseSearchCompiledSql.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;
using log4net;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Repository.SessionCaseSearchCompiledSql
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SessionCaseSearchCompiledSqlServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                ["ParserAssertSelectOnly"] = "True"
            });

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<Guid> createdWorkflowGuids = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var compiledIds = dbContext.SessionCaseSearchCompiledSql
                .Where(w => createdWorkflowGuids.Contains(w.CaseWorkflowGuid)).Select(w => w.Id);
            await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .Where(w => compiledIds.Contains(w.SessionCaseSearchCompiledSqlId)).DeleteAsync();
            await dbContext.SessionCaseSearchCompiledSql
                .Where(w => createdWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var statusGuids = dbContext.CaseWorkflowStatus
                .Where(s => createdCaseWorkflowStatusIds.Contains(s.Id)).Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => statusGuids.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => createdWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => createdCaseWorkflowStatusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<SessionCaseSearchCompiledSqlService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return SessionCaseSearchCompiledSqlService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new NullServiceChangeBus(), dynamicEnvironment,
                auditLog ?? TestLog.NoOp);
        }

        private async Task<Seeded> SeedAsync(DbContext dbContext, string ownerUser)
        {
            var model = await new Data.Repository.EntityAnalysisModelRepository(dbContext, ownerUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
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
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow
            });
            createdCaseWorkflowIds.Add(workflowId);
            createdWorkflowGuids.Add(workflowGuid);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = workflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0
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
                CreatedDate = DateTime.UtcNow
            });
            createdCaseWorkflowStatusIds.Add(statusId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = statusGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0
            });

            var caseKeyValue = $"{DatabaseFixture.Prefix}Ckv{Guid.NewGuid():N}";
            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CreatedDate = DateTime.UtcNow,
                Locked = 0,
                ClosedStatusId = 0,
                CaseKey = "AccountId",
                CaseKeyValue = caseKeyValue,
                Diary = 0,
                Rating = 3,
                LastClosedStatus = 0,
                Json = "{}"
            });
            createdCaseIds.Add(caseId);

            return new Seeded(workflowGuid, caseKeyValue, caseId);
        }

        private static string SelectJson() => JsonConvert.SerializeObject(new
        {
            condition = "AND",
            rules = new[]
            {
                new
                {
                    id = "CaseKeyValue", field = "\"Case\".\"CaseKeyValue\"", type = "string", @operator = "order",
                    value = "ASC"
                }
            }
        });

        private static string FilterJson(string caseKeyValue) => JsonConvert.SerializeObject(new
        {
            condition = "AND",
            rules = new[]
            {
                new
                {
                    id = "CaseKeyValue", field = "CaseKeyValue", type = "string", @operator = "equal",
                    value = caseKeyValue
                }
            }
        });

        private static SessionCaseSearchCompiledSqlDto Request(Seeded seeded) => new()
        {
            CaseWorkflowGuid = seeded.WorkflowGuid,
            SelectJson = SelectJson(),
            FilterJson = FilterJson(seeded.CaseKeyValue)
        };

        private Task<int> InsertRowAsync(DbContext dbContext, Seeded seeded, string user, byte? rebuild,
            DateTime? rebuildDate)
        {
            return dbContext.InsertWithInt32IdentityAsync(new Data.Poco.SessionCaseSearchCompiledSql
            {
                Guid = Guid.NewGuid(),
                CaseWorkflowGuid = seeded.WorkflowGuid,
                SelectJson = SelectJson(),
                FilterJson = FilterJson(seeded.CaseKeyValue),
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
                Rebuild = rebuild,
                RebuildDate = rebuildDate,
                Prepared = 0
            });
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
                SessionCaseSearchCompiledSqlService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantOrUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task EveryOperationWithoutPermissionThrowsForbiddenAndWritesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var rowId = await InsertRowAsync(dbContext, seeded, fx.Seed.UserWithoutPermission, 0, null);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);
            var guid = await dbContext.SessionCaseSearchCompiledSql.Where(r => r.Id == rowId).Select(r => r.Guid)
                .FirstAsync();

            var executeEx = await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteByGuidAsync(guid));
            var lastEx = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetLastAsync());
            var insertEx = await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(Request(seeded)));

            foreach (var ex in new[] { executeEx, lastEx, insertEx })
            {
                ex.Code.Should().Be("PermissionDenied");
                ex.RequiredSpecifications.Should().Equal(1);
            }

            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
            (await dbContext.SessionCaseSearchCompiledSql.CountAsync(r => r.CaseWorkflowGuid == seeded.WorkflowGuid))
                .Should().Be(1);
            (await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .CountAsync(e => e.SessionCaseSearchCompiledSqlId == rowId)).Should().Be(0);
        }

        [Fact]
        public async Task InsertCompilesValidRulesStoresTheRowAndMapsEveryFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);
            var request = Request(seeded);
            var before = DateTimeOffset.UtcNow.AddSeconds(-5);

            var dto = await service.InsertAsync(request);

            dto.Id.Should().BeGreaterThan(0);
            dto.Guid.Should().NotBeEmpty();
            dto.Prepared.Should().Be(1, $"compiled SQL must prepare cleanly, error: {dto.Error}");
            dto.Error.Should().BeNull();
            dto.CreatedUser.Should().Be(user);
            dto.CreatedDate.Should().BeAfter(before);
            dto.CaseWorkflowGuid.Should().Be(seeded.WorkflowGuid);
            dto.SelectJson.Should().Be(request.SelectJson);
            dto.FilterJson.Should().Be(request.FilterJson);
            dto.Rebuild.Should().Be(0);
            dto.RebuildDate.Should().BeNull();
            dto.NotFound.Should().BeFalse();
            JsonConvert.DeserializeObject<List<object>>(dto.FilterTokens.Required()).Required()
                .Should().Equal(seeded.CaseKeyValue, seeded.WorkflowGuid.ToString(), user);

            var row = await dbContext.SessionCaseSearchCompiledSql.FirstAsync(r => r.Id == dto.Id);
            row.Guid.Should().Be(dto.Guid);
            row.CreatedUser.Should().Be(user);
            row.SelectSqlSearch.Should().StartWith("select \"Case\".\"Id\" as \"Id\"")
                .And.Contain("as \"CaseKeyValue\"");
            row.OrderSql.Should().Be("order by \"Case\".\"CaseKeyValue\" ASC");
            row.WhereSql.Should().Contain("\"Case\".\"CaseKeyValue\" = (@1)");
            row.Prepared.Should().Be(1);
        }

        [Fact]
        public async Task InsertDoesNotHonourClientSuppliedIdentityOwnerOrTokensAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);
            var request = Request(seeded);
            request.Id = 999999;
            request.Guid = Guid.NewGuid();
            request.CreatedUser = fx.Seed.UserTenantB;
            request.FilterTokens = "[\"evil\"]";

            var dto = await service.InsertAsync(request);

            dto.Id.Should().NotBe(999999);
            dto.Guid.Should().NotBe(request.Guid);
            dto.CreatedUser.Should().Be(user);
            dto.FilterTokens.Should().NotContain("evil");
        }

        [Fact]
        public async Task InsertWithEmptyFilterRulesStoresAnUnpreparedRowWithTheDatabaseErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);
            var request = Request(seeded);
            request.FilterJson = "{\"condition\":\"AND\",\"rules\":[]}";

            var dto = await service.InsertAsync(request);

            dto.Prepared.Should().Be(0);
            dto.Error.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task InsertWithMissingRequiredFieldsThrowsValidationAndStoresNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(new SessionCaseSearchCompiledSqlDto()));

            ex.Result.Errors.Select(e => e.PropertyName).Should()
                .BeEquivalentTo("SelectJson", "FilterJson", "CaseWorkflowGuid");
            ex.Result.Errors.Select(e => e.ErrorCode).Should()
                .BeEquivalentTo("SelectJsonNotEmpty", "FilterJsonNotEmpty", "CaseWorkflowGuidNotEmpty");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
            (await dbContext.SessionCaseSearchCompiledSql.CountAsync(r => r.CaseWorkflowGuid == seeded.WorkflowGuid))
                .Should().Be(0);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.InsertAsync(null));
        }

        [Fact]
        public async Task InsertWithMalformedJsonUnknownFieldOrUnknownWorkflowFailsAsLegacyAndStoresNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);

            var malformed = Request(seeded);
            malformed.FilterJson = "{not json";
            await Assert.ThrowsAsync<JsonReaderException>(() => service.InsertAsync(malformed));

            var unknownField = Request(seeded);
            unknownField.FilterJson = FilterJson("x").Replace("\"CaseKeyValue\"", "\"NoSuchField\"");
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InsertAsync(unknownField));

            var unknownWorkflow = Request(seeded);
            unknownWorkflow.CaseWorkflowGuid = Guid.NewGuid();
            await Assert.ThrowsAsync<NullReferenceException>(() => service.InsertAsync(unknownWorkflow));

            (await dbContext.SessionCaseSearchCompiledSql.CountAsync(r => r.CaseWorkflowGuid == seeded.WorkflowGuid))
                .Should().Be(0);
        }

        [Fact]
        public async Task InsertAgainstAnotherTenantsWorkflowFailsAndStoresNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seededB = await SeedAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NullReferenceException>(() => service.InsertAsync(Request(seededB)));

            (await dbContext.SessionCaseSearchCompiledSql.CountAsync(r => r.CaseWorkflowGuid == seededB.WorkflowGuid))
                .Should().Be(0);
        }

        [Fact]
        public async Task GetLastWhenNoneExistsReturnsNotFoundPlaceholderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            var owned = await dbContext.SessionCaseSearchCompiledSql
                .CountAsync(r => r.CreatedUser == fx.Seed.UserWithPermissionNoApproveByReview);
            if (owned > 0)
            {
                return;
            }

            var dto = await service.GetLastAsync();

            dto.NotFound.Should().BeTrue();
            dto.Id.Should().Be(0);
            dto.CreatedUser.Should().BeNull();
        }

        [Fact]
        public async Task GetLastReturnsTheCallersNewestRowNeverAnotherUsersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var other = fx.Seed.UserWithPermissionNoApproveByReview;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);

            var first = await service.InsertAsync(Request(seeded));
            var second = await service.InsertAsync(Request(seeded));
            await InsertRowAsync(dbContext, seeded, other, 0, null);

            var last = await service.GetLastAsync();

            last.Id.Should().Be(second.Id).And.NotBe(first.Id);
            last.CreatedUser.Should().Be(user);
            last.Guid.Should().Be(second.Guid);
            last.NotFound.Should().BeFalse();
            last.Prepared.Should().Be(1);
        }

        [Fact]
        public async Task GetLastRebuildsAFlaggedStaleRowInPlaceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var staleId = await InsertRowAsync(dbContext, seeded, user, 1, default(DateTime));
            var service = await BuildServiceAsync(dbContext, user);

            var last = await service.GetLastAsync();

            last.Prepared.Should().Be(1, last.Error);
            last.CreatedUser.Should().Be(user);
            last.CaseWorkflowGuid.Should().Be(seeded.WorkflowGuid);
            last.Rebuild.Should().Be(0);
            last.RebuildDate.Should().NotBeNull();
            (await dbContext.SessionCaseSearchCompiledSql.CountAsync(r => r.CaseWorkflowGuid == seeded.WorkflowGuid))
                .Should().Be(1);
            (await dbContext.SessionCaseSearchCompiledSql.SingleAsync(r => r.Id == staleId)).Rebuild.Should().Be(0);
        }

        [Fact]
        public async Task GetLastRebuildsAFlaggedRowWithNullRebuildDateOnceOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var flaggedNullDate = await InsertRowAsync(dbContext, seeded, user, 1, null);
            var service = await BuildServiceAsync(dbContext, user);

            var first = await service.GetLastAsync();
            var afterFirst = await dbContext.SessionCaseSearchCompiledSql.SingleAsync(r => r.Id == flaggedNullDate);

            afterFirst.Rebuild.Should().Be(0);
            afterFirst.RebuildDate.Should().NotBeNull();
            afterFirst.Prepared.Should().Be(1, afterFirst.Error);
            first.Rebuild.Should().Be(0);

            var stampedAt = afterFirst.RebuildDate;
            await service.GetLastAsync();
            var afterSecond = await dbContext.SessionCaseSearchCompiledSql.SingleAsync(r => r.Id == flaggedNullDate);

            afterSecond.RebuildDate.Should().Be(stampedAt);
            (await dbContext.SessionCaseSearchCompiledSql.CountAsync(r => r.CaseWorkflowGuid == seeded.WorkflowGuid))
                .Should().Be(1);
        }

        [Fact]
        public async Task GetLastDoesNotStampAFailedRebuildAndRebuildsOnALaterReadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var flagged = await InsertRowAsync(dbContext, seeded, user, 1, null);
            var unreachable = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] =
                    "Host=localhost;Port=1;Database=postgres;Username=postgres;Password=postgres;Timeout=2;CommandTimeout=2;",
                ["ParserAssertSelectOnly"] = "True"
            });
            var failingService = await SessionCaseSearchCompiledSqlService.CreateAsync(dbContext, user, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), unreachable, TestLog.NoOp);

            var failed = await failingService.GetLastAsync();

            failed.Prepared.Should().Be(0);
            var afterFailure = await dbContext.SessionCaseSearchCompiledSql.SingleAsync(r => r.Id == flagged);
            afterFailure.Rebuild.Should().Be(1);
            afterFailure.RebuildDate.Should().BeNull();

            var service = await BuildServiceAsync(dbContext, user);
            var recovered = await service.GetLastAsync();

            recovered.Prepared.Should().Be(1, recovered.Error);
            var afterRecovery = await dbContext.SessionCaseSearchCompiledSql.SingleAsync(r => r.Id == flagged);
            afterRecovery.Rebuild.Should().Be(0);
            afterRecovery.RebuildDate.Should().NotBeNull();
        }

        [Fact]
        public async Task GetLastDoesNotRebuildUnflaggedRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);

            var notFlagged = await InsertRowAsync(dbContext, seeded, user, 0, null);
            (await service.GetLastAsync()).Id.Should().Be(notFlagged);

            var alreadyRebuilt = await InsertRowAsync(dbContext, seeded, user, 1, DateTime.UtcNow);
            var last = await service.GetLastAsync();

            last.Id.Should().Be(alreadyRebuilt);
            last.Prepared.Should().Be(0);
            (await dbContext.SessionCaseSearchCompiledSql.CountAsync(r => r.CaseWorkflowGuid == seeded.WorkflowGuid))
                .Should().Be(2);
        }

        [Fact]
        public async Task ExecuteByGuidReturnsRowsAndWritesOneExecutionLogRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);
            var compiled = await service.InsertAsync(Request(seeded));

            var result = await service.ExecuteByGuidAsync(compiled.Guid);

            result.Rows.Should().ContainSingle();
            result.Rows[0]["Id"].Should().Be(seeded.CaseId);
            result.Rows[0]["CaseKeyValue"].Should().Be(seeded.CaseKeyValue);
            result.Rows[0]["BackColor"].Should().Be("#445566");
            result.Rows[0]["ForeColor"].Should().Be("#112233");
            result.Schema["CaseKeyValue"].Should().Be("string");
            result.Schema["Id"].Should().Be("number");

            var executions = await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .Where(e => e.SessionCaseSearchCompiledSqlId == compiled.Id).ToListAsync();
            var execution = executions.Should().ContainSingle().Subject;
            execution.Records.Should().Be(1);
            execution.CreatedUser.Should().Be(user);
            execution.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            execution.ResponseTime.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task ExecuteByGuidWithNoMatchingCaseReturnsEmptyRowsAndLogsZeroRecordsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);
            var request = Request(seeded);
            request.FilterJson = FilterJson("no-such-case-key");
            var compiled = await service.InsertAsync(request);

            var result = await service.ExecuteByGuidAsync(compiled.Guid);

            result.Rows.Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .SingleAsync(e => e.SessionCaseSearchCompiledSqlId == compiled.Id)).Records.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteByGuidOfUnknownGuidIsNotFoundAndWritesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteByGuidAsync(Guid.NewGuid()));

            ex.Code.Should().Be("NotFound");
        }

        [Fact]
        public async Task ExecuteByGuidIsOwnerScopedSameTenantOtherUserAndOtherTenantCannotExecuteAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, owner);
            var compiled = await (await BuildServiceAsync(dbContext, owner)).InsertAsync(Request(seeded));

            foreach (var intruder in new[] { fx.Seed.UserWithPermissionNoApproveByReview, fx.Seed.UserTenantB })
            {
                var service = await BuildServiceAsync(dbContext, intruder);
                await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteByGuidAsync(compiled.Guid));
            }

            (await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .CountAsync(e => e.SessionCaseSearchCompiledSqlId == compiled.Id)).Should().Be(0);
        }

        [Fact]
        public async Task ExecuteByGuidRebuildsAFlaggedStaleRowInPlaceAndExecutesItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var staleId = await InsertRowAsync(dbContext, seeded, user, 1, null);
            var staleGuid = await dbContext.SessionCaseSearchCompiledSql.Where(r => r.Id == staleId)
                .Select(r => r.Guid).FirstAsync();
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.ExecuteByGuidAsync(staleGuid);

            result.Rows.Should().ContainSingle();
            var rebuilt = await dbContext.SessionCaseSearchCompiledSql
                .Where(r => r.CaseWorkflowGuid == seeded.WorkflowGuid).SingleAsync();
            rebuilt.Id.Should().Be(staleId);
            rebuilt.Guid.Should().Be(staleGuid);
            rebuilt.Rebuild.Should().Be(0);
            rebuilt.RebuildDate.Should().NotBeNull();
            (await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .SingleAsync(e => e.SessionCaseSearchCompiledSqlId == rebuilt.Id)).Records.Should().Be(1);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAndWritesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedAsync(dbContext, user);
            var compiled = await (await BuildServiceAsync(dbContext, user)).InsertAsync(Request(seeded));
            var service = await BuildServiceAsync(dbContext, user);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ExecuteByGuidAsync(compiled.Guid, cts.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetLastAsync(cts.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.InsertAsync(Request(seeded), cts.Token));

            (await dbContext.SessionCaseSearchCompiledSql.CountAsync(r => r.CaseWorkflowGuid == seeded.WorkflowGuid, token: cts.Token))
                .Should().Be(1);
            (await dbContext.GetTable<Data.Poco.SessionCaseSearchCompiledSqlExecution>()
                .CountAsync(e => e.SessionCaseSearchCompiledSqlId == compiled.Id, token: cts.Token)).Should().Be(0);
        }

        [Fact]
        public async Task EachOperationEmitsOneAuditLineOneSpanAndNoChangeEventAsync()
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

            var compiled = await service.InsertAsync(Request(seeded));
            await service.GetLastAsync();
            await service.ExecuteByGuidAsync(compiled.Guid);

            auditLog.Entries.Should().HaveCount(3);
            auditLog.Entries.Select(e => e.Message).Should().Contain(m => m.Contains("op=Insert"))
                .And.Contain(m => m.Contains("op=GetLast")).And.Contain(m => m.Contains("op=ExecuteByGuid"));
            auditLog.Entries.Should().OnlyContain(e =>
                e.Message.Contains("area=SessionCaseSearchCompiledSql") && e.Message.Contains("outcome=ok"));
            foreach (var name in new[] { "Insert", "GetLast", "ExecuteByGuid" })
            {
                activities.Should().ContainSingle(a => a.OperationName == $"SessionCaseSearchCompiledSql.{name}");
            }

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ForbiddenAndNotFoundAuditTheirOutcomeAndPublishNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var auditForbidden = new TestLog();
            var auditNotFound = new TestLog();
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                auditLog: auditForbidden, serviceChangeBus: bus);
            var notFound = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: auditNotFound, serviceChangeBus: bus);

            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetLastAsync());
            await Assert.ThrowsAsync<NotFoundException>(() => notFound.ExecuteByGuidAsync(Guid.NewGuid()));

            auditForbidden.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=forbidden");
            auditNotFound.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=not_found");
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void OnlyTheSafeLastSearchReadIsPublishedAsAnAgentTool()
        {
            var attribute = typeof(SessionCaseSearchCompiledSqlService)
                .GetMethod(nameof(SessionCaseSearchCompiledSqlService.GetLastAsync)).Required()
                .GetCustomAttribute<ServiceOperationAttribute>().Required();
            attribute.Name.Should().Be("SessionCaseSearchCompiledSqlGetLast");
            attribute.Kind.Should().Be(OperationKind.Read);
            attribute.Idempotent.Should().BeTrue();

            foreach (var method in new[]
                     {
                         nameof(SessionCaseSearchCompiledSqlService.ExecuteByGuidAsync),
                         nameof(SessionCaseSearchCompiledSqlService.InsertAsync)
                     })
            {
                typeof(SessionCaseSearchCompiledSqlService).GetMethod(method).Required()
                    .GetCustomAttribute<ServiceOperationAttribute>().Should().BeNull();
            }

            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("SessionCaseSearchCompiledSqlGetLast");
            names.Where(n => n.StartsWith("SessionCaseSearchCompiledSql")).Should().ContainSingle();
        }
    }
}