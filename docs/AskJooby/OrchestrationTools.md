---
layout: default
title: Orchestration Tools
nav_order: 2
parent: Ask Jooby
---

# Orchestration tools: planning, delegation, hand-back, clarifying questions, and workflows

Six tools make up the control surface AskJooby uses to run a turn across more than one agent: writing a plan before
delegating, delegating one or many sub-tasks to specialist Joobriks, handing a sub-task back to whoever sent it,
stopping a turn outright to ask the caller a clarifying question, and following (and recording progress against) a
graph-directed workflow.

## `WritePlan`: required before the first delegation

`PlanTool.WritePlan` (`Jube.AskJooby/Tools/Delegation/PlanTool.cs`) writes the plan for the current turn, before
anything is handed over: each step's description, which specialist it goes to (or none, if the calling agent will
answer it itself), which stable (domain) owns that specialist, and a concrete check for knowing the step worked --
the tool's description insists this must be "a concrete result you can observe, not 'it is done'." A turn's plan is
written once, at the start; a step that turns out wrong is handled by a later step of the same plan rather than by
calling `WritePlan` again.

This is a hard gate, not just a prompt convention. `AgentFactory.cs` adds a `PlanTool` for an agent only when
delegation is actually possible for it, and which plan the tool records against depends on depth: at depth 0
(Jooby itself, the root orchestrator) it records against `context.TurnActivity`, the whole turn's plan; at depth 1
or deeper (a stable supervisor delegating within its own stable) it records a *stable-scoped* plan instead, via the
`ownerStable` constructor argument, and `WritePlan` refuses any step there whose specialist does not belong to that
same stable -- "hand that step back to Jooby rather than planning it here." `SpawnAgent`/`SpawnAgents` both check
for an existing plan before running (see below) and refuse to delegate until one exists, so a plan genuinely has to
be written, in code, not merely requested in a system prompt.

`WritePlan` validates its own input before recording anything: between 1 and 12 steps (`MaxSteps`), every step
needs both a description and a check, and a step's named `Specialist` is resolved against
`AgentDefinitions.HierarchyDomainOf` -- if the specialist's real owning stable disagrees with a `Stable` the caller
also supplied, the call is rejected outright rather than silently preferring one or the other.

## `SpawnAgent`/`SpawnAgents`: delegating to a Joobrik

`SpawnAgentTool` (`Jube.AskJooby/Tools/Delegation/SpawnAgentTool.cs`) delegates a self-contained sub-task to a
specialist agent (a "Joobrik") and returns only that Joobrik's finished report -- its own tool calls and reasoning
never reach the caller, only the final answer. `SpawnAgent` takes one `agentType`/`task` pair; `SpawnAgents` takes
two or more independent sub-tasks and runs them concurrently (`Task.WhenAll`), returning every report together,
labelled by which sub-task produced it. `SpawnAgents` exists specifically for sub-tasks that do not depend on each
other or on anything only the caller knows beyond what it put in each task's own self-contained description --
where one step depends on another's answer, `SpawnAgent` is used in sequence instead.

Several refusals are enforced in code before a Joobrik is ever actually spawned, each returning a specific
instruction back to the model rather than a bare error:

* **Token budget exhaustion.** `childContext.TokenBudget?.IsExhausted` stops further spawning outright and tells
  the model to take stock, report what it has established, name what it was about to delegate next, and ask
  whether it is on the right lines -- rather than silently truncating the turn.
* **Delegation not allowed.** `RefuseDelegateNotAllowed` checks the spawning agent's own `AllowedDelegates` list
  (from `AgentDefinitions.All`); an `agentType` not on that list, or the current caller not being permitted to use
  it, is refused by name.
* **No plan yet.** `RefuseWithoutPlan` -- at depth 0, a single `SpawnAgent` call is allowed without a plan (only
  `SpawnAgents`, i.e. multi-way fan-out, strictly requires one at the root), but every unplanned handover is still
  counted (`NoteUnplannedHandover`) in `UnplannedHandovers`.
