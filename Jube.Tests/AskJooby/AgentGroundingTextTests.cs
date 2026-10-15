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

using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AgentGroundingTextTests
    {
        [Fact]
        public void CollectReturnsTheValidatorsInstructionsAndItsToolsUnderItsDisplayName()
        {
            var grounding = AgentGroundingText.Collect(["validator"]);

            grounding.Should().ContainKey("Jadek");
            grounding["Jadek"].Should().Contain("fact-checker");
            grounding["Jadek"].Should().Contain("Tools available to this agent:");
            grounding["Jadek"].Should().Contain("SearchRag");
        }

        [Fact]
        public void CollectSkipsAgentTypesThatAreNotRegistered()
        {
            var grounding = AgentGroundingText.Collect(["not-a-real-agent-type"]);

            grounding.Should().BeEmpty();
        }

        [Fact]
        public void CollectDeduplicatesRepeatedAgentTypes()
        {
            var grounding = AgentGroundingText.Collect(["validator", "validator", "validator"]);

            grounding.Should().HaveCount(1);
        }
    }
}