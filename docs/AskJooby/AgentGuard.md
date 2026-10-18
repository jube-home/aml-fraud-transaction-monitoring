---
layout: default
title: AgentGuard
nav_order: 11
parent: Ask Jooby
---

# AgentGuard: screening both directions of every AskJooby turn

AskJooby talks to an LLM, and an LLM takes instructions from anything that reaches its context, not only the caller
typing in the chat box: a RAG chunk pulled from the Pasture, a memory recalled from the Salt Lick, a prior turn replayed
from history, or the result of a tool call it just made. Any of those can carry text crafted to look like a system
instruction rather than data. AgentGuard (the `AgentGuard`/`AgentGuard.Core`/`AgentGuard.Onnx` NuGet packages) is the
library AskJooby uses to screen that traffic in both directions -- what comes in before an agent sees it, and what goes
out before the caller does -- and to leave a record of what it withheld or blocked and why. It never silently rewrites
content it lets through; when a rule fires, it blocks (or, for retrieved content, withholds just that item) and says
which rule and why.

AgentGuard is a declarative rules engine: a `GuardrailPolicyBuilder` composes named rules into a `GuardrailPolicy`, a
`GuardrailPipeline` runs that policy against a `GuardrailContext` (the text being checked, plus which phase -- input or
output -- it belongs to), and a `GuardrailResult` says whether the content passed, was modified, or was blocked.
AskJooby wires four separate pipelines, each scoped to one trust boundary, rather than one pipeline for everything --
the right rules for a raw chat message are not the right rules for a tool's return value, and the right rules for what
the model is about to say are not the right rules for what it just received.

## The four pipelines

### 1. Chat input (`askjooby-input-injection`)

`AskJoobyInjectionGuard.CheckAsync`/`FindAsync` (`Jube.AskJooby/Security/AskJoobyInjectionGuard.cs`) screens a raw chat
message, and a small text attachment, before any agent sees it: `NormalizeInput` (decodes base64/hex/Unicode evasion
tricks first, so an encoded payload cannot slip past the rules below it in disguise), `BlockPromptInjection` (the
deterministic regex floor, unconditional, never gated by anything below), then `DetectSecrets` (blocks API keys, tokens,
connection strings pasted into chat) also unconditionally, then `BlockPromptInjectionWithDefender` (the bundled StackOne
Defender ONNX model) at a confidence bar set by `AskJoobyChatInputPromptInjectionMainThreshold` (default 0.97,
deliberately liberal -- see that variable's own entry in `docs/Concepts/EnvironmentVariables/index.md` for why 0.97
specifically), and finally `EnforceTopicBoundaryWithLlm` only when the tenant's `TopicBoundaryInput` rule is enabled,
which is **off by default** (`AskJoobyEnableChatInputTopicBoundary`, also False by default) -- unlike every other rule
in this pipeline, which defaults to enabled with no row configured. All of these cross-cutting defaults are read once at
startup into one `AskJoobyInjectionGuardSettings` record via `AskJoobyInjectionGuard.Configure`, not as separate loose
parameters. The topic-boundary rule defaults to off because, in practice, it rejects ordinary, squarely on-platform
requests ("build me a c5 tree in r using the demo dataset") often enough to be unworkable for a trusted on-prem
deployment, and -- unlike the Defender classifier -- it is binary with no confidence threshold to loosen instead of just
turning off. Secrets detection is never part of either toggle -- a leaked key is worth catching regardless of how
trusted the person pasting it is. It deliberately does **not** run PII redaction here -- AskJooby's entire job is
reasoning about PII-bearing fraud/AML data, so redacting PII from ordinary chat input would break the product rather
than protect it; PII screening is scoped to the one place that actually needs it, `AskJoobySensitiveDataGuard`, over
what a `Remember` call tries to persist long-term.

