---
layout: default
title: Software Inventory
nav_order: 2
parent: Deploying with Jube Cluster
grand_parent: Getting Started
---

# Software Inventory

This page lists every container image, package and runtime version that makes up the `Jube.Cluster` Docker Swarm stack, as defined in `Jube.Cluster/docker-compose.yml` and the Dockerfiles it builds from. It is useful for an upgrade, a security audit, or for the purposes of confirming what is actually running before raising a support ticket.

Two kinds of image make up the stack, being those pulled straight off the shelf (Garnet, HAProxy, and the Redis image from which the Garnet cluster manager borrows `redis-cli`) and those built in the repository from a Dockerfile (etcd, Patroni, the R sandbox and Jube itself). For the former, the version below is whatever tag or digest `docker-compose.yml` pins, and for the latter it is whatever the Dockerfile installs at build time. **Unpinned** means that the compose file or Dockerfile does not fix a version, which is to say that it floats to whatever is current when the image is built or pulled, and it is worth locking down deliberately before a production build.

## Quick reference

| Component  | Version                    |
|------------|----------------------------|
| PostgreSQL | 17                         |
| Patroni    | 4.1.5                      |
| etcd       | v3.5.34                    |
| Garnet     | `latest`, pinned by digest |
| Redis      | 7-alpine                   |
| HAProxy    | 2.8                        |
| .NET       | 10.0                       |
| R          | 4.5.1                      |
| Alpine     | 3.21                       |

## Consensus & coordination

etcd is Patroni's leader-election store, having five nodes, deliberately odd, such that a network partition cannot produce a tied vote (see [Architecture Overview](../DeploymentRunbook/index.html#architecture-overview)).

It is built from `Jube.Cluster/etcd/Dockerfile`, being the stock `quay.io/coreos/etcd:v3.5.34` binaries (statically linked, hence any base image can run them) copied onto Alpine, with an entrypoint that bootstraps etcd's username and password auth once, idempotently, on the startup of every node (it creates a `root` user and a `patroni` user scoped to Patroni's DCS key prefix, then enables auth, which is a no-op on every later restart once auth is already on). Every `etcdctl` call that the bootstrap makes, including the readiness and "is auth already on?" probes that decide whether it has anything left to do, authenticates as `root` through `ETCDCTL_USER` rather than argv. It has to, as once auth is on an unauthenticated `etcdctl endpoint health` is rejected like anything else, and a bootstrap that probed without credentials could never reach its own no-op path and would instead probe again every three seconds for the life of the container. The wait is bounded as well, so a node that never becomes healthy gives up and says so rather than polling indefinitely. The image is tagged and referenced as `${ETCD_IMAGE}` in the compose file, the same convention as `${PATRONI_IMAGE}` below.

| Service           | Image           | Version   | Notes                                                                  |
|-------------------|-----------------|-----------|------------------------------------------------------------------------|
| `etcd1` … `etcd5` | `${ETCD_IMAGE}` | `v3.5.34` | Built, not pulled, see above. `ETCDCTL_API=3`. Client auth mandatory.  |

## Database & backup

