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
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json.Linq;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    internal sealed class JournalJobsSeed(DatabaseFixture fx)
    {
        private readonly List<Guid> archives = [];
        private readonly List<int> cases = [];
        private readonly List<int> models = [];
        private readonly List<int> workflows = [];
        private readonly List<int> xPaths = [];

        public string Me => fx.Seed.UserWithPermission;

        public static string NewKey() => $"{DatabaseFixture.Prefix}K{Guid.NewGuid():N}"[..30];

        public async Task<(int Model, int WorkflowId, Guid Workflow)> WorkflowAsync(DbContext dbContext)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, Me).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                }).ConfigureAwait(false);
            models.Add(model.Id);

            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = guid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);
            workflows.Add(id);

            return (model.Id, id, guid);
        }

        public async Task XPathAsync(DbContext dbContext, int workflowId, string name, bool drill = false,
            bool segment = false)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowXPath
            {
                Guid = Guid.NewGuid(),
                CaseWorkflowId = workflowId,
                Name = name,
                XPath = $"$.payload.{name}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Drill = (byte)(drill ? 1 : 0),
                Segment = (byte)(segment ? 1 : 0),
                Version = 1,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);
            xPaths.Add(id);
        }

        public async Task CaseAsync(DbContext dbContext, Guid workflow, string caseKey)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = 0,
                ClosedStatusId = 0,
                CaseKey = caseKey
            }).ConfigureAwait(false);
            cases.Add(id);
        }

        public async Task ArchiveAsync(DbContext dbContext, int model, JObject payload, DateTime? created = null)
        {
            var guid = Guid.NewGuid();
            var at = created ?? DateTime.UtcNow;
            var json = new JObject { ["payload"] = payload, ["tag"] = new JArray() };

            await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json.ToString(Newtonsoft.Json.Formatting.None),
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = model,
                EntryKeyValue = "k",
                ResponseElevation = 1,
                ActivationRuleCount = 0,
                CreatedDate = at,
                ReferenceDate = at
            }).ConfigureAwait(false);
            archives.Add(guid);
        }

        public async Task CleanupAsync(DbContext dbContext)
        {
            await dbContext.Archive.Where(w => archives.Contains(w.EntityAnalysisModelInstanceEntryGuid))
                .DeleteAsync();

            var workflowGuids = dbContext.CaseWorkflow.Where(w => workflows.Contains(w.Id)).Select(w => w.Guid);

            await dbContext.CaseKeyJournalPeer.Where(w => workflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.Case.Where(w => cases.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathVersion>()
                .Where(w => w.CaseWorkflowXPathId != null && xPaths.Contains(w.CaseWorkflowXPathId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflowXPath.Where(w => xPaths.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && workflows.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflows.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => models.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => models.Contains(w.Id)).DeleteAsync();
        }
    }
}
