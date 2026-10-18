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
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Routing.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class RoutingRulesCoverageTests
    {
        private static IReadOnlyCollection<string> Domains()
        {
            return [.. AgentDefinitions.HierarchyDomainOf.Values.Distinct()];
        }

        [Fact]
        public void EveryDomainIsReachableFromRouting()
        {
            var routed = RoutingRules.All.Select(rule => rule.AgentType).ToHashSet();

            Domains().Should().OnlyContain(domain => routed.Contains(domain),
                "a domain with no routing rule cannot be routed to at all -- AgentRouter only ever "
                + "considers candidates drawn from RoutingRules.All, so the whole hierarchy under that "
                + "domain is reachable only if a generalist happens to spawn into it");
        }

        [Fact]
        public void EveryRoutingRuleNamesAnAgentThatActuallyExists()
        {
            var known = AgentDefinitions.All.Keys.ToHashSet();

            RoutingRules.All.Select(rule => rule.AgentType).Should().OnlyContain(agentType => known.Contains(agentType),
                "a rule naming an agent that is not registered is silently dropped when the router "
                + "filters candidates against the available agents, so it routes nothing");
        }

        [Fact]
        public void EveryRoutingRuleCarriesExemplarsAndADescriptionForTheStagesThatUseThem()
        {
            foreach (var rule in RoutingRules.All)
            {
                rule.Exemplars.Should().NotBeEmpty(
                    $"'{rule.AgentType}' is invisible to the embedding stage without exemplars");
                rule.Description.Should().NotBeNullOrWhiteSpace(
                    $"'{rule.AgentType}' is invisible to the model stage without a description");
            }
        }

        [Fact]
        public void RoutingIsOfferedPerDomainRatherThanPerLeaf()
        {
            var leaves = AgentDefinitions.HierarchyDomainOf.Keys.ToHashSet();

            RoutingRules.All.Should().NotContain(rule => leaves.Contains(rule.AgentType),
                "routing a single sentence to one of the leaves misfires often and loses the sibling "
                + "work its domain would have pulled in, so a domain is routed to and fans out itself");
        }
    }
}