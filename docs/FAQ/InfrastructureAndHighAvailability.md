---
layout: default
title: Infrastructure and High Availability
nav_order: 11
parent: FAQ
---

# Infrastructure and High Availability

## Deployment

**Q: Is Docker required?**

A: No, but Docker is the supported route. The recommended production approach is Docker Swarm using the Jube Cluster template and runbook, in the main repository under `Jube.Cluster`. See [Deploying With Jube Cluster](../GettingStarted/DeployingWithJubeCluster/index.html). For Kubernetes there is a chart, as described in [Deploying with Helm](../GettingStarted/DeployingWithHelm/index.html), which runs Jube alone and expects the database and cache to be provided.

**Q: What does the cluster contain?**

A: HAProxy, UI and API instances, a single job server, the R sandbox, PostgreSQL wrapped by Patroni with etcd for quorum, a Garnet cluster of four nodes kept by a cluster manager, and WAL shipping via pgBackRest, with a monitoring sidecar on each host. The cluster manager guards against split brain in the cache, in that a former primary returning from the dead is demoted and resynchronised rather than left to stand as a second primary. Redis under Sentinel, which Garnet replaced, is kept as an alternative compose file. See the [Deployment Runbook](../GettingStarted/DeployingWithJubeCluster/DeploymentRunbook/index.html) and the [Software Inventory](../GettingStarted/DeployingWithJubeCluster/SoftwareInventory/index.html).

**Q: Can the services run on immutable or minimal OS images?**

A: Yes. Nothing is needed at host level except Docker and the mounts; everything runs in containers, and the containers' write surface is confined to their volumes, so hardened, declarative OS images work well. The runbook carries Fedora and SELinux specifics.

**Q: How should secrets and images be handled?**

A: Run the `secrets-init.sh` script and capture its stdout once, as it generates credentials into Docker Swarm secrets and keeps no record of them on disk. etcd requires authentication with a Docker secret and a self-bootstrapping custom image built via `build-images.sh`. Stage images from an internal registry or tar files, because production hosts rarely reach public registries.

**Q: Why diff the cluster files when upgrading?**

A: Releases change cluster details: the PostgreSQL and etcd images and their authentication, the cache (Garnet having taken the place of Redis being a case in point), HAProxy settings, retry and stale-connection tuning, pgBackRest specifics. Always diff `Jube.Cluster` against your deployed copy and rehearse the runbook before upgrading.

## Operations

**Q: How does HAProxy know a node is healthy?**

A: For Jube it polls `/api/ready`, which answers only once the engine is ready (the cache converged, sanctions loaded, models synchronised) and stops answering upon the node being asked to stop. For PostgreSQL it queries Patroni's HTTP endpoint, so traffic always follows the current primary.

**Q: How do I do zero-downtime upgrades?**

A: Use `docker service update` (rolling) for each service. Always upgrade the jobs server first, because it runs the database migrations; wait until stable, then update the rest.

**Q: How long does failover take?**

A: Roughly 2 seconds for PostgreSQL. For the cache it depends on which is deployed: the Garnet cluster manager checks every five seconds and promotes a replica once the primary has been absent for three checks, so about 15 seconds, whereas Redis under Sentinel is about 5 seconds. External load-balancer retry settings can bridge that window without dropped transactions. Aggressive node cycling can leave Patroni "waiting for leader"; check etcd quorum, replication lag, and that the pgBackRest stanza exists.

{% comment %}TODO(verify): the figure of roughly 2 seconds for a PostgreSQL failover cannot be settled from the configuration, which has Patroni at ttl 30 and loop_wait 5 and HAProxy marking a server down after three failed checks three seconds apart. Confirm the figure by measurement, and whether it describes a switchover or the loss of the primary.{% endcomment %}

**Q: What is the backup strategy?**

A: PostgreSQL via pgBackRest: a full backup weekly, a differential on each of the other days, and a verify weekly, with WAL shipped continuously for point-in-time recovery, and retention (the last two full backups) set in the pgBackRest configuration. The pgBackRest volumes and the storage of the primary cache node are the true backing; rebuild everything else from configuration as code, and back up consistent volume snapshots.

**Q: Is there a single point of failure?**

A: For the processing of transactions, no. The job server is one instance by design, and its failure delays background tasks (archive writes catch up, synchronisation schedules wait) while transaction processing continues. The Garnet cluster manager is likewise a single instance, and its absence means only that a failed cache primary is not replaced until it returns.

## Sizing

**Q: What are the sizing guidelines?**

A: Three small VMs can handle about 1,400 transactions per second with full high availability. Memory for the cache is the main constraint, and the PostgreSQL cache offload reduces it. Final sizing needs real payload sizes and retention. Multi-zone cloud load balancing simplifies external networking; stretched clusters should watch network jitter and cache timeouts, for which backoff is tunable.

**Q: Why is PostgreSQL ingestion the bottleneck?**

A: Large archive rows with WAL and indexes are the slowest part. Beyond several thousand transactions per second, tune it: bulk copy, multiple writers, fewer indexes.

**Q: Does the runtime version matter?**

A: Jube runs on .NET 10, which is faster in places and required for the agent framework. Keep images current with the release you deploy.
