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

    public class GetEntityAnalysisModelActivationRuleOverrideQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetEntityAnalysisModelActivationRuleOverrideQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRuleOverrideQueryRow>> ExecuteAsync(
            Guid entityAnalysisModelGuid, string overrideKey,
            string overrideKeyValue, CancellationToken token = default)
        {
            var overrideRows = await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => w.OverrideKey == overrideKey && w.OverrideKeyValue == overrideKeyValue
                                                         && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                                                         && (w.Deleted == 0 || w.Deleted == null)
                                                         && (w.DeleteExpiryDate == null ||
                                                             w.DeleteExpiryDate > DateTime.UtcNow)
                                                         && w.EntityAnalysisModel.TenantRegistryId ==
                                                         tenantRegistryId)
                .Select(s => new
                {
                    s.EntityAnalysisModelActivationRuleName,
                    s.DeleteExpiryDate,
                    s.OverrideKind
                }).ToListAsync(token);

            var models = await
                (from m in dbContext.EntityAnalysisModel
                    join x in dbContext.EntityAnalysisModelRequestXpath
                        on m.Id equals x.EntityAnalysisModelId
                    join r in dbContext.EntityAnalysisModelActivationRule
                        on m.Id equals r.EntityAnalysisModelId
                    where x.EnableOverride == 1
                          && r.EnableOverride == 1
                          && (x.Deleted == 0 || x.Deleted == null)
                          && (m.Deleted == 0 || m.Deleted == null)
                          && (r.Deleted == 0 || r.Deleted == null)
                          && m.TenantRegistryId == tenantRegistryId
                          && x.Name == overrideKey
                          && m.Guid == entityAnalysisModelGuid
                          && (r.OverrideKey == null || r.OverrideKey == "" || r.OverrideKey == overrideKey)
                    select new
                    {
                        r.Name,
                        m.Guid,
                        r.Id,
                        r.EnableForce
                    }).Distinct().ToListAsync(token);

            var responses = models
                .Select(model =>
                {
                    var overrideRow =
                        overrideRows.FirstOrDefault(s => s.EntityAnalysisModelActivationRuleName == model.Name);
                    return new EntityAnalysisModelActivationRuleOverrideQueryRow
                    {
                        Name = model.Name,
                        EntityAnalysisModelGuid = model.Guid,
                        EntityAnalysisModelActivationRuleOverrideId = model.Id,
                        EnableForce = model.EnableForce == 1,
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