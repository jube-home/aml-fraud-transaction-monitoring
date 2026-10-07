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

namespace Jube.Data.Query.GetEntityAnalysisModelSyncWatermarkQuery
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Jube.Data.Query.GetEntityAnalysisModelSyncWatermarkQuery.Models;
    using Jube.Data.Query.Models;

    public class GetEntityAnalysisModelSyncWatermarkQuery(DbContext dbContext)
    {
        public async Task<EntityAnalysisModelSyncWatermark> ExecuteAsync(int tenantRegistryId,
            CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            var approval = await dbContext.EntityApproval
                .Where(w => w.TenantRegistryId == tenantRegistryId)
                .GroupBy(_ => 1)
                .Select(g => new Aggregate
                {
                    Count = g.Count(),
                    MaxId = g.Max(m => m.Id)
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var overrides = await dbContext.EntityAnalysisModelOverride
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .GroupBy(_ => 1)
                .Select(g => new Aggregate
                {
                    Count = g.Count(),
                    MaxId = g.Max(m => m.Id),
                    MaxCreatedDate = g.Max(m => m.CreatedDate),
                    MaxDeletedDate = g.Max(m => m.DeletedDate),
                    NextExpiryDate = g.Min(m => m.DeleteExpiryDate > now ? m.DeleteExpiryDate : null)
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var activationRuleOverrides = await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .GroupBy(_ => 1)
                .Select(g => new Aggregate
                {
                    Count = g.Count(),
                    MaxId = g.Max(m => m.Id),
                    MaxCreatedDate = g.Max(m => m.CreatedDate),
                    MaxDeletedDate = g.Max(m => m.DeletedDate),
                    NextExpiryDate = g.Min(m => m.DeleteExpiryDate > now ? m.DeleteExpiryDate : null)
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var exhaustivePromoted = await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w => w.Active == 1
                            && (w.Deleted == 0 || w.Deleted == null)
                            && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.EntityAnalysisModel
                                .TenantRegistryId == tenantRegistryId)
                .GroupBy(_ => 1)
                .Select(g => new Aggregate
                {
                    Count = g.Count(),
                    MaxId = g.Max(m => m.Id),
                    MaxCreatedDate = g.Max(m => m.CreatedDate)
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var apiKeys = await dbContext.UserRegistry
                .Where(u => u.Active == 1)
                .SelectMany(u => dbContext.UserRegistryApiKey
                        .Where(k => k.UserRegistryId == u.Id && (k.Deleted == 0 || k.Deleted == null)),
                    (u, k) => new
                    {
                        u,
                        k
                    })
                .SelectMany(c => dbContext.RoleRegistry
                        .Where(rr => rr.Guid == c.u.RoleRegistryGuid
                                     && rr.TenantRegistryId == tenantRegistryId
                                     && (rr.Deleted == 0 || rr.Deleted == null)),
                    (c, rr) => c.k)
                .GroupBy(_ => 1)
                .Select(g => new Aggregate
                {
                    Count = g.Count(),
                    MaxId = g.Max(m => m.Id)
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var modelRoles = await dbContext.EntityAnalysisModelRole
                .Where(r => r.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (r.Deleted == 0 || r.Deleted == null))
                .SelectMany(r => dbContext.RoleRegistry
                        .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null)),
                    (r, rr) => r)
                .GroupBy(_ => 1)
                .Select(g => new Aggregate
                {
                    Count = g.Count(),
                    MaxId = g.Max(m => m.Id)
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var listValues = await dbContext.EntityAnalysisModelListValue
                .Where(w => w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .GroupBy(_ => 1)
                .Select(g => new Aggregate
                {
                    Count = g.Count(),
                    MaxId = g.Max(m => m.Id),
                    MaxCreatedDate = g.Max(m => m.CreatedDate),
                    MaxDeletedDate = g.Max(m => m.DeletedDate)
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var dictionaryKvps = await dbContext.EntityAnalysisModelDictionaryKvp
                .Where(w => w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .GroupBy(_ => 1)
                .Select(g => new Aggregate
                {
                    Count = g.Count(),
                    MaxId = g.Max(m => m.Id),
                    MaxCreatedDate = g.Max(m => m.CreatedDate),
                    MaxDeletedDate = g.Max(m => m.DeletedDate)
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var listValueExpiry = await dbContext.EntityAnalysisModelListValue
                .Where(w => w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null)
                            && w.DeleteExpiryDate > now)
                .Select(s => s.DeleteExpiryDate)
                .MinAsync(token).ConfigureAwait(false);

            var dictionaryKvpExpiry = await dbContext.EntityAnalysisModelDictionaryKvp
                .Where(w => w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null)
                            && w.DeleteExpiryDate > now)
                .Select(s => s.DeleteExpiryDate)
                .MinAsync(token).ConfigureAwait(false);

            return new EntityAnalysisModelSyncWatermark
            {
                ApprovalCount = approval?.Count ?? 0,
                ApprovalMaxId = approval?.MaxId ?? 0,
                OverrideCount = overrides?.Count ?? 0,
                OverrideMaxId = overrides?.MaxId ?? 0,
                OverrideMaxMutationDate = Latest(overrides?.MaxCreatedDate, overrides?.MaxDeletedDate),
                ActivationRuleOverrideCount = activationRuleOverrides?.Count ?? 0,
                ActivationRuleOverrideMaxId = activationRuleOverrides?.MaxId ?? 0,
                ActivationRuleOverrideMaxMutationDate = Latest(activationRuleOverrides?.MaxCreatedDate,
                    activationRuleOverrides?.MaxDeletedDate),
                ExhaustivePromotedMaxId = exhaustivePromoted?.MaxId ?? 0,
                ExhaustivePromotedMaxCreatedDate = exhaustivePromoted?.MaxCreatedDate,
                ApiKeyCount = apiKeys?.Count ?? 0,
                ApiKeyMaxId = apiKeys?.MaxId ?? 0,
                ModelRoleCount = modelRoles?.Count ?? 0,
                ModelRoleMaxId = modelRoles?.MaxId ?? 0,
                ListValueCount = listValues?.Count ?? 0,
                ListValueMaxId = listValues?.MaxId ?? 0,
                ListValueMaxMutationDate = Latest(listValues?.MaxCreatedDate, listValues?.MaxDeletedDate),
                DictionaryKvpCount = dictionaryKvps?.Count ?? 0,
                DictionaryKvpMaxId = dictionaryKvps?.MaxId ?? 0,
                DictionaryKvpMaxMutationDate = Latest(dictionaryKvps?.MaxCreatedDate,
                    dictionaryKvps?.MaxDeletedDate),
                NextExpiryDate = Earliest(overrides?.NextExpiryDate, activationRuleOverrides?.NextExpiryDate,
                    listValueExpiry, dictionaryKvpExpiry)
            };
        }

        private static DateTime? Latest(params DateTime?[] candidates)
        {
            DateTime? latest = null;

            foreach (var candidate in candidates)
            {
                if (candidate.HasValue && (latest == null || candidate.Value > latest.Value))
                {
                    latest = candidate;
                }
            }

            return latest;
        }

        private static DateTime? Earliest(params DateTime?[] candidates)
        {
            DateTime? earliest = null;

            foreach (var candidate in candidates)
            {
                if (candidate.HasValue && (earliest == null || candidate.Value < earliest.Value))
                {
                    earliest = candidate;
                }
            }

            return earliest;
        }
    }
}