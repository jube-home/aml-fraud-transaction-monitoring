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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Tools.Workflows;
using Jube.AskJooby.Tools.Workflows.Interfaces;
using Jube.AskJooby.Workflows;
using Jube.AskJooby.Workflows.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class WorkflowProgressToolTests
    {
        private const string Workflow = "velocity-and-volume";

        [Fact]
        public async Task WithNothingDoneTheFirstStepIsTheNextOneAndNoneAreMarkedCompleteAsync()
        {
            var tool = new WorkflowProgressTool(new FakeWorkflowProgressClient());

            var progress = await tool.GetWorkflowProgress(Workflow, CancellationToken.None);

            progress.Should().Contain("outstanding");
            progress.Should().NotContain("done, inferred");
            progress.Should().NotContain("done, stated");
            progress.Should().Contain("**Next:", "an agent needs the step to take, not just a list");
        }

        [Fact]
        public async Task AStepWhoseOperationsHaveAllRunIsCountedDoneWithoutAnyoneSayingSoAsync()
        {
            var node = Node("counter");
            var client = new FakeWorkflowProgressClient { Called = [.. node.Tools] };
            var tool = new WorkflowProgressTool(client);

            var progress = await tool.GetWorkflowProgress(Workflow, CancellationToken.None);

            progress.Should().Contain("[done, inferred from the calls made] " + node.Title);
        }

        [Fact]
        public async Task AStepWithSomeOfItsOperationsRunIsNotCountedDoneAsync()
        {
            var node = Workflows().SelectMany(workflow => workflow.Nodes).First(candidate => candidate.Tools.Count > 1);
            var client = new FakeWorkflowProgressClient { Called = [node.Tools[0]] };
            var tool = new WorkflowProgressTool(client);

            var progress = await tool.GetWorkflowProgress(FindContaining(node), CancellationToken.None);

            progress.Should().Contain("[outstanding] " + node.Title);
        }

        [Fact]
        public async Task AnAgentCanOverruleAnInferenceThatSaysAStepIsDoneAsync()
        {
            var node = Node("counter");

            var client = new FakeWorkflowProgressClient
            {
                Called = [.. node.Tools],
                Reported = new Dictionary<string, bool> { ["reuse-check"] = true, [node.Id] = false }
            };

            var tool = new WorkflowProgressTool(client);

            var progress = await tool.GetWorkflowProgress(Workflow, CancellationToken.None);

            progress.Should().Contain("NOT done, stated");

            progress.Should().Contain("**Next: " + node.Title,
                "a step stated not done is outstanding work, so it has to be the step offered next");
        }

        [Fact]
        public async Task AnOperatorStepIsOnlyEverCompletedByBeingStatedAsync()
        {
            var operatorStep = PlatformOpsWorkflows.All
                .SelectMany(workflow => workflow.Nodes.Select(node => new { workflow, node }))
                .First(step => step.node.Boundary == WorkflowBoundary.Operator);

            var client = new FakeWorkflowProgressClient();
            var tool = new WorkflowProgressTool(client);

            var before = await tool.GetWorkflowProgress(operatorStep.workflow.Id, CancellationToken.None);
            before.Should().Contain("[outstanding] " + operatorStep.node.Title);

            await tool.RecordWorkflowStep(operatorStep.workflow.Id, operatorStep.node.Id, true,
                "The DBA confirmed it.", CancellationToken.None);

            var after = await tool.GetWorkflowProgress(operatorStep.workflow.Id, CancellationToken.None);
            after.Should().Contain("[done, stated] " + operatorStep.node.Title);
        }

        [Fact]
        public async Task TheReasonIsCarriedWithTheStatementRatherThanDiscardedAsync()
        {
            var client = new FakeWorkflowProgressClient();
            var tool = new WorkflowProgressTool(client);

            await tool.RecordWorkflowStep(Workflow, "counter", false,
                "The counter was created against the wrong model.", CancellationToken.None);

            client.Records.Should().ContainSingle()
                .Which.Note.Should().Be("The counter was created against the wrong model.");
        }

        [Fact]
        public async Task AStepIdThatIsNotInTheGraphIsRefusedAndTheRealOnesAreNamedAsync()
        {
            var client = new FakeWorkflowProgressClient();
            var tool = new WorkflowProgressTool(client);

            var answer = await tool.RecordWorkflowStep(Workflow, "no-such-step", true, "done", CancellationToken.None);

            answer.Should().Contain("has no step");
            answer.Should().Contain("counter", "the step ids are few enough to just show");
            client.Records.Should().BeEmpty("a statement about a step that does not exist must not be stored");
        }

        [Fact]
        public async Task AnUnknownWorkflowIsRefusedByBothOperationsRatherThanReportedEmptyAsync()
        {
            var client = new FakeWorkflowProgressClient();
            var tool = new WorkflowProgressTool(client);

            (await tool.GetWorkflowProgress("no-such-workflow", CancellationToken.None))
                .Should().Contain("There is no workflow");

            (await tool.RecordWorkflowStep("no-such-workflow", "counter", true, "done", CancellationToken.None))
                .Should().Contain("There is no workflow");

            client.Records.Should().BeEmpty();
        }

        [Fact]
        public async Task EveryStepDoneSaysSoRatherThanOfferingANextStepAsync()
        {
            var workflow = Workflows().First(candidate => candidate.Id == Workflow);

            var client = new FakeWorkflowProgressClient
            {
                Reported = workflow.Nodes.ToDictionary(node => node.Id, _ => true)
            };

            var tool = new WorkflowProgressTool(client);

            var progress = await tool.GetWorkflowProgress(Workflow, CancellationToken.None);

            progress.Should().Contain("Every step is done");
            progress.Should().NotContain("**Next:");
        }

        private static IEnumerable<GraphWorkflow> Workflows()
        {
            return RuleTaxonomyWorkflows.All.Concat(PlatformOpsWorkflows.All);
        }

        private static GraphWorkflowNode Node(string id)
        {
            return Workflows().First(workflow => workflow.Id == Workflow).Nodes.First(node => node.Id == id);
        }

        private static string FindContaining(GraphWorkflowNode node)
        {
            return Workflows().First(workflow => workflow.Nodes.Contains(node)).Id;
        }

        private sealed class FakeWorkflowProgressClient : IWorkflowProgressClient
        {
            public HashSet<string> Called { get; init; } = [];
            public Dictionary<string, bool> Reported { get; init; } = new();
            public List<(string WorkflowId, string NodeId, bool Completed, string? Note)> Records { get; } = [];

            public Task<WorkflowProgress> ReadAsync(string workflowId, CancellationToken token = default)
            {
                return Task.FromResult(new WorkflowProgress(Called, Reported));
            }

            public Task RecordAsync(string workflowId, string nodeId, bool completed, string? note,
                CancellationToken token = default)
            {
                Records.Add((workflowId, nodeId, completed, note));
                Reported[nodeId] = completed;

                return Task.CompletedTask;
            }
        }
    }
}