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
using Jube.Data.Context;

namespace Jube.Data.Query
{
    public class GetEntityAnalysisModelSuppressionQuery
    {
        private readonly DbContext _dbContext;
        private readonly int _tenantRegistryId;

        public GetEntityAnalysisModelSuppressionQuery(DbContext dbContext, string userName)
        {
            _dbContext = dbContext;
            _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(string suppressionKey, string suppressionKeyValue)
        {
            var suppressions = _dbContext.EntityAnalysisModelSuppression
                .Where(w => w.SuppressionKey == suppressionKey && w.SuppressionKeyValue == suppressionKeyValue
                                                               && (w.Deleted == 0 || w.Deleted == null)
                                                               && w.EntityAnalysisModel.TenantRegistryId ==
                                                               _tenantRegistryId)
                .Select(s => s.EntityAnalysisModelId).ToList();

            var models =
                (from m in _dbContext.EntityAnalysisModel
                    join x in _dbContext.EntityAnalysisModelRequestXpath
                        on m.Id equals x.EntityAnalysisModelId
                    where x.EnableSuppression == 1
                          && (x.Deleted == 0 || x.Deleted == null)
                          && (m.Deleted == 0 || m.Deleted == null)
                          && m.TenantRegistryId == _tenantRegistryId
                          && x.Name == suppressionKey
                    select m).Distinct().ToList();

            var responses = models
                .Select(model => new Dto
                {
                    Name = model.Name,
                    EntityAnalysisModelId = model.Id,
                    Suppression = suppressions.Contains(model.Id)
                }).ToList();

            return responses;
        }

        public class Dto
        {
            public string Name { get; set; }
            public int EntityAnalysisModelId { get; set; }
            public bool Suppression { get; set; }
        }
    }
}