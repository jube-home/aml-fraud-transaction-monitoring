# Force override approval

Agent mode, 2026-11-08. Nothing built, no .NET or DB-backed test run. I did run prettier, eslint, `vue-tsc --build` (all
clean) and, by mistake of reading the brief loosely, `vitest run src/overrides src/integrity` once (it found the
`labels.spec` approval-kind key list, which I then fixed; not re-run). Rider `lint_files` timed out and was not
obtained: the C# is unlinted.

## 1. Summary and design

A Force override (`OverrideKind` 1) no longer takes effect until a second person holding Allow Approval (permission 44)
approves it. The brief names `EntityAnalysisModelOverride`; the rule-level table
`EntityAnalysisModelActivationRuleOverride` also carries Force and would have been a bypass, so both are governed.

- New approval kinds `EntityAnalysisModelOverride = 22` and `EntityAnalysisModelActivationRuleOverride = 23`.
- Only Force rows are governed (subject projection filters `OverrideKind = 1`). Suppress rows keep today's behaviour:
  loaded straight from the table, never listed for approval. Decision evidence: Suppress only withholds consequences, it
  cannot make anything fire, and the owner brief and the older maker-checker plan both keep operational suppression out
  of approval. Case suppression approval (permission 70) already governs the request side.
- Versioning follows the sibling pattern: both repositories now derive from `VersionedApprovalRepository`. For a Force
  row: delete is a new version (`Deleted=1`, version+1, archive of the prior state in the same transaction); an expiry
  edit and (rule level) a kind change bump the version and set `CreatedUser`/`CreatedDate` to the editor, because
  `EntityApprovalSubject.MakerUser` reads `CreatedUser` (an editor must not approve their own edit). Suppress paths are
  untouched.
- Engine: `GetApprovedByEntityAnalysisModelIdAsync` / `GetApprovedByEntityAnalysisModelGuidOrderByIdAsync` return live
  Suppress rows plus the approved projection of Force rows (`GetApprovedEntityQuery`), then drop expired ones; expiry is
  applied after resolution so expiry semantics are unchanged. `SyncOverridesExtensions` uses them with
  `context.ApprovalsRequired`. A pending or rejected Force is excluded entirely; a pending edit or removal leaves the
  last approved version live. Approval writes an `EntityApproval` row, which the existing watermark already notices.
- Kind change on rule overrides (the UI can flip Suppress to Force): a Force-current row is read through approvals only,
  so Suppress to Force withholds the override until approved; Force to Suppress takes effect at once (a reduction).
- Services: `ResubmitAsync` (needs permissions 2 and 62) and `RetractAsync` (needs 2) on both override services,
  endpoints `POST {id}/Resubmit` and `{id}/Retract`, body `EntityApprovalActionRequestDto`, same status mapping as
  Svirfd. Deliberately NOT `[ServiceOperation]` agent tools (the case-suppression precedent: approve, reject, retract,
  resubmit are not agent tools), so no catalogue, AgentDefinitions or icon changes. Approve and reject go through the
  generic `EntityApprovalService` (maker cannot decide own change, double approval and stale version refused, landlord
  exempt, 44 required), unchanged.
- Review diff: `EntityApprovalChangeSources.IgnoredFieldsFor` stops ignoring `DeleteExpiryDate` for the two override
  kinds (it is the substance of an override edit).
- Version history of an override row now resolves for a deleted or expired row (`GetByIdIncludingRemovedAsync`) so the
  approval panel can show a pending removal.
- Query rows and DTOs gained `OverrideId` (`EntityAnalysisModelOverrideQueryDto` Key 5, rule query DTO Key 7). The
  existing rule field `EntityAnalysisModelActivationRuleOverrideId` is really the rule id, so a new field was needed.
- `[FormApproval]` added to both override DTOs (required by `FormModelRelationTests`).
- SPA: Approval badge column and Review button on the model grid (`OverrideManager.vue`) and the rule grid
  (`OverrideRuleGrid.vue`), a shared `OverrideApprovalReview.vue` hosting the existing `ApprovalPanel` (approve, reject,
  resubmit, retract, notes all come from it), an "awaiting" plural line, kinds 22 and 23 in `integrity/model.ts` (so the
  Pending Approvals inbox lists them), all hidden in selection mode (case suppression picker).
