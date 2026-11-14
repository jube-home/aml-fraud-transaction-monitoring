---
layout: default
title: Deploying with Helm
nav_order: 3
parent: Getting Started
---

# Deploying with Helm

The chart in `Jube.Helm` runs Jube on Kubernetes the way Jube Cluster runs it on Docker Swarm: one stateless image
started in three roles,
plus the R sandbox. The database and the cache are yours to provide.

| Role | What it does                                                                                                                                      | Default replicas |
|------|---------------------------------------------------------------------------------------------------------------------------------------------------|------------------|
| jobs | Runs the database migration at start, and the background work: training, reprocessing, the sanctions loader, case automation, the callback queues | 1                |
| api  | Runs the engine, takes transaction invocations (`/api/invoke`) and the landlord functions                                                         | 2                |
| ui   | Serves the web application and its API, with no engine                                                                                            | 2                |

The settings of each role are the ones the swarm file gives it (the chart's `values.yaml` was generated from
`Jube.Cluster/docker-compose.yml`), so a deployment tested on the cluster behaves the same here.
Settings the three roles share are under `env.common`, the rest under `env.ui`, `env.api` and `env.jobs`; any Jube
environment variable
can be added or overridden there.

## Install

1. Provide Postgres with the pgvector extension and `pg_stat_statements` in `shared_preload_libraries`, with the roles
   from `Jube.Cluster/database.sh` (`jube_app`, `jube_reporting`, `jube_migration`, `jube_purge`), and a Redis-protocol
   cache such as Garnet.
2. Make a Kubernetes Secret with these keys, each a file the application reads from `/run/secrets`: `ASK_JOOBY_API_KEY`,
   `JUBE_APP_PASSWORD`, `JUBE_REPORTING_PASSWORD`, `JUBE_MIGRATION_PASSWORD`, `JUBE_PURGE_PASSWORD`, `JWT_KEY`,
   `PASSWORD_HASHING_KEY`, `RESP_PASSWORD`, `API_HMAC_KEY`, `ENCRYPTION_KEY`, `CASE_SIMILARITY_HASHING_KEY`,
   `PASSWORD_ASYMMETRIC_ENCRYPTION_PRIVATE_KEY` and `PATRONI_ETCD3_PASSWORD`. `Jube.Cluster/secrets-init.sh` shows how
   the cluster makes them.
3. Install, naming the image you built or pulled, the database, the cache and the public address:

```
helm install jube ./Jube.Helm \
  --set image.repository=registry.example.com/jube.app --set image.tag=1.0.0 \
  --set secrets.existingSecret=jube-secrets \
  --set postgres.host=postgres.example.internal \
  --set "resp.connectionString=cache.example.internal:6379\,password=[@RESP_PASSWORD@]" \
  --set publicUrl=https://jube.example.com \
  --set passwordAsymmetricEncryptionPublicKey=... \
  --set ingress.enabled=true --set ingress.host=jube.example.com
```

The api and ui pods have an init container that waits for the jobs role to answer `/api/ready`, which it only does once
the migration has finished, so
the first install settles by itself in a few minutes. Readiness, liveness and startup probes all use `/api/ready`; pods
get 60 seconds to drain on SIGTERM, as [Orchestration](../../Concepts/Orchestration/index.html) describes.

## Read replicas

Set `postgres.replicaHost` (and `replicaPort`) to send the reporting connection to a replica, as the swarm's port 5433
does. Left empty, reporting uses the primary.

<!-- TODO(docs): the chart has been linted and rendered with Helm 3.15 (the helm workflow does this on every change) but has never been installed on a cluster; the first real install will show what is missing. -->
<!-- TODO(docs): not in the chart: Patroni, etcd and HAProxy (use a managed Postgres or an operator), Garnet or Redis, jube-monitoring (it reads the Docker socket, which Kubernetes does not offer), the OpenTelemetry listener, a HorizontalPodAutoscaler, a PodDisruptionBudget, Kerberos keytab mounts for Negotiate sign-in and pod anti-affinity. -->
<!-- TODO(docs): with secrets.create the secrets sit in your values file and the Helm release history; prefer an existing Secret, ideally from an external secrets operator. -->
<!-- TODO(docs): EnableOpenTelemetry is False for api and jobs as in the swarm; with no listener deployed, leave it. -->
<!-- TODO(docs): the migration runs in the jobs pod at start, so scaling jobs above one starts a second migrator racing the first; keep jobs at one replica until a leader election or an advisory lock around the migration is confirmed. -->
<!-- TODO(docs): the jube-r network policy denies all egress and allows ingress only from Jube pods; confirm the CNI in use enforces NetworkPolicy. -->
<!-- TODO(docs): the image reference defaults to jube.app:latest; pin a version or a digest (the release workflow publishes signed tarballs, and a registry push is undecided). -->
