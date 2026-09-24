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

    public class GetModelEntityGuidsQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<IReadOnlyDictionary<(string Kind, int Id), Guid>> ExecuteAsync(int entityAnalysisModelId,
            CancellationToken token = default)
        {
            var value = new Dictionary<(string Kind, int Id), Guid>();

            Add("RequestXPath", await dbContext.EntityAnalysisModelRequestXpath
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("InlineFunction", await dbContext.EntityAnalysisModelInlineFunction
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("InlineScript", await dbContext.EntityAnalysisModelInlineScript
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisInlineScriptId != null)
                .Select(s => new { Id = s.EntityAnalysisInlineScriptId!.Value, s.Guid }).ToListAsync(token)
                .ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("GatewayRule", await dbContext.EntityAnalysisModelGatewayRule
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("AbstractionRule", await dbContext.EntityAnalysisModelAbstractionRule
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("AbstractionCalculation", await dbContext.EntityAnalysisModelAbstractionCalculation
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("TtlCounter", await dbContext.EntityAnalysisModelTtlCounter
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("Sanction", await dbContext.EntityAnalysisModelSanction
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("HttpAdaptation", await dbContext.EntityAnalysisModelHttpAdaptation
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            Add("ActivationRule", await dbContext.EntityAnalysisModelActivationRule
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s => new { s.Id, s.Guid }).ToListAsync(token).ConfigureAwait(false), x => (x.Id, x.Guid));

            return value;

            void Add<T>(string kind, IEnumerable<T> rows, Func<T, (int Id, Guid Guid)> select)
            {
                foreach (var (id, guid) in rows.Select(select))
                {
                    value.TryAdd((kind, id), guid);
                }
            }
        }
    }
}