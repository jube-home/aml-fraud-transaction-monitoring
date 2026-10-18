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

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.AskJoobyRagChunk;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class BgeRerankerTests
    {
        private static readonly IReadOnlyList<string> documents = ["first", "second", "third"];

        private static BgeReranker Reranker(HttpStatusCode status, string body)
        {
            return new BgeReranker(
                new HttpClient(new StubHandler(status, body)), "https://reranker.test/v1/rerank",
                "key", "bge-reranker-v2-m3", TestLog.NoOp);
        }

        private static BgeReranker Unreachable()
        {
            return new BgeReranker(
                new HttpClient(new ThrowingHandler()), "https://reranker.test/v1/rerank",
                "key", "bge-reranker-v2-m3", TestLog.NoOp);
        }

        [Fact]
        public async Task TheCohereShapeIsReadAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK,
                """{"results":[{"index":2,"relevance_score":0.91},{"index":0,"relevance_score":0.42}]}""");

            var outcome = await reranker.RankAsync("why is the counter empty", documents, 2, CancellationToken.None);

            outcome.Documents.Should().NotBeNull();
            outcome.Documents!.Should().HaveCount(2);
            outcome.Documents![0].Index.Should().Be(2);
            outcome.Documents[0].Score.Should().BeApproximately(0.91, 1e-9);
        }

        [Fact]
        public async Task TheTextEmbeddingsInferenceShapeIsReadAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK, """[{"index":1,"score":0.77}]""");

            var outcome = await reranker.RankAsync("why is the counter empty", documents, 2, CancellationToken.None);

            outcome.Documents.Should().NotBeNull();
            outcome.Documents!.Should().ContainSingle();
            outcome.Documents![0].Index.Should().Be(1);
            outcome.Documents[0].Score.Should().BeApproximately(0.77, 1e-9);
        }

        [Fact]
        public async Task CandidatesComeBackBestFirstWhateverOrderTheProviderSentAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK,
                """{"results":[{"index":0,"relevance_score":0.1},{"index":1,"relevance_score":0.9},{"index":2,"relevance_score":0.5}]}""");

            var outcome = await reranker.RankAsync("q", documents, 3, CancellationToken.None);

            outcome.Documents!.Should().HaveCount(3);
            outcome.Documents![0].Index.Should().Be(1);
            outcome.Documents[1].Index.Should().Be(2);
            outcome.Documents[2].Index.Should().Be(0);
        }

        [Fact]
        public async Task AnIndexOutsideTheCandidatesIsDiscardedRatherThanThrowingAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK,
                """{"results":[{"index":9,"relevance_score":0.99},{"index":1,"relevance_score":0.3}]}""");

            var outcome = await reranker.RankAsync("q", documents, 3, CancellationToken.None);

            outcome.Documents!.Should().ContainSingle().Which.Index.Should().Be(1);
        }

        [Fact]
        public async Task ARefusalIsReportedAsNotHavingRunRatherThanAsRejectingEverythingAsync()
        {
            var reranker = Reranker(HttpStatusCode.NotFound, """{"error":"unknown route /v1/rerank"}""");

            var outcome = await reranker.RankAsync("q", documents, 3, CancellationToken.None);

            (outcome.Documents is null).Should().BeTrue(
                "null tells the caller to keep the order retrieval gave it; an empty list would read "
                + "as the reranker having judged every candidate irrelevant, and a search must not "
                + "come back empty because a second stage was unreachable");
        }

        [Fact]
        public async Task AnUnreachableEndpointIsReportedAsNotHavingRunAsync()
        {
            var outcome = await Unreachable().RankAsync("q", documents, 3, CancellationToken.None);

            (outcome.Documents is null).Should().BeTrue();
        }

        [Fact]
        public async Task AnAnswerInNoRecognisedShapeIsReportedAsNotHavingRunAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK, """{"unexpected":true}""");

            var outcome = await reranker.RankAsync("q", documents, 3, CancellationToken.None);

            (outcome.Documents is null).Should().BeTrue();
        }

        [Fact]
        public async Task NothingToRankIsNotACallAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK, """{"results":[]}""");

            (await reranker.RankAsync("q", [], 3, CancellationToken.None)).Documents.Should().BeNull();
            (await reranker.RankAsync("q", documents, 0, CancellationToken.None)).Documents.Should().BeNull();
        }

        [Fact]
        public async Task AUsageTokenCountInTheResponseIsCapturedAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK,
                """{"results":[{"index":0,"relevance_score":0.5}],"usage":{"total_tokens":123}}""");

            var outcome = await reranker.RankAsync("q", documents, 1, CancellationToken.None);

            outcome.TokensCompared.Should().Be(123);
        }

        [Fact]
        public async Task NoUsageFieldLeavesTokensComparedNullAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK, """{"results":[{"index":0,"relevance_score":0.5}]}""");

            var outcome = await reranker.RankAsync("q", documents, 1, CancellationToken.None);

            outcome.TokensCompared.Should().BeNull();
        }

        [Fact]
        public async Task TheBareArrayShapeNeverCarriesUsageAsync()
        {
            var reranker = Reranker(HttpStatusCode.OK, """[{"index":0,"score":0.5}]""");

            var outcome = await reranker.RankAsync("q", documents, 1, CancellationToken.None);

            outcome.TokensCompared.Should().BeNull();
        }

        private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }
        }

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException("no route to host");
            }
        }
    }
}