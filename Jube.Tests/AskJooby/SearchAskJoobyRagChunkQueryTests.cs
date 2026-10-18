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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.SearchAskJoobyRagChunkQuery;
using Jube.Data.Repository.Models;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class SearchAskJoobyRagChunkQueryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int EmbeddingDimensions = 1024;
        private readonly System.Collections.Generic.List<string> createdHashes = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            if (createdHashes.Count > 0)
            {
                await dbContext.AskJoobyRagChunk.Where(w => createdHashes.Contains(w.Hash)).DeleteAsync();
            }
        }

        private string NewHash()
        {
            var hash = $"{DatabaseFixture.Prefix}rag-{Guid.NewGuid():N}";
            createdHashes.Add(hash);

            return hash;
        }

        private async Task InsertChunkAsync(DbContext dbContext, string hash, string content,
            float[]? embedding = null)
        {
            await dbContext.InsertAsync(new AskJoobyRagChunk
            {
                Hash = hash, CategoryId = (byte)AskJoobyRagChunkCategory.Document,
                KindId = (byte)AskJoobyRagChunkKind.Section, SourceFile = "test.md",
                QualifiedName = hash, Content = content, CreatedDate = DateTime.UtcNow
            });

            if (embedding is not null)
            {
                await dbContext.ExecuteAsync(
                    "UPDATE \"AskJoobyRagChunk\" SET \"Embedding\" = @embedding::vector WHERE \"Hash\" = @hash",
                    new DataParameter("embedding", ToPgVectorText(embedding)),
                    new DataParameter("hash", hash));
            }
        }

        private static string ToPgVectorText(float[] values)
        {
            return "[" + string.Join(",", values.Select(v => v.ToString("R", CultureInfo.InvariantCulture))) + "]";
        }

        private static float[] UnitVector(int dimension)
        {
            var vector = new float[EmbeddingDimensions];
            vector[dimension] = 1f;

            return vector;
        }

        [Fact]
        public async Task KeywordOnlySearchMatchesAChunkThatOnlyContainsOneOfSeveralQueryTermsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var hash = NewHash();
            var tokenA = $"zzragmarkera{Guid.NewGuid():N}";
            var tokenB = $"zzragmarkerb{Guid.NewGuid():N}";
            var tokenC = $"zzragmarkerc{Guid.NewGuid():N}";
            var tokenD = $"zzragmarkerd{Guid.NewGuid():N}";

            await InsertChunkAsync(dbContext, hash, $"This chunk only mentions {tokenA}.");

            var query = new SearchAskJoobyRagChunkQuery(dbContext);
            var rows = await query.ExecuteAsync($"{tokenA} {tokenB} {tokenC} {tokenD}", string.Empty, 10);

            rows.Should().Contain(r => r.Hash == hash,
                "a chunk covering only one of several named concepts must still be found -- requiring " +
                "every concept to co-occur in one chunk is exactly the AND-semantics bug being fixed, and " +
                "no real corpus row can contain any of these fabricated tokens to crowd it out");
        }

        [Fact]
        public async Task KeywordOnlySearchStillHonoursTheMinScoreFloorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var hash = NewHash();
            var marker = $"zzragmarker{Guid.NewGuid():N}";

            await InsertChunkAsync(dbContext, hash, $"{marker} describes the abstraction rule in detail.");

            var query = new SearchAskJoobyRagChunkQuery(dbContext);

            var rows = await query.ExecuteAsync(
                $"{marker} gateway activation override sampling", string.Empty, 10, minScore: 1.0);

            rows.Should().NotContain(r => r.Hash == hash,
                "ts_rank is a genuine relevance score on the text-only fallback, so an impossibly high " +
                "floor must still filter a weak match out");
        }

        [Fact]
        public async Task HybridSearchDoesNotApplyTheMinScoreFloorToTheFusedRankScoreAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var hash = NewHash();
            var marker = $"zzragmarker{Guid.NewGuid():N}";
            var embedding = UnitVector(0);

            await InsertChunkAsync(dbContext, hash, $"{marker} unrelated filler content.", embedding);

            var query = new SearchAskJoobyRagChunkQuery(dbContext);
            var rows = await query.ExecuteAsync(marker, ToPgVectorText(embedding), 10, minScore: 1.0);

            rows.Should().Contain(r => r.Hash == hash,
                "a reciprocal-rank-fusion score is a rank transform, not a relevance measure, so an " +
                "absolute floor must never be applied to it -- the old floor silently discarded any " +
                "match found by only one leg once its own rank fell past roughly the top 16 of 40");
        }
    }
}