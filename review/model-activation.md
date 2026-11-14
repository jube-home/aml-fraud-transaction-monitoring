# Slice review: model-activation

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5 (npm part). Nothing was built, no .NET or DB-backed test was run, no
Rider lint run, Playwright not run. Scratch scripts lived in `scratchpad/model-activation/` (server_tr.py was first
written in the shared scratchpad, copied here, and the 42 resx re-verified by key and placeholder parity against the
neutral files).

## 1. Summary and verdict

Verdict: READY AFTER ONE SERIAL TEST PASS (list in section 5). Seven real defects were fixed (two security relevant: a
model level override bypassing rule opt-ins, and a cross-tenant Case Workflow reference). Override surface: Suppress is
the default; Force is gated at three points (permission 62 checked before validation, rule `EnableForce`, rule
`OverrideKey` binding) and engine admission re-checks all of them at evaluation time. Override rows are keyed to the
caller's tenant through the model. No path found that creates an activation without rule opt-in after the fixes. No
permissive path was reintroduced (no grant or role code touched).

## 2. Files reviewed

Read: `Jube.Validations` ActivationRuleOverride, Override, ActivationRule (whole), ResponseElevationRange (whole)
validators; `Jube.Service` ActivationRuleOverride service (insert, update, expiry, kind, permission helpers, mapper),
Override service (insert/update/expiry, mapper), ActivationRule service (permissions, insert, mapper),
ResponseElevationRange service constructor and permissions; `Jube.Data` repositories for rule override, model override,
activation rule (whole non-query parts), range (update and ParentModelVisible); `EntityAnalysisModelParentGuard`;
`ActivationRuleOverridesExtensions`, `EntityAnalysisModelOverride` (engine), `IterateActivationRulesExtensions` lines
40-100, `SyncOverridesExtensions`; endpoint file `EntityAnalysisModelActivationRuleOverrideEndpoints` (lines 14-120,
others same shape, not read); `OverridePage.vue` diff; `src/overrides/api.ts`. Tests read: see section 5. Pull-ins from
`--closure` (DataGrid, useFormat, i18n, en.json, joobyContext, ProblemAlert) treated as inherited. Not read:
`OverrideManager.vue`, `OverrideRuleGrid.vue` bodies, activation rule and range endpoint files, EntityApproval
maker-checker services.

## 3. Findings

| #  | Sev    | Where                                           | What                                                                                                                                                                                                       | Status                                                                        |
|----|--------|-------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------|
| 1  | High   | ActivationRuleOverridesExtensions.Admissible    | Model level override (rule-less) carried `AllActivationRules` unfiltered: Force reached rules with EnableForce off, and any all-rules override ignored the rule's single OverrideKey and ignored admission | Fixed: expanded per admitting rule; unchanged when every rule admits          |
| 2  | High   | ActivationRule validator                        | CaseWorkflowGuid/StatusGuid only NotEmpty: cross-tenant and cross-model workflow references accepted                                                                                                       | Fixed: `CaseWorkflowStatusVisibleAsync`, key `CaseWorkflowStatusGuidNotFound` |
| 3  | Medium | ActivationRuleRepository.UpdateAsync            | Client could move the rule to another Model and forge or wipe counters (ForcedActivationCounter was wiped by every update)                                                                                 | Fixed: pinned from existing; zeroed on service insert                         |
| 4  | Medium | ResponseElevationRange validator and repository | Parent check used tenantless `GetByIdAsync` (no Deleted filter); update could move range to another Model                                                                                                  | Fixed: `ParentModelVisibleAsync`, pinned model id                             |
| 5  | Medium | Override services                               | Expiry change on a Force override needed only perm 2 (extends a widening override)                                                                                                                         | Fixed: perm 62 required when the target row is Force                          |
| 6  | Medium | Override mappers                                | Delete Expiry Date validated but dropped on create                                                                                                                                                         | Fixed                                                                         |
| 7  | Low    | Override repositories                           | Update with an Id matching no visible row inserted a new row; model Override Update by key had no tenant filter                                                                                            | Fixed (KeyNotFound; tenant filter)                                            |
| 8  | Low    | resx                                            | `EntityAnalysisModelTtlCounterGuidNotFound` and all seven locales missing                                                                                                                                  | Fixed                                                                         |
| 9  | Info   | validators `sealed partial`                     | GeneratedRegex forced partial in the rule and range validators                                                                                                                                             | Fixed in follow-up (section 12)                                               |
| 10 | Info   | DTO ListColumn titles                           | hard-coded English, shared                                                                                                                                                                                 | Left, BUG line in SHARED-TODO (form-model-engine owns the keys)               |
| 11 | Design | CreatedUser/CreatedDate overwritten on update   | by design for maker-checker                                                                                                                                                                                | Left, labelled design (section 12)                                            |

