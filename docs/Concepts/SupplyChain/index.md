---
layout: default
title: Supply Chain Assurance
nav_order: 15
parent: Concepts
---

# Supply Chain Assurance

What a deployer can check about the software they run: what is in it, where it came from and that it has not been
altered.

## Software bill of materials

The `supply-chain` workflow runs on every push to `master`, on every release tag and on request. It publishes two
CycloneDX
JSON documents as the `sbom` artifact:

| File                 | Contents                                                                                                           |
|----------------------|--------------------------------------------------------------------------------------------------------------------|
| jube-dotnet.cdx.json | Every NuGet package of the solution, with versions, licences and package URLs, development-only packages excluded. |
| jube-vue.cdx.json    | Every npm package that ships in the web front end, development dependencies excluded.                              |

Feed them to any SBOM consumer (Dependency-Track, Grype, Trivy, a procurement questionnaire) to answer "does this
contain
component X". The older hand-maintained [Package Dependency Appendix](../../PackageDependencyAppendix.html) remains for
licence review, but where it and the SBOM disagree, the SBOM is right.

## Signed releases

A release tag (`v*`) additionally builds the images (`jube.app`, `jube.monitoring`, `jube.opentelemetrylistener` and
`jube.r`),
saves each as a compressed tarball, adds the SBOMs and a `SHA256SUMS` file, records a build provenance attestation for
every file (GitHub's attestation, which names the repository, the workflow, the commit and the runner) and signs every
file with cosign
using the workflow's own identity, with no long-lived key to guard. Each file has a `.sigstore.json` bundle beside it.

Verification instructions are in `SECURITY.md` at the root of the repository.

A release run ends by creating a **draft** GitHub release holding the signed tarballs, the SBOMs, `SHA256SUMS`, the
cosign bundles and the provenance, for a person to review and publish. Nothing is pushed to any registry: the project
builds from master, and pushing signed images to a public registry (and verifying them with `cosign verify` against an
image reference) is deliberately not done.
<!-- TODO(docs): the jube.etcd, jube.patroni and jube-loadtest images are scanned daily but not part of the signed release; decide whether the cluster images belong in it. -->
<!-- TODO(docs): the release workflow has not run yet; the first tag will show whether CycloneDX and cyclonedx-npm accept the solution layout and whether the 90 minute limit is enough for four image builds. -->

## Vulnerability scanning

| Check             | What it covers                                                                                               | When                                     |
|-------------------|--------------------------------------------------------------------------------------------------------------|------------------------------------------|
| CodeQL            | C# and JavaScript/TypeScript source                                                                          | Every push and pull request, weekly      |
| Trivy             | The seven container images (app, monitoring, OpenTelemetry listener, etcd, Patroni, R sandbox and load test) | Daily; a fixable critical fails it       |
| OWASP ZAP         | A running stack through the API and the web application                                                      | Daily when the code or the scan is stale |
| Dependency review | See the SBOM above                                                                                           | On demand                                |

## Pinned base images

The third party images the compose files start (the pgvector Postgres image and the Garnet cache) are pinned by digest,
so a compose file deploys exactly the image that was tested, not whatever the tag points at today. To take an update,
pull
the new image, read its release notes, and change the digest in `docker-compose.yml`, `docker-compose.redis.yml` and
`Jube.Cluster/docker-compose.yml` together.

<!-- TODO(docs): the images the Dockerfiles start FROM (the .NET runtime and SDK, R and the others) are referenced by tag, not digest; Trivy scans the result daily, which is the control today. -->
<!-- TODO(docs): Dependency review is listed as on demand; add the GitHub dependency review action to pull requests if it is wanted. -->
