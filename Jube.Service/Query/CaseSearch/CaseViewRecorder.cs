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

using Jube.Data.Context;
using Jube.Data.Repository;
using LinqToDB;

namespace Jube.Service.Query.CaseSearch
{
    internal static class CaseViewRecorder
    {
        public static async Task RecordAsync(DbContext dbContext, string userName, int caseId,
            CancellationToken token = default)
        {
            var lastEventId = await dbContext.CaseEvent.Where(w => w.CaseId == caseId)
                .MaxAsync(m => (int?)m.Id, token).ConfigureAwait(false);

            var riskSequence = await dbContext.CaseRiskAssessment.Where(w => w.CaseId == caseId)
                .MaxAsync(m => (int?)m.Sequence, token).ConfigureAwait(false);

            await new CaseViewRepository(dbContext, userName).UpsertAsync(caseId, lastEventId, riskSequence, token)
                .ConfigureAwait(false);
        }
    }
}
