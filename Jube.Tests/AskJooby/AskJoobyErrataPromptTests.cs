using System;
using System.Collections.Generic;
using FluentAssertions;
using Jube.AskJooby.Prompts;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyErrataPromptTests
    {
        private const string Header =
            "This build has the following errors, omissions and footguns that you must be aware of:";

        [Fact]
        public void NoErrataMeansNoErrataBlockAtAll()
        {
            var prompt = InstructionsBuilder.Build("AGENT-SPECIFIC", 0);

            prompt.Should().NotContain(Header);
        }

        [Fact]
        public void EveryErratumIsListedUnderTheHeaderInTheOrderGiven()
        {
            IReadOnlyList<string> errata = ["Dynamic SQL cannot see CTE names.", "Retention job is paused."];

            var prompt = InstructionsBuilder.Build("AGENT-SPECIFIC", 0, null, errata);

            prompt.Should().Contain(Header + "\n- Dynamic SQL cannot see CTE names.\n- Retention job is paused.\n\n");
        }

        [Fact]
        public void ErrataSitAfterTheSharedPreambleAndBeforeTheAgentsOwnInstructions()
        {
            IReadOnlyList<string> errata = ["Something is wrong."];

            var prompt = InstructionsBuilder.Build("AGENT-SPECIFIC", 0, null, errata);

            var errataAt = prompt.IndexOf(Header, StringComparison.Ordinal);
            errataAt.Should().BeGreaterThan(0);
            errataAt.Should().BeLessThan(prompt.IndexOf("AGENT-SPECIFIC", StringComparison.Ordinal));

            prompt.IndexOf(JoobyPersona.Instructions, StringComparison.Ordinal).Should().Be(0,
                "the persona stays at the very front so the cached prefix is unchanged by errata");
        }
    }
}