* **Fan-out without a stable-scoped plan.** `RefuseFanOutWithoutStablePlan` -- a stable supervisor (depth 1) sending
  several Joobriks of its own stable at once must first call `WritePlan` for that stable specifically.
* **A plan exists but omits this stable.** `RefuseUnplannedStable` -- at depth 0, once a plan exists, a handover to
  a stable the plan never named is refused; the plan has to be extended with a step for that stable, or the agent
  has to answer that part itself.

Each spawned run is watched by an idle-timeout watchdog (`AgentIdleWatchdog`) and, when `context.TokenBudget` is in
play, cut off if the turn's overall token budget is overrun mid-run (`Guarded`) -- in either case the Joobrik is
"called in" or "sent back to the woodlands for a lie down," and whatever partial report it had written by then is
returned to the caller labelled as likely incomplete, rather than the caller waiting indefinitely or getting
nothing at all. When a spawned Joobrik calls `HandBack` instead of answering (see below), `SpawnAgent`/`SpawnAgents`
turn that into a specific instruction back to the delegating agent: act on it now, by routing to the named owner if
it is one of the delegating agent's own specialists, or by handing back further if not -- "ending this turn with no
reply at all is not a valid way to act on a hand-back."

Both tools' own descriptions repeat the same instruction found throughout this tool family: when combining several
specialists' separately-returned numbers or rows into one finding, cache them with `CacheAssembledTable`
(`docs/AskJooby/DataPointerCache.md`) before reporting onward, rather than repeating them as prose -- that is the
only way the combined result becomes chartable or exportable afterwards.

## `HandBack`: returning a sub-task to whoever owns it

`HandBackTool.HandBack` (`Jube.AskJooby/Tools/Delegation/HandBackTool.cs`) is how a spawned Joobrik ends its own
sub-task when the answer belongs to a slice it does not hold. The tool's own description is specific about timing:
call this "the moment you establish that the answer is not in your own operations -- not after writing SQL against
another slice's tables, and not after reporting that you could not find it." Calling `HandBack` ends the sub-task
outright -- its description says to write nothing further afterward.

Three arguments shape what happens next: `whatIsNeeded` (phrased as the question the *next* specialist has to
answer, not a summary of the current agent's own difficulty -- "Not stated." where there genuinely is no specific
question), `whatIEstablished` (what was worked out before stopping, so the next specialist does not redo it --
"nothing" where that is the literal truth), and `owningAgent`, which is deliberately conservative: it is named only
when the calling agent has actually seen that name in the Pasture or its own instructions, and is checked against
the real roster (`AgentDefinitions.All`) before being passed on at all -- a name that does not resolve to a real
agent is dropped rather than forwarded, with an explicit note in the reply that the claim was dropped, because "a
plausible-looking guess is worse than leaving this unnamed." An unnamed hand-back still reaches whoever can route
it; a wrong name only sends it somewhere that hands it back again; a name for an agent that does not exist is worse
than either.

## `AskClarifyingQuestion`: the turn-ending question

`AskClarifyingQuestionTool.AskClarifyingQuestion` (`Jube.AskJooby/Tools/Clarification/AskClarifyingQuestionTool.cs`)
is called *instead of* writing a prose reply, specifically when the caller's request is genuinely ambiguous between
a small number of distinct, non-overlapping readings -- the tool's own description gives the example of "how many
rules do I have" when Jube has several unrelated rule types. It takes a `question`, 2-6 short `options`, and
`allowFreeText` (default `true`, so the caller can still type their own answer rather than being limited to the
offered options).

The tool returns a small JSON payload (`AskClarifyingQuestionPayload`: `question`, `options`, `allowFreeText`, and a
fixed `instruction` string) that the frontend renders as a selectable prompt -- checkboxes, not free text, unless
`allowFreeText` lets the caller type instead. The `instruction` field is the mechanic worth knowing about
specifically: it always reads "Already shown to the caller as a selectable prompt -- do not repeat the question or
options in your own reply text, and do not call any further tools this turn. Your turn is complete; wait for their
answer." -- and the tool's own `[Description]` repeats the same thing even more bluntly: "This call IS your entire
reply for this turn -- after calling it, write nothing further and call no other tool."

