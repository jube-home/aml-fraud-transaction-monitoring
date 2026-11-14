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

namespace Jube.Data.Query.GetNextEntityAnalysisModelsReprocessingRuleInstanceQuery
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetNextEntityAnalysisModelsReprocessingRuleInstanceQuery(DbContext dbContext)
    {
        public static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromMinutes(30);

        public async Task<GetNextEntityAnalysisModelsReprocessingRuleInstanceQueryDto> ExecuteAsync(
            int entityAnalysisModelId, CancellationToken token = default, TimeSpan? staleAfter = null)
        {
            await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, token);

            try
            {
                var cutoff = DateTime.UtcNow - (staleAfter ?? DefaultStaleAfter);

                await dbContext.EntityAnalysisModelReprocessingRuleInstance
                    .Where(w => (w.StatusId == 1 || w.StatusId == 2 || w.StatusId == 3)
                                && (w.Deleted == 0 || w.Deleted == null)
                                && w.EntityAnalysisModelReprocessingRule.EntityAnalysisModelId ==
                                entityAnalysisModelId
                                && (w.UpdatedDate == null || w.UpdatedDate < cutoff)
                                && (w.StartedDate == null || w.StartedDate < cutoff))
                    .Set(s => s.StatusId, Convert.ToByte(0))
                    .UpdateAsync(token);

                var query = await dbContext.EntityAnalysisModelReprocessingRuleInstance
                    .Where(w => w.StatusId == 0
                                && (w.Deleted == 0 || w.Deleted == null)
                                && w.EntityAnalysisModelReprocessingRule.Active == 1
                                && (w.EntityAnalysisModelReprocessingRule.Deleted == 0
                                    || w.EntityAnalysisModelReprocessingRule.Deleted == null)
                                && w.EntityAnalysisModelReprocessingRule.EntityAnalysisModelId ==
                                entityAnalysisModelId)
                    .OrderBy(o => o.EntityAnalysisModelReprocessingRule.Priority)
                    .ThenBy(o => o.Id)
                    .Select(s =>
                        new GetNextEntityAnalysisModelsReprocessingRuleInstanceQueryDto
                        {
                            EntityAnalysisModelsReprocessingRuleId = s.EntityAnalysisModelReprocessingRule
                                .Id,
                            ReprocessingIntervalValue =
                                s.EntityAnalysisModelReprocessingRule.ReprocessingValue,
                            ReprocessingSample = s.EntityAnalysisModelReprocessingRule.ReprocessingSample,
                            RuleScriptTypeId = s.EntityAnalysisModelReprocessingRule.RuleScriptTypeId,
                            CoderRuleScript = s.EntityAnalysisModelReprocessingRule.CoderRuleScript,
                            BuilderRuleScript = s.EntityAnalysisModelReprocessingRule.BuilderRuleScript,
                            EntityAnalysisModelId = s.EntityAnalysisModelReprocessingRule.EntityAnalysisModelId.Value,
                            Id =
                                s.Id,
                            ReprocessingIntervalType = s.EntityAnalysisModelReprocessingRule.ReprocessingInterval
                        }
                    )
                    .FirstOrDefaultAsync(token);

                if (query != null)
                {
                    await dbContext.EntityAnalysisModelReprocessingRuleInstance
                        .Where(d =>
                            d.Id ==
                            query.Id)
                        .Set(s => s.StatusId, Convert.ToByte(1))
                        .Set(s => s.StartedDate, DateTime.UtcNow)
                        .Set(s => s.UpdatedDate, DateTime.UtcNow)
                        .UpdateAsync(token);
                }

                await dbContext.CommitTransactionAsync(token);

                return query;
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(token);
                throw;
            }
        }
    }
}