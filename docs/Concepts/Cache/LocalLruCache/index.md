---
layout: default
title: Local Least Recently Used (LRU) Cache
nav_order: 2
parent: Cache
grand_parent: Concepts
---

# Local Least Recently Used (LRU) Cache

As introduced in the architecture section of this documentation, the real-time performance of the platform is in large part owing to the use of a Remote Key Value \ Data Structure Pair data store, henceforth Resp, which implies any server speaking the RESP protocol (Garnet being the server provided in the Docker Compose file, with Redis being a compatible alternative). In many cases the centralisation of and the serialised access to this data are extremely important, especially in maintaining the latest transaction ledgers, so that irrespective of the node processing the transaction, the transaction order is preserved.

Referencing the [Cache Schema](../CacheSchema/index.html) documentation, it can be noted that the Payload prefix contains data referenced intensely, and to a degree inefficiently, with the trade being no duplication of Payload storage for the ledgers. For example, consider a search key that has a limit of 100 records, and consider that there might be four of them; for each transaction stored in the ledger the payload will need to be recalled from Resp, and while Jube manages this process to ensure there is no duplication of Payload requests, it remains a significant and inefficient request volume for Resp, and significant transport overhead.

Jube is quite compute intensive given the level of task parallelism that happens during transaction processing. Assuming that Jube is configured using compute from one of the hyperscalers (e.g., AWS, Azure, Digital Ocean), this may well create a situation where the compute provisioned (i.e., Virtual Machine) has a large amount of redundant memory. Meanwhile, given the load that Payload matching causes on the Resp server, and the running cost of that server in practice, Resp is substantially and inefficiently overallocated.

To improve performance, achieve better memory server density and better Resp allocation, Jube maintains a local Least Recently Used (LRU) cache of the Payload prefix data. During transaction processing, for Payload prefix data referencing, the local LRU cache is attempted, and Resp is used only in case of a cache miss. Serialisation schemas are common in both the local LRU cache and Resp, being optimised MessagePack binary serialisation, and it follows that deserialisation overhead is unaffected given local or remote Payload recall.

Given the total availability of Payload prefix data in the local LRU cache, a fourfold improvement in transaction throughput can be achieved.

## Configuration

The local LRU cache is governed by the following Environment Variables, each of which is described further in [Environment Variables](../../EnvironmentVariables/index.html):

| Value                      | Default    | Description                                                                                                                                  |
|----------------------------|------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| LocalCache                 | True       | Whether the local LRU cache is maintained at all.                                                                                            |
| LocalCacheBytes            | 1073741824 | The total size in bytes beyond which Payload prefix data is evicted, being 1GB by default.                                                   |
| LocalCacheFill             | True       | Whether the local LRU cache is filled from Resp on startup.                                                                                  |
| RespPublishSubscribeEvents | False      | Whether additions and removals are published for the benefit of the local LRU cache of other instances.                                      |
| LruJournalMaxAgeInterval   | h          | The interval, taken together with LruJournalMaxAgeValue, giving the age beyond which an LruJournal entry is pruned and is not used for fill. |
| LruJournalMaxAgeValue      | 1          | The number of intervals, as above.                                                                                                           |
| WaitLruJournalPrune        | 10000      | The time in milliseconds between each pruning of the LruJournal.                                                                             |
| RespMessagePackCompression | True       | Whether Payload prefix data is compressed, in Resp and the local LRU cache alike.                                                            |

The local LRU cache can evict Payload prefix data given the total size of the local LRU cache exceeding the value set in the LocalCacheBytes environment variable. It is oftentimes possible to maintain all Payload prefix data in the local cache, however, even with partial cache hits, there remains significant performance improvement given a significant proportion of load being offloaded to the local LRU cache. Given that the local LRU cache is on the basis of least recently used eviction, it affords a better than proportional reduction in Resp load.

The local LRU cache is enabled by default for 1GB but is subject to the LocalCache environment variable.

The LocalCacheFill environment variable determines whether, on Jube startup, the local cache should be filled from Resp, and this is the default. In the event that fill is disabled, it will allow a faster startup, but at the expense of increased pressure on Resp, and is henceforth discouraged.

The local LRU cache shares the same compression schema as Resp, subject to the RespMessagePackCompression environment variable.

## Fill order and the LruJournal

Rather than filling in an arbitrary order, startup fill walks the LruJournal Sorted Set (see [Cache Schema](../CacheSchema/index.html)) for each model, most recently used first, so that the local cache is primed with the Payload data most likely to be needed again rather than whatever a scan happens to return first. Every live recall of a Journal key during transaction processing timestamps that key into the LruJournal of its model, so the ordering reflects genuinely recent usage rather than insertion order. For each Journal key so found, the Journal is walked, most recent first, and the Payload data it refers to is recalled into the local LRU cache, with replicas being preferred for these reads where they exist.

