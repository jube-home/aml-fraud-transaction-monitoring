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

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jube.AskJooby.Rag.Models;
using Jube.Service.AskJoobyRagChunk.Interfaces;
using log4net;

namespace Jube.Service.AskJoobyRagChunk
{
    public sealed class BgeReranker(HttpClient httpClient, string endpoint, string? apiKey, string model, ILog log)
        : IRagReranker
    {
        private const int RerankTimeoutSeconds = 10;

        public string Model => model;

        public async Task<RerankOutcome> RankAsync(
            string query, IReadOnlyList<string> documents, int topN, CancellationToken token = default)
        {
            if (documents.Count == 0 || topN <= 0)
            {
                return new RerankOutcome(null, null);
            }

            using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(RerankTimeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, guard.Token);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = JsonContent.Create(new
                {
                    model,
                    query,
                    documents,
                    top_n = Math.Min(topN, documents.Count),

                    top_k = Math.Min(topN, documents.Count),
                    return_documents = false
                });

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);
                }

                using var response = await httpClient.SendAsync(request, linked.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(linked.Token).ConfigureAwait(false);

                    log.Warn($"AskJooby reranker: {endpoint} answered {(int)response.StatusCode} for model "
                             + $"{model}, so this search falls back to the retrieval order. {Shorten(body)}");

                    return new RerankOutcome(null, null);
                }

                await using var stream = await response.Content.ReadAsStreamAsync(linked.Token).ConfigureAwait(false);

                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: linked.Token)
                    .ConfigureAwait(false);

                return new RerankOutcome(Read(document.RootElement, documents.Count),
                    ReadTokensCompared(document.RootElement));
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                log.Warn($"AskJooby reranker: {endpoint} did not answer within {RerankTimeoutSeconds}s for "
                         + $"model {model}, so this search falls back to the retrieval order.");

                return new RerankOutcome(null, null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                log.Warn($"AskJooby reranker: {endpoint} could not be reached for model {model}, so this "
                         + "search falls back to the retrieval order.", exception);

                return new RerankOutcome(null, null);
            }
        }

        private static int? ReadTokensCompared(JsonElement root)
        {
            return root.ValueKind == JsonValueKind.Object
                   && root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object
                   && usage.TryGetProperty("total_tokens", out var totalTokens) &&
                   totalTokens.TryGetInt32(out var tokens)
                ? tokens
                : null;
        }

        private static IReadOnlyList<RerankedDocument>? Read(JsonElement root, int candidateCount)
        {
            var results = root.ValueKind switch
            {
                JsonValueKind.Array => root,
                JsonValueKind.Object when root.TryGetProperty("results", out var inner)
                                          && inner.ValueKind == JsonValueKind.Array => inner,
                _ => default
            };

            if (results.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var ranked = new List<RerankedDocument>();

            foreach (var element in results.EnumerateArray())
            {
                if (!element.TryGetProperty("index", out var indexElement)
                    || !indexElement.TryGetInt32(out var index)
                    || index < 0 || index >= candidateCount)
                {
                    continue;
                }

                var score = element.TryGetProperty("relevance_score", out var relevance)
                            && relevance.TryGetDouble(out var relevanceScore)
                    ? relevanceScore
                    : element.TryGetProperty("score", out var plain) && plain.TryGetDouble(out var plainScore)
                        ? plainScore
                        : double.NaN;

                if (!double.IsNaN(score))
                {
                    ranked.Add(new RerankedDocument(index, score));
                }
            }

            return ranked.OrderByDescending(candidate => candidate.Score).ToList();
        }

        private static string Shorten(string value)
        {
            var trimmed = value.Trim().Replace('\n', ' ');

            return trimmed.Length <= 300 ? trimmed : trimmed[..300] + "...";
        }
    }
}