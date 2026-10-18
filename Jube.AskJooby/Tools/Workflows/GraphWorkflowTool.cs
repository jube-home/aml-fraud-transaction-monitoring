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
using Jube.AskJooby.Workflows;
using Jube.AskJooby.Workflows.Models;

namespace Jube.AskJooby.Tools.Workflows
{
    public static class GraphWorkflowTool
    {
        private static readonly HashSet<string> noise = new(StringComparer.Ordinal)
        {
            "that", "this", "with", "from", "into", "over", "when", "which", "where", "what", "rule",
            "rules", "than", "then", "they", "them", "have", "been", "will", "would", "could", "each",
            "more", "most", "some", "such", "only", "same", "also", "does", "make", "made", "using",
            "used", "jube", "platform", "every", "there", "their", "about", "other", "within"
        };

        [Description(
            "Finds the graph-directed workflow that fits a task, by matching the task against each " +
            "workflow's exemplars, title and definition. Use this FIRST for any build or diagnosis " +
            "request, before searching the Pasture: a workflow states the order the objects have to " +
            "be made in, which the documentation describes but does not enforce. Returns the " +
            "candidates with their ids; call GetGraphWorkflow with the id for the steps. Matching is " +
            "by word overlap and costs nothing -- no retrieval, no model call -- so it works when a " +
            "turn's search allowance is already spent.")]
        public static string FindGraphWorkflow(
            [Description("The task in the caller's own words, e.g. 'count transactions over 243.64 in three days'.")]
            string task)
        {
            var words = Words(task);

            if (words.Count == 0)
            {
                return Catalogue();
            }

            var scored = All()
                .Select(workflow => new { workflow, score = Score(workflow, words) })
                .Where(candidate => candidate.score > 0)
                .OrderByDescending(candidate => candidate.score)
                .ThenBy(candidate => candidate.workflow.Id, StringComparer.Ordinal)
                .Take(5)
                .ToList();

            if (scored.Count == 0)
            {
                return "No workflow matched that task. The full set is short -- read it and decide "
                       + "whether one of these is really what is being asked for:\n\n" + Catalogue();
            }

            var builder = new StringBuilder();
            builder.AppendLine($"{scored.Count} workflow(s) match. Call GetGraphWorkflow with an id for the steps.");
            builder.AppendLine();

            foreach (var candidate in scored)
            {
                builder.AppendLine($"## {candidate.workflow.Id}");
                builder.AppendLine($"**{candidate.workflow.Title}** ({candidate.workflow.Nodes.Count} steps)");
                builder.AppendLine(candidate.workflow.Definition);
                builder.AppendLine();
                builder.AppendLine($"Use this one when: {candidate.workflow.Disambiguation}");
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        [Description(
            "Returns a graph-directed workflow in full: every step in order, which agent owns it, " +
            "which catalogued operations it calls, what each step produces, what it requires before " +
            "it can run, and what goes wrong when it is done out of order. Follow it in the order " +
            "given rather than deciding the order yourself -- the edges are the point, because a Jube " +
            "rule is several objects that only work in one sequence. Call FindGraphWorkflow first if " +
            "you do not already have an id.")]
        public static string GetGraphWorkflow(
            [Description("The workflow id, exactly as FindGraphWorkflow returned it.")]
            string id)
        {
            var workflow = All().FirstOrDefault(candidate =>
                string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));

            if (workflow is null)
            {
                return $"There is no workflow with the id '{id}'. The ids are:\n\n" + Catalogue();
            }

            var builder = new StringBuilder();
            builder.AppendLine($"# {workflow.Title}");
            builder.AppendLine($"Workflow {workflow.Number}, id `{workflow.Id}`.");
            builder.AppendLine();
            builder.AppendLine($"**Definition.** {workflow.Definition}");
            builder.AppendLine();
            builder.AppendLine($"**Construction.** {workflow.Construction}");
            builder.AppendLine();
            builder.AppendLine($"**Use this rather than another type when.** {workflow.Disambiguation}");

            if (!string.IsNullOrWhiteSpace(workflow.OrderingRule))
            {
                builder.AppendLine();
                builder.AppendLine($"**Ordering rule.** {workflow.OrderingRule}");
            }

            if (workflow.Prerequisites is { Count: > 0 })
            {
                builder.AppendLine();

                builder.AppendLine("**Begins only after.** " + string.Join(", ", workflow.Prerequisites)
                                                             + " -- this graph consumes artefacts those produce and does not produce them "
                                                             + "itself.");
            }

            builder.AppendLine();
            builder.AppendLine($"## Steps ({workflow.Nodes.Count}), in order");

            var position = 0;

            foreach (var node in workflow.Nodes)
            {
                position++;
                builder.AppendLine();
                builder.AppendLine($"### {position}. {node.Title}  `{node.Id}`");
                builder.AppendLine(node.Intent);

                builder.AppendLine(node.Boundary == WorkflowBoundary.Agent
                    ? $"- Owned by: `{node.Agent}`"
                    : "- **Operator step.** The product cannot perform this. Say what needs doing "
                      + "and who must do it; do not look for a tool.");

                if (node.Tools.Count > 0)
                {
                    builder.AppendLine($"- Operations: {string.Join(", ", node.Tools)}");
                }

                if (node.Requires.Count > 0)
                {
                    builder.AppendLine($"- Requires first: {string.Join(", ", node.Requires)}");
                }

                if (node.Produces.Count > 0)
                {
                    builder.AppendLine($"- Produces: {string.Join(", ", node.Produces)}");
                }

                if (!string.IsNullOrWhiteSpace(node.Hazard))
                {
                    builder.AppendLine($"- If skipped or done out of order: {node.Hazard}");
                }
            }

            return builder.ToString().TrimEnd();
        }

        private static IEnumerable<GraphWorkflow> All()
        {
            return RuleTaxonomyWorkflows.All.Concat(PlatformOpsWorkflows.All)
                .Concat(AdvancedAnalyticsTrainingWorkflows.All);
        }

        private static string Catalogue()
        {
            return string.Join("\n", All().Select(workflow => $"- `{workflow.Id}` -- {workflow.Title}"));
        }

        private static int Score(GraphWorkflow workflow, HashSet<string> words)
        {
            var score = workflow.Exemplars.Sum(exemplar => Words(exemplar).Count(words.Contains) * 3);

            score += Words(workflow.Title).Count(words.Contains) * 2;
            score += Words(workflow.Definition).Count(words.Contains);

            return score;
        }

        private static HashSet<string> Words(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            return value
                .Split([' ', '\t', '\n', '\r', ',', '.', ';', ':', '(', ')', '"', '\'', '/', '-'],
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.ToLowerInvariant())
                .Where(word => word.Length > 3 && !noise.Contains(word))
                .ToHashSet(StringComparer.Ordinal);
        }
    }
}