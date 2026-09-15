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
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class EtcdMetricSampler : IDisposable
    {
        private readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private readonly Dictionary<string, (string LeaderId, string Alarms, bool? HealthOk)> lastSeenByEndpoint =
            new();

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public async Task<(EtcdMemberStatus Status, List<EtcdClusterEvent> Events)> SampleAsync(string endpoint,
            CancellationToken token = default)
        {
            try
            {
                var baseUrl = $"http://{endpoint}";

                var versionJson = await GetJsonAsync($"{baseUrl}/version", token).ConfigureAwait(false);
                var healthJson = await GetJsonAsync($"{baseUrl}/health", token).ConfigureAwait(false);
                var statusJson = await PostJsonAsync($"{baseUrl}/v3/maintenance/status", "{}", token)
                    .ConfigureAwait(false);
                var memberListJson = await PostJsonAsync($"{baseUrl}/v3/cluster/member/list", "{}", token)
                    .ConfigureAwait(false);
                var alarmJson = await PostJsonAsync($"{baseUrl}/v3/maintenance/alarm", "{\"action\":\"GET\"}", token)
                    .ConfigureAwait(false);
                var metricsText = await GetTextAsync($"{baseUrl}/metrics", token).ConfigureAwait(false);

                if (statusJson == null)
                {
                    return (null, []);
                }

                var thisMemberId = (string)statusJson["header"]?["member_id"];
                var leaderId = (string)statusJson["leader"];

                string name = null, peerUrls = null, clientUrls = null;
                bool? isLearner = null;
                if (memberListJson?["members"] is JArray members)
                {
                    var self = members.FirstOrDefault(m => (string)m["ID"] == thisMemberId);
                    if (self != null)
                    {
                        name = (string)self["name"];
                        peerUrls = JoinArray(self["peerURLs"]);
                        clientUrls = JoinArray(self["clientURLs"]);
                        isLearner = (bool?)self["isLearner"] ?? false;
                    }
                }

                var alarmTypes = new List<string>();
                if (alarmJson?["alarms"] is JArray alarmsArray)
                {
                    alarmTypes.AddRange(alarmsArray.Select(a => (string)a["alarm"]).Where(a => a != null));
                }

                var metrics = ParsePrometheusMetrics(metricsText);

                double? Avg(string sumKey, string countKey)
                {
                    if (!metrics.TryGetValue(sumKey, out var sum) || !metrics.TryGetValue(countKey, out var count) ||
                        count <= 0)
                    {
                        return null;
                    }

                    return sum / count * 1_000_000.0;
                }

                long? MetricLong(string key)
                {
                    return metrics.TryGetValue(key, out var value) ? (long)value : null;
                }

                bool? MetricBool(string key)
                {
                    return metrics.TryGetValue(key, out var value) ? value != 0 : null;
                }

                var alarms = alarmTypes.Count > 0 ? string.Join(",", alarmTypes) : null;
                var healthOk = healthJson != null && (string)healthJson["health"] == "true";

                var status = new EtcdMemberStatus
                {
                    Endpoint = endpoint,
                    MemberId = thisMemberId,
                    Name = name,
                    PeerUrls = peerUrls,
                    ClientUrls = clientUrls,
                    IsLearner = isLearner,
                    Version = (string)versionJson?["etcdserver"],
                    ClusterVersion = (string)versionJson?["etcdcluster"],
                    HealthOk = healthOk,
                    HealthReason = (string)healthJson?["reason"],
                    LeaderId = leaderId,
                    IsLeader = thisMemberId != null && thisMemberId == leaderId,
                    HasLeader = MetricBool("etcd_server_has_leader"),
                    LeaderChangesTotal = MetricLong("etcd_server_leader_changes_seen_total"),
                    DbSizeBytes = (long?)statusJson["dbSize"],
                    DbSizeInUseBytes = (long?)statusJson["dbSizeInUse"],
                    RaftIndex = (long?)statusJson["raftIndex"],
                    RaftTerm = (long?)statusJson["raftTerm"],
                    RaftAppliedIndex = (long?)statusJson["raftAppliedIndex"],
                    ProposalsCommittedTotal = MetricLong("etcd_server_proposals_committed_total"),
                    ProposalsAppliedTotal = MetricLong("etcd_server_proposals_applied_total"),
                    ProposalsPendingCount = MetricLong("etcd_server_proposals_pending"),
                    ProposalsFailedTotal = MetricLong("etcd_server_proposals_failed_total"),
                    WalFsyncAvgMicroseconds = Avg("etcd_disk_wal_fsync_duration_seconds_sum",
                        "etcd_disk_wal_fsync_duration_seconds_count"),
                    BackendCommitAvgMicroseconds = Avg("etcd_disk_backend_commit_duration_seconds_sum",
                        "etcd_disk_backend_commit_duration_seconds_count"),
                    SlowApplyTotal = MetricLong("etcd_server_slow_apply_total"),
                    SlowReadIndexesTotal = MetricLong("etcd_server_slow_read_indexes_total"),
                    AlarmCount = alarmTypes.Count,
                    Alarms = alarms
                };

                var events = DetectEvents(endpoint, status, alarms, healthOk);

                return (status, events);
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                return (null, []);
            }
        }

        private List<EtcdClusterEvent> DetectEvents(string endpoint, EtcdMemberStatus status, string alarms,
            bool healthOk)
        {
            var events = new List<EtcdClusterEvent>();

            if (lastSeenByEndpoint.TryGetValue(endpoint, out var last))
            {
                if (status.LeaderId != last.LeaderId)
                {
                    events.Add(NewEvent(status, EtcdClusterEventType.LeaderChanged, last.LeaderId, status.LeaderId));
                }

                if (healthOk != last.HealthOk)
                {
                    events.Add(NewEvent(status, EtcdClusterEventType.HealthChanged, last.HealthOk?.ToString(),
                        healthOk.ToString()));
                }

                var lastAlarms = SplitAlarms(last.Alarms);
                var currentAlarms = SplitAlarms(alarms);

                foreach (var raised in currentAlarms.Except(lastAlarms))
                {
                    events.Add(NewEvent(status, EtcdClusterEventType.AlarmRaised, null, raised));
                }

                foreach (var cleared in lastAlarms.Except(currentAlarms))
                {
                    events.Add(NewEvent(status, EtcdClusterEventType.AlarmCleared, cleared, null));
                }
            }

            lastSeenByEndpoint[endpoint] = (status.LeaderId, alarms, healthOk);

            return events;
        }

        private static HashSet<string> SplitAlarms(string alarms)
        {
            return string.IsNullOrEmpty(alarms)
                ? []
                : alarms.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        }

        private static EtcdClusterEvent NewEvent(EtcdMemberStatus status, EtcdClusterEventType eventType,
            string previousValue, string newValue)
        {
            return new EtcdClusterEvent
            {
                Endpoint = status.Endpoint,
                MemberId = status.MemberId,
                Name = status.Name,
                EventTypeId = (int)eventType,
                PreviousValue = previousValue,
                NewValue = newValue
            };
        }

        private static string JoinArray(JToken token)
        {
            return token is JArray array ? string.Join(",", array.Select(v => (string)v)) : null;
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

        private async Task<string> GetTextAsync(string url, CancellationToken token)
        {
            try
            {
                return await httpClient.GetStringAsync(url, token).ConfigureAwait(false);
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                return null;
            }
        }

        private static Dictionary<string, double> ParsePrometheusMetrics(string metricsText)
        {
            var values = new Dictionary<string, double>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(metricsText))
            {
                return values;
            }

            foreach (var line in metricsText.Split('\n'))
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                var spaceIndex = line.LastIndexOf(' ');
                if (spaceIndex <= 0)
                {
                    continue;
                }

                var namePart = line[..spaceIndex];
                var braceIndex = namePart.IndexOf('{');
                var name = braceIndex >= 0 ? namePart[..braceIndex] : namePart;

                if (values.ContainsKey(name))
                {
                    continue;
                }

                if (double.TryParse(line[(spaceIndex + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var value))
                {
                    values[name] = value;
                }
            }

            return values;
        }
    }
}