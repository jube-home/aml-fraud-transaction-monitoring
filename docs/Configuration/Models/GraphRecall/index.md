---
layout: default
title: Graph Recall
nav_order: 31
parent: Models
grand_parent: Configuration
---

# Graph Recall

Graph Recall reads the tenant's relationship graph during invocation and gives rules and the response the facts the
graph holds about a value of the transaction: how close it is to confirmed fraud, which communities it bridges, whether
it is a hub, which patterns it belongs to and the facts of the edge to a counterpart. It is a child of the model, in the
Network hub beside Svirfd. Rules read it as `Graph.{Name}.{Member}`.

<!-- TODO(screenshot): file=model-network-graph-recall-entities.png route=/models/models/:id state="Network hub, Graph Recall tab, Account and Beneficiary entities joined as counterparts" -->
<!-- TODO(docs): the owner must review this whole page; it was drafted from the DTO, validator and engine code, not from the running UI -->

## Entity fields

Each entity needs the **Edit Models** permission (15), like every child of the model.

| Field            | Meaning                                                                                                                                         |
|------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|
| Name             | The name rules read it by. Letters and digits, starting with a letter, at most 64 characters, unique within the model.                          |
| Field            | The request field of the model that carries the value, at most 128 characters.                                                                  |
| Node Key         | The graph node key the value is filed under when it differs from the field name.                                                                |
| Counterpart      | Another entity of the same model this one is joined to for edge facts and bridge detection. Never itself. Joins are explicit and never guessed. |
| Graph Registry   | Restricts edge facts to the definitions of one registry. Empty uses every active definition of the tenant.                                      |
| Response Payload | Return the entity in the response JSON under `Graph`.                                                                                           |
| Archive Key      | Write the entity into the archive JSON. Exclusive with Response Payload.                                                                        |
| Active           | Whether the entity is recalled.                                                                                                                 |

## Isolation and defaults

- The engine keeps one in-memory snapshot per tenant, loaded by the background loader from that tenant's graph rows
  only. A transaction is evaluated against the snapshot of the tenant that owns the model; if there is none, there is no
  fallback to another tenant.
- When the snapshot is absent (recall switched off with `EnableGraphRecall`, the loader has not finished its first
  cycle, or the tenant has no active registry) every entity carries typed defaults: `Known` False, every distance one
  beyond the distance cap, no tags, no patterns, no edge. A rule never fails because of it.
- Only active, undeleted registries and edge definitions contribute edges and joins.
- Memory is bounded per tenant by `GraphRecallMaximumNodesPerTenant` (default 2000000) and
  `GraphRecallMaximumEdgesPerTenant` (default 4000000). A hub, confirmed, tagged or high propagation node is loaded
  first. Reaching either cap is logged and the snapshot carries `Truncated` or `EdgesTruncated`.
- `GraphRecallHashKeys` keeps a 64-bit hash of each value instead of the value, which saves memory at the price of a
  very small collision
  probability. <!-- TODO(docs): state the collision probability at 2000000 nodes once the owner confirms the hash -->
- If the loader cannot load any tenant it retries after at most 30 seconds; otherwise it waits `GraphRecallLoaderWait`
  (default 15 minutes).

## Members

`Known`, `Hub`, `Degree`, `DistanceToConfirmed`, `RelaxedDistanceToConfirmed` (the distance counting a non-hub
counterpart one hop closer), `CommunityId`, `BridgesCommunities` (the entity and its counterpart are connected and sit
in different communities), `BridgesToTagged` (another entity of the transaction has a tag within two hops), `Edge.*`,
`Tag(name).*`, `Pattern(kind)` and the scores. The Rule Coder lists them all.
<!-- TODO(docs): list every member with type and meaning from the rule vocabulary -->

See [Environment Variables](../../../Concepts/EnvironmentVariables/index.md) for the settings
and [Svirfd on a model](../Svirfd/index.md) for the other child of the Network hub.
