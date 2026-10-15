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

using System.Collections.Concurrent;
using Jube.AskJooby.Guidance.Interfaces;

namespace Jube.AskJooby.Guidance
{
    public sealed class AgentGuidanceExchange(int maximumPerConversation = 32) : IAgentGuidanceExchange
    {
        private readonly ConcurrentDictionary<string, Turn> turns = new(StringComparer.Ordinal);

        public void Send(string conversationId, string guidance)
        {
            if (string.IsNullOrWhiteSpace(conversationId) || string.IsNullOrWhiteSpace(guidance))
            {
                return;
            }

            var turn = turns.GetOrAdd(conversationId, _ => new Turn());

            lock (turn.gate)
            {
                if (turn.entries.Count >= maximumPerConversation)
                {
                    turn.entries.RemoveAt(0);

                    foreach (var runId in turn.cursors.Keys.ToList())
                    {
                        turn.cursors[runId] = Math.Max(0, turn.cursors[runId] - 1);
                    }
                }

                turn.entries.Add(guidance.Trim());
            }
        }

        public IReadOnlyList<string> Drain(string conversationId, string runId)
        {
            if (string.IsNullOrWhiteSpace(conversationId) || string.IsNullOrWhiteSpace(runId)
                                                          || !turns.TryGetValue(conversationId, out var turn))
            {
                return [];
            }

            lock (turn.gate)
            {
                var cursor = turn.cursors.GetValueOrDefault(runId, 0);

                if (cursor >= turn.entries.Count)
                {
                    return [];
                }

                var pending = turn.entries.Skip(cursor).ToList();
                turn.cursors[runId] = turn.entries.Count;

                return pending;
            }
        }

        public void Release(string conversationId)
        {
            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                turns.TryRemove(conversationId, out _);
            }
        }

        private sealed class Turn
        {
            internal readonly Dictionary<string, int> cursors = new(StringComparer.Ordinal);
            internal readonly List<string> entries = [];
            internal readonly object gate = new();
        }
    }
}