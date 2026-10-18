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

using Jube.AskJooby.Tools.Rag.Interfaces;

namespace Jube.AskJooby.Rag
{
    public sealed class RetrievedBiteLedger(int maxSearches = 64) : IRetrievedBiteLedger
    {
        private const string Unattributed = "";
        private readonly Lock gate = new();
        private readonly Dictionary<string, HashSet<string>> seenByAgent = new(StringComparer.Ordinal);
        private bool anyRetrieved;
        private int searches;

        public bool Register(string biteKey)
        {
            return Register(Unattributed, biteKey);
        }

        public IRetrievedBiteLedger ForAgent(string? agentKey)
        {
            return new AgentScopedRetrievedBiteLedger(this, agentKey);
        }

        public bool TryStartSearch()
        {
            lock (gate)
            {
                if (maxSearches > 0 && searches >= maxSearches)
                {
                    return false;
                }

                searches++;

                return true;
            }
        }

        public bool AnyRetrieved
        {
            get
            {
                lock (gate)
                {
                    return anyRetrieved;
                }
            }
        }

        public bool Register(string? agentKey, string biteKey)
        {
            var bucket = string.IsNullOrWhiteSpace(agentKey) ? Unattributed : agentKey;

            lock (gate)
            {
                if (!seenByAgent.TryGetValue(bucket, out var seen))
                {
                    seen = new HashSet<string>(StringComparer.Ordinal);
                    seenByAgent[bucket] = seen;
                }

                var added = seen.Add(biteKey);
                anyRetrieved = anyRetrieved || added;

                return added;
            }
        }
    }
}