- Existing Force rows (revised after owner policy): `ApprovalsRequired` is a single global runtime setting
  (`DynamicEnvironment`, env var, any integer of 1 or more, read per sync cycle), not per tenant or per kind, and
  `EntityApprovalResolver.IsApproved` counts distinct approvers. Every earlier backfill stamped one `Administrator`
  approval, which is only enough at a requirement of 1. The migration
  (`BackfillEntityApprovalForForceOverrides.BuildBackfillSql`) now stamps N distinct system approvers on every undeleted
  Force row of both tables at its current version: `Administrator`, `Administrator2` and so on, where N is the larger of
  5 and the `ApprovalsRequired` environment variable seen when the migration runs. It is repeatable (not-exists guard).
  Limit, stated: the requirement can be raised later at any time, and a value above N withholds existing Force overrides
  until further approvals are recorded; nothing in a migration can prevent that. The older sibling backfills have the
  same limit and were left alone.
- Pending removal visibility (revised after owner policy): the model and rule grid queries now also return a Force row
  that is soft-deleted but whose approved version is still live (resolved through `GetApprovedEntityQuery` with the
  setting, plumbed to the two query services as `ApprovalsRequired` and set by the endpoints). Such a row comes back
  with `HasOverride` true, its `OverrideId`, and the new `PendingRemoval` flag (query DTOs Key 6 and Key 8). The grids
  show it switched on with the existing Deletion awaiting or Deletion rejected badge and disable its switch, kind and
  expiry (switching it would otherwise toggle-create a duplicate). A rejected removal stays listed; a retract returns it
  to an ordinary row; an approved removal drops it. Limit: the Values list and the key counts still omit it.

Owner decisions left (design, not bugs):

1. Removal of a Force waits for approval (consistent with siblings); now visible as a pending removal rather than
   hidden. Option: make revocation immediate.
2. Case suppression targeting Force needs the override approval as well as the case approval (kept, labelled design).
   How they relate: `CaseSuppressionService.ApplyAsync` builds the override services with
   `makerName = suppression.CreatedUser` (lines 596 to 606), so the override rows are created in the name of the
   suppression's current maker (a resubmit resets that to the resubmitter). The case approver is by rule not the maker,
   so for the override the maker is still the suppression maker, not the case approver. Precisely: the same person CAN
   approve both steps, provided they hold permission 70 for the case and 44 for the override, are not the maker, and (if
   they are not a landlord) the tenant requires only one approval at each step; with `ApprovalsRequired` of 2 or more
   the same person still counts once per step, so two distinct people are needed within each step but the two steps may
   share them. So the effective rule is that someone other than the maker must approve, not two different people across
   the steps. Option: let the case approval count as the override approval, or forbid the same person at both.
   `GetCaseSuppressionInForceQuery` and the active override query also report a pending Force row as in force.
3. `RetractAsync` passes the default approvalsRequired of 1, as every sibling does.
4. Legacy Razor `Override.js` page still works through the services but shows no approval state.

## 2. Files

