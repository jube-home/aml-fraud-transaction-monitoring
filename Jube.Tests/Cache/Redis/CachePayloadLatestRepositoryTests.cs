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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis;
using Jube.Cache.Redis.Models;
using Jube.Cache.Redis.Serialization;
using Jube.Data.Poco;
using Jube.Dictionary;
using Jube.Extensions;
using Jube.TaskCancellation.TaskHelper;
using Jube.Test.Infrastructure;
using MessagePack;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CachePayloadLatestRepositoryTests
    {
        private static async Task<CachePayloadLatest> ReadPayloadLatestAsync(FakeHybridResilientRedisDatabase fake,
            string hashKey, string field)
        {
            var bytes = await fake.HashGetAsync(hashKey, field);
            bytes.HasValue.Should().BeTrue($"expected a value at hash {hashKey} field {field}");
            return MessagePackSerializer.Deserialize<CachePayloadLatest>((byte[])bytes!,
                MessagePackSerializerOptionsHelper
                    .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true));
        }

        [Fact]
        public async Task UpsertAsyncWithoutPayloadWritesPayloadLatestHashReferenceDateLatestHashAndSortedSetAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CachePayloadLatestRepository("unused", fake, TestLog.NoOp);
            var tenant = 7;
            var model = Guid.NewGuid();
            var instanceEntryGuid = Guid.NewGuid();
            var referenceDate = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
            const string entryKey = "AccountId";
            const string entryKeyValue = "123456";

            await repository.UpsertAsync(tenant, model, referenceDate, instanceEntryGuid, entryKey, entryKeyValue);

            var payloadLatestHashKey = $"PayloadLatest:{tenant}:{model:N}:{entryKey}";
            var cachePayloadLatest = await ReadPayloadLatestAsync(fake, payloadLatestHashKey, entryKeyValue);
            cachePayloadLatest.Key.Should().Be($"Payload:{tenant}:{model:N}");
            cachePayloadLatest.Field.Should().Be(instanceEntryGuid.ToString());
            cachePayloadLatest.ReferenceDate.Should().Be(referenceDate);
            cachePayloadLatest.ReclassificationCount.Should().Be(0);
            cachePayloadLatest.ReclassificationDate.Should().BeNull();

            var referenceDateLatestHashKey = $"ReferenceDateLatest:{tenant}:{model:N}";
            var timestamp = await fake.HashGetAsync(referenceDateLatestHashKey, entryKey);
            ((long)timestamp).Should().Be(referenceDate.ToUnixTimeMilliSeconds());

            var referenceDateLatestSortedSetKey = $"ReferenceDateLatest:{tenant}:{model:N}:{entryKey}";
            var entries = await fake.SortedSetRangeByScoreWithScoresAsync(referenceDateLatestSortedSetKey);
            entries.Should().ContainSingle(e => e.Element.ToString() == entryKeyValue &&
                                                (long)e.Score == referenceDate.ToUnixTimeMilliSeconds());
        }

        [Fact]
        public async Task UpsertAsyncWithPayloadUsesKeyIncludingInstanceEntryGuidAndLeavesFieldUnsetAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CachePayloadLatestRepository("unused", fake, TestLog.NoOp);
            var tenant = 3;
            var model = Guid.NewGuid();
            var instanceEntryGuid = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow;
            const string entryKey = "Iban";
            const string entryKeyValue = "GB33BUKB20201555555555";

            await repository.UpsertAsync(tenant, model, new DictionaryNoBoxing<int>(), referenceDate,
                instanceEntryGuid, entryKey, entryKeyValue);

            var payloadLatestHashKey = $"PayloadLatest:{tenant}:{model:N}:{entryKey}";
            var cachePayloadLatest = await ReadPayloadLatestAsync(fake, payloadLatestHashKey, entryKeyValue);
            cachePayloadLatest.Key.Should().Be($"Payload:{tenant}:{model:N}:{instanceEntryGuid:N}");
            cachePayloadLatest.Field.Should().BeNull();
        }

        [Fact]
        public async Task GetDistinctKeysPreferReplicaAsyncWithNoDateFilterReturnsAllFieldNamesAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CachePayloadLatestRepository("unused", fake, TestLog.NoOp);
            var tenant = 1;
            var model = Guid.NewGuid();
            const string key = "AccountId";
            var hashKey = $"PayloadLatest:{tenant}:{model:N}:{key}";

            await SeedAsync(fake, hashKey, "field-a", DateTime.UtcNow);
            await SeedAsync(fake, hashKey, "field-b", DateTime.UtcNow);

            var result = await repository.GetDistinctKeysPreferReplicaAsync(tenant, model, key);

            result.Should().BeEquivalentTo("field-a", "field-b");
        }

        [Fact]
        public async Task GetDistinctKeysPreferReplicaAsyncWithDateBeforeFiltersOutNewerEntriesAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CachePayloadLatestRepository("unused", fake, TestLog.NoOp);
            var tenant = 1;
            var model = Guid.NewGuid();
            const string key = "AccountId";
            var hashKey = $"PayloadLatest:{tenant}:{model:N}:{key}";
            var cutoff = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await SeedAsync(fake, hashKey, "old", cutoff.AddDays(-1));
            await SeedAsync(fake, hashKey, "new", cutoff.AddDays(1));

            var result = await repository.GetDistinctKeysPreferReplicaAsync(tenant, model, key, cutoff);

            result.Should().ContainSingle().Which.Should().Be("old");
        }

        [Fact]
        public async Task GetDistinctKeysPreferReplicaAsyncWithDateRangeFiltersToWindowAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CachePayloadLatestRepository("unused", fake, TestLog.NoOp);
            var tenant = 1;
            var model = Guid.NewGuid();
            const string key = "AccountId";
            var hashKey = $"PayloadLatest:{tenant}:{model:N}:{key}";
            var from = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            await SeedAsync(fake, hashKey, "before", from.AddDays(-1));
            await SeedAsync(fake, hashKey, "inside", from.AddDays(1));
            await SeedAsync(fake, hashKey, "after", to.AddDays(1));

            var result = await repository.GetDistinctKeysPreferReplicaAsync(tenant, model, key, from, to);

            result.Should().ContainSingle().Which.Should().Be("inside");
        }

        [Fact]
        public async Task UpsertAsyncWritesAKeyThatGetDistinctKeysPreferReplicaAsyncCanFindAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CachePayloadLatestRepository("unused", fake, TestLog.NoOp);
            var tenant = 9;
            var model = Guid.NewGuid();
            const string entryKey = "AccountId";
            const string entryKeyValue = "some-value";

            await repository.UpsertAsync(tenant, model, DateTime.UtcNow, Guid.NewGuid(), entryKey, entryKeyValue);

            var result = await repository.GetDistinctKeysPreferReplicaAsync(tenant, model, entryKey);

            result.Should().ContainSingle().Which.Should().Be(entryKeyValue);
        }

        [Fact]
        public async Task BatchSortedSetRemoveAsyncRemovesAllValuesAcrossMultipleThousandEntryBatchesAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            const string key = "SomeSortedSet";
            var values = Enumerable.Range(0, 1500).Select(i => (RedisValue)$"member-{i}").ToArray();

            foreach (var value in values)
            {
                await fake.SortedSetAddAsync(key, value, 1.0);
            }

            await InvokeBatchSortedSetRemoveAsync(fake, key, values);

            var remaining = await fake.SortedSetRangeByScoreWithScoresAsync(key);
            remaining.Should().BeEmpty();
        }

        [Fact]
        public async Task BatchHashDeleteAsyncDeletesAllFieldsAcrossMultipleThousandEntryBatchesAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            const string key = "SomeHash";
            var fields = Enumerable.Range(0, 1500).Select(i => (RedisValue)$"field-{i}").ToArray();

            foreach (var field in fields)
            {
                await fake.HashSetAsync(key, field, "value");
            }

            await InvokeBatchHashDeleteAsync(fake, key, fields);

            (await fake.HashLengthAsync(key)).Should().Be(0);
        }

        private static Task InvokeBatchSortedSetRemoveAsync(FakeHybridResilientRedisDatabase fake, RedisKey key,
            IEnumerable<RedisValue> values)
        {
            return CachePayloadLatestRepository.BatchSortedSetRemoveAsync(fake, key, values);
        }

        private static Task InvokeBatchHashDeleteAsync(FakeHybridResilientRedisDatabase fake, RedisKey key,
            IEnumerable<RedisValue> fields)
        {
            return CachePayloadLatestRepository.BatchHashDeleteAsync(fake, key, fields);
        }

        [Fact]
        public void AggregateResponseTimesForBulkInsertGroupsByTaskTypeAndSumsComputeTime()
        {
            var batch = new CachePayloadLatestRemovalBatch { Id = 42 };
            var tasks = new[]
            {
                new TimedTaskResult(
                    TaskType.SortedSetRemoveReferenceDateLatest, 100, 0),
                new TimedTaskResult(
                    TaskType.SortedSetRemoveReferenceDateLatest, 50, 0),
                new TimedTaskResult(
                    TaskType.HashDeletePayloadLatest, 30, 0)
            };

            var result = CachePayloadLatestRepository.AggregateResponseTimesForBulkInsert(tasks, batch);

            result.Should().HaveCount(2);
            result.Should().ContainSingle(r =>
                r.TaskTypeId == (int)TaskType.SortedSetRemoveReferenceDateLatest &&
                r.ResponseTime == 150 && r.CachePayloadLatestRemovalBatchId == 42);
            result.Should().ContainSingle(r =>
                r.TaskTypeId == (int)TaskType.HashDeletePayloadLatest &&
                r.ResponseTime == 30 && r.CachePayloadLatestRemovalBatchId == 42);
        }

        private static Task SeedAsync(FakeHybridResilientRedisDatabase fake, string hashKey, string field,
            DateTime updatedDate)
        {
            var cachePayloadLatest = new CachePayloadLatest { UpdatedDate = updatedDate };
            var bytes = MessagePackSerializer.Serialize(cachePayloadLatest,
                MessagePackSerializerOptionsHelper
                    .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true));
            return fake.HashSetAsync(hashKey, field, bytes);
        }
    }
}