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
using LinqToDB.Async;
using Newtonsoft.Json.Linq;

namespace Jube.Test.Service.Query.Graph
{
    internal sealed class GraphJobsSeed(DatabaseFixture fx)
    {
        private readonly List<Guid> archives = [];
        private readonly List<int> cases = [];
        private readonly List<int> models = [];
        private readonly List<int> modelTags = [];
        private readonly List<int> registries = [];
        private readonly List<int> statuses = [];
        private readonly List<int> workflows = [];
        private readonly string suffix = Guid.NewGuid().ToString("N")[..10];

        public string Me => fx.Seed.UserWithPermission;

        public string AccountKey => $"{DatabaseFixture.Prefix}Acct{suffix}";

        public string DeviceKey => $"{DatabaseFixture.Prefix}Dev{suffix}";

        public string BeneficiaryKey => $"{DatabaseFixture.Prefix}Bene{suffix}";

        public string AmountKey => $"{DatabaseFixture.Prefix}Amt{suffix}";

        public Guid Registry { get; private set; }

        public Guid PaysDefinition { get; private set; }

        public Guid DeviceDefinition { get; private set; }

        public int ModelId { get; private set; }

        public Guid Workflow { get; private set; }

        public Guid OpenStatus { get; private set; }

        public Guid ConfirmedStatus { get; private set; }

        public int TenantRegistryId { get; private set; }

