---
layout: default
title: Software Inventory
nav_order: 2
parent: Deploying with Jube Cluster
grand_parent: Getting Started
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Software Inventory

Every container image, package and runtime version that makes up the `Jube.Cluster` Docker Swarm stack, as defined in
`Jube.Cluster/docker-compose.yml` and the Dockerfiles it builds from. Useful for an upgrade, a security audit, or just
confirming what is actually running before raising a support ticket.

Two kinds of image make up the stack: pulled straight off the shelf (Redis, HAProxy, the two dashboards) and built
in-repo from a Dockerfile (etcd, Patroni, Jube itself). For the former, the version below is whatever tag
`docker-compose.yml` pins; for the latter, it's whatever the Dockerfile installs at build time. **Unpinned** means the
compose file or Dockerfile doesn't fix a version - it floats to whatever is current when the image is built or pulled,
and is worth locking down deliberately before a production build.

## Quick reference

| Component  | Version  |
|------------|----------|
| PostgreSQL | 17       |
| Patroni    | 4.1.5    |
| etcd       | v3.5.34  |
| Redis      | 7-alpine |
| HAProxy    | 2.8      |
| .NET       | 10.0     |
| Alpine     | 3.21     |

## Consensus & coordination

etcd is Patroni's leader-election store - five nodes, deliberately odd, so a network partition can't produce a tied vote
(see [Architecture Overview](../DeploymentRunbook/index.html#architecture-overview)).

Built from `Jube.Cluster/etcd/Dockerfile` - the stock `quay.io/coreos/etcd:v3.5.34` binaries (statically linked, so any
base image can run them) copied onto Alpine, with an entrypoint that bootstraps etcd's username/password auth once,
idempotently, on every node's own startup (creates a `root` user and a `patroni` user scoped to Patroni's DCS key
prefix, then enables auth - a no-op on every later restart once auth is already on). Every `etcdctl` call that
bootstrap makes, including the readiness and "is auth already on?" probes that decide whether it has anything left to
do, authenticates as `root` through `ETCDCTL_USER` rather than argv. It has to: once auth is on, an unauthenticated
`etcdctl endpoint health` is rejected like anything else, so a bootstrap that probed without credentials could never
reach its own no-op path and would instead re-probe every three seconds for the life of the container. The wait is
bounded as well, so a node that never becomes healthy gives up and says so rather than polling indefinitely. Tagged
and referenced as `${ETCD_IMAGE}` in the compose file, same convention as `${PATRONI_IMAGE}` below.

| Service           | Image           | Version  | Notes                                                                  |
|-------------------|-----------------|----------|-------------------------------------------------------------------------|
| `etcd1` … `etcd5` | `${ETCD_IMAGE}` | `v3.5.34`| Built, not pulled - see above. `ETCDCTL_API=3`. Client auth mandatory. |

## Database & backup

Built from `Jube.Cluster/patroni/Dockerfile` - a two-stage Alpine build that compiles Patroni into a virtualenv, then
lays it over a slim Postgres/pgBackRest runtime image. Tagged and referenced as `${PATRONI_IMAGE}` in the compose file
(see [Building and Distributing Images](../DeploymentRunbook/index.html#building-and-distributing-images)).

| Component    | Version    | Notes                                                                                                                                     |
|--------------|------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| Alpine Linux | 3.21       | Both build and runtime stages.                                                                                                            |
| PostgreSQL   | 17.x       | `postgresql17` / `postgresql17-client` / `postgresql17-dev` - Alpine 3.21's packaged 17 branch; patch version floats with the base image. |
| Patroni      | 4.1.5      | Pinned: `patroni[etcd3]==4.1.5`, installed via pip into a dedicated venv. 4.0.11/4.1.1 is the floor - see below.                          |
| psycopg2     | 2.9.9      | Pinned, Patroni's Postgres driver.                                                                                                        |
| pgBackRest   | *unpinned* | Alpine `pgbackrest` package - whatever the 3.21 repos currently carry.                                                                    |

Patroni's version floor here is set by etcd client authentication, not by anything Postgres-side. Patroni's etcd3
driver fetches the etcd member list to build its own endpoint failover list, and up to and including 3.3.2 it did that
**before** authenticating. Once `auth enable` has run, etcd rejects `member/list` with `etcdserver: user name is empty`,
so Patroni could never complete client construction, retried it every five seconds forever
(`waiting on etcd`, logged at INFO and therefore invisible under this cluster's `log.level: WARNING`), and the cluster
never came up. Patroni authenticates as part of that first member-list fetch from 4.0.11 and 4.1.1 onward, so any
Patroni older than those cannot be used with an auth-enabled etcd at all.

Upgrading an already-bootstrapped cluster to this image is a rolling restart, one member at a time
(`patronictl restart postgres-cluster <member>`), standbys before the primary, confirming
`patronictl list` shows the cluster healthy between each. Nothing in `patroni/patroni*.yml` changes: the whole of this
deployment's configuration validates identically under 3.3.2 and 4.1.5 (`patroni --validate-config`).

> **Dev-only comparison** - the repo-root `docker-compose.yml` (single-node evaluation setup, not part of the Swarm
> cluster) runs plain `postgres:17` rather than the Patroni-wrapped image above.

## Cache & high availability

Redis master/replica under Sentinel - five Sentinels, for the same odd-quorum reasoning as etcd. All six roles run the
same off-the-shelf image.

| Service                                               | Image            | Version | Notes                                                                                                    |
|-------------------------------------------------------|------------------|---------|----------------------------------------------------------------------------------------------------------|
| `redis-master`, `redis-replica1`-`3`, `sentinel1`-`5` | `redis:7-alpine` | 7.x     | Major version pinned; patch floats with the alpine tag. Same image serves both Redis and Sentinel roles. |

> **Dev-only comparison** - the repo-root `docker-compose.yml` instead runs `redis/redis-stack:latest` - a different
> image family, fully unpinned.

## Edge & load balancing

| Service   | Image     | Version | Notes                                                                                                                          |
|-----------|-----------|---------|--------------------------------------------------------------------------------------------------------------------------------|
| `haproxy` | `haproxy` | `2.8`   | Pinned in `docker-compose.yml`. Routes Postgres primary/replica traffic (via Patroni's REST API) and both Jube HTTP frontends. |

## Application tier

Built from `Jube.App/Dockerfile`, tagged as `${JUBE_IMAGE}` and shared by `jube-ui`, `jube-api` and `jube-jobs` - same
image, three different Environment Variable profiles.

| Component                            | Version   | Notes                             |
|--------------------------------------|-----------|-----------------------------------|
| `mcr.microsoft.com/dotnet/sdk`       | 10.0      | Build stage.                      |
| `mcr.microsoft.com/dotnet/aspnet`    | 10.0      | Runtime base for the final image. |
| Target framework (`Jube.App.csproj`) | `net10.0` |                                   |

> **Not part of the Swarm stack** - `Jube.LoadTest/Dockerfile` also targets `dotnet/sdk:9.0` and
> `dotnet/runtime:9.0`, but it's a standalone load-testing tool, not a service in `Jube.Cluster/docker-compose.yml`.

## Observability

Built from `Jube.Monitoring/Dockerfile`, tagged as `${JUBE_MONITORING_IMAGE}` - the one component in this section
that is Jube's own code, not a third-party image. Deployed `mode: global` (one instance per physical node, manager or
worker), since it polls the Docker Engine API local to whichever host
it runs on - see [Docker Monitoring Sidecar](../../../Concepts/API/InfrastructureHealthMetrics/index.html#docker-monitoring-sidecar-jubemonitoring)
for what it captures.

| Component                                   | Version   | Notes                                                                                                    |
|----------------------------------------------|-----------|-----------------------------------------------------------------------------------------------------------|
| `mcr.microsoft.com/dotnet/sdk`               | 10.0      | Build stage.                                                                                               |
| `mcr.microsoft.com/dotnet/runtime`           | 10.0      | Runtime base for the final image - a console process, not a web app, so `runtime` rather than `aspnet`.   |
| Target framework (`Jube.Monitoring.csproj`)  | `net10.0` |                                                                                                             |

Built from `Jube.OpenTelemetryListener/Dockerfile`, tagged as `${JUBE_OTEL_LISTENER_IMAGE}` - a throwaway local
OTLP/HTTP receiver for manually confirming OpenTelemetry data actually leaves the cluster during testing (prints a
best-effort summary of every request to console; not a spec-compliant collector). Single replica - see
[Log-Derived OpenTelemetry Counters](../../../Concepts/API/OpenTelemetryLogCounter/index.html).

| Component                                              | Version   | Notes                                                                                                    |
|---------------------------------------------------------|-----------|-----------------------------------------------------------------------------------------------------------|
| `mcr.microsoft.com/dotnet/sdk`                         | 10.0      | Build stage.                                                                                               |
| `mcr.microsoft.com/dotnet/aspnet`                      | 10.0      | Runtime base for the final image.                                                                          |
| Target framework (`Jube.OpenTelemetryListener.csproj`) | `net10.0` |                                                                                                             |

## Image tags resolved outside the repo

`${ETCD_IMAGE}`, `${PATRONI_IMAGE}`, `${JUBE_IMAGE}`, `${JUBE_MONITORING_IMAGE}` and `${JUBE_OTEL_LISTENER_IMAGE}`
aren't hardcoded anywhere in `docker-compose.yml` - they come from a `.env` file created at deploy time (not
committed), following the build-tag-load workflow in the
[Deployment Runbook](../DeploymentRunbook/index.html#building-and-distributing-images):

```bash
docker build --no-cache -f Jube.Cluster/etcd/Dockerfile -t jube.etcd:<sha> Jube.Cluster/etcd
docker build --no-cache -f Jube.Cluster/patroni/Dockerfile -t jube.patroni:<sha> .
docker build --no-cache -f Jube.App/Dockerfile -t jube.app:<sha> .
docker build --no-cache -f Jube.Monitoring/Dockerfile -t jube.monitoring:<sha> .
docker build --no-cache -f Jube.OpenTelemetryListener/Dockerfile -t jube.opentelemetrylistener:<sha> .
```

`Jube.Cluster/build-images.sh` automates all five of these, tagging with git HEAD's short SHA and updating
`Jube.Cluster/.env` directly.

`.env` then sets `ETCD_IMAGE=jube.etcd:<sha>`, `PATRONI_IMAGE=jube.patroni:<sha>`, `JUBE_IMAGE=jube.app:<sha>`,
`JUBE_MONITORING_IMAGE=jube.monitoring:<sha>` and `JUBE_OTEL_LISTENER_IMAGE=jube.opentelemetrylistener:<sha>`.

Images are distributed as tar files (`docker save` / `docker load`) rather than pulled from a registry - the standard
pattern for an air-gapped or tightly firewalled on-premises deployment, which this cluster is designed for. Whatever
date/tag was actually loaded on the running nodes is the ground truth, not this page - confirm with
`docker images` on each host.

## Sources

`Jube.Cluster/docker-compose.yml`, `Jube.Cluster/patroni/Dockerfile`, `Jube.App/Dockerfile`,
`Jube.Monitoring/Dockerfile`, `Jube.LoadTest/Dockerfile`, and the repo-root `docker-compose.yml`. Package versions inside Alpine-based images (marked
*unpinned*) reflect whatever the Alpine 3.21 package repositories carried at build time, not a version fixed in source -
re-check after any rebuild.
