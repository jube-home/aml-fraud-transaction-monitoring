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

using System.Linq;
using FluentAssertions;
using Jube.AskJooby.Tools.Workflows;
using Jube.AskJooby.Workflows;
using Jube.AskJooby.Workflows.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class GraphWorkflowToolTests
    {
        [Fact]
        public void ATaskInTheCallersOwnWordsFindsTheWorkflowForIt()
        {
            var found = GraphWorkflowTool.FindGraphWorkflow(
                "count transactions over an amount in a three day window");

            found.Should().Contain("velocity-and-volume",
                "a counter over a rolling window is the velocity type, and the exemplars were "
                + "written in the words a caller actually uses so this needs no retrieval at all");
        }

        [Fact]
        public void ATrainingTaskInTheCallersOwnWordsFindsTheTrainingWorkflow()
        {
            var found = GraphWorkflowTool.FindGraphWorkflow(
                "try and build me a logistic regression model to find fraud");

            found.Should().Contain("train-advanced-analytics-trial-model",
                "training a model on cached data is now a graph, not something re-derived each time");
        }

        [Fact]
        public void EveryWorkflowCanBeFetchedByTheIdTheSearchReturns()
        {
            var ids = RuleTaxonomyWorkflows.All.Concat(PlatformOpsWorkflows.All)
                .Concat(AdvancedAnalyticsTrainingWorkflows.All)
                .Select(workflow => workflow.Id).ToList();

            ids.Should().NotBeEmpty();

            foreach (var id in ids)
            {
                var graph = GraphWorkflowTool.GetGraphWorkflow(id);

                graph.Should().NotContain("There is no workflow", $"'{id}' was listed, so it must resolve");
                graph.Should().Contain("Steps (");
            }
        }

        [Fact]
        public void TheStepsCarryTheEdgesAndNotJustTheProse()
        {
            var graph = GraphWorkflowTool.GetGraphWorkflow("velocity-and-volume");

            graph.Should().Contain("Requires first:");
            graph.Should().Contain("Produces:");
            graph.Should().Contain("Operations:");
            graph.Should().Contain("in order");
        }

        [Fact]
        public void TheTtlCounterReferenceConventionIsInTheGraphWhereAnAgentWillSeeIt()
        {
            var graph = GraphWorkflowTool.GetGraphWorkflow("velocity-and-volume");

            graph.Should().Contain("TTLCounter.",
                "the dot-notation reference is stated on the node, so an agent following the graph "
                + "never has to reconstruct it from documentation");
        }

        [Fact]
        public void AnOperatorStepSaysSoRatherThanNamingAnAgentThatCouldActOnIt()
        {
            var operatorStep = PlatformOpsWorkflows.All
                .SelectMany(workflow => workflow.Nodes.Select(node => new { workflow, node }))
                .First(step => step.node.Boundary == WorkflowBoundary.Operator);

            var graph = GraphWorkflowTool.GetGraphWorkflow(operatorStep.workflow.Id);

            graph.Should().Contain("Operator step.");
            graph.Should().Contain("do not look for a tool");
        }

        [Fact]
        public void AnUnknownIdAnswersWithTheIdsThatDoExist()
        {
            var graph = GraphWorkflowTool.GetGraphWorkflow("no-such-workflow");

            graph.Should().Contain("There is no workflow");
            graph.Should().Contain("velocity-and-volume", "the whole catalogue is short enough to just show");
        }

        [Fact]
        public void ATaskMatchingNothingSaysSoRatherThanGuessing()
        {
            var found = GraphWorkflowTool.FindGraphWorkflow("qwertyuiop zxcvbnm");

            found.Should().Contain("No workflow matched",
                "a task outside the taxonomy is far more likely than a taxonomy missing it, and "
                + "quietly returning the least-bad graph would send the agent down the wrong one");
        }

        [Fact]
        public void AQuestionMadeEntirelyOfCommonWordsGetsTheCatalogueRatherThanAConfidentMatch()
        {
            var found = GraphWorkflowTool.FindGraphWorkflow("a rule that is used when there is more of them");

            found.Should().Contain("velocity-and-volume").And.Contain("cardinality");

            found.Should().NotContain("workflow(s) match",
                "nothing was actually matched, so it must not be presented as a match");
        }
    }
}