---
layout: default
title: Cache, Memory and State
nav_order: 2
parent: FAQ
---

# Cache, Memory and State

## Field caching and search keys

**Q: What does the "Cache" switch on a field do?**

A: It controls whether that field is stored in the cache payload used by abstraction rules. The full payload is always archived regardless. Switching it off saves memory but makes the field unavailable to abstraction rules from then on (forward-only). In practice this matters less than it sounds: the [Rule Taxonomy](../RuleTaxonomy/index.html) steers the great bulk of state towards lightweight TTL counters, with abstraction rules reserved for cardinality and complex aggregation, so few fields should need caching at all. The switch is among the fields of a [Request XPath](../Configuration/Models/RequestXPath/index.html).

**Q: What does "Search Key" do on a field?**

A: It indexes a field (account, IP, device) so abstraction rules can reach into the cached history for that key, with a time-to-live (TTL Interval Type and TTL Interval Value) and a Fetch Limit. It is purely an abstraction-rule concept: TTL counters maintain their own keyed state and are unaffected by the Search Key switch. Each search key costs one cache query per invocation, so keep them to the minimum the rules genuinely need.

**Q: Why is there a fetch limit on search keys?**

A: To stop a key with a huge history from saturating the engine by returning an unbounded number of records per transaction. History is fetched up to the lower of the model's Cache Fetch Limit and the search key's Fetch Limit, each being 100 unless changed, and that truncation happens before the rule's time window is applied.

**Q: What happens if I turn Cache or Search Key off after running?**

A: Nothing breaks. Changes are forward-only, and as with any change to a Request XPath they take effect once approved. The one destructive setting is the cache time-to-live, which purges data older than the interval.

**Q: How much memory does a transaction use in the cache?**

A: Roughly 400 to 500 bytes compressed (up to about 700 to 800 bytes uncompressed). Caching fewer fields and preferring TTL counters reduces this considerably, and the [Rule Taxonomy](../RuleTaxonomy/index.html) sets out highly opinionated guard rails for exactly this reason.

## TTL counters

**Q: What are the two ways to maintain state, and which should I use?**

A: Abstraction rules need full payloads in cache, so they are memory-expensive. TTL counters store only a count (or sum) plus expiry entries. For counts and sums, use TTL counters. Use abstraction rules only for what counters can't do: distinct counts and complex aggregations.

**Q: How do TTL counters work?**

A: An activation rule increments the counter. Expiry entries are recorded at a resolution interval (minutes, hours or days), and the counter is decremented by a background process as entries expire. A coarser resolution means fewer entries and less memory, but less precise expiry. Ageing follows the model's reference date, not the wall clock. See [TTL Counters](../Configuration/Models/TTLCounters/index.html).

**Q: What is TTL counter Online Aggregation, and when should I use it?**

A: A per-counter switch. With it off (the default), reads return a pre-aggregated running value that the background decrement keeps approximately right, which is cheap, and fine for most windows. With it on, each read counts the individual entries in the window ending at the transaction's reference date, so the value is exact, at the cost of a little more pressure on the cache and engine. Online aggregation is not a magic bullet: precision is still bounded by the resolution interval. Reserve it for edge-sensitive short windows (for example a 60-second burst counter with a fine resolution); such cases tend not to crop up much. Pick the resolution as a fraction of the window either way.

**Q: Can TTL counters be shared across models?**

A: Yes, if the models share a common key. A rule in Model A can increment a counter defined in Model B, which is a cornerstone of crafting a genuinely enterprise-wide fraud management system across channels. See [TTL Counter Activation Rule Incrementation](../Configuration/Models/TTLCounterActivationRuleIncrementation/index.html).

**Q: Are TTL counters idempotent?**

A: Not unless asked to be. Setting the `ActivationRuleIdempotency` Environment Variable to True, it being False by default, has TTL counter increments, case creation and notifications each applied at most once per transaction lifecycle, even under reprocessing or retry. The records that make this possible are extra writes to the cache, kept for as long as the payload of the transaction, which is why it is off unless wanted. See [Environment Variables](../Concepts/EnvironmentVariables/index.html).

