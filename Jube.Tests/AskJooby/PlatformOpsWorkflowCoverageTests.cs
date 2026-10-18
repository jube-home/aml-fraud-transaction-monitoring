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
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Workflows;
using Jube.AskJooby.Workflows.Models;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class PlatformOpsWorkflowCoverageTests
    {
        private static IEnumerable<(GraphWorkflow Workflow, GraphWorkflowNode Node)> Nodes()
        {
            return PlatformOpsWorkflows.All.SelectMany(workflow => workflow.Nodes.Select(node => (workflow, node)));
        }

        [Fact]
        public void EveryWorkflowCarriesEnoughSemanticsToBeChosenWithoutThePasture()
        {
            PlatformOpsWorkflows.All.Should().NotBeEmpty();

            foreach (var workflow in PlatformOpsWorkflows.All)
            {
                workflow.Definition.Should().NotBeNullOrWhiteSpace(workflow.Id);
                workflow.Construction.Should().NotBeNullOrWhiteSpace(workflow.Id);
                workflow.Disambiguation.Should().NotBeNullOrWhiteSpace(workflow.Id);

                workflow.Exemplars.Should().HaveCountGreaterThanOrEqualTo(6,
                    workflow.Id + " is reached by matching a complaint against its exemplars, and an "
                                + "operator describes an incident in whatever words the incident suggests");
            }

            PlatformOpsWorkflows.All.Select(workflow => workflow.Id).Should().OnlyHaveUniqueItems();
            PlatformOpsWorkflows.All.Select(workflow => workflow.Number).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void AnAgentNodeNamesARegisteredAgentAndAnOperatorNodeNamesNobody()
        {
            foreach (var (workflow, node) in Nodes())
            {
                if (node.Boundary == WorkflowBoundary.Operator)
                {
                    node.Agent.Should().BeEmpty(
                        $"{workflow.Id}/{node.Id} is a step only a person can perform, and naming an agent "
                        + "on it is how a human step gets silently adopted by one");
                    node.Tools.Should().BeEmpty(
                        $"{workflow.Id}/{node.Id} has no tools because the product exposes none that could "
                        + "do it");
                }
                else
                {
                    AgentDefinitions.All.Should().ContainKey(node.Agent,
                        $"{workflow.Id}/{node.Id} must name an agent that can actually be delegated to");
                }
            }
        }

        [Fact]
        public void EveryToolNamedAnywhereIsOneTheCatalogueCarries()
        {
            var catalogued = ServiceToolCatalogue.All.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

            foreach (var (workflow, node) in Nodes())
            {
                var phantom = node.Tools.Where(tool => !catalogued.Contains(tool)).ToList();

                phantom.Should().BeEmpty(
                    $"{workflow.Id}/{node.Id} names operations that do not exist: "
                    + string.Join(", ", phantom));
            }

            PlatformOpsWorkflows.All.Should().OnlyContain(
                workflow => workflow.Nodes.Any(node => node.Tools.Count > 0),
                "a workflow that calls nothing at all is a document, not a workflow");
        }

        [Fact]
        public void EveryNodeIsOwnedByAnAgentThatActuallyHoldsTheToolsItNames()
        {
            var broken = new List<string>();

            foreach (var (workflow, node) in Nodes())
            {
                if (string.IsNullOrEmpty(node.Agent))
                {
                    continue;
                }

                var held = AgentDefinitions.HierarchyToolNames
                    .GetValueOrDefault(node.Agent, []).ToHashSet(StringComparer.Ordinal);

                broken.AddRange(node.Tools.Where(tool => !held.Contains(tool))
                    .Select(tool => $"{workflow.Id}/{node.Id} -> {node.Agent} lacks {tool}"));
            }

            broken.Should().BeEmpty("a step delegated to an agent that cannot perform it is refused on "
                                    + "arrival, and the graph that named it reads as authoritative right up "
                                    + "to the moment it fails: " + string.Join("; ", broken));
        }

        [Fact]
        public void NoPlatformOperationsNodeClaimsToActOnThePlatform()
        {
            var allowed = new[]
            {
                "EntityAnalysisModelSynchronisationScheduleCreate"
            };

            foreach (var (workflow, node) in Nodes().Where(pair => pair.Node.Boundary == WorkflowBoundary.Agent))
            {
                foreach (var tool in node.Tools)
                {
                    var descriptor = ServiceToolCatalogue.All.Single(candidate => candidate.Name == tool);

                    var mutates = descriptor.Kind is OperationKind.Write or OperationKind.Delete
                                  && !allowed.Contains(tool)
                                  && !tool.StartsWith("OpenTelemetry", StringComparison.Ordinal);

                    mutates.Should().BeFalse(
                        $"{workflow.Id}/{node.Id} binds {tool}, which mutates. Every act on this platform "
                        + "is an operator at a shell, and a workflow that implies otherwise is the "
                        + "dangerous outcome this set exists to prevent.");
                }
            }
        }

        [Fact]
        public void EveryWorkflowHandsTheActOverWhereThereIsAnActToHandOver()
        {
            var diagnosticOnly = new[]
            {
                "platform-health-triage", "jube-application-operational-residue"
            };

            foreach (var workflow in
                     PlatformOpsWorkflows.All.Where(candidate => !diagnosticOnly.Contains(candidate.Id)))
            {
                workflow.Nodes.Should().Contain(node => node.Boundary == WorkflowBoundary.Operator,
                    workflow.Id + " reaches a point where something must be done, and the graph has to "
                                + "name the person who does it");
            }
        }

        [Fact]
        public void NoNodeRequiresAnArtefactNothingBeforeItProduces()
        {
            foreach (var workflow in PlatformOpsWorkflows.All)
            {
                var produced = new HashSet<string>(StringComparer.Ordinal);

                foreach (var prerequisite in workflow.Prerequisites ?? [])
                {
                    PlatformOpsWorkflows.ById.Should().ContainKey(prerequisite,
                        workflow.Id + " names a prerequisite workflow that must exist");

                    foreach (var artefact in PlatformOpsWorkflows.ById[prerequisite]
                                 .Nodes.SelectMany(node => node.Produces))
                    {
                        produced.Add(artefact);
                    }
                }

                foreach (var node in workflow.Nodes)
                {
                    foreach (var required in node.Requires)
                    {
                        produced.Should().Contain(required,
                            $"{workflow.Id}/{node.Id} requires \"{required}\", which nothing before it "
                            + "produces -- a graph whose edges do not resolve reads as authoritative and "
                            + "is not");
                    }

                    foreach (var artefact in node.Produces)
                    {
                        produced.Add(artefact);
                    }
                }
            }
        }

        [Fact]
        public void EveryObservationalWorkflowTakesTheWideSampleBeforeTheRecentOne()
        {
            var exempt = new[] { "rolling-deploy-and-migration-order", "backup-and-restore-assurance" };

            foreach (var workflow in PlatformOpsWorkflows.All.Where(candidate => !exempt.Contains(candidate.Id)))
            {
                workflow.Nodes[0].Id.Should().Be("baseline-then-recent",
                    workflow.Id + " reports numbers, and a number without a baseline is not a reading");

                workflow.Nodes[0].Produces.Should().Contain("BaselineWindow", workflow.Id);
                workflow.Nodes[0].Produces.Should().Contain("RecentWindow", workflow.Id);

                workflow.Nodes[1].Requires.Should().Contain("BaselineWindow",
                    workflow.Id + " must not be able to reach its first real reading before the wide "
                                + "sample has been taken");
            }

            PlatformOpsWorkflows.ById["rolling-deploy-and-migration-order"].Nodes
                .Should().Contain(
                    node => node.Produces.Any(artefact => artefact.EndsWith("Before", StringComparison.Ordinal)),
                    "a deploy's baseline is the before-and-after of what it changes");
        }

        [Fact]
        public void TheTriageWorkflowReadsTheStackFromTheBottom()
        {
            var order = PlatformOpsWorkflows.ById["platform-health-triage"]
                .Nodes.Select(node => node.Id).ToList();

            order.IndexOf("consensus-layer").Should().BeLessThan(order.IndexOf("database-leader"));
            order.IndexOf("database-leader").Should().BeLessThan(order.IndexOf("primary-postgres"));
            order.IndexOf("primary-postgres").Should().BeLessThan(order.IndexOf("routing-layer"));
            order.IndexOf("log-corroboration").Should().BeLessThan(order.IndexOf("verdict-and-handoff"));
            order.IndexOf("container-and-host-layer").Should().BeLessThan(order.IndexOf("log-corroboration"));
        }

        [Fact]
        public void TheBackupWorkflowStatesItsOwnBlindnessBeforeAnythingElse()
        {
            var backup = PlatformOpsWorkflows.ById["backup-and-restore-assurance"];

            backup.Nodes[0].Id.Should().Be("declare-the-boundary");
            backup.Nodes[0].Tools.Should().BeEmpty("it reads nothing; it says what cannot be read");

            backup.Nodes.Skip(1).Should().OnlyContain(node => node.Requires.Count > 0,
                "nothing in this workflow may be reported before the boundary has been stated");
        }
    }
}