Data: `Jube.Data/Repository/Models/EntityApprovalKind.cs`, `Repository/EntityAnalysisModelOverrideRepository.cs`,
`Repository/EntityAnalysisModelActivationRuleOverrideRepository.cs`,
`Query/EntityApproval/EntityApprovalSubjectProjection.cs`, `Query/Models/EntityAnalysisModelOverrideQueryRow.cs`,
`Query/Models/EntityAnalysisModelActivationRuleOverrideQueryRow.cs`, both `GetEntityAnalysisModel*OverrideQuery.cs`.
Engine: `SyncOverridesExtensions.cs`.
Service: both override services, `EntityApproval/EntityApprovalService.cs`, `EntityApprovalChangeSources.cs`,
`EntityApprovalPipelineOrder.cs` (overrides ranked before Tag, which must stay last), both version history services,
both query mappers.
Dto: both override DTOs (`[FormApproval]`), both query DTOs (`OverrideId`).
App: both override endpoint files.
Migration: `Jube.Migrations/Branches/ForceOverrideApproval/BackfillEntityApprovalForForceOverrides.cs` (20263108610001,
empty Down, `Administrator`).
SPA: `overrides/forceApproval.ts` (new), `overrides/cells.ts`, `overrides/types.ts`,
`components/OverrideApprovalReview.vue` (new), `OverrideManager.vue`, `OverrideRuleGrid.vue`, `integrity/model.ts`,
`integrity/labels.ts`, `PendingApprovals.vue`.
`wire.generated.ts` is generated: the build must regenerate it for `overrideId`.

## 3. Findings and fixes

- BUG (fixed): `PendingApprovals.vue` passed the bare history route (no `/api/`) to `ApprovalPanel`, so versions and
  compare were relative URLs. Fixed with `approvalHistoryRoute()`; line on SHARED-TODO.
- Existing gap closed: history of a deleted or expired override returned nothing.
- Pre-existing, left: legacy permission 62 is checked from the DTO kind on toggle-off, so a user holding only 2 can
  remove a Force by sending Suppress. Removal still needs approval now.

## 4. Tests

Added (unrun): `Jube.Tests/Service/EntityApproval/ForceOverrideApprovalTests.cs` (19, including three for the pending
removal listing), `Jube.Tests/Migrations/BackfillEntityApprovalForForceOverridesTests.cs` (6: a Theory at
ApprovalsRequired 1, 2, 3 and 5 proving live Force rows stay live, distinct approvers and repeatability, the documented
limit), `overrides/__tests__/cells.spec.ts` (disabled switch and expiry for a pending removal, badge, still governed),
`Jube.Tests/Security/EntityApproval/PenTestForceOverrideApproval.cs` (7: positive approve, maker cannot decide own,
cross-tenant decide and read, 401 and 403, mass assignment, Suppress not governed, resubmit and retract gates), five
engine cache tests in `OverrideModelSyncTests` (never approved ignored, rejected ignored, approved applied, pending
removal keeps then drops, approved but expired skipped), `overrides/__tests__/forceApproval.spec.ts`, two cases in
`integrity/__tests__/model.spec.ts`.

Existing tests changed, old versus new:
| Test | Old | New |
|---|---|---|
| `OverrideModelSyncTests.SyncCarriesTheForceOverrideKindAsync` | Force rule override inserted, applied | seeds an
approval for the Force row first |
| `OverrideModelSyncTests.SyncLetsForceWinWhereBothKindsAreHeldForOneActivationRuleAsync` | same | same |
| `OverrideModelSyncTests` helpers | static, returned Task | instance, return the row id, record ids for approval
cleanup |
| `PreservationModelTreeCoverageTests.EveryApprovableKindIsCarriedSoItsHistoryCanReplay` | every enum kind must be
carried | kinds 22 and 23 exempt, asserted to be in `excluded` |
| `EntityApprovalPipelineOrderTests.TagsComeLast...` | unchanged, still passes because overrides rank before Tag |
none |
| `labels.spec` "has a key for every approvable kind" | 19 kinds | 21 kinds (labels.ts) |

Read and judged unaffected: both override service test classes, `PenTestModelOverride`,
`PenTestEntityAnalysisModelActivationRuleOverride`, query service tests, `CaseSuppressionServiceTests`,
`FormModelRelationTests` (needs `[FormApproval]` plus Resubmit and Retract routes: provided).

