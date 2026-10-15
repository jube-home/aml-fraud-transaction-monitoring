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
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Observability;
using Microsoft.Extensions.AI;
using OpenAI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJoobyLive")]
    public sealed class AskJoobyValidationModelCoherenceTests
    {
        private const string Affinity = "jube-tests-validation-model-coherence";

        [Fact]
        public async Task ValidationModelFollowsASimpleDirectInstructionAsync()
        {
            var client = BuildLiveChatClientOrNull();

            if (client is null)
            {
                return;
            }

            using var affinity = ModelAffinityScope.Begin(Affinity);

            var response = await client.GetResponseAsync([
                new ChatMessage(ChatRole.User,
                    "Reply with exactly one word: the word \"acknowledged\". Nothing else, no punctuation.")
            ]);

            var text = response.Text.Trim();
            text.Should().NotBeNullOrWhiteSpace("a coherent model must produce SOME reply to a direct instruction");

            text.Should().ContainEquivalentOf("acknowledged",
                "a model that can't follow a one-word instruction isn't fit to validate anything else");
        }

        [Fact]
        public async Task ValidationModelProducesNonDegenerateProseForAnOpenEndedQuestionAsync()
        {
            var client = BuildLiveChatClientOrNull();

            if (client is null)
            {
                return;
            }

            using var affinity = ModelAffinityScope.Begin(Affinity);

            var response = await client.GetResponseAsync([
                new ChatMessage(ChatRole.User,
                    "In one sentence, what is the purpose of a fraud detection rule engine?")
            ]);

            var text = response.Text.Trim();
            text.Should().NotBeNullOrWhiteSpace();
            text.Length.Should().BeGreaterThan(10, "a real sentence, not an empty or truncated reply");

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var distinctRatio = (double)words.Distinct(StringComparer.OrdinalIgnoreCase).Count() / words.Length;

            distinctRatio.Should().BeGreaterThan(0.4,
                "a coherent sentence doesn't repeat the same handful of words over and over");
        }

        private static IChatClient? BuildLiveChatClientOrNull()
        {
            var apiKey = Environment.GetEnvironmentVariable("AskJoobyApiKey");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            var endpoint = Environment.GetEnvironmentVariable("AskJoobyApiEndpoint")
                           ?? "https://openrouter.ai/api/v1";
            var model = Environment.GetEnvironmentVariable("AskJoobyValidationModel") ?? "deepseek-v4-pro";

            var credential = new ApiKeyCredential(apiKey);

            var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
            options.AddPolicy(new ModelAffinityPolicy(), PipelinePosition.PerCall);
            var openAiClient = new OpenAIClient(credential, options);

            return openAiClient.GetChatClient(model).AsIChatClient();
        }
    }
}