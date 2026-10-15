# Jube.R

A standalone, sandboxed R analytics server. Plumber (R's own HTTP framework) runs R in-process and exposes it over HTTP
directly — there is no bridging layer, no embedded-interpreter PInvoke, nothing else in front of it. A caller (the
analytics orchestrator, once that side is wired up in the parent branch after this merges)
sends R source as JSON over HTTP and gets back a JSON result: console output, an error if there was one, and
timing/version metadata — never a raw console transcript.

Every package this repo's methodology guide exercises (see `docs/AnalyticalMethodology/`) is baked in — see
`packages.R`, the single source of truth for what `library(<name>)` can reach inside a script here.

## Build

```bash
docker build -t jube-r .
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

| Method & path    | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
|------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `GET /health`    | Liveness, R/plumber version, uptime, and the *real, current* script rules (size limit, nesting-depth limit, every forbidden function name) — read straight from `R/sandbox.R`, not separate documentation that can drift.                                                                                                                                                                                                                                                                     |
| `GET /packages`  | Every package installed in the image, so a caller can check what's safe to `library()` before writing a script around it, rather than discovering a missing package at execution time.                                                                                                                                                                                                                                                                                                        |
| `POST /validate` | `{"script": "..."}` → checks shape, syntax and disallowed function references **without running it**, plus non-blocking `lintr` feedback. Cheap, meant to be called often — an LLM writing R against this service should validate in a loop before ever calling `/execute`, the same way it would read a compiler error and retry.                                                                                                                                                            |
| `POST /execute`  | `{"script": "...", "timeoutSeconds": 30, "userName": "alice", "dataframes": [{"name": "transactions", "columns": [{"Name": "Amount", "Type": "double"}], "data": [{"Amount": 100}]}], "variables": [{"name": "threshold", "value": 100}]}` → validates first (a script that fails validation is never evaluated, reported the same way as any other outcome: `status: "rejected"`), builds every `dataframes` entry into a data.frame and binds every `variables` entry into the sandbox environment (see "Dataframe and variable binding" below), then runs the script in a fresh, throwaway environment with a wall-clock/CPU timeout, and returns `{status, consoleOutput, error, warnings, durationMs, timedOut, rVersion, timestamp}`. `timeoutSeconds`, `userName`, `dataframes` and `variables` are all optional (default `JUBE_R_SCRIPT_TIMEOUT_SECONDS`, no binding at all). `userName` is carried only so this server's own logging can show which user triggered a run -- it plays no part in deciding what the script can do. |

## Environment variables

| Variable                            | Default   | Meaning                                                                                                           |
|-------------------------------------|-----------|-------------------------------------------------------------------------------------------------------------------|
| `JUBE_R_HOST`                       | `0.0.0.0` | Interface the router binds to.                                                                                    |
| `JUBE_R_PORT`                       | `8080`    | Port the router binds to.                                                                                         |
| `JUBE_R_SCRIPT_TIMEOUT_SECONDS`     | `30`      | Default `/execute` timeout when the caller doesn't specify one.                                                   |
| `JUBE_R_SCRIPT_TIMEOUT_SECONDS_MAX` | `300`     | Hard cap — a caller-supplied `timeoutSeconds` above this is silently clamped down to it, never rejected outright. |

## Script safety model

Layered, deliberately not relying on any single mechanism — see the docstring at the top of `R/sandbox.R` for the full
reasoning, in particular *why* the forbidden-function list blocks indirection primitives (`get`, `do.call`, `eval`,
`assign`, ...) rather than only the dangerous functions they could be used to reach, and why it's a static walk of the
parsed expression tree rather than runtime function-rebinding (trivially bypassed by `base::system(...)`).

1. **Shape check on the raw text**, before parsing: a length limit and a nesting-depth limit, mirroring the fix this
   repo's own rule compiler needed for the same failure class (a pathological input reaching the parser/compiler and
   overflowing the process's C stack, which cannot be caught in either R or .NET and kills the whole process, not just
   the one call).
2. **A static walk of the parsed expression tree**, before evaluation: rejects any reference to a disallowed function or
   primitive — as a call, as a bare value (`f <- system`), or through `::`/`:::` qualification. Never runs a script this
   rejects.
3. **A wall-clock and CPU timeout** (`setTimeLimit()`) around evaluation.
4. **A fresh, empty environment per call**, discarded after. `<<-` is blocked specifically because it would otherwise be
   able to write into the shared `globalenv()` every call's environment is parented on (needed so a script can see
   loaded packages), leaking state from one request into every request after it in the same long-running process.

This is regression-tested by a hostile-script corpus (attack seeds × wrapping shapes — direct call, aliased, buried in
other code, qualified, reached through `get()`/`do.call()`) in
`tests/testthat/test-forbidden-corpus.R`, the same pattern this repo already uses for its rule-compiler security corpus:
a flat list of attack seeds crossed with shape variants, asserting every combination is rejected, run as fast in-process
calls with no HTTP and no container.

**What this does not cover:** a script that never calls a forbidden function can still exhaust memory or CPU with
entirely legal R (`rep(0, 1e12)`, a tight non-recursive loop). The timeout bounds CPU/wall time; the container resource
limits below are what bound memory. This file's checks are the R-level half of the boundary, not the whole of it — see
the next section for the other half.

## Dataframe and variable binding

`/execute`'s `dataframes` array (`{name, columns, data}`) and `variables` array (`{name, value}`) let a caller get
real data and plain values into a script without this server ever reaching a database itself:

- A `dataframes` entry's pointer is resolved, and tenant-checked, entirely on the .NET side
  (`ScriptExecutionService.ResolveDataframesAsync`) before `/execute` is ever called — the same tenant-scoped,
  non-distinguishing `AskJoobyDataCacheClient` lookup every other AskJoobyDataCache pointer consumer already uses.
  By the time a request reaches this server, `columns`/`data` are already-authorised table data, in the same shape
  `"AskJoobyDataCache"`'s own `"ColumnsJson"`/`"DataJson"` columns store it — this server never sees, and never
  needs to check, who the data belongs to. `R/data_cache.R` holds the one remaining piece of logic this needs: a
  pure, no-I/O JSON-to-`data.frame` mapping (column names picked from `columns` by name, values from `data` the
  same way, never by position or JSON key order — see `jsonb-reorders-keys`), fed straight from the parsed request
  body. Building two or more dataframes in one call is all-or-nothing — if any one entry's columns/data are
  malformed, nothing runs, with one generic `"could not build dataframe: <name>"` error.
- A `variables` entry's R type is inferred from its JSON value's own shape (number → numeric, string → character,
  true/false → logical, array → vector) rather than a declared type field. A JSON `null` is refused outright at the
  .NET facade and never reaches here — there is no single R type a null could mean.
- `userName` is logged (see `/execute`'s own handler in `plumber.R`) purely so a request can be attributed to the
  user who triggered it; the script itself never sees it and it plays no part in deciding what a script can do.

## Unauthenticated by design, and container hardening

This layer does no auth. The boundary is the deployment network (this is meant to sit behind an internal-only service
mesh, reachable only from the orchestrator that calls it) and the container itself — not an API key this file would just
be checking against its own environment anyway.

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
docker build -t jube-r .
docker run --rm \
  --entrypoint /usr/local/bin/Rscript \
  -v "$(pwd)/tests:/app/tests:ro" \
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
  status codes, JSON shape) rather than re-testing the security logic already covered above.

## Adding a package

Add it to `packages.R`, rebuild. That file is the single source of truth for what this image can do; do not
`install.packages()` inside a running container — there is no package manager to do it with, and no
`/opt/r-library` write access for `rrunner` outside a build (installing into a read-only, non-writable runtime image
would fail even if the package manager were still there).
