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

using System.ComponentModel;
using System.Text;
using Jube.AskJooby.Tools.Workflows.Interfaces;
using Jube.AskJooby.Workflows;
using Jube.AskJooby.Workflows.Models;

namespace Jube.AskJooby.Tools.Workflows
{
    public sealed class WorkflowProgressTool(IWorkflowProgressClient client)
    {
        [Description(
            "Shows how far a graph-directed workflow has got in THIS conversation: every step with " +
            "whether it is done, and which step is next. Call it before starting work on a workflow " +
            "and after any interruption -- an earlier turn may have completed several steps already, " +
            "and redoing them wastes the budget that stopped the turn in the first place. A step is " +
            "counted done when the operations it names have been run successfully, or when an agent " +
            "said so explicitly.")]
        public async Task<string> GetWorkflowProgress(
            [Description("The workflow id, as FindGraphWorkflow or GetGraphWorkflow returned it.")]
            string workflowId,
            CancellationToken token = default)
        {
            var workflow = WorkflowPosition.Find(workflowId);

            if (workflow is null)
            {
                return $"There is no workflow with the id '{workflowId}'.";
            }

            var progress = await client.ReadAsync(workflow.Id, token).ConfigureAwait(false);
            var builder = new StringBuilder();
            var outstanding = new List<GraphWorkflowNode>();
            var position = 0;

            builder.AppendLine($"# {workflow.Title} -- progress in this conversation");
            builder.AppendLine();

            foreach (var node in workflow.Nodes)
            {
                position++;
                var state = WorkflowPosition.State(node, progress);

                if (state is NodeState.Outstanding or NodeState.Denied)
                {
                    outstanding.Add(node);
                }

                builder.AppendLine($"{position}. [{Describe(state)}] {node.Title}  `{node.Id}`");
            }

            builder.AppendLine();

            if (outstanding.Count == 0)
            {
                builder.AppendLine("Every step is done. Check the result against the workflow's ordering rule "
                                   + "before calling the whole thing finished.");

                return builder.ToString().TrimEnd();
            }

            var next = outstanding[0];
            builder.AppendLine($"**Next: {next.Title}** (`{next.Id}`)");
            builder.AppendLine(next.Intent);

            if (next.Requires.Count > 0)
            {
                builder.AppendLine($"- Requires first: {string.Join(", ", next.Requires)}");
            }

            if (!string.IsNullOrWhiteSpace(next.Hazard))
            {
                builder.AppendLine($"- If skipped or done out of order: {next.Hazard}");
            }

            builder.AppendLine();

            builder.AppendLine($"{outstanding.Count} step(s) outstanding. If any of the steps marked done above "
                               + "was not really done -- the operation ran against something else, or it failed "
                               + "in a way the status did not catch -- say so with RecordWorkflowStep rather than "
                               + "working around it.");

            return builder.ToString().TrimEnd();
        }

        [Description(
            "States whether a step of a workflow is done, overruling what the tool calls imply. Use " +
            "it to mark a step done that has no operations of its own -- an operator step, or one " +
            "carried out by hand -- and to mark a step NOT done when it looks done but is not, which " +
            "is the more important of the two: a wrong inference left standing silently skips work. " +
            "Always give the reason.")]
        public async Task<string> RecordWorkflowStep(
            [Description("The workflow id.")] string workflowId,
            [Description("The step id, exactly as the workflow listed it.")]
            string nodeId,
            [Description("True where the step is done, false where it is not and must still be carried out.")]
            bool completed,
            [Description("Why you are saying this, in one sentence.")]
            string note,
            CancellationToken token = default)
        {
            var workflow = WorkflowPosition.Find(workflowId);

            if (workflow is null)
            {
                return $"There is no workflow with the id '{workflowId}', so nothing was recorded.";
            }

            var node = workflow.Nodes.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, nodeId, StringComparison.OrdinalIgnoreCase));

            if (node is null)
            {
                return $"'{workflowId}' has no step '{nodeId}', so nothing was recorded. Its steps are: "
                       + string.Join(", ", workflow.Nodes.Select(candidate => candidate.Id));
            }

            await client.RecordAsync(workflow.Id, node.Id, completed, note, token).ConfigureAwait(false);

            return completed
                ? $"Recorded: '{node.Title}' is done. It will show as done for the rest of this conversation, "
                  + "including on a later turn."
                : $"Recorded: '{node.Title}' is NOT done, and still has to be carried out whatever the tool "
                  + "calls suggest.";
        }

        private static string Describe(NodeState state)
        {
            return state switch
            {
                NodeState.Stated => "done, stated",
                NodeState.Inferred => "done, inferred from the calls made",
                NodeState.Denied => "NOT done, stated -- an inference was overruled",
                _ => "outstanding"
            };
        }
    }
}