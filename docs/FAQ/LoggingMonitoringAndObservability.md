---
layout: default
title: Logging, Monitoring and Observability
nav_order: 12
parent: FAQ
---

# Logging, Monitoring and Observability

## Logging

**Q: How is logging configured?**

A: Log4Net, configured programmatically from environment variables, defaulting to warning and error. Log to standard
output so Docker collects it, and ship the Docker logs to your platform. Avoid heavy log stacks such as Elasticsearch;
the built-in observability pages cover most needs.

**Q: Why not leave info logging on?**

A: Info logging writes hundreds or thousands of messages per transaction and cuts throughput from about 1,800 to about
120 transactions per second. Use it for testing only.

**Q: How do I trace one transaction?**

A: The invocation trace is surfaced in the response and archive payloads, so start there. For deeper inspection, enable
info logging temporarily and grep the service logs for the transaction GUID.

## Monitoring

**Q: What should I monitor in production?**

A: Port polling and health checks first — they surface problems before logs do. Then the performance counter tables:
HTTP processing counters, model counters (volume, declines, average response time of about 5 ms or less), and the
asynchronous queue balances (archive, case creation, notifications). Also Redis memory and payload counts, the
write-ahead hash, and the local cache hit and miss tables.

**Q: Is there built-in observability tooling?**

A: Yes, extensively. Around 40 pages under the API concepts cover infrastructure health metrics, invocation trace logs,
application log entries, background processing performance, PostgreSQL statistics and WAF attack logs, alongside
activation and gateway rule match counters with history and reset, and compile and synchronisation error tables — all
designed to let an analyst find a bottleneck without infrastructure access. OpenTelemetry export is available
(`EnableOpenTelemetry`, `OpenTelemetryBackendEndpoint`, with a metric sample percentage; the `/api/invoke` ingress is
deliberately excluded from tracing to protect latency). An optional Docker monitoring sidecar holds the Docker socket
and exposes container health to Jube through `DockerMonitoringProxyUrl`, so the application never mounts the socket
itself. See the pages under [API Concepts](../Concepts/API/InfrastructureHealthMetrics/index.html).
