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
using Jube.Data.Context;

namespace Jube.Data.Query
{
    public class GetExhaustiveSearchInstancePromotedTrialInstanceLearningCurveQuery
    {
        private readonly DbContext _dbContext;
        private readonly int _tenantRegistryId;

        public GetExhaustiveSearchInstancePromotedTrialInstanceLearningCurveQuery(DbContext dbContext, string userName)
        {
            _dbContext = dbContext;
            _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(
            int exhaustiveSearchInstanceId)
        {
            return _dbContext
                .ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == _tenantRegistryId)
                .OrderBy(o => o.Id)
                .Select(s =>
                    new Dto
                    {
                        Score = Math.Round(s.Score.Value, 2),
                        CreatedDate = s.CreatedDate.Value
                    }
                );
        }

        public class Dto
        {
            public double Score { get; set; }
            public DateTime CreatedDate { get; set; }
        }
    }
}