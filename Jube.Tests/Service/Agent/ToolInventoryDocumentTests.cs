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

using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Unit")]
    public sealed class ToolInventoryDocumentTests
    {
        [Fact]
        public void TheInventoryIsPresentAndEveryEntryHasBothAnIndexRowAndASection()
        {
            File.Exists(AgentDocuments.InventoryPath).Should().BeTrue(AgentDocuments.InventoryPath);

            var incomplete = AgentDocuments.Inventory.Where(e => e.Row is null)
                .Select(e => e.Name).OrderBy(n => n).ToList();

            incomplete.Should().BeEmpty(
                "an operation listed in one half of the inventory but not the other will be found by only one "
                + "kind of lookup: " + string.Join(", ", incomplete));
        }

        [Fact]
        public void TheInventoryCoversExactlyTheCatalogue()
        {
            var catalogued = AgentDocuments.KnownTools.Select(t => t.Name).ToHashSet();
            var inventoried = AgentDocuments.Inventory.Select(e => e.Name).ToHashSet();

            var missing = catalogued.Except(inventoried).OrderBy(n => n).ToList();
            var phantom = inventoried.Except(catalogued).OrderBy(n => n).ToList();

            missing.Should().BeEmpty(
                "the inventory is the exhaustive backstop, so an uninventoried operation is unreachable when "
                + "routing by meaning fails: " + string.Join(", ", missing));

            phantom.Should().BeEmpty(
                "the inventory lists operations the catalogue no longer carries: " + string.Join(", ", phantom));
        }

        [Fact]
        public void NoOperationIsInventoriedTwice()
        {
            AgentDocuments.Inventory.Select(e => e.Name).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void EveryInventoryRowStatesTheKindTheCatalogueDeclares()
        {
            foreach (var tool in AgentDocuments.KnownTools)
            {
                var entry = AgentDocuments.Inventory.Single(e => e.Name == tool.Name);
                entry.Row!.Kind.Should().Be(tool.Kind.ToString(), tool.Name);
            }
        }

        [Fact]
        public void EveryInventoryRowStatesTheAccessTheCatalogueEnforces()
        {
            foreach (var tool in AgentDocuments.KnownTools)
            {
                var access = AgentDocuments.Inventory.Single(e => e.Name == tool.Name).Row!.Access;

                if (tool.RequiresLandlord)
                {
                    access.Should().Be("landlord", tool.Name);
                }
                else if (tool.RequiredPermissions.Count > 0)
                {
                    access.Should().Be(string.Join(", ", tool.RequiredPermissions), tool.Name);
                }
                else
                {
                    access.Should().Be("**none**", tool.Name);
                }
            }
        }

        [Fact]
        public void EveryInventoryRowNamesASlugTheHierarchyDefines()
        {
            var slugs = AgentDocuments.Agents.Select(a => a.Slug).ToHashSet();

            var unknown = AgentDocuments.Inventory
                .Where(e => e.Row is not null && e.Row.Slug != "—" && !slugs.Contains(e.Row.Slug))
                .Select(e => e.Name + " -> " + e.Row!.Slug)
                .OrderBy(n => n)
                .ToList();

            unknown.Should().BeEmpty(
                "an inventory row naming a slug the hierarchy does not define points at an agent that cannot "
                + "be routed to, and a heading mis-shape that silently drops an agent from the parse "
                + "surfaces here: " + string.Join(", ", unknown));
        }

        [Fact]
        public void EveryInventoryRowNamesAnAgentTheHierarchyGivesTheOperationTo()
        {
            var holders = AgentDocuments.Agents
                .SelectMany(a => a.Tools.Select(t => (t.Name, a.Slug)))
                .GroupBy(p => p.Name)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Slug).ToList());

            foreach (var (tool, slugs) in holders)
            {
                var entry = AgentDocuments.Inventory.Single(e => e.Name == tool);

                slugs.Should().Contain(entry.Row!.Slug,
                    tool + " names an owner that does not hold it; a read held by several agents is "
                         + "listed against the one whose slice it belongs to");
            }
        }
    }
}