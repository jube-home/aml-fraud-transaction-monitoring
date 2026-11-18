---
layout: default
title: Deploying with Helm
nav_order: 3
parent: Getting Started
---

# Deploying with Helm

The chart in `Jube.Helm` runs Jube on Kubernetes in the manner that Jube Cluster runs it on Docker Swarm, which is to say one stateless image started in three roles, plus the R sandbox. The database and the cache are not part of the chart and are to be provided.

| Role | What it does                                                                                                                                      | Default replicas |
|------|---------------------------------------------------------------------------------------------------------------------------------------------------|------------------|
| jobs | Runs the database migration at start, and the background work: training, reprocessing, the sanctions loader, case automation, the callback queues | 1                |
| api  | Runs the engine, takes transaction invocations (`/api/invoke`) and the landlord functions                                                         | 2                |
| ui   | Serves the web application and its API, with no engine                                                                                            | 2                |

The settings of each role are the ones the swarm file gives it, the chart's `values.yaml` having been generated from `Jube.Cluster/docker-compose.yml`, and it follows that a deployment tested on the cluster behaves the same here. Settings the three roles share are under `env.common`, the rest under `env.ui`, `env.api` and `env.jobs`, and any Jube Environment Variable can be added or overridden there.

## Install

1. Provide Postgres with the pgvector extension and `pg_stat_statements` in `shared_preload_libraries`, with the roles from `Jube.Cluster/database.sh` (`jube_app`, `jube_reporting`, `jube_migration`, `jube_purge`), and a cache speaking the Resp protocol, such as Garnet.
2. Make a Kubernetes Secret with the following keys, each a file the application reads from `/run/secrets`: `ASK_JOOBY_API_KEY`, `JUBE_APP_PASSWORD`, `JUBE_REPORTING_PASSWORD`, `JUBE_MIGRATION_PASSWORD`, `JUBE_PURGE_PASSWORD`, `JWT_KEY`, `PASSWORD_HASHING_KEY`, `RESP_PASSWORD`, `API_HMAC_KEY`, `ENCRYPTION_KEY`, `CASE_SIMILARITY_HASHING_KEY`, `PASSWORD_ASYMMETRIC_ENCRYPTION_PRIVATE_KEY` and `PATRONI_ETCD3_PASSWORD`. `Jube.Cluster/secrets-init.sh` shows how the cluster makes them.
3. Install, naming the image built or pulled, the database, the cache and the public address:

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

The api and ui pods have an init container that waits for the jobs role to answer `/api/ready`, which it does only once the migration has finished, hence the first install settles by itself in a few minutes. Readiness, liveness and startup probes all use `/api/ready`, and pods are given 60 seconds to drain on SIGTERM, as [Orchestration](../../Concepts/Orchestration/index.html) describes.

## Read replicas

Set `postgres.replicaHost` (and `replicaPort`) to send the reporting connection to a replica, as port 5433 of the swarm does. Left empty, reporting uses the primary.

## What the chart does not include

The chart deploys Jube and the R sandbox only, and the following are absent from it:

* Patroni, etcd and HAProxy, for which a managed Postgres or an operator is to be used.
* Garnet or Redis.
* jube-monitoring, as it reads the Docker socket, which Kubernetes does not offer.
* The OpenTelemetry listener. `EnableOpenTelemetry` is False for the api and jobs roles, as in the swarm, and with no listener deployed it should be left so.
* A HorizontalPodAutoscaler and a PodDisruptionBudget.
* Kerberos keytab mounts for Negotiate sign-in.
* Pod anti-affinity.

## Points to keep in mind

* **Secrets.** With `secrets.create` the secrets sit in the values file and in the Helm release history. An existing Secret named in `secrets.existingSecret` is to be preferred, ideally one maintained by an external secrets operator.
* **The jobs role stays at one replica.** The migration runs in the jobs pod at start, such that scaling jobs above one starts a second migrator racing the first.
* **The R sandbox relies on NetworkPolicy.** The network policy of `jube-r` denies all egress and allows ingress only from the Jube pods, which holds only where the CNI in use enforces NetworkPolicy, and this should be confirmed for the cluster in question.
* **The image is not pinned.** The image reference defaults to `jube.app:latest`, and a version or a digest should be pinned in its place.

{% comment %}TODO(verify): the templates under Jube.Helm/templates as committed have their template delimiters written with spaces between the braces, in the manner of an editor reformat, and Helm is unlikely to lint or render them as they stand. Run helm lint and helm template (the helm workflow) and correct the templates if so.{% endcomment %}

{% comment %}TODO(verify): the chart had been linted and rendered with Helm 3.15, by the helm workflow on every change, but had never been installed on a cluster. Confirm whether a real install has since taken place, and whether a leader election or an advisory lock around the migration now allows the jobs role to scale beyond one.{% endcomment %}
