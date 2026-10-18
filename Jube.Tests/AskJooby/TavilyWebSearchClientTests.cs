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

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Models;
using Jube.Service.AskJoobyWebSearch;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class TavilyWebSearchClientTests
    {
        private const string Endpoint = "https://api.tavily.test/search";

        private static TavilyWebSearchClient Client(HttpStatusCode status, string body, out CapturingHandler handler)
        {
            handler = new CapturingHandler(status, body);

            return new TavilyWebSearchClient(new HttpClient(handler), Endpoint, "test-key");
        }

        [Fact]
        public async Task SendsTheApiKeyAsABearerTokenAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":[]}""", out var handler);

            await client.SearchAsync(new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced,
                WebSearchAnswerMode.Advanced));

            handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("test-key");
        }

        [Theory]
        [InlineData(WebSearchDepth.Basic, "basic")]
        [InlineData(WebSearchDepth.Advanced, "advanced")]
        public async Task MapsSearchDepthOntoTheProviderOwnStringAsync(WebSearchDepth depth, string expected)
        {
            var client = Client(HttpStatusCode.OK, """{"results":[]}""", out var handler);

            await client.SearchAsync(new WebSearchRequest("jube.io", 5, depth, WebSearchAnswerMode.None));

            using var body = JsonDocument.Parse(handler.LastRequestBody!);

            body.RootElement.GetProperty("search_depth").GetString().Should().Be(expected);
        }

        [Theory]
        [InlineData(WebSearchAnswerMode.None)]
        [InlineData(WebSearchAnswerMode.Basic)]
        [InlineData(WebSearchAnswerMode.Advanced)]
        public async Task MapsAnswerModeOntoTheProviderOwnIncludeAnswerValueAsync(WebSearchAnswerMode answerMode)
        {
            var client = Client(HttpStatusCode.OK, """{"results":[]}""", out var handler);

            await client.SearchAsync(new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced, answerMode));

            using var body = JsonDocument.Parse(handler.LastRequestBody!);
            var includeAnswer = body.RootElement.GetProperty("include_answer");

            switch (answerMode)
            {
                case WebSearchAnswerMode.None:
                    includeAnswer.GetBoolean().Should().BeFalse();
                    break;
                case WebSearchAnswerMode.Basic:
                    includeAnswer.GetString().Should().Be("basic");
                    break;
                case WebSearchAnswerMode.Advanced:
                    includeAnswer.GetString().Should().Be("advanced");
                    break;
            }
        }

        [Theory]
        [InlineData(WebSearchRawContentMode.None)]
        [InlineData(WebSearchRawContentMode.Text)]
        [InlineData(WebSearchRawContentMode.Markdown)]
        public async Task MapsRawContentModeOntoTheProviderOwnIncludeRawContentValueAsync(
            WebSearchRawContentMode rawContentMode)
        {
            var client = Client(HttpStatusCode.OK, """{"results":[]}""", out var handler);

            await client.SearchAsync(new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced,
                WebSearchAnswerMode.Advanced, rawContentMode));

            using var body = JsonDocument.Parse(handler.LastRequestBody!);
            var includeRawContent = body.RootElement.GetProperty("include_raw_content");

            switch (rawContentMode)
            {
                case WebSearchRawContentMode.None:
                    includeRawContent.GetBoolean().Should().BeFalse();
                    break;
                case WebSearchRawContentMode.Text:
                    includeRawContent.GetString().Should().Be("text");
                    break;
                case WebSearchRawContentMode.Markdown:
                    includeRawContent.GetString().Should().Be("markdown");
                    break;
            }
        }

        [Fact]
        public async Task SendsTheQueryAndMaxResultsAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":[]}""", out var handler);

            await client.SearchAsync(
                new WebSearchRequest("jube.io", 7, WebSearchDepth.Advanced, WebSearchAnswerMode.Advanced));

            using var body = JsonDocument.Parse(handler.LastRequestBody!);

            body.RootElement.GetProperty("query").GetString().Should().Be("jube.io");
            body.RootElement.GetProperty("max_results").GetInt32().Should().Be(7);
        }

        [Fact]
        public async Task ParsesTheAnswerAndResultsAsync()
        {
            const string responseBody = """
                                        {
                                          "answer": "jube.io is an AML platform.",
                                          "results": [
                                            {
                                              "title": "Jube",
                                              "url": "https://jube.io",
                                              "content": "Jube is a fraud and AML platform.",
                                              "score": 0.87,
                                              "published_date": "2024-01-01",
                                              "raw_content": "# Jube\n\nJube is a fraud and AML platform, in full."
                                            }
                                          ]
                                        }
                                        """;

            var client = Client(HttpStatusCode.OK, responseBody, out _);

            var response = await client.SearchAsync(
                new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced, WebSearchAnswerMode.Advanced,
                    WebSearchRawContentMode.Markdown));

            response.Answer.Should().Be("jube.io is an AML platform.");
            response.Results.Should().ContainSingle();
            response.Results[0].Title.Should().Be("Jube");
            response.Results[0].Url.Should().Be("https://jube.io");
            response.Results[0].Content.Should().Be("Jube is a fraud and AML platform.");
            response.Results[0].Score.Should().Be(0.87);
            response.Results[0].PublishedDate.Should().Be("2024-01-01");
            response.Results[0].RawContent.Should().Be("# Jube\n\nJube is a fraud and AML platform, in full.");
        }

        [Fact]
        public async Task ARegularResultWithNoRawContentFieldLeavesItNullAsync()
        {
            const string responseBody =
                """{"results":[{"title":"A","url":"https://a.test","content":"c"}]}""";

            var client = Client(HttpStatusCode.OK, responseBody, out _);

            var response = await client.SearchAsync(
                new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced, WebSearchAnswerMode.None));

            response.Results[0].RawContent.Should().BeNull();
        }

        [Fact]
        public async Task CarriesTheRawResponseVerbatimAsync()
        {
            const string responseBody =
                """{"answer":null,"results":[{"title":"A","url":"https://a.test","content":"c"}]}""";

            var client = Client(HttpStatusCode.OK, responseBody, out _);

            var response = await client.SearchAsync(
                new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced, WebSearchAnswerMode.Advanced));

            response.RawJson.Should().Be(responseBody);
        }

        [Fact]
        public async Task MissingAnswerAndEmptyResultsParseCleanlyAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":[]}""", out _);

            var response = await client.SearchAsync(
                new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced, WebSearchAnswerMode.None));

            response.Answer.Should().BeNull();
            response.Results.Should().BeEmpty();
        }

        [Fact]
        public async Task ThrowsWithTheStatusCodeAndBodyWhenTheProviderAnswersAnErrorAsync()
        {
            var client = Client(HttpStatusCode.Unauthorized, """{"detail":"invalid api key"}""", out _);

            var act = () => client.SearchAsync(
                new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced, WebSearchAnswerMode.Advanced));

            (await act.Should().ThrowAsync<HttpRequestException>())
                .Which.Message.Should().Contain("401").And.Contain("invalid api key");
        }

        [Fact]
        public async Task RecordsTheCallAgainstTheGivenRunWhenARecorderIsSuppliedAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":[]}""", out _);
            var recorder = new RecordingRunRecorder();

            await client.SearchAsync(
                new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced, WebSearchAnswerMode.Advanced),
                recorder, "42");

            recorder.RunId.Should().Be("42");
            recorder.Query.Should().Be("jube.io");
            recorder.ResponseJson.Should().Be("""{"results":[]}""");
        }

        [Fact]
        public Task RecordsNothingWhenNoRecorderIsSuppliedAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":[]}""", out _);

            var act = () => client.SearchAsync(
                new WebSearchRequest("jube.io", 5, WebSearchDepth.Advanced, WebSearchAnswerMode.Advanced));

            return act.Should().NotThrowAsync();
        }

        private sealed class RecordingRunRecorder : IRunRecorder
        {
            public string? RunId { get; private set; }
            public string? Query { get; private set; }
            public string? ResponseJson { get; private set; }

            public Task<string> StartRunAsync(string agentType, int depth, string? parentRunId, string task,
                CancellationToken token = default)
            {
                return Task.FromResult(string.Empty);
            }

            public Task CompleteRunAsync(string runId, string status, string? result, long durationMs,
                CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public Task RecordToolCallAsync(string runId, string toolName, string? argumentsJson, string? result,
                string status, long durationMs, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public Task RecordModelCallAsync(string runId, string model, int? inputTokens, int? outputTokens,
                long? cachedInputTokens, string? promptPrefixHash, string? usageCountsJson,
                long? promptDetailCachedTokens, string? finishReason, long durationMs, long? firstChunkMs,
                CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public Task RecordWebSearchCallAsync(string runId, string query, string? responseJson, long durationMs,
                CancellationToken token = default)
            {
                RunId = runId;
                Query = query;
                ResponseJson = responseJson;

                return Task.CompletedTask;
            }
        }

        private sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }
            public string? LastRequestBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                LastRequestBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}