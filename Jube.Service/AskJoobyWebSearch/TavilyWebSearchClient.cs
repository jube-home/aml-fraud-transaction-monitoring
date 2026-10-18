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

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Models;

namespace Jube.Service.AskJoobyWebSearch
{
    public sealed class TavilyWebSearchClient(HttpClient httpClient, string endpoint, string apiKey)
        : IWebSearchClient
    {
        public Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken token = default)
        {
            return SearchAsync(request, null, null, token);
        }

        public async Task<WebSearchResponse> SearchAsync(WebSearchRequest request, IRunRecorder? recorder,
            string? runId, CancellationToken token = default)
        {
            var clock = Stopwatch.StartNew();

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new
                {
                    query = request.Query,
                    max_results = request.MaxResults,
                    search_depth = ToSearchDepth(request.Depth),
                    include_answer = ToIncludeAnswer(request.AnswerMode),
                    include_raw_content = ToIncludeRawContent(request.RawContentMode)
                })
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await httpClient.SendAsync(httpRequest, token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                throw new HttpRequestException(
                    $"Tavily web search answered {(int)response.StatusCode} for query '{request.Query}': " +
                    Shorten(body));
            }

            var rawJson = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            using var document = JsonDocument.Parse(rawJson);

            var result = Read(document.RootElement, rawJson);

            if (recorder is not null && runId is not null)
            {
                await recorder.RecordWebSearchCallAsync(runId, request.Query, rawJson, clock.ElapsedMilliseconds,
                    token).ConfigureAwait(false);
            }

            return result;
        }

        private static string ToSearchDepth(WebSearchDepth depth)
        {
            return depth switch
            {
                WebSearchDepth.Basic => "basic",
                WebSearchDepth.Advanced => "advanced",
                _ => throw new ArgumentOutOfRangeException(nameof(depth), depth, null)
            };
        }

        private static object ToIncludeAnswer(WebSearchAnswerMode answerMode)
        {
            return answerMode switch
            {
                WebSearchAnswerMode.None => false,
                WebSearchAnswerMode.Basic => "basic",
                WebSearchAnswerMode.Advanced => "advanced",
                _ => throw new ArgumentOutOfRangeException(nameof(answerMode), answerMode, null)
            };
        }

        private static object ToIncludeRawContent(WebSearchRawContentMode rawContentMode)
        {
            return rawContentMode switch
            {
                WebSearchRawContentMode.None => false,
                WebSearchRawContentMode.Text => "text",
                WebSearchRawContentMode.Markdown => "markdown",
                _ => throw new ArgumentOutOfRangeException(nameof(rawContentMode), rawContentMode, null)
            };
        }

        private static WebSearchResponse Read(JsonElement root, string rawJson)
        {
            var answer = root.TryGetProperty("answer", out var answerElement)
                         && answerElement.ValueKind == JsonValueKind.String
                ? answerElement.GetString()
                : null;

            var results = new List<WebSearchResult>();

            if (root.TryGetProperty("results", out var resultsElement)
                && resultsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in resultsElement.EnumerateArray())
                {
                    results.Add(new WebSearchResult(
                        element.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                        element.TryGetProperty("url", out var url) ? url.GetString() ?? "" : "",
                        element.TryGetProperty("content", out var content) ? content.GetString() ?? "" : "",
                        element.TryGetProperty("score", out var score) && score.TryGetDouble(out var parsedScore)
                            ? parsedScore
                            : null,
                        element.TryGetProperty("published_date", out var publishedDate)
                        && publishedDate.ValueKind == JsonValueKind.String
                            ? publishedDate.GetString()
                            : null,
                        element.TryGetProperty("raw_content", out var rawContent)
                        && rawContent.ValueKind == JsonValueKind.String
                            ? rawContent.GetString()
                            : null));
                }
            }

            return new WebSearchResponse(answer, results, rawJson);
        }

        private static string Shorten(string value)
        {
            var trimmed = value.Trim().Replace('\n', ' ');

            return trimmed.Length <= 300 ? trimmed : trimmed[..300] + "...";
        }
    }
}