**Retrieved content takes a different, narrower pipeline, never this one.** `MemoryTool`, `RagSearchTool`,
`ConversationHistoryTool`, `WebSearchTool` and `OpenCorporatesTool` run each candidate memory, Bite, history hit or
external result through `AskJoobyInjectionGuard.CheckToolContentAsync`/`FindInToolContentAsync` individually --
`NormalizeInput`, `BlockPromptInjection` and `BlockPromptInjectionWithDefender` unconditionally (this one has no tenant
toggle, because the content here did not originate with the logged-in caller and the risk does not shrink because the
caller is trusted), then `DetectSecrets`, and **no topic-boundary judge at all**. A decontextualised row -- a
transaction field, a rule fragment, one Bite out of a search result -- has no topic of its own for an LLM judge to
classify, and running one here reliably misjudged the platform's own data as off-topic; injection and secrets screening
are deterministic and need no topic to apply to. Each hit that fails is withheld individually (the turn is not blocked
over it), recording the withheld count and the rule that caught it via `AgentGuardRejection.ForWithheldContent`.

### 2. Chat output (`askjooby-output-guard`)

The same class's `CheckOutputAsync` screens the model's own finished reply text before it reaches the caller. This is
deliberately a **narrower** pipeline than the input one: only `EnforceTopicBoundaryWithLlm` (keeps a reply inside Jube's
own subject matter) and, if the Opir offline content-safety model is configured (`AGENTGUARD_OPIR_MODEL_PATH`/
`_TOKENIZER_PATH`/`_PREFIX_PATH`), `BlockUnsafeContentWithOpir`. It never runs the prompt-injection pattern family
(`BlockPromptInjection`/`BlockPromptInjectionWithDefender`/secrets detection) against the model's own output text.

This asymmetry is a deliberate design decision, not an oversight: AskJooby routinely writes SQL, JSON and other
rule-syntax back to the caller as part of an ordinary, correct answer, and the injection-pattern rules are tuned to flag
exactly that kind of syntax (`"role":`, `UNION SELECT`, bracketed tags, and the like) when it appears anywhere,
including in a perfectly legitimate reply. Running them against output was judged to cost more in false positives on the
platform's own domain language than it could ever catch, since nothing in a reply the model is about to send is
"indirect" the way a tool result or a retrieved chunk is -- there is no second author to distrust. Topic-boundary and
content-safety are the two output rules that still matter regardless of syntax, so those are what stay.

The topic-boundary classifier judges the bare reply text in isolation against `AllowedTopics` -- operating, configuring,
authoring or training anything on Jube's platform (deliberately broad: a request using only generic business,
statistical or technical vocabulary and naming no Jube-specific term is still in scope, since ordinary platform work
routinely reads exactly like that in isolation), the account/transaction/case data AskJooby reasons about, and
AskJooby's own procedural narration. This same broadened list is what the input pipeline's (off-by-default) topic check
above judges against too. Because some of AskJooby's own replies are not conversational prose -- the validator agent (
"Jadek", see `ResponseValidator`) answers in a terse `STATUS: PASSED`/`STATUS: FAILED`/`REASON:`/`KIND:` form when
fact-checking another agent's draft -- the output pipeline's topic-boundary prompt explicitly names that shape as
in-scope whenever it concerns Jube's own content, including a bare `STATUS: PASSED` with no `REASON` at all (the normal
shape of a clean pass, not evidence of being off-topic). Without that framing, a classifier judging the acronym-laden
string with no indication of what it actually is can misjudge it as unrelated to fraud/AML.

