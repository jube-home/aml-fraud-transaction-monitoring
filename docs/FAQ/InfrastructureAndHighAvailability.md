---
layout: default
title: Infrastructure and High Availability
nav_order: 11
parent: FAQ
---

# Infrastructure and High Availability

## Deployment

**Q: Is Docker required?**

A: No, but Docker is the supported route. The recommended production approach is Docker Swarm using the Jube Cluster
template and runbook, in the main repository under `Jube.Cluster`. See
[Deploying With Jube Cluster](../GettingStarted/DeployingWithJubeCluster/index.html).

**Q: What does the cluster contain?**

A: HAProxy, UI and API instances, a single job server, PostgreSQL wrapped by Patroni with etcd for quorum, Redis with
Sentinel, and log shipping via pgBackRest. An entry point guards against Redis split brain.

**Q: Can the services run on immutable or minimal OS images?**

A: Yes. Nothing is needed at host level except Docker and the mounts; everything runs in containers, and the containers'
write surface is confined to their volumes, so hardened, declarative OS images work well. The runbook carries Fedora and
SELinux specifics.

**Q: How should secrets and images be handled?**

A: Run the `secrets-init.sh` script and capture its stdout once — it generates credentials into Docker Swarm secrets and
nothing is written to disk. etcd requires authentication with a Docker secret and a self-bootstrapping custom image
built via `build-images.sh`. Stage images from an internal registry or tar files, because production hosts rarely reach
public registries.

**Q: Why diff the cluster files when upgrading?**

A: Releases change cluster details: the PostgreSQL and etcd images and their authentication, HAProxy settings, retry and
stale-connection tuning, pgBackRest specifics. Always diff `Jube.Cluster` against your deployed copy and rehearse the
runbook before upgrading.

## Operations

**Q: How does HAProxy know a node is healthy?**

A: For Jube it polls `/api/ready` (engine started, models synchronised). For PostgreSQL it queries Patroni's HTTP
endpoint, so traffic always follows the current primary.

**Q: How do I do zero-downtime upgrades?**

A: Use `docker service update` (rolling) for each service. Always upgrade the jobs server first, because it runs the
database migrations; wait until stable, then update the rest.

**Q: How long does failover take?**

A: Roughly 2 seconds for PostgreSQL and about 5 seconds for Redis. External load-balancer retry settings can bridge that
window without dropped transactions. Aggressive node cycling can leave Patroni "waiting for leader"; check etcd quorum,
replication lag, and that the pgBackRest stanza exists.

**Q: What is the backup strategy?**

A: PostgreSQL via pgBackRest: weekly full, daily differential, weekly verify, with WAL shipped continuously for
point-in-time recovery and retention controlled in the Patroni configuration. The pgBackRest volumes and the primary
Redis storage are the true backing; rebuild everything else from configuration as code, and back up consistent volume
snapshots.

**Q: Is there a single point of failure?**

A: Only the job server (one instance by design). Its failure delays background tasks — archive writes catch up,
synchronisation schedules wait — but transaction processing continues.

## Sizing

**Q: What are the sizing guidelines?**

A: Three small VMs can handle about 1,400 transactions per second with full high availability. Memory (Redis) is the
main constraint, and the PostgreSQL cache offload reduces it. Final sizing needs real payload sizes and retention.
Multi-zone cloud load balancing simplifies external networking; stretched clusters should watch network jitter and Redis
timeouts, for which backoff is tunable.

**Q: Why is PostgreSQL ingestion the bottleneck?**

A: Large archive rows with WAL and indexes are the slowest part. Beyond several thousand transactions per second, tune
it: bulk copy, multiple writers, fewer indexes.

**Q: Does the runtime version matter?**

A: Jube runs on .NET 10, which is faster in places and required for the agent framework. Keep images current with the
release you deploy.