**Q: Can I have a counter grouped by two keys?**

A: Build a composite key. An inline function can create one for use in rules and as an abstraction function key, but to act as a search key the composite must be decorated by an inline script; inline-function search keys were deferred as significant structural work.

## Search key cache

**Q: What is the search key cache for?**

A: For keys with too many records to aggregate in real time, and for profiles across large groups. It pushes abstraction rules to a background engine that reads from PostgreSQL and stores pre-computed values for real-time use. There are two intervals to keep apart: the Selection Interval is how often a value is recalculated, and the Cache TTL Interval is how long a calculated value lives.

**Q: Why do my abstraction rules stay at 0 when the search key cache is on?**

A: With the search key cache on, the rule no longer calculates in real time. It reads the background-computed value, so counts only update after the job runs. To test real-time behaviour, turn the search key cache off for that key.

**Q: Does the background engine recalculate everything every time?**

A: No. It builds a distinct list of keys and only recalculates keys that have new data and whose interval has passed.

## The cache tier

**Q: What is the cache?**

A: Anything that speaks the Resp protocol, which is the wire protocol of Redis. Jube's own compose files run Garnet, both for the quickstart and for the cluster, and the settings are named for the protocol rather than the product (`RespConnectionString` and so on). Redis remains available as an alternative compose file in each case. See [Deploying with Docker](../GettingStarted/DeployingWithDocker/index.html) and the [Deployment Runbook](../GettingStarted/DeployingWithJubeCluster/DeploymentRunbook/index.html).

**Q: What is the local cache?**

A: A least-recently-used cache in each engine for immutable transaction payloads, up to a byte limit (`LocalCacheBytes`, 1 GB unless changed). It gives roughly a 30% speed-up and reduces load on the cache. Misses fall back to the cache, and a warm-up option (`LocalCacheFill`) refills it on container start. A couple of GB is generally enough; most implementations barely touch cache limits. See [Local LRU Cache](../Concepts/Cache/LocalLruCache/index.html).

**Q: Can I use Redis or Valkey instead of Garnet?**

A: Anything RESP wire-compatible works. Garnet is what ships, Redis is kept as the alternative compose file, and Valkey is a drop-in replacement for Redis.

**Q: Why use an in-memory cache rather than PostgreSQL for real-time?**

A: The real-time pipeline does no disk I/O. The cache gives tighter latency, while PostgreSQL holds configuration, case management and analytics.

**Q: Can PostgreSQL back the cache to reduce memory?**

A: Yes. Setting `RespHsetOffloadToPostgres` to True, it being False by default, moves every hash operation (the payload hash structures) to the PostgreSQL table CacheSetHash, which is hash-partitioned on key into four partitions. The rest (pub/sub, journals) stays in the cache. This substantially reduces memory sizing, at the cost of needing good disk or NAS volumes; benchmark before committing. See [Cache Schema](../Concepts/Cache/CacheSchema/index.html).

**Q: What is stored where in the cache?**

A: Payloads are a hash keyed by transaction, serialised with MessagePack and LZ4 compression, with field names swapped for small integer indexes. A per-key journal (a sorted set) links keys such as account to transactions.

**Q: Is the cache durable, and what are the trade-offs?**

A: Yes. The cluster deployment runs Garnet with an append-only file committed once a second, and recovery from it on start, which bounds worst-case loss to about one second of writes. The Redis alternative does the same with `appendfsync everysec`. Fsync-on-every-write closes even that window but, on Redis, cuts throughput by around 70%, so almost nobody runs it; the one-second exposure is normally documented and accepted as the lesser risk. For production, run replicas (the cluster has three, with a cluster manager promoting one should the primary be lost), keep the primary's volume on NAS so a lost host can be reconstituted, and ship backups off-host. Key eviction must never be allowed on the cache: on Redis, run `noeviction`.
