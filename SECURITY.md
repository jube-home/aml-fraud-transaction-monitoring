# Security policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately and do not open a public issue.

Use GitHub's private vulnerability reporting for this repository: open the **Security** tab, choose **Report a
vulnerability** and describe the problem, the version or commit you tested and, if you can, the steps to reproduce it.

We will acknowledge a report within five working days, tell you whether we consider it a vulnerability, and keep you
informed until it is fixed. Please give us a reasonable time to release a fix before disclosing details publicly; we
will credit you in the release notes unless you prefer otherwise.

<!-- TODO: confirm the reporting channel and the acknowledgement time, and add a security contact address if one is wanted in addition to GitHub private reporting. -->

## What is in scope

The Jube application and its services in this repository: the web application and API, the engine, the case management
and sanctions components, AskJooby, the cluster deployment files and the container images built from them.

Out of scope are findings that need a privileged position the product already grants (an administrator misusing their
permissions), denial of service by sheer volume, missing hardening headers on a page that holds no data, and issues in
third party software that is not shipped here.

## Supported versions

Security fixes are made on the `master` branch and released from it. Run the most recent release.

## How the project is checked

- Every push and pull request is analysed with CodeQL for C# and for JavaScript and TypeScript.
- Container images are scanned daily with Trivy, and a fixable critical vulnerability fails the build.
- A dynamic scan with OWASP ZAP runs against a running stack.
- A suite of penetration tests (`Category=PenTest`) runs in continuous integration.
- Every build publishes CycloneDX software bills of materials, and release images are accompanied by build provenance
  attestations and cosign signatures (see the `supply-chain` workflow).

## Verifying a release

Each release publishes image tarballs, their SBOMs, a `SHA256SUMS` file and, for each file, a `.sigstore.json` bundle.
Verify a file with cosign, naming the workflow identity that signed it:

```
cosign verify-blob \
  --bundle jube.app-vX.Y.Z.tar.gz.sigstore.json \
  --certificate-identity-regexp 'https://github.com/.+/.github/workflows/supply-chain.yml@refs/tags/v.+' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com \
  jube.app-vX.Y.Z.tar.gz
```

<!-- TODO: publishing images to a public registry is undecided; until then releases carry signed tarballs only. -->
