---
layout: default
title: Supply Chain Assurance
nav_order: 15
parent: Concepts
---

# Supply Chain Assurance

Supply Chain Assurance sets out what a deployer can check about the software being run, which is to say what is in it, where it came from and that it has not been altered.

## Software bill of materials

The `supply-chain` workflow runs on every push to `master`, on every release tag and on request. It publishes two CycloneDX JSON documents as the `sbom` artifact:

| File                 | Contents                                                                                                           |
|----------------------|--------------------------------------------------------------------------------------------------------------------|
| jube-dotnet.cdx.json | Every NuGet package of the solution, with versions, licences and package URLs, development-only packages excluded. |
| jube-vue.cdx.json    | Every npm package that ships in the web front end, development dependencies excluded.                              |

The documents can be fed to any SBOM consumer (Dependency-Track, Grype, Trivy, a procurement questionnaire) for the purposes of answering whether the software contains a given component. The older hand-maintained [Package Dependency Appendix](../../PackageDependencyAppendix.html) remains for licence review, but where it and the SBOM disagree, it is the SBOM that is right.

## Signed releases

A release tag (`v*`) additionally builds the images (`jube.app`, `jube.monitoring`, `jube.opentelemetrylistener` and `jube.r`), saves each as a compressed tarball, adds the SBOMs and a `SHA256SUMS` file, records a build provenance attestation for every file (the attestation of GitHub, which names the repository, the workflow, the commit and the runner) and signs every file with cosign using the identity of the workflow itself, there being no long-lived key to guard. Each file has a `.sigstore.json` bundle beside it.

Verification instructions are in `SECURITY.md` at the root of the repository.

A release run ends by creating a draft GitHub release holding the signed tarballs, the SBOMs, `SHA256SUMS`, the cosign bundles and the provenance, for a person to review and publish. Nothing is pushed to any registry, as the project builds from master, and the pushing of signed images to a public registry (and verifying them with `cosign verify` against an image reference) is deliberately not done.

{% comment %}TODO(verify): the jube.etcd, jube.patroni and jube-loadtest images are scanned daily but are not part of the signed release; owner to decide whether the cluster images belong in it{% endcomment %}

{% comment %}TODO(verify): the release job of the supply-chain workflow has not yet run; the first tag will show whether CycloneDX and cyclonedx-npm accept the solution layout and whether the 90 minute limit is enough for four image builds{% endcomment %}

## Vulnerability scanning

| Check             | What it covers                                                                                               | When                                     |
|-------------------|--------------------------------------------------------------------------------------------------------------|------------------------------------------|
| CodeQL            | C# and JavaScript/TypeScript source                                                                          | Every push and pull request, weekly      |
| Trivy             | The seven container images (app, monitoring, OpenTelemetry listener, etcd, Patroni, R sandbox and load test) | Daily; a fixable critical fails it       |
| OWASP ZAP         | A running stack through the API and the web application                                                      | Daily when the code or the scan is stale |
| Dependency review | See the SBOM above                                                                                           | On demand                                |

{% comment %}TODO(verify): Dependency review is listed as on demand; owner to decide whether the GitHub dependency review action is to be added to pull requests{% endcomment %}

## Pinned base images

The third party images that the compose files start, being the pgvector Postgres image and the Garnet cache, are pinned by digest, such that a compose file deploys exactly the image that was tested, and not whatever the tag points at today. To take an update, it is a matter of pulling the new image, reading its release notes, and changing the digest in `docker-compose.yml`, `docker-compose.redis.yml` and `Jube.Cluster/docker-compose.yml` together.

The images from which the Dockerfiles start (the .NET runtime and SDK, R and the others) are referenced by tag, not digest, and it is the daily Trivy scan of the result that is the control for them.

The alternative compose file docker-compose.redis.yml, which starts Redis in place of Garnet, pins both the pgvector image and the redis/redis-stack image by tag and digest.
