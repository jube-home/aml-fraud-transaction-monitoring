---
layout: default
title: R Server
nav_order: 4
parent: Analytical Methodology
---

A standalone, sandboxed R analytics server (`Jube.R` in the repository root). Plumber (R's own HTTP framework) runs R
in-process and exposes it over HTTP directly — there is no bridging layer, no embedded-interpreter PInvoke, nothing else
in front of it. A caller sends R source as JSON over HTTP and gets back a JSON result: console output, an error if there
was one, and timing/version metadata — never a raw console transcript.

Every package the [Analytical Methodology](../index.md) guide exercises is baked in — see
`Jube.R/packages.R`, the single source of truth for what `library(<name>)` can reach inside a script here.

## Build

```bash
docker build -t jube-r Jube.R
```

Two stages: `build` has a full compiler toolchain and installs every package in `packages.R` into
`/opt/r-library`; `runtime` starts from a clean base, copies in only that compiled library and the router, and then
strips itself down (see the Dockerfile's own comments for exactly what and why) before the image is done.

## Run

```bash
docker run --rm -p 8080:8080 jube-r
```

`ENTRYPOINT` is exec-form `Rscript /app/entrypoint.R` — the container's only process is the plumber router, started
directly by the container runtime rather than through a shell. `entrypoint.R` reads its host/port from `JUBE_R_HOST`/
`JUBE_R_PORT` (default `0.0.0.0:8080`).

## Endpoints

Every response is JSON. None of this layer does authentication or sessions — see "Unauthenticated by design"
below for why, and what actually has to enforce access instead.

| Method & path    | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
|------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `GET /health`    | Liveness, R/plumber version, uptime, and the *real, current* script rules (size limit, nesting-depth limit, every forbidden function name) — read straight from `R/sandbox.R`, not separate documentation that can drift.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| `GET /packages`  | Every package installed in the image, so a caller can check what's safe to `library()` before writing a script around it, rather than discovering a missing package at execution time.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| `POST /validate` | `{"script": "..."}` → checks shape, syntax and disallowed function references **without running it**, plus non-blocking `lintr` feedback. Cheap, meant to be called often — an LLM writing R against this service should validate in a loop before ever calling `/execute`, the same way it would read a compiler error and retry.                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `POST /execute`  | `{"script": "...", "timeoutSeconds": 30}` → validates first (a script that fails validation is never evaluated, reported the same way as any other outcome: `status: "rejected"`), then runs it on a background daemon (see "Background execution and concurrency" below) in a fresh, throwaway environment with a wall-clock/CPU timeout, and returns `{status, consoleOutput, error, warnings, durationMs, timedOut, rVersion, timestamp}`. `timeoutSeconds` is optional (default `JUBE_R_SCRIPT_TIMEOUT_SECONDS`) and capped at `JUBE_R_SCRIPT_TIMEOUT_SECONDS_MAX`. The call itself is still a single, ordinary blocking HTTP request from the caller's point of view — only the main Plumber process's own internal handling of it is asynchronous. |

The script a caller submits is genuinely arbitrary R source, not a fixed menu of named operations — the sandbox below is
a narrow blocklist of specific dangerous primitives, not an allowlist of "safe" functions. Every installed package's
full surface stays available, so a caller can write real, open-ended analysis rather than being limited to whatever a
fixed tool catalogue anticipated in advance.

## Environment variables

| Variable                            | Default                   | Meaning                                                                                                                                                                               |
|-------------------------------------|---------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `JUBE_R_HOST`                       | `0.0.0.0`                 | Interface the router binds to.                                                                                                                                                        |
| `JUBE_R_PORT`                       | `8080`                    | Port the router binds to.                                                                                                                                                             |
| `JUBE_R_SCRIPT_TIMEOUT_SECONDS`     | `30`                      | Default `/execute` timeout when the caller doesn't specify one.                                                                                                                       |
| `JUBE_R_SCRIPT_TIMEOUT_SECONDS_MAX` | `300`                     | Hard cap — a caller-supplied `timeoutSeconds` above this is silently clamped down to it, never rejected outright.                                                                     |
| `JUBE_R_DAEMON_COUNT`               | `2`                       | Background mirai daemons a script actually runs on (see "Background execution and concurrency" below). Matches this image's own documented `--cpus 2` deployment flag by default.     |
| `JUBE_R_MIRAI_GRACE_MS`             | `5000`                    | Added, in milliseconds, to a call's own `timeoutSeconds` to get the hard backstop deadline mirai kills the task at if the script's own `setTimeLimit()` timeout somehow doesn't fire. |
| `JUBE_R_MAX_INFLIGHT_EXECUTE`       | `JUBE_R_DAEMON_COUNT * 3` | How many `/execute` calls may be running or queued on the daemon pool at once before a new one is rejected outright with `503`.                                                       |
| `JUBE_R_MAX_REQUEST_BODY_BYTES`     | `52428800` (50MB)         | Ceiling on a request body's raw size, checked before it is parsed as JSON.                                                                                                            |

## Background execution and concurrency

Plumber itself is a single R process on a single OS thread. Before this, `eval()` for one script blocked that process
from answering *any* other request — including `/health` — for as long as the script ran, up to
`JUBE_R_SCRIPT_TIMEOUT_SECONDS_MAX`. `/execute` now hands the actual run off to a pool of `JUBE_R_DAEMON_COUNT`
background [`mirai`](https://mirai.r-lib.org/) daemons — separate R processes started once at startup — and returns a
`promises` promise from the route handler instead of a plain value; Plumber resolves that asynchronously once the daemon
replies, so the main process stays free to answer `/health`, `/validate` and other `/execute`
calls while one script is still running.

Daemons start as brand-new R sessions with nothing sourced into them. `mirai::everywhere()` runs the same
`source("R/sandbox.R")` / `source("R/data_cache.R")` calls that the main process ran, on every daemon, so
`run_sandboxed()` and the validation/forbidden-symbol/shape-check machinery it depends on exist there too — the
sandboxing rules in the section below apply inside a daemon exactly as they do in the main process, not only on the way
in. A script is additionally validated once synchronously in the main process, before it is allowed to occupy a daemon
or a place in mirai's queue at all, so a hostile script costs no more than a plain `/validate`
call and never eats into the concurrency budget below; `run_sandboxed()` repeats the same check again once it reaches a
daemon regardless, so a direct call to this server that somehow bypassed the first check is still covered.

Two independent timeouts bound a single `/execute` call, deliberately staggered so the more graceful one always gets the
first chance to fire:

1. The `setTimeLimit()` wall-clock/CPU timeout described in the next section, at exactly the call's own (clamped)
   `timeoutSeconds` — the normal path, returning a clean `{"status": "error", "timedOut": true, ...}`.
2. A mirai task-level `.timeout`, at `timeoutSeconds * 1000 + JUBE_R_MIRAI_GRACE_MS` milliseconds — a hard backstop from
   *outside* R entirely, for the case `setTimeLimit()` doesn't fire (e.g. a script stuck in uninterruptible native
   code). This still reports back as `{"status": "error", "timedOut": true, ...}`, with the error message noting it was
   the backstop rather than the script's own timeout — the wire contract a caller sees is identical either way.

On the .NET side, `RServerClient`'s own `HttpClient` timeout (`RServerHttpTimeout`, default 330 seconds) stays
comfortably above both of these and above `ScriptExecutionService`'s own per-call cancellation guard (up to 310
seconds), so none of them ever lose a race against this one and turn a clean, explained result into an ordinary,
unexplained HTTP timeout instead.

A small, fixed daemon pool means an unbounded flood of concurrent `/execute` calls would otherwise queue inside mirai's
own dispatcher without limit, growing memory and making every caller wait longer rather than any one of them failing
fast. Once `JUBE_R_MAX_INFLIGHT_EXECUTE` calls are already running or queued, a new one is rejected immediately with
`503` and a `Retry-After` header, without ever touching the daemon pool.

## Script safety model

Layered, deliberately not relying on any single mechanism — see the docstring at the top of `R/sandbox.R` for the full
reasoning, in particular *why* the forbidden-function list blocks indirection primitives (`get`,
`do.call`, `eval`, `assign`, ...) rather than only the dangerous functions they could be used to reach, and why it's a
static walk of the parsed expression tree rather than runtime function-rebinding (trivially bypassed by
`base::system(...)`).

1. **Shape check on the raw text**, before parsing: a length limit and a nesting-depth limit, mirroring the guard the
   repo's own rule compiler applies to the same failure class (a pathological input reaching the parser/compiler and
   overflowing the process's C stack, which cannot be caught in either R or .NET and kills the whole process, not just
   the one call).
