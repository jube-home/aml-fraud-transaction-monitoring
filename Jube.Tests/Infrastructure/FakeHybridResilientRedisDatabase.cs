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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jube.ResilientRedisConnection;
using StackExchange.Redis;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Test.Infrastructure
{
    public sealed class FakeHybridResilientRedisDatabase : IHybridResilientRedisDatabase
    {
        private readonly ConcurrentDictionary<RedisKey, ConcurrentDictionary<RedisValue, RedisValue>> hashes = new();
        private readonly ConcurrentDictionary<RedisKey, ConcurrentDictionary<RedisValue, byte>> sets = new();
        private readonly ConcurrentDictionary<RedisKey, ConcurrentDictionary<RedisValue, double>> sortedSets = new();
        private readonly ConcurrentDictionary<RedisKey, RedisValue> strings = new();

        public ConcurrentQueue<(RedisChannel Channel, RedisValue Message)> PublishedMessages { get; } = new();
        public string? ThrowOnMethod { get; set; }
        public Exception? ThrowOnMethodException { get; set; }

        public bool KeyExists(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
#pragma warning disable VSTHRD002
            return KeyExistsAsync(key, flags).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var exists = (hashes.TryGetValue(key, out var h) && !h.IsEmpty)
                         || (sets.TryGetValue(key, out var s) && !s.IsEmpty)
                         || (sortedSets.TryGetValue(key, out var z) && !z.IsEmpty)
                         || strings.ContainsKey(key);
            return Task.FromResult(exists);
        }

        public bool KeyRename(RedisKey key, RedisKey newKey, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();

            if (hashes.TryRemove(key, out var h))
            {
                hashes[newKey] = h;
                return true;
            }

            if (sets.TryRemove(key, out var s))
            {
                sets[newKey] = s;
                return true;
            }

            if (sortedSets.TryRemove(key, out var z))
            {
                sortedSets[newKey] = z;
                return true;
            }

            if (strings.TryRemove(key, out var v))
            {
                strings[newKey] = v;
                return true;
            }

            return false;
        }

        public Task<bool> HashSetAsync(RedisKey key, RedisValue field, RedisValue value, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var hash = hashes.GetOrAdd(key, _ => new ConcurrentDictionary<RedisValue, RedisValue>());
            var existed = hash.ContainsKey(field);

            switch (when)
            {
                case When.NotExists when existed:
                    return Task.FromResult(false);
                case When.Exists when !existed:
                    return Task.FromResult(false);
                default:
                    hash[field] = value;
                    return Task.FromResult(!existed);
            }
        }

        public void HashSet(RedisKey key, RedisValue field, RedisValue value, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
#pragma warning disable VSTHRD002
            HashSetAsync(key, field, value, when, flags).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        public void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var hash = hashes.GetOrAdd(key, _ => new ConcurrentDictionary<RedisValue, RedisValue>());
            foreach (var entry in hashFields)
            {
                hash[entry.Name] = entry.Value;
            }
        }

        public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (hashes.TryGetValue(key, out var hash) && hash.TryGetValue(field, out var value))
            {
                return Task.FromResult(value);
            }

            return Task.FromResult(RedisValue.Null);
        }

        public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] fields,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            hashes.TryGetValue(key, out var hash);
            var result = fields.Select(f => hash != null && hash.TryGetValue(f, out var v) ? v : RedisValue.Null)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task<bool> HashDeleteAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var removed = hashes.TryGetValue(key, out var hash) && hash.TryRemove(field, out _);
            return Task.FromResult(removed);
        }

        public bool HashDelete(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
#pragma warning disable VSTHRD002
            return HashDeleteAsync(key, field, flags).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] fields, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (!hashes.TryGetValue(key, out var hash))
            {
                return Task.FromResult(0L);
            }

            var count = fields.Count(f => hash.TryRemove(f, out _));
            return Task.FromResult((long)count);
        }

        public Task<long> HashIncrementAsync(RedisKey key, RedisValue field, long value,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var hash = hashes.GetOrAdd(key, _ => new ConcurrentDictionary<RedisValue, RedisValue>());
            var current = hash.TryGetValue(field, out var existing) ? (long)existing : 0L;
            var updated = current + value;
            hash[field] = updated;
            return Task.FromResult(updated);
        }

        public Task<double> HashIncrementAsync(RedisKey key, RedisValue field, double value,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var hash = hashes.GetOrAdd(key, _ => new ConcurrentDictionary<RedisValue, RedisValue>());
            var current = hash.TryGetValue(field, out var existing) ? (double)existing : 0d;
            var updated = current + value;
            hash[field] = updated;
            return Task.FromResult(updated);
        }

        public Task<long> HashDecrementAsync(RedisKey key, RedisValue field, long value,
            CommandFlags flags = CommandFlags.None)
        {
            return HashIncrementAsync(key, field, -value, flags);
        }

        public Task<double> HashDecrementAsync(RedisKey key, RedisValue field, double value,
            CommandFlags flags = CommandFlags.None)
        {
            return HashIncrementAsync(key, field, -value, flags);
        }

        public Task<long> HashStringLengthAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (hashes.TryGetValue(key, out var hash) && hash.TryGetValue(field, out var value))
            {
                return Task.FromResult(value.Length());
            }

            return Task.FromResult(0L);
        }

        public Task<long> HashLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            return Task.FromResult(hashes.TryGetValue(key, out var hash) ? hash.Count : 0L);
        }

        public async IAsyncEnumerable<HashEntry> HashScanAsync(RedisKey key, RedisValue pattern = default,
            int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            foreach (var entry in HashScan(key, pattern, pageSize, cursor, pageOffset, flags))
            {
                yield return entry;
            }

            await Task.CompletedTask;
        }

        public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern = default, int pageSize = 250,
            long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (!hashes.TryGetValue(key, out var hash))
            {
                yield break;
            }

            var regex = GlobToRegex(pattern);
            foreach (var kvp in hash)
            {
                if (regex == null || regex.IsMatch(kvp.Key.ToString()))
                {
                    yield return new HashEntry(kvp.Key, kvp.Value);
                }
            }
        }

        public Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var set = sets.GetOrAdd(key, _ => new ConcurrentDictionary<RedisValue, byte>());
            return Task.FromResult(set.TryAdd(value, 0));
        }

        public Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            return Task.FromResult(sets.TryGetValue(key, out var set) && set.ContainsKey(value));
        }

        public Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            return Task.FromResult(sets.TryGetValue(key, out var set) && set.TryRemove(value, out _));
        }

        public Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (!sets.TryGetValue(key, out var set))
            {
                return Task.FromResult(0L);
            }

            var count = values.Count(v => set.TryRemove(v, out _));
            return Task.FromResult((long)count);
        }

        public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var members = sets.TryGetValue(key, out var set) ? set.Keys.ToArray() : [];
            return Task.FromResult(members);
        }

        public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var zset = sortedSets.GetOrAdd(key, _ => new ConcurrentDictionary<RedisValue, double>());
            var isNew = !zset.ContainsKey(member);
            zset[member] = score;
            return Task.FromResult(isNew);
        }

        public Task<long> SortedSetLengthAsync(RedisKey key, double min = double.NegativeInfinity,
            double max = double.PositiveInfinity, Exclude exclude = Exclude.None,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (!sortedSets.TryGetValue(key, out var zset))
            {
                return Task.FromResult(0L);
            }

            var count = zset.Values.Count(score => InRange(score, min, max, exclude));
            return Task.FromResult((long)count);
        }

        public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity, Exclude exclude = Exclude.None,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (!sortedSets.TryGetValue(key, out var zset))
            {
                return Task.FromResult(0L);
            }

            var toRemove = zset.Where(kv => InRange(kv.Value, start, stop, exclude)).Select(kv => kv.Key).ToList();
            var count = toRemove.Count(member => zset.TryRemove(member, out _));
            return Task.FromResult((long)count);
        }

        public Task<bool> SortedSetUpdateAsync(RedisKey key, RedisValue member, double score,
            SortedSetWhen when = SortedSetWhen.Always, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            var zset = sortedSets.GetOrAdd(key, _ => new ConcurrentDictionary<RedisValue, double>());
            var existed = zset.TryGetValue(member, out var existingScore);
            var changed = !existed || !existingScore.Equals(score);
            zset[member] = score;
            return Task.FromResult(changed);
        }

        public Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            return Task.FromResult(sortedSets.TryGetValue(key, out var zset) && zset.TryRemove(member, out _));
        }

        public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (!sortedSets.TryGetValue(key, out var zset))
            {
                return Task.FromResult(0L);
            }

            var count = members.Count(m => zset.TryRemove(m, out _));
            return Task.FromResult((long)count);
        }

        public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key, long start = 0, long stop = -1,
            Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (!sortedSets.TryGetValue(key, out var zset))
            {
                return Task.FromResult(Array.Empty<SortedSetEntry>());
            }

            var ordered = OrderEntries(zset, order).ToList();
            var effectiveStop = stop < 0 ? ordered.Count + stop : stop;
            var effectiveStart = Math.Max(0, start);
            effectiveStop = Math.Min(ordered.Count - 1, effectiveStop);

            if (effectiveStart > effectiveStop || ordered.Count == 0)
            {
                return Task.FromResult(Array.Empty<SortedSetEntry>());
            }

            var slice = ordered.Skip((int)effectiveStart).Take((int)(effectiveStop - effectiveStart + 1))
                .Select(kv => new SortedSetEntry(kv.Key, kv.Value)).ToArray();
            return Task.FromResult(slice);
        }

        public Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key,
            double start = double.NegativeInfinity, double stop = double.PositiveInfinity,
            Exclude exclude = Exclude.None,
            Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            if (!sortedSets.TryGetValue(key, out var zset))
            {
                return Task.FromResult(Array.Empty<SortedSetEntry>());
            }

            var filtered = zset.Where(kv => InRange(kv.Value, start, stop, exclude));
            var ordered = OrderEntries(filtered, order).Skip((int)skip);

            if (take >= 0)
            {
                ordered = ordered.Take((int)take);
            }

            return Task.FromResult(ordered.Select(kv => new SortedSetEntry(kv.Key, kv.Value)).ToArray());
        }

        public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null,
            CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            strings[key] = value;
            return Task.FromResult(true);
        }

        public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null,
            CommandFlags flags = CommandFlags.None)
        {
#pragma warning disable VSTHRD002
            return StringSetAsync(key, value, expiry, flags).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            return Task.FromResult(strings.TryGetValue(key, out var value) ? value : RedisValue.Null);
        }

        public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
        {
            MaybeThrow();
            PublishedMessages.Enqueue((channel, message));
            return Task.FromResult(1L);
        }

        private void MaybeThrow([CallerMemberName] string? method = null)
        {
            if (ThrowOnMethod == null || ThrowOnMethod != method)
            {
                return;
            }

            ThrowOnMethod = null;
            throw ThrowOnMethodException ?? new InvalidOperationException($"Injected failure for {method}.");
        }

        private static Regex? GlobToRegex(RedisValue pattern)
        {
            if (pattern.IsNull || pattern.IsNullOrEmpty)
            {
                return null;
            }

            var escaped = Regex.Escape(pattern.ToString()).Replace(@"\*", ".*").Replace(@"\?", ".");
            return new Regex($"^{escaped}$");
        }

        private static bool InRange(double score, double min, double max, Exclude exclude)
        {
            var aboveMin = exclude is Exclude.Start or Exclude.Both ? score > min : score >= min;
            var belowMax = exclude is Exclude.Stop or Exclude.Both ? score < max : score <= max;
            return aboveMin && belowMax;
        }

        private static IEnumerable<KeyValuePair<RedisValue, double>> OrderEntries(
            IEnumerable<KeyValuePair<RedisValue, double>> entries, Order order)
        {
            return order == Order.Descending
                ? entries.OrderByDescending(kv => kv.Value).ThenByDescending(kv => kv.Key.ToString())
                : entries.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key.ToString());
        }
    }
}