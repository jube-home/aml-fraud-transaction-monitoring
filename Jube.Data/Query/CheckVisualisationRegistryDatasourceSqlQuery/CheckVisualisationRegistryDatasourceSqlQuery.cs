/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Reporting;
using Jube.Data.Reporting.Check;
using Jube.Data.Repository.Exceptions;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Data.Query.CheckVisualisationRegistryDatasourceSqlQuery
{
    public class CheckVisualisationRegistryDatasourceSqlQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<IReadOnlyList<SqlFinding>> ExecuteAsync(int visualisationRegistryId, string sql,
            string reportConnectionString, IReadOnlyDictionary<string, string> allowedTables,
            CancellationToken token = default)
        {
            var registries = await dbContext.VisualisationRegistry
                .Where(w => w.Id == visualisationRegistryId
                            && w.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .Select(w => w.ContextKindId).ToListAsync(token).ConfigureAwait(false);

            if (registries.Count == 0)
            {
                throw new KeyNotFoundException();
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw SqlValidationFailedException.Coded(DatasourceMessages.SqlEmpty);
            }

            var parameters = await DatasourceParameterDefaults
                .LoadAsync(dbContext, tenantRegistryId, visualisationRegistryId,
                    registries[0] == ContextParameters.CaseKind, token).ConfigureAwait(false);

            return await SqlPreflight.CheckAsync(sql, reportConnectionString, allowedTables, parameters, token)
                .ConfigureAwait(false);
        }
    }
}
