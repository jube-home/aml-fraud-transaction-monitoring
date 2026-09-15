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
using FluentAssertions;
using Jube.Cache.Redis.Serialization;
using Jube.Dictionary;
using MessagePack;
using Xunit;
using CachePayloadLatest = Jube.Cache.Redis.Models.CachePayloadLatest;
using CacheSanction = Jube.Cache.Redis.Models.CacheSanction;
using EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue =
    Jube.Cache.Redis.Models.EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue;
using ExpiredTtlCounterEntry = Jube.Cache.Redis.Models.ExpiredTtlCounterEntry;
using LocalCacheInstanceKey = Jube.Cache.Redis.Models.LocalCacheInstanceKey;
using Sanction = Jube.Cache.Redis.Models.Sanction;

namespace Jube.Test.Cache.Redis.Models
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class ModelsTests
    {
        private static readonly MessagePackSerializerOptions contractlessOptions = MessagePackSerializerOptionsHelper
            .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true);

        [Fact]
        public void CachePayloadLatestHoldsAssignedFields()
        {
            var now = DateTime.UtcNow;
            var model = new CachePayloadLatest
            {
                Key = "Payload:1:abc",
                Field = "field-1",
                UpdatedDate = now,
                ReclassificationDate = now.AddDays(1),
                ReferenceDate = now.AddHours(-1),
                ReclassificationCount = 3
            };

            model.Key.Should().Be("Payload:1:abc");
            model.Field.Should().Be("field-1");
            model.UpdatedDate.Should().Be(now);
            model.ReclassificationDate.Should().Be(now.AddDays(1));
            model.ReferenceDate.Should().Be(now.AddHours(-1));
            model.ReclassificationCount.Should().Be(3);
        }

        [Fact]
        public void CachePayloadLatestRoundTripsThroughContractlessMessagePack()
        {
            var original = new CachePayloadLatest
            {
                Key = "Payload:1:abc",
                Field = "field-1",
                UpdatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ReclassificationDate = null,
                ReferenceDate = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                ReclassificationCount = 0
            };

            var bytes = MessagePackSerializer.Serialize(original, contractlessOptions);
            var deserialized = MessagePackSerializer.Deserialize<CachePayloadLatest>(bytes, contractlessOptions);

            deserialized.Key.Should().Be(original.Key);
            deserialized.Field.Should().Be(original.Field);
            deserialized.UpdatedDate.Should().Be(original.UpdatedDate);
            deserialized.ReclassificationDate.Should().BeNull();
            deserialized.ReferenceDate.Should().Be(original.ReferenceDate);
        }

        [Fact]
        public void CacheSanctionHoldsAssignedFieldsAndRoundTripsThroughContractlessMessagePack()
        {
            var original = new CacheSanction
                { CreatedDate = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc), Value = 2.5 };

            var bytes = MessagePackSerializer.Serialize(original, contractlessOptions);
            var deserialized = MessagePackSerializer.Deserialize<CacheSanction>(bytes, contractlessOptions);

            deserialized.CreatedDate.Should().Be(original.CreatedDate);
            deserialized.Value.Should().Be(2.5);
        }

        [Fact]
        public void CacheSanctionWithNullValueRoundTrips()
        {
            var original = new CacheSanction { CreatedDate = DateTime.UtcNow, Value = null };

            var bytes = MessagePackSerializer.Serialize(original, contractlessOptions);
            var deserialized = MessagePackSerializer.Deserialize<CacheSanction>(bytes, contractlessOptions);

            deserialized.Value.Should().BeNull();
        }

        [Fact]
        public void SanctionHoldsAssignedFieldsAndRoundTripsThroughContractlessMessagePack()
        {
            var original = new Sanction
                { CreatedDate = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc), Value = 1.75 };

            var bytes = MessagePackSerializer.Serialize(original, contractlessOptions);
            var deserialized = MessagePackSerializer.Deserialize<Sanction>(bytes, contractlessOptions);

            deserialized.CreatedDate.Should().Be(original.CreatedDate);
            deserialized.Value.Should().Be(1.75);
        }

        [Fact]
        public void EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueHoldsAssignedFields()
        {
            var model = new EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue
            {
                AbstractionRuleName = "rule-1",
                SearchKey = "AccountId",
                SearchValue = "12345"
            };

            model.AbstractionRuleName.Should().Be("rule-1");
            model.SearchKey.Should().Be("AccountId");
            model.SearchValue.Should().Be("12345");
        }

        [Fact]
        public void ExpiredTtlCounterEntryHoldsAssignedFields()
        {
            var referenceDate = DateTime.UtcNow;
            var entry = new ExpiredTtlCounterEntry
            {
                ReferenceDate = referenceDate,
                DataName = "AccountId",
                DataValue = "12345",
                Value = 3.0
            };

            entry.ReferenceDate.Should().Be(referenceDate);
            entry.DataName.Should().Be("AccountId");
            entry.DataValue.Should().Be("12345");
            entry.Value.Should().Be(3.0);
        }

        [Fact]
        public void LocalCacheInstanceKeyStartsAtZeroForAllCountersAndExposesTheGivenLruCache()
        {
            var lru = new LruCacheConcurrentSizedDictionary<string, byte[]>(b => b.Length, 1024);

            var key = new LocalCacheInstanceKey(lru);

            key.LruCacheConcurrentSizedDictionary.Should().BeSameAs(lru);
            key.Requests.Should().Be(0);
            key.Misses.Should().Be(0);
            key.DualMiss.Should().Be(0);
            key.HashRemove.Should().Be(0);
            key.HashRemoveMiss.Should().Be(0);
            key.HashRemoveSubscription.Should().Be(0);
            key.HashRemoveSubscriptionMiss.Should().Be(0);
            key.HashSetSubscriptions.Should().Be(0);
            key.MissRemoteResponseTime.Should().Be(0);
            key.UnpackResponseTime.Should().Be(0);
        }
    }
}