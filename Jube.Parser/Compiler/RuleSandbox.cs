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

namespace Jube.Parser.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class RuleSandbox : IDisposable
    {
        private readonly int capacity;
        private readonly Dictionary<string, LinkedListNode<Entry>> entries = new();
        private readonly TimeSpan executionTimeout;
        private readonly LinkedList<Entry> recency = new();
        private readonly Lock sync = new();
        private readonly SemaphoreSlim loadGate;

        public RuleSandbox(int capacity = 64, int maximumConcurrentLoads = 2, TimeSpan? executionTimeout = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(maximumConcurrentLoads, 1);

            this.capacity = capacity;
            this.executionTimeout = executionTimeout ?? TimeSpan.FromSeconds(5);
            loadGate = new SemaphoreSlim(maximumConcurrentLoads, maximumConcurrentLoads);
        }

        public int Count
        {
            get
            {
                lock (sync)
                {
                    return entries.Count;
                }
            }
        }

        public long Bytes
        {
            get
            {
                lock (sync)
                {
                    var total = 0L;
                    foreach (var entry in recency)
                    {
                        total += entry.Bytes;
                    }

                    return total;
                }
            }
        }

        public long Loads { get; private set; }
        public long Hits { get; private set; }
        public long Evictions { get; private set; }

        public void Dispose()
        {
            lock (sync)
            {
                foreach (var entry in recency)
                {
                    entry.Context.Unload();
                }

                recency.Clear();
                entries.Clear();
            }

            loadGate.Dispose();
        }

        public async Task<TDelegate> GetDelegateAsync<TDelegate>(RuleParseResult parsed,
            CancellationToken token = default) where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(parsed);

            if (!parsed.Compiled || parsed.CompiledBinary == null || parsed.ClassName == null)
            {
                throw new InvalidOperationException("Only a rule that has parsed and compiled can be loaded.");
            }

            var key = CacheKey(parsed, typeof(TDelegate));

            if (TryGet(key, out var cached))
            {
                return (TDelegate)cached;
            }

            await loadGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (TryGet(key, out cached))
                {
                    return (TDelegate)cached;
                }

                var entry = Load<TDelegate>(key, parsed);
                Add(entry);
                return (TDelegate)entry.Match;
            }
            finally
            {
                loadGate.Release();
            }
        }

        public async Task<TResult> RunAsync<TResult>(Func<TResult> invocation, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(invocation);

            var run = Task.Run(invocation, token);
            try
            {
                return await run.WaitAsync(executionTimeout, token).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    $"The rule did not complete within {executionTimeout.TotalMilliseconds} milliseconds.");
            }
        }

        public bool Evict(RuleParseResult parsed, Type delegateType)
        {
            ArgumentNullException.ThrowIfNull(parsed);
            ArgumentNullException.ThrowIfNull(delegateType);

            lock (sync)
            {
                if (!entries.Remove(CacheKey(parsed, delegateType), out var node))
                {
                    return false;
                }

                recency.Remove(node);
                Unload(node.Value);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Entry Load<TDelegate>(string key, RuleParseResult parsed) where TDelegate : Delegate
        {
            var context = new SimpleUnloadableAssemblyLoadContext();
            try
            {
                using var stream = new MemoryStream(parsed.CompiledBinary);
                var assembly = context.LoadFromStream(stream);
                var method = assembly.GetType(parsed.ClassName)?.GetMethod("Match")
                             ?? throw new InvalidOperationException(
                                 $"The compiled rule does not expose {parsed.ClassName}.Match.");

                var match = Delegate.CreateDelegate(typeof(TDelegate), method);
                return new Entry(key, context, match, parsed.CompiledBinary.LongLength);
            }
            catch
            {
                context.Unload();
                throw;
            }
        }

        private bool TryGet(string key, out Delegate match)
        {
            lock (sync)
            {
                if (entries.TryGetValue(key, out var node))
                {
                    recency.Remove(node);
                    recency.AddFirst(node);
                    Hits++;
                    match = node.Value.Match;
                    return true;
                }
            }

            match = null;
            return false;
        }

        private void Add(Entry entry)
        {
            lock (sync)
            {
                entries[entry.Key] = recency.AddFirst(entry);
                Loads++;

                while (entries.Count > capacity)
                {
                    var last = recency.Last!;
                    recency.RemoveLast();
                    entries.Remove(last.Value.Key);
                    Unload(last.Value);
                }
            }
        }

        private void Unload(Entry entry)
        {
            Evictions++;
            entry.Context.Unload();
        }

        private static string CacheKey(RuleParseResult parsed, Type delegateType)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(
                string.Concat(delegateType.FullName, "\n", parsed.ClassName, "\n", parsed.ParsedRuleText)));
            return Convert.ToHexString(bytes);
        }

        private sealed record Entry(
            string Key,
            SimpleUnloadableAssemblyLoadContext Context,
            Delegate Match,
            long Bytes);
    }
}