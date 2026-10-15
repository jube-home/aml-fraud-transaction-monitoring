---
layout: default
title: Run Script Tool
nav_order: 7
parent: Ask Jooby
---

# RunScriptTool: the AskJooby side of the R sandbox

`RunScriptTool` (`Jube.AskJooby/Tools/ScriptExecution/RunScriptTool.cs`) is the AskJooby-facing wrapper around the
sandboxed R execution server documented in full at `docs/AnalyticalMethodology/RServer/index.md` -- that page covers the server itself in
depth (the sandbox model, timeouts, concurrency, the unauthenticated-by-design boundary); this page covers the
`.NET` tool surface a model actually calls, which that page does not mention by name at all. Nothing below repeats
the sandbox internals; it only covers what is specific to calling that server as an AskJooby tool: four methods, and
the pointer/variable binding convention that gets real data into a script.

## The four methods

* **`CheckRServerHealth()`** -- liveness, R/Plumber version, uptime, and the *real, current* script-safety rules
  (size limit, nesting-depth limit, forbidden function names), read straight from the server's own `/health`
  endpoint rather than from separate documentation that can drift out of date. The tool's description frames this
  as the thing to call first: confirm the server is reachable and learn its actual current limits before writing a
  script against it.
* **`ListRPackages()`** -- every package already installed on the server, so a script can be written around what
  is genuinely available (`library(<name>)`) rather than discovering a missing package only once the script
  actually runs.
* **`ValidateRScript(script)`** -- checks shape, syntax, and use of disallowed functions, plus non-blocking style
  feedback, without running anything. The tool's description calls this "cheap and meant to be called repeatedly in
  a validate-then-fix loop, the same way a compiler error would be read and corrected" -- `RunScript`'s own
  description goes further and says a script that fails validation is "never worth running," i.e. `ValidateRScript`
  is expected to be called first as a matter of course, not only when something has already gone wrong.
* **`RunScript(script, timeoutSeconds = null, dataframes = null, variables = null)`** -- runs the script for real
  and blocks until the result is back: "nothing is queued and nothing is taken as done until this call returns."
  `timeoutSeconds` (1-300) is passed straight through to the server's own wall-clock/CPU limit; left unset, the
  server's own default applies. The result reports whether it succeeded, the full console output, any warnings, how
  long it took, the R version it ran on, a structured result if the script printed one between the
  `<<<JUBE_RESULT_START>>>`/`<<<JUBE_RESULT_END>>>` markers (see `docs/AnalyticalMethodology/RServer/index.md`'s own section on this
  convention), and, on failure, the error and whether it was specifically a timeout. The tool's description
  explicitly steers an agent away from using it to train, fit or search for a classifier, regressor, clusterer or
  anomaly model -- that is what Advanced Analytics and Exhaustive are for, with their own instance, promotion
  evidence and (for the open-ended search graph) an Exhaustive comparison that fitting a model here would skip
  entirely. `RunScript` stays for exploring, shaping and summarising data the platform has no dedicated tool for,
  not for producing the model itself.

## Getting real data into a script: pointers, not pasted rows

A script cannot see Jube's data unless the caller puts it there explicitly, and `RunScript` offers two different
ways to do that, deliberately distinguished by how expensive the value is:

* **`dataframes`** (`IReadOnlyList<RScriptDataframeRequest>`, up to 10 per call) -- each entry names a
  `data_pointer` (from `CacheAssembledTable`, `RunSqlQuery`, or any other tool that caches a table -- see
  `docs/AskJooby/DataPointerCache.md`) and the name it should be hydrated under; the server turns each one into a
  same-named `data.frame` inside the script's own environment before the script runs, automatically scoped to the
  caller's own tenant -- "you cannot choose whose data it reads." If *any* pointer in the list cannot be resolved,
  nothing in the script runs at all, and the caller gets back a single generic error that deliberately does not say
  whether the pointer was wrong, stale, or simply never theirs -- the ambiguity is intentional, so that a caller
  probing with someone else's pointer learns nothing about whether it exists.
* **`variables`** (`IReadOnlyList<RScriptVariableRequest>`, up to 50 per call) -- plain scalar values (a threshold,
  a label, a short list) that are not worth minting a cache pointer for, bound as ordinary R variables instead. The
  tool's description is explicit that a null value should be omitted from this list entirely rather than sent as
  `null`.

This mirrors the same pointer-over-pasted-data discipline the rest of the tool surface follows: a script that needs
a whole table gets a pointer it hydrates itself, server-side, rather than the model pasting rows into the script
text (which would cost tokens to write out and risk the model inventing values along the way); a script that needs
one number gets that number directly, because minting and resolving a pointer for a single scalar would cost more
than it saves.

## Where this tool is wired

`RunScriptTool` is added inside `BasicResearchTools` (`Jube.AskJooby/Agents/Basic/AgentDefinitions.cs`) whenever
`args.ScriptExecution` is non-null -- i.e. whenever the deployment has the R server configured at all -- which
means it reaches essentially every ordinary agent, the same way the SQL/cache tool family does. `RunScript`'s
`script` argument is declared `AlreadyValidatedElsewhere` in `AgentGuardToolProvenance`
(`Jube.AskJooby/Security/AgentGuardToolProvenance.cs`), exempting it from the generic tool-call-guard's
code-injection scan for every agent that holds the tool -- the R sandbox's own layered, statically-verified safety
model already guards it, and the generic cross-language regex cannot tell R syntax from JavaScript. See
`docs/AskJooby/AgentGuard.md`.

## Cross-reference

* `docs/AnalyticalMethodology/RServer/index.md` -- the R server itself: the sandbox model, the four HTTP endpoints, timeouts and
  concurrency, and the `<<<JUBE_RESULT_START>>>` structured-result convention this tool's `RunScript` surfaces.
* `docs/AskJooby/DataPointerCache.md` -- what a `data_pointer` is, and every other tool that produces or consumes
  one; `dataframes` is one more consumer of the same mechanic.
* `docs/AskJooby/AgentGuard.md` -- how a live `RunScript` binding changes the strictness of the tool-call guard.
