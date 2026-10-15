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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Query.GetExhaustiveSearchInstancePromotionHistoryQuery
{
    public class GetExhaustiveSearchInstancePromotionHistoryQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstancePromotionHistoryQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<GetExhaustiveSearchInstancePromotionHistoryQueryDto>> ExecuteAsync(
            int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            var promotions = await dbContext
                .ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == 0
                        || w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == null)
                    && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .OrderBy(o => o.Id)
                .Select(s => new
                {
                    s.Id,
                    s.ExhaustiveSearchInstanceTrialInstanceId,
                    s.Score,
                    s.TopologyComplexity,
                    s.CreatedDate,
                    s.Active
                }).ToListAsync(token);

            if (promotions.Count == 0)
            {
                return new List<GetExhaustiveSearchInstancePromotionHistoryQueryDto>();
            }

            var trialInstanceIds = await dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(w => w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId)
                .OrderBy(o => o.Id)
                .Select(s => s.Id)
                .ToListAsync(token);

            var dtos = new List<GetExhaustiveSearchInstancePromotionHistoryQueryDto>(promotions.Count);
            var previousTrialInstanceCount = 0;

            foreach (var promotion in promotions)
            {
                var trialInstanceCount = CountUpTo(trialInstanceIds,
                    promotion.ExhaustiveSearchInstanceTrialInstanceId.GetValueOrDefault());

                dtos.Add(new GetExhaustiveSearchInstancePromotionHistoryQueryDto
                {
                    Id = promotion.Id,
                    ExhaustiveSearchInstanceTrialInstanceId =
                        promotion.ExhaustiveSearchInstanceTrialInstanceId.GetValueOrDefault(),
                    Score = promotion.Score.GetValueOrDefault(),
                    TopologyComplexity = promotion.TopologyComplexity.GetValueOrDefault(),
                    CreatedDate = promotion.CreatedDate.GetValueOrDefault(),
                    Active = promotion.Active == 1,
                    TrialInstances = Math.Max(trialInstanceCount - previousTrialInstanceCount, 0),
                    TrialInstancesToDate = trialInstanceCount
                });

                previousTrialInstanceCount = Math.Max(previousTrialInstanceCount, trialInstanceCount);
            }

            return dtos;
        }

        private static int CountUpTo(List<int> sortedIds, int id)
        {
            int lo = 0, hi = sortedIds.Count;

            while (lo < hi)
            {
                var mid = (lo + hi) / 2;

                if (sortedIds[mid] <= id)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }
    }
}