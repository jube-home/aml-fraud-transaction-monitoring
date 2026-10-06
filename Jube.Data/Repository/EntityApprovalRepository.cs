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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;
    using Poco;
    using Query.Models;

    public class EntityApprovalRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityApprovalRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityApprovalRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityApprovalRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<List<EntityApprovalRow>> GetRowsByKindAsync(EntityApprovalKind kind,
            CancellationToken token = default)
        {
            var kindId = (int)kind;

            return dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && w.EntityId != null && w.EntityVersion != null && w.StateId != null)
                .Select(s => new EntityApprovalRow(s.EntityId.Value, s.EntityVersion.Value,
                    (EntityApprovalState)s.StateId.Value, s.CreatedUser))
                .ToListAsync(token);
        }

        public async Task<List<EntityApprovalRow>> GetRowsByKindAndEntityIdsAsync(EntityApprovalKind kind,
            ICollection<int> entityIds, CancellationToken token = default)
        {
            if (entityIds == null || entityIds.Count == 0)
            {
                return [];
            }

            var kindId = (int)kind;

            return await dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && w.EntityId != null && w.EntityVersion != null && w.StateId != null
                            && entityIds.Contains(w.EntityId.Value))
                .Select(s => new EntityApprovalRow(s.EntityId.Value, s.EntityVersion.Value,
                    (EntityApprovalState)s.StateId.Value, s.CreatedUser))
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<List<EntityApproval>> GetHistoryAsync(EntityApprovalKind kind, int entityId,
            CancellationToken token = default)
        {
            var kindId = (int)kind;

            return dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && w.EntityId == entityId
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue))
                .OrderByDescending(o => o.Id)
                .ToListAsync(token);
        }

        public Task<EntityApproval> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityApproval.FirstOrDefaultAsync(
                w => w.Id == id && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue), token);
        }

        public async Task<int> GetMaxIdAsync(int tenantRegistryIdOfWatermark, CancellationToken token = default)
        {
            var max = await dbContext.EntityApproval
                .Where(w => w.TenantRegistryId == tenantRegistryIdOfWatermark)
                .Select(s => (int?)s.Id)
                .MaxAsync(token).ConfigureAwait(false);

            return max ?? 0;
        }

        public async Task<EntityApproval> InsertAsync(EntityApproval model, CancellationToken token = default)
        {
            model.CreatedUser ??= userName;
            model.CreatedDate ??= DateTime.UtcNow;
            model.TenantRegistryId ??= tenantRegistryId;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        private async Task<int> BulkCopyAsync(List<EntityApproval> prepared, CancellationToken token)
        {
            var result = await dbContext.BulkCopyAsync(prepared, token).ConfigureAwait(false);
            return (int)result.RowsCopied;
        }

        public Task<int> InsertManyAsync(IEnumerable<EntityApproval> models, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(models);

            var prepared = models.Select(s =>
            {
                s.CreatedUser ??= userName;
                s.CreatedDate ??= DateTime.UtcNow;
                s.TenantRegistryId ??= tenantRegistryId;
                s.Guid = s.Guid == Guid.Empty ? Guid.NewGuid() : s.Guid;
                return s;
            }).ToList();

            return prepared.Count == 0 ? Task.FromResult(0) : BulkCopyAsync(prepared, token);
        }

        private static readonly Dictionary<string, EntityApprovalKind> byModelId = new()
        {
            { "EntityAnalysisModelRequestXpath", EntityApprovalKind.EntityAnalysisModelRequestXPath },
            { "EntityAnalysisModelInlineScript", EntityApprovalKind.EntityAnalysisModelInlineScript },
            { "EntityAnalysisModelInlineFunction", EntityApprovalKind.EntityAnalysisModelInlineFunction },
            { "EntityAnalysisModelGatewayRule", EntityApprovalKind.EntityAnalysisModelGatewayRule },
            { "EntityAnalysisModelSanction", EntityApprovalKind.EntityAnalysisModelSanction },
            { "EntityAnalysisModelAbstractionRule", EntityApprovalKind.EntityAnalysisModelAbstractionRule },
            {
                "EntityAnalysisModelAbstractionCalculation",
                EntityApprovalKind.EntityAnalysisModelAbstractionCalculation
            },
            { "EntityAnalysisModelTtlCounter", EntityApprovalKind.EntityAnalysisModelTtlCounter },
            { "EntityAnalysisModelHttpAdaptation", EntityApprovalKind.EntityAnalysisModelHttpAdaptation },
            { "ExhaustiveSearchInstance", EntityApprovalKind.ExhaustiveSearchInstance },
            { "EntityAnalysisModelActivationRule", EntityApprovalKind.EntityAnalysisModelActivationRule },
            { "EntityAnalysisModelTag", EntityApprovalKind.EntityAnalysisModelTag }
        };

        private static readonly Dictionary<string, EntityApprovalKind> byModelGuid = new()
        {
            { "EntityAnalysisModelList", EntityApprovalKind.EntityAnalysisModelList },
            { "EntityAnalysisModelDictionary", EntityApprovalKind.EntityAnalysisModelDictionary }
        };

        private static readonly Dictionary<string, (string Parent, string ParentId, EntityApprovalKind Kind)>
            byParent = new()
            {
                {
                    "EntityAnalysisModelListValue",
                    ("EntityAnalysisModelList", "EntityAnalysisModelListId",
                        EntityApprovalKind.EntityAnalysisModelListValue)
                },
                {
                    "EntityAnalysisModelDictionaryKvp",
                    ("EntityAnalysisModelDictionary", "EntityAnalysisModelDictionaryId",
                        EntityApprovalKind.EntityAnalysisModelDictionaryKvp)
                }
            };

        public async Task<int> ApproveEntireModelAsync(int entityAnalysisModelId, string approvedBy,
            CancellationToken token = default)
        {
            var stamped = await ExecuteAsync($"""
                                              insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                                  "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                              select m."TenantRegistryId", {(int)EntityApprovalKind.EntityAnalysisModel},
                                                  m."Id", coalesce(m."Version",1), {(int)EntityApprovalState.Approved},
                                                  now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                              from "EntityAnalysisModel" m
                                              where m."Id" = @modelId
                                                and m."TenantRegistryId" is not null
                                                and not exists (
                                                    select 1 from "EntityApproval" a
                                                    where a."EntityApprovalKindId" = {(int)EntityApprovalKind.EntityAnalysisModel}
                                                      and a."EntityId" = m."Id"
                                                      and a."EntityVersion" = coalesce(m."Version",1)
                                                      and a."CreatedUser" = @approvedBy);
                                              """, entityAnalysisModelId, approvedBy, token).ConfigureAwait(false);

            foreach (var (table, kind) in byModelId)
            {
                stamped += await ExecuteAsync($"""
                                               insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                                   "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                               select m."TenantRegistryId", {(int)kind}, r."Id",
                                                   coalesce(r."Version",1), {(int)EntityApprovalState.Approved},
                                                   now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                               from "{table}" r
                                               join "EntityAnalysisModel" m on m."Id" = r."EntityAnalysisModelId"
                                               where r."EntityAnalysisModelId" = @modelId
                                                 and coalesce(r."Deleted",0) = 0
                                                 and m."TenantRegistryId" is not null
                                                 and not exists (
                                                     select 1 from "EntityApproval" a
                                                     where a."EntityApprovalKindId" = {(int)kind}
                                                       and a."EntityId" = r."Id"
                                                       and a."EntityVersion" = coalesce(r."Version",1)
                                                       and a."CreatedUser" = @approvedBy);
                                               """, entityAnalysisModelId, approvedBy, token).ConfigureAwait(false);
            }

            foreach (var (table, kind) in byModelGuid)
            {
                stamped += await ExecuteAsync($"""
                                               insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                                   "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                               select m."TenantRegistryId", {(int)kind}, r."Id",
                                                   coalesce(r."Version",1), {(int)EntityApprovalState.Approved},
                                                   now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                               from "{table}" r
                                               join "EntityAnalysisModel" m on m."Guid" = r."EntityAnalysisModelGuid"
                                               where m."Id" = @modelId
                                                 and coalesce(r."Deleted",0) = 0
                                                 and m."TenantRegistryId" is not null
                                                 and not exists (
                                                     select 1 from "EntityApproval" a
                                                     where a."EntityApprovalKindId" = {(int)kind}
                                                       and a."EntityId" = r."Id"
                                                       and a."EntityVersion" = coalesce(r."Version",1)
                                                       and a."CreatedUser" = @approvedBy);
                                               """, entityAnalysisModelId, approvedBy, token).ConfigureAwait(false);
            }

            foreach (var (table, parent) in byParent)
            {
                stamped += await ExecuteAsync($"""
                                               insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                                   "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                               select m."TenantRegistryId", {(int)parent.Kind}, r."Id",
                                                   coalesce(r."Version",1), {(int)EntityApprovalState.Approved},
                                                   now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                               from "{table}" r
                                               join "{parent.Parent}" p on p."Id" = r."{parent.ParentId}"
                                               join "EntityAnalysisModel" m on m."Guid" = p."EntityAnalysisModelGuid"
                                               where m."Id" = @modelId
                                                 and coalesce(r."Deleted",0) = 0
                                                 and m."TenantRegistryId" is not null
                                                 and not exists (
                                                     select 1 from "EntityApproval" a
                                                     where a."EntityApprovalKindId" = {(int)parent.Kind}
                                                       and a."EntityId" = r."Id"
                                                       and a."EntityVersion" = coalesce(r."Version",1)
                                                       and a."CreatedUser" = @approvedBy);
                                               """, entityAnalysisModelId, approvedBy, token).ConfigureAwait(false);
            }

            return stamped;
        }

        private Task<int> ExecuteAsync(string sql, int modelId, string approvedBy, CancellationToken token)
        {
            return dbContext.ExecuteAsync(sql, token,
                new DataParameter("modelId", modelId),
                new DataParameter("approvedBy", approvedBy));
        }
    }
}