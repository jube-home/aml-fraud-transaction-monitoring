---
layout: default
title: OpenCorporates
nav_order: 9
parent: Ask Jooby
---

# OpenCorporatesTool: KYB company lookups and UBO search

`OpenCorporatesTool` (`Jube.AskJooby/Tools/OpenCorporates/OpenCorporatesTool.cs`) gives the case-analyst stable three methods against the OpenCorporates v0.4 REST API: `SearchCompanyByName`, `GetCompanyDetails`, and `SearchBeneficialOwners`. It follows the same shape as the `SearchWeb`/Tavily slice (see `docs/Concepts/EnvironmentVariables/index.md`'s `AskJoobyWebSearch*` rows) -- a plain `IOpenCorporatesClient` interface, a hand-written HTTP facade (`Jube.Service/AskJoobyOpenCorporates/OpenCorporatesClient.cs`), config-gated DI registration, AgentGuard on both directions, and a dedicated call-log table -- but scoped to one agent stable rather than cross-cut to every agent, since KYB/UBO lookups are a narrow compliance capability, not something every agent plausibly needs.

## Scope: case-analyst only, not cross-cut

Unlike `SearchRag`/`SearchWeb` (added inside `BasicResearchTools`, which every agent gets), the three OpenCorporates tools are added inside `AgentDefinitions.Bound` -- the helper every specialist agent's `ToolsFactory` already calls -- gated on `AgentDefinitions.HierarchyDomainOf.GetValueOrDefault(agentType, agentType) == "case-analyst"`. Every leaf and sub-agent under the case-analyst stable resolves to `"case-analyst"` through that same lookup (used elsewhere for `SpawnAgent`'s stable-ownership checks), so the gate is a single condition rather than a new leaf agent, a `RoutingRules` entry, or a `ServiceToolCatalogue` registration.

## The three methods

- **`SearchCompanyByName`** -- searches OpenCorporates' registry by legal/trade name across every jurisdiction it covers. Returns each matching company's jurisdiction, company number, type, current status, incorporation date and registered address, with the provider's own `source` metadata (publisher/url/retrieved_at/terms) preserved for audit.
- **`GetCompanyDetails`** -- the full record for one already-identified company, by jurisdiction code and company number. Carries through `ultimate_beneficial_owners` when OpenCorporates happens to have it on the record directly, but that field is unreliably populated -- `SearchBeneficialOwners` is the dedicated path for a real UBO search.
- **`SearchBeneficialOwners`** -- OpenCorporates has no single "get me the UBOs" call behind the two methods above; genuine beneficial-ownership data lives at a separate endpoint, `GET /v0.4/statements/control_statements/search`, filtered by `controlled_entity_name` (and optionally `controlling_entities_name`) with `is_ultimate_beneficial_owner=true`. Each result names the controlling entity, whether it is recorded as the *ultimate* beneficial owner, when control began, and the statement's own `provenances` (distinct from a plain company's `source` field -- OpenCorporates uses the two names for two different kinds of record). Coverage varies by jurisdiction and company; an empty result means OpenCorporates holds no statement, not that the company provably has no beneficial owner -- the tool's own description says this explicitly so the model doesn't overclaim from a clean search.

OpenCorporates' own documentation doesn't give a worked example response for the control-statements endpoint, so `OpenCorporatesClient`'s parsing is deliberately defensive (several candidate field names and nesting shapes tried in order, nothing throws on an unexpected shape) and always keeps the full raw response alongside whatever it could parse -- that raw JSON is what the `AskJoobyOpenCorporatesCall` log row preserves regardless of how well the typed parse went.

## AgentGuard, both directions

`OpenCorporatesTool` screens the outbound query/jurisdiction/company-number/owner-name arguments with `AskJoobyInjectionGuard.CheckAsync` before a call is ever sent (refusing and recording a rejection under `askjooby-opencorporates-query-withheld` if flagged), and screens every free-text field coming back -- a company's `name`, `previous_names`, `registered_address_in_full`, and a statement's `controlling`/`controlled` entity names -- before it reaches the model, withholding just the flagged entry and recording under `askjooby-opencorporates-result-withheld`. This matters more here than for a plain web search: a shell company's registered name or address is adversary-controlled text, and a KYB tool exists specifically to reason about entities that have a real incentive to register something unusual. The universal `ToolCallAndResultGuardChatClient` (see `docs/AskJooby/AgentGuard.md`) still screens the same call and result underneath, independent of this tool's own checks.

## Call logging

Every call -- `SearchCompanyByName`, `GetCompanyDetails`, `SearchBeneficialOwners` -- writes one row to `AskJoobyOpenCorporatesCall` (`Operation`, `Query`, verbatim `ResponseJson`, `DurationMs`, keyed to the `AskJoobyRun`) via `IRunRecorder.RecordOpenCorporatesCallAsync` / `AskJoobyRunRecorder`, the same pattern as `AskJoobyWebSearchCall`/`AskJoobyRerankCall`. This is separate from the generic `AskJoobyToolCall` log (which already records every tool call's formatted text result): the dedicated table exists so the provider's raw payload survives both AgentGuard screening and the tool's own prose summary, neither of which is the verbatim response a compliance review might need later.

## Configuration

See `docs/Concepts/EnvironmentVariables/index.md` for the full description of `EnableOpenCorporates`, `AskJoobyOpenCorporatesApiKey`, `AskJoobyOpenCorporatesApiEndpoint`, and `AskJoobyOpenCorporatesMaxResults`. Both `EnableOpenCorporates` and a non-blank `AskJoobyOpenCorporatesApiKey` are required for the tool to be registered at all (`Jube.App/Startup.cs`'s `AddSingletonForAskJoobyOpenCorporates`) -- otherwise it is left out of the case-analyst stable's tool set entirely, the same "never offered, not offered-then-failing" principle every other config-gated AskJooby tool follows.

## Wiring a tool from this class elsewhere

`OpenCorporatesTool`'s methods are ordinary `[Description]`-annotated instance methods, turned into model-callable tools with `AIFunctionFactory.Create` -- the same mechanism every other AskJooby tool uses (there is no `IAgentTool`/`IFunctionTool` marker interface in this codebase):

```csharp
var openCorporatesTool = new OpenCorporatesTool(openCorporatesClient);

var tools = new[]
{
    AIFunctionFactory.Create(openCorporatesTool.SearchCompanyByName),
    AIFunctionFactory.Create(openCorporatesTool.GetCompanyDetails),
    AIFunctionFactory.Create(openCorporatesTool.SearchBeneficialOwners)
};
```

In AskJooby itself this happens inside `AgentDefinitions.Bound` (see "Scope" above), with the `guardRejectionLedger`/`recorder`/`runId` constructor parameters threaded from the current `RunContext` so AgentGuard rejections and call logging land against the right run.
