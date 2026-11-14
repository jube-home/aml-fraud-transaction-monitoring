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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using LinqToDB;
using CaseRow = Jube.Data.Poco.Case;

namespace Jube.Service.Query.CaseChecklist
{
    public sealed class CaseChecklistAuditWriter(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<long> WriteAsync(CaseRow caseRow, CaseChecklistAction action, string actor,
            CaseChecklistItem? item, string? itemName, string detail, int? timelineEventTypeId, string? timelineBefore,
            DateTime now, CancellationToken token)
        {
            var events = new CaseChecklistEventRepository(dbContext, tenantRegistryId);

            var audit = await events.InsertAsync(new CaseChecklistEvent
            {
                CaseId = caseRow.Id,
                CaseChecklistItemId = item?.Id,
                CaseWorkflowChecklistItemGuid = item?.CaseWorkflowChecklistItemGuid,
                ActionId = (byte)action,
                Actor = actor,
                CreatedDate = now,
                Detail = detail
            }, token).ConfigureAwait(false);

            if (!timelineEventTypeId.HasValue)
            {
                return audit.Id;
            }

            var timelineEventId = await dbContext.InsertWithInt32IdentityAsync(new CaseEvent
            {
                CaseId = caseRow.Id,
                CaseEventTypeId = timelineEventTypeId.Value,
                CaseKey = caseRow.CaseKey,
                CaseKeyValue = caseRow.CaseKeyValue,
                Before = timelineBefore ?? itemName,
                After = audit.Id.ToString(),
                CreatedUser = actor,
                CreatedDate = now
            }, token: token).ConfigureAwait(false);

            await events.SetCaseEventIdAsync(audit.Id, timelineEventId, token).ConfigureAwait(false);

            return audit.Id;
        }
    }
}
