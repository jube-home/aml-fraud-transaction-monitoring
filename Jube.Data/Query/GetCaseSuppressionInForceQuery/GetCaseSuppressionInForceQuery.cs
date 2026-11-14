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

namespace Jube.Data.Query.GetCaseSuppressionInForceQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetCaseSuppressionInForceQuery(DbContext dbContext, int tenantRegistryId)
    {
        public const int Cap = 200;

        public async Task<List<GetCaseSuppressionInForceQueryDto>> ExecuteAsync(
            IReadOnlyCollection<(string Key, string Value)> pairs, CancellationToken token = default)
        {
            if (pairs.Count == 0)
            {
                return [];
            }

            var keys = pairs.Select(p => p.Key).Distinct().ToList();
            var values = pairs.Select(p => p.Value).Distinct().ToList();
            var now = DateTime.UtcNow;

            var models = await (from o in dbContext.EntityAnalysisModelOverride
                join m in dbContext.EntityAnalysisModel on o.EntityAnalysisModelGuid equals m.Guid
                where m.TenantRegistryId == tenantRegistryId && (m.Deleted == 0 || m.Deleted == null)
                                                           && (o.Deleted == 0 || o.Deleted == null)
                                                           && (o.DeleteExpiryDate == null || o.DeleteExpiryDate > now)
                                                           && keys.Contains(o.OverrideKey)
                                                           && values.Contains(o.OverrideKeyValue)
                select new GetCaseSuppressionInForceQueryDto
                {
                    Id = o.Id, IsRule = false, OverrideKey = o.OverrideKey, OverrideKeyValue = o.OverrideKeyValue,
                    EntityAnalysisModelName = m.Name, OverrideKindId = o.OverrideKind ?? 0,
                    DeleteExpiryDate = o.DeleteExpiryDate, CreatedUser = o.CreatedUser, CreatedDate = o.CreatedDate
                }).Take(Cap * 2).ToListAsync(token).ConfigureAwait(false);

            var rules = await (from o in dbContext.EntityAnalysisModelActivationRuleOverride
                join m in dbContext.EntityAnalysisModel on o.EntityAnalysisModelGuid equals m.Guid
                where m.TenantRegistryId == tenantRegistryId && (m.Deleted == 0 || m.Deleted == null)
                                                           && (o.Deleted == 0 || o.Deleted == null)
                                                           && (o.DeleteExpiryDate == null || o.DeleteExpiryDate > now)
                                                           && keys.Contains(o.OverrideKey)
                                                           && values.Contains(o.OverrideKeyValue)
                select new GetCaseSuppressionInForceQueryDto
                {
                    Id = o.Id, IsRule = true, OverrideKey = o.OverrideKey, OverrideKeyValue = o.OverrideKeyValue,
                    EntityAnalysisModelName = m.Name, RuleName = o.EntityAnalysisModelActivationRuleName,
                    OverrideKindId = o.OverrideKind ?? 0, DeleteExpiryDate = o.DeleteExpiryDate,
                    CreatedUser = o.CreatedUser, CreatedDate = o.CreatedDate
                }).Take(Cap * 2).ToListAsync(token).ConfigureAwait(false);

            var wanted = pairs.ToHashSet();

            return models.Concat(rules).Where(r => wanted.Contains((r.OverrideKey, r.OverrideKeyValue)))
                .OrderBy(r => r.OverrideKey).ThenBy(r => r.OverrideKeyValue).ThenBy(r => r.EntityAnalysisModelName)
                .ThenBy(r => r.RuleName).Take(Cap).ToList();
        }
    }
}