**A Landlord-only topic on top.** `AllowedTopics` does not cover AskJooby's own platform-ops/infrastructure surface
(Postgres, Redis, Docker, etcd and Patroni observability and administration -- the `platform-ops` agent domain); a
Landlord asking something like "does the infrastructure look ok" falls outside it as far as the classifier is concerned.
`AskJoobyInjectionGuard` builds its pipelines over `AllowedTopicsForLandlord` -- `AllowedTopics` plus one more topic
naming this platform's own live operational infrastructure -- when the caller is a Landlord, and over the narrower
`AllowedTopics` otherwise. `CheckAsync`/`CheckOutputAsync`/`FindAsync` take an `isLandlord` parameter (default `false`,
so every caller that doesn't explicitly pass it keeps the narrower, non-Landlord boundary) selecting which to run
against. This is a scoped widening, not a general loosening of the rule: every tool over that third topic is itself
`RequiresLandlord`-gated in `ServiceToolCatalogue`, so a non-Landlord caller could never act on it regardless of what
the topic-boundary classifier would allow, and a non-Landlord caller's own `AllowedTopics` are completely unchanged.
`isLandlord` is threaded through from `permissionValidation.Landlord` at the pre-send input check
(`AskJoobyConversationService.SendMessageAsync`) and from `RunContext.IsLandlord` at `ToolCallAndResultGuardChatClient`
's construction in `AgentFactory` for the output check; the retrieved-content checks in `RagSearchTool`/
`ConversationHistoryTool`/`MemoryTool`/`CompactedHistoryTool` and the small-attachment check in
`BuildAttachmentContextAsync` are deliberately left on the narrower default, since the widening is about the
*conversational framing* of a reply discussing live infrastructure, not about whether infrastructure-related Pasture
content is itself in scope (it already is, under the existing "...and codebase" topic).

### 3. Tool calls and tool results (`ToolCallAndResultGuardChatClient`)

`ToolCallAndResultGuardChatClient` (`Jube.AskJooby/Security/ToolCallAndResultGuardChatClient.cs`) is a
`DelegatingChatClient` wired as the outermost decorator in `AgentFactory`'s chat-client chain, so every agent at every
depth gets both checks for free. It guards a different trust boundary from the two pipelines above: not the chat text
itself, but a proposed *action* (the arguments the model wants to hand a real tool implementation) and the *result* of
one (content that is about to re-enter the model's context from a tool it just called).

* **Tool calls** (`askjooby-tool-call-guard`, `GuardToolCalls`): inspects a tool call's arguments for SQL injection,
  code injection, path traversal, command injection, SSRF, template injection and XSS before the harness ever dispatches
  the call. The arguments exempted from this generic scan, and why, are declared once in `AgentGuardToolProvenance`
  (`Jube.AskJooby/Security/AgentGuardToolProvenance.cs`) -- every agent that holds a given tool gets the same,
  intent-named provenance for its arguments, regardless of what else that agent happens to hold. Two kinds are declared:
  `ToolArgumentProvenance.AlreadyValidatedElsewhere` for an argument that already passes a domain-specific, stronger
  validator before AgentGuard ever sees it -- `RunSqlQuery`'s `sql` (guarded by `PostgresSqlValidator`/
  `AssertSelectOnly`'s AST-level SELECT-only check) and `ValidateRScript`/`RunScript`'s `script` (guarded by the R
  sandbox's own layered, statically-verified safety model -- see `docs/AnalyticalMethodology/RServer/index.md`); double-guarding either with
  this generic regex only risks breaking a legitimate query or script this layer cannot tell from an attack, and in
  practice the generic cross-language code-injection regex reliably flagged ordinary R as "JavaScript code injection."
  And `ToolArgumentProvenance.NaturalLanguage` for an argument that is inherently free text to a delegate or a search
  index, never code -- `SpawnAgent`'s `task`, `SpawnAgents`' `subTasks`, `SearchRag`'s `query`, `SearchWeb`'s `query`;
  scanning these for code-injection patterns is a category error, not a tuning problem, so the exemption applies to
  every agent that holds the tool, not only ones that happen to lack some other capability.
* **Tool results** (`askjooby-tool-result-guard`, `GuardToolResults`): inspects a tool's return value for indirect
  prompt injection -- role markers, instruction-override phrasing, encoded payloads and the like -- before it reaches
  the model, with the action set to Block rather than sanitize-and-continue, matching the same "no downside to blocking"
  standard the input guard already applies to chat text. The tools skipped by this check, and why, are also declared
  once in `AgentGuardToolProvenance`, as a `ToolResultProvenance`. `ToolResultProvenance.PlatformGenerated` covers a
  result that is this codebase's own static scaffolding, never externally supplied -- `AskClarifyingQuestion`
  (`Jube.AskJooby/Tools/Clarification/AskClarifyingQuestionTool.cs`), whose JSON result carries a hardcoded
  `instruction` field telling the model its turn is over, and whose literal shape `"instruction": "..."` is exactly the
  "JSON-style role injection" pattern this rule exists to catch in a *genuinely external* tool result (a poisoned email,
  a scraped document). `ToolResultProvenance.PreScreened` covers a result assembled entirely from items that were
  already screened individually before assembly -- `SearchRag` (`RagSearchTool`), whose every candidate Bite already
  passes through `AskJoobyInjectionGuard.CheckToolContentAsync`'s injection/secrets pipeline before assembly (see §1
  above), so by the time a result reaches this second, cruder regex pass it has already been through the one that
  actually distinguishes adversarial content from data; the second pass added nothing except a structural,
  self-referential failure mode, since large parts of the Pasture are AgentGuard's own security documentation, written
  in exactly the vocabulary ("injection," "override," "instruction," "role," JSON samples) this rule matches on, so
  asking Jadek to research AgentGuard's own behaviour would otherwise reliably trip the detector on the very docs
  describing it. The general principle still holds for anything not declared in `AgentGuardToolProvenance` -- a tool
  whose result can carry retrieved, user-, or document-controlled content keeps full screening by default --
  `MemoryTool` and `ConversationHistoryTool` are not declared `PreScreened`, because nothing about their design
  currently guarantees the same prior per-item screening invariant for every code path that reaches them; if that ever
  changes for one of them, a `PreScreened` declaration is the right fix, but it must be verified for that tool
  specifically, never assumed or added to silence one false positive.

Both of these run at the `Output` guardrail phase (the model's proposed next action, or what it is about to be shown),
independent of the two `AskJoobyInjectionGuard` pipelines above.

## The rejection ledger

Every rule firing -- whichever of the four pipelines caught it -- is recorded through `IAgentGuardRejectionLedger`/
`AgentGuardRejection` as a fact of what happened, never the content itself: phase (`Input`/`Output`), pipeline name,
rule name, a human-readable reason, and the action taken (`Blocked` or `Withheld`), matching AgentGuard's own hash-only
default for this kind of audit record. `AskJoobyPersistedGuardRejectionLedger` (`Jube.Service/AskJoobyConversation/`)
writes each one to the `AskJoobyAgentGuardRejection` table, scoped to the conversation and tenant it happened in.

A chat-audit export (`AskJoobyChatAuditMarkdownBuilder`) surfaces the full list for a conversation under a
`## AgentGuard Rejections` heading, one row per firing with when/phase/pipeline/rule/action/reason -- this is the trail
to read first when a transcript shows a turn that stalled, degraded, or had a reply visibly replaced ("Message is
outside the allowed topics...", "I'm not going to act on that tool result..."), since the ledger says exactly which rule
fired and on what, without needing to reproduce the call to find out.

## Where to look when a turn hits AgentGuard

* A chat message or a retrieved chunk/memory/history hit was blocked or withheld: `AskJoobyInjectionGuard.CheckAsync`/
  `FindAsync`, `Jube.AskJooby/Security/AskJoobyInjectionGuard.cs`.
* A reply the model was about to send was replaced: `AskJoobyInjectionGuard.CheckOutputAsync`, same file -- check the
  topic-boundary `AllowedTopics`/system prompt first, since that is the rule this screens with.
* A proposed tool call, or a tool's own result, was blocked: `ToolCallAndResultGuardChatClient`,
  `Jube.AskJooby/Security/ToolCallAndResultGuardChatClient.cs` -- check `AgentGuardToolProvenance`
  (`Jube.AskJooby/Security/AgentGuardToolProvenance.cs`) for whether the tool's argument or result is already declared
  exempt, and why.
* What actually fired, for a specific conversation: the `## AgentGuard Rejections` section of that conversation's
  chat-audit export, or the `AskJoobyAgentGuardRejection` table directly.
