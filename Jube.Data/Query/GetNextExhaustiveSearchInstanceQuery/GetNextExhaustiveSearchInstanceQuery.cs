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
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository.Models;
using LinqToDB;

namespace Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery
{
    public class GetNextExhaustiveSearchInstanceQuery(DbContext dbContext)
    {
        public async Task<GetNextExhaustiveSearchInstanceQueryDto> ExecuteAsync(CancellationToken token = default)
        {
            await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);

            try
            {
                var query = await dbContext.ExhaustiveSearchInstance
                    .Where(w => w.StatusId == 0
                                && (w.Deleted == 0 || w.Deleted == null)
                                && w.EntityAnalysisModelId != null
                                && w.EntityAnalysisModel.TenantRegistryId != null)
                    .OrderBy(o => o.Id)
                    .Select(s =>
                        new GetNextExhaustiveSearchInstanceQueryDto
                        {
                            Id = s.Id,
                            EntityAnalysisModelId = s.EntityAnalysisModelId.GetValueOrDefault(),
                            TenantRegistryId = s.EntityAnalysisModel.TenantRegistryId.GetValueOrDefault(),
                            FilterJson = s.FilterJson,
                            FilterTokens = s.FilterTokens,
                            FilterSql = s.FilterSql,
                            Anomaly = s.Anomaly == 1,
                            AnomalyProbability = s.AnomalyProbability,
                            Filter = s.Filter == 1,
                            CategoricalOptionsJson = s.CategoricalOptionsJson,
                            DataSourceId = s.DataSourceId ?? 0,
                            ExhaustiveInstanceCsvId = s.ExhaustiveInstanceCsvId,
                            ExhaustiveInstanceJsonId = s.ExhaustiveInstanceJsonId,
                            BaseRate = s.BaseRate,
                            AnomalyAlgorithmId = s.AnomalyAlgorithmId,
                            AnomalyParametersJson = s.AnomalyParametersJson,
                            CalibrationMethod = s.CalibrationMethodId == ExhaustiveCalibrationMethod.Automatic
                                ? "automatic"
                                : s.CalibrationMethodId == ExhaustiveCalibrationMethod.Platt
                                    ? "platt"
                                    : s.CalibrationMethodId == ExhaustiveCalibrationMethod.Isotonic
                                        ? "isotonic"
                                        : s.CalibrationMethodId == ExhaustiveCalibrationMethod.Beta
                                            ? "beta"
                                            : null,
                            Bootstrap = s.Bootstrap == null || s.Bootstrap == 1,
                            BootstrapTo = s.BootstrapTo,
                            BootstrapWeightFactor = s.BootstrapWeightFactor,
                            ChallengerModelBaseProbability = s.ChallengerModelBaseProbability
                        }
                    )
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (query != null)
                {
                    await dbContext.ExhaustiveSearchInstance
                        .Where(d =>
                            d.Id ==
                            query.Id)
                        .Set(s => s.StatusId, Convert.ToByte(1))
                        .Set(s => s.UpdatedDate, DateTime.UtcNow)
                        .UpdateAsync(token).ConfigureAwait(false);
                }

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);

                return query;
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(token).ConfigureAwait(false);
                throw;
            }
        }
    }
}