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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Reasoning;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ProviderMarkupLeakTests
    {
        private const string EndOfThinking = "<｜end▁of▁thinking｜>";

        private const string SwallowedSpawn = EndOfThinking + "\n\n"
                                                            + "<｜｜DSML｜｜ calls>\n"
                                                            + "<｜｜DSML｜｜ invoke name=\"SpawnAgent\">\n"
                                                            + "<｜｜DSML｜｜ parameter name=\"agentType\" "
                                                            + "string=\"true\">entity-analysis-model-activation-rule-author";

        private static AIFunction Think(out StringCapture captured)
        {
            var capture = new StringCapture();
            captured = capture;

            return new ProviderMarkupAiFunction(AIFunctionFactory.Create(
                (string thought) =>
                {
                    capture.Value = thought;

                    return "Noted.";
                }, "Think"));
        }

        private static Task<object?> InvokeAsync(AIFunction function, string thought)
        {
            return function.InvokeAsync(
                new AIFunctionArguments { ["thought"] = thought }, CancellationToken.None).AsTask();
        }

        [Fact]
        public async Task ASwallowedToolCallIsCutOutOfTheArgumentItLeakedIntoAsync()
        {
            var function = Think(out var captured);

            await InvokeAsync(function, "Let me make a single spawn_agent call now." + SwallowedSpawn);

            captured.Value.Should().Be("Let me make a single spawn_agent call now.",
                "the markup and everything after it was a different message that was never "
                + "addressed to this tool, and the thought the model meant to record is what "
                + "precedes it");
        }

        [Fact]
        public async Task TheModelIsToldTheCallDidNotRunAndWhichOneItWasAsync()
        {
            var function = Think(out _);

            var result = await InvokeAsync(function, "Let me make a single spawn_agent call now." + SwallowedSpawn);

            var text = result.Should().BeOfType<string>().Subject;
            text.Should().StartWith("Noted.", "the tool still did its own job on the cleaned argument");
            text.Should().Contain("SpawnAgent", "the leaked text names the tool the model was reaching for");
            text.Should().Contain("did not run");

            text.Should().Contain("Issue it again",
                "a silent failure the model reads as success is the whole defect; it has to be told "
                + "plainly to re-issue rather than treat the work as done");
        }

        [Fact]
        public async Task DeepSeeksOwnSeparatorFormIsRecognisedTooAsync()
        {
            var function = Think(out _);

            var result = await InvokeAsync(function,
                "Checking the counter." + "<｜tool▁calls▁begin｜>"
                                        + "<｜tool▁call▁begin｜>function<｜tool▁sep｜>SearchRag");

            result.Should().BeOfType<string>().Subject.Should().Contain("SearchRag");
        }

        [Fact]
        public async Task MarkupThatNamesNothingStillWarnsRatherThanPassingSilentlyAsync()
        {
            var function = Think(out var captured);

            var result = await InvokeAsync(function, "Weighing the two reads." + EndOfThinking);

            captured.Value.Should().Be("Weighing the two reads.");

            result.Should().BeOfType<string>().Subject.Should().Contain("A tool call you made",
                "something was swallowed even where the remnant does not say what, and reporting "
                + "that honestly beats guessing at a tool name");
        }

        [Fact]
        public async Task AnOrdinaryArgumentIsUntouchedAndUnannotatedAsync()
        {
            var function = Think(out var captured);

            var result = await InvokeAsync(function,
                "The counter is read before this transaction's own increment, so the 8th "
                + "transaction sees 7.");

            captured.Value.Should().Be(
                "The counter is read before this transaction's own increment, so the 8th "
                + "transaction sees 7.");

            result?.ToString().Should().Be("Noted.",
                "an untouched call is passed straight through, still carrying whatever shape the "
                + "tool underneath returned");
        }

        [Fact]
        public async Task AnAsciiPipeIsNotMistakenForAControlTokenAsync()
        {
            var function = Think(out var captured);
            const string thought = "The builder script reads If (A | B) Then, and the table was | Id | Name |.";

            var result = await InvokeAsync(function, thought);

            captured.Value.Should().Be(thought);
            result?.ToString().Should().Be("Noted.");
        }

        [Fact]
        public void StripAndNameReadTheLeakWithoutExecutingAnythingFromIt()
        {
            ProviderMarkup.Contains("plain text").Should().BeFalse();
            ProviderMarkup.Contains("tail" + EndOfThinking).Should().BeTrue();
            ProviderMarkup.Strip("tail" + SwallowedSpawn).Should().Be("tail");
            ProviderMarkup.IntendedToolName(SwallowedSpawn).Should().Be("SpawnAgent");
            ProviderMarkup.IntendedToolName(EndOfThinking).Should().BeNull();
        }

        private sealed class StringCapture
        {
            public string? Value { get; set; }
        }
    }
}