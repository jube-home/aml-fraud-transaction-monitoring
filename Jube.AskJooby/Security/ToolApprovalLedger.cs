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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jube.AskJooby.Security.Models;

namespace Jube.AskJooby.Security
{
    public sealed class ToolApprovalLedger(TimeSpan? timeToLive = null, int capacity = 500, int pendingPerConversation = 20)
    {
        public static readonly ToolApprovalLedger Shared = new();

        private readonly object gate = new();
        private readonly List<ToolApprovalRequest> requests = [];
        private readonly TimeSpan lifetime = timeToLive ?? TimeSpan.FromMinutes(30);

        public static string Canonicalise(IReadOnlyDictionary<string, object?> arguments)
        {
            var ordered = new SortedDictionary<string, object?>(StringComparer.Ordinal);

            foreach (var pair in arguments)
            {
                ordered[pair.Key] = pair.Value;
            }

            try
            {
                return JsonSerializer.Serialize(ordered);
            }
            catch (NotSupportedException)
            {
                return string.Join("|", ordered.Select(pair => $"{pair.Key}={pair.Value}"));
            }
        }

        public static string Hash(string toolName, string argumentsJson)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{toolName}\n{argumentsJson}")));
        }

        public ToolApprovalRequest Request(string conversationKey, string userName, int tenantRegistryId,
            string toolName, string argumentsJson)
        {
            var hash = Hash(toolName, argumentsJson);

            lock (gate)
            {
                Purge();

                var existing = requests.FirstOrDefault(r =>
                    r.State == ToolApprovalState.Pending && Matches(r, conversationKey, userName, tenantRegistryId)
                                                         && r.ArgumentsHash == hash);

                if (existing is not null)
                {
                    return existing;
                }

                var pending = requests.Where(r => r.State == ToolApprovalState.Pending
                                                  && Matches(r, conversationKey, userName, tenantRegistryId)).ToList();

                if (pending.Count >= pendingPerConversation)
                {
                    requests.Remove(pending[0]);
                }

                var created = new ToolApprovalRequest
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ConversationKey = conversationKey,
                    UserName = userName,
                    TenantRegistryId = tenantRegistryId,
                    ToolName = toolName,
                    ArgumentsHash = hash,
                    ArgumentsJson = argumentsJson,
                    CreatedUtc = DateTime.UtcNow
                };

                requests.Add(created);

                while (requests.Count > capacity)
                {
                    requests.RemoveAt(0);
                }

                return created;
            }
        }

        public bool TryConsume(string conversationKey, string userName, int tenantRegistryId, string toolName,
            string argumentsJson, ToolApprovalState state)
        {
            var hash = Hash(toolName, argumentsJson);

            lock (gate)
            {
                Purge();

                var match = requests.FirstOrDefault(r => r.State == state
                                                         && Matches(r, conversationKey, userName, tenantRegistryId)
                                                         && r.ArgumentsHash == hash);

                return match is not null && requests.Remove(match);
            }
        }

        public ToolApprovalDecisionResult Decide(string approvalId, string conversationKey, string userName,
            int tenantRegistryId, bool approve)
        {
            lock (gate)
            {
                Purge();

                var match = requests.FirstOrDefault(r => r.Id == approvalId
                                                         && Matches(r, conversationKey, userName, tenantRegistryId));

                if (match is null)
                {
                    return ToolApprovalDecisionResult.NotFound;
                }

                if (match.State != ToolApprovalState.Pending)
                {
                    return ToolApprovalDecisionResult.AlreadyDecided;
                }

                match.State = approve ? ToolApprovalState.Approved : ToolApprovalState.Rejected;

                return ToolApprovalDecisionResult.Recorded;
            }
        }

        public ToolApprovalState? StateOf(string approvalId, string conversationKey, string userName,
            int tenantRegistryId)
        {
            lock (gate)
            {
                Purge();

                return requests.FirstOrDefault(r => r.Id == approvalId
                                                    && Matches(r, conversationKey, userName, tenantRegistryId))?.State;
            }
        }

        public IReadOnlyList<ToolApprovalRequest> Pending(string conversationKey, string userName,
            int tenantRegistryId)
        {
            lock (gate)
            {
                Purge();

                return requests.Where(r => r.State == ToolApprovalState.Pending
                                           && Matches(r, conversationKey, userName, tenantRegistryId)).ToList();
            }
        }

        private static bool Matches(ToolApprovalRequest request, string conversationKey, string userName,
            int tenantRegistryId)
        {
            return request.ConversationKey == conversationKey && request.TenantRegistryId == tenantRegistryId
                                                              && string.Equals(request.UserName, userName,
                                                                  StringComparison.OrdinalIgnoreCase);
        }

        private void Purge()
        {
            var cutoff = DateTime.UtcNow - lifetime;

            requests.RemoveAll(r => r.CreatedUtc < cutoff);
        }
    }
}
