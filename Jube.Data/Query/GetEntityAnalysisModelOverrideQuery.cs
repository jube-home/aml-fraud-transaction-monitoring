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

namespace Jube.Data.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Models;

    public class GetEntityAnalysisModelOverrideQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetEntityAnalysisModelOverrideQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<EntityAnalysisModelOverrideKeyQueryRow>> ExecuteKeysAsync(
            CancellationToken token = default)
        {
            var enabledKeys = await (from x in dbContext.EntityAnalysisModelRequestXpath
                    join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                    where x.EnableOverride == 1
                          && (x.Deleted == 0 || x.Deleted == null)
                          && (m.Deleted == 0 || m.Deleted == null)
                          && m.TenantRegistryId == tenantRegistryId
                    select new { x.Name, ModelId = m.Id })
                .Distinct()
                .ToListAsync(token);

            if (enabledKeys.Count == 0)
            {
                return [];
            }

            var names = enabledKeys.Select(s => s.Name).Distinct().ToList();

            var modelRows = await dbContext.EntityAnalysisModelOverride
                .Where(w => names.Contains(w.OverrideKey)
                            && (w.Deleted == 0 || w.Deleted == null)
                            && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow)
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null))
                .Select(s => new EntityAnalysisModelOverrideKeyBinding
                {
                    OverrideKey = s.OverrideKey,
                    OverrideKeyValue = s.OverrideKeyValue,
                    ActivationRuleName = null,
                    OverrideKind = s.OverrideKind,
                    DeleteExpiryDate = s.DeleteExpiryDate,
                    CreatedDate = s.CreatedDate
                })
                .ToListAsync(token);

            var activationRuleRows = await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => names.Contains(w.OverrideKey)
                            && (w.Deleted == 0 || w.Deleted == null)
                            && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow)
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null))
                .Select(s => new EntityAnalysisModelOverrideKeyBinding
                {
                    OverrideKey = s.OverrideKey,
                    OverrideKeyValue = s.OverrideKeyValue,
                    ActivationRuleName = s.EntityAnalysisModelActivationRuleName,
                    OverrideKind = s.OverrideKind,
                    DeleteExpiryDate = s.DeleteExpiryDate,
                    CreatedDate = s.CreatedDate
                })
                .ToListAsync(token);

            var all = modelRows.Concat(activationRuleRows).ToList();

            return names.Select(name =>
                {
                    var rows = all.Where(w => w.OverrideKey == name).ToList();
                    var nextExpiry = rows.Where(w => w.DeleteExpiryDate != null)
                        .Select(s => s.DeleteExpiryDate).Min();
                    var lastCreated = rows.Where(w => w.CreatedDate != null).Select(s => s.CreatedDate).Max();

                    return new EntityAnalysisModelOverrideKeyQueryRow
                    {
                        OverrideKey = name,
                        EnabledOnModels = enabledKeys.Count(c => c.Name == name),
                        Values = rows.Select(s => s.OverrideKeyValue).Distinct().Count(),
                        Overrides = rows.Count,
                        Forced = rows.Count(c => c.OverrideKind == 1),
                        ActivationRules = rows.Where(w => w.ActivationRuleName != null)
                            .Select(s => s.ActivationRuleName).Distinct().Count(),
                        AllActivationRules = rows.Count(c => c.ActivationRuleName == null),
                        NextExpiryDate = nextExpiry == null
                            ? null
                            : new DateTimeOffset(DateTime.SpecifyKind(nextExpiry.Value, DateTimeKind.Utc)),
                        LastCreatedDate = lastCreated == null
                            ? null
                            : new DateTimeOffset(DateTime.SpecifyKind(lastCreated.Value, DateTimeKind.Utc))
                    };
                })
                .OrderBy(o => o.OverrideKey)
                .ToList();
        }

        public async Task<IEnumerable<EntityAnalysisModelOverrideValueQueryRow>> ExecuteValuesAsync(string overrideKey,
            int limit,
            CancellationToken token = default)
        {
            var modelRows = await dbContext.EntityAnalysisModelOverride
                .Where(w => w.OverrideKey == overrideKey
                            && (w.Deleted == 0 || w.Deleted == null)
                            && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow)
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null))
                .Select(s => new EntityAnalysisModelOverrideValueBinding
                {
                    OverrideKeyValue = s.OverrideKeyValue,
                    Name = s.EntityAnalysisModel.Name,
                    EntityAnalysisModelGuid = s.EntityAnalysisModelGuid,
                    DeleteExpiryDate = s.DeleteExpiryDate,
                    CreatedDate = s.CreatedDate,
                    CreatedUser = s.CreatedUser,
                    OverrideKind = s.OverrideKind
                })
                .Distinct()
                .Take(limit)
                .ToListAsync(token);

            var activationRuleRows = await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => w.OverrideKey == overrideKey
                            && (w.Deleted == 0 || w.Deleted == null)
                            && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow)
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null))
                .Select(s => new EntityAnalysisModelOverrideValueBinding
                {
                    OverrideKeyValue = s.OverrideKeyValue,
                    Name = s.EntityAnalysisModel.Name,
                    EntityAnalysisModelGuid = s.EntityAnalysisModelGuid,
                    DeleteExpiryDate = s.DeleteExpiryDate,
                    CreatedDate = s.CreatedDate,
                    CreatedUser = s.CreatedUser,
                    OverrideKind = s.OverrideKind
                })
                .Distinct()
                .Take(limit)
                .ToListAsync(token);

            return modelRows.Concat(activationRuleRows)
                .GroupBy(g => new { g.OverrideKeyValue, g.EntityAnalysisModelGuid })
                .Select(Reduce)
                .OrderBy(o => o.OverrideKeyValue)
                .ThenBy(o => o.Name)
                .Take(limit)
                .ToList();
        }

        private static EntityAnalysisModelOverrideValueQueryRow Reduce(
            IGrouping<object, EntityAnalysisModelOverrideValueBinding> group)
        {
            var rows = group.OrderBy(o => o.CreatedDate ?? DateTime.MaxValue).ToList();
            var earliest = rows[0];
            var forced = rows.Any(a => a.OverrideKind == 1);
            var expiry = rows.Max(m => m.DeleteExpiryDate);

            return new EntityAnalysisModelOverrideValueQueryRow
            {
                OverrideKeyValue = earliest.OverrideKeyValue,
                Name = earliest.Name,
                EntityAnalysisModelGuid = earliest.EntityAnalysisModelGuid,
                OverrideKind = forced ? (byte)1 : (byte)0,
                CreatedUser = earliest.CreatedUser,
                CreatedDate = earliest.CreatedDate == null
                    ? null
                    : new DateTimeOffset(DateTime.SpecifyKind(earliest.CreatedDate.Value, DateTimeKind.Utc)),
                DeleteExpiryDate = expiry == null
                    ? null
                    : new DateTimeOffset(DateTime.SpecifyKind(expiry.Value, DateTimeKind.Utc))
            };
        }

        public async Task<IEnumerable<EntityAnalysisModelOverrideQueryRow>> ExecuteAsync(string overrideKey,
            string overrideKeyValue,
            CancellationToken token = default)
        {
            var overrideRows = await dbContext.EntityAnalysisModelOverride
                .Where(w => w.OverrideKey == overrideKey && w.OverrideKeyValue == overrideKeyValue
                                                         && (w.Deleted == 0 || w.Deleted == null)
                                                         && (w.DeleteExpiryDate == null ||
                                                             w.DeleteExpiryDate > DateTime.UtcNow)
                                                         && w.EntityAnalysisModel.TenantRegistryId ==
                                                         tenantRegistryId)
                .Select(s => new
                {
                    s.EntityAnalysisModelGuid,
                    s.DeleteExpiryDate,
                    s.OverrideKind
                }).ToListAsync(token: token);

            var models = await
                (from m in dbContext.EntityAnalysisModel
                    join x in dbContext.EntityAnalysisModelRequestXpath
                        on m.Id equals x.EntityAnalysisModelId
                    where x.EnableOverride == 1
                          && (x.Deleted == 0 || x.Deleted == null)
                          && (m.Deleted == 0 || m.Deleted == null)
                          && m.TenantRegistryId == tenantRegistryId
                          && x.Name == overrideKey
                    select m).Distinct().ToListAsync(token);

            var responses = models
                .Select(model =>
                {
                    var overrideRow = overrideRows.FirstOrDefault(s => s.EntityAnalysisModelGuid == model.Guid);
                    return new EntityAnalysisModelOverrideQueryRow
                    {
                        Name = model.Name,
                        EntityAnalysisModelGuid = model.Guid,
                        HasOverride = overrideRow != null,
                        OverrideKind = overrideRow?.OverrideKind ?? 0,
                        DeleteExpiryDate = overrideRow?.DeleteExpiryDate == null
                            ? null
                            : new DateTimeOffset(DateTime.SpecifyKind(overrideRow.DeleteExpiryDate.Value,
                                DateTimeKind.Utc))
                    };
                }).ToList();

            return responses;
        }
    }
}