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

using Jube.AskJooby.Workflows.Models;

namespace Jube.AskJooby.Workflows
{
    public static class PlatformOpsWorkflows
    {
        private const string Router = "platform-ops";
        private const string Logs = "application-and-container-logs";
        private const string Containers = "containers-load-balancer-and-overlay-network";
        private const string EtcdPatroni = "etcd-and-patroni-cluster-state";
        private const string Postgres = "postgres-observability";
        private const string Redis = "redis-observability";
        private const string Queues = "engine-queue-and-throughput-counters";
        private const string Invoke = "invoke-path-performance-and-warnings";
        private const string SyncSchedule = "entity-analysis-model-synchronisation-schedule";
        private const string RuntimeMetrics = "telemetry-log-counters-and-runtime-metrics";
        private const string Otlp = "telemetry-metrics-and-otlp-dispatch";

        private const string MonitoringGap =
            "Jube.Monitoring is absent from the cluster compose, so this returns nothing on the real stack "
            + "and its silence means nothing. Say so rather than reporting health.";

        private static GraphWorkflowNode BaselineThenRecent(string agent, IReadOnlyList<string> tools,
            string subject)
        {
            return new GraphWorkflowNode(
                "baseline-then-recent",
                "Sample wide, then recent",
                $"Read {subject} twice: first over a long span with a samplePercentage draw, which is what "
                + "makes the wide read affordable, then over the window actually in question. Carry both "
                + "forward so every later node reports against this deployment's own normal.",
                agent, tools,
                ["BaselineWindow", "RecentWindow"],
                [],
                "A reading on its own is not evidence. Four hundred is a crisis on a system that normally "
                + "runs at forty and a Tuesday on one that normally runs at three hundred and eighty, and "
                + "nothing in the figure itself says which. Taking the most recent rows over a long range "
                + "instead of a sampled draw returns the recent window again wearing a longer label.");
        }

        private static GraphWorkflowNode Operator(string id, string title, string intent,
            IReadOnlyList<string> produces, IReadOnlyList<string> requires, string hazard)
        {
            return new GraphWorkflowNode(id, title, intent, string.Empty, [], produces, requires, hazard,
                WorkflowBoundary.Operator);
        }