2. **A static walk of the parsed expression tree**, before evaluation: rejects any reference to a disallowed function or
   primitive — as a call, as a bare value (`f <- system`), or through `::`/`:::` qualification. Never runs a script this
   rejects.
3. **A wall-clock and CPU timeout** (`setTimeLimit()`) around evaluation, backed by the mirai task-level hard backstop
   described above for the case this doesn't fire.
4. **A fresh, empty environment per call**, discarded after. `<<-` is blocked specifically because it would otherwise be
   able to write into the shared `globalenv()` every call's environment is parented on (needed so a script can see
   loaded packages), leaking state from one request into every request after it in the same long-running process.

Ahead of all of this, a `requestSize` `@filter` and a check inside `parse_json_body()` reject a request whose body is
too large before it is parsed at all (`JUBE_R_MAX_REQUEST_BODY_BYTES`), and both `/validate` and
`/execute` reject a JSON body containing any field they don't recognise (e.g. a stray extra key), rather than silently
ignoring it. All of this — the forbidden-symbol list, the shape check, these ingress checks — is re-enforced here
deliberately, duplicating `ScriptExecutionService`'s own equivalent pre-flight checks on the .NET side
(`ValidateRunRequest`): this server is unauthenticated by design (see below), so a caller that reaches it directly,
bypassing the .NET layer entirely, still cannot run or request anything the .NET layer would have refused.

