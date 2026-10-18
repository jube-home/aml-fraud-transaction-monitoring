---
layout: default
title: Orchestration Runtime
nav_order: 1
parent: Agent Hierarchy
grand_parent: Ask Jooby
---

# Orchestration: models, the plan gate, and prompt caching

This describes the runtime as it stands on top of the agent hierarchy (`AgentDefinitions.cs`, `PageAgentToolBinder`):
the models each role runs on, the plan gate that sits in front of every delegation, and the prompt-caching layout that
keeps a conversation's preamble from being paid for on every turn.

## Models

| Role           | Model               | Why                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
|----------------|---------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Executor       | DeepSeek V4.1 Flash | Every agent turn. Cheap input, so the hierarchy's narrow toolboxes matter more for attention than for cost.                                                                                                                                                                                                                                                                                                                                                                                |
| Critic (Jadek) | DeepSeek V4.1 Flash | The same locally served model as the executor, chosen for regional availability. This gives up the independence a critic drawn from a different model family would have -- a critic sharing a family of weights with the executor shares some of its blind spots -- so the critic leans on the thing that does differ: it searches the Pasture itself and judges the reply against retrieved text rather than against its own prior. Low reasoning effort, since the judgement is bounded. |

There is no elective reasoner tool and no `AskJoobyEnablePlanning` switch in the code or the configuration. The critic
and graph-directed workflows are what the hierarchy leans on in preference to a reasoning tier.

Thinking is off by default (`AskJoobyEnableThinking` is False) and is not a substitute for a plan. The orchestrator
writes a plan before any handover to a specialist, and the code refuses a handover without one; leaf specialists do not
plan. `WritePlan` (`Jube.AskJooby/Tools/Delegation/PlanTool.cs`) is offered to the orchestrator beside its spawn tools
and records the plan on the turn; `SpawnAgent`/`SpawnAgents` refuse at depth 0 until a plan exists, fail closed when the
turn has no ledger, and count each refusal in `UnplannedHandovers`. See `docs/AskJooby/OrchestrationTools.md` for the
tool-level detail.

A mandatory planning stage in front of every request is not run. The retrieval corpus carries the Jube-specific
knowledge a plan depends on, and Flash plans well enough against it that forcing a second model through the critical
path would buy latency and configuration rather than quality.

DeepSeek's own thinking (`ThinkingOptions`, the `reasoning_content` stream `BasicAgentRunnable` surfaces) stays off by
default, switched on by `AskJoobyEnableThinking`. The planner reasons natively while it plans.

A plan is validated deterministically, in code, before a single node runs, rather than by trusting the planner to have
reasoned its way to a good one.

The critic stays a separate model from the executor; independence is the point of it.

## Prompt caching

The inference provider caches a prompt prefix per worker and matches it byte for byte, so two things decide whether a
conversation pays for its own preamble once or on every turn.

The first is routing. Every request the inference client makes carries `X-Model-Affinity` set to the conversation's Guid
(`ModelAffinityScope`, stamped on by `ModelAffinityPolicy`), which asks the provider to keep sending that conversation
to the worker already holding its prefix. One `AsyncLocal` set at the top of a turn reaches the Worker, every spawned
Joobrik, the validator, the suggestions call and the embeddings, including anything detached onto its own task, so a
whole turn pins to one worker without any of those paths knowing the mechanism exists. The live model-coherence test
uses the same policy under a fixed value, since its prompts never change between runs.

The second is layout: static first, volatile last, everywhere. `InstructionsBuilder` sorts the specialist list rather
than merely enumerating it, keeping that block static. `ResponseValidator.BuildTask` and `ResponseReviser.BuildTask`
lead with the static block of unchanging criteria and append the volatile question-and-reply part, with only a one-line
format reminder after it, because the instruction immediately before the model answers is the one that gets followed.
For the same reason the page a caller happens to be on is appended to their message rather than prepended to it: it
changes as they move around Jube within one conversation.

## What already exists

| Concern                | Component                                                                                                                        |
|------------------------|----------------------------------------------------------------------------------------------------------------------------------|
| Response criticism     | `ResponseValidator`, `ValidationResult(Approved, Feedback, FailureKind)`, `ResponseReviser`, gated by `AskJoobyEnableValidation` |
| Bypass signalling      | None. Every substantive reply is validated, and no tool output is exempt                                                         |
| Chat interception      | `UserQueryGuardChatClient` and `PersistedChatClient`, both `IChatClient` decorators                                              |
| Tool interception      | `PersistedAiFunction`, `SnapshotOnWriteAiFunction`                                                                               |
| Snapshot gating        | `IAutoSnapshot.Taken` / `TakeBeforeWriteAsync`, enforced per write-capable agent                                                 |
| Snapshot and restore   | `PreservationSnapshot` (Write) and `PreservationRecover` (Delete, Destructive) -- real catalogued operations                     |
| Run and tool telemetry | `IRunRecorder.StartRunAsync` / `CompleteRunAsync` / `RecordToolCallAsync` / `RecordModelCallAsync`                               |
| Delegation and depth   | `AgentDefinition.AllowedDelegates`, `RunContext.MaxDepth` (3), `RunContext.MaxFanOut` (8)                                        |

Every chat client and tool interception point above is a decorator, or a per-turn signal object carried on `RunContext`,
matching the same pattern throughout.
