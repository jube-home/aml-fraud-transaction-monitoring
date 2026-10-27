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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Concurrency;
    using Context;
    using LinqToDB;
    using Poco;

    public class CaseRiskAssessmentRepository(DbContext dbContext)
    {
        private const int MaxSequenceAttempts = 20;

        public Task<CaseRiskAssessment> GetLatestAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseRiskAssessment.Where(w => w.CaseId == caseId)
                .OrderByDescending(o => o.Sequence).FirstOrDefaultAsync(token);
        }

        public async Task<Dictionary<int, CaseRiskAssessment>> GetLatestForCasesAsync(
            IReadOnlyCollection<int> caseIds, CancellationToken token = default)
        {
            var ids = caseIds.ToList();

            var rows = await dbContext.CaseRiskAssessment
                .Where(w => ids.Contains(w.CaseId)
                            && w.Sequence == dbContext.CaseRiskAssessment.Where(o => o.CaseId == w.CaseId)
                                .Max(o => o.Sequence))
                .ToListAsync(token);

            return rows.ToDictionary(k => k.CaseId);
        }

        public Task<List<CaseRiskAssessment>> ListHistoryAsync(int caseId, int take,
            CancellationToken token = default)
        {
            return dbContext.CaseRiskAssessment.Where(w => w.CaseId == caseId)
                .OrderByDescending(o => o.Sequence).Take(take).ToListAsync(token);
        }

        public async Task<CaseRiskAssessment> InsertAsync(CaseRiskAssessment model,
            CancellationToken token = default)
        {
            for (var attempt = 1;; attempt++)
            {
                var highest = await dbContext.CaseRiskAssessment.Where(w => w.CaseId == model.CaseId)
                    .MaxAsync(m => (int?)m.Sequence, token);

                model.Sequence = highest.GetValueOrDefault() + 1;
                model.CreatedDate = DateTime.UtcNow;

                try
                {
                    model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token);

                    return model;
                }
                catch (Exception ex) when (attempt < MaxSequenceAttempts && UniqueViolationCheck.Is(ex))
                {
                }
            }
        }
    }
}
