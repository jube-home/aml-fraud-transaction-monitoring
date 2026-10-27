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

namespace Jube.Engine.CaseRisk
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query.GetCaseRiskCandidatesQuery;
    using Jube.CaseRisk;
    using LinqToDB;
    using log4net;

    public static class CaseRiskSweep
    {
        public static async Task<CaseRiskSweepResult> RunAsync(DbContext dbContext, CaseRiskSettings settings,
            TimeSpan sweepInterval, int batchSize, DateTime now, ILog log, CancellationToken token = default)
        {
            var candidates = await new GetCaseRiskCandidatesQuery(dbContext)
                .ExecuteAsync(now - sweepInterval, batchSize, token);

            var assessor = new CaseRiskAssessor(dbContext, settings);
            var written = 0;
            var failed = 0;

            foreach (var candidate in candidates)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    if (await assessor.AssessAsync(candidate, now, token))
                    {
                        written++;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    log.Error($"Case Risk: could not assess case id {candidate.CaseId}: {ex}");

                    await dbContext.Case.Where(w => w.Id == candidate.CaseId)
                        .Set(s => s.RiskCheckedDate, now)
                        .Set(s => s.RiskRecheckTriggerId, (byte?)null)
                        .UpdateAsync(token);
                }
            }

            return new CaseRiskSweepResult(candidates.Count, written, failed);
        }
    }

    public sealed record CaseRiskSweepResult(int Examined, int Written, int Failed);
}