Known item (b): `TtlCounterVisibleAsync` read; ties counter to the model guid and tenant. Elevation and override paths
now have the equivalent checks (findings 2, 4; override validators already use tenant-scoped `GetByGuidAsync` and rule
lookup by model and tenant). Design question, not changed: Force override rows are created at rule opt-in, and removal
of Enable Force later leaves rows that the engine ignores (documented).

## 4. Fixes made

Files: ActivationRuleOverridesExtensions.cs; EntityAnalysisModelActivationRuleDtoValidator.cs, Repository, Service
(insert counters); ResponseElevationRangeDtoValidator, Repository, Service ctor; both Override mappers, repositories
(`IsActiveForceAsync`, KeyNotFound, tenant filter) and services; `EntityAnalysisModelActivationRuleResources.cs` and
neutral resx (one new key). No code comments added.

## 5. Test and pen-test matrix

Read: `EntityAnalysisModelActivationRuleServiceTests` (63 -> 73), `...TtlCounterTests` (model-abstraction, 4),
`...ActivationRuleOverrideServiceTests` (41 -> 44), `...OverrideServiceTests` (35 -> 38),
`...ResponseElevationRangeServiceTests` (53 -> 55), `ActivationRulesHelperExtensionsTests` (+3),
`IterateActivationRulesExtensionsTests` (Force tests 192-295), `PenTestEntityAnalysisModelActivationRuleOverride`,
`PenTestModelOverride` (+1), `ModelAreaPenTestBase` (test list: matrix, IDOR, mass assignment, injection, lengths,
locked, concurrency), `PenTestModelResponseElevationRange` (via base), `OverrideRouteTests`. Existing coverage: matrix
per verb, cross-tenant read/update/delete, mass assignment, hostile strings, validators one per rule, audit and change
event. Added:

- Service: `CaseWorkflowStatusOfTheSelectedModelIsAcceptedAsync`, `UnknownCaseWorkflowGuidsAreRefusedAsync`,
  `ACaseWorkflowOfAnotherTenantCannotBeReferencedAsync`,
  `ACaseWorkflowOfAnotherModelInTheSameTenantCannotBeReferencedAsync`, `UpdateCannotMoveARuleToAnotherModelAsync`,
  `CountersSuppliedByTheClientAreIgnoredOnInsertAndUpdateAsync`; both override files:
  `UpdateDeleteExpiryDateOfAForceRowIsRefusedWithoutTheForceOverridePermissionAsync`,
  `InsertKeepsTheDeleteExpiryDateTheCallerSuppliedAsync`,
  `UpdateWithAnIdThatMatchesNoVisibleRowThrowsNotFoundAndCreatesNothingAsync`; range:
  `InsertUnderASoftDeletedModelIsRejectedAsync`, `UpdateCannotMoveARangeToAnotherModelAsync`.
- Unit: three `ActivationRuleGetOverridesDoesNotLetAModelLevel...` tests.
- PenTest: `ForceOverrides_NeedTheSeparateForcePermission_OnEveryWritePathAsync`.
  Must be run (owner serial pass): `EntityAnalysisModelActivationRuleServiceTests`, `...TtlCounterTests`, both Override
  service test classes, `EntityAnalysisModelResponseElevationRangeServiceTests`, `ActivationRulesHelperExtensionsTests`,
  `IterateActivationRulesExtensionsTests`, `OverrideModelSyncTests`, `PenTestModelOverride`,
  `PenTestEntityAnalysisModelActivationRuleOverride`, `PenTestEntityAnalysisModelActivationRule`,
  `PenTestModelResponseElevationRange`, plus a build (new tests insert CaseWorkflow/CaseWorkflowStatus rows directly:
  confirm no unlisted NOT NULL column). Not pinned by a test: `CacheActivation*Idempotency` call-site swap from
  model-abstraction BUG line (activation owner was asked to add one; still open).
  Front end: prettier, eslint clean on slice files; `vue-tsc --build` passes; vitest on `src/overrides` and
  `src/components`: 8 files, 76 tests pass. Playwright specs `override`, `overridePage` exist (7 and 15 tests), not run.

