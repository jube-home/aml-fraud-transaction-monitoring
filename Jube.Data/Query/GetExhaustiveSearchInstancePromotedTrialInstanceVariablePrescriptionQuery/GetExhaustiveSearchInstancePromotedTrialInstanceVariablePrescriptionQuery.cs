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

namespace Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery
{
    public class GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery(DbContext dbContext,
            string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => (int?)s.TenantRegistryId).FirstOrDefault() ?? -1;
        }

        public async Task<IEnumerable<GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQueryDto>>
            ExecuteAsync(
                int exhaustiveSearchInstancePromotedTrialInstanceId, CancellationToken token = default)
        {
            var variables =
                from t in dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                from s in dbContext
                    .ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                    .Where(w =>
                        w.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id).DefaultIfEmpty()
                from g in dbContext
                    .ExhaustiveSearchInstancePromotedTrialInstance
                    .Where(w => w.ExhaustiveSearchInstanceTrialInstanceId == t.ExhaustiveSearchInstanceTrialInstanceId)
                from v in dbContext
                    .ExhaustiveSearchInstanceVariable
                    .Where(w => w.Id == t.ExhaustiveSearchInstanceVariableId)
                from ptv in dbContext
                    .ExhaustiveSearchInstancePromotedTrialInstanceVariable
                    .Where(w => w.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id).DefaultIfEmpty()
                where g.Id == exhaustiveSearchInstancePromotedTrialInstanceId
                      && g.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                          .EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                      && (t.Removed == 0 || t.Removed == null)
                      && (t.Deleted == 0 || t.Deleted == null)
                      && (v.Deleted == 0 || v.Deleted == null)
                      && (g.Deleted == 0 || g.Deleted == null)
                      && (g.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == 0
                          || g.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == null)
                orderby s.Sensitivity descending, ptv.Positive descending
                select new GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQueryDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    VariableMean = v.Mean.Value,
                    VariableStandardDeviation = v.StandardDeviation.Value,
                    VariableMaximum = v.Maximum.Value,
                    VariableMinimum = v.Minimum.Value,
                    PrescriptionMean = ptv.Mean ?? 0,
                    PrescriptionStandardDeviation = ptv.StandardDeviation ?? 0,
                    PrescriptionMaximum = ptv.Maximum ?? 0,
                    PrescriptionMinimum = ptv.Minimum ?? 0,
                    Sensitivity = s.Sensitivity ?? 0,
                    Positive = ptv.Positive,
                    PromotedTrialInstanceVariableId = ptv.Id
                };

            var joined = await variables.ToListAsync(token);

            var promotedTrialInstanceVariableIds = joined.Select(s => s.PromotedTrialInstanceVariableId).ToList();

            var histograms = await dbContext.ExhaustiveSearchInstancePrescriptionHistogram
                .Where(w => promotedTrialInstanceVariableIds.Contains(
                    w.ExhaustiveSearchInstancePromotedTrialInstanceVariableId.GetValueOrDefault()))
                .ToListAsync(token);

            foreach (var dto in joined)
            {
                foreach (var histogram in histograms.Where(w =>
                             w.ExhaustiveSearchInstancePromotedTrialInstanceVariableId ==
                             dto.PromotedTrialInstanceVariableId))
                {
                    dto.HistogramValues.Add(new HistogramValue
                    {
                        Frequency = histogram.Frequency.GetValueOrDefault(),
                        Bin = histogram.BinRangeStart.GetValueOrDefault(),
                        BinEnd = histogram.BinRangeEnd.GetValueOrDefault()
                    });
                }
            }

            return joined;
        }
    }
}