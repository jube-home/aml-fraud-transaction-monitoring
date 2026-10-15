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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.SearchAskJoobyMemoryQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class AskJoobyMemoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> createdConversationIds = [];
        private readonly List<int> createdTenantIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (createdTenantIds.Count == 0)
            {
                return;
            }

            await using var dbContext = fx.GetDbContext();

            await dbContext.AskJoobyMemory.Where(w => createdTenantIds.Contains(w.TenantRegistryId!.Value))
                .DeleteAsync();

            if (createdConversationIds.Count > 0)
            {
                await dbContext.AskJoobyConversation.Where(w => createdConversationIds.Contains(w.Id))
                    .DeleteAsync();
            }

            await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedTenantAsync(DbContext dbContext)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyMemoryTenant{Guid.NewGuid():N}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(id);

            return id;
        }

        private async Task<int> SeedConversationAsync(
            DbContext dbContext, int tenantRegistryId, string createdUser)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyConversation
            {
                TenantRegistryId = tenantRegistryId,
                Guid = Guid.NewGuid(),
                Title = $"{DatabaseFixture.Prefix}AskJoobyMemoryConversation",
                CreatedDate = DateTime.UtcNow,
                CreatedUser = createdUser,
                Version = 1
            });
            createdConversationIds.Add(id);

            return id;
        }

        private static string BuildVector(double seed)
        {
            var random = new Random((int)(seed * 1000));

            var values = Enumerable.Range(0, 1024)
                .Select(i => (Math.Sin(seed + i) + random.NextDouble() * 0.01).ToString(CultureInfo.InvariantCulture));

            return "[" + string.Join(",", values) + "]";
        }

        [Fact]
        public async Task RecallNeverSurfacesAnotherUsersUserScopedMemoryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "alice");
            var query = new SearchAskJoobyMemoryQuery(dbContext);

            await repository.InsertAsync(
                "Alice likes to review TTL counter rules every Monday morning.", (byte)AskJoobyMemoryScope.User, null);
            await repository.InsertAsync(
                "Carol prefers dark mode in the rule editor.", (byte)AskJoobyMemoryScope.User, null);

            var asAlice = await query.ExecuteAsync("TTL counter", string.Empty, tenantId, "alice", 10);
            var asCarol = await query.ExecuteAsync("TTL counter", string.Empty, tenantId, "carol", 10);

            asAlice.Should().Contain(m => m.Content.Contains("TTL counter"));

            asCarol.Should().NotContain(m => m.Content.Contains("TTL counter"),
                "a User-scope memory must never surface for a different user");
        }

        [Fact]
        public async Task RecallSurfacesSystemScopedMemoriesRegardlessOfWhoCreatedThemAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "bob");
            var query = new SearchAskJoobyMemoryQuery(dbContext);

            await repository.InsertAsync(
                "The Piecrust Redis extension supports hash expiry directly.", (byte)AskJoobyMemoryScope.System, null);

            var asDave = await query.ExecuteAsync("piecrust redis", string.Empty, tenantId, "dave", 10);

            asDave.Should().ContainSingle(m => m.CreatedUser == "bob" && m.ScopeId == (byte)AskJoobyMemoryScope.System,
                "System scope is deliberately shared tenant-wide, even to a user who created nothing");
        }

        [Fact]
        public async Task RecallRanksByVectorSimilarityWhenAnEmbeddingIsSuppliedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "erin");
            var query = new SearchAskJoobyMemoryQuery(dbContext);

            var closeVector = BuildVector(1.0);
            var queryVector = BuildVector(1.0);
            var farVector = BuildVector(50.0);

            await repository.InsertAsync("Close match content.", (byte)AskJoobyMemoryScope.User, closeVector);
            await repository.InsertAsync("Far match content.", (byte)AskJoobyMemoryScope.User, farVector);

            var results = await query.ExecuteAsync(
                "content that shares no keywords with either row", queryVector, tenantId, "erin", 10);

            results.Should().NotBeEmpty();

            results.First().Content.Should().Be("Close match content.",
                "the near-identical embedding should rank first even with no text overlap");
        }

        [Fact]
        public async Task InsertAsyncPersistsTheEmbeddingColumnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "frank");
            var vector = BuildVector(3.0);

            var saved = await repository.InsertAsync("Something worth remembering.", (byte)AskJoobyMemoryScope.User,
                vector);

            var hasEmbedding = await dbContext.QueryToListAsync<bool>(
                "SELECT \"Embedding\" IS NOT NULL FROM \"AskJoobyMemory\" WHERE \"Id\" = @id",
                CancellationToken.None, new DataParameter("id", saved.Id));

            hasEmbedding.Should().ContainSingle(v => v);
        }

        [Fact]
        public async Task InsertAsyncAndSearchBothPreserveTheFeedbackStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "grace");
            var query = new SearchAskJoobyMemoryQuery(dbContext);

            const string sharedToken = "zzznarwhalprobe";

            var positive = await repository.InsertAsync(
                $"{sharedToken}: a confirmed-correct example.", (byte)AskJoobyMemoryScope.System, null, 1);
            var negative = await repository.InsertAsync(
                $"{sharedToken}: a confirmed-incorrect example.", (byte)AskJoobyMemoryScope.System, null, 0);

            var neutral = await repository.InsertAsync(
                $"{sharedToken}: an unreviewed agent-generated fact.", (byte)AskJoobyMemoryScope.System, null);

            positive.StatusId.Should().Be(1);
            negative.StatusId.Should().Be(0);
            neutral.StatusId.Should().BeNull();

            var results = await query.ExecuteAsync(sharedToken, string.Empty, tenantId, "grace", 10);

            results.Should().Contain(m => m.Id == positive.Id && m.StatusId == 1);
            results.Should().Contain(m => m.Id == negative.Id && m.StatusId == 0);
            results.Should().Contain(m => m.Id == neutral.Id && m.StatusId == null);
        }

        [Fact]
        public async Task RecallExcludesAMemoryWhoseExpiryHasAlreadyPassedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "heidi");
            var query = new SearchAskJoobyMemoryQuery(dbContext);

            const string sharedToken = "zzzexpirygopher";

            var expired = await repository.InsertAsync(
                $"{sharedToken}: short-lived scratch note.", (byte)AskJoobyMemoryScope.User, null,
                expiresAt: DateTime.UtcNow.AddMinutes(-5));

            var stillLive = await repository.InsertAsync(
                $"{sharedToken}: still within its window.", (byte)AskJoobyMemoryScope.User, null,
                expiresAt: DateTime.UtcNow.AddMinutes(5));

            var neverExpires = await repository.InsertAsync(
                $"{sharedToken}: a durable fact.", (byte)AskJoobyMemoryScope.User, null);

            var results = await query.ExecuteAsync(sharedToken, string.Empty, tenantId, "heidi", 10);

            results.Should().NotContain(m => m.Id == expired.Id,
                "a memory past its ExpiresAt must be filtered out of recall entirely");

            results.Should().Contain(m => m.Id == stillLive.Id);
            results.Should().Contain(m => m.Id == neverExpires.Id);
        }

        [Fact]
        public async Task RecallSinceExcludesMemoriesRecordedBeforeTheCutoffAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "ivan");
            var query = new SearchAskJoobyMemoryQuery(dbContext);

            const string sharedToken = "zzzsincewombat";
            var recent = await repository.InsertAsync($"{sharedToken}: recorded just now.",
                (byte)AskJoobyMemoryScope.User, null);
            var old = await repository.InsertAsync($"{sharedToken}: recorded a while ago.",
                (byte)AskJoobyMemoryScope.User, null);
            await BackdateAsync(dbContext, old.Id, DateTime.UtcNow.AddDays(-30));

            var results = await query.ExecuteAsync(
                sharedToken, string.Empty, tenantId, "ivan", 10, DateTime.UtcNow.AddDays(-1));

            results.Should().Contain(m => m.Id == recent.Id);

            results.Should().NotContain(m => m.Id == old.Id,
                "since should exclude memories recorded before the cutoff");
        }

        [Fact]
        public async Task RecallRanksAFresherMemoryAboveAnEquallyMatchingOlderOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "judy");
            var query = new SearchAskJoobyMemoryQuery(dbContext, 1);

            const string sharedToken = "zzzdecaylemur";
            var fresh = await repository.InsertAsync($"{sharedToken}: a fact worth recalling.",
                (byte)AskJoobyMemoryScope.User, null);
            var old = await repository.InsertAsync($"{sharedToken}: a fact worth recalling.",
                (byte)AskJoobyMemoryScope.User, null);
            await BackdateAsync(dbContext, old.Id, DateTime.UtcNow.AddDays(-10));

            var results = await query.ExecuteAsync(sharedToken, string.Empty, tenantId, "judy", 10);

            var freshResult = results.Single(m => m.Id == fresh.Id);
            var oldResult = results.Single(m => m.Id == old.Id);

            freshResult.Score.Should().BeGreaterThan(oldResult.Score,
                "a 10-day-old memory should rank below an equally-matching fresh one under a 1-day half-life");
        }

        private static Task<List<int>> BackdateAsync(DbContext dbContext, int id, DateTime createdDate)
        {
            return dbContext.QueryToListAsync<int>(
                "UPDATE \"AskJoobyMemory\" SET \"CreatedDate\" = @createdDate WHERE \"Id\" = @id RETURNING \"Id\"",
                CancellationToken.None,
                new DataParameter("createdDate", createdDate),
                new DataParameter("id", id));
        }

        [Fact]
        public async Task InsertAsyncDefaultsToFactKindAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "karl");

            var saved = await repository.InsertAsync("A durable fact.", (byte)AskJoobyMemoryScope.User, null);

            saved.KindId.Should().Be((byte)AskJoobyMemoryKind.Fact);
        }

        [Fact]
        public async Task RecallExcludesReflectionsByDefaultButIncludesFactsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "laura");
            var query = new SearchAskJoobyMemoryQuery(dbContext);

            const string sharedToken = "zzzreflectionibis";
            var fact = await repository.InsertAsync($"{sharedToken}: a confirmed observation.",
                (byte)AskJoobyMemoryScope.User, null);

            var reflection = await repository.InsertAsync(
                $"{sharedToken}: working through how to approach this.", (byte)AskJoobyMemoryScope.User, null,
                kindId: (byte)AskJoobyMemoryKind.Reflection);

            var defaultResults = await query.ExecuteAsync(sharedToken, string.Empty, tenantId, "laura", 10);

            var withReflections = await query.ExecuteAsync(
                sharedToken, string.Empty, tenantId, "laura", 10, includeReflections: true);

            defaultResults.Should().Contain(m => m.Id == fact.Id);

            defaultResults.Should().NotContain(m => m.Id == reflection.Id,
                "Reflections must be excluded from recall unless explicitly requested");

            withReflections.Should().Contain(m => m.Id == reflection.Id);
            withReflections.Should().Contain(m => m.Id == fact.Id);
        }

        [Fact]
        public async Task ConversationScopeIsVisibleWithinItsOwnConversationOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationA = await SeedConversationAsync(dbContext, tenantId, "mia");
            var conversationB = await SeedConversationAsync(dbContext, tenantId, "mia");
            var repository = new AskJoobyMemoryRepository(dbContext, tenantId, "mia");
            var query = new SearchAskJoobyMemoryQuery(dbContext);

            const string sharedToken = "zzzconversationtoucan";

            var noteInA = await repository.InsertAsync(
                $"{sharedToken}: scoped to this specific task.", (byte)AskJoobyMemoryScope.Conversation, null,
                conversationId: conversationA);

            var resultsInA = await query.ExecuteAsync(
                sharedToken, string.Empty, tenantId, "mia", 10, conversationId: conversationA);
            var resultsInB = await query.ExecuteAsync(
                sharedToken, string.Empty, tenantId, "mia", 10, conversationId: conversationB);

            var resultsWithNoConversation = await query.ExecuteAsync(
                sharedToken, string.Empty, tenantId, "mia", 10);

            resultsInA.Should().Contain(m => m.Id == noteInA.Id,
                "a Conversation-scope memory must be visible from within its own conversation");
            resultsInB.Should().NotContain(m => m.Id == noteInA.Id,
                "a Conversation-scope memory must never leak into a different conversation");

            resultsWithNoConversation.Should().NotContain(m => m.Id == noteInA.Id);
        }
    }
}