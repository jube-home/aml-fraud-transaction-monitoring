/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Engine.InboundQueue;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.InboundQueue
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class InboundQueueConsumerTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly Guid model = Guid.NewGuid();

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.TransactionInboundQueue.Where(w => w.EntityAnalysisModelGuid == model).DeleteAsync();
        }

        private async Task<List<long>> EnqueueAsync(int count, string prefix = "m")
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new TransactionInboundQueueRepository(dbContext);
            var ids = new List<long>();

            for (var i = 0; i < count; i++)
            {
                var row = new TransactionInboundQueue
                {
                    EntityAnalysisModelGuid = model, Payload = Encoding.UTF8.GetBytes($"{{\"n\":\"{prefix}{i}\"}}"),
                    EnqueuedBy = "test"
                };
                await repository.InsertAsync(row);
                ids.Add(row.Id);
            }

            return ids;
        }

        private InboundQueueConsumer Consumer(Func<Guid, byte[], CancellationToken, Task<bool>> invoke,
            InboundModelState state = InboundModelState.Ready, bool loaded = true, int attempts = 5, int batch = 50,
            int concurrency = 8, string node = "node-a", int visibility = 120) =>
            new(fx.ConnectionString, TestLog.NoOp,
                new InboundQueueOptions(batch, concurrency, 10, attempts, visibility), node, () => loaded,
                guid => guid == model ? state : InboundModelState.Unknown, invoke);

        private async Task<List<TransactionInboundQueue>> RowsAsync()
        {
            await using var dbContext = fx.GetDbContext();

            return await dbContext.TransactionInboundQueue.Where(w => w.EntityAnalysisModelGuid == model)
                .OrderBy(o => o.Id).ToListAsync();
        }

        [Fact]
        public async Task ProcessedRowsAreDeletedAndEachPayloadReachesTheModelAsync()
        {
            await EnqueueAsync(5);
            var seen = new ConcurrentBag<string>();

            var count = await Consumer((_, bytes, _) =>
            {
                seen.Add(Encoding.UTF8.GetString(bytes));

                return Task.FromResult(true);
            }).RunOnceAsync(CancellationToken.None);

            count.Should().Be(5);
            seen.Should().BeEquivalentTo(Enumerable.Range(0, 5).Select(i => $"{{\"n\":\"m{i}\"}}"));
            (await RowsAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task TwoNodesRunningTogetherProcessEveryRowExactlyOnceAsync()
        {
            await EnqueueAsync(60);
            var seen = new ConcurrentBag<string>();

            Task<bool> Handle(Guid _, byte[] bytes, CancellationToken __)
            {
                seen.Add(Encoding.UTF8.GetString(bytes));

                return Task.FromResult(true);
            }

            var a = Consumer(Handle, batch: 10, node: "a");
            var b = Consumer(Handle, batch: 10, node: "b");

            for (var round = 0; round < 8; round++)
            {
                await Task.WhenAll(a.RunOnceAsync(CancellationToken.None), b.RunOnceAsync(CancellationToken.None));
            }

            seen.Should().HaveCount(60);
            seen.Distinct().Should().HaveCount(60);
            (await RowsAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task AFailureIsRetriedLaterWithAGrowingDelayThenKilledAtTheMaximumAsync()
        {
            var ids = await EnqueueAsync(1);

            await Consumer((_, _, _) => Task.FromResult(false), attempts: 3).RunOnceAsync(CancellationToken.None);

            var first = (await RowsAsync()).Single();
            (first.StatusId, first.Attempts, first.LastError, first.LockedBy)
                .Should().Be(((short)TransactionInboundQueueStatus.Pending, 1, "NoResponse", null));
            first.AvailableDate.Should().BeAfter(DateTime.UtcNow.AddSeconds(2));

            await using (var dbContext = fx.GetDbContext())
            {
                await dbContext.TransactionInboundQueue.Where(w => w.Id == ids[0])
                    .Set(s => s.AvailableDate, DateTime.UtcNow.AddSeconds(-1)).Set(s => s.Attempts, 2).UpdateAsync();
            }

            await Consumer((_, _, _) => Task.FromResult(false), attempts: 3).RunOnceAsync(CancellationToken.None);

            var dead = (await RowsAsync()).Single();
            (dead.StatusId, dead.Attempts, dead.LastError).Should()
                .Be(((short)TransactionInboundQueueStatus.Dead, 3, "NoResponse"));
        }

        [Fact]
        public async Task AnExceptionRecordsOnlyItsTypeNeverItsMessageAsync()
        {
            await EnqueueAsync(1);

            await Consumer((_, _, _) => throw new InvalidOperationException("secret connection text"))
                .RunOnceAsync(CancellationToken.None);

            var row = (await RowsAsync()).Single();
            row.LastError.Should().Be("Exception InvalidOperationException");
        }

        [Fact]
        public async Task AnUnknownOrUnstartedModelIsRetriedNotLostAsync()
        {
            await EnqueueAsync(1);

            await Consumer((_, _, _) => Task.FromResult(true), InboundModelState.NotStarted)
                .RunOnceAsync(CancellationToken.None);

            var row = (await RowsAsync()).Single();
            (row.StatusId, row.LastError).Should().Be(((short)TransactionInboundQueueStatus.Pending, "ModelNotStarted"));

            await using var dbContext = fx.GetDbContext();
            await dbContext.TransactionInboundQueue.Where(w => w.Id == row.Id)
                .Set(s => s.AvailableDate, DateTime.UtcNow.AddSeconds(-1)).UpdateAsync();

            await Consumer((_, _, _) => Task.FromResult(true), InboundModelState.Unknown)
                .RunOnceAsync(CancellationToken.None);

            (await RowsAsync()).Single().LastError.Should().Be("ModelNotFound");
        }

        [Fact]
        public async Task NothingIsClaimedUntilTheModelsHaveLoadedAsync()
        {
            await EnqueueAsync(2);

            var count = await Consumer((_, _, _) => Task.FromResult(true), loaded: false)
                .RunOnceAsync(CancellationToken.None);

            count.Should().Be(0);
            (await RowsAsync()).Should().HaveCount(2).And.OnlyContain(r => r.Attempts == 0);
        }

        [Fact]
        public async Task ARowClaimedByADeadNodeIsTakenAgainOnlyAfterItsClaimExpiresAsync()
        {
            var ids = await EnqueueAsync(2);

            await using (var dbContext = fx.GetDbContext())
            {
                await dbContext.TransactionInboundQueue.Where(w => w.Id == ids[0])
                    .Set(s => s.StatusId, (short)2).Set(s => s.LockedBy, "crashed")
                    .Set(s => s.LockedUntil, DateTime.UtcNow.AddMinutes(-1)).UpdateAsync();
                await dbContext.TransactionInboundQueue.Where(w => w.Id == ids[1])
                    .Set(s => s.StatusId, (short)2).Set(s => s.LockedBy, "busy")
                    .Set(s => s.LockedUntil, DateTime.UtcNow.AddMinutes(5)).UpdateAsync();
            }

            var seen = new ConcurrentBag<string>();
            await Consumer((_, bytes, _) =>
            {
                seen.Add(Encoding.UTF8.GetString(bytes));

                return Task.FromResult(true);
            }).RunOnceAsync(CancellationToken.None);

            seen.Should().ContainSingle().Which.Should().Contain("m0");
            (await RowsAsync()).Should().ContainSingle(r => r.Id == ids[1]);
        }

        [Fact]
        public async Task RowsAreClaimedOldestFirstAndInBatchesAsync()
        {
            await EnqueueAsync(6);
            var order = new ConcurrentQueue<string>();

            var consumer = Consumer((_, bytes, _) =>
            {
                order.Enqueue(Encoding.UTF8.GetString(bytes));

                return Task.FromResult(true);
            }, batch: 3, concurrency: 1);

            (await consumer.RunOnceAsync(CancellationToken.None)).Should().Be(3);
            order.Should().Equal("{\"n\":\"m0\"}", "{\"n\":\"m1\"}", "{\"n\":\"m2\"}");
            (await RowsAsync()).Should().HaveCount(3);
        }

        [Fact]
        public async Task CancellationReleasesWhatWasNotStartedSoNothingWaitsForTheClaimToExpireAsync()
        {
            await EnqueueAsync(4);
            using var source = new CancellationTokenSource();

            var consumer = Consumer(async (_, _, token) =>
            {
                source.Cancel();
                await Task.Delay(Timeout.Infinite, token);

                return true;
            }, concurrency: 1);

            await consumer.RunOnceAsync(source.Token);

            var rows = await RowsAsync();
            rows.Should().HaveCount(4).And.OnlyContain(r => r.StatusId == (short)TransactionInboundQueueStatus.Pending
                                                          && r.LockedBy == null && r.LastError == "Cancelled");
        }
    }
}
