/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class DynamicSqlQueryLogRepository(DbContext dbContext)
    {
        public Task InsertAsync(string sql, int? conversationId, string createdUser, bool succeeded,
            int? rowCount, long durationMs, string errorMessage, CancellationToken token = default)
        {
            return dbContext.InsertAsync(new DynamicSqlQueryLog
            {
                AskJoobyConversationId = conversationId,
                CreatedUser = createdUser,
                Sql = sql,
                Succeeded = succeeded,
                RowCount = rowCount,
                DurationMs = durationMs,
                ErrorMessage = errorMessage,
                CreatedDate = DateTime.UtcNow
            }, token: token);
        }

        public Task<List<DynamicSqlQueryLog>> ListByConversationAsync(int conversationId,
            CancellationToken token = default)
        {
            return dbContext.DynamicSqlQueryLog
                .Where(w => w.AskJoobyConversationId == conversationId)
                .OrderBy(o => o.Id)
                .ToListAsync(token);
        }
    }
}