using System.Collections.Generic;
using FluentAssertions;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Basic.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AgentFactorySpecialistDescriptionTests
    {
        private static AgentDefinition DefinitionWith(
            string? delegationSummary, IReadOnlyList<string>? allowedDelegates)
        {
            return new AgentDefinition(
                "Fallback instructions text.", [], _ => [],
                AllowedDelegates: allowedDelegates,
                DelegationSummary: delegationSummary);
        }

        [Fact]
        public void AStableWithItsOwnSpecialistsHasThemAppendedByName()
        {
            var definition = DefinitionWith(
                "Owns model training and analytics.",
                ["exhaustive-search", "advanced-analytics-r-script"]);

            var description = AgentFactory.DescribeSpecialist(definition);

            description.Should().Be(
                "Owns model training and analytics. Its own specialists: exhaustive-search, advanced-analytics-r-script.");
        }

        [Fact]
        public void AnUnrestrictedAgentWithNullAllowedDelegatesGetsNothingAppended()
        {
            var definition = DefinitionWith("Owns model training and analytics.", null);

            var description = AgentFactory.DescribeSpecialist(definition);

            description.Should().Be("Owns model training and analytics.");
        }

        [Fact]
        public void ALeafWithNoDelegatesGetsNothingAppended()
        {
            var definition = DefinitionWith("Owns the R script run lifecycle.", []);

            var description = AgentFactory.DescribeSpecialist(definition);

            description.Should().Be("Owns the R script run lifecycle.");
        }

        [Fact]
        public void FallsBackToInstructionsWhenThereIsNoDelegationSummary()
        {
            var definition = DefinitionWith(null, ["advanced-analytics-r-script"]);

            var description = AgentFactory.DescribeSpecialist(definition);

            description.Should().Be(
                "Fallback instructions text. Its own specialists: advanced-analytics-r-script.");
        }
    }
}