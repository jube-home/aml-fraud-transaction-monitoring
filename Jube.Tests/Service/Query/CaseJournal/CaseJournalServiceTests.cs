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
using System.Globalization;
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
using Jube.Service.Exceptions.Query.CaseJournal;
using Jube.Service.Observability;
using Jube.Service.Query.CaseJournal;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseJournal
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseJournalServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string DrillName = "AccountId";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<long> createdArchiveIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdXPathIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.Archive.Where(w => createdArchiveIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowXPathGuids = dbContext.CaseWorkflowXPath.Where(s => createdXPathIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathRole>()
                .Where(w => caseWorkflowXPathGuids.Contains(w.CaseWorkflowXPathGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathVersion>()
                .Where(w => w.CaseWorkflowXPathId != null && createdXPathIds.Contains(w.CaseWorkflowXPathId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflowXPath.Where(w => createdXPathIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowGuids =
                dbContext.CaseWorkflow.Where(s => createdWorkflowIds.Contains(s.Id)).Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseJournalService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseJournalService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp, false,
                Environment.GetEnvironmentVariable("JubeTestConnectionString"));
        }

        private static string NewKeyValue() => $"{DatabaseFixture.Prefix}Acct{Guid.NewGuid():N}";

        private async Task<int> CreateModelAsync(DbContext dbContext, string createdUser)
        {
            var saved = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                }).ConfigureAwait(false);
            createdModelIds.Add(saved.Id);
            return saved.Id;
        }

        private async Task<Guid> CreateWorkflowAsync(DbContext dbContext, int modelId, string user,
            bool grantRoleToUser)
        {
            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = guid,
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);
            createdWorkflowIds.Add(id);

            if (!grantRoleToUser)
            {
                return guid;
            }

            var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == user)
                .Select(u => u.RoleRegistryGuid).FirstAsync();
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = guid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return guid;
        }

        private async Task CreateXPathAsync(DbContext dbContext, Guid workflowGuid, string user, string name,
            string xPath, bool grantRoleToUser, bool boldLine = false, bool regexFormat = false)
        {
            var workflowId = await dbContext.CaseWorkflow.Where(w => w.Guid == workflowGuid).Select(w => w.Id)
                .FirstAsync();
            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowXPath
            {
                Guid = guid,
                CaseWorkflowId = workflowId,
                Name = name,
                XPath = xPath,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Drill = 0,
                Version = 1,
                BoldLineMatched = (byte)(boldLine ? 1 : 0),
                BoldLineFormatBackColor = "#111111",
                BoldLineFormatForeColor = "#222222",
                ConditionalRegularExpressionFormatting = (byte)(regexFormat ? 1 : 0),
                RegularExpression = "^Match",
                ConditionalFormatBackColor = "#333333",
                ConditionalFormatForeColor = "#444444",
                ForeRowColorScope = 1,
                BackRowColorScope = 0,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);
            createdXPathIds.Add(id);

            if (grantRoleToUser)
            {
                var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == user)
                    .Select(u => u.RoleRegistryGuid).FirstAsync();
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowXPathRole
                {
                    CaseWorkflowXPathGuid = guid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = roleGuid,
                    CreatedUser = user,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                });
            }
        }

        private async Task<(long Id, Guid Guid, DateTime Created)> CreateArchiveAsync(DbContext dbContext,
            int modelId, string keyValue, double responseElevation = 5, int activationRuleCount = 0,
            JObject? payload = null, object[]? tags = null)
        {
            var guid = Guid.NewGuid();
            var created = DateTime.UtcNow;
            payload ??= new JObject();
            payload[DrillName] = keyValue;
            var json = new JObject
            {
                ["payload"] = payload,
                ["tag"] = new JArray(tags ?? [])
            }.ToString(Newtonsoft.Json.Formatting.None);

            var id = await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json,
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = modelId,
                EntryKeyValue = keyValue,
                ResponseElevation = responseElevation,
                ActivationRuleCount = activationRuleCount,
                CreatedDate = created,
                ReferenceDate = created,
            }).ConfigureAwait(false);
            createdArchiveIds.Add(id);
            return (id, guid, created);
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
                CaseJournalService.CreateAsync(dbContext, userName, log, localizers, new NullServiceChangeBus(),
                    TestLog.NoOp, false, null));

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
        public async Task GetThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetAsync(DrillName, "x", Guid.NewGuid(), 10, false, 0));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([1]);
        }

        [Fact]
        public async Task ForbiddenDoesNotTouchArchiveDataAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            await CreateArchiveAsync(dbContext, modelId, keyValue);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetAsync(DrillName, keyValue, Guid.NewGuid(), 10, false, 0));
        }

        [Fact]
        public async Task GetReturnsSeededArchiveRowFieldByFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            var (id, guid, created) = await CreateArchiveAsync(dbContext, modelId, keyValue, 7.5, 2,
                tags: ["Alpha", "Beta"]);
            var workflowGuid = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission, true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(DrillName, keyValue, workflowGuid, 10, false, 0);

            dto.Rows.Should().ContainSingle();
            var row = dto.Rows.Required()[0];
            Convert.ToInt64(row["Id"]).Should().Be(id);
            $"{row["EntityAnalysisModelInstanceEntryGuid"]}".Should().Be(guid.ToString());
            row["EntryKeyValue"].Should().Be(keyValue);
            Convert.ToDouble(row["ResponseElevation"]).Should().Be(7.5);
            row["Activation"].Should().BeNull();
            row["CreatedDate"].Should().NotBeNull();
            row["ReferenceDate"].Should().NotBeNull();
            ((JArray)row["Tag"].Required()).Select(t => t.ToString()).Should().BeEquivalentTo(["Alpha", "Beta"]);
            var expectedUtc = new DateTime(created.Ticks - created.Ticks % 10, DateTimeKind.Utc);
            foreach (var column in new[] { "CreatedDate", "ReferenceDate" })
            {
                var offset = row[column].Should().BeOfType<DateTimeOffset>().Subject;
                offset.Offset.Should().Be(TimeSpan.Zero);
                offset.UtcDateTime.Should().Be(expectedUtc);
            }

            dto.Schema.Should().ContainKeys("Id", "Activation", "EntityAnalysisModelInstanceEntryGuid",
                "ReferenceDate", "CreatedDate", "ResponseElevation", "EntryKeyValue", "Tag");
            var schema = dto.Schema.Required();
            schema["Tag"].Should().Be("array");
            schema["Id"].Should().Be("number");
        }

        [Fact]
        public async Task GetReturnsNewestFirstAndHonoursLimitAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            var (first, _, _) = await CreateArchiveAsync(dbContext, modelId, keyValue);
            var (second, _, _) = await CreateArchiveAsync(dbContext, modelId, keyValue);
            var (third, _, _) = await CreateArchiveAsync(dbContext, modelId, keyValue);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var all = await service.GetAsync(DrillName, keyValue, Guid.NewGuid(), 10, false, 0);
            var limited = await service.GetAsync(DrillName, keyValue, Guid.NewGuid(), 2, false, 0);

            all.Rows.Required().Select(r => Convert.ToInt64(r["Id"])).Should().Equal(third, second, first);
            limited.Rows.Required().Select(r => Convert.ToInt64(r["Id"])).Should().Equal(third, second);
        }

        [Fact]
        public async Task GetReturnsEmptyRowsButFixedSchemaWhenNothingMatchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(DrillName, NewKeyValue(), Guid.NewGuid(), 10, false, 0);

            dto.Rows.Should().BeEmpty();
            dto.Schema.Should().ContainKey("Id");
        }

        [Fact]
        public async Task ActivationsOnlyAndResponseElevationFiltersApplyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            var (noActivationLow, _, _) = await CreateArchiveAsync(dbContext, modelId, keyValue, 1);
            var (activatedLow, _, _) = await CreateArchiveAsync(dbContext, modelId, keyValue, 2, 1);
            var (activatedHigh, _, _) = await CreateArchiveAsync(dbContext, modelId, keyValue, 9, 3);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var everything = await service.GetAsync(DrillName, keyValue, Guid.NewGuid(), 10, false, 0);
            var activationsOnly = await service.GetAsync(DrillName, keyValue, Guid.NewGuid(), 10, true, 0);
            var elevated = await service.GetAsync(DrillName, keyValue, Guid.NewGuid(), 10, false, 5);

            everything.Rows.Required().Select(r => Convert.ToInt64(r["Id"]))
                .Should().BeEquivalentTo([noActivationLow, activatedLow, activatedHigh]);
            activationsOnly.Rows.Required().Select(r => Convert.ToInt64(r["Id"]))
                .Should().BeEquivalentTo([activatedLow, activatedHigh]);
            elevated.Rows.Required().Select(r => Convert.ToInt64(r["Id"])).Should().BeEquivalentTo([activatedHigh]);
        }

        [Fact]
        public async Task DrillNameAndValueMustBothMatchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            await CreateArchiveAsync(dbContext, modelId, keyValue);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync("OtherKey", keyValue, Guid.NewGuid(), 10, false, 0)).Rows.Should().BeEmpty();
            (await service.GetAsync(DrillName, keyValue + "x", Guid.NewGuid(), 10, false, 0)).Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBArchiveIsInvisibleToTenantAAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelB = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var keyValue = NewKeyValue();
            var (idA, _, _) = await CreateArchiveAsync(dbContext, modelA, keyValue);
            var (idB, _, _) = await CreateArchiveAsync(dbContext, modelB, keyValue);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var fromA = await serviceA.GetAsync(DrillName, keyValue, Guid.NewGuid(), 10, false, 0);
            var fromB = await serviceB.GetAsync(DrillName, keyValue, Guid.NewGuid(), 10, false, 0);

            fromA.Rows.Required().Select(r => Convert.ToInt64(r["Id"])).Should().Equal(idA);
            fromB.Rows.Required().Select(r => Convert.ToInt64(r["Id"])).Should().Equal(idB);
        }

        [Fact]
        public async Task TenantBWorkflowXPathColumnsAreNotExposedToTenantAAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelB = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var keyValue = NewKeyValue();
            await CreateArchiveAsync(dbContext, modelA, keyValue, payload: new JObject { ["Amount"] = 1 });
            var workflowB = await CreateWorkflowAsync(dbContext, modelB, fx.Seed.UserTenantB, true);
            await CreateXPathAsync(dbContext, workflowB, fx.Seed.UserTenantB, $"{DatabaseFixture.Prefix}TbCol",
                "payload.Amount", true);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = await serviceA.GetAsync(DrillName, keyValue, workflowB, 10, false, 0);

            dto.Schema.Should().NotContainKey($"{DatabaseFixture.Prefix}TbCol");
            dto.Rows.Should().ContainSingle().Which.Should().NotContainKey($"{DatabaseFixture.Prefix}TbCol");
        }

        [Fact]
        public async Task PermittedXPathBecomesTypedColumnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            await CreateArchiveAsync(dbContext, modelId, keyValue,
                payload: new JObject { ["Amount"] = 42, ["Note"] = "hello" });
            var workflowGuid = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission, true);
            var amountCol = $"{DatabaseFixture.Prefix}Amount";
            var missingCol = $"{DatabaseFixture.Prefix}Missing";
            await CreateXPathAsync(dbContext, workflowGuid, fx.Seed.UserWithPermission, amountCol,
                "payload.Amount", true);
            await CreateXPathAsync(dbContext, workflowGuid, fx.Seed.UserWithPermission, missingCol,
                "payload.DoesNotExist", true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(DrillName, keyValue, workflowGuid, 10, false, 0);

            var row = dto.Rows.Should().ContainSingle().Subject;
            Convert.ToInt64(row[amountCol]).Should().Be(42);
            var schema = dto.Schema.Required();
            schema[amountCol].Should().Be("number");
            row.Should().ContainKey(missingCol);
            row[missingCol].Should().BeNull();
            schema[missingCol].Should().Be("string");
        }

        [Fact]
        public async Task XPathWithoutRoleGrantIsExcludedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            await CreateArchiveAsync(dbContext, modelId, keyValue, payload: new JObject { ["Amount"] = 42 });
            var workflowGuid = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission, true);
            var col = $"{DatabaseFixture.Prefix}NoRole";
            await CreateXPathAsync(dbContext, workflowGuid, fx.Seed.UserWithPermission, col, "payload.Amount", false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(DrillName, keyValue, workflowGuid, 10, false, 0);

            dto.Schema.Should().NotContainKey(col);
            dto.Rows.Should().ContainSingle().Which.Should().NotContainKey(col);
        }

        [Fact]
        public async Task XPathOfWorkflowWithoutWorkflowRoleGrantIsExcludedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            await CreateArchiveAsync(dbContext, modelId, keyValue, payload: new JObject { ["Amount"] = 42 });
            var workflowGuid = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission, false);
            var col = $"{DatabaseFixture.Prefix}NoWfRole";
            await CreateXPathAsync(dbContext, workflowGuid, fx.Seed.UserWithPermission, col, "payload.Amount", true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(DrillName, keyValue, workflowGuid, 10, false, 0);

            dto.Schema.Should().NotContainKey(col);
        }

        [Fact]
        public async Task BoldLineAndCellFormatAreEmittedWhenXPathMatchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var keyValue = NewKeyValue();
            await CreateArchiveAsync(dbContext, modelId, keyValue, payload: new JObject { ["Note"] = "Matching" });
            var workflowGuid = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission, true);
            var col = $"{DatabaseFixture.Prefix}Fmt";
            await CreateXPathAsync(dbContext, workflowGuid, fx.Seed.UserWithPermission, col, "payload.Note", true,
                true, true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(DrillName, keyValue, workflowGuid, 10, false, 0);

            var row = dto.Rows.Should().ContainSingle().Subject;
            row[col].Should().Be("Matching");
            var boldLine = JObject.FromObject(row["BoldLine"].Required());
            boldLine["BoldLineKey"].Required().ToString().Should().Be(col);
            boldLine["BoldLineFormatBackColor"].Required().ToString().Should().Be("#111111");
            boldLine["BoldLineFormatForeColor"].Required().ToString().Should().Be("#222222");
            var cellFormat = JArray.FromObject(row["CellFormat"].Required()).Should().ContainSingle().Subject;
            cellFormat["CellFormatKey"].Required().ToString().Should().Be(col);
            cellFormat["CellFormatBackColor"].Required().ToString().Should().Be("#333333");
            cellFormat["CellFormatForeColor"].Required().ToString().Should().Be("#444444");
            cellFormat["CellFormatForeRow"].Required().Value<bool>().Should().BeTrue();
            cellFormat["CellFormatBackRow"].Required().Value<bool>().Should().BeFalse();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.GetAsync(DrillName, "x", Guid.NewGuid(), 10, false, 0, cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetAsync(DrillName, "x", Guid.NewGuid(), 10, false, 0));

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

            await Assert.ThrowsAnyAsync<Exception>(() =>
                service.GetAsync(DrillName, "x", Guid.NewGuid(), 10, false, 0));

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync(DrillName, NewKeyValue(), Guid.NewGuid(), 10, false, 0);

            var span = activities.Should().ContainSingle(a => a.OperationName == "CaseJournal.Get").Subject;
            span.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync(DrillName, NewKeyValue(), Guid.NewGuid(), 10, false, 0);

            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Get");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(DrillName, NewKeyValue(), Guid.NewGuid(), 10, false, 0);

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetAsync(DrillName, NewKeyValue(), Guid.NewGuid(), 10, false, 0);
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                forbidden.GetAsync(DrillName, "x", Guid.NewGuid(), 10, false, 0));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueDescriptorHasUniquePascalCaseNoUnderscoreNameAndOperationAttributeMatches()
        {
            var tools = new List<ServiceToolDescriptor>();
            typeof(ServiceToolCatalogue).GetMethod("AddCaseJournal",
                BindingFlags.NonPublic | BindingFlags.Static).Required().Invoke(null, [tools]);

            tools.Should().ContainSingle().Which.Name.Should().Be("CaseJournalGet");
            tools[0].Name.Should().NotContain("_");
            tools[0].Kind.Should().Be(OperationKind.Read);
            ServiceToolCatalogue.All.Select(t => t.Name).Should().OnlyHaveUniqueItems();

            var attribute = typeof(CaseJournalService).GetMethod(nameof(CaseJournalService.GetAsync)).Required()
                .GetCustomAttribute<ServiceOperationAttribute>().Required();
            attribute.Name.Should().Be("CaseJournalGet");
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
                    service.GetAsync(DrillName, "x", Guid.NewGuid(), 10, false, 0));

                localizers.Create(typeof(CaseJournalResources))[CaseJournalResources.PermissionDenied].Value
                    .Should().Be(ex.Message);
                ex.Message.Should().StartWith("Vous");
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}