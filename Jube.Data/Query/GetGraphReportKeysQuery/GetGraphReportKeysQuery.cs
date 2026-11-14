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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Data.Query.GetGraphReportKeysQuery
{
    public class GetGraphReportKeysQuery(DbContext dbContext)
    {
        public Task<List<GetGraphReportKeysQueryDto>> ExecuteAsync(int tenantRegistryId,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelRequestXpath
                .InnerJoin(dbContext.EntityAnalysisModel, (x, m) => x.EntityAnalysisModelId == m.Id,
                    (x, m) => new { Xpath = x, Model = m })
                .Where(j => j.Model.TenantRegistryId == tenantRegistryId
                            && j.Model.Active == 1
                            && (j.Model.Deleted == null || j.Model.Deleted == 0)
                            && j.Xpath.Active == 1
                            && (j.Xpath.Deleted == null || j.Xpath.Deleted == 0)
                            && j.Xpath.Name != null)
                .Select(j => new GetGraphReportKeysQueryDto
                {
                    Key = j.Xpath.Name,
                    ModelName = j.Model.Name,
                    ReportTable = j.Xpath.ReportTable == 1
                })
                .ToListAsync(token);
        }
    }
}
