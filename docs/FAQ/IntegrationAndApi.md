---
layout: default
title: Integration and API
nav_order: 9
parent: FAQ
---

# Integration and API

## Invoking models

**Q: How do I send a transaction?**

A: POST JSON to the model's invoke endpoint (`/api/Invoke/EntityAnalysisModel/{guid}`) with an `X-API-KEY` header.

**Q: Should the UI and API endpoints be separated?**

A: Yes. Use separate subdomains and external routing, since UI and API are the same binary with different environment
settings. Restrict the API subdomain to known IPs, or only to `api/invoke`, at the load balancer, and leave
`EnablePublicInvokeController` off on UI-only nodes so invoke traffic is refused there.

**Q: Can I process asynchronously?**

A: Yes, two ways. Appending `/Async` validates and extracts the payload, queues the rest (the queue is capped by
`MaximumModelInvokeAsyncQueue`, default 10,000, returning 429 above it) and returns a callback token immediately; a
`GET .../Callback/{guid}` collects the result exactly once, returning 408 on timeout, and callback events are published
cluster-wide so any node can answer. Alternatively, a model can enable Implicit Async: a normal synchronous call waits
up to the model's timeout (default 5 seconds) and, if processing isn't done, returns a response marked
`ImplicitAsyncTimedOut` while the work completes in the background — the task is never dropped, and its eventual
completion or failure is tracked. Synchronous integration with a strict timeout remains the most common pattern. See
[Asynchronous Model Invocation](../Concepts/API/HTTPAsynchronousModelInvocation/index.html).

**Q: Is idempotency enforced on the transaction key?**

A: At activation level, yes: TTL counter updates, case creation and notifications each fire only once per transaction.
Duplicate posts of the same transaction ID are still processed twice at ingestion.

## Messaging

**Q: Should I use AMQP/RabbitMQ?**

A: It remains supported in both directions — inbound consumption from the `jubeInbound` queue (with the model GUID as a
header) and outbound publication of the response payload to the `jubeOutbound` exchange, plus notification and
activation feeds — but it appears in few implementations, and plain HTTP is the primary integration path. Where AMQP
earns its keep is outbound: publishing the response payload to an exchange for downstream pipelines. See
[AMQP](../Concepts/API/AMQP/index.html).

## Performance

**Q: What is the latency and throughput?**

A: Typically 1.5 to 5 ms per transaction, and about 1,400 to 2,000 transactions per second on modest hardware. The
biggest linear performance cost per invocation is abstraction-rule search keys — each one is a cache query per
transaction — which is another reason the [Rule Taxonomy](../RuleTaxonomy/index.html) urges a minimal cache
footprint and TTL counters for the bulk of state.

**Q: Can I do batch or back-processing?**

A: Possible but not recommended in production. [Reprocessing](../Configuration/Reprocessing/index.html) replays archive
records through the pipeline. Only rules with "enable reprocessing" set take part, results update the archive with old
versions preserved in ArchiveVersion, and activation-level idempotency prevents double-counting. Tread lightly in
production.

## API surface

**Q: Is there Swagger/API documentation?**

A: Yes, via `/swagger`. Newer APIs are also decorated with the descriptions that the AI agent tooling consumes.

**Q: Does the Activation Watcher work behind a load balancer?**

A: Yes, it is fully stateless. The feed is Server-Sent Events: the engine publishes each activation to a per-tenant
Redis pub/sub channel, every node subscribes to the fan-out and pushes to its own connected clients, so no server
affinity is needed and least-connections or round-robin balancing both work. A heartbeat
(`WatcherStreamHeartbeatIntervalSeconds`) keeps intermediaries from closing idle streams, and replay is served from the
database when persistence is enabled. The map feature needs an image source allowance in the CSP.
