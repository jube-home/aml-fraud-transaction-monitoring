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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Unit")]
    public sealed class HierarchyAgentDefinitionsTests
    {
        [Fact]
        public void TheHierarchyIsRegisteredAlongsideTheOriginalThreeAgents()
        {
            AgentDefinitions.All.Should().ContainKeys("general", "researcher", "validator", "web-researcher");
            AgentDefinitions.All.Count.Should().Be(233);
            AgentDefinitions.HierarchyToolNames.Should().HaveCount(174);
        }

        [Fact]
        public void EveryCataloguedToolIsClaimedByAtLeastOneLeafAgent()
        {
            AgentDefinitions.HierarchyToolNames.Should().OnlyContain(p => p.Value.Distinct().Count() == p.Value.Count,
                "a leaf may not name the same operation twice in its own roster");

            AgentDefinitions.HierarchyToolNames.SelectMany(p => p.Value).Distinct()
                .Should().BeEquivalentTo(ServiceToolCatalogue.All.Select(t => t.Name));
        }

        [Fact]
        public void EveryMutatingToolIsClaimedByExactlyOneLeafAgent()
        {
            var shared = AgentDefinitions.HierarchyToolNames
                .SelectMany(p => p.Value.Select(name => (Agent: p.Key, Tool: name)))
                .Where(p => ServiceToolCatalogue.All.Single(t => t.Name == p.Tool).Kind
                    is OperationKind.Write or OperationKind.Delete)
                .GroupBy(p => p.Tool).Where(g => g.Count() > 1)
                .Select(g => g.Key + " (" + string.Join(", ", g.Select(p => p.Agent)) + ")")
                .OrderBy(s => s).ToList();

            shared.Should().BeEmpty(
                "two agents able to change the same thing is two agents able to undo each other, and the "
                + "preservation snapshot is taken per agent: " + string.Join("; ", shared));
        }

        [Fact]
        public void OnlyIdempotentNonDestructiveReadsAreSharedBetweenLeafAgents()
        {
            var shared = AgentDefinitions.HierarchyToolNames.SelectMany(p => p.Value)
                .GroupBy(name => name).Where(g => g.Count() > 1).Select(g => g.Key)
                .Select(name => ServiceToolCatalogue.All.Single(t => t.Name == name)).ToList();

            shared.Should().OnlyContain(
                t => t.Kind == OperationKind.Read && t.Idempotent && !t.Destructive,
                "sharing is only safe where calling the operation twice is the same as calling it once");
        }

        [Fact]
        public void NoLeafAgentClaimsMoreThanFifteenTools()
        {
            AgentDefinitions.HierarchyToolNames.Should().OnlyContain(p => p.Value.Count <= 15);
        }

        [Fact]
        public void EveryLeafThatWritesAnObjectCanAlsoReadAndValidateIt()
        {
            var catalogued = ServiceToolCatalogue.All.Select(t => t.Name).ToHashSet();
            var missing = new List<string>();

            foreach (var (slug, toolNames) in AgentDefinitions.HierarchyToolNames)
            {
                foreach (var verb in new[] { "Create", "Update" })
                {
                    foreach (var write in toolNames.Where(n => n.EndsWith(verb, StringComparison.Ordinal))
                                 .Where(n => ServiceToolCatalogue.All.Single(t => t.Name == n).Kind
                                             == OperationKind.Write))
                    {
                        var stem = write[..^verb.Length];

                        foreach (var companion in new[]
                                 {
                                     "Validate", "Get", "GetByGuid", "Filter", "FilterFields", "Count", "List"
                                 })
                        {
                            var name = stem + companion;

                            if (catalogued.Contains(name) && !toolNames.Contains(name))
                            {
                                missing.Add(slug + " writes " + write + " but does not hold " + name);
                            }
                        }
                    }
                }
            }

            missing.Should().BeEmpty(
                "an agent that can change an object and cannot read or validate it has only free-form SQL "
                + "left to find out what it is changing, which is where the guessed column names come "
                + "from: " + string.Join("; ", missing));
        }

        [Fact]
        public void EveryDelegateNamedByAnAgentExists()
        {
            foreach (var (agentType, definition) in AgentDefinitions.All)
            {
                foreach (var delegateName in definition.AllowedDelegates ?? [])
                {
                    AgentDefinitions.All.Should().ContainKey(delegateName,
                        $"{agentType} names it as a delegate");
                }
            }
        }

        [Fact]
        public void TheRootDelegatesOnlyToTheNineDomainsSoItsPromptDoesNotListEveryAgent()
        {
            var general = AgentDefinitions.All["general"];

            general.AllowedDelegates.Should().NotBeNull();
            general.AllowedDelegates!.Should().HaveCount(9);
            general.AllowedDelegates.Should().OnlyContain(d => !d.Contains('.'));
        }

        [Fact]
        public void EveryLeafIsReachableFromTheRootWithinTheDelegationDepthBudget()
        {
            var depth = new Dictionary<string, int> { ["general"] = 0 };
            var queue = new Queue<string>(["general"]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var child in AgentDefinitions.All[current].AllowedDelegates ?? [])
                {
                    if (depth.TryAdd(child, depth[current] + 1))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            var unreachable = AgentDefinitions.HierarchyToolNames.Keys
                .Where(slug => !depth.ContainsKey(slug)).OrderBy(s => s).ToList();

            unreachable.Should().BeEmpty("a leaf the root cannot reach can never be delegated to: "
                                         + string.Join(", ", unreachable));

            depth.Values.Max().Should().BeLessThanOrEqualTo(3,
                "RunContext.MaxDepth is 3, so a deeper leaf could never be spawned");
        }

        [Fact]
        public void NoParentExceedsTheFanOutBudget()
        {
            foreach (var (agentType, definition) in AgentDefinitions.All)
            {
                (definition.AllowedDelegates?.Count ?? 0).Should().BeLessThanOrEqualTo(9,
                    $"{agentType} would offer more candidates than a caller can weigh");
            }
        }

        [Fact]
        public void AnAgentHoldingAMutatingToolRequiresAPreservationSnapshot()
        {
            foreach (var (slug, toolNames) in AgentDefinitions.HierarchyToolNames)
            {
                var mutates = toolNames.Any(name => ServiceToolCatalogue.All
                    .Single(t => t.Name == name).Kind is OperationKind.Write or OperationKind.Delete);

                AgentDefinitions.All[slug].RequiresPreservationSnapshot.Should().Be(mutates, slug);
            }
        }

        [Fact]
        public void TheYokeCountReportsTheWholeBoundToolSurfaceNotJustTheGeneralistsOwnTools()
        {
            var withoutBinding = AgentDefinitions.CountAvailableTools(_ => true, true, true);

            var bound = AgentDefinitions.HierarchyToolNames.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<AgentToolDefinition>)
                [
                    .. pair.Value
                        .Select(name => new AgentToolDefinition(
                            AIFunctionFactory.Create(() => string.Empty, name), []))
                ]);

            var withBinding = AgentDefinitions.CountAvailableTools(_ => true, true, true, bound);

            withoutBinding.Should().BeLessThan(50,
                "with no bound tools only the generalist's own toolkit is visible");

            withBinding.Should().BeGreaterThan(640,
                "the greeting should report every tool the caller can actually reach, the same way "
                + "the Joobrik count reports every agent");
        }

        [Fact]
        public void EveryLeafDeclaresThePermissionsItsOwnToolsRequire()
        {
            foreach (var (slug, toolNames) in AgentDefinitions.HierarchyToolNames)
            {
                var expected = toolNames
                    .SelectMany(name => ServiceToolCatalogue.All.Single(t => t.Name == name).RequiredPermissions)
                    .Distinct().OrderBy(p => p).ToList();

                AgentDefinitions.All[slug].RequiredPermissions.Should().BeEquivalentTo(expected, slug);
            }
        }
    }
}