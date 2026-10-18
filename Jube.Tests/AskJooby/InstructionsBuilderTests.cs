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
using FluentAssertions;
using Jube.AskJooby.Prompts;
using Jube.AskJooby.Prompts.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class InstructionsBuilderTests
    {
        [Fact]
        public void ReachedSpawnDepthLimitAddsAnExplicitNoteNamingTheConfiguredCeiling()
        {
            var instructions = InstructionsBuilder.Build("Agent specific text.", 3, reachedSpawnDepthLimit: 6);

            instructions.Should().Contain("AskJoobyMaxSpawnDepth = 6")
                .And.Contain("SpawnAgent")
                .And.Contain("SpawnAgents");
        }

        [Fact]
        public void NoSpawnDepthLimitMeansNoCeilingNoteAtAll()
        {
            var instructions = InstructionsBuilder.Build("Agent specific text.", 0);

            instructions.Should().NotContain("AskJoobyMaxSpawnDepth");
        }

        [Fact]
        public void TheCeilingNoteSitsBeforeTheGenericRecapTextSoItReadsAsAnExceptionToIt()
        {
            var instructions = InstructionsBuilder.Build("Agent specific text.", 4, reachedSpawnDepthLimit: 6);

            var ceilingNoteIndex = instructions.IndexOf("AskJoobyMaxSpawnDepth", StringComparison.Ordinal);
            var recapIndex = instructions.IndexOf("Before you respond", StringComparison.Ordinal);

            ceilingNoteIndex.Should().BeGreaterThan(-1);
            recapIndex.Should().BeGreaterThan(-1);
            ceilingNoteIndex.Should().BeLessThan(recapIndex);
        }

        [Fact]
        public void PageContextAddsPurposeAndLikelyAsksToEveryAgentsOwnInstructions()
        {
            var pageContext = new PageContext("Case", "edit", "A single case record under investigation.",
                "what the case's current status is.", """{"id":42}""");

            var instructions = InstructionsBuilder.Build("Agent specific text.", 1, pageContext: pageContext);

            instructions.Should().Contain("Case")
                .And.Contain("A single case record under investigation.")
                .And.Contain("what the case's current status is.")
                .And.Contain("42");
        }

        [Fact]
        public void NoPageContextMeansNoPageNoteAtAll()
        {
            var instructions = InstructionsBuilder.Build("Agent specific text.", 0);

            instructions.Should().NotContain("background for you to reason with");
        }
    }
}