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
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class PatroniMetricSampler : IDisposable
    {
        private const int MaxSeenHistoryKeys = 5000;

        private readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private readonly Dictionary<string, (string Role, string State, int? TimelineId)> lastSeenByName = new();
        private readonly HashSet<string> seenHistoryKeys = [];

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public async Task<(List<PatroniMemberStatus> Statuses, List<PatroniClusterEvent> Events)> SampleAsync(
            IReadOnlyList<string> endpoints, CancellationToken token = default)
        {
            var results = new List<PatroniMemberStatus>();
            if (endpoints.Count == 0)
            {
                return (results, []);
            }

            var cluster = await GetClusterAsync(endpoints, token).ConfigureAwait(false);
            var clusterMembersByName = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            string scope = null;
            if (cluster != null)
            {
                scope = (string)cluster["scope"];
                if (cluster["members"] is JArray members)
                {
                    foreach (var member in members)
                    {
                        var name = (string)member["name"];
                        if (name != null)
                        {
                            clusterMembersByName[name] = member;
                        }
                    }
                }
            }

            var syncStandbyByApplicationName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var endpoint in endpoints)
            {
                var patroni = await GetPatroniAsync(endpoint, token).ConfigureAwait(false);
                if (patroni == null)
                {
                    continue;
                }

                var name = (string)patroni["patroni"]?["name"];
                clusterMembersByName.TryGetValue(name ?? string.Empty, out var clusterMember);

                if (patroni["replication"] is JArray replication)
                {
                    foreach (var replica in replication)
                    {
                        if ((string)replica["sync_state"] == "sync")
                        {
                            var applicationName = (string)replica["application_name"];
                            if (applicationName != null)
                            {
                                syncStandbyByApplicationName.Add(applicationName);
                            }
                        }
                    }
                }

                var xlog = patroni["xlog"];

                results.Add(new PatroniMemberStatus
                {
                    Name = name,
                    Host = (string)clusterMember?["host"] ?? SplitHost(endpoint),
                    Port = (int?)clusterMember?["port"],
                    ApiUrl = $"http://{endpoint}/patroni",
                    Role = (string)clusterMember?["role"] ?? (string)patroni["role"],
                    State = (string)clusterMember?["state"] ?? (string)patroni["state"],
                    TimelineId = (int?)clusterMember?["timeline"] ?? (int?)patroni["timeline"],
                    LagBytes = ParseLag(clusterMember?["lag"]),
                    PendingRestart = (bool?)patroni["pending_restart"],
                    PatroniVersion = (string)patroni["patroni"]?["version"],
                    Scope = scope ?? (string)patroni["patroni"]?["scope"],
                    PostgresServerVersion = (int?)patroni["server_version"],
                    DatabaseSystemIdentifier = (string)patroni["database_system_identifier"],
                    XlogLocationBytes = (long?)xlog?["location"] ?? (long?)xlog?["replayed_location"],
                    ReceivedLocationBytes = (long?)xlog?["received_location"],
                    ReplayPaused = (bool?)xlog?["paused"],
                    ClusterUnlocked = (bool?)patroni["cluster_unlocked"]
                });
            }

            foreach (var result in results)
            {
                if (result.Name != null && syncStandbyByApplicationName.Contains(result.Name))
                {
                    result.SyncStandby = true;
                }
                else if (result.Role != null && !result.Role.Contains("leader", StringComparison.OrdinalIgnoreCase))
                {
                    result.SyncStandby = false;
                }
            }

            var events = new List<PatroniClusterEvent>();
            foreach (var result in results)
            {
                if (result.Name == null)
                {
                    continue;
                }

                if (lastSeenByName.TryGetValue(result.Name, out var last))
                {
                    if (result.Role != last.Role)
                    {
                        events.Add(NewDiffEvent(result, PatroniClusterEventType.RoleChanged, last.Role,
                            result.Role));
                    }

                    if (result.State != last.State)
                    {
                        events.Add(NewDiffEvent(result, PatroniClusterEventType.StateChanged, last.State,
                            result.State));
                    }

                    if (result.TimelineId != last.TimelineId)
                    {
                        events.Add(NewDiffEvent(result, PatroniClusterEventType.TimelineChanged,
                            last.TimelineId?.ToString(), result.TimelineId?.ToString()));
                    }
                }

                lastSeenByName[result.Name] = (result.Role, result.State, result.TimelineId);
            }

            return (results, events);
        }

        public async Task<List<PatroniClusterEvent>> SampleHistoryEventsAsync(IReadOnlyList<string> etcdEndpoints,
            string scope, string dcsNamespace, CancellationToken token = default)
        {
            var events = new List<PatroniClusterEvent>();
            if (etcdEndpoints.Count == 0 || string.IsNullOrEmpty(scope))
            {
                return events;
            }

            var key = $"{dcsNamespace}{scope}/history";
            var keyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));

            JArray history = null;
            foreach (var etcdEndpoint in etcdEndpoints)
            {
                var rangeResult = await PostJsonAsync($"http://{etcdEndpoint}/v3/kv/range",
                    $"{{\"key\":\"{keyBase64}\"}}", token).ConfigureAwait(false);
                var valueBase64 = (string)rangeResult?["kvs"]?[0]?["value"];
                if (valueBase64 == null)
                {
                    continue;
                }

                try
                {
                    history = ParseHistoryJson(Encoding.UTF8.GetString(Convert.FromBase64String(valueBase64)));
                }
                catch (Exception)
                {
                    history = null;
                }

                break;
            }

            if (history == null)
            {
                return events;
            }

            if (seenHistoryKeys.Count > MaxSeenHistoryKeys)
            {
                seenHistoryKeys.Clear();
            }

            string previousLeader = null;
            foreach (var entry in history)
            {
                if (entry is not JArray fields)
                {
                    continue;
                }

                var timeline = At(fields, 0)?.Type == JTokenType.Integer ? (int?)At(fields, 0) : null;
                var lsn = At(fields, 1)?.Type == JTokenType.Integer ? (long?)At(fields, 1) : null;
                var reason = (string)At(fields, 2);
                var timestamp = (string)At(fields, 3);
                var newLeader = (string)At(fields, 4);

                var dedupeKey = $"{timeline}:{lsn}";
                if (!seenHistoryKeys.Add(dedupeKey))
                {
                    previousLeader = newLeader;
                    continue;
                }

                events.Add(new PatroniClusterEvent
                {
                    OccurredDate = DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var occurredDate)
                        ? occurredDate.UtcDateTime
                        : null,
                    Scope = scope,
                    Name = newLeader,
                    EventTypeId = (int)PatroniClusterEventType.Failover,
                    PreviousValue = previousLeader,
                    NewValue = newLeader,
                    Reason = reason,
                    TimelineId = timeline,
                    LsnBytes = lsn
                });

                previousLeader = newLeader;
            }

            return events;
        }

        private static JArray ParseHistoryJson(string json)
        {
            using var stringReader = new StringReader(json);
            using var jsonReader = new JsonTextReader(stringReader);
            jsonReader.DateParseHandling = DateParseHandling.None;
            return (JArray)JToken.ReadFrom(jsonReader);
        }

        private static JToken At(JArray array, int index)
        {
            return index < array.Count ? array[index] : null;
        }

        private static PatroniClusterEvent NewDiffEvent(PatroniMemberStatus status,
            PatroniClusterEventType eventType, string previousValue, string newValue)
        {
            return new PatroniClusterEvent
            {
                Scope = status.Scope,
                Name = status.Name,
                EventTypeId = (int)eventType,
                PreviousValue = previousValue,
                NewValue = newValue
            };
        }

        private async Task<JObject> GetClusterAsync(IReadOnlyList<string> endpoints, CancellationToken token)
        {
            foreach (var endpoint in endpoints)
            {
                var cluster = await GetJsonAsync($"http://{endpoint}/cluster", token).ConfigureAwait(false);
                if (cluster != null)
                {
                    return cluster;
                }
            }

            return null;
        }

        private Task<JObject> GetPatroniAsync(string endpoint, CancellationToken token)
        {
            return GetJsonAsync($"http://{endpoint}/patroni", token);
        }

        private async Task<JObject> GetJsonAsync(string url, CancellationToken token)
        {
            try
            {
                var text = await httpClient.GetStringAsync(url, token).ConfigureAwait(false);
                return JObject.Parse(text);
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                return null;
            }
        }

        private async Task<JObject> PostJsonAsync(string url, string body, CancellationToken token)
        {
            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                using var response = await httpClient.PostAsync(url, content, token).ConfigureAwait(false);
                var text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                return JObject.Parse(text);
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                return null;
            }
        }

        private static string SplitHost(string endpoint)
        {
            var colonIndex = endpoint.LastIndexOf(':');
            return colonIndex > 0 ? endpoint[..colonIndex] : endpoint;
        }

        private static long? ParseLag(JToken lagToken)
        {
            return lagToken?.Type == JTokenType.Integer ? (long?)lagToken : null;
        }
    }
}