        public static readonly IReadOnlyList<GraphWorkflow> All =
        [
            new(
                1,
                "platform-health-triage",
                "Platform Health Triage",
                "The ordered bottom-up read of the whole stack that answers whether the platform is healthy "
                + "and, if not, at which layer it stopped being healthy.",
                "Ten nodes, each layer consuming the verdict of the layer beneath it as an edge, ending in a "
                + "verdict node that holds no tools and hands remediation over.",
                "Use when the complaint is unscoped -- the platform is broken, Jube is down, something is "
                + "wrong. If the complaint already names a subsystem go straight to that subsystem's "
                + "workflow; this one exists to stop an agent guessing which subsystem.",
                [
                    "is the cluster healthy", "the platform is down", "jube is broken",
                    "what's wrong with the environment", "health check the stack", "everything is slow",
                    "triage this incident", "is it us or the database"
                ],
                [
                    new GraphWorkflowNode(
                        "baseline-then-recent", "Sample wide, then recent, and fix both windows",
                        "Take a long sample first -- a week or more -- then the window the complaint is "
                        + "about, and carry both forward so every later node reports a reading against its "
                        + "own normal rather than against nothing. Check in the same step whether the "
                        + "capture queue dropped anything inside either window.",
                        Queues, ["CaptureQueueHealthList"],
                        ["BaselineWindow", "RecentWindow", "ObservationWindow", "CaptureIntegrity"], [],
                        "A reading with no baseline is not evidence. An agent given only the recent window "
                        + "will supply a baseline of its own -- usually zero, which makes everything an "
                        + "incident. And a non-zero dropped count means a bounded queue discarded real "
                        + "events, so every later silence means nothing and the triage concludes no "
                        + "incident when there was one."),
                    new GraphWorkflowNode(
                        "consensus-layer", "Read etcd first",
                        "Quorum, leader identity, alarms, and the fsync and commit latencies that precede a "
                        + "Patroni failover.",
                        EtcdPatroni, ["EtcdMemberStatusList", "EtcdClusterEventList"],
                        ["EtcdQuorumState", "EtcdLeaderIdentity", "EtcdAlarms", "EtcdDiskLatency"],
                        ["BaselineWindow", "RecentWindow", "ObservationWindow"],
                        "Every layer above borrows its truth from etcd. Read Postgres first and a quorum loss "
                        + "presents as four simultaneous unexplained database failures."),
                    new GraphWorkflowNode(
                        "database-leader", "Establish who holds the leader lock",
                        "Leader, sync standby, per-member lag, timeline agreement, the unlocked flag, pending "
                        + "restarts and the system identifiers.",
                        EtcdPatroni, ["PatroniMemberStatusList", "PatroniClusterEventList"],
                        [
                            "LeaderMember", "SyncStandbyMember", "MemberLagBytes", "TimelineAgreement",
                            "ClusterUnlocked", "PendingRestartFlags", "DatabaseSystemIdentifiers"
                        ],
                        ["EtcdQuorumState"],
                        "An unlocked cluster with healthy etcd is a different incident from the same flag with "
                        + "a lost quorum, and the remediation differs completely. Without the node above, the "
                        + "two are indistinguishable."),
                    new GraphWorkflowNode(
                        "primary-postgres", "Read the primary's own condition",
                        "Connections, waiting backends, cache hit ratio, deadlocks, temp spill, WAL generated "
                        + "and standby lag by application name.",
                        Postgres, ["PostgresMetricList", "PostgresReplicationStatusList"],
                        [
                            "PrimaryConnectionCount", "WaitingBackends", "CacheHitRatio", "Deadlocks",
                            "TempSpill", "WalBytesGenerated", "StandbyLagByApplicationName", "SyncState"
                        ],
                        ["LeaderMember"],
                        "These are sampled through HAProxy, so they are whoever was primary at sample time and "
                        + "never a named node. Attributing them to a specific member without knowing the "
                        + "leader is wrong the moment a failover happened in the window."),
                    new GraphWorkflowNode(
                        "routing-layer", "Check the proxy agrees with the DCS",
                        "Backend server states, the primary singleton invariant, check failures and queue "
                        + "depth.",
                        Containers, ["HAProxyServerStatusList", "HAProxyReachabilityProbeList"],
                        [
                            "BackendServerStates", "PrimaryBackendSingleton", "CheckFailCounts",
                            "QueueDepthPerServer", "ReachabilityLatency"
                        ],
                        ["LeaderMember"],
                        "Exactly one server up in the primary backend is the invariant: two is split brain, "
                        + "zero is a write outage, and neither can be judged without knowing who Patroni "
                        + "believes the leader is. " + MonitoringGap),
                    new GraphWorkflowNode(
                        "cache-layer", "Read Sentinel as an independent domain",
                        "Master identity, quorum, per-slave lag, memory pressure and evictions.",
                        Redis, ["RedisSentinelStatusList", "RedisSentinelEventList", "RedisMetricList"],
                        [
                            "SentinelMasterIdentity", "SentinelQuorum", "SlaveReplicationLag",
                            "RedisMemoryPressure", "EvictionCount"
                        ],
                        ["ObservationWindow"],
                        "Sentinel is independent of Patroni and etcd. Folding it into the database verdict "
                        + "invents a shared cause for two unrelated failures."),
                    new GraphWorkflowNode(
                        "application-connection-health", "Read how clients experienced the cache",
                        "Multiplexer queue depth, connection failures and configuration-changed broadcasts.",
                        Redis, ["RedisConnectionMultiplexerMetricList", "RedisConnectionEventList"],
                        ["MultiplexerQueueDepth", "ConnectionFailureEvents", "ConfigurationChangedBroadcasts"],
                        ["SentinelMasterIdentity"],
                        "A burst of configuration-changed broadcasts is the normal consequence of a Sentinel "
                        + "switch. Read without the node above it looks like the application losing Redis for "
                        + "no reason."),
                    new GraphWorkflowNode(
                        "container-and-host-layer", "Read containers and hosts last, not first",
                        "Container states, restart counts, OOM kills, per-host resource use and DNS drift.",
                        Containers,
                        [
                            "DockerContainerMetricList", "DockerHostMetricList", "DockerEventList",
                            "OverlayNetworkTaskDriftList"
                        ],
                        [
                            "ContainerStates", "RestartCounts", "OomKills", "PerHostResourceUse",
                            "DnsVsSwarmAddressDrift"
                        ],
                        ["ObservationWindow"],
                        "An OOM kill here is usually the cause of everything the layers above reported. Found "
                        + "last it explains them; read first it is undifferentiated noise. " + MonitoringGap),
                    new GraphWorkflowNode(
                        "log-corroboration", "Corroborate with logs, never diagnose from them",
                        "Application error bursts and infrastructure container output, searched against "
                        + "whatever the earlier layers flagged.",
                        Logs, ["ApplicationLogEntryList", "ContainerLogEntryList"],
                        ["ApplicationErrorBurst", "InfrastructureContainerLogLines"],
                        ["ObservationWindow"],
                        "Started from, logs anchor the verdict on whichever subsystem is noisiest rather than "
                        + "whichever failed. HAProxy has no log directive at all, so its absence proves "
                        + "nothing."),
                    new GraphWorkflowNode(
                        "verdict-and-handoff", "State the layer that failed and hand remediation over",
                        "Name the layer, say what was not observable, and hand over. Offer no remediation: "
                        + "there is none to offer from inside the product.",
                        Router, [],
                        ["TriageVerdict", "RemediationHandoff", "PlatformCleared"],
                        [
                            "EtcdQuorumState", "LeaderMember", "PrimaryConnectionCount",
                            "BackendServerStates", "SentinelMasterIdentity", "ContainerStates",
                            "ApplicationErrorBurst"
                        ],
                        "Every remediation for every finding here is outside the product. Offering one, or "
                        + "implying one was performed, is the exact failure this set exists to prevent.")
                ],
                "Sample the wide window before the recent one, then read the stack bottom-up -- consensus, "
                + "leader, database, routing, cache, containers, logs -- because every layer borrows its "
                + "truth from the one below, and a fault read top-down is always attributed to the wrong "
                + "layer."),

            new(
                2,
                "database-leader-and-failover-forensics",
                "Database Leader and Failover Forensics",
                "Who is the leader, did we fail over, why, and what is safe to do next.",
                "Current state, then the authoritative DCS history, then consensus and routing correlation, "
                + "then impact, then an operator switchover and two verification nodes.",
                "Use when leadership, promotion, demotion, split brain, timelines or \"the database moved\" "
                + "is in question. Use connection saturation instead when the primary is known good and "
                + "simply overloaded.",
                [
                    "who is the leader", "did we fail over", "why did patroni promote",
                    "is there a split brain", "which node is primary", "switch the leader back",
                    "the database moved last night", "what's the sync standby"
                ],
                [
                    BaselineThenRecent(EtcdPatroni,
                        ["PatroniMemberStatusList", "PatroniClusterEventList"],
                        "cluster membership and its event history"),
                    new GraphWorkflowNode(
                        "current-topology", "Establish present leadership before any history",
                        "Leader, sync standby, roles, timelines, system identifiers and the leader singleton.",
                        EtcdPatroni, ["PatroniMemberStatusList"],
                        [
                            "CurrentLeader", "CurrentSyncStandby", "MemberRoles", "MemberTimelines",
                            "DatabaseSystemIdentifiers", "ClusterUnlocked", "LeaderSingleton",
                            "MemberLagBytes"
                        ],
                        ["BaselineWindow", "RecentWindow"],
                        "In synchronous mode only the current sync standby is a promotable candidate. A plan "
                        + "naming any other member is refused by Patroni, and the refusal is correct -- this "
                        + "is precisely the concluded-the-platform-was-broken failure. A member whose system "
                        + "identifier differs has diverged and is not a member at all."),
                    new GraphWorkflowNode(
                        "authoritative-failover-history", "Read the DCS history key, not the sampled diffs",
                        "Failover records with reason, timeline and LSN, and the role and timeline change "
                        + "sequences.",
                        EtcdPatroni, ["PatroniClusterEventList"],
                        ["FailoverRecords", "RoleChangeSequence", "TimelineChangeSequence"],
                        ["CurrentLeader"],
                        "Role and timeline rows are derived by diffing one-minute samples and miss any "
                        + "failover that completed inside a single sample. Only the failover rows, from "
                        + "Patroni's own history key, are authoritative; treating a missing role change as "
                        + "\"no failover happened\" is the standard mistake."),
                    new GraphWorkflowNode(
                        "consensus-correlation", "Ask whether the cause was the DCS",
                        "etcd leader changes, health transitions, the raft term at the failover, and fsync "
                        + "and commit latency.",
                        EtcdPatroni, ["EtcdClusterEventList", "EtcdMemberStatusList"],
                        ["EtcdLeaderChanges", "EtcdHealthTransitions", "RaftTermAtFailover", "WalFsyncLatency"],
                        ["FailoverRecords"],
                        "A failover caused by a DCS blip or slow etcd fsync is a consensus incident; "
                        + "remediating the database leaves the cause in place and the next failover follows. "
                        + "The leader lock survives roughly thirty seconds without the DCS, so a shorter blip "
                        + "leaves no Patroni trace at all."),
                    new GraphWorkflowNode(
                        "routing-follow-through", "Measure when routing actually moved",
                        "Backend transition timing, sessions killed at mark-down, and the restored singleton.",
                        Containers, ["HAProxyServerStatusList"],
                        ["BackendTransitionTiming", "SessionsKilledAtMarkDown", "PostFailoverSingleton"],
                        ["FailoverRecords", "CurrentLeader"],
                        "On-marked-down shutdown-sessions means in-flight connections to the demoted primary "
                        + "were killed, not drained, so application errors in that exact window are expected "
                        + "and must not be investigated as a separate fault. " + MonitoringGap),
                    new GraphWorkflowNode(
                        "application-impact", "Size the impact against the routing window",
                        "Throughput dip, invoke warnings and stage latency excursion.",
                        Invoke,
                        [
                            "HttpProcessingCounterList", "ModelInvokeWarningList",
                            "EntityAnalysisModelTaskPerformanceCounterList"
                        ],
                        ["ThroughputDipWindow", "InvokeWarnings", "StageLatencyExcursion"],
                        ["BackendTransitionTiming"],
                        "Measuring impact before establishing when routing moved attributes the whole outage "
                        + "to the failover, including the part caused by whatever triggered it."),
                    new GraphWorkflowNode(
                        "error-corroboration", "Corroborate from logs",
                        "Connection exception bursts and Patroni container output.",
                        Logs, ["ApplicationLogEntryList", "ContainerLogEntryList"],
                        ["ConnectionExceptionBurst", "PatroniContainerLogLines"],
                        ["ThroughputDipWindow"],
                        "Patroni logs at WARNING, so a clean Patroni container log is the normal state and is "
                        + "not evidence of a healthy failover."),
                    Operator(
                        "switchover-execution", "Switch over, at a shell",
                        "patronictl switchover, naming the current leader and the current sync standby.",
                        ["IntendedNewLeader"],
                        ["CurrentSyncStandby", "LeaderSingleton", "MemberLagBytes"],
                        "The product cannot do this and must not imply it can. The command must name the "
                        + "current sync standby; returning leadership to a non-adjacent member needs two "
                        + "sequential switchovers. Never force-update two Patroni services at once -- it "
                        + "costs quorum."),
                    new GraphWorkflowNode(
                        "post-switchover-verification", "Verify the cluster converged",
                        "New leader, timeline agreement across members, residual lag.",
                        EtcdPatroni, ["PatroniMemberStatusList", "PatroniClusterEventList"],
                        ["VerifiedNewLeader", "NewTimelineAgreement", "ResidualLag"],
                        ["IntendedNewLeader"],
                        "A switchover is not finished when the command returns; the old leader must rejoin on "
                        + "the new timeline, and a member still on the old timeline is not yet a replica."),
                    new GraphWorkflowNode(
                        "post-switchover-routing", "Verify routing followed",
                        "The restored primary singleton and how long the backend took to recover.",
                        Containers, ["HAProxyServerStatusList", "HAProxyReachabilityProbeList"],
                        ["RestoredPrimarySingleton", "BackendRecoveryLatency"],
                        ["VerifiedNewLeader"],
                        "Health checks lag the DCS by several seconds on mark-down and mark-up, so checking "
                        + "immediately shows a false outage. " + MonitoringGap)
                ],
                "Establish who holds the leader lock now before reading any history, and read the DCS history "
                + "key before the sampled role diffs, because the sampled diffs can miss a failover entirely "
                + "while the history key cannot."),

            new(
                3,
                "connection-saturation-and-pool-pressure",
                "Connection Saturation and Pool Pressure",
                "Whether the platform is out of database connections, and whether the binding ceiling is the "
                + "proxy, the server or the application's own pool.",
                "Stored trend, then live census, then the proxy's queue, then query shapes, then application "
                + "backlog and runtime pressure, then an operator budget revision.",
                "Use when connections fail immediately, when everything timed out at once, or when pool "
                + "exhaustion is suspected. Use replication lag instead when writes succeed but durability "
                + "or disk is the worry.",
                [
                    "we're out of connections", "connection pool exhausted", "max_connections",
                    "everything timed out at once", "the database is refusing connections",
                    "too many clients already", "why are queries queueing"
                ],
                [
                    BaselineThenRecent(Postgres,
                        ["PostgresMetricList"],
                        "the connection and wait metrics"),
                    new GraphWorkflowNode(
                        "saturation-trend", "Take the stored trend first",
                        "Active connections, waiting backends and deadlocks over the window.",
                        Postgres, ["PostgresMetricList"],
                        ["ActiveConnectionTrend", "WaitingBackendsTrend", "DeadlockTrend"],
                        ["BaselineWindow", "RecentWindow"],
                        "This is the primary's figure read through HAProxy, not per node and not the pool's "
                        + "view, and it is one-minute granular, so a sub-minute exhaustion spike never "
                        + "appears."),
                    new GraphWorkflowNode(
                        "live-backend-census", "Take the live census second",
                        "Backends by application and user, blocking chains, longest transaction age and "
                        + "idle-in-transaction count.",
                        Postgres, ["PostgresActivityList"],
                        [
                            "BackendsByApplicationAndUser", "BlockingChains", "LongestTransactionAge",
                            "IdleInTransactionCount"
                        ],
                        ["ActiveConnectionTrend"],
                        "This tool is a single live instant, not a stored snapshot. Run alone at a quiet "
                        + "moment it proves nothing, which is exactly why the stored trend comes first."),
                    new GraphWorkflowNode(
                        "proxy-queue", "Read the proxy's queue before the database's limit",
                        "Current and queued sessions per server, and check failures.",
                        Containers, ["HAProxyServerStatusList"],
                        ["CurrentSessionsPerServer", "QueuedSessionsPerServer", "CheckFailCounts"],
                        ["BackendsByApplicationAndUser"],
                        "The binding ceiling in this cluster is HAProxy's per-server maxconn, not "
                        + "max_connections, and the primary backend only ever routes to one server. Tuning "
                        + "the database first changes nothing and the symptom moves rather than clears. "
                        + MonitoringGap),
                    new GraphWorkflowNode(
                        "worst-query-shapes", "Look for what is holding the connections",
                        "Worst statement shapes, likely missing indexes and unused indexes.",
                        Postgres,
                        [
                            "PostgresStatementStatisticsList", "PostgresTableStatisticsList",
                            "PostgresIndexStatisticsList"
                        ],
                        ["WorstQueryShapes", "LikelyMissingIndexes", "UnusedIndexes"],
                        ["BlockingChains"],
                        "pg_stat_statements is not preloaded on this cluster, so the statement tool returns "
                        + "nothing and its silence must never be read as no bad queries."),
                    new GraphWorkflowNode(
                        "application-backlog", "Separate cause from consequence",
                        "Archive and case-creation backlogs, and capture queue health.",
                        Queues,
                        [
                            "EntityAnalysisAsynchronousQueueBalanceList",
                            "EntityAnalysisModelAsynchronousQueueBalanceList", "CaptureQueueHealthList"
                        ],
                        ["ArchiveBacklog", "CaseCreationBacklog"],
                        ["ActiveConnectionTrend"],
                        "A growing archive backlog is a consequence of connection starvation as often as a "
                        + "cause of it, and deciding which without the trend is guesswork."),
                    new GraphWorkflowNode(
                        "runtime-pressure", "Rule out thread-pool starvation",
                        "Per-instance thread pool and garbage collection pressure.",
                        RuntimeMetrics, ["DotNetRuntimeMetricList"],
                        ["ThreadPoolPressurePerInstance", "GcPressurePerInstance"],
                        ["ArchiveBacklog"],
                        "Thread-pool starvation and connection starvation present identically from outside "
                        + "the process. This node is per-instance while everything above is cluster-wide."),
                    Operator(
                        "budget-remediation", "Revise the connection budget, at a shell",
                        "Lower the per-replica pool, or raise the server and proxy ceilings, deliberately and "
                        + "together.",
                        ["RevisedConnectionBudget"],
                        ["CurrentSessionsPerServer", "ActiveConnectionTrend"],
                        "A thousand-connection pool per replica across nine application replicas, against an "
                        + "unset max_connections defaulting to a hundred and a per-server proxy maxconn of a "
                        + "hundred, is over-subscribed by roughly two orders of magnitude. Adding replicas "
                        + "without lowering the pool makes it strictly worse.")
                ],
                "Take the stored trend before the live census, and read the proxy's queue before the "
                + "database's limit, because this cluster's binding ceiling is HAProxy's per-server maxconn "
                + "and not max_connections."),

            new(
                4,
                "replication-lag-and-wal-archive-growth",
                "Replication Lag and WAL Archive Growth",
                "Whether standbys are keeping up, whether the synchronous guarantee is still in force, and "
                + "whether WAL is accumulating faster than it is archived.",
                "Patroni's lag view, then the primary's, then WAL generation, then archive-failure evidence "
                + "from two log surfaces, then disk, then operator remediation.",
                "Use when lag, durability, WAL, archiving, pgBackRest or disk growth is the subject. Use "
                + "leader forensics when the question is who is leader.",
                [
                    "replication lag", "is the standby keeping up", "wal is growing",
                    "archive command failing", "are we still synchronous", "disk filling up on the database",
                    "can we still fail over safely"
                ],
                [
                    BaselineThenRecent(EtcdPatroni,
                        ["PatroniMemberStatusList"],
                        "member lag"),
                    new GraphWorkflowNode(
                        "member-lag", "Read Patroni's lag view first",
                        "Lag bytes per member, the receive-versus-replay gap, replay pause and the sync "
                        + "standby's identity.",
                        EtcdPatroni, ["PatroniMemberStatusList"],
                        ["LagBytesPerMember", "ReceiveVsReplayGap", "ReplayPaused", "SyncStandbyIdentity"],
                        ["BaselineWindow", "RecentWindow"],
                        "A member past the maximum lag on failover is silently ineligible for promotion, so "
                        + "the cluster can read as healthy while having no promotable candidate at all."),
                    new GraphWorkflowNode(
                        "primary-replication-view", "Read the sync state before anything else",
                        "Standby states, sync state per standby, and the connected standby count.",
                        Postgres, ["PostgresReplicationStatusList"],
                        ["StandbyStates", "SyncStateByStandby", "ConnectedStandbyCount"],
                        ["LagBytesPerMember"],
                        "Synchronous mode is not strict here, so the primary silently falls back to "
                        + "asynchronous rather than blocking when the sync standby goes. Writes keep "
                        + "succeeding and the durability guarantee is gone with no error anywhere. This is "
                        + "the most dangerous silent state in the cluster and only the sync state reveals it."),
                    new GraphWorkflowNode(
                        "wal-generation-trend", "Measure how fast WAL is being made",
                        "WAL bytes generated, database size and temp bytes, as trends rather than instants.",
                        Postgres, ["PostgresMetricList"],
                        ["WalBytesGeneratedTrend", "DatabaseSizeTrend", "TempBytesTrend"],
                        ["StandbyStates"],
                        "Slot retention is bounded; past the limit the slot is invalidated and the standby "
                        + "needs a full reinit rather than catching up. The trend tells you how long you "
                        + "have, the instantaneous reading does not."),
                    new GraphWorkflowNode(
                        "postgres-log-evidence", "Look for archive failures in the Postgres log",
                        "Checkpoint warnings and archive command failures.",
                        Postgres, ["PostgresLogEntryList"],
                        ["CheckpointWarnings", "ArchiveCommandFailures"],
                        ["WalBytesGeneratedTrend"],
                        "The logging collector is not enabled on this cluster, so this returns nothing at "
                        + "all. It must not be read as no archive failures."),
                    new GraphWorkflowNode(
                        "container-log-evidence", "Look for them in container output instead",
                        "pgBackRest error lines and Patroni archive warnings, by substring.",
                        Logs, ["ContainerLogEntryList"],
                        ["PgBackRestErrorLines", "PatroniArchiveWarnings"],
                        ["ArchiveCommandFailures"],
                        "This is the only surface on which archive command failure is visible on this "
                        + "cluster, and it is stdout scraped rather than structured -- a substring search, "
                        + "not a status. " + MonitoringGap),
                    new GraphWorkflowNode(
                        "disk-headroom", "Establish what is NOT known about disk",
                        "Docker's own accounting, reported as what it is.",
                        Containers, ["DockerHostMetricList"],
                        ["DockerDiskUsagePerHost"],
                        ["PgBackRestErrorLines"],
                        "This reports Docker's image, container and volume accounting, not host filesystem "
                        + "free space and not the backup or Redis mounts. The thing that actually fills -- "
                        + "the write-ahead log when archiving stalls -- is measured by nothing we have. Do "
                        + "not represent this as disk headroom. " + MonitoringGap),
                    Operator(
                        "archive-remediation", "Restore archive flow, at a shell",
                        "Check the stanza, remount the repository, drop an invalidated slot, or reinit a "
                        + "member.",
                        ["RestoredArchiveFlow"],
                        ["PgBackRestErrorLines", "DockerDiskUsagePerHost"],
                        "Outside the product entirely. A stale repository bind writes to local disk silently, "
                        + "which is why the backup script carries a mount tripwire, and the archive push "
                        + "queue will start discarding WAL rather than blocking once its spool limit is "
                        + "passed.")
                ],
                "Read Patroni's lag before the primary's, and read the sync state before anything else, "
                + "because a cluster that has quietly dropped to asynchronous looks identical to a healthy "
                + "one on every other reading."),

            new(
                5,
                "redis-sentinel-failover-verification",
                "Redis Sentinel Failover Verification",
                "Whether Sentinel performed a master switch, which node holds the master role now, and "
                + "whether every client and replica has converged on it.",
                "Event evidence, then topology, then the new master's health, then client and workload "
                + "recovery, then an operator check that no second master can appear.",
                "Use for anything Redis, Sentinel, cache or multiplexer. Entirely independent of the Patroni "
                + "workflows -- do not merge a Redis incident into a database incident.",
                [
                    "did redis fail over", "who is the redis master", "sentinel switched master",
                    "the cache is down", "redis connection failures", "is sentinel at quorum",
                    "redis memory pressure", "cache evictions"
                ],
                [
                    BaselineThenRecent(Redis,
                        ["RedisSentinelEventList", "RedisMetricList"],
                        "the Sentinel event stream and the metrics behind it"),
                    new GraphWorkflowNode(
                        "switch-evidence", "Establish from Sentinel's own events that a switch occurred",
                        "Switch-master events, the subjective and objective down sequence, and failover state "
                        + "transitions.",
                        Redis, ["RedisSentinelEventList"],
                        ["SwitchMasterEvents", "SdownOdownSequence", "FailoverStateTransitions"],
                        ["BaselineWindow", "RecentWindow"],
                        "These are pub/sub captures into a bounded queue, so a dropped switch-master makes a "
                        + "completed failover invisible. Check capture queue health if the sequence looks "
                        + "truncated."),
                    new GraphWorkflowNode(
                        "topology-now", "Read the topology",
                        "Master identity, slave and sentinel counts, the quorum setting, and per-slave link "
                        + "status and lag.",
                        Redis, ["RedisSentinelStatusList"],
                        [
                            "MasterIdentity", "NumSlaves", "NumOtherSentinels", "QuorumSetting",
                            "MasterLinkStatusPerSlave", "ReplicationLagSecondsPerSlave"
                        ],
                        ["SwitchMasterEvents"],
                        "A topology snapshot alone cannot distinguish a failover from a configuration that "
                        + "was always that way. Where the zone variables collapse every sentinel onto one "
                        + "host the quorum figure is arithmetically satisfied and operationally meaningless."),
                    new GraphWorkflowNode(
                        "new-master-health", "Read the new master's condition",
                        "Memory pressure, evictions, persistence state, connected replicas and fragmentation.",
                        Redis, ["RedisMetricList"],
                        [
                            "MemoryPressure", "EvictedKeys", "AofState", "RdbLastSaveAge",
                            "ConnectedReplicas", "FragmentationRatio"
                        ],
                        ["MasterIdentity"],
                        "The configured master's data is a bind mount while the replicas use named volumes, "
                        + "so after a promotion the dataset is in a different place from where the backup "
                        + "procedure looks. \"The master is healthy\" and \"the master's data is backed up\" "
                        + "have become different statements."),
                    new GraphWorkflowNode(
                        "client-recovery", "Establish when clients actually reconnected",
                        "Reconnection timing, queue drain profile and residual failures.",
                        Redis, ["RedisConnectionMultiplexerMetricList", "RedisConnectionEventList"],
                        ["ReconnectionTiming", "QueueDrainProfile", "ResidualFailures"],
                        ["MasterIdentity"],
                        "A configuration-changed broadcast and a failed-then-restored pair are the expected "
                        + "client-side signature of a switch. Counting them as faults double-counts the "
                        + "incident."),
                    new GraphWorkflowNode(
                        "workload-recovery", "Judge latency against the reconnection window",
                        "Per-call-site recovery and slow commands on the new master.",
                        Redis, ["RedisCallCounterList", "RedisSlowOperationList"],
                        ["PerCallSiteRecovery", "SlowCommandsOnNewMaster"],
                        ["ReconnectionTiming"],
                        "The new master is cold, so elevated latency immediately after a switch is warm-up "
                        + "and not a fault. Judging it before the reconnection window is established mis-"
                        + "dates it."),
                    Operator(
                        "old-master-rejoin-check", "Confirm a second master cannot appear",
                        "Check the demoted master rejoined as a replica, and that a service restart will not "
                        + "revert it.",
                        ["ConfirmedSingleMaster"],
                        ["MasterIdentity", "NumSlaves"],
                        "The compose hard-codes the replica role on the replica services and not on the "
                        + "master, so after a promotion a restart of one of those services reverts it to its "
                        + "compose-time role. Sentinel normally reconfigures it, but the window is real, "
                        + "nothing in the product can close it, and a second master is a silent data-loss "
                        + "event.")
                ],
                "Establish from Sentinel's own event stream that a switch occurred before reading topology, "
                + "because a snapshot alone cannot tell a failover from a configuration that never changed."),

            new(
                6,
                "rolling-deploy-and-migration-order",
                "Rolling Deploy and Migration Order",
                "Bringing a new image into the cluster in the one order that works.",
                "Three pre-flight observations, an operator scale of the migrating service alone, a migration "
                + "watch, then the remaining services and three verification nodes.",
                "Use for any deploy, upgrade, image change or stack deploy. Use node drain when the host "
                + "rather than the image is changing.",
                [
                    "deploy the new image", "roll out the update", "scale jube back up",
                    "run the migration", "why is the api failing after deploy", "upgrade jube",
                    "the stack won't come up after a deploy"
                ],
                [
                    new GraphWorkflowNode(
                        "cluster-fit-to-deploy", "Check the cluster can absorb a stop-first update",
                        "Leader singleton, zero lag across members, pending restarts and quorum margin.",
                        EtcdPatroni, ["PatroniMemberStatusList", "EtcdMemberStatusList"],
                        ["LeaderSingleton", "ZeroLagAcrossMembers", "PendingRestartFlags", "EtcdQuorumMargin"],
                        [],
                        "The update config is parallelism one, stop-first, so there is no spare capacity "
                        + "during the update. Deploying onto a member carrying a pending restart or non-zero "
                        + "lag is a stop-first cycle onto a cluster that cannot absorb it."),
                    new GraphWorkflowNode(
                        "synchronisation-baseline", "Record the synchronisation reference before",
                        "The current schedule and its history.",
                        SyncSchedule,
                        [
                            "EntityAnalysisModelSynchronisationScheduleGetCurrent",
                            "EntityAnalysisModelSynchronisationScheduleList"
                        ],
                        ["CurrentSyncReference"], [],
                        "Without the pre-deploy reference there is nothing to compare against afterwards and "
                        + "\"the models didn't resync\" becomes unfalsifiable."),
                    new GraphWorkflowNode(
                        "engine-node-baseline", "Record the node roster and throughput before",
                        "Which engine nodes are registered, and what they were doing.",
                        Queues,
                        [
                            "EntityAnalysisModelSynchronisationNodeStatusEntriesGet",
                            "EntityAnalysisModelProcessingCounterList"
                        ],
                        ["NodeRosterBefore", "PreDeployThroughput"],
                        ["CurrentSyncReference"],
                        "The roster before is the only way to know a replica failed to rejoin after."),
                    Operator(
                        "scale-jobs-first", "Scale the migrating service alone, at a shell",
                        "Bring up only the service that owns the migration, and nothing else.",
                        ["JobsScaled"],
                        ["LeaderSingleton", "ZeroLagAcrossMembers"],
                        "Migration is enabled on one service only. Scaling the API or the UI before the "
                        + "migration completes starts application code against an unmigrated schema. All "
                        + "three services ship at zero replicas for exactly this reason, and this is the "
                        + "ordering constraint most likely to be inferred wrongly."),
                    new GraphWorkflowNode(
                        "watch-migration", "Wait for the migration to finish, in the right log",
                        "Watch for completion or failure.",
                        Logs, ["ApplicationLogEntryList", "ContainerLogEntryList"],
                        ["MigrationCompleted", "MigrationFailure"],
                        ["JobsScaled"],
                        "The application log carries only warnings and errors, so the success signal is in "
                        + "the container log and not in the application log. Waiting for a success row in "
                        + "the wrong tool waits for ever. " + MonitoringGap),
                    Operator(
                        "scale-api-and-ui", "Scale the remaining services, at a shell",
                        "Only once the migration is positively confirmed complete.",
                        ["ApiAndUiScaled"],
                        ["MigrationCompleted"],
                        "Not on a failure, and not on \"no errors seen\" -- the absence of an error is not "
                        + "the presence of a completed migration."),
                    new GraphWorkflowNode(
                        "routing-reregistration", "Verify the proxy picked the new tasks up",
                        "Backends up, DNS drift and restart-loop evidence.",
                        Containers,
                        ["HAProxyServerStatusList", "OverlayNetworkTaskDriftList", "DockerEventList"],
                        ["BackendsUp", "DnsDriftState", "RestartLoopEvidence"],
                        ["ApiAndUiScaled"],
                        "A task whose address changed without DNS catching up looks exactly like an "
                        + "application that will not start, and the drift reading is the only thing that "
                        + "tells the two apart. " + MonitoringGap),
                    new GraphWorkflowNode(
                        "throughput-recovery", "Compare throughput against the baseline, not a threshold",
                        "Post-deploy throughput and any new warnings.",
                        Invoke,
                        [
                            "HttpProcessingCounterList", "ModelInvokeWarningList",
                            "EntityAnalysisModelTaskPerformanceCounterList"
                        ],
                        ["PostDeployThroughput", "NewWarnings"],
                        ["BackendsUp", "PreDeployThroughput"],
                        "Comparing against an absolute threshold instead of the pre-deploy figure turns every "
                        + "quiet period into a failed deploy."),
                    new GraphWorkflowNode(
                        "sync-verification", "Verify every replica rejoined the roster",
                        "The node roster after, against the roster before.",
                        Queues, ["EntityAnalysisModelSynchronisationNodeStatusEntriesGet"],
                        ["NodeRosterAfter"],
                        ["NodeRosterBefore", "ApiAndUiScaled"],
                        "A replica that started but never registered serves traffic with stale models, and "
                        + "the proxy's readiness check will have marked it up regardless.")
                ],
                "Only one service runs the migration, so it goes up alone and nothing else goes up until its "
                + "container log says the migration finished."),

            new(
                7,
                "swarm-node-drain-for-maintenance",
                "Swarm Node Drain for Maintenance",
                "Taking one Swarm node out of service without taking the cluster with it.",
                "Establish what the node hosts and the exposure of each pinned singleton, move leadership "
                + "away and verify it moved, drain, verify ingress, return.",
                "Use when a host is being patched, rebooted or replaced. Use rolling deploy when the image "
                + "rather than the host is changing.",
                [
                    "drain a node", "patch a host", "take a host down for maintenance",
                    "reboot a swarm node", "can I safely restart this host", "node maintenance window"
                ],
                [
                    BaselineThenRecent(Containers,
                        ["DockerHostMetricList"],
                        "host resource use"),
                    new GraphWorkflowNode(
                        "what-the-node-hosts", "Establish what is pinned to the node",
                        "Containers by host and the host's resource profile.",
                        Containers, ["DockerContainerMetricList", "DockerHostMetricList"],
                        ["ContainersByHost", "HostResourceProfile"],
                        ["BaselineWindow", "RecentWindow"],
                        "Every etcd, Patroni, Redis and Sentinel service is pinned by zone label and cannot "
                        + "move: draining does not relocate them, it removes them. No tool reads node labels, "
                        + "so the zone-to-host mapping has to be taken from the environment file by hand. "
                        + MonitoringGap),
                    new GraphWorkflowNode(
                        "consensus-and-database-exposure", "Work out what losing the node costs",
                        "Quorum margin after the loss, and whether the leader or sync standby sits there.",
                        EtcdPatroni, ["EtcdMemberStatusList", "PatroniMemberStatusList"],
                        ["EtcdQuorumMarginAfterLoss", "LeaderOnTargetNode", "SyncStandbyOnTargetNode"],
                        ["ContainersByHost"],
                        "Losing one of five etcd members still holds quorum; losing a second does not. Where "
                        + "the zone variables collapse every member onto one host the margin is zero, and "
                        + "this node must say so rather than report a healthy count."),
                    new GraphWorkflowNode(
                        "cache-exposure", "Work out the cache exposure too",
                        "Whether the Redis master is there, and the sentinel quorum margin after the loss.",
                        Redis, ["RedisSentinelStatusList"],
                        ["MasterOnTargetNode", "SentinelQuorumMarginAfterLoss"],
                        ["ContainersByHost"],
                        "Draining the node holding the Redis master forces a Sentinel failover rather than "
                        + "avoiding one. Better to let it happen deliberately and verify it than discover it "
                        + "mid-drain."),
                    Operator(
                        "move-the-leader-first", "Move leadership off the node, at a shell",
                        "Switch over, and only to the current sync standby.",
                        ["LeadershipMoved"],
                        ["LeaderOnTargetNode", "SyncStandbyOnTargetNode"],
                        "If the sync standby is also on the target node, two sequential switchovers are "
                        + "needed and this step cannot be skipped."),
                    new GraphWorkflowNode(
                        "verify-leadership-moved", "Verify leadership actually moved",
                        "Confirm the leader is no longer on the target node.",
                        EtcdPatroni, ["PatroniMemberStatusList"],
                        ["VerifiedLeaderOffTargetNode"],
                        ["LeadershipMoved"],
                        "A switchover is not finished when the command returns."),
                    Operator(
                        "drain", "Drain the node, at a shell",
                        "Set the node's availability to drain.",
                        ["NodeDrained"],
                        ["VerifiedLeaderOffTargetNode", "EtcdQuorumMarginAfterLoss"],
                        "HAProxy runs in global mode with host-published ports, so draining a node also "
                        + "removes that node's ingress -- anything pointed at that host's address stops "
                        + "working even though the stack is healthy."),
                    new GraphWorkflowNode(
                        "post-drain-verification", "Verify what is left is serving",
                        "Remaining ingress health and DNS drift.",
                        Containers,
                        [
                            "HAProxyServerStatusList", "HAProxyReachabilityProbeList",
                            "OverlayNetworkTaskDriftList", "DockerEventList"
                        ],
                        ["RemainingIngressHealth", "DnsDriftState"],
                        ["NodeDrained"],
                        "DNS drift after a drain is the classic residue: the task is gone, Docker DNS still "
                        + "answers with its address, and the backend never recovers. " + MonitoringGap),
                    Operator(
                        "return-to-active", "Return the node to service, at a shell",
                        "Set availability back to active and bring pinned services back one at a time.",
                        ["NodeActive"],
                        ["RemainingIngressHealth"],
                        "Pinned services do not automatically come back healthy. Force-update individual "
                        + "Patroni services one at a time, never two at once, or quorum is lost.")
                ],
                "Move leadership off the node before draining it, because a drain on the leader is an "
                + "unplanned failover and the point of a maintenance window is that nothing is unplanned."),

            new(
                8,
                "etcd-quorum-health-and-recovery",
                "etcd Quorum Health and Recovery",
                "Whether the consensus layer is healthy, degraded or has lost quorum, and what that costs "
                + "Patroni.",
                "Member census, event history, the Patroni consequence, the write-outage blast radius, "
                + "operator recovery, verification.",
                "Use when etcd, quorum, raft, the DCS or \"nothing can write\" is the subject. If the only "
                + "symptom is a Patroni failover, start at leader forensics, which routes here.",
                [
                    "etcd quorum", "lost the dcs", "nothing can write", "cluster unlocked", "etcd alarm",
                    "raft term keeps changing", "etcd database size", "no space alarm"
                ],
                [
                    BaselineThenRecent(EtcdPatroni,
                        ["EtcdMemberStatusList", "EtcdClusterEventList"],
                        "member status and cluster events"),
                    new GraphWorkflowNode(
                        "member-census", "Ask each member whether it has a leader",
                        "Leader presence per member, raft term and index spread, alarms, database size "
                        + "against size in use, and fsync and commit latency.",
                        EtcdPatroni, ["EtcdMemberStatusList"],
                        [
                            "HasLeaderPerMember", "LeaderIdentity", "RaftTermSpread", "RaftIndexSpread",
                            "Alarms", "DbSizeVsInUse", "WalFsyncLatency", "ProposalsPendingCount"
                        ],
                        ["BaselineWindow", "RecentWindow"],
                        "A member answering healthily while reporting no leader is the signature of quorum "
                        + "loss, not of a dead member, and counting how many are up misses it entirely. A "
                        + "large database size against a small size in use means defragmentation is overdue, "
                        + "and that is what precedes a no-space alarm."),
                    new GraphWorkflowNode(
                        "event-history", "Reconstruct the transitions",
                        "Leader changes, health transitions, alarms raised and cleared.",
                        EtcdPatroni, ["EtcdClusterEventList"],
                        ["LeaderChangeSequence", "HealthTransitions", "AlarmRaisedCleared"],
                        ["LeaderIdentity"],
                        "Derived by diffing one-minute samples, so a quorum blip shorter than a minute is "
                        + "invisible here and shows only as its Patroni consequence."),
                    new GraphWorkflowNode(
                        "patroni-consequence", "Establish what it cost the database",
                        "Unlocked windows, role churn and failover records.",
                        EtcdPatroni, ["PatroniMemberStatusList", "PatroniClusterEventList"],
                        ["ClusterUnlockedWindows", "RoleChurn", "FailoverRecords"],
                        ["HealthTransitions"],
                        "Patroni tolerates roughly thirty seconds without the DCS before the leader lock "
                        + "expires and the primary demotes itself. A shorter blip leaves no Patroni trace at "
                        + "all; a longer one takes the whole cluster read-only."),
                    new GraphWorkflowNode(
                        "write-outage-blast-radius", "Size the write outage",
                        "The outage window and the connection collapse around it.",
                        Postgres, ["PostgresMetricList", "PostgresReplicationStatusList"],
                        ["WriteOutageWindow", "ConnectionCollapse"],
                        ["ClusterUnlockedWindows"],
                        "These are sampled through the proxy, and during a leaderless window the primary "
                        + "backend has no server, so the sampler cannot connect at all. The gap in the data "
                        + "is the finding, not the absence of a problem."),
                    Operator(
                        "defrag-alarm-and-member-recovery", "Recover quorum, at a shell",
                        "Defragment, disarm the alarm, and replace members through the membership API.",
                        ["QuorumRestored"],
                        ["Alarms", "DbSizeVsInUse", "RaftIndexSpread"],
                        "A no-space alarm does not clear itself after a defragmentation -- it must be "
                        + "disarmed explicitly, and until it is the cluster stays read-only while every "
                        + "health reading looks fine. The members are configured with an initial cluster "
                        + "state of new, so a naive full restart can bootstrap a fresh cluster over the old "
                        + "data; member replacement must go through remove and add, never through recreating "
                        + "the service."),
                    new GraphWorkflowNode(
                        "post-recovery-verification", "Verify quorum and the leader lock came back",
                        "Quorum, the leader lock and cleared alarms.",
                        EtcdPatroni, ["EtcdMemberStatusList", "PatroniMemberStatusList"],
                        ["VerifiedQuorum", "VerifiedLeaderLock", "ClearedAlarms"],
                        ["QuorumRestored"],
                        "Patroni will not re-acquire the leader lock instantly, so verifying inside one loop "
                        + "interval shows a false continued outage.")
                ],
                "Read etcd's own view of whether it has a leader before reading anything Patroni says, "
                + "because Patroni's entire state is a function of the DCS and will mislead you about its "
                + "own cause."),

            new(
                9,
                "backup-and-restore-assurance",
                "Backup and Restore Assurance",
                "What can and cannot be known about backup and restore health from inside the product.",
                "A node that states the boundary, two indirect-evidence nodes that are near-empty on this "
                + "cluster, then two operator nodes that do the actual work.",
                "Use whenever backups, pgBackRest, point-in-time recovery, restore, recovery objectives or "
                + "recovery testing come up. It will not answer the question; it will say who can.",
                [
                    "are our backups working", "when was the last backup", "can we restore to yesterday",
                    "pgbackrest status", "test the backups", "what's our recovery point objective",
                    "run a restore drill"
                ],
                [
                    new GraphWorkflowNode(
                        "declare-the-boundary", "Say first what cannot be seen",
                        "State plainly that no tool reads the backup system, before reporting anything that "
                        + "might be mistaken for backup health.",
                        Router, [],
                        ["BackupVisibilityStatement"], [],
                        "There is no backup age, no stanza status, no repository size, no archive queue depth "
                        + "and no restore-drill result anywhere in the catalogue. Any answer about backup "
                        + "health that does not begin by saying this is fabricated."),
                    new GraphWorkflowNode(
                        "indirect-archive-evidence", "Gather what indirect evidence exists",
                        "WAL generation trend, and archive failure lines if the Postgres log were available.",
                        Postgres, ["PostgresMetricList", "PostgresLogEntryList"],
                        ["WalGenerationTrend", "ArchiveFailureLines"],
                        ["BackupVisibilityStatement"],
                        "The logging collector is off here, so the log tool is empty. WAL generation rising "
                        + "without a matching fall is weak inferential evidence of an archiving stall and "
                        + "nothing stronger."),
                    new GraphWorkflowNode(
                        "container-log-evidence", "Scrape the container output",
                        "pgBackRest lines in Patroni container stdout, by substring.",
                        Logs, ["ContainerLogEntryList"],
                        ["PgBackRestStderrLines"],
                        ["WalGenerationTrend"],
                        "This is the entire observability surface for the backup system. It catches loud "
                        + "failures and misses quiet ones, such as a stale mount writing to local disk. "
                        + MonitoringGap),
                    Operator(
                        "run-and-verify-backup", "Run and verify a backup, at a shell",
                        "The backup wrapper already does this well: exclusive lock, split-brain guard, "
                        + "pre-flight check and verification of age and error state.",
                        ["VerifiedBackup"],
                        ["PgBackRestStderrLines"],
                        "Set the mount marker in production or a missing mount silently backs up to local "
                        + "disk."),
                    Operator(
                        "prove-by-restore", "Prove it by restoring, at a shell",
                        "The restore drill is safe by construction -- no network, repository read-only, "
                        + "archiving off.",
                        ["ProvenRestore"],
                        ["VerifiedBackup"],
                        "Override the verification query with one against real business data: a drill that "
                        + "checks database sizes proves the restore ran, not that it is useful. A backup is "
                        + "unproven until a restore has succeeded, and nothing in the product knows whether "
                        + "one ever has.")
                ],
                "State what cannot be seen before reporting anything that can, because every reading here is "
                + "indirect and will otherwise be over-read."),

            new(
                10,
                "jube-application-operational-residue",
                "Jube Application Operational Residue",
                "The flat operational read of the application itself, once the platform beneath it has been "
                + "cleared: errors, container output, engine liveness and throughput, and telemetry.",
                "Six nodes with no required order between them, every one of them conditional on the "
                + "platform having been cleared first.",
                "Use when the platform has already been cleared, or when the question is explicitly about "
                + "the application rather than the infrastructure. If the platform has not been cleared, "
                + "run platform health triage first -- an engine that stopped because the primary moved is "
                + "not an engine fault.",
                [
                    "are there any errors", "check the application logs", "is the engine running",
                    "queue backlog", "how many transactions are we doing", "model response time",
                    "telemetry counters", "any warnings today"
                ],
                [
                    new GraphWorkflowNode(
                        "baseline-then-recent", "Sample wide, then recent",
                        "Take a long sample of the counters first, then the window in question, and carry "
                        + "both forward. This is the one piece of order this workflow has, and it is the "
                        + "piece that makes every reading below it mean something.",
                        Queues, ["EntityAnalysisModelProcessingCounterList", "CaptureQueueHealthList"],
                        ["BaselineWindow", "RecentWindow"],
                        ["PlatformCleared"],
                        "Counters are the whole of this workflow and a counter without a baseline says "
                        + "nothing at all. Errors in the log are the same: a dozen warnings an hour is "
                        + "either the normal background of this deployment or the start of something, and "
                        + "only the wide sample distinguishes them."),
                    new GraphWorkflowNode(
                        "error-surface", "Read the error surface",
                        "Warnings, errors and fatals by logger, and firewall attacks.",
                        Logs, ["ApplicationLogEntryList", "WafAttackList"],
                        ["WarnErrorFatalByLogger", "WafAttackRows"],
                        ["PlatformCleared", "BaselineWindow", "RecentWindow"],
                        "This table only ever holds warnings and above, so an empty result means no errors "
                        + "were logged at that level, not that nothing went wrong."),
                    new GraphWorkflowNode(
                        "container-surface", "Read the container output",
                        "Standard output and error across the containers, by substring.",
                        Logs, ["ContainerLogEntryList"],
                        ["ContainerStdoutStderrLines"],
                        ["PlatformCleared", "BaselineWindow", "RecentWindow"],
                        "Unfiltered across every container on the host, so a substring search is mandatory "
                        + "and a broad window will swamp the context. " + MonitoringGap),
                    new GraphWorkflowNode(
                        "engine-liveness-and-queues", "Read engine liveness and the queues",
                        "Throughput, archive and case-creation backlogs, stage latency and the response-time "
                        + "profile.",
                        Queues,
                        [
                            "EntityAnalysisModelProcessingCounterList",
                            "EntityAnalysisAsynchronousQueueBalanceList",
                            "EntityAnalysisModelAsynchronousQueueBalanceList",
                            "EntityAnalysisModelStagePerformanceCounterList",
                            "EntityAnalysisModelResponseTimePipelineCounterList", "CaptureQueueHealthList"
                        ],
                        [
                            "EngineThroughput", "ArchiveBacklog", "CaseCreationBacklog", "StageLatency",
                            "ResponseTimeProfile"
                        ],
                        ["PlatformCleared", "BaselineWindow", "RecentWindow"],
                        "The engine is disabled on the user interface service, so a zero reading from a UI "
                        + "instance is correct and not an outage."),
                    new GraphWorkflowNode(
                        "invoke-path", "Read the invoke path",
                        "HTTP throughput, invoke warnings and task latency.",
                        Invoke,
                        [
                            "HttpProcessingCounterList", "ModelInvokeWarningList",
                            "EntityAnalysisModelTaskPerformanceCounterList"
                        ],
                        ["InvokeThroughput", "InvokeWarnings", "TaskLatency"],
                        ["PlatformCleared", "BaselineWindow", "RecentWindow"],
                        "Zero processing is the engine-liveness alarm, but it is also simply what a quiet "
                        + "period looks like."),
                    new GraphWorkflowNode(
                        "runtime-metrics", "Read the per-instance runtime",
                        "Runtime profile per instance and the active log-counter rules.",
                        RuntimeMetrics, ["DotNetRuntimeMetricList", "OpenTelemetryLogCounterList"],
                        ["RuntimeProfilePerInstance", "ActiveLogCounterRules"],
                        ["PlatformCleared", "BaselineWindow", "RecentWindow"],
                        "Per-instance, while everything above is cluster-wide; comparing them directly mixes "
                        + "scales."),
                    new GraphWorkflowNode(
                        "telemetry-counters", "Read the telemetry counters",
                        "Aggregated meters and dispatch health.",
                        Otlp, ["OpenTelemetryMetricList", "OtlpDispatchCounterList"],
                        ["AggregatedMeters", "OtlpDispatchHealth"],
                        ["PlatformCleared", "BaselineWindow", "RecentWindow"],
                        "The meters are populated regardless of whether telemetry is enabled, but dispatch "
                        + "health is only meaningful when an external collector is configured, and none is "
                        + "configured in this cluster.")
                ],
                "Clear the platform first, then sample wide before recent -- a counter without a "
                + "baseline is not a reading, it is a number.",
                ["platform-health-triage"])
        ];

        public static IReadOnlyDictionary<string, GraphWorkflow> ById { get; } =
            All.ToDictionary(workflow => workflow.Id, workflow => workflow, StringComparer.Ordinal);
    }
}