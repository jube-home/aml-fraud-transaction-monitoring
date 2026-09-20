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
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using LinqToDB;

namespace Jube.Test.Security.Visualisation;

internal static class VisModelSupport
{
    public static async Task<VisModelSeed> CreateAsync(DatabaseFixture fx, PenTestHost host, PenTestUser user,
        string label)
    {
        await using var db = fx.GetDbContext();
        var userName = host.UserName(user);
        var model = await new EntityAnalysisModelRepository(db, userName).InsertAsync(new EntityAnalysisModel
        {
            Name = VisSupport.Unique("Mdl" + label), Guid = Guid.NewGuid(), Active = 1, Locked = 0, Deleted = 0
        });

        var xpathName = VisSupport.Unique("Xp" + label);
        await db.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
        {
            EntityAnalysisModelId = model.Id, Name = xpathName, DataTypeId = 1, XPath = "$.x", Active = 1, Locked = 0,
            Deleted = 0, SearchKey = 0, ResponsePayload = 1, ReportTable = 1, Version = 1,
            CreatedUser = VisSupport.Prefix, CreatedDate = DateTime.UtcNow, Guid = Guid.NewGuid()
        });

        var tagName = VisSupport.Unique("Tg" + label);
        await db.InsertWithInt32IdentityAsync(new EntityAnalysisModelTag
        {
            EntityAnalysisModelId = model.Id, Name = tagName, Active = 1, Locked = 0, Deleted = 0,
            ResponsePayload = 1, ReportTable = 1, Version = 1, CreatedUser = VisSupport.Prefix,
            CreatedDate = DateTime.UtcNow, Guid = Guid.NewGuid()
        });

        var workflowName = VisSupport.Unique("Wf" + label);
        var workflow = await new CaseWorkflowRepository(db, userName).InsertAsync(new CaseWorkflow
        {
            Name = workflowName, EntityAnalysisModelId = model.Id, Active = 1, Locked = 0, Deleted = 0,
            EnableVisualisation = 0, VisualisationRegistryGuid = Guid.Empty
        });

        var children = new Dictionary<string, string>();

        string Child(string kind)
        {
            var name = VisSupport.Unique(kind + label);
            children[kind] = name;
            return name;
        }

        await db.InsertWithInt32IdentityAsync(new CaseWorkflowXPath
        {
            CaseWorkflowId = workflow.Id, Name = Child("CaseWorkflowXPath"), XPath = "$.x", Active = 1, Locked = 0,
            Deleted = 0, Version = 1, Guid = Guid.NewGuid(), CreatedUser = VisSupport.Prefix,
            CreatedDate = DateTime.UtcNow
        });
        await db.InsertWithInt32IdentityAsync(new CaseWorkflowForm
        {
            CaseWorkflowId = workflow.Id, Name = Child("CaseWorkflowForm"), Active = 1, Locked = 0, Deleted = 0,
            Version = 1, Guid = Guid.NewGuid(), CreatedUser = VisSupport.Prefix, CreatedDate = DateTime.UtcNow
        });
        await db.InsertWithInt32IdentityAsync(new CaseWorkflowAction
        {
            CaseWorkflowId = workflow.Id, Name = Child("CaseWorkflowAction"), Active = 1, Locked = 0, Deleted = 0,
            Version = 1, Guid = Guid.NewGuid(), CreatedUser = VisSupport.Prefix, CreatedDate = DateTime.UtcNow
        });
        await db.InsertWithInt32IdentityAsync(new CaseWorkflowMacro
        {
            CaseWorkflowId = workflow.Id, Name = Child("CaseWorkflowMacro"), Active = 1, Locked = 0, Deleted = 0,
            Version = 1, Guid = Guid.NewGuid(), CreatedUser = VisSupport.Prefix, CreatedDate = DateTime.UtcNow
        });
        await db.InsertWithInt32IdentityAsync(new CaseWorkflowFilter
        {
            CaseWorkflowId = workflow.Id, Name = Child("CaseWorkflowFilter"), Active = 1, Locked = 0, Deleted = 0,
            Version = 1, Guid = Guid.NewGuid(), CreatedUser = VisSupport.Prefix, CreatedDate = DateTime.UtcNow
        });
        await db.InsertWithInt32IdentityAsync(new CaseWorkflowDisplay
        {
            CaseWorkflowId = workflow.Id, Name = Child("CaseWorkflowDisplay"), Active = 1, Locked = 0, Deleted = 0,
            Version = 1, Guid = Guid.NewGuid(), CreatedUser = VisSupport.Prefix, CreatedDate = DateTime.UtcNow
        });
        await db.InsertWithInt32IdentityAsync(new CaseWorkflowStatus
        {
            CaseWorkflowId = workflow.Id, Name = Child("CaseWorkflowStatus"), Active = 1, Locked = 0, Deleted = 0,
            Version = 1, Priority = 1, Guid = Guid.NewGuid(), CreatedUser = VisSupport.Prefix,
            CreatedDate = DateTime.UtcNow
        });