This is regression-tested by a hostile-script corpus (attack seeds × wrapping shapes — direct call, aliased, buried in
other code, qualified, reached through `get()`/`do.call()`) in `tests/testthat/test-forbidden-corpus.R`, the same
pattern this repo already uses for its rule-compiler security corpus: a flat list of attack seeds crossed with shape
variants, asserting every combination is rejected, run as fast in-process calls with no HTTP and no container.

**What this does not cover:** a script that never calls a forbidden function can still exhaust memory or CPU with
entirely legal R (`rep(0, 1e12)`, a tight non-recursive loop). The timeout bounds CPU/wall time; the container resource
limits below are what bound memory. This is the R-level half of the boundary, not the whole of it — see the next section
for the other half.

## Unauthenticated by design, and container hardening

This layer does no auth. The boundary is the deployment network (this is meant to sit behind an internal-only service
mesh, reachable only from the orchestrator that calls it) and the container itself — not an API key this layer would
just be checking against its own environment anyway.

**bash is present in the image, and that's not optional.** `/usr/local/bin/R` — the binary `Rscript` itself execs to do
any work at all, on every single invocation — is a bash script in the base image (`#!/bin/bash`). Stripping every shell
out of the runtime image was tried first; it made the image unable to run any R at all, not just unable to give an
interactive shell. There is no way to keep bash executable for R's own use while making it unreachable by anyone else —
an interactive `docker exec <container> bash` runs as the same OS user (`rrunner`, non-root) as `Rscript` itself, so the
same permission that lets one run lets the other.

Given that, baked into the image itself:

- No package manager (`apt`/`dpkg` and their lists are removed from the runtime stage) — nothing in the running
  container can install anything, even as root.
- No compiler toolchain in the runtime image — only the build stage has one; it never ships.
- Runs as a dedicated, non-root, non-login system user (`rrunner`, shell `/usr/sbin/nologin`) — not root, and `su`/
  `login` can't start an interactive session for it even if something tried.
- Every setuid/setgid bit shipped in the base image is stripped in the runtime stage.
- The application code itself (`/app`) is owned by root — `rrunner` can read and execute it, not modify it.

Not bakeable into an image — these are `docker run`/Swarm deploy flags, and matter just as much:

