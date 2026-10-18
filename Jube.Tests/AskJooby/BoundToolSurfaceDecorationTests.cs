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
using System.Linq;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.RuleExtensionTools;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class BoundToolSurfaceDecorationTests
    {
        [Fact]
        public void EveryAgentHandedTheRuleExtensionOperationsIsToldWhatTheyAreFor()
        {
            foreach (var agentType in RuleExtensionAgentToolNames.Agents)
            {
                AgentDefinitions.All.Should().ContainKey(agentType);

                AgentDefinitions.All[agentType].Instructions.Should().Contain("RuleExtensionMethodSearch",
                    agentType + " is handed the extension method surface and would have to guess it exists");
                AgentDefinitions.All[agentType].Instructions.Should().Contain("RuleExtensionMethodValidate",
                    agentType + " can resolve a chain link by link and is not told so");
            }
        }

        [Fact]
        public void NoAgentIsToldAboutTheRuleExtensionOperationsWithoutHoldingThem()
        {
            var told = AgentDefinitions.All
                .Where(pair => pair.Value.Instructions.Contains("RuleExtensionMethodSearch", StringComparison.Ordinal))
                .Select(pair => pair.Key).OrderBy(slug => slug, StringComparer.Ordinal).ToList();

            told.Should().BeEquivalentTo(RuleExtensionAgentToolNames.Agents);
        }

        [Fact]
        public void EveryAgentHoldingARuleExtensionValidationIsToldToValidate()
        {
            foreach (var agentType in RuleExtensionAgentToolNames.Agents)
            {
                AgentDefinitions.All[agentType].Instructions.Should().Contain("Validations are gold",
                    agentType + " holds a validation operation on a surface the validation pass cannot see");
            }
        }

        [Fact]
        public void EveryComputationAnalystIsToldItsOperationsTakeTheNumbersThemselves()
        {
            foreach (var agentType in AdvancedAnalyticsComputationAgentToolNames.All.Keys)
            {
                AgentDefinitions.All.Should().ContainKey(agentType);

                AgentDefinitions.All[agentType].Instructions.Should().Contain("pure functions over numbers",
                    agentType + " holds array-taking functions and nothing tells it where the array comes from");
            }
        }

        [Fact]
        public void EveryTrainingAgentIsToldItCanCancelARunItOpened()
        {
            foreach (var agentType in AdvancedAnalyticsTrainingAgentToolNames.All.Keys)
            {
                AgentDefinitions.All.Should().ContainKey(agentType);

                AgentDefinitions.All[agentType].Instructions.Should()
                    .Contain("CancelAdvancedAnalyticsTrainingRun", agentType + " holds it and is never told");
            }
        }

        [Fact]
        public void TheDelegationSummaryOfAnAgentWithAnAddedSurfaceSaysSoRatherThanUnderstatingIt()
        {
            foreach (var agentType in RuleExtensionAgentToolNames.Agents)
            {
                AgentDefinitions.All[agentType].DelegationSummary.Should()
                    .Contain("rule extension method operations",
                        agentType + "'s parent is told a smaller surface than the agent actually holds");
            }
        }
    }
}