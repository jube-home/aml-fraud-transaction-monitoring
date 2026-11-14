---
layout: default
title: Getting Transactions In
nav_order: 20
parent: Concepts
---

# Getting Transactions In

There are three ways to hand a transaction to a model. They differ in whether the caller waits for the answer and in
what they need.

| Way                                                                   | The caller waits?              | Needs                                        | Delivery                                     |
|-----------------------------------------------------------------------|--------------------------------|----------------------------------------------|----------------------------------------------|
| `POST /api/Invoke/EntityAnalysisModel/{guid}`                         | Yes, gets the model's response | Nothing extra                                | The response is the receipt                  |
| **Inbound queue** `POST /api/Invoke/Queue/EntityAnalysisModel/{guid}` | No, gets 202 and a row id      | Nothing extra: it lives in the Jube database | At least once, durable                       |
| RabbitMQ: publish to the `jubeInbound` queue                          | No                             | A RabbitMQ broker                            | At least once, acknowledged after processing |

Jube deliberately does not add Kafka, and it does not use Redis publish and subscribe for ingestion, which would lose a
transaction whenever no engine node happened to be listening. For callers that cannot wait for the answer, the database
queue is the default way in, because it needs nothing beyond the database Jube already has; RabbitMQ is fully supported
for sites that already run it.

## The inbound queue

The caller needs the same access as for the live route: an API key whose role is allowed on the model. The body must be
JSON and within `MaxInvokeControllerRequestBytes`. The reply is `202` with `{"id": ...}` once the transaction is safely
stored, `429` when more than `InboundQueueMaximumDepth` rows are pending (back off and retry), `400` for an empty,
oversized or malformed body, `404` for an unknown or inactive model, and `403` without access.

Each engine node (`EnableInboundQueue`, on by default) claims a batch with `FOR UPDATE SKIP LOCKED`, so any number of
nodes can work the same queue without taking a row twice, runs each transaction through its model, and deletes the row
when the model has produced a response. If a node dies with a batch claimed, the claims expire after
`InboundQueueVisibilitySeconds` and another node takes the rows. A failure is retried after 5 seconds, then 10, 20 and
so on up to 5 minutes; after `InboundQueueMaximumAttempts` the row is marked **Dead** and kept, with a short plain
reason (never database or exception text), for an operator.

Delivery is at least once, so a transaction can, rarely, be processed twice: when a node finishes a transaction and dies
before deleting the row. Put a unique reference in the transaction if the model must tolerate that.

An operator with the Read Write Model permission sees the queue at `GET /api/InboundQueue/Depth` (pending, processing,
dead) and `GET /api/InboundQueue/Dead`, and can requeue (`POST /api/InboundQueue/Dead/{id}/Requeue`) or delete
(`DELETE /api/InboundQueue/Dead/{id}`) a dead row. The dead list never returns a payload. The depth, the dead list,
requeue and delete are all scoped to the caller's tenant through the model each row belongs to, so one tenant never sees
another tenant's queue volume or rows. A row that has already been claimed `InboundQueueMaximumAttempts` times without
finishing, for example one that crashes the node every time, is marked Dead on the next claim rather than being retried
for ever.

A producer that cannot make HTTP calls can insert into `TransactionInboundQueue` directly (`EntityAnalysisModelGuid`,
`Payload`, `StatusId` 1, `AvailableDate`, `EnqueuedBy`, `CreatedDate`). That bypasses the API's checks, so grant it only
to a database role you control. In a cluster the `jube_app` role is given `DELETE` on this one table, and on no other.

The cost of this design is write load on the same Postgres that archives every transaction: an insert and a delete per
message, and ordinary vacuum work. It suits moderate volumes and integration-challenged sites; at sustained high volume
use RabbitMQ.

Administration > Inbound queue shows the pending, processing and dead counts, refreshed every ten seconds, and lists the
dead rows with the model, the number of attempts, the short reason and who enqueued each, with Requeue and Delete on
every row. It needs the Read Write Model permission.

<!-- TODO(screenshot): The Inbound queue page with dead rows. -->

## RabbitMQ

Publish to the `jubeInbound` queue with the header `EntityAnalysisModelGuid` set to the model's Guid. The consumer
starts whenever RabbitMQ is configured (it used to start only when `EnableCallback` was True, which is unrelated). It
holds at most `AmqpPrefetch` unacknowledged messages and acknowledges a message only after the model has processed it,
so a node that dies mid-message returns it to the queue. For a message it cannot process yet it puts the message back:
while the models are still loading it waits a second and requeues; for a failure, an unknown model or a model that is
not started it requeues once and, if the redelivery fails too, drops it rather than loop for ever. A message without the
header, or with one that is not a Guid, is dropped.

The queue is declared non-durable by default so as not to clash with queues created by earlier versions; see
`AmqpInboundQueueDurable`.

## Batches

Jube does not import batch files. A site that works in batches usually puts a small bridge in front, which posts each
transaction to the API or the inbound queue, or publishes it to RabbitMQ, and a bridge is the supported pattern.

<!-- TODO(docs): not built: waking idle nodes with LISTEN/NOTIFY instead of a quarter-second poll, per-model concurrency limits, and the inbound queue in the infrastructure health dashboards. -->
