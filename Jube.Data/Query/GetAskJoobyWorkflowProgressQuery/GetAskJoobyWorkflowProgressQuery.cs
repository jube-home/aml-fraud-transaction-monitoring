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

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using LinqToDB;

namespace Jube.Data.Query.GetAskJoobyWorkflowProgressQuery
{
    public class GetAskJoobyWorkflowProgressQuery(DbContext dbContext, int conversationId)
    {
        public Task<List<string>> ExecuteCalledOperationsAsync(CancellationToken token = default)
        {
            return (from call in dbContext.AskJoobyToolCall
                    join run in dbContext.AskJoobyRun on call.AskJoobyRunId equals run.Id
                    where run.AskJoobyConversationId == conversationId
                          && call.ToolName != null
                          && call.StatusId == (byte)AskJoobyToolCallStatus.Completed
                    select call.ToolName)
                .Distinct()
                .ToListAsync(token);
        }

        public async Task<string> ExecuteLastWorkflowIdAsync(CancellationToken token = default)
        {
            var arguments = await (from call in dbContext.AskJoobyToolCall
                    join run in dbContext.AskJoobyRun on call.AskJoobyRunId equals run.Id
                    where run.AskJoobyConversationId == conversationId
                          && call.ToolName == "GetGraphWorkflow"
                          && call.StatusId == (byte)AskJoobyToolCallStatus.Completed
                          && call.Arguments != null
                    orderby call.Id descending
                    select call.Arguments)
                .FirstOrDefaultAsync(token);

            if (arguments is null)
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(arguments);

                return document.RootElement.TryGetProperty("id", out var id)
                       && id.ValueKind == JsonValueKind.String
                    ? id.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public Task<string> ExecuteLastAgentTypeAsync(CancellationToken token = default)
        {
            return (from routing in dbContext.AskJoobyRouting
                    where routing.AskJoobyConversationId == conversationId
                          && routing.AgentType != null
                    orderby routing.Id descending
                    select routing.AgentType)
                .FirstOrDefaultAsync(token);
        }

        public Task<List<AskJoobyWorkflowStep>> ExecuteAuditAsync(CancellationToken token = default)
        {
            return (from step in dbContext.AskJoobyWorkflowStep
                    where step.AskJoobyConversationId == conversationId
                    orderby step.Id
                    select step)
                .ToListAsync(token);
        }

        public async Task<List<AskJoobyWorkflowStepDto>> ExecuteReportedAsync(
            string workflowId, CancellationToken token = default)
        {
            var rows = await (from step in dbContext.AskJoobyWorkflowStep
                    where step.AskJoobyConversationId == conversationId
                          && step.WorkflowId == workflowId
                          && step.NodeId != null
                    orderby step.Id
                    select new AskJoobyWorkflowStepDto
                    {
                        NodeId = step.NodeId,
                        Completed = step.Completed == 1
                    })
                .ToListAsync(token);

            return
            [
                .. rows
                    .GroupBy(row => row.NodeId!)
                    .Select(row => row.Last())
            ];
        }
    }
}