This is also the one tool named by name in `ToolCallAndResultGuardChatClient`'s `SkippedTools` list
(`docs/AskJooby/AgentGuard.md`), exempted from the generic tool-*result* injection guard specifically because its
JSON result's hardcoded `"instruction": "..."` field is exactly the literal shape ("JSON-style role injection") that
guard exists to catch in a genuinely external tool result -- but this tool's result is never externally supplied;
the `instruction` text never varies and the `question`/`options` are the model's own prior arguments, already
screened by the tool-*call* guard. "A tool whose result can carry retrieved, user-, or document-controlled content...
must never be added to this list," per that same page -- `AskClarifyingQuestion` is the one exception because its
result genuinely carries none.

## `FindGraphWorkflow`/`GetGraphWorkflow` and `GetWorkflowProgress`/`RecordWorkflowStep`: graph-directed workflows

`GraphWorkflowTool` (`Jube.AskJooby/Tools/Workflows/GraphWorkflowTool.cs`) and `WorkflowProgressTool`
(`Jube.AskJooby/Tools/Workflows/WorkflowProgressTool.cs`) are the tools behind "a Jube rule is several objects that
only work in one sequence" -- a graph-directed workflow states the order a build or a diagnosis has to proceed in,
which the rest of the documentation describes in prose but never enforces. `FindGraphWorkflow(task)` matches the
caller's own words against each catalogued workflow's exemplars, title and definition by plain word overlap --
"costs nothing -- no retrieval, no model call -- so it works when a turn's search allowance is already spent" -- and
returns the best-matching candidates with their ids. `GetGraphWorkflow(id)` returns one workflow in full: every step
in order, which agent owns it (or, for an **operator step**, an explicit note that "the product cannot perform
this" and the model should say what needs doing and who must do it rather than looking for a tool), which catalogued
operations it calls, what it produces, what it requires first, and what goes wrong if it is done out of order
(`Hazard`). The catalogue these two methods search is the concatenation of three workflow sets --
`RuleTaxonomyWorkflows.All`, `PlatformOpsWorkflows.All`, and `AdvancedAnalyticsTrainingWorkflows.All` -- all defined
in code, not configuration.

`GetWorkflowProgress(workflowId)` and `RecordWorkflowStep(workflowId, nodeId, completed, note)` track how far a
workflow has got *within the current conversation specifically*: the first reports every step's state (done because
an agent said so, done because the operations it names were already run successfully and inferred complete, or
outstanding) and names the next outstanding step; the second lets an agent override that inference in either
direction -- marking done a step that has no operations of its own (an operator step, or one carried out by hand),
or, more importantly per the tool's own description, marking a step *not* done when the tool-call history makes it
look done but it is not, "because a wrong inference left standing silently skips work." Calling `GetWorkflowProgress`
before starting work on a workflow, and again after any interruption, is how an earlier turn's completed steps are
not redone and do not waste the budget that stopped the previous turn in the first place.

## Cross-reference

* `docs/AskJooby/DataPointerCache.md` -- `CacheAssembledTable`, which `HandBack`/`SpawnAgent`/`SpawnAgents` all tell
  the model to reach for before passing several gathered numbers upward.
* `docs/AskJooby/AgentGuard.md` -- `AskClarifyingQuestion`'s exemption from the tool-result guard, and the guard
  pipelines the rest of this tool family's retrieved/forwarded content runs through.
* `docs/AgentHierarchy/index.md` and `docs/AgentHierarchy/orchestration.md` -- the hierarchy and stables that
  `WritePlan`/`SpawnAgent`/`HandBack` route across, and the plan gate's models and status.
