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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Dictionary.Interfaces;
using Xunit;

namespace Jube.Test.Dictionary
{
    [Trait("Category", "Unit")]
    public sealed class LruCacheConcurrentSizedDictionaryTests
    {
        private static LruCacheConcurrentSizedDictionary<string, string> NewCache(
            long maxSizeBytes = 3L * 1024 * 1024 * 1024, double evictionThresholdRatio = 0.9)
        {
            return new LruCacheConcurrentSizedDictionary<string, string>(v => v.Length, maxSizeBytes,
                evictionThresholdRatio);
        }

        [Fact]
        public void TheConstructorThrowsWhenGivenNoSizeEstimator()
        {
            var act = () => new LruCacheConcurrentSizedDictionary<string, string>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ANewCacheIsEmptyAndNotFull()
        {
            var cache = NewCache();

            cache.Count.Should().Be(0);
            cache.TotalSize.Should().Be(0);
            cache.IsFull.Should().BeFalse();
            cache.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void AddingAnItemMakesItRetrievableAndCountedInTotalSize()
        {
            var cache = NewCache();

            cache.Add("key", "value");

            cache.Count.Should().Be(1);
            cache["key"].Should().Be("value");
            cache.TotalSize.Should().Be("value".Length);
            cache.ContainsKey("key").Should().BeTrue();
        }

        [Fact]
        public void TheIndexerSetterIsEquivalentToAdd()
        {
            var cache = NewCache();

            cache["key"] = "value";

            cache["key"].Should().Be("value");
        }

        [Fact]
        public void TheIndexerGetterThrowsForAMissingKey()
        {
            var cache = NewCache();

            var act = () => cache["missing"];

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void AddViaKeyValuePairDelegatesToAddOrUpdate()
        {
            var cache = NewCache();

            cache.Add(new KeyValuePair<string, string>("key", "value"));

            cache["key"].Should().Be("value");
        }

        [Fact]
        public void UpdatingAnExistingKeyReplacesItsValueWithoutChangingTheCount()
        {
            var cache = NewCache();
            cache.Add("key", "short");

            cache.Add("key", "a longer value");

            cache.Count.Should().Be(1);
            cache["key"].Should().Be("a longer value");
            cache.TotalSize.Should().Be("a longer value".Length);
        }

        [Fact]
        public void AddingANewKeyIncrementsTheAddCounterNotTheUpdateCounter()
        {
            var cache = NewCache();

            cache.Add("key", "value");

            cache.AddCounter.Should().Be(1);
            cache.UpdateCount.Should().Be(0);
            cache.RequestCount.Should().Be(1);
            cache.AddBytes.Should().Be("value".Length);
        }

        [Fact]
        public void UpdatingAnExistingKeyIncrementsTheUpdateCounterNotTheAddCounter()
        {
            var cache = NewCache();
            cache.Add("key", "value");

            cache.Add("key", "value2");

            cache.UpdateCount.Should().Be(1);
            cache.AddCounter.Should().Be(1, "the add counter should not move on a pure update");
            cache.RequestCount.Should().Be(2);
        }

        [Fact]
        public void RemoveDeletesTheEntryAndUpdatesCountersAndTotalSize()
        {
            var cache = NewCache();
            cache.Add("key", "value");

            var removed = cache.Remove("key");

            removed.Should().BeTrue();
            cache.Count.Should().Be(0);
            cache.TotalSize.Should().Be(0);
            cache.RemoveCount.Should().Be(1);
            cache.RemoveBytes.Should().Be("value".Length);
        }

        [Fact]
        public void RemoveOfAMissingKeyReturnsFalseAndDoesNotAffectCounters()
        {
            var cache = NewCache();

            var removed = cache.Remove("missing");

            removed.Should().BeFalse();
            cache.RemoveCount.Should().Be(0);
        }

        [Fact]
        public void RemoveByKeyValuePairOnlySucceedsWhenTheValueMatchesToo()
        {
            var cache = NewCache();
            cache.Add("key", "value");

            var wrongValueRemoved = cache.Remove(new KeyValuePair<string, string>("key", "different"));
            var rightValueRemoved = cache.Remove(new KeyValuePair<string, string>("key", "value"));

            wrongValueRemoved.Should().BeFalse();
            rightValueRemoved.Should().BeTrue();
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void TryGetValueReturnsTrueAndTheValueWhenPresent()
        {
            var cache = NewCache();
            cache.Add("key", "value");

            var found = cache.TryGetValue("key", out var value);

            found.Should().BeTrue();
            value.Should().Be("value");
        }

        [Fact]
        public void TryGetValueReturnsFalseAndDefaultWhenAbsent()
        {
            var cache = NewCache();

            var found = cache.TryGetValue("missing", out var value);

            found.Should().BeFalse();
            value.Should().BeNull();
        }

        [Fact]
        public void ContainsChecksBothKeyAndValueEquality()
        {
            var cache = NewCache();
            cache.Add("key", "value");

            cache.Contains(new KeyValuePair<string, string>("key", "value")).Should().BeTrue();
            cache.Contains(new KeyValuePair<string, string>("key", "other")).Should().BeFalse();
            cache.Contains(new KeyValuePair<string, string>("missing", "value")).Should().BeFalse();
        }

        [Fact]
        public void ClearRemovesEveryEntryAndResetsTotalSizeButNotTheCounters()
        {
            var cache = NewCache();
            cache.Add("a", "1");
            cache.Add("b", "2");

            cache.Clear();

            cache.Count.Should().Be(0);
            cache.TotalSize.Should().Be(0);
            cache.AddCounter.Should().Be(2, "Clear resets state, not historical counters");
        }

        [Fact]
        public void KeysAndValuesExposeSnapshotsOfTheCurrentContents()
        {
            var cache = NewCache();
            cache.Add("a", "1");
            cache.Add("b", "2");

            cache.Keys.Should().BeEquivalentTo("a", "b");
            cache.Values.Should().BeEquivalentTo("1", "2");
        }

        [Fact]
        public void CopyToFillsAnArrayStartingAtTheGivenIndex()
        {
            var cache = NewCache();
            cache.Add("a", "1");
            var array = new KeyValuePair<string, string>[3];

            cache.CopyTo(array, 1);

            array[0].Should().Be(default(KeyValuePair<string, string>));
            array[1].Key.Should().Be("a");
        }

        [Fact]
        public void TheGenericEnumeratorYieldsEveryEntry()
        {
            var cache = NewCache();
            cache.Add("a", "1");
            cache.Add("b", "2");

            cache.Select(kv => kv.Key).Should().BeEquivalentTo("a", "b");
        }

        [Fact]
        public void TheNonGenericEnumeratorIsAlsoUsable()
        {
            var cache = NewCache();
            cache.Add("a", "1");

            IEnumerable nonGeneric = cache;
            var count = 0;
            foreach (var _ in nonGeneric)
            {
                count++;
            }

            count.Should().Be(1);
        }

        [Fact]
        public void EstimatedSizeBytesIsAnAliasForTotalSize()
        {
            var cache = NewCache();
            cache.Add("key", "value");

            ((ISized)cache).EstimatedSizeBytes().Should().Be(cache.TotalSize);
        }

        [Fact]
        public void ResetCountersZeroesOutAllCountersButLeavesTheDataIntact()
        {
            var cache = NewCache();
            cache.Add("key", "value");
            cache.Remove("key");

            cache.ResetCounters();

            cache.AddCounter.Should().Be(0);
            cache.RemoveCount.Should().Be(0);
            cache.UpdateCount.Should().Be(0);
            cache.RequestCount.Should().Be(0);
            cache.EvictionCount.Should().Be(0);
        }

        [Fact]
        public void IsFullBecomesTrueOnceTheTotalSizeExceedsTheConfiguredMaximumWhenEvictionCannotKeepUp()
        {
            var cache = new LruCacheConcurrentSizedDictionary<string, string>(v => v.Length, 5,
                10) { { "key", "value" } };

            cache.IsFull.Should().BeFalse();

            cache.Add("key2", "value2");

            cache.IsFull.Should().BeTrue();
        }

        [Fact]
        public void ExceedingTheMaxSizeEvictsEntriesDownToAtOrBelowTheEvictionThreshold()
        {
            var cache = new LruCacheConcurrentSizedDictionary<string, string>(v => v.Length, 10,
                0.5);

            for (var i = 0; i < 5; i++)
            {
                cache.Add($"key{i}", "12345");
            }

            cache.TotalSize.Should().BeLessThanOrEqualTo(5);
            cache.EvictionCount.Should().BeGreaterThan(0);
            cache.ContainsKey("key4").Should().BeTrue("the most recently added entry should survive eviction");
        }

        [Fact]
        public void AnEntryThatHasBeenAccessedIsGivenOneGraceRoundBeforeItCanBeEvicted()
        {
            var cache = new LruCacheConcurrentSizedDictionary<string, string>(v => v.Length, 10,
                0.5) { { "keep", "12345" } };
            _ = cache["keep"];

            cache.Add("filler0", "12345");
            cache.Add("filler1", "12345");

            cache.ContainsKey("keep").Should()
                .BeTrue("an accessed entry is skipped once by the eviction sweep before it can be evicted");
            cache.ContainsKey("filler0").Should().BeFalse();
            cache.ContainsKey("filler1").Should().BeFalse();
        }

        [Fact]
        public void RemovingAnEntryCanTriggerEvictionWhenTheCacheIsStillOverThreshold()
        {
            var cache = new LruCacheConcurrentSizedDictionary<string, string>(v => v.Length, 10)
            {
                { "a", "12345" },
                { "b", "12345" },
                { "c", "12345" }
            };

            cache.Remove("c");

            cache.TotalSize.Should().BeLessThanOrEqualTo(10);
        }

        [Fact]
        public async Task ConcurrentAddsFromManyThreadsAllSucceedWithoutLosingOrCorruptingEntriesAsync()
        {
            var cache = NewCache();
            const int perThread = 200;
            const int threads = 8;

            await Task.WhenAll(Enumerable.Range(0, threads).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < perThread; i++)
                {
                    cache.Add($"t{t}-{i}", "v");
                }
            })));

            cache.Count.Should().Be(threads * perThread);
            cache.AddCounter.Should().Be(threads * perThread);
        }
    }
}