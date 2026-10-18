/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Unit")]
    public sealed class AgentHierarchyDocumentTests
    {
        private const int MaximumToolsPerAgent = 15;

        private static IEnumerable<AssignedTool> AssignedTools()
        {
            return AgentDocuments.Agents.SelectMany(a => a.Tools);
        }

        [Fact]
        public void TheDocumentIsPresentAndParsesIntoLeafAgentsThatAllHoldTools()
        {
            File.Exists(AgentDocuments.HierarchyPath).Should().BeTrue(AgentDocuments.HierarchyPath);
            AgentDocuments.Agents.Should().NotBeEmpty();

            AgentDocuments.Agents.Should().OnlyContain(a => a.Tools.Count > 0,
                "a leaf agent that owns no tool is a routing tier masquerading as a leaf");
        }

        [Fact]
        public void EveryCataloguedToolIsOwnedByAnAgent()
        {
            var unowned = AgentDocuments.KnownTools.Select(t => t.Name)
                .Except(AssignedTools().Select(t => t.Name)).OrderBy(n => n).ToList();

            unowned.Should().BeEmpty(
                "every operation in ServiceToolCatalogue, plus the handful of known tool families "
                + "it deliberately never carries, needs an owning agent in docs/AgentHierarchy/index.md, "
                + "and these have none: " + string.Join(", ", unowned));
        }

        [Fact]
        public void NoAgentOwnsAToolThatTheCatalogueDoesNotCarry()
        {
            var phantom = AssignedTools().Select(t => t.Name)
                .Except(AgentDocuments.KnownTools.Select(t => t.Name)).OrderBy(n => n).ToList();

            phantom.Should().BeEmpty(
                "docs/AgentHierarchy/index.md grants tools that no longer exist in ServiceToolCatalogue "
                + "or the other known tool sources: " + string.Join(", ", phantom));
        }

        [Fact]
        public void NoMutatingToolIsOwnedByMoreThanOneAgent()
        {
            var mutating = ServiceToolCatalogue.All
                .Where(t => t.Kind is OperationKind.Write or OperationKind.Delete)
                .Select(t => t.Name).ToHashSet();

            var duplicated = AssignedTools().Where(t => mutating.Contains(t.Name))
                .GroupBy(t => t.Name).Where(g => g.Count() > 1)
                .Select(g => g.Key).OrderBy(n => n).ToList();

            duplicated.Should().BeEmpty(
                "a change with two owning agents makes routing ambiguous and splits the preservation "
                + "snapshot: " + string.Join(", ", duplicated));
        }

        [Fact]
        public void OnlyReadsAreSharedBetweenAgents()
        {
            var shared = AssignedTools().GroupBy(t => t.Name).Where(g => g.Count() > 1)
                .Select(g => g.Key).Except(AgentDocuments.sharedAcrossAgentsByDesign).ToList();

            var wrong = shared.Where(name => AgentDocuments.KnownTools.Single(t => t.Name == name).Kind
                                             != OperationKind.Read).OrderBy(n => n).ToList();

            wrong.Should().BeEmpty(
                "a read is shared because reading an object is how an agent learns what it is about to "
                + "change; nothing else may be, outside the operations named transient and shared by "
                + "design: " + string.Join(", ", wrong));
        }

        [Fact]
        public void NoAgentHoldsMoreThanFifteenTools()
        {
            var oversized = AgentDocuments.Agents.Where(a => a.Tools.Count > MaximumToolsPerAgent)
                .Select(a => $"{a.Slug} ({a.Tools.Count})").OrderBy(n => n).ToList();

            oversized.Should().BeEmpty(
                $"an agent over {MaximumToolsPerAgent} tools must be split: " + string.Join(", ", oversized));
        }

        [Fact]
        public void EveryAgentSlugIsUniqueBecauseItIsTheAddress()
        {
            var duplicated = AgentDocuments.Agents.GroupBy(a => a.Slug).Where(g => g.Count() > 1)
                .Select(g => g.Key).OrderBy(n => n).ToList();

            duplicated.Should().BeEmpty("two agents cannot share one address: " + string.Join(", ", duplicated));
        }

        [Fact]
        public void EveryMutatingToolIsFlaggedForConfirmationAndNoReadIs()
        {
            var mutating = ServiceToolCatalogue.All
                .Where(t => t.Kind is OperationKind.Write or OperationKind.Delete)
                .Select(t => t.Name).ToHashSet();

            var flagged = AssignedTools().Where(t => t.Flagged).Select(t => t.Name).ToHashSet();

            var unflagged = mutating.Except(flagged).OrderBy(n => n).ToList();
            var overflagged = flagged.Except(mutating).OrderBy(n => n).ToList();

            unflagged.Should().BeEmpty(
                "a Write or Delete operation must carry the confirmation flag: " + string.Join(", ", unflagged));
            overflagged.Should().BeEmpty(
                "a Read operation must not demand confirmation: " + string.Join(", ", overflagged));
        }

        [Fact]
        public void EveryToolCitesThePermissionSourceItWasReadFrom()
        {
            var uncited = AgentDocuments.ToolBulletsWithFullText(File.ReadAllLines(AgentDocuments.HierarchyPath))
                .Where(e => !e.FullText.Contains("permissions from `"))
                .Select(e => e.Tool)
                .OrderBy(n => n).ToList();

            uncited.Should().BeEmpty(
                "a permission that is retyped rather than sourced from the owning service will drift: "
                + string.Join(", ", uncited));
        }
    }
}