## 6. Translations

Created 42 resx (pl, ar, ka, id, sv, ur, hi for ActivationRule, ActivationRuleOverride, ActivationRuleOverrideQuery,
Override, OverrideQuery, ResponseElevationRange); key parity and `{0}` parity checked by script; ar and ur wrap Latin
tokens, hex colours and placeholders in U+2068/U+2069 isolates; Western digits kept. `.fr.resx` untouched. Machine
translation, POC quality. SPA: `override.*` 50 keys present in en/pl/ar (audit: 0 missing); proposals for ka, id, sv,
ur, hi in `review/model-activation.locale-proposals.json`. Not audited visually: font clipping for ka/ur/hi and RTL
layout of the Override page (no browser run); shared font work is already in SHARED-TODO (data-grid).

## 7. Docs and TODO markers

Five pages under `docs/Configuration/Models/` (BasicActivationRules, ActivationRulesOverride, ResponseElevation,
ResponseElevationRanges, ActivationRuleNotifications): 35 screenshot markers placed above existing images (5, 18, 6, 0,
6), plus 12 TODO (docs) markers for new behaviours and verification (grep `TODO(` in those files for lines).
ResponseElevationRanges has no images: owner to supply. `docs/Testing/ServiceModelConfiguration.md`,
`SecurityAccessIsolation.md`, `Engine.md` updated with the new tests and counts.

## 8. Shared-code TODOs raised

See the last eight lines of `review/SHARED-TODO.md` (four BUG lines fixed, partial class, ListColumn titles, CreatedUser
overwrite, slices.json).

## 9. Manual test sheet

Override page (`/override`, permission 2): list keys, values, models, rules; toggle Suppress on and off; set and clear
expiry; refusal for a past date; Force option absent without permission 62, 403 if forced by API; Force on a rule
without Enable Force refused; rule bound to another key refused; model level Suppress/Force against a rule without
opt-in has no effect in a test transaction; two tabs toggling the same override; deep link and refresh; narrow width;
dark theme; en, pl, ar (RTL); keyboard only; hostile text in key and value (script, SQL, 300 characters). Rule form:
Case Workflow Status from another Model refused; TTL Counter from another Model refused; edit does not change counters
or Model; response elevation outside any active range refused; range overlap, decline plus hold, web hook to localhost
refused, credential variable not JUBE_HOOK_.

## 10. Commit pathspecs

slices.json paths plus: the C# files in section 4, the 42 new resx and the changed neutral resx/cs, the new tests, five
docs pages, three Testing pages, `review/model-activation*`. Hand-pick hunks in shared
`ActivationRulesHelperExtensionsTests.cs`, `PenTestModelOverride.cs`, `SHARED-TODO.md`.

## 11. Not covered

Rider lint on C#; the central `EntityApprovalService` beyond the rule and range kinds (maker-checker slice); activation
rule and range endpoint files; engine side of response elevation counters and notifications; webhook delivery (SSRF
policy read only through the validator); visual RTL and font checks; Playwright; the cross-model TTL counter design
question (model-abstraction finding 4).

## 12. Follow-up pass (owner policy: bugs fixed, gaps closed)

1. **Partial classes merged.** `EntityAnalysisModelActivationRuleDtoValidator` and
   `EntityAnalysisModelResponseElevationRangeDtoValidator` were `sealed partial` only for `[GeneratedRegex]` (single
   file, no other part anywhere: grep of the repo finds one declaration each). Now plain `sealed class` with
   `static readonly Regex` fields (Compiled, CultureInvariant, 1 s timeout). The hex pattern was changed from `$` to
   `\z`, so `#FFFFFF` followed by a newline no longer passes (before, `$` matched before a trailing newline). No usings
   changed (`System.Text.RegularExpressions` already imported, ImplicitUsings covers `TimeSpan`). Tests to run: both
   service test classes (colour tests); no new test for the newline case.
