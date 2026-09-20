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
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Jube.Test.Security.DynamicSql.Models;

namespace Jube.Test.Security.DynamicSql;

internal sealed class CaseSeedData(DatabaseFixture fx)
{
    private readonly List<int> caseIds = [];
    private readonly List<int> workflowIds = [];
    private readonly List<int> statusIds = [];
    private readonly List<int> modelIds = [];
    private readonly List<long> archiveIds = [];
    private readonly List<Guid> workflowGuids = [];

    public async Task<Seeded> AddCaseAsync(string ownerUser, string? caseKeyValue = null)
    {
        await using var db = fx.GetDbContext();
        var tenantId = (await db.UserInTenant.FirstAsync(u => u.User == ownerUser)).TenantRegistryId;
        var modelGuid = Guid.NewGuid();
        var modelId = await db.InsertWithInt32IdentityAsync(new EntityAnalysisModel
        {
            Name = $"{DatabaseFixture.Prefix}W12Model{Guid.NewGuid():N}"[..40],
            Guid = modelGuid,
            TenantRegistryId = tenantId,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            CreatedDate = DateTime.UtcNow,
            CreatedUser = DatabaseFixture.Prefix
        });
        modelIds.Add(modelId);

        var role = await db.UserRegistry.Where(u => u.Name == ownerUser).Select(u => u.RoleRegistryGuid).FirstAsync();
        var workflowGuid = Guid.NewGuid();
        var workflowId = await db.InsertWithInt32IdentityAsync(new CaseWorkflow
        {
            Name = $"{DatabaseFixture.Prefix}W12Flow{Guid.NewGuid():N}"[..40],
            Guid = workflowGuid,
            EntityAnalysisModelId = modelId,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            CreatedUser = ownerUser,
            CreatedDate = DateTime.UtcNow
        });
        workflowIds.Add(workflowId);
        workflowGuids.Add(workflowGuid);
        await db.InsertAsync(new CaseWorkflowRole
        {
            CaseWorkflowGuid = workflowGuid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = role,
            CreatedUser = ownerUser,
            CreatedDate = DateTime.UtcNow,
            Deleted = 0
        });

        var statusGuid = Guid.NewGuid();
        statusIds.Add(await db.InsertWithInt32IdentityAsync(new CaseWorkflowStatus
        {
            Name = $"{DatabaseFixture.Prefix}W12Status{Guid.NewGuid():N}"[..40],
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
        }));
        await db.InsertAsync(new CaseWorkflowStatusRole
        {
            CaseWorkflowStatusGuid = statusGuid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = role,
            CreatedUser = ownerUser,
            CreatedDate = DateTime.UtcNow,
            Deleted = 0
        });

        var key = caseKeyValue ?? $"{DatabaseFixture.Prefix}W12Ckv{Guid.NewGuid():N}";
        var caseId = await db.InsertWithInt32IdentityAsync(new Jube.Data.Poco.Case
        {
            EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
            CaseWorkflowGuid = workflowGuid,
            CaseWorkflowStatusGuid = statusGuid,
            CreatedDate = DateTime.UtcNow,
            Locked = 0,
            ClosedStatusId = 0,
            CaseKey = "AccountId",
            CaseKeyValue = key,
            Diary = 0,
            Rating = 3,
            LastClosedStatus = 0,
            Json = "{}"
        });
        caseIds.Add(caseId);
        return new Seeded(workflowGuid, key, caseId, modelId, modelGuid);
    }

    public async Task AddXpathAsync(int modelId, string name, int dataTypeId)
    {
        await using var db = fx.GetDbContext();
        await db.InsertAsync(new EntityAnalysisModelRequestXpath
        {
            EntityAnalysisModelId = modelId,
            Name = name,
            DataTypeId = dataTypeId,
            XPath = $"$.{name}",
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            Guid = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            CreatedUser = DatabaseFixture.Prefix
        });
    }

    public async Task AddArchiveAsync(int modelId, Guid modelGuid, string payloadJson, DateTime referenceDate)
    {
        await using var db = fx.GetDbContext();
        var entry = Guid.NewGuid();
        var json =
            $"{{\"entityAnalysisModelInstanceEntryGuid\":\"{entry}\",\"responseElevation\":{{\"value\":12.5}}," +
            $"\"prevailingEntityAnalysisModelActivationRuleId\":7,\"entityAnalysisModelGuid\":\"{modelGuid}\"," +
            "\"entityAnalysisModelActivationRuleCount\":3,\"createdDate\":\"2026-03-01T10:00:00Z\"," +
            $"\"referenceDate\":\"{referenceDate:yyyy-MM-ddTHH:mm:ssZ}\",\"payload\":{payloadJson}}}";
        archiveIds.Add(await db.InsertWithInt64IdentityAsync(new Archive
        {
            Json = json,
            EntityAnalysisModelInstanceEntryGuid = entry,
            EntityAnalysisModelId = modelId,
            CreatedDate = DateTime.UtcNow,
            ReferenceDate = referenceDate,
            ResponseElevation = 12.5,
            ActivationRuleCount = 3,
            EntryKeyValue = "ZzW12Entry"
        }));
    }

    public async Task DisposeAsync()
    {
        await using var db = fx.GetDbContext();
        var compiledIds = db.SessionCaseSearchCompiledSql.Where(w => workflowGuids.Contains(w.CaseWorkflowGuid))
            .Select(w => w.Id);
        await db.GetTable<SessionCaseSearchCompiledSqlExecution>()
            .Where(w => compiledIds.Contains(w.SessionCaseSearchCompiledSqlId)).DeleteAsync();
        await db.SessionCaseSearchCompiledSql.Where(w => workflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
        await db.GetTable<CaseEvent>().Where(w => caseIds.Contains(w.CaseId ?? 0)).DeleteAsync();
        await db.Case.Where(w => caseIds.Contains(w.Id)).DeleteAsync();
        var statusGuids = db.CaseWorkflowStatus.Where(s => statusIds.Contains(s.Id)).Select(s => s.Guid);
        await db.GetTable<CaseWorkflowStatusRole>().Where(w => statusGuids.Contains(w.CaseWorkflowStatusGuid))
            .DeleteAsync();
        await db.GetTable<CaseWorkflowRole>().Where(w => workflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
        await db.CaseWorkflowStatus.Where(w => statusIds.Contains(w.Id)).DeleteAsync();
        await db.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();
        var models = modelIds.Select(id => (int?)id).ToList();
        await db.Archive.Where(w => archiveIds.Contains(w.Id)).DeleteAsync();
        await db.GetTable<EntityAnalysisModelSampleExecutionLog>().Where(w => models.Contains(w.EntityAnalysisModelId))
            .DeleteAsync();
        await db.EntityAnalysisModelRequestXpath.Where(w => models.Contains(w.EntityAnalysisModelId)).DeleteAsync();
        await db.GetTable<EntityAnalysisModelVersion>().Where(w => modelIds.Contains(w.EntityAnalysisModelId))
            .DeleteAsync();
        await db.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
    }
}