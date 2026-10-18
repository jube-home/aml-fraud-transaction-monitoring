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
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.ServiceToolCatalogue;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsTrainingWorkflowCoverageTests
    {
        [Fact]
        public void EveryWorkflowCarriesEnoughSemanticsToBeChosenWithoutThePasture()
        {
            foreach (var workflow in AdvancedAnalyticsTrainingWorkflows.All)
            {
                workflow.Definition.Should().NotBeNullOrWhiteSpace(workflow.Id);
                workflow.Construction.Should().NotBeNullOrWhiteSpace(workflow.Id);
                workflow.Disambiguation.Should().NotBeNullOrWhiteSpace(workflow.Id);

                workflow.Exemplars.Should().HaveCountGreaterThanOrEqualTo(4,
                    workflow.Id + " is matched against its exemplars when no retrieval is available, so a "
                                + "thin set is the same as being unreachable");
            }
        }

        [Fact]
        public void EveryWorkflowIdAndNodeIdIsUnique()
        {
            AdvancedAnalyticsTrainingWorkflows.All.Select(workflow => workflow.Id).Should()
                .OnlyHaveUniqueItems();

            foreach (var workflow in AdvancedAnalyticsTrainingWorkflows.All)
            {
                workflow.Nodes.Select(node => node.Id).Should()
                    .OnlyHaveUniqueItems(workflow.Id + " addresses its nodes by id");
            }
        }

        [Fact]
        public void NoNodeRequiresAnArtefactThatNoEarlierNodeProduces()
        {
            foreach (var workflow in AdvancedAnalyticsTrainingWorkflows.All)
            {
                var produced = new HashSet<string>(StringComparer.Ordinal);

                foreach (var node in workflow.Nodes)
                {
                    foreach (var required in node.Requires)
                    {
                        produced.Should().Contain(required,
                            $"{workflow.Id}/{node.Id} requires \"{required}\", which nothing before it "
                            + "produces -- a graph whose edges do not resolve is worse than no graph, because "
                            + "it reads as authoritative");
                    }

                    foreach (var artefact in node.Produces)
                    {
                        produced.Add(artefact);
                    }
                }
            }
        }

        [Fact]
        public void EveryNodeIsOwnedByAnAgentThatExists()
        {
            foreach (var workflow in AdvancedAnalyticsTrainingWorkflows.All)
            {
                foreach (var node in workflow.Nodes)
                {
                    AgentDefinitions.All.Should().ContainKey(node.Agent,
                        $"{workflow.Id}/{node.Id} names an agent that must be registered to be delegated to");
                }
            }
        }

        [Fact]
        public void EveryNodeIsOwnedByAnAgentThatActuallyHoldsTheToolsItNames()
        {
            var broken = new List<string>();

            foreach (var workflow in AdvancedAnalyticsTrainingWorkflows.All)
            {
                foreach (var node in workflow.Nodes)
                {
                    var held = AgentDefinitions.HierarchyToolNames.GetValueOrDefault(node.Agent, [])
                        .Concat(AdvancedAnalyticsTrainingAgentToolNames.All.GetValueOrDefault(node.Agent, []))
                        .Concat(AdvancedAnalyticsModelEvaluationAgentToolNames.All.GetValueOrDefault(node.Agent, []))
                        .ToHashSet(StringComparer.Ordinal);

                    foreach (var tool in node.Tools.Where(tool => !held.Contains(tool)))
                    {
                        broken.Add($"{workflow.Id}/{node.Id} -> {node.Agent} lacks {tool}");
                    }
                }
            }

            broken.Should().BeEmpty("a step delegated to an agent that cannot perform it is refused on "
                                    + "arrival: " + string.Join("; ", broken));
        }

        [Fact]
        public void EveryNodeNamesCataloguedToolsThatActuallyExist()
        {
            var catalogued = ServiceToolCatalogue.All.Select(tool => tool.Name)
                .Concat(AdvancedAnalyticsAgentToolBinder.Descriptors.Select(descriptor => descriptor.ToolName))
                .ToHashSet(StringComparer.Ordinal);

            foreach (var workflow in AdvancedAnalyticsTrainingWorkflows.All)
            {
                foreach (var node in workflow.Nodes)
                {
                    node.Tools.Should().NotBeEmpty($"{workflow.Id}/{node.Id} must say what it calls");

                    var phantom = node.Tools.Where(tool => !catalogued.Contains(tool)).ToList();

                    phantom.Should().BeEmpty(
                        $"{workflow.Id}/{node.Id} names operations the catalogue does not carry, so the "
                        + "step cannot be performed as written: " + string.Join(", ", phantom));
                }
            }
        }

        [Fact]
        public void EveryWorkflowDeclaresTheOrderingRuleThatMakesItAGraphRatherThanAListOfSteps()
        {
            foreach (var workflow in AdvancedAnalyticsTrainingWorkflows.All)
            {
                workflow.OrderingRule.Should().NotBeNullOrWhiteSpace(workflow.Id);
            }
        }
    }
}