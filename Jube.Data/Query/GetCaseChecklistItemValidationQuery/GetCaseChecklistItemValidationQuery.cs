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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Data.Query.GetCaseChecklistItemValidationQuery
{
    public class GetCaseChecklistItemValidationQuery(DbContext dbContext)
    {
        public Task<GetCaseChecklistItemValidationQueryDto> ExecuteAsync(Guid caseWorkflowChecklistItemGuid,
            CancellationToken token = default)
        {
            return (from i in dbContext.CaseWorkflowChecklistItem
                where i.Guid == caseWorkflowChecklistItemGuid && (i.Deleted == 0 || i.Deleted == null)
                select new GetCaseChecklistItemValidationQueryDto
                {
                    CaseWorkflowChecklistItemGuid = i.Guid,
                    ItemName = i.Name,
                    EnableValidation = i.EnableValidation == 1,
                    ValidateSql = i.ValidateSql,
                    ValidateMessage = i.ValidateMessage
                }).FirstOrDefaultAsync(token);
        }
    }
}
