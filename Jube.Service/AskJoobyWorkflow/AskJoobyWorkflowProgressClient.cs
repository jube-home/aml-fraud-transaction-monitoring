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

using Jube.AskJooby.Tools.Workflows.Interfaces;
using Jube.AskJooby.Workflows.Models;
using Jube.Data.Poco;
using Jube.Data.Query.GetAskJoobyWorkflowProgressQuery;
using Jube.Data.Repository;

namespace Jube.Service.AskJoobyWorkflow
{
    public sealed class AskJoobyWorkflowProgressClient(
        GetAskJoobyWorkflowProgressQuery query,
        AskJoobyWorkflowStepRepository repository,
        int conversationId) : IWorkflowProgressClient
    {
        public async Task<WorkflowProgress> ReadAsync(string workflowId, CancellationToken token = default)
        {
            var operations = await query.ExecuteCalledOperationsAsync(token).ConfigureAwait(false);
            var reported = await query.ExecuteReportedAsync(workflowId, token).ConfigureAwait(false);

            return new WorkflowProgress(
                operations.ToHashSet(StringComparer.Ordinal),
                reported.ToDictionary(step => step.NodeId, step => step.Completed, StringComparer.Ordinal));
        }

        public Task RecordAsync(string workflowId, string nodeId, bool completed, string? note,
            CancellationToken token = default)
        {
            return repository.InsertAsync(new AskJoobyWorkflowStep
            {
                AskJoobyConversationId = conversationId,
                WorkflowId = workflowId,
                NodeId = nodeId,
                Completed = (short)(completed ? 1 : 0),
                Note = note
            }, token);
        }
    }
}