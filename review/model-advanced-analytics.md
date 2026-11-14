# Slice review: model-advanced-analytics

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing was built and no .NET or DB-backed test was run. npm
prettier, eslint, type-check and vitest (askJoobyAnalytics and approval specs, 34 tests) ran clean on slice files.

## 1. Summary and verdict

Verdict: READY AFTER OWNER DECISIONS and one serial test pass. Two maker-checker bypasses fixed (output flags;
model-version promote and rollback), one RTL chart defect fixed in Jube.Charting. Landlord self-approval remains a
labelled design decision. SPA ka/id/sv/ur/hi proposals are now machine translated (follow-up pass).

Answers to the brief:

- **Naming.** No `Accord` in Jube code, UI, routes, resx or the slice docs (grep over tracked files excluding Accord.*
  projects). Remaining hits are library `using` lines, project references, `$type` marker strings in
  `Jube.Service/ModelDiagrams/SerialisationMarkers.cs` (functional, they match library type names), tests that assert
  the name is absent, and docs. Two API docs pages (PerformanceTrace, InfrastructureHealthMetrics) were reworded.
  `docs/AnalyticalMethodology/*` still names it (shared TODO, likely a legitimate exception).
- **Model serialisation.** `AdvancedAnalyticsModelJson` (Jube.Engine/AdvancedAnalytics/Serialisation):
  TypeNameHandling.None, MetadataPropertyHandling.Ignore (so `$type` is inert), MaxDepth 32, 16M character cap on both
  read and write, duplicate keys and trailing content refused, MissingMemberHandling.Error, kind checked. No
  BinaryFormatter or binder found in the slice. Covered by `AdvancedAnalyticsModelJsonTests` (24, names only).
- **Upload.** There is no upload endpoint in this slice (instance endpoints: list, get, delete, active, resubmit,
  retract; model endpoints: read only). Size and content validation therefore apply only to the engine load path above.
- **Tenant isolation.** Instance repository filters TenantRegistryId on get, list, update, delete; model service reads
  through tenant-scoped repositories; allocation uses `EntityAnalysisModelParentGuard`. Cross-tenant tests exist (see
  matrix).
- **Maker-checker.** `AskJoobyAnalyticsInstanceRepository` derives from `VersionedApprovalRepository`.
  `EntityApprovalService.RefuseOwnChange` refuses maker self approve/reject; landlord is exempt (design, tested by
  `ALandlordMayApproveTheirOwnChangeAsync`). Approval history carried by retract/resubmit tests. Gap found: finding 1.
- **Rollback / promotion.** Service-only (agent tools), Promote Model permission, evidence audited, but not approval
  gated (design, finding 3).

## 2. Files reviewed

