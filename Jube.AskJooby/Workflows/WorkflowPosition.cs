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

using Jube.AskJooby.Workflows.Models;

namespace Jube.AskJooby.Workflows
{
    public static class WorkflowPosition
    {
        public static GraphWorkflow? Find(string? id)
        {
            return id is null
                ? null
                : RuleTaxonomyWorkflows.All.Concat(PlatformOpsWorkflows.All)
                    .Concat(AdvancedAnalyticsTrainingWorkflows.All).FirstOrDefault(candidate =>
                        string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static NodeState State(GraphWorkflowNode node, WorkflowProgress progress)
        {
            if (progress.Reported.TryGetValue(node.Id, out var reported))
            {
                return reported ? NodeState.Stated : NodeState.Denied;
            }

            if (node.Tools.Count > 0 && node.Tools.All(progress.CalledOperations.Contains))
            {
                return NodeState.Inferred;
            }

            return NodeState.Outstanding;
        }

        public static IReadOnlyList<GraphWorkflowNode> Outstanding(
            GraphWorkflow workflow, WorkflowProgress progress)
        {
            return workflow.Nodes
                .Where(node => State(node, progress) is NodeState.Outstanding or NodeState.Denied)
                .ToList();
        }
    }
}