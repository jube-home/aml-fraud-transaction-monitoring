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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetExhaustiveSearchInstancePromotedTrialInstanceRocQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstancePromotedTrialInstanceRocQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<Dto>> ExecuteAsync(
            int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            var promotedExhaustiveSearchInstanceTrialInstanceId = await dbContext
                .ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && w.Active == 1
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == 0
                        || w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == null)
                    && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id)
                .Select(s => s.ExhaustiveSearchInstanceTrialInstanceId)
                .FirstOrDefaultAsync(token);

            var points = await dbContext.ExhaustiveSearchInstancePromotedTrialInstanceRoc
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstanceId == promotedExhaustiveSearchInstanceTrialInstanceId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id)
                .Select(s => new
                {
                    s.Id,
                    s.Score,
                    s.FalsePositive,
                    s.TrueNegative,
                    s.TruePositive,
                    s.FalseNegative
                }).ToListAsync(token);

            return points.Select(s => new Dto
            {
                Id = s.Id,
                Score = s.Score.GetValueOrDefault(),
                Fpr = Rate(s.FalsePositive.GetValueOrDefault(), s.TrueNegative.GetValueOrDefault()),
                Tpr = Rate(s.TruePositive.GetValueOrDefault(), s.FalseNegative.GetValueOrDefault())
            }).ToList();
        }

        private static double Rate(int numerator, int other)
        {
            var denominator = (long)numerator + other;
            return denominator == 0 ? 0d : (double)numerator / denominator;
        }

        public class Dto
        {
            public int Id { get; set; }
            public double Score { get; set; }
            public double Fpr { get; set; }
            public double Tpr { get; set; }
        }
    }
}