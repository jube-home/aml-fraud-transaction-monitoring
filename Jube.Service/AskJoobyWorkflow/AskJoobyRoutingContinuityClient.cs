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

using Jube.AskJooby.Routing.Interfaces;
using Jube.AskJooby.Routing.Models;
using Jube.AskJooby.Workflows.Models;
using Jube.Data.Query.GetAskJoobyWorkflowProgressQuery;

namespace Jube.Service.AskJoobyWorkflow
{
    public sealed class AskJoobyRoutingContinuityClient(GetAskJoobyWorkflowProgressQuery query)
        : IRoutingContinuityClient
    {
        public async Task<RoutingContinuity?> ReadAsync(CancellationToken token = default)
        {
            var workflowId = await query.ExecuteLastWorkflowIdAsync(token).ConfigureAwait(false);

            if (workflowId is null)
            {
                return null;
            }

            var agentType = await query.ExecuteLastAgentTypeAsync(token).ConfigureAwait(false);

            if (agentType is null)
            {
                return null;
            }

            var operations = await query.ExecuteCalledOperationsAsync(token).ConfigureAwait(false);
            var reported = await query.ExecuteReportedAsync(workflowId, token).ConfigureAwait(false);

            var progress = new WorkflowProgress(
                operations,
                reported.Where(row => row.NodeId is not null)
                    .ToDictionary(row => row.NodeId!, row => row.Completed));

            return new RoutingContinuity(agentType, workflowId, progress);
        }
    }
}