        return new VisModelSeed
        {
            ModelId = model.Id,
            ModelGuid = model.Guid,
            XPathName = xpathName,
            TagName = tagName,
            WorkflowId = workflow.Id,
            WorkflowGuid = workflow.Guid,
            WorkflowName = workflowName,
            WorkflowChildren = children
        };
    }

    public static async Task SweepAsync(DatabaseFixture fx)
    {
        await using var db = fx.GetDbContext();
        var workflowIds = db.GetTable<CaseWorkflow>()
            .Where(w => w.Name != null && w.Name.StartsWith(VisSupport.Prefix + "Wf"))
            .Select(w => (int?)w.Id);
        var modelIds = db.GetTable<EntityAnalysisModel>()
            .Where(w => w.Name != null && w.Name.StartsWith(VisSupport.Prefix + "Mdl")).Select(w => (int?)w.Id);
        await db.GetTable<CaseWorkflowXPath>().Where(w => workflowIds.Contains(w.CaseWorkflowId)).DeleteAsync();
        await db.GetTable<CaseWorkflowForm>().Where(w => workflowIds.Contains(w.CaseWorkflowId)).DeleteAsync();
        await db.GetTable<CaseWorkflowAction>().Where(w => workflowIds.Contains(w.CaseWorkflowId)).DeleteAsync();
        await db.GetTable<CaseWorkflowMacro>().Where(w => workflowIds.Contains(w.CaseWorkflowId)).DeleteAsync();
        await db.GetTable<CaseWorkflowFilter>().Where(w => workflowIds.Contains(w.CaseWorkflowId)).DeleteAsync();
        await db.GetTable<CaseWorkflowDisplay>().Where(w => workflowIds.Contains(w.CaseWorkflowId)).DeleteAsync();
        await db.GetTable<CaseWorkflowStatus>().Where(w => workflowIds.Contains(w.CaseWorkflowId)).DeleteAsync();
        await db.GetTable<EntityAnalysisModelTag>().Where(w => modelIds.Contains(w.EntityAnalysisModelId))
            .DeleteAsync();
    }

    public static async Task DeleteAsync(DatabaseFixture fx, VisModelSeed? seed)
    {
        if (seed == null)
        {
            return;
        }

        await using var db = fx.GetDbContext();
        await db.GetTable<EntityAnalysisModelTag>().Where(w => w.EntityAnalysisModelId == seed.ModelId).DeleteAsync();
        await db.GetTable<CaseWorkflowXPath>().Where(w => w.CaseWorkflowId == seed.WorkflowId).DeleteAsync();
        await db.GetTable<CaseWorkflowForm>().Where(w => w.CaseWorkflowId == seed.WorkflowId).DeleteAsync();
        await db.GetTable<CaseWorkflowAction>().Where(w => w.CaseWorkflowId == seed.WorkflowId).DeleteAsync();
        await db.GetTable<CaseWorkflowMacro>().Where(w => w.CaseWorkflowId == seed.WorkflowId).DeleteAsync();
        await db.GetTable<CaseWorkflowFilter>().Where(w => w.CaseWorkflowId == seed.WorkflowId).DeleteAsync();
        await db.GetTable<CaseWorkflowDisplay>().Where(w => w.CaseWorkflowId == seed.WorkflowId).DeleteAsync();
        await db.GetTable<CaseWorkflowStatus>().Where(w => w.CaseWorkflowId == seed.WorkflowId).DeleteAsync();
        await db.GetTable<CaseWorkflow>().Where(w => w.Id == seed.WorkflowId).DeleteAsync();
        await db.GetTable<EntityAnalysisModelRequestXpath>().Where(w => w.EntityAnalysisModelId == seed.ModelId)
            .DeleteAsync();
        await db.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == seed.ModelId)
            .DeleteAsync();
        await db.GetTable<EntityAnalysisModel>().Where(w => w.Id == seed.ModelId).DeleteAsync();
    }
}