The database image is built from `Jube.Cluster/patroni/Dockerfile`, a two-stage Alpine build that compiles Patroni into a virtualenv, then lays it over a slim Postgres and pgBackRest runtime image. It is tagged and referenced as `${PATRONI_IMAGE}` in the compose file (see [Building and Distributing Images](../DeploymentRunbook/index.html#building-and-distributing-images)).

| Component    | Version    | Notes                                                                                                                                                                        |
|--------------|------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Alpine Linux | 3.21       | Both build and runtime stages.                                                                                                                                               |
| PostgreSQL   | 17.x       | `postgresql17` / `postgresql17-client` / `postgresql17-contrib`, with `postgresql17-dev` in the build stage. Alpine 3.21's packaged 17 branch; patch version floats with the base image. |
| pgvector     | *unpinned* | Alpine `postgresql-pgvector` package, whatever the 3.21 repositories currently carry.                                                                                        |
| Patroni      | 4.1.5      | Pinned: `patroni[etcd3]==4.1.5`, installed via pip into a dedicated venv. 4.0.11/4.1.1 is the floor, see below.                                                              |
| psycopg2     | 2.9.9      | Pinned, Patroni's Postgres driver.                                                                                                                                           |
| pgBackRest   | *unpinned* | Alpine `pgbackrest` package, whatever the 3.21 repositories currently carry.                                                                                                 |

Patroni's version floor here is set by etcd client authentication, not by anything on the side of Postgres. Patroni's etcd3 driver fetches the etcd member list to build its own endpoint failover list, and up to and including 3.3.2 it did that **before** authenticating. Once `auth enable` has run, etcd rejects `member/list` with `etcdserver: user name is empty`, so Patroni could never complete client construction, retried it every five seconds forever (`waiting on etcd`, logged at INFO and therefore invisible under this cluster's `log.level: WARNING`), and the cluster never came up. Patroni authenticates as part of that first member-list fetch from 4.0.11 and 4.1.1 onward, and it follows that any Patroni older than those cannot be used with an auth-enabled etcd at all.

Upgrading an already-bootstrapped cluster to this image is a rolling restart, one member at a time (`patronictl restart postgres-cluster <member>`), standbys before the primary, confirming that `patronictl list` shows the cluster healthy between each. Nothing in `patroni/patroni*.yml` changes, as the whole of this deployment's configuration validates identically under 3.3.2 and 4.1.5 (`patroni --validate-config`).

The image also wraps `patronictl` itself. Patroni's etcd credential arrives as a Docker secret that the entrypoint `export`s into the Patroni process, which leaves a `docker exec` shell holding `PATRONI_ETCD3_USERNAME` and no password, such that operator commands were refused by the same authenticated etcd that the daemon talks to perfectly well. `/usr/local/bin/patronictl` sits ahead of the real binary on `PATH`, loads the secrets from the one definition the entrypoint also sources, and hands off, as described in [Creating Secrets](../DeploymentRunbook/index.html#creating-secrets) in the Deployment Runbook.

> **Dev-only comparison**: the repo-root `docker-compose.yml` (the single-node evaluation setup, not part of the Swarm cluster) runs the `pgvector/pgvector:pg17` image, pinned by digest, rather than the Patroni-wrapped image above.

## Cache & high availability

The cache is a Garnet cluster of one shard, `garnet1` being the primary and owning all of the slots, with `garnet2` to `garnet4` as replicas. Membership and the detection of failure is Garnet's own gossip, in the manner of Redis Cluster, while `garnet-cluster-manager` performs the bootstrap of a fresh cluster and the promotion of a replica upon the loss of the primary, which Garnet's gossip deliberately does not do itself. The cluster manager is a shell script (`Jube.Cluster/garnet/cluster-manager.sh`) mounted into the stock Redis image, used only for the `redis-cli` that it carries.

| Service                  | Image                                | Version                    | Notes                                                                                                  |
|--------------------------|--------------------------------------|----------------------------|--------------------------------------------------------------------------------------------------------|
| `garnet1` … `garnet4`    | `ghcr.io/microsoft/garnet`           | `latest`, pinned by digest | The digest in `docker-compose.yml` fixes the image exactly, whatever `latest` has since moved to.      |
| `garnet-cluster-manager` | `redis:7-alpine`                     | 7.x                        | Major version pinned; patch floats with the alpine tag. Runs `redis-cli` only, never a Redis server.   |

The alternative `Jube.Cluster/docker-compose.redis.yml` runs Redis master and replica under Sentinel in place of Garnet, with five Sentinels for the same odd-quorum reasoning as etcd. All of its roles run the same off-the-shelf image.

| Service                                               | Image            | Version | Notes                                                                                                    |
|-------------------------------------------------------|------------------|---------|----------------------------------------------------------------------------------------------------------|
| `redis-master`, `redis-replica1`-`3`, `sentinel1`-`5` | `redis:7-alpine` | 7.x     | Major version pinned; patch floats with the alpine tag. Same image serves both Redis and Sentinel roles. |

> **Dev-only comparison**: the repo-root `docker-compose.yml` runs the same Garnet image, pinned by the same digest, as a single node. The repo-root `docker-compose.redis.yml` instead runs `redis/redis-stack:latest`, a different image family, fully unpinned.

## Edge & load balancing

| Service   | Image     | Version | Notes                                                                                                                          |
|-----------|-----------|---------|--------------------------------------------------------------------------------------------------------------------------------|
| `haproxy` | `haproxy` | `2.8`   | Pinned in `docker-compose.yml`. Routes Postgres primary/replica traffic (via Patroni's REST API) and both Jube HTTP frontends. |

## Application tier

The application image is built from `Jube.App/Dockerfile`, tagged as `${JUBE_IMAGE}` and shared by `jube-ui`, `jube-api` and `jube-jobs`, which is to say the same image with three different Environment Variable profiles.

| Component                            | Version   | Notes                             |
|--------------------------------------|-----------|-----------------------------------|
| `mcr.microsoft.com/dotnet/sdk`       | 10.0      | Build stage.                      |
| `mcr.microsoft.com/dotnet/aspnet`    | 10.0      | Runtime base for the final image. |
| Target framework (`Jube.App.csproj`) | `net10.0` |                                   |

The R sandbox, `jube-r`, is an image of its own, built from `Jube.R/Dockerfile` with `Jube.R` as its context and tagged as `${JUBE_R_IMAGE}`. It is the sandboxed R server in which R scripts are executed, reached by Jube over `RServerUrl`, and runs as two replicas from the first deploy as it has no migration to wait upon.

| Component       | Version | Notes                                                           |
|-----------------|---------|-----------------------------------------------------------------|
| `rocker/r-ver`  | 4.5.1   | Build and runtime stages, set by the `R_VERSION` build argument. |

> **Not part of the Swarm stack**: `Jube.LoadTest/Dockerfile` targets `dotnet/sdk:10.0` and `dotnet/runtime:10.0`, but it is a standalone load-testing tool, not a service in `Jube.Cluster/docker-compose.yml`.

## Observability

`jube-monitoring` is built from `Jube.Monitoring/Dockerfile` and tagged as `${JUBE_MONITORING_IMAGE}`, being the one component in this section that is Jube's own code and not a third-party image. It is deployed `mode: global` (one instance per physical node, manager or worker), since it polls the Docker Engine API local to whichever host it runs on. See [Docker Monitoring Sidecar](../../../Concepts/API/InfrastructureHealthMetrics/index.html#docker-monitoring-sidecar-jubemonitoring) for what it captures.

| Component                                   | Version   | Notes                                                                                                         |
|---------------------------------------------|-----------|---------------------------------------------------------------------------------------------------------------|
| `mcr.microsoft.com/dotnet/sdk`              | 10.0      | Build stage.                                                                                                  |
| `mcr.microsoft.com/dotnet/aspnet`           | 10.0      | Runtime base for the final image, as the sidecar also answers HTTP on port 8081 as a proxy to the Docker socket. |
| Target framework (`Jube.Monitoring.csproj`) | `net10.0` |                                                                                                               |

`jube-otel-listener` is built from `Jube.OpenTelemetryListener/Dockerfile` and tagged as `${JUBE_OTEL_LISTENER_IMAGE}`. It is a throwaway local OTLP/HTTP receiver for manually confirming that OpenTelemetry data actually leaves the cluster during testing (it prints a best-effort summary of every request to console, and is not a spec-compliant collector). It runs as a single replica, see [Log-Derived OpenTelemetry Counters](../../../Concepts/API/OpenTelemetryLogCounter/index.html).

| Component                                              | Version   | Notes                             |
|--------------------------------------------------------|-----------|-----------------------------------|
| `mcr.microsoft.com/dotnet/sdk`                         | 10.0      | Build stage.                      |
| `mcr.microsoft.com/dotnet/aspnet`                      | 10.0      | Runtime base for the final image. |
| Target framework (`Jube.OpenTelemetryListener.csproj`) | `net10.0` |                                   |

## Image tags resolved outside the repo

`${ETCD_IMAGE}`, `${PATRONI_IMAGE}`, `${JUBE_IMAGE}`, `${JUBE_MONITORING_IMAGE}`, `${JUBE_OTEL_LISTENER_IMAGE}` and `${JUBE_R_IMAGE}` are not hardcoded anywhere in `docker-compose.yml`. They come from a `.env` file created at deploy time (not committed), following the build-tag-load workflow in the [Deployment Runbook](../DeploymentRunbook/index.html#building-and-distributing-images):

```bash
docker build --no-cache -f Jube.Cluster/etcd/Dockerfile -t jube.etcd:<sha> Jube.Cluster/etcd
docker build --no-cache -f Jube.Cluster/patroni/Dockerfile -t jube.patroni:<sha> .
docker build --no-cache -f Jube.App/Dockerfile -t jube.app:<sha> .
docker build --no-cache -f Jube.Monitoring/Dockerfile -t jube.monitoring:<sha> .
docker build --no-cache -f Jube.OpenTelemetryListener/Dockerfile -t jube.opentelemetrylistener:<sha> .
docker build --no-cache -f Jube.R/Dockerfile -t jube.r:<sha> Jube.R
```

`Jube.Cluster/build-images.sh` automates all six of these, tagging with the short SHA of git HEAD and updating `Jube.Cluster/.env` directly.

`.env` then sets `ETCD_IMAGE=jube.etcd:<sha>`, `PATRONI_IMAGE=jube.patroni:<sha>`, `JUBE_IMAGE=jube.app:<sha>`, `JUBE_MONITORING_IMAGE=jube.monitoring:<sha>`, `JUBE_OTEL_LISTENER_IMAGE=jube.opentelemetrylistener:<sha>` and `JUBE_R_IMAGE=jube.r:<sha>`.

Images are distributed as tar files (`docker save` / `docker load`) rather than pulled from a registry, which is the standard pattern for an air-gapped or tightly firewalled on-premises deployment, for which this cluster is designed. Whatever date or tag was actually loaded on the running nodes is the ground truth, not this page, and it is to be confirmed with `docker images` on each host.

## Sources

`Jube.Cluster/docker-compose.yml`, `Jube.Cluster/docker-compose.redis.yml`, `Jube.Cluster/etcd/Dockerfile`, `Jube.Cluster/patroni/Dockerfile`, `Jube.App/Dockerfile`, `Jube.R/Dockerfile`, `Jube.Monitoring/Dockerfile`, `Jube.OpenTelemetryListener/Dockerfile`, `Jube.LoadTest/Dockerfile`, and the repo-root `docker-compose.yml`. Package versions inside Alpine-based images (marked *unpinned*) reflect whatever the Alpine 3.21 package repositories carried at build time, not a version fixed in source, and are to be checked again after any rebuild.