```bash
docker run --rm -p 8080:8080 \
  --read-only \
  --tmpfs /tmp \
  --cap-drop ALL \
  --security-opt no-new-privileges \
  --pids-limit 256 \
  --memory 2g --cpus 2 \
  jube-r
```

No `--network none` here — unlike the earlier single-script-per-container design this grew out of, the router itself
needs to accept inbound connections. Network isolation is the deployment network's job (an internal-only overlay, no
route to anything but the orchestrator that calls this), not a per-container flag.

## Tests

`tests/` isn't copied into the built image (test code has no reason to ship in the production artifact); the image
already has every test dependency (`testthat`, `httr`, `processx`) from `packages.R` though, so mount the test directory
in and override the entrypoint to run the suite instead of the router:

```bash
docker build -t jube-r Jube.R
docker run --rm \
  --entrypoint /usr/local/bin/Rscript \
  -v "$(pwd)/Jube.R/tests:/app/tests:ro" \
  jube-r tests/testthat.R
```

Two kinds of coverage, deliberately kept separate:

- **Fast unit/corpus tests** (`test-shape-guard.R`, `test-forbidden-corpus.R`, `test-execution.R`) call
  `R/sandbox.R`'s functions directly — no HTTP, no spawned server. This is where the actual security regression coverage
  lives, including a check that the corpus itself references every entry in
  `FORBIDDEN_SYMBOLS` at least once, so a symbol added to the blocklist with no seed exercising it can't silently go
  untested.
- **A thin HTTP integration test** (`test-plumber-integration.R`) spawns a real instance of this server via
  `processx` and hits `/health`, `/packages`, `/validate` and `/execute` over real HTTP, checking the wiring (routes,
  status codes, JSON shape) rather than re-testing the security logic already covered above. This is also where the
  background-daemon offload itself is exercised end to end: one test fires a slow script at `/execute` from a second,
  independent process and confirms `/health` still answers promptly while it is still running, and others confirm the
  request-size and unknown-field ingress checks reject before anything reaches a daemon.

## Adding a package

Add it to `packages.R`, rebuild. That file is the single source of truth for what this image can do; do not
`install.packages()` inside a running container — there is no package manager to do it with, and no
`/opt/r-library` write access for `rrunner` outside a build (installing into a read-only, non-writable runtime image
would fail even if the package manager were still there).

## Scope

This server's own HTTP contract is deliberately synchronous and stateless: one HTTP request, one script, one JSON
response, no job IDs, no polling, no blob storage — the background daemon offload described above changes how the main
process handles a request internally, not this contract. Job orchestration, state tracking and the analytics
orchestrator that calls this service are handled in Jube.App, wired up separately once this branch merges — not part of
this server.

## Extracting a structured result from `consoleOutput` (Jube.App convention)

This server's own `/execute` response has no typed result field, only `consoleOutput` — deliberately, since it does not
know or care what any given script is computing. The orchestrator in Jube.App (`RScriptResultParser`, next to the job
worker that calls this server) layers a convention on top of that plain string, entirely on its own side: a script that
wants to hand back a structured result prints it between two exact literal markers,
`<<<JUBE_RESULT_START>>>` and `<<<JUBE_RESULT_END>>>`, each on its own `cat()`/`print()` call. A script that follows
this convention should build the value as a list and serialise it with `jsonlite::toJSON(list(...), auto_unbox = TRUE)`
immediately before printing it between the markers — `jsonlite` is already installed (see `Jube.R/packages.R`) and is
not on the forbidden-symbol list in `Jube.R/R/sandbox.R`, so no script needs anything beyond what is already available.

If both markers are present and the text between them parses as JSON, the orchestrator stores it verbatim as the run's
result, alongside the full `consoleOutput` it was extracted from. If the markers are absent, there is no result to
extract and none is reported — this is not an error. If the markers are present but the text between them fails to parse
as JSON, no result is stored, the run itself is not failed over it (the script ran fine; only the convention was
misused), and a warning is attached to the run recording that the markers were found but their content was discarded.
This convention is specific to Jube.App's own R script job orchestration, not a rule this server enforces or is aware
of.
