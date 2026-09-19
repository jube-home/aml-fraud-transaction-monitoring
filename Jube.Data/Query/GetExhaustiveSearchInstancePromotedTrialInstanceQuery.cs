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

    public class GetExhaustiveSearchInstancePromotedTrialInstanceQuery
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;

        public GetExhaustiveSearchInstancePromotedTrialInstanceQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public GetExhaustiveSearchInstancePromotedTrialInstanceQuery(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<Dto>> ExecuteAsync(
            int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return await dbContext
                .ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == 0
                        || w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == null)
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue))
                .OrderByDescending(o => o.Id)
                .Select(s =>
                    new Dto
                    {
                        Id = s.Id,
                        Score = s.Score.Value,
                        CreatedDate = s.CreatedDate.Value,
                        Active = s.Active.Value == 1,
                        TopologyComplexity = s.TopologyComplexity.Value,
                        Json = s.Json,
                        ExhaustiveSearchInstanceTrialInstanceId = s.ExhaustiveSearchInstanceTrialInstanceId.Value
                    }
                ).ToListAsync(token);
        }

        public class Dto
        {
            public int Id { get; set; }
            public int ExhaustiveSearchInstanceTrialInstanceId { get; set; }
            public bool Active { get; set; }
            public double Score { get; set; }
            public int TopologyComplexity { get; set; }
            public string Json { get; set; }
            public DateTime CreatedDate { get; set; }
        }
    }
}