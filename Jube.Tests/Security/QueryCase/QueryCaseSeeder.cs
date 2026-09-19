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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using PocoCase = Jube.Data.Poco.Case;

namespace Jube.Test.Security.QueryCase;

public sealed class QueryCaseSeeder(DatabaseFixture fx)
{
    private readonly List<int> actionIds = [];
    private readonly List<int> activationRuleIds = [];
    private readonly List<int> activationRuleSuppressionIds = [];
    private readonly List<int> caseIds = [];
    private readonly List<int> displayIds = [];
    private readonly List<int> entryIds = [];
    private readonly List<int> formIds = [];
    private readonly List<int> inlineFunctionIds = [];
    private readonly List<int> macroIds = [];
    private readonly List<int> modelIds = [];
    private readonly List<int> noteIds = [];
    private readonly List<int> scheduleIds = [];
    private readonly List<int> statusIds = [];
    private readonly List<int> suppressionIds = [];
    private readonly List<int> workflowIds = [];
    private readonly List<int> xpathIds = [];
    private readonly List<int> syncEntryIds = [];

    public static string Unique(string label) => $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..32];

    public Task<Guid> RoleOfAsync(DbContext dbContext, string userName) =>
        dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

    public async Task<int> TenantOfAsync(string userName)
    {
        await using var dbContext = fx.GetDbContext();
        return await dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId)
            .FirstAsync();
    }

    public async Task<int> CountEventsAsync(int caseId)
    {
        await using var dbContext = fx.GetDbContext();
        return await dbContext.GetTable<CaseEvent>().CountAsync(w => w.CaseId == caseId);
    }

    public async Task<CaseGraph> CaseAsync(string owner, string? key = null, string? value = null,
        string json = "{}", string[]? workflowRoles = null, string[]? statusRoles = null,
        bool workflowDeleted = false, bool statusDeleted = false, bool modelDeleted = false)
    {
        await using var dbContext = fx.GetDbContext();
        workflowRoles ??= [owner];
        statusRoles ??= [owner];
        key ??= "AccountId";
        value ??= Unique("Val");

        var model = await new EntityAnalysisModelRepository(dbContext, owner).InsertAsync(
            new EntityAnalysisModel
            {
                Name = Unique("Model"), Guid = Guid.NewGuid(), Active = 1, Locked = 0,
                Deleted = (byte)(modelDeleted ? 1 : 0)
            });
        modelIds.Add(model.Id);
        var modelId = model.Id;
        var modelGuid = model.Guid;

        var workflowGuid = Guid.NewGuid();
        var workflowId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflow
        {
            Name = Unique("Workflow"), Guid = workflowGuid, EntityAnalysisModelId = model.Id,
            VisualisationRegistryGuid = Guid.NewGuid(), EnableVisualisation = 1, Active = 1, Locked = 0,
            Deleted = (byte)(workflowDeleted ? 1 : 0), CreatedUser = owner, CreatedDate = DateTime.UtcNow
        });
        workflowIds.Add(workflowId);
        foreach (var roleUser in workflowRoles)
        {
            await dbContext.InsertAsync(new CaseWorkflowRole
            {
                CaseWorkflowGuid = workflowGuid, Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleOfAsync(dbContext, roleUser), CreatedUser = owner,
                CreatedDate = DateTime.UtcNow, Deleted = 0
            });
        }

        var statusGuid = Guid.NewGuid();
        var statusId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflowStatus
        {
            Name = Unique("Status"), Guid = statusGuid, CaseWorkflowId = workflowId, ForeColor = "#112233",
            BackColor = "#445566", Active = 1, Locked = 0, Deleted = (byte)(statusDeleted ? 1 : 0), Priority = 1,
            EnableNotification = 0, EnableHttpEndpoint = 0, CreatedUser = owner, CreatedDate = DateTime.UtcNow
        });
        statusIds.Add(statusId);
        foreach (var roleUser in statusRoles)
        {
            await dbContext.InsertAsync(new CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = statusGuid, Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleOfAsync(dbContext, roleUser), CreatedUser = owner,
                CreatedDate = DateTime.UtcNow, Deleted = 0
            });
        }

        var caseId = await dbContext.InsertWithInt32IdentityAsync(new PocoCase
        {
            EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(), CaseWorkflowGuid = workflowGuid,
            CaseWorkflowStatusGuid = statusGuid, CaseKey = key, CaseKeyValue = value, Json = json,
            CreatedDate = DateTime.UtcNow, Diary = 0, Locked = 0, ClosedStatusId = 0, Rating = 3,
            LastClosedStatus = 0
        });
        caseIds.Add(caseId);

        return new CaseGraph(caseId, modelId, modelGuid, workflowId, workflowGuid, statusId, statusGuid, key,
            value, owner);
    }

    public async Task<int> NoteAsync(CaseGraph graph, string note, string[]? actionRoles = null)
    {
        await using var dbContext = fx.GetDbContext();
        var actionGuid = Guid.NewGuid();
        var actionId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflowAction
        {
            Name = Unique("Action"), Guid = actionGuid, CaseWorkflowId = graph.WorkflowId, Active = 1, Locked = 0,
            Deleted = 0, CreatedUser = graph.Owner, CreatedDate = DateTime.UtcNow
        });
        actionIds.Add(actionId);
        foreach (var roleUser in actionRoles ?? [graph.Owner])
        {
            await dbContext.InsertAsync(new CaseWorkflowActionRole
            {
                CaseWorkflowActionGuid = actionGuid, Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleOfAsync(dbContext, roleUser), CreatedUser = graph.Owner,
                CreatedDate = DateTime.UtcNow, Deleted = 0
            });
        }

        var id = await dbContext.InsertWithInt32IdentityAsync(new CaseNote
        {
            Note = note, ActionId = actionId, PriorityId = 1, CreatedDate = DateTime.UtcNow,
            CreatedUser = graph.Owner, CaseId = graph.CaseId, CaseKey = graph.Key, CaseKeyValue = graph.Value
        });
        noteIds.Add(id);
        return id;
    }

    public async Task EventAsync(CaseGraph graph, string? before, string? after, int typeId = 8)
    {
        await using var dbContext = fx.GetDbContext();
        await dbContext.InsertAsync(new CaseEvent
        {
            CaseId = graph.CaseId, CaseEventTypeId = (byte)typeId, Before = before, After = after,
            CreatedUser = graph.Owner, CreatedDate = DateTime.UtcNow, CaseKey = graph.Key,
            CaseKeyValue = graph.Value
        });
    }

    public async Task<int> FormEntryAsync(CaseGraph graph)
    {
        await using var dbContext = fx.GetDbContext();
        var formId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflowForm
        {
            Name = Unique("Form"), Guid = Guid.NewGuid(), CaseWorkflowId = graph.WorkflowId, Active = 1, Locked = 0,
            Deleted = 0, Html = "<div></div>", CreatedUser = graph.Owner, CreatedDate = DateTime.UtcNow
        });
        formIds.Add(formId);
        var id = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflowFormEntry
        {
            CaseId = graph.CaseId, CaseWorkflowFormId = formId, CaseKey = graph.Key, CaseKeyValue = graph.Value,
            CreatedUser = graph.Owner, CreatedDate = DateTime.UtcNow
        });
        entryIds.Add(id);
        return id;
    }

    public async Task<int> DisplayAsync(CaseGraph graph, string html, string[]? roles = null, bool active = true)
    {
        await using var dbContext = fx.GetDbContext();
        var guid = Guid.NewGuid();
        var id = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflowDisplay
        {
            Name = Unique("Display"), Guid = guid, CaseWorkflowId = graph.WorkflowId, Html = html,
            Active = (byte)(active ? 1 : 0), Locked = 0, Deleted = 0, CreatedUser = graph.Owner,
            CreatedDate = DateTime.UtcNow
        });
        displayIds.Add(id);
        foreach (var roleUser in roles ?? [graph.Owner])
        {
            await dbContext.InsertAsync(new CaseWorkflowDisplayRole
            {
                CaseWorkflowDisplayGuid = guid, Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleOfAsync(dbContext, roleUser), CreatedUser = graph.Owner,
                CreatedDate = DateTime.UtcNow, Deleted = 0
            });
        }

        return id;
    }

    public async Task<int> MacroAsync(CaseGraph graph, string? endpoint, byte endpointType = 1,
        string[]? roles = null, bool active = true)
    {
        await using var dbContext = fx.GetDbContext();
        var guid = Guid.NewGuid();
        var id = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflowMacro
        {
            Name = Unique("Macro"), Guid = guid, CaseWorkflowId = graph.WorkflowId,
            Active = (byte)(active ? 1 : 0), Locked = 0, Deleted = 0, Version = 1,
            EnableHttpEndpoint = (byte)(endpoint == null ? 0 : 1), HttpEndpoint = endpoint,
            HttpEndpointTypeId = endpointType, EnableNotification = 0, CreatedUser = graph.Owner,
            CreatedDate = DateTime.UtcNow
        });
        macroIds.Add(id);
        foreach (var roleUser in roles ?? [graph.Owner])
        {
            await dbContext.InsertAsync(new CaseWorkflowMacroRole
            {
                CaseWorkflowMacroGuid = guid, Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleOfAsync(dbContext, roleUser), CreatedUser = graph.Owner,
                CreatedDate = DateTime.UtcNow, Deleted = 0
            });
        }

        return id;
    }

    public async Task<(int Id, Guid Guid)> ModelAsync(string owner, bool deleted = false)
    {
        await using var dbContext = fx.GetDbContext();
        var model = await new EntityAnalysisModelRepository(dbContext, owner).InsertAsync(new EntityAnalysisModel
        {
            Name = Unique("Model"), Guid = Guid.NewGuid(), Active = 1, Locked = 0, Deleted = (byte)(deleted ? 1 : 0)
        });
        modelIds.Add(model.Id);
        return (model.Id, model.Guid);
    }

    public async Task<int> XPathAsync(int modelId, string name, int dataTypeId = 1, bool suppression = false,
        byte deleted = 0)
    {
        await using var dbContext = fx.GetDbContext();
        var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
        {
            EntityAnalysisModelId = modelId, Name = name, XPath = "$.a", DataTypeId = (byte)dataTypeId, Active = 1,
            Locked = 0, Deleted = deleted, Version = 1, Guid = Guid.NewGuid(), CreatedDate = DateTime.UtcNow,
            CreatedUser = DatabaseFixture.Prefix, EnableSuppression = (byte)(suppression ? 1 : 0)
        });
        xpathIds.Add(id);
        return id;
    }

    public async Task InlineFunctionAsync(int modelId, string name, int returnDataTypeId = 1)
    {
        await using var dbContext = fx.GetDbContext();
        inlineFunctionIds.Add(await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelInlineFunction
        {
            EntityAnalysisModelId = modelId, Name = name, ReturnDataTypeId = (byte)returnDataTypeId,
            FunctionScript = "Return 1", Active = 1, Locked = 0, Deleted = 0, Version = 1, Guid = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
        }));
    }

    public async Task<int> ActivationRuleAsync(string owner, int modelId, string name, bool enableSuppression = true)
    {
        await using var dbContext = fx.GetDbContext();
        var rule = await new EntityAnalysisModelActivationRuleRepository(dbContext, owner).InsertAsync(
            new EntityAnalysisModelActivationRule
            {
                EntityAnalysisModelId = modelId, Name = name, Active = 1, Locked = 0, Deleted = 0,
                EnableSuppression = (byte)(enableSuppression ? 1 : 0)
            });
        activationRuleIds.Add(rule.Id);
        return rule.Id;
    }

    public async Task SuppressionAsync(Guid modelGuid, string key, string value, DateTime? expiry = null,
        bool deleted = false)
    {
        await using var dbContext = fx.GetDbContext();
        suppressionIds.Add(await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelSuppression
        {
            EntityAnalysisModelGuid = modelGuid, SuppressionKey = key, SuppressionKeyValue = value,
            DeleteExpiryDate = expiry, Deleted = (byte)(deleted ? 1 : 0), CreatedDate = DateTime.UtcNow,
            CreatedUser = DatabaseFixture.Prefix, Version = 1
        }));
    }

    public async Task ActivationRuleSuppressionAsync(Guid modelGuid, string ruleName, string key, string value,
        DateTime? expiry = null, bool deleted = false)
    {
        await using var dbContext = fx.GetDbContext();
        activationRuleSuppressionIds.Add(await dbContext.InsertWithInt32IdentityAsync(
            new EntityAnalysisModelActivationRuleSuppression
            {
                EntityAnalysisModelGuid = modelGuid, SuppressionKey = key, SuppressionKeyValue = value,
                EntityAnalysisModelActivationRuleName = ruleName, DeleteExpiryDate = expiry,
                Deleted = (byte)(deleted ? 1 : 0),
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix, Version = 1
            }));
    }

    public async Task ScheduleAsync(int tenantRegistryId, DateTime scheduleDate)
    {
        await using var dbContext = fx.GetDbContext();
        scheduleIds.Add(await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelSynchronisationSchedule
        {
            CreatedDate = DateTime.UtcNow, ScheduleDate = scheduleDate, TenantRegistryId = tenantRegistryId,
            CreatedUser = DatabaseFixture.Prefix
        }));
    }

    public async Task NodeEntryAsync(int tenantRegistryId, string instance, DateTime heartbeat,
        DateTime? synchronised)
    {
        await using var dbContext = fx.GetDbContext();
        syncEntryIds.Add(await dbContext.InsertWithInt32IdentityAsync(
            new EntityAnalysisModelSynchronisationNodeStatusEntry
            {
                Instance = instance, HeartbeatDate = heartbeat, SynchronisedDate = synchronised,
                TenantRegistryId = tenantRegistryId
            }));
    }

    public async Task CleanupAsync()
    {
        await using var dbContext = fx.GetDbContext();

        await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry.Where(w => syncEntryIds.Contains(w.Id))
            .DeleteAsync();
        await dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w => scheduleIds.Contains(w.Id))
            .DeleteAsync();

        await dbContext.GetTable<CaseEvent>()
            .Where(w => w.CaseId != null && caseIds.Contains(w.CaseId.Value)).DeleteAsync();
        await dbContext.CaseNote.Where(w => noteIds.Contains(w.Id)).DeleteAsync();
        await dbContext.CaseWorkflowFormEntry.Where(w => entryIds.Contains(w.Id)).DeleteAsync();
        await dbContext.Case.Where(w => caseIds.Contains(w.Id)).DeleteAsync();

        var macroGuids = dbContext.CaseWorkflowMacro.Where(w => macroIds.Contains(w.Id)).Select(w => w.Guid);
        await dbContext.GetTable<CaseWorkflowMacroRole>().Where(w => macroGuids.Contains(w.CaseWorkflowMacroGuid))
            .DeleteAsync();
        await dbContext.CaseWorkflowMacro.Where(w => macroIds.Contains(w.Id)).DeleteAsync();

        var displayGuids = dbContext.CaseWorkflowDisplay.Where(w => displayIds.Contains(w.Id)).Select(w => w.Guid);
        await dbContext.GetTable<CaseWorkflowDisplayRole>()
            .Where(w => displayGuids.Contains(w.CaseWorkflowDisplayGuid)).DeleteAsync();
        await dbContext.CaseWorkflowDisplay.Where(w => displayIds.Contains(w.Id)).DeleteAsync();

        await dbContext.CaseWorkflowForm.Where(w => formIds.Contains(w.Id)).DeleteAsync();

        var actionGuids = dbContext.CaseWorkflowAction.Where(w => actionIds.Contains(w.Id)).Select(w => w.Guid);
        await dbContext.GetTable<CaseWorkflowActionRole>().Where(w => actionGuids.Contains(w.CaseWorkflowActionGuid))
            .DeleteAsync();
        await dbContext.CaseWorkflowAction.Where(w => actionIds.Contains(w.Id)).DeleteAsync();

        var statusGuids = dbContext.CaseWorkflowStatus.Where(w => statusIds.Contains(w.Id)).Select(w => w.Guid);
        await dbContext.GetTable<CaseWorkflowStatusRole>().Where(w => statusGuids.Contains(w.CaseWorkflowStatusGuid))
            .DeleteAsync();
        var workflowGuids = dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).Select(w => w.Guid);
        await dbContext.GetTable<CaseWorkflowRole>().Where(w => workflowGuids.Contains(w.CaseWorkflowGuid))
            .DeleteAsync();
        await dbContext.CaseWorkflowStatus.Where(w => statusIds.Contains(w.Id)).DeleteAsync();
        await dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();

        await dbContext.GetTable<EntityAnalysisModelActivationRuleSuppression>()
            .Where(w => activationRuleSuppressionIds.Contains(w.Id)).DeleteAsync();
        await dbContext.GetTable<EntityAnalysisModelSuppression>().Where(w => suppressionIds.Contains(w.Id))
            .DeleteAsync();
        await dbContext.GetTable<EntityAnalysisModelInlineFunction>()
            .Where(w => inlineFunctionIds.Contains(w.Id)).DeleteAsync();
        await dbContext.GetTable<EntityAnalysisModelRequestXpath>().Where(w => xpathIds.Contains(w.Id))
            .DeleteAsync();
        foreach (var id in activationRuleIds)
        {
            await dbContext.GetTable<EntityAnalysisModelActivationRuleVersion>()
                .Where(w => w.EntityAnalysisModelActivationRuleId == id).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelActivationRule>().Where(w => w.Id == id).DeleteAsync();
        }

        await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => modelIds.Contains(w.EntityAnalysisModelId))
            .DeleteAsync();
        await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
    }
}