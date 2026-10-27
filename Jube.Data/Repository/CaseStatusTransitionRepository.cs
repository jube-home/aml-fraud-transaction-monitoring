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
    using Dto.Options;
    using LinqToDB;
    using Poco;

    public class CaseStatusTransitionRepository(DbContext dbContext)
    {
        private const int MaxAttempts = 5;

        public Task<CaseStatusTransition> GetOpenAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseStatusTransition.FirstOrDefaultAsync(w => w.CaseId == caseId
                                                                          && w.ExitedDate == null, token);
        }

        public Task<List<CaseStatusTransition>> ListForCaseAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseStatusTransition.Where(w => w.CaseId == caseId)
                .OrderBy(o => o.EnteredDate).ThenBy(o => o.Id).ToListAsync(token);
        }

        public async Task EnterAsync(int caseId, Guid caseWorkflowGuid, Guid caseWorkflowStatusGuid,
            CaseTransitionEnterReason reason, string user, DateTime at, CancellationToken token = default)
        {
            for (var attempt = 1;; attempt++)
            {
                var open = await GetOpenAsync(caseId, token);

                if (open != null)
                {
                    if (open.CaseWorkflowStatusGuid == caseWorkflowStatusGuid)
                    {
                        return;
                    }

                    await CloseRowAsync(open, at, user, CaseTransitionExitReason.StatusChanged, token);
                }

                try
                {
                    await dbContext.InsertWithInt64IdentityAsync(new CaseStatusTransition
                    {
                        CaseId = caseId,
                        CaseWorkflowGuid = caseWorkflowGuid,
                        CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                        PreviousCaseWorkflowStatusGuid = open?.CaseWorkflowStatusGuid,
                        EnterReasonId = (byte)reason,
                        EnteredDate = at,
                        EnteredUser = user
                    }, token: token);

                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts && UniqueViolationCheck.Is(ex))
                {
                }
            }
        }

        public async Task CloseAsync(int caseId, DateTime at, string user, CancellationToken token = default)
        {
            var open = await GetOpenAsync(caseId, token);

            if (open != null)
            {
                await CloseRowAsync(open, at, user, CaseTransitionExitReason.Closed, token);
            }
        }

        private Task CloseRowAsync(CaseStatusTransition open, DateTime at, string user,
            CaseTransitionExitReason reason, CancellationToken token)
        {
            var seconds = (int)Math.Max(0, (at - open.EnteredDate).TotalSeconds);

            return dbContext.CaseStatusTransition.Where(w => w.Id == open.Id && w.ExitedDate == null)
                .Set(s => s.ExitedDate, at)
                .Set(s => s.ExitedUser, user)
                .Set(s => s.ExitReasonId, (byte?)reason)
                .Set(s => s.DurationSeconds, seconds)
                .UpdateAsync(token);
        }
    }
}