As the LruJournal only ever grows on read, two things keep it from growing without bound and from priming the cache with data that is no longer relevant:

* A dedicated background task prunes LruJournal entries older than LruJournalMaxAgeInterval and LruJournalMaxAgeValue taken together (for example, 1 h) on an interval set by WaitLruJournalPrune.
* Startup fill itself also checks that same age threshold as it walks the journal, and stops warming further as soon as it reaches an entry past the cutoff, so that a slow or delayed prune cycle cannot cause stale entries to be used to prime the cache.

{% comment %}TODO(verify): in FillPreferReplicaAsync (Jube.Cache/Resp/CachePayloadRepository.cs) the fill adds each payload under journalEntry.ToString(), which for a sorted set entry is the element followed by its score, whereas transaction processing looks the payload up by the element alone. Confirm on a running system that payloads loaded by the fill are subsequently hit, as it is suspected they are not.{% endcomment %}

## Parity between instances

Given an arrangement of Jube in a clustered and highly available manner, it is incumbent to update the local LRU cache of all Jube instances. Subject to the RespPublishSubscribeEvents environment variable, which is False by default and must therefore be switched on in a clustered arrangement wanting this behaviour, events are published on the addition to and the deletion from the local LRU cache. Distributed instances of Jube are subscribed to the events, each updating its local LRU cache to maintain parity between all instances. Events are published using the Publish \ Subscribe functionality of Resp, based on the following channel structure:

| Prefix     | Type   | Channel Structure                                    | Value                                                                           |
|------------|--------|------------------------------------------------------|---------------------------------------------------------------------------------|
| HashSet    | Create | HashSet:Server:InstanceCorrelationGuid:PayloadKey:HashSetKey | MessagePack serialisation of payload.                                   |
| HashRemove | Remove | HashRemove:Server:InstanceCorrelationGuid:PayloadKey | The HashSetKey to be removed, which is to say the identifier of the payload.    |

In the above, Server is the host name of the instance, InstanceCorrelationGuid is a Guid created by the instance on startup and recorded in the LocalCacheInstance table, PayloadKey is the Payload key as set out in the Cache Schema (itself being several segments, as Payload:TenantRegistryId:EntityAnalysisModelGuid) and HashSetKey is the EntityAnalysisModelInstanceEntryGuid of the payload.

A shortcoming of the Publish \ Subscribe functionality is that it can't discriminate where events have originated from itself. Henceforth, the Server and InstanceCorrelationGuid combination that created the event, which by implication will already maintain an up-to-date local LRU cache, will be subscribed, potentially creating a recursion. On receipt of events, the Server and InstanceCorrelationGuid are compared, as together they definitively identify the source of the event. Given own source, then it will not update the local LRU cache, and the event will be completely ignored.

## Observability

Given the significant effect the local LRU cache can have on overall performance, observability is provided via three Postgres tables that are updated every minute:

| Table                 | Description                                                                                                                                                                                              |
|-----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| LocalCacheInstance    | The total bytes and payloads stored in the local LRU cache, the .Net memory pressure and details on the status of a fill (updated for each 100k filled).                                                 |
| LocalCacheInstanceKey | For a given Resp Key, the number and bytes of requests, misses, Hash Set subscriptions received and Hash Removal Subscription received. Includes Resp response time on miss and deserialisation times. |
| LocalCacheInstanceLru | For the local LRU cache, the total bytes and payloads available, the number, and bytes, of requests, updates, additions, removals and eviction.                                                         |

For example, for the LocalCacheInstanceLru table:

```sql
select *
from "LocalCacheInstanceLru"
order by 1 desc
```

The query returns one row for each minute and for each instance, the most recent first. The columns are Id, LocalCacheInstanceId, CreatedDate, Bytes and Count, followed by a Bytes and Count pair for each of Request, Add, Remove, Eviction and Update. An abbreviated example for an instance at rest, holding 7 payloads in 7,756 bytes and serving no requests, is as follows:

| Id  | LocalCacheInstanceId | CreatedDate                | Bytes | Count | RequestBytes | RequestCount |
|-----|----------------------|----------------------------|-------|-------|--------------|--------------|
| 126 | 3                    | 2025-10-29 12:22:33.945044 | 7756  | 7     | 0            | 0            |
| 125 | 3                    | 2025-10-29 12:21:33.945528 | 7756  | 7     | 0            | 0            |
| 124 | 3                    | 2025-10-29 12:20:33.947202 | 7756  | 7     | 0            | 0            |
