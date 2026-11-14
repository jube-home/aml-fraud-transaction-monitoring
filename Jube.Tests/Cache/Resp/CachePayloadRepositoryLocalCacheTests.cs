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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Resp;
using Jube.Cache.Resp.Serialization;
using Jube.Cache.Resp.Serialization.DictionaryNoBoxing.MessagePack.Models;
using Jube.Dictionary;
using Jube.Test.Infrastructure;
using MessagePack;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache.Resp
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CachePayloadRepositoryLocalCacheTests
    {
        private static readonly ConnectionMultiplexer inertMultiplexer =
            ConnectionMultiplexer.Connect("127.0.0.1:59999,abortConnect=false,connectTimeout=100,connectRetry=0");

        private static CachePayloadRepository CreateRepository(
            FakeHybridResilientRespDatabase db,
            TestLog log,
            bool localCache = true,
            string hostName = "this-host",
            string localCacheInstanceGuidStringOverride = "this-instance-guid")
        {
            return new CachePayloadRepository(
                inertMultiplexer,
                db,
                "unused-postgres-connection-string",
                log,
                false,
                localCache,
                10_000_000,
                false,
                false,
                false,
                TimeSpan.FromHours(1),
                false,
                hostNameProvider: () => hostName,
                localCacheInstanceGuidStringOverride: localCacheInstanceGuidStringOverride);
        }

        private static byte[] SerializeEnvelope(DictionaryNoBoxing<int> payload)
        {
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = payload };

            return MessagePackSerializer.Serialize(envelope,
                MessagePackSerializerOptionsHelper.EnveloperMessagePackSerializerWithCompressionOptions(false));
        }

        [Fact]
        public void CheckThatTheEventDidNotComeFromThisHostReturnsTrueOnlyWhenBothHostAndGuidMatch()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog(),
                hostName: "host-a", localCacheInstanceGuidStringOverride: "guid-a");

            repository.CheckThatTheEventDidNotComeFromThisHost("guid-a", "host-a").Should().BeTrue();
            repository.CheckThatTheEventDidNotComeFromThisHost("guid-a", "host-b").Should().BeFalse();
            repository.CheckThatTheEventDidNotComeFromThisHost("guid-b", "host-a").Should().BeFalse();
        }

        [Fact]
        public void HandleHashSetMessageIgnoresMessageThatOriginatedFromThisInstance()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog(),
                hostName: "host-a", localCacheInstanceGuidStringOverride: "guid-a");

            var keyPayload = "Payload:1:abc";
            var hSetKey = "entry-1";
            var channel = RedisChannel.Pattern($"HashSet:host-a:guid-a:{keyPayload}:{hSetKey}");

            repository.HandleHashSetMessage(channel, new byte[] { 1, 2, 3 });

            var localCacheForPayloadKey = repository.GetLocalCacheEntry(keyPayload);

            localCacheForPayloadKey.HashSetSubscriptions.Should().Be(0,
                "a message whose embedded host/instance-guid match this instance should be filtered out");

            localCacheForPayloadKey.LruCacheConcurrentSizedDictionary.TryGetValue(hSetKey, out _).Should().BeFalse();
        }

        [Fact]
        public void HandleHashSetMessageFromAnotherInstanceAddsToLocalCache()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog(),
                hostName: "host-a", localCacheInstanceGuidStringOverride: "guid-a");

            var keyPayload = "Payload:1:abc";
            var hSetKey = "entry-1";
            var channel = RedisChannel.Pattern($"HashSet:host-b:guid-b:{keyPayload}:{hSetKey}");
            var value = new byte[] { 9, 9, 9 };

            repository.HandleHashSetMessage(channel, value);

            var localCacheForPayloadKey = repository.GetLocalCacheEntry(keyPayload);
            localCacheForPayloadKey.HashSetSubscriptions.Should().Be(1);

            localCacheForPayloadKey.LruCacheConcurrentSizedDictionary.TryGetValue(hSetKey, out var cached)
                .Should().BeTrue();

            cached.Should().Equal(value);
        }

        [Fact]
        public void HandleHashRemoveMessageIgnoresMessageThatOriginatedFromThisInstance()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog(),
                hostName: "host-a", localCacheInstanceGuidStringOverride: "guid-a");

            var keyPayload = "Payload:1:abc";
            var hSetKey = "entry-1";

            var entry = repository.GetLocalCacheEntry(keyPayload);
            repository.AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(entry, hSetKey, [1, 2, 3]);

            var channel = RedisChannel.Pattern($"HashRemove:host-a:guid-a:{keyPayload}");
            repository.HandleHashRemoveMessage(channel, hSetKey);

            entry.HashRemoveSubscription.Should().Be(0);

            entry.LruCacheConcurrentSizedDictionary.TryGetValue(hSetKey, out _).Should().BeTrue(
                "the message should have been ignored as self-originated, leaving the entry in place");
        }

        [Fact]
        public void HandleHashRemoveMessageFromAnotherInstanceRemovesFromLocalCache()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog(),
                hostName: "host-a", localCacheInstanceGuidStringOverride: "guid-a");

            var keyPayload = "Payload:1:abc";
            var hSetKey = "entry-1";

            var entry = repository.GetLocalCacheEntry(keyPayload);
            repository.AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(entry, hSetKey, [1, 2, 3]);

            var channel = RedisChannel.Pattern($"HashRemove:host-b:guid-b:{keyPayload}");
            repository.HandleHashRemoveMessage(channel, hSetKey);

            entry.LruCacheConcurrentSizedDictionary.TryGetValue(hSetKey, out _).Should().BeFalse();
        }

        [Fact]
        public void DeleteFromLocalCacheSubscriptionBranchCountsOnceOnAHitAndDoesNotIncrementMiss()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog());
            var entry = repository.GetLocalCacheEntry("Payload:1:abc");
            repository.AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(entry, "k", [1]);

            repository.DeleteFromLocalCache(entry, "k", true);

            entry.HashRemoveSubscription.Should().Be(1);
            entry.HashRemoveSubscriptionMiss.Should().Be(0);
        }

        [Fact]
        public void DeleteFromLocalCacheSubscriptionBranchCountsOnceAndIncrementsMissOnAMiss()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog());
            var entry = repository.GetLocalCacheEntry("Payload:1:abc");

            repository.DeleteFromLocalCache(entry, "not-present", true);

            entry.HashRemoveSubscription.Should().Be(1);
            entry.HashRemoveSubscriptionMiss.Should().Be(1);
        }

        [Fact]
        public void DeleteFromLocalCacheNonSubscriptionBranchDoesNotIncrementHashRemoveMissOnAHit()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog());
            var entry = repository.GetLocalCacheEntry("Payload:1:abc");
            repository.AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(entry, "k", [1]);

            repository.DeleteFromLocalCache(entry, "k", false);

            entry.HashRemove.Should().Be(1);
            entry.HashRemoveMiss.Should().Be(0);
        }

        [Fact]
        public void DeleteFromLocalCacheNonSubscriptionBranchIncrementsHashRemoveMissOnAGenuineMiss()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog());
            var entry = repository.GetLocalCacheEntry("Payload:1:abc");

            repository.DeleteFromLocalCache(entry, "not-present", false);

            entry.HashRemove.Should().Be(1);
            entry.HashRemoveMiss.Should().Be(1);
        }

        [Fact]
        public void DeleteFromLocalCacheDoesNothingWhenLocalCacheIsDisabled()
        {
            var repository = CreateRepository(new FakeHybridResilientRespDatabase(), new TestLog(),
                false);

            var entry = repository.GetLocalCacheEntry("Payload:1:abc");

            repository.DeleteFromLocalCache(entry, "k", false);

            entry.HashRemove.Should().Be(0);
        }

        [Fact]
        public async Task GetPayloadBatchAsyncPrefersLocalCacheAndFallsBackToRespOnMissAsync()
        {
            var db = new FakeHybridResilientRespDatabase();
            var log = new TestLog();
            var repository = CreateRepository(db, log);

            var tenantRegistryId = 1;
            var modelGuid = Guid.NewGuid();
            var keyPayload = $"Payload:{tenantRegistryId}:{modelGuid:N}";
            var hitGuid = Guid.NewGuid();
            var missGuid = Guid.NewGuid();
            var trueMissGuid = Guid.NewGuid();

            var hitPayload = new DictionaryNoBoxing<int>();
            hitPayload.Add(1, "from-local-cache");
            var hitBytes = SerializeEnvelope(hitPayload);

            var entry = repository.GetLocalCacheEntry(keyPayload);

            repository.AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(entry, $"{hitGuid:N}",
                hitBytes);

            var respOnlyPayload = new DictionaryNoBoxing<int>();
            respOnlyPayload.Add(1, "from-resp");
            await db.HashSetAsync(keyPayload, $"{missGuid:N}", SerializeEnvelope(respOnlyPayload));

            var keys = new RedisValue[] { $"{hitGuid:N}", $"{missGuid:N}", $"{trueMissGuid:N}" };

            var result = await repository.GetPayloadBatchAsync(tenantRegistryId, modelGuid, keys, Guid.NewGuid());

            result.Should().ContainKey($"{hitGuid:N}");
            result[$"{hitGuid:N}"][1].AsString().Should().Be("from-local-cache");

            result.Should().ContainKey($"{missGuid:N}");
            result[$"{missGuid:N}"][1].AsString().Should().Be("from-resp");

            result.Should().NotContainKey($"{trueMissGuid:N}");

            entry.Requests.Should().Be(3);
            entry.Misses.Should().Be(2, "hitGuid was a local-cache hit, the other two were misses that went to RESP");
            entry.DualMiss.Should().Be(1, "trueMissGuid was missing from both the local cache and RESP");
        }

        [Fact]
        public async Task GetPayloadBatchAsyncAlwaysGoesToRespWhenLocalCacheDisabledAsync()
        {
            var db = new FakeHybridResilientRespDatabase();
            var repository = CreateRepository(db, new TestLog(), false);

            var tenantRegistryId = 1;
            var modelGuid = Guid.NewGuid();
            var entryGuid = Guid.NewGuid();
            var payload = new DictionaryNoBoxing<int>();
            payload.Add(1, "value");
            var keyPayload = $"Payload:{tenantRegistryId}:{modelGuid:N}";
            await db.HashSetAsync(keyPayload, $"{entryGuid:N}", SerializeEnvelope(payload));

            var entry = repository.GetLocalCacheEntry(keyPayload);

            var result = await repository.GetPayloadBatchAsync(tenantRegistryId, modelGuid,
                [new RedisValue($"{entryGuid:N}")], Guid.NewGuid());

            result[$"{entryGuid:N}"][1].AsString().Should().Be("value");

            entry.Misses.Should().Be(1,
                "localCache:false means every request counts as a miss, even though a value happened to be stashed in the (unused) local cache dictionary");
        }
    }
}