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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository.Models;
using LinqToDB;

namespace Jube.Data.Query.GetAskJoobyConversationRetrievedDocsQuery
{
    public class GetAskJoobyConversationRetrievedDocsQuery(DbContext dbContext)
    {
        private const int SqlFetchLimit = 60;
        private const int MaxDistinctResults = 12;

        public async Task<List<string>> ExecuteAsync(int conversationId, CancellationToken token = default)
        {
            var recent = await dbContext.AskJoobyToolCall
                .Where(tc => tc.ToolName == "SearchRag"
                             && tc.StatusId == (byte)AskJoobyToolCallStatus.Completed && tc.Result != null
                             && !tc.Result.StartsWith("No matching Bites found")
                             && dbContext.AskJoobyRun.Any(r =>
                                 r.Id == tc.AskJoobyRunId && r.AskJoobyConversationId == conversationId))
                .OrderByDescending(tc => tc.CreatedDate)
                .Select(tc => tc.Result)
                .Take(SqlFetchLimit)
                .ToListAsync(token).ConfigureAwait(false);

            var distinct = recent.Distinct().Take(MaxDistinctResults).ToList();
            distinct.Reverse();

            return distinct;
        }
    }
}