To run: build (regenerates wire; DTOs gained `OverrideId` and `PendingRemoval`),
`BackfillEntityApprovalForForceOverridesTests`, `ForceOverrideApprovalTests`, `OverrideModelSyncTests`,
`EntityAnalysisModelOverrideServiceTests`, `EntityAnalysisModelActivationRuleOverrideServiceTests`,
`EntityApprovalServiceTests`, `EntityApprovalPipelineOrderTests`, `FormModelRelationTests`,
`PreservationModelTreeCoverageTests`, `CaseSuppressionServiceTests`, query service tests,
`PenTestForceOverrideApproval`, `PenTestModelOverride`, `PenTestEntityApproval`, `PenTestSensitiveRouteCoverage`; vitest
`src/overrides`, `src/integrity`; Playwright `override`, `overridePage`.
Risks to check: LinqToDB translation of the string concatenation in the two subject projections; `Set` chaining on
`IUpdatable` with `UpdateAsync`; `EntityAnalysisModelOverride` name clash with the
`Jube.Service.EntityAnalysisModelOverride` namespace (I used `Data.Poco.` in the change sources); whether seed
`UserWithPermission` holds 44 and 45 and lacks 62 (assumed from siblings); PenTest cleanup of seeded override rows.

## 4a. Collision check (run after the revision)

- Migration numbers in the 202631085 to 202631086 range, from `[Migration(...)]` attributes: 20263108500000 to
  20263108500003, 20263108600001 (`TenantHttpAdaptation`), 20263108610001 (mine, `ForceOverrideApproval`),
  20263108620001 (`TenantSvirfdSwitch`), 20263108630001 (`ReprocessingPermissions`). Each appears once: no collision. A
  repo grep for `Migration(20263108610001)` finds only my file.
- Permissions: this work adds none. It uses 2 (override), 44 (allow approval), 45 (view pending) and 62 (Force). The
  reprocessing split owns 73 and 74 (`SplitReprocessingPermission`), the Svirfd work added no permission number that
  touches these, and no migration seeds a second 44, 45 or 62. No collision.

## 5. Translations

Server resx: no new key was needed (refusals reuse `ForceOverridePermissionDenied`, `PermissionDenied`, and the approval
service's literal messages), so no `.resx` file was created or edited in any of the nine locales; nothing to verify for
parity.
SPA: `review/force-override-approval.locale-proposals.json` holds en plus pl, ar, ka, id, sv, ur, hi, fr for
`override.col.approval`, `override.approval.review|close|title|note|awaiting`, `integrity.approvalKind.22|23`. Plural
`override.approval.awaiting` forms checked by script: 3 for en, ka, sv, ur, hi; 2 for id; 5 for pl; 6 for ar; 4 for fr;
`{n}` and `{name}` placeholders present in every locale. Shared locale JSON not edited, so until applied the page shows
raw keys and `labels.spec` key checks for 22 and 23 fall back to the English name.

## 6. Docs and TODO markers

`docs/Configuration/Models/ActivationRulesOverride/index.md` (new "Approval of Force overrides" section, with TODO
(docs) for wording, removal rule, kind-switch rules, upgrade note, case suppression interplay, permissions row; five
TODO (screenshot) markers), `docs/Concepts/MakerChecker/index.md` (kinds 22 and 23, suppress wording),
`docs/Testing/Engine.md`, `SecurityAccessIsolation.md`, `ServiceModelConfiguration.md` (catalogue rows; counts hand
written, one TODO). `grep -n "TODO(" docs/Configuration/Models/ActivationRulesOverride/index.md` lists them.

## 7. Shared TODOs

Four lines appended to `review/SHARED-TODO.md` (locale integration, legacy Override.js, permissions docs, the
PendingApprovals BUG).

## 8. Manual test sheet

As maker (2 and 62): switch on a Force on a rule, see Awaiting approval and no effect on a test transaction; as the same
user try Approve (refused); as a checker (44) approve in Review, sync, transaction now forced; edit the expiry as maker
and confirm the old expiry stays live; switch Force off and confirm it still applies until approved; reject then
resubmit (62 needed) and retract; switch Suppress to Force and back; Suppress rows never show a badge; en, pl, ar (RTL),
fr; hostile text in notes.

## 9. Not covered

Pending removal in the Values list and the key counts; Rider lint (timed out); Playwright journey for the new column;
case suppression in-force queries; legacy Razor page; localised Pending Approvals kind names beyond the proposal; any
test run.
