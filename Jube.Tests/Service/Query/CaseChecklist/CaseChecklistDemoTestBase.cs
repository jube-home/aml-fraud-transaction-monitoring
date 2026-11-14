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
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Dto.Repository.Case;
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist.Interfaces;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Case;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Query.CaseChecklist
{
    public abstract class CaseChecklistDemoTestBase(DatabaseFixture fx) : IAsyncLifetime
    {
        protected const string Administrator = "Administrator";
        protected const string WorkflowName = "Detailed Account Financial Transactions Cases";
        protected const string FirstLine = "First Line Review";
        protected const string Supervisor = "Supervisor Review";

        protected static readonly DateTime Start = new(2026, 11, 4, 9, 0, 0, DateTimeKind.Utc);

        protected static readonly IStringLocalizerFactory Localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        protected static readonly JubeDynamicEnvironment DynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = Connection(),
                ["ReportConnectionString"] = Connection(),
            });

        private static readonly EngineJsonSerializationHelper jsonSerializationHelper = new();
        private readonly List<int> caseIds = [];

        protected DatabaseFixture Fx { get; } = fx;
        protected CaseChecklistTestUsers Users { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await using var dbContext = Fx.GetDbContext();

            Users = await CaseChecklistTestUsers.CreateAsync(dbContext, WorkflowName, 1);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = Fx.GetDbContext();

            await dbContext.CaseChecklistOverride.Where(w => caseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseChecklistEvent.Where(w => caseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseChecklistItem.Where(w => caseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseEvent.Where(w => w.CaseId != null && caseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.CaseNote.Where(w => w.CaseId != null && caseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.CaseStatusTransition.Where(w => caseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.Case.Where(w => caseIds.Contains(w.Id)).DeleteAsync();
            await Users.DeleteAsync(dbContext);
        }

        protected static string Connection()
        {
            return Environment.GetEnvironmentVariable("JubeTestConnectionString")
                   ?? Environment.GetEnvironmentVariable("ConnectionString")
                   ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;";
        }

        protected static string UniqueKey() => $"{DatabaseFixture.Prefix}Chk{Guid.NewGuid():N}"[..30];

        protected static JubeDynamicEnvironment EnvironmentWith(string key, string value)
        {
            return TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = Connection(),
                ["ReportConnectionString"] = Connection(),
                [key] = value,
            });
        }

        protected static Task<CaseChecklistService> ChecklistAsync(DbContext dbContext, string user,
            TimeProvider clock, IServiceChangeBus? serviceChangeBus = null,
            JubeDynamicEnvironment? environment = null)
        {
            return CaseChecklistService.CreateAsync(dbContext, user, TestLog.NoOp, Localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), environment ?? DynamicEnvironment, TestLog.NoOp,
                clock);
        }

        protected static Task<CaseService> CasesAsync(DbContext dbContext, string user,
            IServiceChangeBus? serviceChangeBus = null,
            Func<DbContext, ICaseChecklistReconciler>? checklistReconcilerFactory = null)
        {
            return CaseService.CreateAsync(dbContext, user, TestLog.NoOp, Localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), DynamicEnvironment, jsonSerializationHelper,
                TestLog.NoOp, checklistReconcilerFactory);
        }

        protected async Task<int> CreateCaseAsync(DbContext dbContext, string statusName, string key,
            decimal? amount, string? lockedUser = null, byte closedStatusId = 0)
        {
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Name == WorkflowName)
                .Select(s => s.Guid).FirstAsync();

            var statusGuid = await StatusGuidAsync(dbContext, statusName);

            var json = amount.HasValue
                ? $"{{\"payload\":{{\"CurrencyAmount\":\"{amount.Value}\"}}}}"
                : "{\"payload\":{}}";

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = "AccountId",
                CaseKeyValue = key,
                Json = json,
                CreatedDate = Start,
                Locked = (byte)(lockedUser == null ? 0 : 1),
                LockedUser = lockedUser,
                LockedDate = lockedUser == null ? null : Start,
                ClosedStatusId = closedStatusId,
                Rating = 0,
                LastClosedStatus = 0,
            });
            caseIds.Add(id);

            return id;
        }

        protected static Task<Guid> StatusGuidAsync(DbContext dbContext, string statusName)
        {
            return (from s in dbContext.CaseWorkflowStatus
                join w in dbContext.CaseWorkflow on s.CaseWorkflowId equals w.Id
                where w.Name == WorkflowName && s.Name == statusName
                select s.Guid).FirstAsync();
        }

        protected static Task<long> ItemIdAsync(DbContext dbContext, int caseId, string itemName)
        {
            return dbContext.CaseChecklistItem.Where(w => w.CaseId == caseId && w.ItemName == itemName)
                .Select(s => s.Id).FirstAsync();
        }

        protected static Task<int> AuditCountAsync(DbContext dbContext, int caseId, Jube.Dto.Options.CaseChecklistAction action)
        {
            return dbContext.CaseChecklistEvent.CountAsync(w => w.CaseId == caseId && w.ActionId == (byte)action);
        }

        protected static Task<int> TimelineCountAsync(DbContext dbContext, int caseId, int eventTypeId)
        {
            return dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId && w.CaseEventTypeId == eventTypeId);
        }

        protected static CaseDto Move(int caseId, Guid statusGuid, byte closedStatusId, string? lockedUser)
        {
            return new CaseDto
            {
                Id = caseId,
                CaseWorkflowStatusGuid = statusGuid,
                ClosedStatusId = closedStatusId,
                Locked = lockedUser != null,
                LockedUser = lockedUser,
                Rating = 1,
                Payload = "{}"
            };
        }

        protected static async Task AddNoteAsync(DbContext dbContext, int caseId, string user, bool finding = false)
        {
            await dbContext.InsertAsync(new Data.Poco.CaseNote
            {
                CaseId = caseId,
                Note = "Checklist test note",
                CaseKey = "AccountId",
                CaseKeyValue = "n/a",
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
                ActionId = 0,
                PriorityId = 0,
                IsFinding = finding
            });
        }
    }
}
