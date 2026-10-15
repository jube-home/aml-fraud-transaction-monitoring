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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Workflows;
using Jube.AskJooby.Workflows.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Test.Security.PenTest;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class RuleTaxonomyWorkflowCoverageTests
    {
        private static readonly Regex taxonomyRow =
            new(@"^\|\s*(?<number>\d+)\s*\|\s*\*\*(?<title>[^*]+)\*\*\s*\|", RegexOptions.Compiled);

        private static string TaxonomyPath =>
            Path.Combine(PenTestPaths.RepositoryRoot, "docs", "RuleTaxonomy", "index.md");

        private static IReadOnlyList<(int Number, string Title)> DocumentedTypes()
        {
            return
            [
                .. File.ReadLines(TaxonomyPath)
                    .Select(line => taxonomyRow.Match(line))
                    .Where(match => match.Success)
                    .Select(match => (int.Parse(match.Groups["number"].Value),
                        match.Groups["title"].Value.Trim()))
            ];
        }

        [Fact]
        public void EveryDocumentedTaxonomyTypeHasAWorkflowAndNothingElseDoes()
        {
            var documented = DocumentedTypes();

            documented.Should().NotBeEmpty("the taxonomy table is the specification and must parse");

            RuleTaxonomyWorkflows.All.Select(workflow => workflow.Number).Should()
                .BeEquivalentTo(documented.Select(type => type.Number),
                    "a type with no workflow is a type an agent has to improvise, which is what these exist "
                    + "to stop: " + string.Join(", ", documented.Select(t => $"{t.Number} {t.Title}")));
        }

        [Fact]
        public void EveryWorkflowKeepsTheTitleTheTaxonomyGaveIt()
        {
            var byNumber = RuleTaxonomyWorkflows.All.ToDictionary(workflow => workflow.Number);

            foreach (var (number, title) in DocumentedTypes())
            {
                var expected = title.Replace("&", "and").Replace("/", "or");

                var words = expected.Split(' ', '(', ')')
                    .Where(word => word.Length > 3).Select(word => word.Trim());

                foreach (var word in words)
                {
                    byNumber[number].Title.Should().Contain(word,
                        $"type {number} is \"{title}\" in the documentation and must be recognisable as it here");
                }
            }
        }

        [Fact]
        public void EveryWorkflowCarriesEnoughSemanticsToBeChosenWithoutTheePasture()
        {
            foreach (var workflow in RuleTaxonomyWorkflows.All)
            {
                workflow.Definition.Should().NotBeNullOrWhiteSpace(workflow.Id);
                workflow.Construction.Should().NotBeNullOrWhiteSpace(workflow.Id);
                workflow.Disambiguation.Should().NotBeNullOrWhiteSpace(workflow.Id);

                workflow.Exemplars.Should().HaveCountGreaterThanOrEqualTo(4,
                    workflow.Id + " is selected by matching a request against its exemplars when no retrieval "
                                + "is available, so a thin set is the same as being unreachable");

                workflow.Exemplars.Should().OnlyContain(exemplar => exemplar.Trim().Length > 0);
            }
        }

        [Fact]
        public void EveryRuleTaxonomyNodeIsPerformableByAnAgent()
        {
            foreach (var workflow in RuleTaxonomyWorkflows.All)
            {
                workflow.Nodes.Should().OnlyContain(node => node.Boundary == WorkflowBoundary.Agent,
                    workflow.Id + ": every rule authoring step is one an agent can carry out");
            }
        }

        [Fact]
        public void EveryWorkflowIdAndNodeIdIsUnique()
        {
            RuleTaxonomyWorkflows.All.Select(workflow => workflow.Id).Should().OnlyHaveUniqueItems();

            foreach (var workflow in RuleTaxonomyWorkflows.All)
            {
                workflow.Nodes.Select(node => node.Id).Should()
                    .OnlyHaveUniqueItems(workflow.Id + " addresses its nodes by id");
            }
        }

        [Fact]
        public void NoNodeRequiresAnArtefactThatNoEarlierNodeProduces()
        {
            foreach (var workflow in RuleTaxonomyWorkflows.All)
            {
                var produced = new HashSet<string>(StringComparer.Ordinal);

                foreach (var node in workflow.Nodes)
                {
                    foreach (var required in node.Requires)
                    {
                        produced.Should().Contain(required,
                            $"{workflow.Id}/{node.Id} requires \"{required}\", which nothing before it produces "
                            + "-- a graph whose edges do not resolve is worse than no graph, because it reads "
                            + "as authoritative");
                    }

                    foreach (var artefact in node.Produces)
                    {
                        produced.Add(artefact);
                    }
                }
            }
        }

        [Fact]
        public void EveryWorkflowLooksForSomethingItCouldReuseBeforeItCreatesAnything()
        {
            foreach (var workflow in RuleTaxonomyWorkflows.All)
            {
                workflow.Nodes[0].Id.Should().Be("reuse-check",
                    workflow.Id + " must look at what the model already has before it builds anything. A "
                                + "duplicate counter or abstraction costs the same work on every event for the life "
                                + "of the model, to produce a number the model already had.");

                var created = workflow.Nodes.First(node =>
                    node.Tools.Any(tool => tool.EndsWith("Create", StringComparison.Ordinal)));

                created.Requires.Should().NotBeEmpty(
                    workflow.Id + "/" + created.Id + " creates a component, so it must depend on the "
                    + "reuse verdict and be unreachable before it");
            }
        }

        [Fact]
        public void EveryWorkflowEndsBySynchronisingAndCheckingTheFragmentCompiled()
        {
            foreach (var workflow in RuleTaxonomyWorkflows.All)
            {
                workflow.Nodes.Should().NotBeEmpty(workflow.Id);

                workflow.Nodes[^1].Produces.Should().Contain("CompiledRule",
                    workflow.Id + " must end at synchronisation: a fragment that fails to compile saves "
                                + "without complaint and then never matches, so a workflow that stops at the write has "
                                + "not finished the job it was asked to do");
            }
        }

        [Fact]
        public void EveryNodeIsOwnedByAnAgentThatExists()
        {
            foreach (var workflow in RuleTaxonomyWorkflows.All)
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

            foreach (var workflow in RuleTaxonomyWorkflows.All)
            {
                foreach (var node in workflow.Nodes)
                {
                    var held = AgentDefinitions.HierarchyToolNames
                        .GetValueOrDefault(node.Agent, []).ToHashSet(StringComparer.Ordinal);

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
                .ToHashSet(StringComparer.Ordinal);

            foreach (var workflow in RuleTaxonomyWorkflows.All)
            {
                foreach (var node in workflow.Nodes)
                {
                    node.Tools.Should().NotBeEmpty($"{workflow.Id}/{node.Id} must say what it calls");

                    var phantom = node.Tools.Where(tool => !catalogued.Contains(tool)).ToList();

                    phantom.Should().BeEmpty(
                        $"{workflow.Id}/{node.Id} names operations the catalogue does not carry, so the step "
                        + "cannot be performed as written: " + string.Join(", ", phantom));
                }
            }
        }

        [Fact]
        public void TheTwoTypesWhoseOrderingIsOppositeBothSaySoOnTheWorkflow()
        {
            RuleTaxonomyWorkflows.ById["velocity-and-volume"].OrderingRule.Should()
                .NotBeNullOrWhiteSpace().And.Contain("Priority");
            RuleTaxonomyWorkflows.ById["event-sequencing"].OrderingRule.Should()
                .NotBeNullOrWhiteSpace().And.Contain("BEFORE");
        }
    }
}