| File                                                                                                                                          | Read                                                                                                                                                                                |
|-----------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Jube.App/Endpoints/AskJoobyAnalytics/*.cs                                                                                                     | whole (runner, instance, model endpoints)                                                                                                                                           |
| Jube.Service/Repository/AskJoobyAnalyticsInstance/Service, Mapper, Validators                                                                 | service whole, mapper whole, validators whole                                                                                                                                       |
| Jube.Service/Repository/AskJoobyAnalyticsModel/Service                                                                                        | whole, mapper not read                                                                                                                                                              |
| Jube.Data/Repository/AskJoobyAnalyticsInstanceRepository.cs                                                                                   | lines 1-200                                                                                                                                                                         |
| Jube.Service/EntityApproval/EntityApprovalService.cs                                                                                          | 60-310                                                                                                                                                                              |
| Jube.Engine/.../SyncAskJoobyAnalyticsInstancesExtensions.cs                                                                                   | whole                                                                                                                                                                               |
| Jube.Engine/AdvancedAnalytics/Serialisation/AdvancedAnalyticsModelJson.cs                                                                     | whole                                                                                                                                                                               |
| Jube.App/Vue askJoobyAnalytics/api.ts, approval/api.ts, AnalyticsDiagram, Performance, Evidence, StatusBand (part), ServerChart, JubeChart (part) | read                                                                                                                                                                                |
| Not read in depth                                                                                                                             | AskJoobyAnalyticsInstances.vue, AnalyticsVersions.vue, AnalyticsComparison.vue, ApprovalPanel.vue body, AskJoobyAnalyticsModelCanvas.vue (grepped for physical-direction CSS: none) |

Pull-ins (`--closure`): DataGrid, JubeChart, useFormat, i18n/index, en.json, ProblemAlert are claimed by later slices;
inherited, not reviewed beyond JubeChart sanitiser (finding 5).

Lazy loading: ModelStory.vue wraps both components in `defineAsyncComponent`. The components use only props and their
own vue-query queries, no `window` or injected globals, so lazy loading is safe (read by grep, not run).

## 3. Findings

| # | Severity                | Where                                                   | What                                                                                                                                                                                                                                                                                                                                                               | Status                                                                                                                |
|---|-------------------------|---------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------|
| 1 | Medium bug              | AskJoobyAnalyticsInstanceRepository.UpdateAsync ~92     | Only an Active change snapshotted and bumped Version. ResponsePayload and ReportTable are read by the engine from the approved row (`BuildAdvancedAnalyticsModel`), so changing them on a live approved instance took effect at once with no checker and no version row.                                                                                           | Fixed: any of Active, ResponsePayload, ReportTable bumps the version. Rename stays ungated (pinned by existing test). |
| 2 | Low bug                 | AnalyticsDiagram/Performance, StatusBand, ApprovalPanel | Server narrative paragraphs, model name and approval facts rendered without bidi handling; keys on paragraph text could collide.                                                                                                                                                                                                                                   | Fixed: `dir="auto"`, `<bdi>`, index keys.                                                                             |
| 3 | Medium bug (was design) | AskJoobyAnalyticsModelService.ActivateAsync             | Evidence: `SyncAskJoobyAnalyticsInstancesExtensions` takes the instance through approved resolution but then scores with `trainedModel.ActiveVersionId` read live, and `SetActiveVersionAsync` writes no version or approval row, so promote and rollback changed live scoring for a tenant with no checker. The audit line records evidence but is not a checker. | Fixed in the follow-up (section 12).                                                                                  |
| 4 | Design (labelled)       | EntityApprovalService.RefuseOwnChange, StatusOf         | Landlord exempt from separation of duties. Intentional and tested (`ALandlordMayApproveTheirOwnChangeAsync`, `ALandlordMayRejectTheirOwnChangeAsync`). Intent is the question, so owner decision.                                                                                                                                                                  | Left.                                                                                                                 |
| 5 | Low (hardening, shared) | JubeChart.clean                                         | Keeps `<style>` and style attributes (external CSS fetch). Server SVG is trusted.                                                                                                                                                                                                                                                                                  | Shared TODO.                                                                                                          |
| 6 | Low                     | Instance Update/Allocate/SetActive                      | No optimistic concurrency check although DTO carries Version.                                                                                                                                                                                                                                                                                                      | Shared TODO.                                                                                                          |
| 7 | Info                    | endpoints                                               | No create, rename or allocate HTTP endpoint although the service has them (agent only).                                                                                                                                                                                                                                                                            | Info.                                                                                                                 |
| 8 | Info                    | Naming                                                  | AnalyticalMethodology docs name the library.                                                                                                                                                                                                                                                                                                                       | Shared TODO.                                                                                                          |

## 4. Fixes made

1. Finding 1 repo edit. Test added: `ChangingTheOutputFlagsOfAnApprovedInstanceIsHeldUntilACheckerApprovesItAsync` in
   `Jube.Tests/Service/EntityApproval/AskJoobyAnalyticsInstanceMakerCheckerTests.cs`;
   `docs/Testing/ServiceModelConfiguration.md` updated (9 to 10). Existing
   `UpdateChangesFieldsAndIncrementsVersionAsync` (version 2) remains valid.
2. Finding 2 Vue edits (formatted, eslint and type-check clean).
3. Doc naming: PerformanceTrace and InfrastructureHealthMetrics wording.
4. 21 resx files (section 6).

## 5. Test and pen-test matrix

| Layer                | Evidence (names from file listing, not all bodies read)                                                                                                                            | Status                    |
|----------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------|
| Instance service     | `AskJoobyAnalyticsInstanceServiceTests`, `AskJoobyAnalyticsInstanceActiveSwitchTests`                                                                                              | Present, bodies not read  |
| Maker-checker        | `AskJoobyAnalyticsInstanceMakerCheckerTests` (read), `EntityApprovalServiceTests` (landlord tests read), `ApprovalNoteAndHistoryTests`                                             | Covered; flags gap closed |
| Repositories         | `AskJoobyAnalyticsInstanceRepositoryTests` (read in part: tenant scoping, foreign model, deletion snapshots), `AskJoobyAnalyticsTrainedModelRepositoryTests`                       | Covered                   |
| Model service/mapper | `AskJoobyAnalyticsModelServiceTests`, `AskJoobyAnalyticsModelMapperTests`                                                                                                          | Names only                |
| Pen tests            | `PenTestAskJoobyAnalyticsInstance`, `PenTestAskJoobyAnalyticsInstanceActive`, `PenTestAskJoobyAnalyticsModel`, `PenTestEntityApproval`                                             | Names only, not read      |
| Serialisation        | `AdvancedAnalyticsModelJsonTests`                                                                                                                                                  | Names only                |
| Front end            | vitest `askJoobyAnalytics/__tests__/model.spec.ts`, `approval/__tests__/model.spec.ts` (run, 34 pass); Playwright `askJoobyAnalytics.spec.ts`, `approvalManager.spec.ts` (not run) | No component tests        |

Tests needing the owner's run: new `ChangingTheOutputFlags...` test; `AskJoobyAnalyticsInstanceMakerCheckerTests`,
`AskJoobyAnalyticsInstanceRepositoryTests`, `AskJoobyAnalyticsInstanceServiceTests`; `Category=PenTest` for the three
AskJooby classes.

## 6. Translations

SPA: en, pl, ar complete. ka, id, sv, ur, hi: `review/model-advanced-analytics.locale-proposals.json` now holds machine
translations of all 206 leaf keys of `analytics` and `approval` (generated by my own scripts in a per-slice scratch
folder; checked: key set equals en.json, `{placeholder}` sets and pipe plural counts equal English for every key; Urdu
wraps Latin terms in U+2068/U+2069). POC quality.

Server: created pl, ar, ka, id, sv, ur, hi for `AskJoobyAnalyticsInstanceResources` (12),
`AskJoobyAnalyticsModelResources` (10), `EntityApprovalResources` (4): 21 files (plus the new `InstanceIsLive` key
appended to the neutral and all seven `AskJoobyAnalyticsModelResources` files), generated by my own script in a
per-slice scratch folder, verified XML parses, key order equals neutral, every value differs from English, `{0}`
preserved. ar and ur isolate Latin terms with U+2068/2069 in a few messages. `.fr.resx` untouched.
`AdvancedAnalyticsTrainingResources` belongs to training services (not this slice, no locale files). Only the 21 files
were written under Jube.Resources by me.

## 7. Docs

New `docs/AdvancedAnalytics/index.md` (draft, 9 TODO markers: lines 11, 15, 19, 24, 30, 34, 41, 47, 53).
`docs/Concepts/MakerChecker/index.md` gained 4 markers (10, 185, 186, 203). Existing docs/AdvancedAnalytics pages
(Exhaustive*) belong to model-exhaustive. Screenshots owed: instances, versions, rtl, approval panel.

## 8. Shared-code TODOs

Five lines appended to `review/SHARED-TODO.md` (AnalyticalMethodology naming, JubeChart style hardening, Jube.Charting
RTL audit, slices.json keywords, instance concurrency check).

## 9. Manual test sheet

1. Permissions 48 (Analytics) only, 47 (Promote Model) only, both: list visible, mutating actions refused.
2. Create via agent, rename (no approval), activate (pending), second user approves; maker cannot approve own change;
   landlord can.
3. Toggle Response payload on an approved live instance: pending, engine keeps old flag until approved.
4. Delete with note, reject, resubmit, retract; history carries across versions.
5. Compare two versions, evidence tree, diagram, calibration and ROC.
6. ar and ur: paragraphs, model names, numbers; narrow width; dark theme; keyboard.
7. Open the Models story: sections load lazily without flicker.
8. Cross-tenant ids on every route return 404.

## 10. Commit pathspecs

`slice_audit.py model-advanced-analytics --paths` plus: Jube.Data/Repository/AskJoobyAnalyticsInstanceRepository.cs (one
hunk), the three tests hunks, 21 new resx, `docs/AdvancedAnalytics/index.md`, MakerChecker doc, the two API docs hunks,
`docs/Testing/ServiceModelConfiguration.md` hunk.

## 11. Not covered

No build or test run; Rider lint not run on any C# edit. Playwright not run. Rendering of the new locales and SVG charts
in a browser not seen. Not read in full: `AskJoobyAnalyticsModelMapper`, `AdvancedAnalyticsChartService` internals, the
other model kinds' `FromSnapshot` (only the Logistic path and the envelope validators were read), the bodies of the
three Service pen tests beyond their names and the Instance route list. Resx for `AdvancedAnalyticsTrainingResources`
(training slice).

## 12. Follow-up pass (owner policy, bugs fixed and gaps closed)

**Promote and rollback (finding 3).** Evidence above. Fix: `AskJoobyAnalyticsInstanceRepository.IsLiveAsync` (current
row Active, or approved resolution Active) and `AskJoobyAnalyticsModelService.ActivateAsync` refuses with
`ConflictException` reason `InstanceIsLive` (new resource key, neutral plus pl, ar, ka, id, sv, ur, hi; `.fr` untouched)
after the idempotent no-op return. The instance must be switched off (approval gated), changed, then switched on
(approval gated). Trade-off for the owner: no emergency rollback of a live instance without a checker. Limits: uses
`ApprovalsRequiredResolver.Default` because the service has no setting access (shared TODO). Test:
`PromotionAndRollbackAreRefusedWhileTheInstanceIsLiveAsync` in
`Jube.Tests/AdvancedAnalytics/Service/AskJoobyAnalyticsModelServiceTests.cs`. Existing promote tests use instances with
Active 0 (`NewInstanceAsync` default), so they are unaffected.
`PenTestAskJoobyAnalyticsModel.NoMutatingRouteExists_PromotionAndRollbackAreNotReachableOverHttpAsync` confirms promote
is agent only.

**Permission numbers.** My docs had 47 and 48 swapped: `AdvancedAnalyticsPermissionSpecifications` has Analytics = 48
and PromoteModel = 47. Docs fixed. Pen tests and service already use the right ones.

**Jube.Charting SVG audit.** Read `Theme/ChartCss.cs` and grepped the whole library for `text-anchor`, `direction`,
`unicode-bidi`, `dir=`, `font`. Findings: (a) no `direction` anywhere, and `.end` and `.ctr` use `text-anchor:end` and
`middle`; an inline SVG inherits `direction:rtl` from an Arabic or Urdu page and SVG `start` and `end` follow the inline
direction, so axis labels anchored `end` would sit on the wrong side and overlap the plot. FIXED: body rule and `.jc`
rule now carry `direction:ltr` (no change in left-to-right pages). (b) font stack was system-only with no script
fallbacks; extended with Noto Sans, Noto Sans Arabic, Noto Nastaliq Urdu, Noto Sans Devanagari, Noto Sans Georgian. (c)
Fonts are NOT bundled: the SPA ships IBM Plex (Latin) only (`Jube.App/Vue/src/assets/main.css`), so ar, ur, hi and ka depend
on system or installed Noto fonts (shared TODO to shell-auth). (d) Clipping for Nastaliq line heights and long Georgian
or Swedish labels in fixed-size SVG boxes could not be verified without rendering. Because exact-string goldens pin the
CSS, the same two string replacements were applied to 140 golden files (diagram SVG goldens,
`Jube.Tests/Charting/External/Golden/jube-charting.css`, `Jube.App/wwwroot/css/jube-charting.css`) instead of
regenerating; the Rag msgpack that also contains the font stack was left alone. Test added:
`ChartsAreLeftToRightSoTextAnchorsDoNotFlipInARightToLeftPage` in `ChartStylesheetTests` (catalogue updated, 12 to 13).
Other slices using Jube.Charting are affected only by the golden hash: shared TODO. Tests to run: all
`Jube.Test.Charting` goldens (`ChartStylesheetTests`, `ChartGoldenTests`, `DiagramGoldenTests`, `ExternalGoldenTests`)
and `PenTestExhaustiveChartStylesheet`; if a golden differs, regenerate with `CHARTING_EXTERNAL_GOLDEN_GENERATE_DIR`.

**JubeChart sanitiser (finding 5)** left as hardening, shared TODO.

**Vue bodies.** `AnalyticsVersions.vue` builds cells with `createElement` and `textContent` only,
`AskJoobyAnalyticsInstances.vue` uses `setAttribute('aria-label', ...)`, `ApprovalPanel.vue` (lines 1-120 read, rest
grepped): no `v-html`, `innerHTML`, `eval` in any slice file. No physical left/right classes in any slice component.

**Pen tests read by routes and names plus assertions on permission numbers:** `PenTestAskJoobyAnalyticsInstance` (auth
matrix, delete with 47 versus 48, cross-tenant ids equal missing ids, wrong verbs, hostile stored names returned as JSON
data), `...InstanceActive` (matrix, unallocated cannot switch on, cross-tenant, mass assignment, malformed bodies,
concurrent switches), `...Model` (read matrix, no mutating route, cross-tenant on every read, payload never carries
ModelJson or the tenant column, compare refuses nonsense). Coverage of the slice's HTTP surface is complete; no gap
found.

**Calibrated recall and inference limits (read, no defect).** Load: `LogisticRegressionRecall.FromEnvelope` calls the
envelope and snapshot validators: array lengths against input count, finite values, non-negative deviations, distinct
in-range indicator inputs, priors strictly in (0,1), calibrator method `Platt`, four finite parameters, mode 0 or 1,
epsilon in (0,0.5), raw score only (`AdvancedAnalyticsLogisticRegressionPipelineSnapshot.Validate`,
`Calibrator.FromPrimitives`). Tests exist in `AdvancedAnalyticsModelJsonTests` (type marker refused, length mismatch).
Inference: `AdvancedAnalyticsAdaptationExtensions` takes a stage time budget
(`AdvancedAnalyticsSettings.ResolveStageTimeBudgetMilliseconds`), checks it before each model and before each variable,
allocates `double[variables]` from the model (bounded by the 16M character JSON cap), catches and logs per-model errors,
and reads models from the per-Jube-model snapshot (`ModelSnapshot.AdvancedAnalyticsModels`), so there is no cross-tenant
cache. Residual risk (design): `model.Recall(data)` itself is not interruptible, so one very large model can exceed the
budget by its own run time; there is no cap on model count or variable count beyond the JSON size. Sync reparses each
promoted version's JSON on every cycle (cost, not correctness).

Files written by scripts in this pass: only the 21 resx (earlier pass), the locale proposals JSON, and the 140 chart
golden files by exact replacement; nothing else.
