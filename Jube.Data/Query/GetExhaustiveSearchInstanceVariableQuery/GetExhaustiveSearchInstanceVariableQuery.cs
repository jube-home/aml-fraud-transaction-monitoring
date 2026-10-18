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

// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Query.GetExhaustiveSearchInstanceVariableQuery
{
    public class GetExhaustiveSearchInstanceVariableQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstanceVariableQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<GetExhaustiveSearchInstanceVariableQueryDto>> ExecuteAsync(
            int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            var histograms = await dbContext.ExhaustiveSearchInstanceVariableHistogram
                .Where(w =>
                    w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId ==
                    tenantRegistryId
                    && w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstanceVariable.Deleted == 0
                        || w.ExhaustiveSearchInstanceVariable.Deleted == null)
                    && (w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstance.Deleted == 0
                        || w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstance.Deleted == null))
                .ToListAsync(token);

            var variables = await dbContext.ExhaustiveSearchInstanceVariable
                .Where(w =>
                    w.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstance.Deleted == 0 || w.ExhaustiveSearchInstance.Deleted == null))
                .OrderBy(o => o.Id)
                .ToListAsync(token);

            var categoricalSources = await dbContext.ExhaustiveSearchInstanceCategoricalField
                .Where(w => w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId
                            && (w.Deleted == 0 || w.Deleted == null))
                .ToDictionaryAsync(k => k.Id, v => v.SourceName, token);

            var joined = new List<GetExhaustiveSearchInstanceVariableQueryDto>();

            foreach (var variable in variables)
            {
                var join = new GetExhaustiveSearchInstanceVariableQueryDto
                {
                    Id = variable.Id,
                    Mean = variable.Mean.GetValueOrDefault(),
                    StandardDeviation = variable.StandardDeviation.GetValueOrDefault(),
                    Name = variable.Name,
                    Kurtosis = variable.Kurtosis.GetValueOrDefault(),
                    Skewness = variable.Skewness.GetValueOrDefault(),
                    Maximum = variable.Maximum.GetValueOrDefault(),
                    Iqr = variable.Iqr.GetValueOrDefault(),
                    DistinctValues = variable.DistinctValues.GetValueOrDefault(),
                    Correlation = variable.Correlation.GetValueOrDefault(),
                    CorrelationAbsRank = variable.CorrelationAbsRank.GetValueOrDefault(),
                    ProcessingTypeId = variable.ProcessingTypeId.GetValueOrDefault(),
                    NormalisationType = variable.NormalisationTypeId switch
                    {
                        0 => "No",
                        1 => "Binary",
                        2 => "Z Score",
                        _ => "Default"
                    },
                    CategoricalSourceName = variable.CategoricalFieldId.HasValue
                                            && categoricalSources.TryGetValue(variable.CategoricalFieldId.Value,
                                                out var sourceName)
                        ? sourceName
                        : null,
                    CategoricalKind = variable.CategoricalKind,
                    CategoricalLevel = variable.CategoricalLevel
                };

                foreach (var histogram in histograms
                             .Where(w =>
                                 w.ExhaustiveSearchInstanceVariableId == variable.Id))
                {
                    join.HistogramValues.Add(new HistogramValue
                    {
                        Frequency = histogram.Frequency.GetValueOrDefault(),
                        Bin = histogram.BinRangeStart.GetValueOrDefault()
                    });
                }

                joined.Add(join);
            }

            return joined;
        }
    }
}