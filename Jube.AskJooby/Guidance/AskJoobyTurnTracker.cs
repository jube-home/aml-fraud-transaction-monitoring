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

using Jube.AskJooby.Guidance.Interfaces;

namespace Jube.AskJooby.Guidance
{
    public sealed class AskJoobyTurnTracker : IAskJoobyTurnTracker
    {
        private readonly Dictionary<string, int> active = new(StringComparer.Ordinal);
        private readonly Lock gate = new();

        public IDisposable Begin(string conversationId)
        {
            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                lock (gate)
                {
                    active[conversationId] = active.GetValueOrDefault(conversationId) + 1;
                }
            }

            return new Handle(this, conversationId);
        }

        public bool IsRunning(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return false;
            }

            lock (gate)
            {
                return active.ContainsKey(conversationId);
            }
        }

        private void End(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return;
            }

            lock (gate)
            {
                if (!active.TryGetValue(conversationId, out var count))
                {
                    return;
                }

                if (count <= 1)
                {
                    active.Remove(conversationId);
                }
                else
                {
                    active[conversationId] = count - 1;
                }
            }
        }

        private sealed class Handle(AskJoobyTurnTracker owner, string conversationId) : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                owner.End(conversationId);
            }
        }
    }
}