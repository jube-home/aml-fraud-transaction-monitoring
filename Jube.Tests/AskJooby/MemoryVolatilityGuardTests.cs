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
    [Trait("Category", "Unit")]
    public sealed class MemoryVolatilityGuardTests
    {
        [Theory]
        [InlineData("Account 8f14e45f-ceea-467f-a0e9-3c1a2b4d5e6f has nine rules")]
        [InlineData("Model Id 2 was synchronised on 2026-10-04")]
        [InlineData("Model 2 currently holds 9 rules")]
        [InlineData("Model 2 holds 3 counters at present")]
        public async Task ALiveValueIsRefusedAndTheModelIsToldToQueryInsteadAsync(string content)
        {
            var tool = new MemoryTool(new RecordingMemoryClient());

            var act = () => tool.Remember(content);

            (await act.Should().ThrowAsync<ArgumentException>())
                .Which.Message.Should().Contain("query the live value");
        }

        [Theory]
        [InlineData(
            "To count events over a window, filter on the status column and group by the entity key, using @entityKey and @windowStart as parameters.")]
        [InlineData(
            "Build a velocity rule in this order: the counter first, then the incrementing rule, then the reading rule, then the priorities.")]
        public async Task AGenericPlanOrParameterisedPatternIsAcceptedAsync(string content)
        {
            var client = new RecordingMemoryClient();
            var tool = new MemoryTool(client);

            var result = await tool.Remember(content);

            result.Should().NotBeNullOrWhiteSpace();
            client.Stored.Should().ContainSingle().Which.Should().Be(content);
        }

        private sealed class RecordingMemoryClient : IMemoryClient
        {
            public List<string> Stored { get; } = [];

            public Task<MemoryEntry> RememberAsync(string content, string scope, byte? status = null,
                DateTime? expiresAt = null, string kind = "Fact", CancellationToken token = default)
            {
                Stored.Add(content);

                return Task.FromResult(new MemoryEntry(1, scope, "user", content));
            }

            public Task<IReadOnlyList<MemoryEntry>> RecallAsync(string query, int topK = 5,
                DateTime? since = null, bool includeReflections = false, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<MemoryEntry>>([]);
            }
        }
    }
}