2. **Maker-checker, read.** `EntityApprovalService` (Approve lines 90-135, Reject, `RefuseOwnChange`, `StatusOf`),
   `VersionedApprovalRepository` (Resubmit, Retract, Archive), `EntityApprovalSubject.MakerUser`,
   `GetApprovedByEntityAnalysisModelIdInPriorityOrderAsync` and the engine sync. Verified: the maker is `CreatedUser` of
   the current version or `DeletedUser` for a delete (`MakerUser => Deleted ? DeletedUser : CreatedUser`); a maker
   cannot approve or reject their own change (landlord exempt, existing design); a second approval by the same user,
   approving an already effective version and rejecting a live one are each refused; rejected versions cannot be
   approved and an edit opens a new cycle; Resubmit and Retract require a rejected current version, bump the version,
   archive the old row and (Retract) carry the baseline approvers forward with `CarriedForwardFromVersion`; the engine
   reads the approved projection for rules (`ApprovalsRequired`) and ranges. No defect found in these. The central
   service belongs to the maker-checker slice (slices.json `EntityApproval`, `MakerChecker`) and was read only through
   the activation rule and range kinds. Override rows (`EntityAnalysisModelOverride`, `...ActivationRuleOverride`) are
   not an `EntityApprovalKind`: they are toggle rows with a soft delete and a Version audit table, gated by permission 2
   and, for Force, 62. That is a design position (an operational switch with its own separate Force permission),
   recorded not changed; the owner may want Force overrides under maker-checker.
   Gap closed: the activation rule service had no Resubmit or Retract tests (the range had). Added to
   `EntityAnalysisModelActivationRuleServiceTests`:
   `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`,
   `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndRecordsTheCallerAsMakerAsync`,
   `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`,
   `RetractOfRejectedEditRestoresLastApprovedContentAndCarriesTheApprovalAsync` (class now 73 tests, catalogue updated).
   Observation, not changed: `UpdateAsync` on a soft-deleted rule or range resurrects it (`Deleted = 0`) as a new
   pending version; this is the shared pattern of the versioned repositories and the new version needs a fresh approval,
   so it is not a bypass.
3. **CreatedUser overwritten on update: DESIGN, confirmed.** Evidence here:
   `EntityAnalysisModelActivationRuleRepository.UpdateAsync` sets `CreatedUser = userName`, `CreatedDate = UtcNow`; so
   does the range repository; `VersionedApprovalRepository.ResubmitAsync` and `RetractAsync` call
   `SetCreatedUser(existing, UserName)`; and `EntityApprovalSubject.MakerUser` reads `CreatedUser` to enforce separation
   of duties. The previous author stays in the Version archive row written in the same update. Preserving the original
   creator would let a maker approve their own edit. Same conclusion as model-abstraction section 13. A separate updater
   column would need a migration (owner schema decision).
4. **Grid titles.** Precise BUG line appended to `review/SHARED-TODO.md` (activation rule DTO line 53, range DTO lines
   50-102); not fixed, form-model-engine owns the keys.
5. **Override Vue components read and fixed** (`OverrideManager.vue`, `OverrideRuleGrid.vue`, plus
   `src/overrides/cells.ts`, `api.ts`). Findings: (a) every switch, kind select and expiry input carried the same
   aria-label ("Override", "Kind", "Delete expiry date"), so a screen reader could not tell rows apart: now
   `label: <name>` with the row name; (b) keys, values, model, rule and user names (free text, may be Latin inside
   Arabic or Urdu) were rendered with no bidi isolation: grid cells use a `format: isolate` that wraps in U+2068 and
   U+2069, the crumbs and the values-view panel title do the same, and the value input has `dir="auto"`; (c) no
   hard-coded UI strings, no physical left/right utilities (alignment uses `align: 'end'`), validation messages come
   from the server localiser through `ProblemAlert` and `override.tooEarly`; (d) cell widgets are plain DOM built with
   `textContent` (no HTML injection). Not fixed, shared: Nastaliq and Devanagari clipping in `select-xs` and `input-xs`
   (fixed `w-28`, `w-48`; data-grid font TODO covers it). Checks: prettier and eslint clean, `vue-tsc --build` clean,
   vitest `src/components` and `src/overrides` 76 pass. Playwright `override` and `overridePage` not run; they assert
   option text and testids only, none that the isolate marks change (crumb text is never asserted).
   Tests to run in addition to section 5: `EntityAnalysisModelActivationRuleServiceTests` (new Resubmit and Retract
   tests), `EntityAnalysisModelResponseElevationRangeServiceTests`, and Playwright `override.spec.ts`,
   `overridePage.spec.ts`.
