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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Memory;
using Jube.AskJooby.Tools.Memory;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class MemoryToolTests
    {
        [Fact]
        public async Task RecallLabelsAThumbsUpMemoryAsACorrectExampleAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([
                new MemoryEntry(1, "System", "alice", "Some fact.", Status: 1)
            ]));

            var result = await tool.Recall("some fact");

            result.Should().Contain("This is an example of a correct response.");
        }

        [Fact]
        public async Task RecallLabelsAThumbsDownMemoryAsAnIncorrectExampleAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([
                new MemoryEntry(1, "System", "alice", "Some wrong claim.", Status: 0)
            ]));

            var result = await tool.Recall("some wrong claim");

            result.Should().Contain("This is an example of an incorrect response -- do not repeat this.");
        }

        [Fact]
        public async Task RecallLabelsAnUnreviewedMemoryAsAgentGeneratedAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([
                new MemoryEntry(1, "System", "alice", "Some durable fact.", Status: null)
            ]));

            var result = await tool.Recall("some durable fact");

            result.Should().Contain("This is an example of an agent generated memory.");
        }

        [Fact]
        public async Task RememberPassesNoExpiryByDefaultAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);

            await tool.Remember("Some fact.");

            client.LastExpiresAt.Should().BeNull();
        }

        [Fact]
        public async Task RememberConvertsExpiresInMinutesToAFutureUtcTimestampAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);
            var before = DateTime.UtcNow;

            await tool.Remember("Short-lived state.", expiresInMinutes: 60);

            var after = DateTime.UtcNow;
            client.LastExpiresAt.Should().NotBeNull();

            client.LastExpiresAt!.Value.Should().BeOnOrAfter(before.AddMinutes(60))
                .And.BeOnOrBefore(after.AddMinutes(60));
        }

        [Fact]
        public Task RememberRejectsAnExpiryOnSystemScopeMemoriesAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([]));

            var act = () => tool.Remember("Shared fact.", "System", 60);

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public Task RememberRejectsAnUnrecognisedScopeAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([]));

            var act = () => tool.Remember("Some fact.", "Team");

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task RememberAllowsAnExpiryOnConversationScopeMemoriesAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);

            await tool.Remember("Task-scoped note.", "Conversation", 60);

            client.LastScope.Should().Be("Conversation");
            client.LastExpiresAt.Should().NotBeNull();
        }

        [Fact]
        public Task RememberRejectsANonPositiveExpiryAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([]));

            var act = () => tool.Remember("Some fact.", expiresInMinutes: 0);

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task RecallPassesNoSinceFilterByDefaultAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);

            await tool.Recall("some fact");

            client.LastSince.Should().BeNull();
        }

        [Fact]
        public async Task RecallConvertsSinceDaysAgoToAPastUtcTimestampAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);
            var before = DateTime.UtcNow;

            await tool.Recall("some fact", sinceDaysAgo: 7);

            var after = DateTime.UtcNow;
            client.LastSince.Should().NotBeNull();

            client.LastSince!.Value.Should().BeOnOrAfter(before.AddDays(-7))
                .And.BeOnOrBefore(after.AddDays(-7));
        }

        [Fact]
        public Task RecallRejectsANonPositiveSinceDaysAgoAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([]));

            var act = () => tool.Recall("some fact", sinceDaysAgo: 0);

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task RememberDefaultsToFactKindAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);

            await tool.Remember("Some fact.");

            client.LastKind.Should().Be("Fact");
        }

        [Fact]
        public async Task RememberPassesReflectionKindThroughAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);

            await tool.Remember("Working through the approach for this task.", kind: "Reflection");

            client.LastKind.Should().Be("Reflection");
        }

        [Fact]
        public Task RememberRejectsAnUnrecognisedKindAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([]));

            var act = () => tool.Remember("Some fact.", kind: "Opinion");

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task RecallExcludesReflectionsByDefaultAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);

            await tool.Recall("some fact");

            client.LastIncludeReflections.Should().BeFalse();
        }

        [Fact]
        public async Task RecallPassesIncludeReflectionsThroughWhenRequestedAsync()
        {
            var client = new FakeMemoryClient([]);
            var tool = new MemoryTool(client);

            await tool.Recall("some fact", includeReflections: true);

            client.LastIncludeReflections.Should().BeTrue();
        }

        [Fact]
        public async Task RecallTagsAReflectionHitInItsOutputAsync()
        {
            var tool = new MemoryTool(new FakeMemoryClient([
                new MemoryEntry(1, "User", "alice", "Planning how to tackle this.", Kind: "Reflection")
            ]));

            var result = await tool.Recall("planning", includeReflections: true);

            result.Should().Contain("[Reflection]");
        }

        private sealed class FakeMemoryClient(IReadOnlyList<MemoryEntry> hits) : IMemoryClient
        {
            public DateTime? LastExpiresAt { get; private set; }
            public DateTime? LastSince { get; private set; }
            public string? LastKind { get; private set; }
            public string? LastScope { get; private set; }
            public bool LastIncludeReflections { get; private set; }

            public Task<MemoryEntry> RememberAsync(string content, string scope, byte? status = null,
                DateTime? expiresAt = null, string kind = "Fact", CancellationToken token = default)
            {
                LastExpiresAt = expiresAt;
                LastKind = kind;
                LastScope = scope;

                return Task.FromResult(new MemoryEntry(1, scope, "user", content, Status: status,
                    ExpiresAt: expiresAt, Kind: kind));
            }

            public Task<IReadOnlyList<MemoryEntry>> RecallAsync(string query, int topK = 5, DateTime? since = null,
                bool includeReflections = false, CancellationToken token = default)
            {
                LastSince = since;
                LastIncludeReflections = includeReflections;

                return Task.FromResult(hits);
            }
        }
    }
}