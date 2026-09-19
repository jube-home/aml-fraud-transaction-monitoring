---
layout: default
title: Infrastructure Health Metrics
nav_order: 7
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Infrastructure Health Metrics

[Application Log Entry](../ApplicationLogEntry/index.html) captures problems as they're logged. Infrastructure Health
Metrics is the complementary always-on evaluation picture -- a per-minute snapshot of the shared infrastructure a Jube
instance depends on: the .NET runtime it's running in, the Postgres database it's storing to, the Redis cache it's
reading and writing, when present the Patroni/etcd orchestration layer managing Postgres high availability, and the
Docker host (s) it's actually running on (via the
standalone [Jube.Monitoring](#docker-monitoring-sidecar-jubemonitoring)
sidecar). Together they mean diagnosing an incident rarely requires shelling into a node or a database server.

Nothing needs configuring for the .NET/Postgres/Redis tables: sampling starts with the engine and runs regardless of
whether any Model is active, on a dedicated background thread independent of the per-Model counter flush. etcd/Patroni
sampling additionally tries to discover endpoints on its own (see [etcd and Patroni](#etcd-and-patroni) below) --
explicit configuration is only needed if that guesswork doesn't match a given deployment's topology. Noting that Jube
maintains a Docker Swarm Reference Cluster, the monitoring leans towards providing full coverage for that topology.

This page is specifically about *shared infrastructure* Jube depends on. The Archiver and case creation pipelines --
Jube's own background work, not a dependency -- get the equivalent per-minute rollup and warn-threshold capture
treatment in [Background Processing Performance](../BackgroundProcessingPerformance/index.html), including the
`CaptureQueueHealth` table that reports on this page's own `RedisSentinelEvent`/`RedisConnectionEvent` capture queues
alongside those two pipelines'.

## What is captured

Grouped below by the subsystem each table watches -- the .NET runtime, Postgres and Redis all report on themselves;
Patroni/etcd and Docker need a fundamentally different approach, each covered in its own section further down. Most of
these are wide tables -- one row per instance per minute, with a broad, fixed set of columns, rather than one row per
named metric -- since each surface exposes a fixed, well-known metric set that's far easier to chart and correlate as a
wide row than to reassemble from many narrow ones. A handful are narrow, one-row-per-event tables instead, where the
underlying thing being captured is itself discrete (a slow command, a connection event, a Sentinel message) rather than
a snapshot of an always-present value; each is called out as such below.

### The .NET runtime

- **`DotNetRuntimeMetric`** -- GC generation counts, allocated/heap/fragmented bytes, memory load, working set and
  private memory, thread count, ThreadPool worker/IO thread availability and configured maximums, ThreadPool queue
  length, and cumulative CPU time. Sampled entirely from in-process APIs (`GC`, `ThreadPool`, `Process`) -- no
  connection of any kind, so it can never itself fail to reach a dependency.

  This is also where container-level CPU and memory visibility live, split across two independent sources depending on
  what needs no special access at all versus what needs the Docker socket the Monitoring sidecar alone holds:
    - **`RuntimeAvailableMemoryBytes`** / **`RuntimeCommittedMemoryBytes`** -- the .NET GC's own view of its memory
      budget, which has been container-limit-aware since .NET Core 3.0 (it already reads the same cgroup limit below, so
      this is a free, built-in confirmation of what the runtime believes it can use).
    - **`GcPauseTimeMicroseconds`** / **`LockContentionCount`** -- cumulative GC pause time and lock-contention count
      (`GC.GetTotalPauseDuration()`/`Monitor.LockContentionCount`), the two most common causes of "the process is up but
      slow" that neither the container nor Postgres/Redis metrics could ever reveal.
    - **`ContainerCpuLimitCores`**, **`ContainerCpuUsageMicroseconds`**, **`ContainerCpuThrottledPeriods`**, **
      `ContainerCpuThrottledMicroseconds`**, **`ContainerMemoryLimitBytes`**, **`ContainerMemoryUsageBytes`** -- read
      directly from the container's own cgroup pseudo-files under `/sys/fs/cgroup` (cgroup v2, falling back to v1),
      which are visible to any process inside the container without any Docker API access at all.
      `ContainerCpuThrottledPeriods`/`ContainerCpuThrottledMicroseconds` in particular are the direct answer to "is the
      orchestrator's CPU limit actually throttling us" -- non-zero and climbing means the container is CPU-starved by
      its own configured limit, not by host contention. All six are null when not running inside a container (or when
      cgroup pseudo-files aren't reachable for some other reason), never zero, so a null reliably means "not applicable"
      rather than "no throttling happened." True Docker/Swarm-level visibility about that same container -- the kind
      only the daemon itself can see -- is a separate concern entirely, covered under
      [Docker Monitoring Sidecar](#docker-monitoring-sidecar-jubemonitoring) below.

### Postgres

- **`PostgresMetric`** -- active connections, cumulative transactions/rows/temp-file/deadlock/conflict counters
  (Postgres' own `pg_stat_database` counters, stored as-is each interval so a query can diff between rows for a rate), a
  derived cache-hit-ratio percentage (computed from the delta since the previous sample, so it reflects recent behaviour
  rather than the lifetime average), replication status (`IsInRecovery`, `ReplicationLagSeconds`, `ReplicaCount`),
  on-disk database size (`DatabaseSizeBytes`), the single longest-running active query in seconds
  (`LongestRunningQuerySeconds`, null when nothing is active), backends currently waiting on a lock (`WaitingBackends`),
  and cumulative WAL bytes generated (`WalBytesGenerated`, from `pg_current_wal_lsn()`/`pg_last_wal_receive_lsn()` on a
  replica).
- **`PostgresReplicationStatus`** -- one row per connected standby per one-minute sample, read directly from
  `pg_stat_replication` on the primary: `Pid`, `UserName`, `ApplicationName`, `ClientAddress`, `State`, the four LSN
  positions (`SentLsn`/`WriteLsn`/`FlushLsn`/`ReplayLsn`), the three lag measures (`WriteLagSeconds`/`FlushLagSeconds`/
  `ReplayLagSeconds`), and synchronous-replication state (`SyncState`/`SyncPriority`). `PostgresMetric` above only
  carries the aggregate (max lag across every replica) -- this is the per-replica detail behind it, read directly from
  Postgres's own native replication view, with or without Patroni managing the cluster. Empty on a standby, or on a
  primary with no connected replicas.

Postgres's own server log and its live activity/blocking view are big enough topics to earn their own sections further
down -- see [Postgres's own server log](#postgress-own-server-log-postgreslogentry) and
[Live activity and blocking](#live-activity-and-blocking-get-apipostgresactivity) below. Table- and index-level DBA
statistics (`PostgresTableStatistics`/`PostgresIndexStatistics`) are a related but distinct concept -- live catalog
queries, not per-minute history -- documented in their own page,
[Postgres Database Statistics](../PostgresDatabaseStatistics/index.html).

### Redis

- **`RedisMetric`** -- connected/blocked clients, memory usage and configured maximum, ops/sec, cumulative
  commands/connections/evictions/expirations, a derived keyspace hit-rate percentage (same delta-based technique as
  Postgres' cache-hit ratio), replica count, replication offset, uptime, total key count across every logical database
  (`TotalKeys`), memory fragmentation ratio (`MemoryFragmentationRatio`), age of the last successful RDB snapshot
  (`RdbLastSaveAgeSeconds`), and whether AOF persistence is on (`AofEnabled`). Also carries the absolute UTC timestamp
  of the last successful RDB background save (`LastBgSaveDate`, sometimes called a "background copy" -- read directly
  from Redis' own `rdb_last_save_time`) and of the last completed AOF rewrite (`LastAofRewriteDate`) -- comparing the
  two flags up the fork contention that happens when a BGSAVE and an AOF rewrite overlap, since both fork the Redis
  process and compete for the same copy-on-write memory. Redis exposes no absolute last-AOF-rewrite timestamp of its own
  (only the duration of the most recent one), so `RedisMetricSampler` derives it by watching `aof_rewrites` (a monotonic
  counter) advance between samples and stamping that moment -- `LastAofRewriteDate` is therefore null until the first
  rewrite is observed after the sampler starts, even if earlier rewrites happened before then.
- **`RedisSlowOperation`** -- a narrow table dumping Redis' own slow-command log: one row per `SLOWLOG` entry,
  `RedisSlowLogId` (Redis' own monotonic sequence number, used to avoid inserting the same entry twice across
  consecutive one-minute samples of the same rolling buffer), `OccurredDate`, `DurationMicroseconds`, the command and
  its arguments (`Command`), the command verb pulled out of it (`CommandName`, e.g. `SET`, `HGETALL`) so slow commands
  can be grouped by type, a best-effort key name (`KeyName` -- not meaningful for multi-key or scripted commands such as
  `MSET`/`EVAL`), and the issuing client's address/name (when Redis reports them).
- **`RedisConnectionMultiplexerMetric`** -- different from the two tables above in one important way: sampled from the
  application's own live connection, not a dedicated diagnostic one. A per-minute snapshot of
  `Jube.Cache.CacheService`'s actual, real, production `ConnectionMultiplexer` (the one every cache read and write in
  the engine goes through), covering both its interactive (command) connection and its subscription (pub/sub)
  connection: connected/connecting status, endpoint count, cumulative operation count, and for each connection type its
  **queue depth** -- `PendingUnsentItems` (queued but not yet sent), `SentItemsAwaitingResponse` (sent, waiting on
  Redis), `ResponsesAwaitingAsyncCompletion` (received, waiting on the client) -- plus cumulative completed/failed
  operation counts and socket count. A climbing queue depth here means Redis (or the network path to it) isn't keeping
  up with the application, and it shows up here before it shows up as elevated invocation response times. It also
  carries counts of the multiplexer's own diagnostic events -- `ConnectionFailedCount`, `ConnectionRestoredCount`,
  `ErrorMessageCount`, `InternalErrorCount`, `ConfigurationChangedCount`, `ConfigurationChangedBroadcastCount` -- which
  previously only ever reached log4net. Unlike the queue-depth and operation-count columns above, these six are
  **per-minute counts, not cumulative**: each is atomically read and reset to zero at sample time
  (`RedisConnectionDiagnosticsCounters.TakeSnapshot()`), so a row's value is "how many of this event happened in that
  one minute," not a running total since process start -- the question these answer is simply whether one tripped, and
  when. A Sentinel-driven failover typically shows up as a jump in `ConfigurationChangedBroadcastCount` in exactly the
  minute it happened.

  A count only ever answers "did one trip" -- it necessarily discards which endpoint, which connection, and what
  actually failed. **`RedisConnectionEvent`** is the detail behind it: `CacheService`'s event handlers capture every
  individual `ConnectionFailed`/`ConnectionRestored`/`ErrorMessage`/`InternalError`/`ConfigurationChanged`/
  `ConfigurationChangedBroadcast` occurrence (mirroring `RedisSentinelEvent`'s capture-queue-and-bulk-flush shape below)
  into `EventType`, `EndPoint`, `ConnectionType` (Interactive/Subscription, when applicable), `FailureType`, `Origin`
  (InternalError only), `Message` (ErrorMessage only) and `Exception` (full exception text, when the event carried one).
  Every captured event is also logged at Error/Warn, so it reaches `ApplicationLogEntry` too -- `RedisConnectionEvent`
  is the structured, per-event view of the same information, independent of whether Sentinel is in use.

  `RedisConnectionEvent` also carries a seventh `EventType`, **`ReconnectRetry`**, that none of the six above raise.
  `CacheService` configures `ConfigurationOptions.ReconnectRetryPolicy` with `RedisReconnectRetryPolicy`, a thin wrapper
  around the real `ExponentialRetry` policy that observes, rather than changes, every call StackExchange.Redis'
  multiplexer makes to `ShouldRetry` while disconnected -- the exact point the exponential backoff is decided. Each time
  the wrapped policy says a retry is actually due (not the more frequent "not yet" calls made while still waiting out
  the backoff), it captures one row with `RetryCount` (which attempt this is, 1-based, since the connection entered the
  connecting state), `BackoffMilliseconds` (how long the backoff before this attempt actually was) and
  `TransactionsImpacted` (a snapshot of `Jube.Cache.Observability.CacheDiagnostics.InFlightCallCount` -- how many cache
  calls, across every repository, were awaiting completion at that instant, i.e. how many transactions this outage was
  stalling). This is what turns "Redis went away for a while" into "it took N retries growing to M ms apart, stalling as
  many as K in-flight calls at the worst point" -- the retry/backoff/impact story a bare `ConnectionFailed`/
  `ConnectionRestored` pair cannot tell on its own.
- **`RedisCallCounter`** -- one row per distinct `Call` label (a `Jube.Cache.Redis` repository method, e.g.
  `"CachePayloadRepository.InsertAsync"`, never a Redis key or value) per one-minute window, with `Count`,
  `TotalMicroseconds`, `MinMicroseconds` and `MaxMicroseconds` for every call to that label during the window. This is
  the dedicated, reset-each-minute counterpart to `CacheDiagnostics`' live `jube.cache.redis.call.count`/
  `jube.cache.redis.call.duration` OpenTelemetry instruments -- same source (`CacheDiagnostics.Record`, called from
  every repository method's `RecordAsync`/`Record` wrapper), same `Call` label, but accumulated in
  `Jube.Cache.Observability.CacheCallCounters` (a global `ConcurrentDictionary<string, Accumulator>`, not per-model,
  since Redis calls are not scoped to any one `EntityAnalysisModel`) and read-and-reset once a minute by the sampling
  group covering it (see [Sampling architecture](#sampling-architecture-fan-out-join-and-opentelemetry) below), the same
  `Interlocked.Exchange` idiom `ManageCountersStarter` already uses for the per-model Task/Stage/Response Time Pipeline
  counters. Answers, directly and without needing an external OTel collector, exactly where Redis load is coming from by
  area of the system -- which repository method, how often, and how slow.
- **`RedisSentinelStatus`** -- one row per entity (`master`/`slave`/`sentinel`) reported by `SENTINEL MASTERS`/
  `SENTINEL SLAVES`/`SENTINEL SENTINELS` per one-minute sample, taken over the same Sentinel connection
  `Jube.Cache.CacheService` already holds when the Redis connection string configures a `ServiceName`: `EntityType`,
  `Name`, `Ip`/`Port`, `Flags` (Sentinel's own state flags -- watch for `s_down`, `o_down`, `failover_in_progress`),
  and, for slave rows, `MasterLinkStatus`/`MasterHost`/`MasterPort`/`SlaveReplOffset`. Only populated when Sentinel is
  actually in use.

  Sentinel's own replies carry a raw replication offset for each slave but no lag *time* -- only the actual master's own
  `INFO replication` reply has that (a `slaveN:...,lag=N` field, seconds since that replica's last ACK). So on each
  cycle, the same dedicated Redis connection `RedisMetricSampler` already holds is also asked for `INFO replication`,
  and the per-replica lag it reports is matched onto the corresponding `RedisSentinelStatus` slave row by `ip:port`,
  populating **`ReplicationLagSeconds`** (null for master/sentinel rows, or if the master couldn't be reached that
  cycle). This is the most direct "is a replica falling behind" signal Sentinel-monitored Redis offers.
- **`RedisSentinelEvent`** -- `RedisSentinelStatus` above is a once-a-minute poll, which can miss a transition that
  happens and resolves between two samples; this fills that gap. `CacheService` subscribes to every channel Sentinel
  publishes on (the `"*"` pattern -- `+sdown`, `-sdown`, `+odown`, `-odown`, `+switch-master`, `+failover-state`,
  `+slave`, `+sentinel`, and any future channel type), captures each one the moment it happens into a bounded in-memory
  queue (mirroring `ApplicationLogEntry`'s capture-and-bulk-flush shape exactly), and
  `InfrastructureHealthMetricsStarter` bulk-inserts whatever accumulated each minute. Each row is just `OccurredDate`,
  `Channel` and `Message` (Sentinel's raw payload for that channel). Every captured event is also logged at Warn, so it
  reaches `ApplicationLogEntry` too via the same log4net capture appender -- `RedisSentinelEvent` is the structured,
  Sentinel-specific view of the same information. Only populated when Sentinel is actually in use.

## etcd and Patroni

Everything above is Postgres or Redis reporting on itself. If a deployment runs Postgres under Patroni (with etcd as the
DCS), neither Postgres nor Redis has any concept of that orchestration layer at all -- Patroni's own failover decisions,
and etcd's own raft consensus health, were a complete blind spot until now.

### Discovery

`EtcdPatroniDiscovery` finds candidate endpoints for each cluster type, in this order:

1. **An explicit list** -- set `EtcdEndpoints` and/or `PatroniEndpoints` to a comma-separated `host:port` list. Always
   correct for a real deployment, since guessing topology from hostnames is inherently fragile; when set, discovery
   below is skipped entirely for that cluster type.
2. **DNS hostname-prefix probing**, when no explicit list is set: try resolving `"{EtcdDiscoveryPrefix}1"` through
   `"{EtcdDiscoveryPrefix}{EtcdPatroniDiscoveryMaxNodes}"` (defaults: prefix `etcd`, 5 nodes) and likewise
   `PatroniDiscoveryPrefix` (default `patroni`) -- the naming convention used by the Patroni project's own reference
   docker-compose demo (`etcd1`/`etcd2`/`etcd3`, `patroni1`/`patroni2`/`patroni3`), and a reasonable default for anyone
   following it. A plain DNS lookup from inside the container is enough for this to work under Docker Compose, Swarm, or
   Kubernetes (each resolves service names this way) -- no Docker socket, no orchestrator API. `EtcdClientPort` (default
   `2379`) and `PatroniApiPort` (default `8008`) control which port is paired with each resolved hostname.

Re-run every one-minute cycle rather than cached, so a cluster that grows a member is picked up without restarting Jube.
A hostname that never resolves is simply never sampled -- neither table is populated at all when etcd/Patroni aren't in
use.

### `EtcdMemberStatus`

One row per etcd cluster member per one-minute sample, polled directly over each member's own HTTP API (`GET /version`,
`GET /health`, `POST /v3/maintenance/status`, `POST /v3/cluster/member/list`, `POST /v3/maintenance/alarm`,
`GET /metrics`) -- every field name was checked against a live etcd 3.5 instance during development, not taken from
documentation alone:

- **Identity** -- `Endpoint`, `MemberId`, `Name`, `PeerUrls`, `ClientUrls`, `IsLearner`, `Version`, `ClusterVersion`.
- **Consensus health** -- `HealthOk`/`HealthReason`, `LeaderId` (compare across every member's row in the same minute to
  catch disagreement -- a split-brain symptom), `IsLeader` (this member's own id matches `LeaderId`), `HasLeader`,
  `LeaderChangesTotal`, `RaftIndex`/`RaftTerm`/`RaftAppliedIndex`.
- **Throughput** -- `ProposalsCommittedTotal`/`ProposalsAppliedTotal`/`ProposalsPendingCount`/`ProposalsFailedTotal`.
- **Performance** -- `WalFsyncAvgMicroseconds`/`BackendCommitAvgMicroseconds` (etcd's own cumulative sum/count from
  `/metrics`, converted to an average; etcd's own guidance treats sustained values above roughly 10ms as a disk that
  can't keep up), `SlowApplyTotal`/`SlowReadIndexesTotal`.
- **Capacity and alarms** -- `DbSizeBytes`/`DbSizeInUseBytes`, `AlarmCount`/`Alarms` (a non-zero `AlarmCount` -- e.g.
  `NOSPACE` when the backend quota is exceeded, `CORRUPT` on detected data corruption -- means etcd itself is refusing
  some operations).

`LeaderChangesTotal` and every `*Total` proposal/slow-operation counter are etcd's own cumulative counters (`/metrics`
is never reset by Jube), unlike `RedisConnectionMultiplexerMetric`'s per-minute event counts above -- diff two rows for
a rate.

### `PatroniMemberStatus`

One row per Patroni-managed Postgres member per one-minute sample. `GET /cluster` (asked of whichever
configured/discovered endpoint answers first) gives the whole cluster's topology in one call -- name, role, state,
host/port, timeline and, for replicas, lag already computed by Patroni itself -- the Patroni-native equivalent of Redis
Sentinel's three-call `SENTINEL MASTERS/SLAVES/SENTINELS`. `GET /patroni` is then called once per member for the detail
`/cluster` doesn't carry:

- **Topology** (from `/cluster`) -- `Role` (`leader`/`replica`/`standby_leader`), `State`, `Host`/`Port`, `TimelineId`,
  `LagBytes` (null for the leader, or when Patroni couldn't compute one).
- **Detail** (from `/patroni`) -- `PatroniVersion`, `Scope`, `PostgresServerVersion`, `DatabaseSystemIdentifier` (every
  healthy member should report the same value -- one that doesn't has diverged), `XlogLocationBytes`/
  `ReceivedLocationBytes`/`ReplayPaused`, `PendingRestart`, `ClusterUnlocked` (true is a red flag, normally only seen
  mid-failover).
- **`SyncStandby`** -- derived, not reported directly by either endpoint: the leader's own `GET /patroni` response
  carries a `replication` array showing each connected replica's `sync_state`, which is matched onto the corresponding
  member's row by name after every endpoint has been sampled that cycle.

Field names for `PatroniMemberStatus` come from Patroni's own documented REST API rather than a live-verified instance
(standing up a full Patroni+Postgres+etcd cluster was disproportionate to one sampler) -- every read is defensive, so an
unexpected shape degrades to a null column rather than throwing.

### `EtcdClusterEvent` and `PatroniClusterEvent`

`EtcdMemberStatus`/`PatroniMemberStatus` are a once-a-minute poll -- to see "what happened", not just "what is the state
right now", each sampler additionally diffs every new sample against the previous one it took for the same
endpoint/member and emits one row per meaningful change:

- **`EtcdClusterEvent`** -- `LeaderChanged` (this member's own view of who the raft leader is changed -- compare across
  members in the same minute to catch disagreement), `HealthChanged` (`GET /health` flipped ok/not-ok), `AlarmRaised`/
  `AlarmCleared`. Verified live: activating and clearing a `NOSPACE` alarm against a real etcd instance correctly
  produced both an `AlarmRaised`/`AlarmCleared` row and a paired `HealthChanged` row (etcd stops serving some requests
  while an alarm is active, so the two are genuinely correlated).
- **`PatroniClusterEvent`** (diff-based) -- `RoleChanged` (leader/replica/standby_leader), `StateChanged`,
  `TimelineChanged` (a member moved to a new Postgres timeline, typically just after a failover).

Diffing only catches a change that survives from one one-minute sample to the next, and only knows *that* something
changed, not *why*. For Patroni, there is a better source: Patroni itself permanently records every failover and
switchover -- reason, timeline, WAL position, timestamp, and the new leader's name -- in its DCS under a `history` key.
Since Patroni's own REST API doesn't expose that key, **`PatroniClusterEvent`** additionally reads it directly out of
etcd (`POST /v3/kv/range`, base64-decoded per etcd's JSON gateway convention) whenever etcd is also
configured/discoverable, producing `EventType` `Failover` rows with `Reason`, `TimelineId` and `LsnBytes` populated and
`PreviousValue`/`NewValue` giving the leader that was replaced and the one that took over. Verified live against a real
etcd instance, including confirming a second read doesn't re-emit history entries already seen (tracked in memory by
`"{timeline}:{lsn}"`, the natural unique key for one historical transition) -- and catching a real bug in the process:
Json.NET's default JSON parser auto-detects ISO-8601-looking strings and silently corrupts them on the way back out as
plain strings, so the history reader parses with `DateParseHandling.None` and does its own explicit, invariant-culture
timestamp parsing instead.

Both event tables reset their "have we seen this" state on a Jube restart -- `EtcdClusterEvent`'s diffs simply
re-baseline (a change that happens to straddle a restart is missed, not mis-reported), while `PatroniClusterEvent`'s
`history` reader re-emits every entry currently in Patroni's history once, the same accepted one-time-replay tradeoff
`RedisSlowOperation` already makes for Redis' own `SLOWLOG` buffer after a restart.

## Docker Monitoring Sidecar (Jube.Monitoring)

Everything above is sampled from inside the Jube process itself. Docker host/container metrics are the one exception:
reading them properly needs the Docker Engine API, which needs the Docker socket (`/var/run/docker.sock`) mounted into
whichever container reads it -- and Jube.App must never hold that mount, since it's a much larger blast radius than the
narrow, host-local visibility this is meant to provide. `Jube.Monitoring` exists to resolve that: a small, standalone
console process/image (never a module inside Jube.App/Jube.Engine), deployed one instance per Docker host, that polls
the Docker Engine API directly over the socket and writes straight into the same shared Postgres database Jube itself
uses.

It shares `Jube.Data`'s `Poco`/`DbContext`/`Repository` classes with the rest of the system rather than defining its own
copies, specifically to avoid the two independently-maintained-schema class of bug -- the one real cost is pulling in
`Jube.Data`'s transitive dependencies (`Accord.*` included), which only grows the image's disk footprint since the .NET
runtime never loads an assembly it doesn't call into. It has no dependency on `DynamicEnvironment`, log4net, or anything
else from Jube.App/Jube.Engine: configuration is read directly from five environment variables (`ConnectionString`;
`DockerSocketPath`, defaulting to `/var/run/docker.sock`; `SampleIntervalSeconds`, defaulting to `60`;
`HAProxyStatsUrl`, defaulting to `http://haproxy:7000/;csv`; and `HAProxyTasksHostname`, defaulting to
`tasks.haproxy` -- the latter two below), and it talks to the Docker Engine API over a Unix domain socket `HttpClient`
(the same `SocketsHttpHandler.ConnectCallback` pattern Docker.DotNet uses internally) rather than any client SDK,
consistent with how this suite calls etcd/Patroni's own HTTP APIs directly above. A failed sample or write is logged to
the console and simply retried on the next cycle.

### `DockerContainerMetric`

A narrow table -- one row per container per one-minute sample, filtered by `OccurredDate` -- covering every container on
the host, in any state:

- **Identity and state** -- `ContainerId`, `Name`, `Image`, `State`, `Status` (Docker's own strings), `HealthStatus`
  (Docker `HEALTHCHECK` status, or `none` when the image defines none).
- **Lifecycle** -- `RestartCount`, `OomKilled`, `ExitCode`, `StartedAt` -- all only available via Docker's
  container-inspect endpoint, not the container list or stats endpoints.
- **CPU and memory** -- `CpuUsagePercent` (computed the same way `docker stats` computes it: Docker's own non-streaming
  stats reply already carries both the current and previous sample in one call, so no state needs to be kept across
  polling cycles), `OnlineCpus`, `MemoryUsageBytes`/`MemoryLimitBytes`/`MemoryPercent` (raw usage including reclaimable
  page cache, so this reads a little higher than `docker stats`' own cache-adjusted figure).
- **Network and block I/O** -- `NetworkRxBytes`/`NetworkTxBytes` (summed across every network interface the container
  has), `BlockReadBytes`/`BlockWriteBytes` (summed from cgroup block-io accounting).
- **Process count** -- `PidsCurrent`/`PidsLimit`.

### `DockerHostMetric`

A wide table -- one row per host per one-minute sample, filtered by `CreatedDate` (there is no `OccurredDate` on this
one, the same convention `PostgresMetric`/`RedisMetric` use):

- **Counts** -- `ContainersTotal`/`ContainersRunning`/`ContainersPaused`/`ContainersStopped`, `ImagesCount`.
- **Host identity** -- `NCpu`, `MemTotalBytes`, `DockerVersion`, `ApiVersion`, `KernelVersion`, `OperatingSystem`,
  `OsType`, `Architecture`.
- **Disk usage**, from `GET /system/df` (which carries no pre-aggregated totals of its own -- every one of these is a
  manual sum over that endpoint's arrays) -- `LayersSizeBytes`, `ImagesSizeBytes`, `ReclaimableImagesBytes` (images
  referenced by zero containers -- candidates for `docker image prune`), `ContainersDiskBytes` (sum of every container's
  writable layer), `VolumesSizeBytes` (sum of every volume that has computed usage data), `BuildCacheSizeBytes`.

### `ContainerLogEntry`

The other side of "never need to log into the box": every container's own stdout/stderr, from every container on the
host, no name filtering -- captured via `GET /containers/{id}/logs`, the same Docker Engine API endpoint `docker logs`
itself calls, over the same socket `Jube.Monitoring` already holds for the two tables above. This is what actually
closes the gap for etcd, Patroni and Redis/Sentinel: none of the three expose their own process logs over any API or
wire protocol of their own (see the note under "Sampling architecture" above), but Docker's own log stream is available
for every one of them regardless, since Docker itself is what's actually holding onto that output.

- **Every container, deliberately unfiltered** -- the same "every container, no exceptions" choice
  `DockerContainerMetric` already makes, so there is no per-system name list to keep in sync with whatever a given
  deployment actually runs. This does mean Jube's own containers' console output lands here too, alongside (not instead
  of) `ApplicationLogEntry`'s log4net-sourced capture -- a genuinely different source (raw stdout vs. structured log4net
  events, useful for e.g. a crash before log4net itself has initialised) rather than a duplicate worth filtering out.
- **Incremental, not a re-read each cycle** -- Docker's `logs` endpoint accepts a `since` parameter (a UNIX timestamp,
  with nanosecond precision as `seconds.nanoseconds`); each container's last-ingested-line timestamp is tracked in
  memory (per Docker container id, not name -- a recreated container gets a new id and correctly starts fresh) and the
  next poll asks for `since = last + 1ns`. Confirmed live against a real Docker Engine that `since` is *inclusive* --
  the exact boundary nanosecond is returned again on a repeat request -- so the `+1ns` is required, not defensive-only.
  On the *first* cycle a given container id is seen, the cursor starts at "now" rather than backfilling that container's
  entire retained log history.
- **Multiplexed stream demuxing** -- when a container is not running with a TTY (true for every container this targets),
  Docker interleaves stdout and stderr in one byte stream framed as 8-byte headers
  (`[stream_type, 0, 0, 0, big-endian uint32 length]`) followed by exactly that many payload bytes;
  `DockerApiClient.GetContainerLogsAsync` demuxes this into separate stdout/stderr byte buffers (concatenated before
  UTF-8 decoding, since both a log line and a multi-byte UTF-8 character can straddle two frames) before splitting into
  individual lines -- all of this confirmed by inspecting raw response bytes against a real Docker Engine, not taken
  from documentation alone.
- **Timestamp parsing** -- every line carries Docker's own RFC3339Nano prefix (`timestamps=true` is always requested),
  parsed into `OccurredDate` and stripped from `Message`; a line that doesn't match the expected 30-character prefix
  shape falls back to storing the whole line as `Message` with no `OccurredDate`, rather than dropping it.
- **Purged, not permanent** -- included in the same weekly purge cycle as every other table in this suite (unlike
  `UserLogin` and `UserLogout`, which are permanent audit trails and deliberately excluded from purging -- see the
  Purging section below for that distinction).

### `DockerEvent`

Fills the gap `DockerContainerMetric`'s once-a-minute snapshot leaves: a container that was OOM-killed and restarted
between two polls shows up there only as an incremented `RestartCount`, with no record of exactly when or why.
`GET /events` (the same Docker Engine API endpoint `docker events` itself calls) carries that as a discrete, timestamped
fact instead -- container start/stop/die/kill/oom/health_status, network connect/disconnect, volume mount/unmount, image
pulls, and (in Swarm scope) service/node/secret/config events too. The same "every container, no filtering" choice as
`ContainerLogEntry` -- one host-wide event timeline, not scoped to any particular container name.

- **`EventType`/`Action`/`ActorId`/`ActorName`/`Scope`** -- taken directly from Docker's own event JSON (`Type`,
  `Action`, `Actor.ID`, `Actor.Attributes.name`, `scope`). `Scope` is `local` or `swarm`.
- **Incremental, same mechanism as `ContainerLogEntry`** -- `GET /events` accepts `since`/`until` (both required here,
  since omitting `until` switches the endpoint into a long-poll/streaming mode this sidecar's once-a-cycle polling model
  can't use); confirmed live that `since` is inclusive at nanosecond precision the same way the logs endpoint is, so the
  next poll asks for `since = last + 1ns`, `until = now`. One shared cursor, not per-container -- `/events` is a single
  host-wide timeline covering every actor (container, network, volume, image, ...), unlike `ContainerLogEntry`'s
  per-container log streams. Starts at "now" on the first cycle, no backfill.
- **`docker exec` audit trail, and a caveat that comes with it** -- `exec_create`/`exec_start`/`exec_die` events fire
  whenever anyone runs `docker exec` against a container, and Docker's own `Action` string for `exec_create`/
  `exec_start` embeds the *exact command line* verbatim (confirmed live: `"exec_create: psql -U postgres -c ..."`). This
  is a genuine audit signal -- did anyone bypass the application and touch a container directly, and with what
  command -- but it cuts both ways: a command line containing a secret as a bare argument (e.g.
  `psql -c "ALTER USER x PASSWORD 'y'"`) would be captured here too, in plain text, queryable by anyone who can browse
  this table (the landlord). This is an existing property of the Docker Engine API itself, not something this table
  introduces or could reasonably filter out (there is no reliable way to distinguish "a command line with a secret in
  it" from any other command line) -- treat it the same way you'd treat shell history on the host itself.
- **Purged, not permanent** -- same weekly cycle as `ContainerLogEntry`, for the same reason (this is operational
  visibility, not the `UserLogin` and `UserLogout` audit trails).

### `HAProxyServerStatus`

Everything above monitors Jube's own process or the Docker host underneath it; nothing watches HAProxy itself, even
though every request to `jube-ui`/`jube-api` and every Postgres connection passes through it first.
`HAProxyServerStatus`
closes that gap by scraping HAProxy's own stats CSV (`http://haproxy:7000/;csv` by default -- the `HAProxyStatsUrl`
environment variable, matching the reference topology's `haproxy.cfg`, which uses `stats uri /`, not `/stats` as some
HAProxy documentation examples assume) once per sample cycle and writing one row per individual server slot -- the
`FRONTEND`/`BACKEND` aggregate rows are skipped, since a single dead slot is the signal this table exists to catch, not
the backend-wide totals.

- **Identity and health** -- `PxName`/`SvName` (the proxy and server-slot names, e.g. `jube_ui`/`jube-ui-3`), `Status`
  (`UP`/`DOWN`/`MAINT`/...), `Addr` (the IP:port HAProxy currently has resolved for that slot -- the field that proves
  or disproves "HAProxy thinks a slot is live at an address nothing is listening on anymore"), `CheckStatus`/`CheckCode`
  (the last health check's result and HTTP status).
- **Flapping and load** -- `ChkFail`/`ChkDown` (cumulative failed checks and down-transitions), `LastChg` (seconds since
  this slot's status last changed -- a slot cycling status every few seconds shows a small, unstable `LastChg`
  rather than one clean failure), `Scur`/`Qcur` (current sessions/queued requests), `Weight`/`Act`/`Bck`.
  `Hrsp2Xx`/`Hrsp5Xx` are populated for `jube_ui`/`jube_api` (HTTP mode) and left null for `postgres_primary`/
  `postgres_replicas` (TCP mode).
- **Field lookup by CSV header name, not positional index** -- HAProxy's stats CSV has grown new trailing columns across
  versions; `HAProxyServerStatusSampler` parses the header row into a name-to-index map rather than hardcoding column
  positions, so it keeps working across an HAProxy upgrade that adds columns.

### `HAProxyReachabilityProbe`

`HAProxyServerStatus` above is HAProxy's own view of its backends; this table is the client's-eye view instead --
synthetic checks against HAProxy from the outside, the one thing `/api/ready` structurally cannot see, since that
endpoint only proves a container can answer on its own loopback. Every sample cycle, `Jube.Monitoring` resolves
`tasks.haproxy` by default (the `HAProxyTasksHostname` environment variable; Swarm's per-task DNS convention, returning
every individual HAProxy replica's address directly rather than the `haproxy` hostname's own VIP/IPVS-balanced address)
and probes each replica on its own:

- **`JubeUi`/`JubeApi`** -- a real `GET /api/ready` issued straight at that replica's `5001`/`5002` port, exercising the
  same routed path a genuine client request takes. `HttpStatusCode` and `ConnectMicroseconds` (start of the connection
  attempt to success or failure) are recorded either way.
- **`PostgresPrimary`/`PostgresReplica`** -- a bare TCP connect to that replica's `5432`/`5433` port; a full login would
  need credentials this sidecar has no reason to hold beyond what it already uses for its own writes.
- **Per-replica attribution** -- resolving `tasks.haproxy` instead of `haproxy` means a problem specific to one node's
  HAProxy (or the overlay path to it) shows up as "this one replica fails, the others don't" in `HAProxyAddress`, rather
  than being averaged away by the VIP's own load balancing across a shared hostname.
- **`Success`/`ErrorMessage`** -- `ErrorMessage` (the exception message, truncated to 1024 characters) is populated only
  on failure; a timed-out TCP connect after 5 seconds is treated as a failure like any other.

### `OverlayNetworkTaskDrift`

The one signal genuinely new to this whole suite: does Docker's embedded DNS agree with what Swarm's scheduler actually
has running? HAProxy's `server-template` directive (and anything else relying on DNS to discover a service's current
tasks) depends entirely on that agreement holding -- a stale DNS answer pointing at a task that was already rescheduled
is exactly the overlay-network/DNS desync class of bug this table exists to catch, independently of whether any health
check has caught up to it yet.

- **Manager-only** -- the Swarm Tasks API is only answerable from a manager node's Docker socket; each
  `Jube.Monitoring` instance checks `Swarm.ControlAvailable` via `GET /info` first and produces no rows at all when
  running on a worker node, rather than reporting a false "no tasks" drift.
- **Always `tasks.<ServiceName>`, never the plain service name, for every service checked** -- `jube-ui`/`jube-api`
  (`dnsrr` mode) happen to resolve the same way either form is asked, but every other service in the reference topology
  (`haproxy`, `patroni1`-`4`, `redis-master`/its replicas, `sentinel1`-`5`, `etcd1`-`5`) uses the default VIP endpoint
  mode, where the plain service name resolves to a virtual IP that IPVS transparently redirects -- never the task's own
  real overlay address. Comparing a VIP against the Swarm API's real task addresses would report every such service as
  permanently "inconsistent" and defeat the point; `tasks.<ServiceName>` bypasses the VIP uniformly for every service,
  VIP-mode or not, so the comparison is meaningful across the board.
- **`DnsResolvedAddresses`/`SwarmTaskAddresses`/`AddressesOnlyInDns`/`AddressesOnlyInSwarm`** -- the two full address
  sets (comma-joined) and their two one-sided differences. A non-empty `AddressesOnlyInDns` is a stale DNS answer for a
  task that no longer exists; a non-empty `AddressesOnlyInSwarm` is a newly-scheduled task DNS hasn't caught up to yet.
  `IsConsistent` is `true` only when both differences are empty -- the headline column for spotting drift at a glance.

### Deployment

One `Jube.Monitoring` instance is needed per Docker host, each with that host's own socket mounted -- a single instance
can only ever see the daemon it's socket-mounted to, so a multi-host deployment needs one per host, not one shared
instance. The root `docker-compose.yml` includes a `jube-monitoring` service for the single-host case;
`Jube.Cluster/docker-compose.yml` (the Swarm reference topology) deploys it with `mode: global` so every node -- manager
or worker -- runs its own instance reporting on its own local Docker daemon.

## Dedicated diagnostic connections

The Postgres and Redis *server* samples (`PostgresMetric`, `RedisMetric`, `RedisSlowOperation`) are taken over
connections dedicated to this purpose, entirely separate from the application's own pooled connections (`Jube.Data`'s
connection pool, and `Jube.Cache.CacheService`'s own Redis multiplexer). This is deliberate: diagnostics need to keep
working even when the application's own connections are the thing in trouble, which is exactly the kind of incident
these tables exist to help diagnose. The Redis diagnostic connection additionally sets `AllowAdmin`, since `INFO` and
`SLOWLOG` are admin-only commands and the application's own production connection deliberately does not set that flag.
`RedisConnectionMultiplexerMetric` is the deliberate exception: it samples the application's *own* connection precisely
because that connection's health is the thing being measured.

## Postgres's own server log (`PostgresLogEntry`)

The other half of "never need to log into the box": Postgres's own server log -- checkpoints, connection issues,
deadlocks, crashes, slow autovacuum, and everything else Postgres itself writes about its own operation -- tailed over
plain SQL, no filesystem or Docker-socket access needed. `pg_ls_logdir()` finds the current log file (name, size,
modification time) and `pg_read_file(path, offset, length)` reads exactly the bytes written since the last cycle; both
are confirmed live against a real Postgres instance to return genuine log content, including real `LOG`/`ERROR`/
`STATEMENT` lines.

- **Prerequisite: `logging_collector = on`** -- off by default in vanilla Postgres. When off, `PostgresLogSampler`
  reports `LoggingCollectorOff` and the flush cycle skips cleanly (one WARN logged the first time, never repeated)
  rather than treating it as an error; the table simply stays empty until the setting is turned on and Postgres is
  restarted (`logging_collector` is not reloadable). The connecting role also needs `pg_read_server_files` or superuser,
  the same privilege level every other sampler in this suite already assumes.
- **Incremental tailing, not a re-read each cycle** -- the first cycle after this starts records the *current* end of
  the current log file as its starting offset rather than replaying whatever history is already in it (a Postgres log
  file has no bound on how large that history could be, unlike Patroni's own small JSON history array, which this suite
  *does* fully replay once -- see `PatroniClusterEvent` above). Every subsequent cycle reads only the delta since the
  last one. Rotation onto a new filename resets the tracked offset to zero for the new file (any final unread bytes of
  the old file are acceptably lost, the same best-effort tradeoff this whole suite makes elsewhere); the same filename
  shrinking below the tracked offset (a fixed-name rotation scheme reusing a name from scratch) does the same.
- **Line parsing** -- Postgres's default `log_line_prefix` (`%m [%p] `) gives
  `<timestamp with ms and timezone name> [<pid>] <LEVEL>:  <message>`; each line matching that shape becomes one row
  (`OccurredDate`, `Pid`, `Level`, `Message`). A deployment running a different `log_line_prefix` doesn't break this --
  lines that don't match simply become their own `Level = null` entry rather than being dropped or crashing the sampler.
  Continuation-style lines Postgres sometimes emits with their own full prefix (a `STATEMENT`/`DETAIL`/`CONTEXT`/`HINT`
  line following an error) become their own rows too, each correctly tagged with its own `Level` and `Pid` -- confirmed
  live that Postgres gives these their own complete prefix rather than emitting them bare.
- **No `Exception` column**, unlike `ApplicationLogEntry` -- Postgres's own log format has no separate
  exception-vs-message split; a `STATEMENT`/`DETAIL`/`CONTEXT` line that *doesn't* carry its own prefix (some Postgres
  versions/configurations do run continuation lines together) is folded into the previous entry's `Message`,
  newline-separated, rather than a placeholder.
- **Purged, not permanent** -- included in the same weekly purge cycle as every other table in this suite (unlike
  `UserLogin` and `UserLogout`, which are permanent audit trails and deliberately excluded from purging -- see the
  Purging section below for that distinction).
- Browsable via `GET /api/PostgresLogEntry` and the Administration > Postgres Log page, same conventions as every other
  table here (`take`/`from`/`to`/`search` against `Message`/`Level`/`samplePercentage`, CSV export).

This complements, rather than duplicates, `PostgresMetric`/`PostgresReplicationStatus` above: those are Postgres's own
numeric self-view (`pg_stat_*`); this is its own narrative account of what happened and when, in Postgres's own words.

## Live activity and blocking (`GET /api/PostgresActivity`)

Deliberately not shaped like anything else in this suite: not a per-minute snapshot, not stored, not purged.
`pg_stat_activity` -- every backend Postgres currently knows about, from ordinary application connections to its own
autovacuum/checkpointer/background-writer workers -- reflects the instant it is queried, so a row from a minute ago says
nothing about a lock that formed and cleared thirty seconds later. The Administration > Postgres Activity page queries
it live on demand via a Refresh button, the way a DBA actually uses this view, rather than trying to force it into the
once-a-minute historical shape everything else here uses. Refresh is manual rather than timer-driven so the grid never
re-sorts itself out from under someone mid-read or mid-click.

- **Real blocking-chain detection, confirmed live** -- `pg_blocking_pids(pid)`, a built-in PostgreSQL function requiring
  no extension, resolves the actual "who is blocking whom" answer, not just "this backend is waiting on something."
  Verified against a real lock conflict (one session holding a row lock, a second blocked behind it): the blocked
  backend's `BlockedByPids` correctly named the exact PID holding the lock, with both backends' real query text and wait
  state visible. Blocked rows are highlighted on the page, with a toolbar legend explaining what the highlight means.
- **Zero extra configuration** -- no `logging_collector`, no `shared_preload_libraries`, no restart. Works against a
  stock Postgres install.
- **A dedicated connection, deliberately** -- `PostgresActivityRepository` opens its own connection, separate from the
  pooled application connection this page's own permission check runs against, for the same reason
  `PostgresMetricSampler`/`PostgresLogSampler` do: this is precisely the tool reached for when something about the
  database is already wrong, which is exactly when the application's own connection pool is most likely to be part of
  the problem.
- **Read-only by design** -- surfaces everything needed to identify a stuck query or a connection-leak pattern
  (`state = 'idle in transaction'` held for a long time is the classic signature), but does not offer
  `pg_cancel_backend`/`pg_terminate_backend` from the page itself. `PostgresActivityDto.Pid` is exactly what an operator
  would pass to either function directly in `psql` if a kill is actually warranted.
- **The complement to this page is `PostgresStatementStatistics` below** -- this page answers "what is running right
  now"; that one answers "which query *shape* is worst overall, across all history."
- Not tenant-scoped (this is server-wide activity, not Jube's own data) and restricted to the landlord tenant like every
  other infrastructure viewer in this suite.

## Per-query aggregate statistics (`GET /api/PostgresStatementStatistics`)

The tool the previous section used to describe as "exists but isn't wired up": `pg_stat_statements`, per- *query-shape*
aggregate statistics -- calls, total/mean/min/max/stddev execution time, rows, cache-hit vs disk-read bytes, temp-file
spill bytes and WAL bytes, accumulated across all history since the extension was created or last reset, not just
currently-running backends. Unlike `PostgresActivity` above, this genuinely is a running total, not an instant-in-time
view -- a query shape that ran once, five minutes ago, still shows up here with its numbers intact. The Administration >
Postgres Statement Statistics page queries it live on demand via a Refresh button, same manual-refresh convention as
`PostgresActivity`, and sorts worst-total-time first by default.

- **Requires deployment-level setup, unlike everything else in this suite** -- `shared_preload_libraries =
  'pg_stat_statements'` set server-side and a restart, because the extension needs a shared-memory hook registered at
  postmaster start; `CREATE EXTENSION` alone does not fail without it, it just leaves a dead extension that only errors
  the first time something queries the view. `AddImplicitAsyncRecallAndTimeoutFeatures`, the migration that creates this
  extension, guards against exactly this: its first statement checks that `vector`/`pg_trgm`/
  `pg_stat_statements` are actually available on the server and that `pg_stat_statements` is actually preloaded, and
  raises one clear error covering all of it before any other DDL in that migration runs, rather than a half-applied
  migration or a confusing runtime failure here later. See
  `Jube.Cluster/PgStatStatementsRollout.md` for the actual rollout steps against an already-running cluster.
- **A dedicated connection, deliberately** -- `PostgresStatementStatisticsRepository` opens its own connection, same
  reasoning as `PostgresActivityRepository` above.
- **Read-only by design** -- does not expose `pg_stat_statements_reset()` from the page itself, matching
  `PostgresActivity`'s choice not to offer `pg_cancel_backend`/`pg_terminate_backend`.
- Not tenant-scoped and is restricted to the landlord tenant like every other read-only infrastructure viewer here.

## Sampling architecture: fan-out, join and OpenTelemetry

`InfrastructureHealthMetricsStarter` runs eleven independent sampling groups concurrently each minute (via
`Task.WhenAll`), rather than one after another -- with as many samplers as this starter now carries (the .NET runtime,
Postgres, Postgres's own server log, Redis, the live Redis connection multiplexer, Redis Sentinel/connection event
drains, the per-minute Redis call counters, etcd, Patroni, and its own OpenTelemetry metric capture), a single slow or
unreachable dependency would otherwise stall every sampler behind it in a serial chain for the rest of the minute. In
practice this matters most for etcd/Patroni: when neither is configured or discoverable, each DNS-based discovery probe
still has to time out for every candidate hostname before giving up, and running that concurrently with the always-fast
Postgres/Redis/runtime samples keeps one minute's cycle bounded by the *slowest* sampler rather than the *sum* of all of
them.

Each group opens its own dedicated `DbContext` (one Npgsql connection each) -- necessary because a single ADO.NET
connection, and the LinqToDB `DataConnection` wrapping it, cannot run concurrent commands from more than one caller. The
eleven groups are:

1. `DotNetRuntimeMetric`
2. `PostgresMetric` + `PostgresReplicationStatus`
3. `RedisMetric` + `RedisSlowOperation` + `RedisSentinelStatus`
4. `RedisConnectionMultiplexerMetric`
5. `RedisConnectionEvent`
6. `RedisSentinelEvent`
7. `RedisCallCounter`
8. `EtcdMemberStatus`
9. `PatroniMemberStatus`
10. `OpenTelemetryMetric`
11. `PostgresLogEntry`

Groups 2 and 3 bundle more than one original sample because `PostgresMetricSampler` and `RedisMetricSampler` each hold
one long-lived connection plus delta/dedup state (`lastSample`, `lastSeenSlowLogId`, ...) across their own methods that
isn't safe to call concurrently with itself -- those samples stay sequential *within* their group, while the group as a
whole still runs in parallel with every other group. Every other sampler here is either stateless or keeps its mutable
state scoped to one group's own sequential call chain, so no locking is needed between groups; `EtcdPatroniDiscovery`,
used by both groups 8 and 9, is itself a stateless helper (constants only, no instance fields), so calling it
concurrently from two different groups is safe. Group 7, `RedisCallCounter`, is the same read-and-reset shape as
`ManageCountersStarter`'s per-model Task/Stage/Response Time Pipeline counters, just global rather than per-model -- it
drains `Jube.Cache.Observability.CacheCallCounters`, the dedicated accumulator every Jube.Cache repository method's
`CacheDiagnostics.Record` call also feeds, rather than polling an external dependency. Group 10 is a different shape
again -- it doesn't poll an external dependency at all either, it drains an in-process aggregation of this same
process's own OpenTelemetry metrics
(see [Local capture of Jube's own OpenTelemetry metrics](#local-capture-of-jubes-own-opentelemetry-metrics) below).
Group 11, `PostgresLogEntry`, holds its own dedicated connection the same way `PostgresMetricSampler` does and for the
same reason, but tails Postgres's own server log rather than `pg_stat_*` views -- see the dedicated section below.

Every one of the fourteen original sampling operations still gets its own OpenTelemetry span and metric regardless of
which of the eleven groups it runs inside, via `Jube.Engine.Observability.SamplerScope` (mirrors
`Jube.Service.Observability.OperationScope`'s span-plus-counter-plus-histogram shape, minus the
actor/tenant/audit/change-event concerns that only make sense for a user-initiated service call). Each span nests under
one parent `InfrastructureHealthMetrics.Cycle` activity for the minute, tagged `jube.sampler`, `jube.outcome` (`ok`/
`skipped`/`error`) and `jube.row.count`; `EngineDiagnostics.SamplerCount`/`SamplerDuration`
(`jube.engine.sampler.count`/`jube.engine.sampler.duration`, tagged `sampler`/`outcome`) carry the same information as
exported metrics. Both are emitted through `Jube.Engine`'s existing `ActivitySource`/`Meter` (`EngineDiagnostics`),
which `Jube.App/Startup.cs`'s `AddOpenTelemetry` already registers for both tracing and metrics export whenever
`EnableOpenTelemetry` is on -- `jube.engine.sampler.*` themselves are, in turn, exactly the kind of measurement group 10
captures locally.

## Local capture of Jube's own OpenTelemetry metrics

Everything `ServiceDiagnostics`/`EngineDiagnostics` record (`jube.service.operation.*`, `jube.engine.stage.duration`,
`jube.engine.sampler.*`, `jube.engine.logs.warn.count`) is, by default, only visible via the OTLP exporter -- i.e. only
if a deployment has an external OpenTelemetry collector configured at all. `OpenTelemetryMetricCapture` gives every
deployment a local, queryable copy of the same data for free, by attaching a second, independent `MeterListener`
alongside whichever one the OpenTelemetry SDK itself may already be running (confirmed live: multiple `MeterListener`s
can observe the same `Meter` concurrently with no interference). It is started unconditionally from `Startup.cs`, before
the application begins accepting requests, and runs regardless of whether `EnableOpenTelemetry`/an external collector is
configured.

This deliberately does not store one row per raw measurement. A histogram like `jube.engine.stage.duration` is recorded
up to seventeen times on every single invocation -- capturing every raw measurement would reintroduce exactly the
per-transaction volume problem OpenTelemetry metrics exist to avoid on this codebase's hottest path
(see [Sampling architecture](#sampling-architecture-fan-out-join-and-opentelemetry) above, and the wider discussion of
`/api/invoke` telemetry this suite is built on). Instead, measurements are aggregated in-process into one
Count/Sum/Min/Max bucket per distinct (`MetricName`, `Tags`) combination -- bounded by cardinality (stages x models,
areas x outcomes, ...), not by transaction volume -- and only the aggregate is written out, once a minute, by group 9
above. This mirrors what the OTLP exporter itself already does internally before handing data to a collector; the
difference is the destination is a local Postgres table as well as (or instead of) an external backend.

- **`OpenTelemetryMetric`** -- one row per distinct (`MetricName`, `Tags`) combination per one-minute drain, with
  `MetricName` (the OpenTelemetry instrument name), `InstrumentType` (`Counter`, `Histogram`, ...), `Tags` (a sorted
  `key=value,key=value` string), `Count`, `Sum`, `Min` and `Max` for that window. Browsable via
  `GET /api/OpenTelemetryMetric`, same conventions as every other table in this suite (landlord-only, not tenant-scoped,
  `take`/`from`/`to`/`search`/`samplePercentage`, `search` matching `MetricName` or `Tags`).

Two admin-editable pages sit alongside this local
capture: [OpenTelemetry Log Counter](../OpenTelemetryLogCounter/index.html) turns a regex match against any of this
page's unstructured log sources into a new counter without a code change,
and [OpenTelemetry Exclude](../OpenTelemetryExclude/index.html)
stops a named instrument -- one of those, or any other -- from being exported at all. A third,
[OTLP Dispatch Counter](../OtlpDispatchCounter/index.html), reports on the export pipeline itself once
`EnableOpenTelemetry` is on -- dispatch success/failure, response times, and drops caused by a full export queue.

### Sampling the listener itself

`OpenTelemetryMetricCaptureSamplePercentage` (default `100`, i.e. every measurement -- this suite's original behaviour,
unchanged unless explicitly configured) controls what fraction of raw measurements the capture `MeterListener` even
looks at, before any aggregation happens. This answers a different question than the read-side `samplePercentage` every
table's `GetLastAsync` already takes: that one thins out which already-aggregated *rows* a caller sees when browsing;
this one thins out which raw *measurements* are counted in the first place, trading accuracy for CPU cost on whatever
hot path is emitting them.

This exists because the meter list above has grown well beyond the once-a-minute InfrastructureHealthMetrics samplers it
started out covering -- `Microsoft.AspNetCore.Hosting`, Kestrel, `Microsoft.AspNetCore.Authentication`/`Authorization`,
and outbound `System.Net.Http` calls all fire on *every single HTTP request*, not once a minute. The sample check runs
as the very first thing in the listener's callback, before the per-measurement tag-list-to-string-key work and the
aggregator dictionary lock, so it sheds real CPU cost proportionally rather than just producing fewer output rows (row
count was already bounded by cardinality, not traffic, so it was never the concern).

Below 100, every aggregate's `Count`/`Sum` become an estimate scaled down by roughly that fraction, not the true
total -- `Min`/`Max` remain the min/max of whatever was actually sampled, which converges towards the true min/max as
volume increases but is not guaranteed to equal it. How many measurements were skipped this way
(`OpenTelemetryMetricCapture.SampledOutCount`) and how many distinct series were dropped for exceeding the defensive
cardinality cap (`DroppedNewSeriesCount`) are both tagged onto the `OpenTelemetryMetric` sampler span every flush cycle,
regardless of whether that cycle had anything new to write out.

### Beyond Jube's own meters -- host, process and framework telemetry

`OpenTelemetryMetricCapture.Start(...)` also listens to a handful of meters this application does not own, so the same
`OpenTelemetryMetric` table captures them too rather than that data only ever reaching an external OTLP collector:

- **`System.Runtime`** -- .NET's own built-in native meter (present since .NET 8/9 regardless of any OpenTelemetry
  package): host process CPU time (`dotnet.process.cpu.time`), GC generations/pause time/heap size, JIT compilation
  time, thread pool thread/queue counts, assembly count, lock contentions.
- **`System.Net.Http`** -- native outbound HttpClient request duration/count.
- **`Microsoft.AspNetCore.Hosting`** / **`Microsoft.AspNetCore.Server.Kestrel`** -- native inbound request
  duration/count and Kestrel connection/queue metrics (`/api/invoke` itself is excluded from the *tracing* side of
  ASP.NET Core instrumentation for volume reasons, but these aggregate metrics carry no per-transaction cardinality, so
  they are not excluded here).
- **`OpenTelemetry.Instrumentation.Process`** -- process memory (working set + virtual) and CPU time under
  OpenTelemetry's own semantic-convention names (`process.memory.usage`, `process.memory.virtual`, `process.cpu.time`,
  `process.uptime`); this one Meter is only ever created once `AddOpenTelemetry` actually builds its `MeterProvider`, so
  unlike the four native meters above it is only populated when `EnableOpenTelemetry` is on.

The first three are genuinely free: .NET does not bother computing these instruments at all until something starts
listening (confirmed live), so registering them here is what turns them on -- they are captured locally even with
`EnableOpenTelemetry` off and no OTel packages wired at all. There is no separate "host" metrics package in the .NET
OpenTelemetry ecosystem beyond this (checked; none exists) -- container-level CPU/memory *limits and throttling* are
instead covered by `DotNetRuntimeMetric`'s own `Container*` columns, read directly from cgroup (see above).

## Aspire-parity tracing additions

Two small, deliberate additions were made to the `/api/invoke` hot path itself alongside the metrics work above,
matching what a .NET Aspire-orchestrated service would give you by default:

- **Redis** -- `AddRedisInstrumentation` is wired against both `CacheService.ConnectionMultiplexer` (the connection
  `TtlCounterExtensions`/`CacheWalRepository` use during a normal invocation) and `CacheService.SentinelMultiplexer`.
  This puts a handful of Redis command spans on the hot path -- a materially smaller addition than a whole-invocation
  span, and one this suite's earlier "no tracing on /api/invoke" stance was explicitly relaxed to allow. Redis spans are
  harvested from StackExchange.Redis's profiling API on a timer
  (`StackExchangeRedisInstrumentationOptions.FlushInterval`, default 10 seconds) rather than emitted synchronously per
  command, so there is no per-command latency cost on the hot path -- only a bounded delay before the span becomes
  visible/exported.
- **RabbitMQ** -- `RabbitMQ.Client` 6.2.1 (the version this repo pins) predates that library's own built-in tracing
  (added around 6.6), and there is no widely-used contrib instrumentation package for it. The one RabbitMQ touch point
  on the invoke hot path (`PublishToAmqp`, conditional on `AMQP` being enabled) is instrumented by hand instead, via
  `EngineDiagnostics.ActivitySource`, tagged with the standard messaging semantic conventions plus
  `jube.entity_analysis_model_instance_entry_guid` -- a span attribute carries no cardinality cost the way a metric tag
  would, so this is exactly where "trace back to the transaction guid" belongs.

Npgsql is the one dependency deliberately left out of this pass: it is pinned at 5.0.18 (no native `ActivitySource`
support -- that arrived in Npgsql 7.0+), every connection in this codebase is a raw ad hoc `NpgsqlConnection` rather
than a shared `NpgsqlDataSource`, and LinqToDB is pinned at 3.6.0 with unverified compatibility with a
`NpgsqlDataSource`-based setup. Adding Npgsql tracing means a major-version bump with real compatibility risk across
every repository in `Jube.Data`, not an incremental addition, so it is out of scope here.

## Pinpointing the call site: code.* attributes

Every span this suite creates (`SamplerScope`, `OperationScope`, the manual RabbitMQ publish span,
`InfrastructureHealthMetricsStarter`'s own cycle span) carries the standard OpenTelemetry `code.function`/
`code.filepath`/`code.lineno` attributes, naming the exact method and line that opened it. These are populated via the
compiler's own caller-info attributes (`[CallerMemberName]`/`[CallerFilePath]`/`[CallerLineNumber]`) -- constants baked
in at compile time, not a stack walk or reflection, so this costs nothing at runtime.
`EngineDiagnostics.TagCodeLocation`/`TagCurrentCodeLocation` hold the shared implementation for `Jube.Engine`;
`OperationScope` in `Jube.Service` carries its own copy of the same shape, since that project does not reference
`Jube.Engine`.

This deliberately does not extend to metric tags: `code.filepath`/`code.lineno` are effectively unique per call site,
and tagging a metric with them would multiply its cardinality by every place that happens to record it -- exactly the
kind of unbounded tag this suite has avoided throughout. For a metric, the instrument name plus its existing business
tags (stage, area/operation) already narrow things down close to one call site; for anything needing more precision than
that, the span-level attributes above are the right place to look, not another metric tag.

## Redis and Postgres client-side metrics

`OpenTelemetryMetric` also now carries genuine per-command Redis and Postgres client latency, closing a gap in the local
capture story: `OpenTelemetry.Instrumentation.StackExchangeRedis` (wired above) produces spans only, with no Meter of
its own, and Npgsql is pinned too old for any OpenTelemetry hook at all -- so neither was reaching the local table
despite the Redis/Postgres *server-side* tables above already being rich.

- **Redis** -- `RedisCommandMetricBridge` (`Jube.Engine.Observability`) attaches a second, independent
  `ActivityListener` to `OpenTelemetry.Instrumentation.StackExchangeRedis`'s own `ActivitySource` (the same "listen
  alongside whatever else is already listening" technique `OpenTelemetryMetricCapture` uses for Meters) and records each
  finished span's duration into `jube.redis.command.count`/`jube.redis.command.duration` on the `Jube.Engine` meter,
  tagged by command name (`SET`, `GET`, `INCR`, ...) and outcome. Since it rides on the same spans, it inherits the same
  characteristics: no per-command hot-path cost (harvested on `FlushInterval`, not synchronously), and only populated
  when `EnableOpenTelemetry` is on (the `ActivitySource` in question is only ever created once `AddRedisInstrumentation`
  above actually runs).
- **Postgres (command latency)** -- `DataDiagnostics` (`Jube.Data.Observability`) wires LinqToDB's own
  `DataConnection.OnTrace` hook once, globally, at startup -- unconditionally, independent of `EnableOpenTelemetry`, the
  same way `OpenTelemetryMetricCapture` itself is unconditional. Every `DataConnection` created afterward (every
  repository, every service, across the whole codebase, not just the invoke pipeline) is covered automatically, with no
  per-call-site change needed. Records `jube.postgres.client.command.count`/`jube.postgres.client.command.duration`,
  tagged by `TraceInfo.Operation` (`ExecuteReader`, `ExecuteNonQuery`, `ExecuteScalar`, `BulkCopy`, ... -- a fixed,
  seven-value enum) and outcome (`ok`/`error`) -- never by SQL text, which would carry unbounded cardinality. This
  complements, rather than replaces, `PostgresMetric`/`PostgresReplicationStatus`/`PostgresIndexStatistics`/
  `PostgresTableStatistics` above: those see the server's own view (cache hit ratios, replication lag, table bloat);
  this is the .NET client's own per-command latency.
- **Postgres (connection pool and wire-level)** -- `NpgsqlEventCounterBridge` (`Jube.Data.Observability`) closes what
  `DataDiagnostics` alone can't see: connection pool exhaustion, bytes actually on the wire, prepared-statement reuse.
  Npgsql is pinned too old (5.0.18) for the native OpenTelemetry support that arrived in 7.0+ -- a version bump is a
  separate, out-of-scope decision (see below) -- but it has carried a rich `EventCounters` surface since well before
  that. Confirmed live: the `Npgsql` `EventSource` emits `busy-connections`, `idle-connections`, `connection-pools`,
  `total-commands`, `current-commands`, `failed-commands`, `prepared-commands-ratio`, `bytes-written-per-second`,
  `bytes-read-per-second`, `commands-per-second`, and (when multiplexing is enabled, which this codebase's connection
  strings do not) three multiplexing-batch averages. A plain BCL `EventListener` matched by
  `EventSource.Name == "Npgsql"` picks these up with no package reference to Npgsql needed at all, recording each onto
  the same `Jube.Data` meter as `jube.postgres.client.<name>` -- the three `-per-second` counters (genuine deltas) via
  `Counter.Add`, everything else (point-in-time or cumulative-so-far values) via `Histogram.Record`, so a per-minute
  aggregate's Min/Max shows the observed range rather than a nonsensical sum. Deliberately does **not** enable the
  sibling `Npgsql.Sql` source, which carries verbose per-command SQL text -- unbounded cardinality, and exactly the kind
  of raw-statement logging this suite has avoided everywhere else.

Both bridges above are deliberately named `jube.postgres.client.*` (not `jube.data.*` or anything mentioning Npgsql by
name) precisely so that searching `OpenTelemetryMetric` for **`postgres`** -- a plain substring match against
`MetricName`, case-insensitive -- finds every Postgres client-side metric in one search, from both bridges, rather than
needing to know which bridge produced which name. Searching for `npgsql` will find nothing: the driver itself is an
implementation detail, never named in a metric this suite emits.

## Purging

Every table in this suite accumulates at least one row a minute, indefinitely, unless something purges it.
`InfrastructureHealthMetricsPurgeStarter` is that something -- a background loop, separate from the sampling starter
above (different concern, different cadence, no shared state -- mirroring `CachePruneTaskStarter` sitting alongside
`ManageCountersStarter` for the same reason), that purges all thirty-eight tables sharing this retention setting once a
cycle: the twenty-four browsable via the endpoints above, `ApplicationLogEntry`, the three
`EntityAnalysisModel*Counter` tables, the Archiver/case-creation/`ModelInvokeWarning` tables covered in
[Background Processing Performance](../BackgroundProcessingPerformance/index.html), and four pre-existing
counter/balance tables (`HttpProcessingCounter`, `EntityAnalysisAsynchronousQueueBalance`,
`EntityAnalysisModelAsynchronousQueueBalance`, `EntityAnalysisModelProcessingCounter`) that predate this suite but share
its retention setting -- `PostgresIndexStatistics`/`PostgresTableStatistics` are excluded, since those are live catalog
snapshots with no historical rows to purge. `ContainerLogEntry`/`DockerEvent` are purged the same way despite being
written by the separate `Jube.Monitoring`
sidecar process, not `InfrastructureHealthMetricsStarter` -- purging is a property of the table, not of whichever
process happens to write to it.

Retention is configured the same way `SearchKeyCacheServerIntervalType`/`SearchKeyCacheServerIntervalValue` already are
elsewhere in this codebase -- a single-letter unit paired with a count:

``` text
InfrastructureHealthMetricsPurgeIntervalType=d
InfrastructureHealthMetricsPurgeIntervalValue=7
```

`d`/`h`/`n`/`s`/`m`/`y` (days/hours/minutes/seconds/months/years) are the accepted unit letters, the same vocabulary
`CachePruneTaskStarter`'s own retention setting uses. The cutoff is computed against `DateTime.UtcNow` every cycle --
this codebase is UTC throughout (there is no `DateTime.Now` call anywhere in `Jube.Engine`, `Jube.Cache` or
`Jube.Data`), so there is no local-time offset to account for. The defaults above keep a week.

The purge cycle itself repeats every `WaitInfrastructureHealthMetricsPurge` milliseconds (default `60000`, matching this
suite's own one-minute sampling cadence), and each table's backlog is deleted
`InfrastructureHealthMetricsPurgeDeleteLimit` rows at a time (default `1000`, matching `CacheTtlDeleteLimit`'s own
default) rather than in one unbounded `DELETE` -- Postgres has no `DELETE ... LIMIT`, so this selects one chunk of
primary keys older than the cutoff, deletes exactly those, and repeats until a chunk comes back smaller than the limit
(confirmed live: `LinqToDB`'s own `Take().DeleteAsync()` fails outright against Postgres with a syntax error --
`ORDER BY`/`LIMIT` on a bare `DELETE` is MySQL/SQL Server syntax, not standard SQL -- the select-then-delete-by-id shape
is the one that actually works). All twenty-five tables purge concurrently via `Task.WhenAll`, for the same reason the
sampling groups above do: a large backlog on one table (many chunk round trips, e.g. after the purge job has been off
for a while) must never stall every other table behind it.

## Browsing it without a database connection

Up to the last 100,000 rows of each table are browsable directly, most recent first:

- `GET /api/DotNetRuntimeMetric`
- `GET /api/PostgresMetric`
- `GET /api/RedisMetric`
- `GET /api/RedisSlowOperation`
- `GET /api/RedisConnectionMultiplexerMetric`
- `GET /api/PostgresReplicationStatus`
- `GET /api/RedisSentinelStatus`
- `GET /api/RedisSentinelEvent`
- `GET /api/RedisConnectionEvent`
- `GET /api/RedisCallCounter`
- `GET /api/EtcdMemberStatus`
- `GET /api/PatroniMemberStatus`
- `GET /api/EtcdClusterEvent`
- `GET /api/PatroniClusterEvent`
- `GET /api/DockerContainerMetric`
- `GET /api/DockerHostMetric`
- `GET /api/OpenTelemetryMetric`
- `GET /api/OtlpDispatchCounter`
- `GET /api/PostgresLogEntry`
- `GET /api/ContainerLogEntry`
- `GET /api/DockerEvent`
- `GET /api/HAProxyServerStatus`
- `GET /api/HAProxyReachabilityProbe`
- `GET /api/OverlayNetworkTaskDrift`

All twenty-four accept the same optional query parameters, pushed down to the database rather than filtered after the
fact:

- **`take`** -- maximum rows to return (default and maximum 100000).
- **`from`** / **`to`** -- restrict to a UTC date/time range (`CreatedDate` for the wide snapshot tables, `OccurredDate`
  for the narrow ones), inclusive at both ends. Each defaults independently to the last 24 hours when omitted, so a
  plain call without either returns the last day's samples rather than the whole table.
- **`search`** -- a case-insensitive substring match against the text-bearing columns each table has (`Instance` for
  `DotNetRuntimeMetric`/`PostgresMetric`/`RedisMetric`; `Instance` or `ClientName` for
  `RedisConnectionMultiplexerMetric`; `Command`, `CommandName`, `KeyName`, `ClientAddress` or `ClientName` for
  `RedisSlowOperation`; `UserName`, `ApplicationName`, `ClientAddress`, `State` or `SyncState` for
  `PostgresReplicationStatus`; `EntityType`, `Name`, `Ip`, `Flags` or `MasterLinkStatus` for `RedisSentinelStatus`;
  `Channel` or `Message` for `RedisSentinelEvent`; `EventType`, `EndPoint`, `FailureType`, `Message` or `Exception` for
  `RedisConnectionEvent`; `Call` for `RedisCallCounter`; `Endpoint`, `Name`, `HealthReason` or `Alarms` for
  `EtcdMemberStatus`; `Name`, `Host`, `Role`, `State` or `Scope` for `PatroniMemberStatus`; `Endpoint`, `Name`,
  `EventType`, `PreviousValue` or `NewValue` for `EtcdClusterEvent`; `Scope`, `Name`, `EventType`, `PreviousValue`,
  `NewValue` or `Reason` for `PatroniClusterEvent`; `Name`, `Image`, `State` or `Status` for `DockerContainerMetric`;
  `Instance` or `OperatingSystem` for `DockerHostMetric`; `MetricName` or `Tags` for `OpenTelemetryMetric`; `Message` or
  `Level` for `PostgresLogEntry`; `ContainerName` or `Message` for `ContainerLogEntry`; `EventType`, `Action` or
  `ActorName` for `DockerEvent`; `Signal` for `OtlpDispatchCounter`; `PxName`, `SvName` or `Status` for
  `HAProxyServerStatus`; `Target` or `HAProxyAddress` for `HAProxyReachabilityProbe`; `ServiceName` for
  `OverlayNetworkTaskDrift`).
- **`samplePercentage`** -- an unexposed, agent-oriented fourth filter, not present in the UI toolbar and not documented
  in any FormField. A percentage from 0 to 100 (values outside that range are clamped); each row that already matches
  `from`/`to`/`search` is included independently with that probability, evaluated server-side
  (`random() < samplePercentage / 100.0`, added to the query's own `WHERE` clause, not applied by fetching everything
  and filtering in .NET) -- so `samplePercentage=10` returns roughly a random one-tenth of matching rows, not the newest
  tenth `take` would otherwise return. Intended for a baseline-and-compare workflow: an agent takes an unbiased random
  sample of activity early (say, right after onboarding a new deployment) and later compares recent activity against
  that stored baseline, which the normal most-recent-first ordering can't provide on its own since it always returns the
  same tail end of the table.

All twenty-one are restricted to the landlord tenant (any other caller receives 403), and none are scoped to a tenant or
Model -- the landlord sees every row, since none of this data is tenant-scoped. Each is also browsable via its own page
under **Performance** in the UI, where only `take`/`from`/`to`/`search` are available from a toolbar above the grid --
`samplePercentage` works against every endpoint above but is deliberately absent from that toolbar and from the page's
own JavaScript, reachable only by calling the endpoint directly.
