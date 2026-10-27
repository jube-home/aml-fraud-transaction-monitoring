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

namespace Jube.Engine.CaseEscalation
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto.Options;
    using LinqToDB;

    public sealed record CaseMove(
        int CaseId,
        Guid CaseWorkflowGuid,
        string CaseKey,
        string CaseKeyValue,
        Guid FromStatusGuid,
        string FromStatusName,
        Guid ToStatusGuid,
        string ToStatusName,
        bool ReleaseLock);

    public static class CaseStatusMover
    {
        public const string AutomationUser = "Administrator";
        public const int EscalationEventTypeId = 17;

        public static async Task<bool> MoveAsync(DbContext dbContext, CaseMove move, DateTime now,
            CancellationToken token = default)
        {
            var moved = await dbContext.Case
                .Where(c => c.Id == move.CaseId && c.CaseWorkflowStatusGuid == move.FromStatusGuid
                                                && c.ClosedStatusId == 0)
                .Set(s => s.CaseWorkflowStatusGuid, move.ToStatusGuid)
                .UpdateAsync(token);

            if (moved == 0)
            {
                return false;
            }

            if (move.ReleaseLock)
            {
                await dbContext.Case.Where(c => c.Id == move.CaseId)
                    .Set(s => s.Locked, (byte?)0)
                    .Set(s => s.LockedUser, (string)null)
                    .Set(s => s.LockedDate, (DateTime?)null)
                    .UpdateAsync(token);
            }

            await new CaseStatusTransitionRepository(dbContext).EnterAsync(move.CaseId, move.CaseWorkflowGuid,
                move.ToStatusGuid, CaseTransitionEnterReason.Escalated, AutomationUser, now, token);

            await new CaseEventRepository(dbContext, AutomationUser).InsertAsync(new CaseEvent
            {
                CaseId = move.CaseId,
                CaseKey = move.CaseKey,
                CaseKeyValue = move.CaseKeyValue,
                CaseEventTypeId = EscalationEventTypeId,
                CreatedUser = AutomationUser,
                Before = move.FromStatusName,
                After = move.ToStatusName
            }, token);

            return true;
        }
    }
}
