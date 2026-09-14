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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis;
using Jube.Data.Poco;
using Jube.Dictionary;
using Jube.Extensions;
using Jube.ResilientRedisConnection;
using Jube.TaskCancellation.TaskHelper;
using Jube.Test.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CachePayloadRepositoryTests
    {
        private static readonly ConnectionMultiplexer inertMultiplexer =
            ConnectionMultiplexer.Connect("127.0.0.1:59999,abortConnect=false,connectTimeout=100,connectRetry=0");

        private static CachePayloadRepository CreateRepository(
            FakeHybridResilientRedisDatabase db,
            TestLog log,
            bool localCache = true,
            long localCacheBytes = 10_000_000,
            bool storePayloadCountsAndBytes = false,
            bool publishSubscribe = false,
            bool activationRuleIdempotency = false,
            string hostName = "test-host",
            string? localCacheInstanceGuidStringOverride = null)
        {
            return new CachePayloadRepository(
                inertMultiplexer,
                db,
                "unused-postgres-connection-string",
                log,
                false,
                localCache,
                localCacheBytes,
                false,
                storePayloadCountsAndBytes,
                publishSubscribe,
                TimeSpan.FromHours(1),
                activationRuleIdempotency,
                hostNameProvider: () => hostName,
                localCacheInstanceGuidStringOverride: localCacheInstanceGuidStringOverride);
        }

        private static DictionaryNoBoxing<int> SamplePayload(string value = "hello")
        {
            var payload = new DictionaryNoBoxing<int>();
            payload.Add(1, value);
            payload.Add(2, 42);
            return payload;
        }

        [Fact]
        public async Task InsertAsyncWritesPayloadHashAndReferenceDateSortedSetAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var log = new TestLog();
            var repository = CreateRepository(db, log);

            var tenantRegistryId = 7;
            var modelGuid = Guid.NewGuid();
            var entryGuid = Guid.NewGuid();
            var referenceDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await repository.InsertAsync(tenantRegistryId, modelGuid, SamplePayload(), referenceDate, entryGuid);

            var keyPayload = $"Payload:{tenantRegistryId}:{modelGuid:N}";
            var bytes = await db.HashGetAsync(keyPayload, $"{entryGuid:N}");
            ((byte[])bytes)!.Should().NotBeNull();

            var unpacked = repository.Unpack((byte[])bytes!);
            unpacked[1].AsString().Should().Be("hello");
            unpacked[2].AsInt().Should().Be(42);

            var keyReferenceDate = $"ReferenceDate:{tenantRegistryId}:{modelGuid:N}";
            var scores = await db.SortedSetRangeByRankWithScoresAsync(keyReferenceDate);
            scores.Should().ContainSingle(e => e.Element == $"{entryGuid:N}"
                                               && (long)e.Score == referenceDate.ToUnixTimeMilliSeconds());
        }

        [Fact]
        public async Task InsertAsyncWithPublishSubscribeDisabledPublishesNothingAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var repository = CreateRepository(db, new TestLog(), publishSubscribe: false);

            await repository.InsertAsync(1, Guid.NewGuid(), SamplePayload(), DateTime.UtcNow, Guid.NewGuid());

            db.PublishedMessages.Should().BeEmpty();
        }

        [Fact]
        public async Task InsertAsyncWithStorePayloadCountsAndBytesIncrementsCountersAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var repository = CreateRepository(db, new TestLog(), storePayloadCountsAndBytes: true);
            var tenantRegistryId = 3;
            var modelGuid = Guid.NewGuid();

            await repository.InsertAsync(tenantRegistryId, modelGuid, SamplePayload(), DateTime.UtcNow,
                Guid.NewGuid());

            var count = await db.HashGetAsync($"PayloadCount:{tenantRegistryId}", modelGuid.ToString("N"));
            var payloadBytes = await db.HashGetAsync($"PayloadBytes:{tenantRegistryId}", modelGuid.ToString("N"));
            ((long)count).Should().Be(1);
            ((long)payloadBytes).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpsertAsyncSwallowsExceptionsAndLogsAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var log = new TestLog();
            var repository = CreateRepository(db, log);
            db.ThrowOnMethod = nameof(IHybridResilientRedisDatabase.HashSetAsync);

            await repository.UpsertAsync(1, Guid.NewGuid(), SamplePayload(), DateTime.UtcNow, Guid.NewGuid());

            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task GetSortedSetKeysAsyncTouchesLruJournalAndExcludesGivenGuidAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var repository = CreateRepository(db, new TestLog());

            var tenantRegistryId = 1;
            var modelGuid = Guid.NewGuid();
            const string key = "AccountId";
            const string value = "12345";
            var redisKey = $"Journal:{tenantRegistryId}:{modelGuid:N}:{key}:{value}";

            var guidA = Guid.NewGuid();
            var guidB = Guid.NewGuid();
            var guidC = Guid.NewGuid();

            await db.SortedSetAddAsync(redisKey, guidA.ToString("N"), 1000);
            await db.SortedSetAddAsync(redisKey, guidB.ToString("N"), 2000);
            await db.SortedSetAddAsync(redisKey, guidC.ToString("N"), 3000);

            var result = await repository.GetSortedSetKeysAsync(tenantRegistryId, modelGuid, key, value, 10, guidB);

            result.Select(r => r.ToString()).Should().Equal(guidA.ToString("N"), guidC.ToString("N"));

            var lruJournalKey = $"LruJournal:{tenantRegistryId}:{modelGuid:N}";
            var lruEntries = await db.SortedSetRangeByRankWithScoresAsync(lruJournalKey);
            lruEntries.Should().ContainSingle(e => e.Element == redisKey);
        }

        [Fact]
        public async Task InsertPayloadJournalAndLedgerAsyncWritesExpectedKeysAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var repository = CreateRepository(db, new TestLog());

            var tenantRegistryId = 2;
            var modelGuid = Guid.NewGuid();
            var entryGuid = Guid.NewGuid();
            const string key = "AccountId";
            const string value = "999";
            var referenceDate = DateTime.UtcNow;

            await repository.InsertPayloadJournalAndLedgerAsync(tenantRegistryId, modelGuid, key, value,
                referenceDate, entryGuid);

            var redisKeyJournal = $"Journal:{tenantRegistryId}:{modelGuid:N}:{key}:{value}";
            var journalEntries = await db.SortedSetRangeByRankWithScoresAsync(redisKeyJournal);
            journalEntries.Should().ContainSingle(e => e.Element == $"{entryGuid:N}");

            var redisKeyPayloadJournal = $"PayloadJournal:{tenantRegistryId}:{modelGuid:N}:{entryGuid:N}";
            var members = await db.SetMembersAsync(redisKeyPayloadJournal);
            members.Should().ContainSingle(m => m == $"{key}:{value}");
        }

        [Fact]
        public async Task PurgeExpiredLruJournalEntriesAsyncRemovesOnlyEntriesOlderThanMaxAgeAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var repository = CreateRepository(db, new TestLog());

            var tenantRegistryId = 4;
            var modelGuid = Guid.NewGuid();
            var lruJournalKey = $"LruJournal:{tenantRegistryId}:{modelGuid:N}";

            var oldTimestamp = DateTime.UtcNow.AddHours(-3).ToUnixTimeMilliSeconds();
            var newTimestamp = DateTime.UtcNow.ToUnixTimeMilliSeconds();
            await db.SortedSetAddAsync(lruJournalKey, "old-entry", oldTimestamp);
            await db.SortedSetAddAsync(lruJournalKey, "new-entry", newTimestamp);

            var removed = await repository.PurgeExpiredLruJournalEntriesAsync(tenantRegistryId, modelGuid,
                TimeSpan.FromHours(1));

            removed.Should().Be(1);
            var remaining = await db.SortedSetRangeByRankWithScoresAsync(lruJournalKey);
            remaining.Should().ContainSingle(e => e.Element == "new-entry");
        }

        [Fact]
        public void AggregateResponseTimesForBulkInsertGroupsByTaskTypeAndSumsComputeTime()
        {
            var batch = new CachePayloadRemovalBatch { Id = 42 };
            var results = new[]
            {
                new TimedTaskResult(TaskType.SortedSetRemoveReferenceDate, 100, 10),
                new TimedTaskResult(TaskType.SortedSetRemoveReferenceDate, 200, 10),
                new TimedTaskResult(TaskType.SetRemoveAsync, 50, 5)
            };

            var aggregated = CachePayloadRepository.AggregateResponseTimesForBulkInsert(results, batch);

            aggregated.Should().HaveCount(2);
            aggregated.Single(a => a.TaskTypeId == (int)TaskType.SortedSetRemoveReferenceDate).ResponseTime
                .Should().Be(300);
            aggregated.Single(a => a.TaskTypeId == (int)TaskType.SetRemoveAsync).ResponseTime.Should().Be(50);
            aggregated.Should().OnlyContain(a => a.CachePayloadRemovalBatchId == 42);
        }

        [Fact]
        public async Task BatchSortedSetRemoveAsyncRemovesAllValuesAcrossMultipleBatchesAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var repository = CreateRepository(db, new TestLog());
            const string key = "SomeSortedSet";

            var values = Enumerable.Range(0, 1500).Select(i => (RedisValue)$"member-{i}").ToArray();
            foreach (var value in values)
            {
                await db.SortedSetAddAsync(key, value, 1);
            }

            await repository.BatchSortedSetRemoveAsync(db, key, values);

            var length = await db.SortedSetLengthAsync(key);
            length.Should().Be(0);
        }

        [Fact]
        public async Task CleanupIdempotencyAsyncRemovesJournalEntryOnceUnderlyingSetIsEmptyAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var repository = CreateRepository(db, new TestLog());

            var tenantRegistryId = 5;
            var modelGuid = Guid.NewGuid();
            var entryGuid = Guid.NewGuid();
            var idempotencySetKey = $"ActivationCaseIdempotency:{tenantRegistryId}:{modelGuid:N}:{Guid.NewGuid():N}";
            var idempotencyJournalKey = $"IdempotencyJournal:{tenantRegistryId}:{modelGuid:N}";

            await db.SetAddAsync(idempotencyJournalKey, idempotencySetKey);
            await db.SetAddAsync(idempotencySetKey, entryGuid.ToString("N"));

            await repository.CleanupIdempotencyAsync(tenantRegistryId, modelGuid,
                [new RedisValue(entryGuid.ToString("N"))]);

            (await db.KeyExistsAsync(idempotencySetKey)).Should().BeFalse();
            var journalMembers = await db.SetMembersAsync(idempotencyJournalKey);
            journalMembers.Should().BeEmpty();
        }

        [Fact]
        public async Task CleanupIdempotencyAsyncKeepsJournalEntryWhenUnderlyingSetStillHasMembersAsync()
        {
            var db = new FakeHybridResilientRedisDatabase();
            var repository = CreateRepository(db, new TestLog());

            var tenantRegistryId = 6;
            var modelGuid = Guid.NewGuid();
            var entryGuidToRemove = Guid.NewGuid();
            var entryGuidToKeep = Guid.NewGuid();
            var idempotencySetKey = $"ActivationCaseIdempotency:{tenantRegistryId}:{modelGuid:N}:{Guid.NewGuid():N}";
            var idempotencyJournalKey = $"IdempotencyJournal:{tenantRegistryId}:{modelGuid:N}";

            await db.SetAddAsync(idempotencyJournalKey, idempotencySetKey);
            await db.SetAddAsync(idempotencySetKey, entryGuidToRemove.ToString("N"));
            await db.SetAddAsync(idempotencySetKey, entryGuidToKeep.ToString("N"));

            await repository.CleanupIdempotencyAsync(tenantRegistryId, modelGuid,
                [new RedisValue(entryGuidToRemove.ToString("N"))]);

            (await db.KeyExistsAsync(idempotencySetKey)).Should().BeTrue();
            var journalMembers = await db.SetMembersAsync(idempotencyJournalKey);
            journalMembers.Should().ContainSingle(m => m == idempotencySetKey);
        }
    }
}