        public async Task SetupAsync(DbContext dbContext, int? deviceHubThreshold = null, bool restrictDevice = false)
        {
            TenantRegistryId = await dbContext.UserInTenant.Where(w => w.User == Me).Select(w => w.TenantRegistryId)
                .FirstAsync();
            var model = await new EntityAnalysisModelRepository(dbContext, Me).InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}GraphModel{suffix}", Guid = Guid.NewGuid(), Active = 1, Locked = 0,
                Deleted = 0
            });
            ModelId = model.Id;
            models.Add(model.Id);
            Workflow = Guid.NewGuid();
            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}GraphWorkflow{suffix}", Guid = Workflow, EntityAnalysisModelId = model.Id,
                Active = 1, Locked = 0, Deleted = 0, EnableVisualisation = 0, Version = 1, CreatedUser = Me,
                CreatedDate = DateTime.UtcNow
            });
            workflows.Add(workflowId);
            OpenStatus = Guid.NewGuid();
            ConfirmedStatus = Guid.NewGuid();
            statuses.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                CaseWorkflowId = workflowId, Guid = OpenStatus, Name = "Open", Active = 1, Deleted = 0, Version = 1,
                Priority = 5, OutcomeId = 0, CreatedUser = Me, CreatedDate = DateTime.UtcNow
            }));
            statuses.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                CaseWorkflowId = workflowId, Guid = ConfirmedStatus, Name = "Confirmed", Active = 1, Deleted = 0,
                Version = 1, Priority = 1, OutcomeId = 1, CreatedUser = Me, CreatedDate = DateTime.UtcNow
            }));
            var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == Me).Select(u => u.RoleRegistryGuid)
                .FirstAsync();
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = Workflow, Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, CreatedUser = Me,
                CreatedDate = DateTime.UtcNow, Deleted = 0, Version = 1
            });

            foreach (var status in new[] { OpenStatus, ConfirmedStatus })
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
                {
                    CaseWorkflowStatusGuid = status, Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, CreatedUser = Me,
                    CreatedDate = DateTime.UtcNow, Deleted = 0, Version = 1
                });
            }

            var registry = await new GraphRegistryRepository(dbContext, Me).InsertAsync(new Data.Poco.GraphRegistry
            {
                Name = $"{DatabaseFixture.Prefix}GraphRegistry{suffix}", Active = 1, Locked = 0, Deleted = 0,
                ContextKindId = 1, DefaultHops = 2, DefaultWindowDays = 30, RenderCap = 150, HubPolicyId = 1
            });
            Registry = registry.Guid;
            registries.Add(registry.Id);
            await dbContext.CaseWorkflow.Where(w => w.Id == workflowId)
                .Set(w => w.EnableGraph, (byte?)1)
                .Set(w => w.GraphRegistryGuid, (Guid?)registry.Guid)
                .UpdateAsync();
            var edges = new GraphRegistryEdgeRepository(dbContext, Me);
            var pays = await edges.InsertAsync(new Data.Poco.GraphRegistryEdge
            {
                GraphRegistryId = registry.Id, Name = "Pays", KindId = 2, FromKey = AccountKey, ToKey = BeneficiaryKey,
                AmountKey = AmountKey, HalfLifeDays = 90, KindPrior = 0.7, MinimumAmount = 0, Hard = 0, Restricted = 0,
                Active = 1, Locked = 0, Deleted = 0
            });
            PaysDefinition = pays.Guid;
            var device = await edges.InsertAsync(new Data.Poco.GraphRegistryEdge
            {
                GraphRegistryId = registry.Id, Name = "Shares device", KindId = 1, FromKey = AccountKey, ToKey = DeviceKey,
                HalfLifeDays = 180, KindPrior = 0.4, MinimumAmount = 0, Hard = 0,
                Restricted = (byte)(restrictDevice ? 1 : 0), HubThreshold = deviceHubThreshold, Active = 1, Locked = 0,
                Deleted = 0
            });
            DeviceDefinition = device.Guid;
        }

        public async Task<Guid> TransactionAsync(DbContext dbContext, DateTime at, string account, string? beneficiary,
            double? amount, string? device)
        {
            var guid = Guid.NewGuid();
            var payload = new JObject { [AccountKey] = account };

            if (beneficiary != null)
            {
                payload[BeneficiaryKey] = beneficiary;
            }

            if (amount.HasValue)
            {
                payload[AmountKey] = amount.Value;
            }

            if (device != null)
            {
                payload[DeviceKey] = device;
            }

            var json = new JObject { ["payload"] = payload, ["tag"] = new JArray() };
            await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json.ToString(Newtonsoft.Json.Formatting.None),
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = ModelId,
                EntryKeyValue = account,
                ResponseElevation = 0,
                ActivationRuleCount = 0,
                CreatedDate = at,
                ReferenceDate = at,
                Version = 1
            });
            var keys = new List<Data.Poco.ArchiveKey>
            {
                new() { ProcessingTypeId = 1, Key = AccountKey, KeyValueString = account, EntityAnalysisModelInstanceEntryGuid = guid, Version = 1 }
            };

            if (beneficiary != null)
            {
                keys.Add(new Data.Poco.ArchiveKey { ProcessingTypeId = 1, Key = BeneficiaryKey, KeyValueString = beneficiary, EntityAnalysisModelInstanceEntryGuid = guid, Version = 1 });
            }

            if (amount.HasValue)
            {
                keys.Add(new Data.Poco.ArchiveKey { ProcessingTypeId = 1, Key = AmountKey, KeyValueFloat = amount.Value, EntityAnalysisModelInstanceEntryGuid = guid, Version = 1 });
            }

            if (device != null)
            {
                keys.Add(new Data.Poco.ArchiveKey { ProcessingTypeId = 1, Key = DeviceKey, KeyValueString = device, EntityAnalysisModelInstanceEntryGuid = guid, Version = 1 });
            }

            foreach (var key in keys)
            {
                await dbContext.InsertAsync(key);
            }

            archives.Add(guid);

            return guid;
        }

        public async Task<int> ModelTagAsync(DbContext dbContext, string name)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelTag
            {
                EntityAnalysisModelId = ModelId, Name = name, Active = 1, Locked = 0, ReportTable = 0, Deleted = 0,
                Version = 1, CreatedUser = Me, CreatedDate = DateTime.UtcNow
            });
            modelTags.Add(id);

            return id;
        }

        public Task ArchiveTagAsync(DbContext dbContext, Guid entry, string name)
        {
            return dbContext.InsertAsync(new Data.Poco.ArchiveTag
            {
                EntityAnalysisModelInstanceEntryGuid = entry, Name = name, Version = 1, CreatedUser = Me,
                CreatedDate = DateTime.UtcNow, Deleted = 0
            });
        }

        public async Task<int> CaseAsync(DbContext dbContext, Guid anchor, string accountValue, bool confirmed)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = anchor,
                CaseWorkflowGuid = Workflow,
                CaseWorkflowStatusGuid = confirmed ? ConfirmedStatus : OpenStatus,
                CreatedDate = DateTime.UtcNow,
                Locked = 0,
                ClosedStatusId = (byte)(confirmed ? 3 : 0),
                ClosedDate = confirmed ? DateTime.UtcNow : null,
                CaseKey = AccountKey,
                CaseKeyValue = accountValue,
                Diary = 0,
                Rating = 0,
                DiaryTypeId = 1,
                QaSampleStatusId = 0
            });
            cases.Add(id);

            return id;
        }

        public async Task<long> ArchiveIdAsync(DbContext dbContext, Guid entry)
        {
            return await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == entry).Select(a => a.Id)
                .FirstAsync();
        }

        public async Task CleanupAsync(DbContext dbContext)
        {
            var caseValues = cases.Select(c => c.ToString()).ToList();
            var nodeIds = dbContext.GraphNode.Where(n => n.Key == AccountKey || n.Key == DeviceKey || n.Key == BeneficiaryKey
                                                         || (n.Key == "Case" && caseValues.Contains(n.Value)))
                .Select(n => n.Id);
            await dbContext.GraphNodeTag.Where(t => nodeIds.Contains(t.GraphNodeId)).DeleteAsync();
            await dbContext.CaseTag.Where(t => cases.Contains(t.CaseId)).DeleteAsync();
            await dbContext.ArchiveTag.Where(t => archives.Contains(t.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.EntityAnalysisModelTag.Where(t => modelTags.Contains(t.Id)).DeleteAsync();
            var edgeIds = dbContext.GraphEdge.Where(e => nodeIds.Contains(e.FromNodeId) || nodeIds.Contains(e.ToNodeId))
                .Select(e => e.Id);
            await dbContext.GraphEdgeEvent.Where(e => edgeIds.Contains(e.GraphEdgeId)).DeleteAsync();
            await dbContext.GraphEdge.Where(e => nodeIds.Contains(e.FromNodeId) || nodeIds.Contains(e.ToNodeId)).DeleteAsync();
            await dbContext.GraphNode.Where(n => n.Key == AccountKey || n.Key == DeviceKey || n.Key == BeneficiaryKey).DeleteAsync();
            var definitionGuids = dbContext.GraphRegistryEdge.Where(e => e.GraphRegistryId != null && registries.Contains(e.GraphRegistryId.Value)).Select(e => e.Guid);
            await dbContext.GraphBuildState.Where(s => definitionGuids.Contains(s.GraphRegistryEdgeGuid)).DeleteAsync();
            await dbContext.GraphRegistryEdge.Where(e => e.GraphRegistryId != null && registries.Contains(e.GraphRegistryId.Value)).DeleteAsync();
            var registryGuids = dbContext.GraphRegistry.Where(r => registries.Contains(r.Id)).Select(r => r.Guid);
            await dbContext.GraphRegistryRole.Where(r => registryGuids.Contains(r.GraphRegistryGuid)).DeleteAsync();
            await dbContext.GraphRegistry.Where(r => registries.Contains(r.Id)).DeleteAsync();
            await dbContext.ArchiveKey.Where(k => archives.Contains(k.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.Archive.Where(a => archives.Contains(a.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.Case.Where(c => cases.Contains(c.Id)).DeleteAsync();
            await dbContext.CaseWorkflowStatusRole.Where(r => r.CaseWorkflowStatusGuid == OpenStatus || r.CaseWorkflowStatusGuid == ConfirmedStatus).DeleteAsync();
            await dbContext.CaseWorkflowRole.Where(r => r.CaseWorkflowGuid == Workflow).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(s => statuses.Contains(s.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflows.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(m => models.Contains(m.Id)).DeleteAsync();
        }
    }
}
