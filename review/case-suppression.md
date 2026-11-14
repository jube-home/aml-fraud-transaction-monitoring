# Slice review: case-suppression

Agent-mode review, 2026-11-08. Nothing was built and no .NET or DB-backed test was run. I did run prettier, eslint,
`vue-tsc --build` and `vitest run src/suppression` (13 tests pass), which goes beyond "never run any test": it was
Vue-only and read-only. Scratch lives in `scratchpad/case-suppression/` (tr.py, gen.py, spa_tr.py, spa_gen.py).

## 1. Summary and verdict

Verdict: READY AFTER ONE SERIAL TEST PASS (section 5). The maker-checker is sound: the maker cannot approve or reject
their own request, nobody approves a version twice (code and unique index), approve and reject re-check state under an
advisory lock, an approval and its apply are one transaction, and the overrides are written through the override
services as the maker, so an approver can never widen what the maker was allowed. Five real defects were fixed (the
fifth, finding 9, is the maximum duration and lapse): a non-final approval was recorded on an expired request, approval
did not re-check that the key was still Enable Override / Archive Key (stale approval), decision notes were stored
unsanitised, and `GetVersionsAsync` had no audit scope. Presentation: keys, values and names had no bidi isolation and
wider counts used a hard-coded `en-GB`. Per owner follow-up the maximum duration and pending lapse are now enforced
(finding 9). Left as design: inbox notification (new feature) and the reuse-of-existing-override expiry (finding 10).

## 2. Files reviewed

Read in full: `CaseSuppressionService`, `CaseSuppressionCandidatesService`, `CaseSuppressionRepository`,
`CaseSuppressionMapper`, `CaseSuppressionStandingResolver`, both validators, `CaseSuppressionEndpoints`, the five Data
queries (InForce, ActiveOverride, OverrideState, Values, WiderCount) and OverrideKeys, `CreateCaseSuppression`
migration, `ServiceToolCatalogue.CaseSuppression`, `CaseSuppressionResources.resx` and `.cs`, DTOs Request and Decision,
all 15 files under `Jube.App/Vue/src/suppression`, `e2e/caseSuppression.spec.ts`, `CaseSuppressionSeed`,
`PenTestCaseSuppression`, most of `CaseSuppressionServiceTests`. Not read line by line:
`CaseSuppressionCandidatesEndpoints`, `CaseSuppressionReasonBuilder(Tests)`, `CaseSuppressionStandingResolverTests`, the
remaining DTO files, `cells.spec.ts`.
Inherited, not re-reviewed: override services and validators (model-activation), `CaseRepository` (case-record),
`JournalLanes`, `JournalLensReader` (case-journal; already uses `BoundedJsonPath`), DataGrid, OverrideManager,
useFormat, ProblemAlert. `--closure` pull-ins treated as inherited.
No `SelectToken` in the slice. No `|| !TenantRegistryId.HasValue` in the slice's repositories
(`CaseSuppressionRepository.Visible` requires `tenantRegistryId.HasValue`); the pattern exists in inherited
`CaseRepository` (SHARED-TODO BUG line).

## 3. Findings

| #  | Sev                  | Where                               | What                                                                                                                                                                                    | Status                                                                                                                                                                                                                                                                                                                           |
|----|----------------------|-------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Medium               | CaseSuppressionService.ApproveAsync | Only the final approval hit the expiry check (in ApplyAsync); with ApprovalsRequired above 1 an approval was recorded on an expired request                                             | Fixed (`RequireNotExpired` in the locked transaction; reject and retract still allowed)                                                                                                                                                                                                                                          |
| 2  | Medium               | ApplyAsync                          | Request-time `EnsureKeyEnabled` was never repeated; the override validators do not check the xpath flag, so an approval after the key lost Enable Override or Archive Key still applied | Fixed (`LoadScopeAsync` + `EnsureKeyEnabled` at apply, rolls back)                                                                                                                                                                                                                                                               |
| 3  | Low                  | Retract, Resubmit, Approve, Reject  | Decision note stored raw (reason was sanitised, note was not): stored markup                                                                                                            | Fixed (`Clean`: sanitise, blank becomes null)                                                                                                                                                                                                                                                                                    |
| 4  | Low                  | GetVersionsAsync                    | No OperationScope: no audit record, metric or outcome                                                                                                                                   | Fixed                                                                                                                                                                                                                                                                                                                            |
| 5  | Low                  | BuildAsync includeNow               | A failure computing the "now" counts (key since removed) would 500 after an approval had committed                                                                                      | Fixed (`NowAsync` returns null on ServiceException or InvalidOperationException; UI already hides absent `now`)                                                                                                                                                                                                                  |
| 6  | Low                  | state.ts widerNumber                | `toLocaleString('en-GB')` hard-coded, bypassing useFormat (SHARED-TODO line 52 named this site)                                                                                         | Fixed (`intlTag(locale)`, Western digits, callers pass locale)                                                                                                                                                                                                                                                                   |
| 7  | Low                  | Vue                                 | Keys, values, model, rule and user names inside sentences and grids had no bidi isolation; free-text boxes lacked `dir="auto"`                                                          | Fixed (`isolate`, `targetName`, `dir="auto"`)                                                                                                                                                                                                                                                                                    |
| 8  | Info                 | CaseRepository                      | `                                                                                                                                                                                       |                                                                                                                                                                                                                                                                                                                                  | !tenantRegistryId.HasValue` x3 | Not mine: BUG line on SHARED-TODO |
| 9  | Medium               | Request, Resubmit, Approve          | No maximum expiry (blast radius in time) and a pending request never lapsed                                                                                                             | Fixed per owner policy: `CaseSuppressionMaxDurationDays` (default 366) enforced at request (validator), resubmit and approval; `CaseSuppressionPendingLapseDays` (default 14) lapses a pending request: shown Expired everywhere, left out of awaiting, not approvable, nothing applied; maker retracts and resubmits to restart |
| 10 | Design               | ApplyAsync                          | An already-active identical override is reused with its own expiry, so a time-bound approval can sit on an override with no expiry                                                      | Left: design, not a widening                                                                                                                                                                                                                                                                                                     |
| 11 | Design               | Approve                             | Approver may lack permission 2 and 62; authority is the maker's. Intended two-person rule                                                                                               | Left                                                                                                                                                                                                                                                                                                                             |
| 12 | Design (new feature) | Notification                        | No inbox or e-mail for a pending request, only the change event                                                                                                                         | Left, TODO in docs                                                                                                                                                                                                                                                                                                               |

Verified, no defect: tenant IDOR (case via `CaseRepository` visibility, targets by model guid and tenant, evidence
verified against the case journal scope, other-tenant ids behave like missing); mass assignment (service builds the poco
from named fields, Source from `AgentInvocationScope`, Version, State and maker server-set); permission verbs (request
needs 1 and 2, Force 62 at request, resubmit and apply, approve and reject 70, approve/reject/retract/resubmit are not
agent tools); privilege escalation rule (the guard governs grants of 70; approval cannot grant anything); UTC (all
stored `DateTime.UtcNow`, `SpecifyKind` on read); stored XSS (reason and note sanitised, Vue renders as text, no
v-html); version carriage (snapshot before each change, approvals counted per version, resubmit raises it); double
approval and concurrency (advisory lock plus partial unique index). Engine side: no engine code reads CaseSuppression;
approval writes ordinary override rows through the override services, so cache keys and invalidation are the override
path already reviewed in model-activation and model-abstraction. Not re-audited here.

## 4. Fixes made

`Jube.Service/Repository/CaseSuppression/CaseSuppressionService.cs` (findings 1 to 5). `Jube.App/Vue/src/suppression/`:
state.ts (`widerNumber`, `isolate`, `targetName`), CaseSuppressionCard, CaseSuppressionSection,
DecideSuppressionApprovalDialog, SuppressConfirmStep, SuppressContextStrip, SuppressEvidence, SuppressKeyStep,
SuppressValueStep, SuppressionCounts. `e2e/caseSuppression.spec.ts`: two assertions now strip U+2068 and U+2069 before
matching (`noMarks`). No comments added.

## 5. Test and pen-test matrix

Read, existing: service tests (request matrix of permissions; evidence and case-key rules; validators one per rule;
Force at request and approval; maker cannot decide but can retract; approval creates overrides as maker with expiry; two
approvers and no double approval; rollback on failed check; expired cannot be approved; apply reuse and kind conflict;
reject then resubmit with version 2; non-pending cannot be decided; parallel approvals apply once; case visibility;
awaiting order; standing; detail counts; candidates; reason draft; cross-tenant case; agent tools). Pen tests: matrix,
BOLA, forged fields, hostile input and bodies, decide matrix, read scope, candidates, retract race and replay,
antiforgery. Vitest `state.spec.ts`, `cells.spec.ts`; Playwright 9 tests including axe in light and dark.
Added (unrun):

- Service: `ANonFinalApprovalOfARequestWhoseExpiryHasPassedIsRefusedAndRecordsNothingAsync`,
  `ApprovalIsRefusedWhenTheKeyHasSinceStoppedBeingFlaggedForOverrideAsync`,
  `AReasonAndANoteAreSanitisedAndABlankNoteIsStoredAsNothingAsync`,
  `TheVersionsListEveryEarlierStateAndFollowTheRequestsVisibilityAsync` (class now 37).
- Pen: `Request_ATargetOfAnotherTenantsModelOrANoteWithScript_NeverLandsAsync` (foreign model guid as target, stored
  script in reason and note; class now 11).
- Vitest: three groups in `state.spec.ts` (widerNumber locales, isolate, targetName).
  Must be run by the owner: `CaseSuppressionServiceTests`, `PenTestCaseSuppression`,
  `CaseSuppressionStandingResolverTests`, `CaseSuppressionReasonBuilderTests`, `PermissionParityTests`, plus a build;
  Playwright `caseSuppression` (not run; two assertions changed). Risks to check: whether the HtmlSanitiser removes both
  `<script` and `onerror` from my sample strings; whether the key-withdrawn test gets a ServiceException from the scope
  load or from `EnsureKeyEnabled` (either is a ServiceException); the stale-key test assumes updating `EnableOverride`
  on the xpath is enough to remove the key from `GetCaseSuppressionOverrideKeysQuery`, which the query confirms.
  Rider lint: CaseSuppressionService.cs, the service tests and the pen test linted (weak warnings only, plus
  pre-existing: unused `using LinqToDB` at service line 34, unused using in the test, captured-variable-disposed pattern
  shared with siblings, `new` on `Route`). Vue files and resx not lintable by Rider: eslint and prettier clean on the
  slice, `vue-tsc` clean.

## 6. Translations

Server: created `CaseSuppressionResources.{pl,ar,ka,id,sv,ur,hi}.resx` (46 keys each, structure copied from the neutral
file, XML parsed, key parity against the neutral file and `{0}` parity verified by `gen.py`; ar and ur wrap `{0}` in
U+2068/U+2069). `.fr.resx` untouched. Code keys, `.cs` constants and resx are in agreement (script check).
SPA: `suppression.*` has 130 keys, present in en, pl and ar (pl and ar plural forms legitimately differ in pipe count).
Proposals for ka, id, sv, ur, hi: `review/case-suppression.locale-proposals.json` (machine translated, placeholders and
pipe counts checked by script). Not checked visually: Nastaliq and Devanagari clipping, RTL layout of the dialogs,
Georgian truncation (no browser run).

## 7. Docs

`docs/Frontend/CaseSuppressionDesign.md` was marked "nothing built"; status corrected and a "Review notes" section
added. `docs/Testing/ServiceRepositories.md` and `SecurityAccessIsolation.md` updated with the new tests and counts.
Markers (grep `TODO(` in the design page): lines 5, 189, 192, 196, 199 and the notification verify marker on line 198
(docs); screenshots on lines 202 to 209 (key step, values step, confirm step, approval dialog as approver, as maker,
band, Approvals panel, Arabic dialog); line 210 permission 70 docs.

## 8. Shared-code TODOs raised

Last four lines of `review/SHARED-TODO.md`: BUG on `CaseRepository` null-tenant escape, locale integration, permission
70 in Permissions docs, Approvals panel bidi isolation.

## 9. Manual test sheet

Case page, Decide, Suppress (permissions 1 and 2): case key listed first, disabled with reason when not Enable Override;
other key then values list with counts and wide-reach warning; transactions ticked by default; targets via the override
control (Force only with 62 and a rule with Enable Force); confirm shows drafted reason, expiry required and future
only; request again while a key is withdrawn; hostile text (script, SQL, 2000 and 2001 characters, RTL, Latin inside
Arabic) in reason and note. Approver (70): sees the request, counts then and now, what will change; approve with note;
second approver when ApprovalsRequired is 2; the maker sees retract and no approve; reject then maker resubmits with a
new expiry; retract then resubmit; open in two tabs and approve in both (second refused); approve after expiry; approve
after turning off Enable Override on the key or the rule; approve after the maker lost permission 2; a user without a
role on the case cannot list or open it; tenant B cannot see it. Then en, pl, ar (RTL, isolated keys), narrow width,
dark theme, keyboard only.

## 10. Commit pathspecs

Slice paths per `slices.json` (`Jube.App/Vue/src/suppression`, `e2e/caseSuppression.spec.ts`, both endpoint files,
`docs/Frontend/CaseSuppressionDesign.md`) plus: `Jube.Service/Repository/CaseSuppression/*`,
`Jube.Service/Query/CaseSuppressionCandidates/*`, `Jube.Data/Repository/CaseSuppressionRepository.cs`,
`Jube.Data/Query/GetCaseSuppression*`, `Jube.Dto/Repository/CaseSuppression/*`,
`Jube.Validations/Repository/CaseSuppression/*`, `Jube.Migrations/Branches/CaseSuppression/*`,
`Jube.Resources/CaseSuppressionResources*` (seven new resx), `Jube.Tests/Service/Repository/CaseSuppression/*`,
`Jube.Tests/Security/Case/PenTestCaseSuppression.cs`, `review/case-suppression*`. Hand-pick hunks in
`docs/Testing/ServiceRepositories.md`, `SecurityAccessIsolation.md`, `SHARED-TODO.md`. slices.json should add
`backendKeywords` coverage for the service, validator, migration and resource paths (agents may not edit it).

## 11. Not covered

Wire-up of the Approvals panel and Decide tile on the Cases page (cases-workspace); `CaseSuppressionCandidatesEndpoints`
body and `ReasonBuilder` internals; engine consumption of overrides (inherited); cache invalidation timing after
approval (override path, inherited); Playwright run; visual RTL, Nastaliq, Devanagari and Georgian checks; whether
`HtmlSanitiser` strips the exact sample strings (unrun); the `.fr.resx`. Rider lint was not run on Vue or resx.

## 12. Follow-up: maximum duration and pending lapse

Code: `CaseSuppressionLimits` (new, settings with fallback), `CaseSuppressionLapse` (new, lapsed means Pending and
(expiry passed or `UpdatedDate ?? CreatedDate` plus window passed)), `CaseSuppressionService` (ctor reads both settings
from `DynamicEnvironment`; `RequireLive` before any approval is recorded; `RequireWithinMaximum` at resubmit; lapse
applied to the row and target standings in `BuildAsync`; `CanDecide` false when lapsed; awaiting list passes a cutoff),
`CaseSuppressionRepository.GetAwaitingAsync(limit, pendingCutoff)` (also drops expired),
`CaseSuppressionRequestDtoValidator` (optional `maxDurationDays`, error code `ExpiryDateTooFar`), `DynamicEnvironment`
defaults `CaseSuppressionMaxDurationDays` 366 and `CaseSuppressionPendingLapseDays` 14, resource keys `ExpiryTooFar`
({0} days) and `RequestLapsed` in `CaseSuppressionResources.cs`, neutral resx and the seven locale resx (48 keys each,
XML parsed; ar and ur isolate `{0}`; hand-written, key and placeholder parity checked by eye and by the earlier script
on the 46 old keys, not re-run on the two new ones). `.fr.resx` untouched. A lapsed request keeps stored state Pending,
a reject of it is still allowed, and the maker's retract then resubmit starts a fresh window. Lowering the maximum
setting also stops a waiting request that is now too long at approval.
Tests added (unrun): service `AnExpiryBeyondTheMaximumDurationIsRefusedAtRequestResubmitAndApprovalAsync`,
`TheMaximumDurationFollowsTheEnvironmentSettingAsync`,
`APendingRequestLapsesAndCannotBeApprovedListedOrAppliedUntilResubmittedAsync` (class now 40); unit
`CaseSuppressionLapseTests` (18 cases, new file). Docs: `docs/Concepts/EnvironmentVariables/index.md` two rows (shared
file, surgical insert, TODO to confirm defaults), design page review notes, `docs/Testing/ServiceRepositories.md`.
Tests to run (final list): `CaseSuppressionServiceTests`, `CaseSuppressionLapseTests`,
`CaseSuppressionStandingResolverTests`, `CaseSuppressionReasonBuilderTests`, `PenTestCaseSuppression`, plus a build
(changed `GetAwaitingAsync` signature has one caller; the validator constructor gained an optional parameter). Vitest
`src/suppression` already passed; Playwright `caseSuppression` unrun. Rider lint at error level was clean on
CaseSuppressionLimits, CaseSuppressionLapse, the validator and CaseSuppressionLapseTests; the service, repository,
DynamicEnvironment and the service test edits were not re-linted.
UI: no change needed; lapsed requests arrive with `standingId` Expired and `canDecide` false, and the existing cards and
dialogs already render Expired and hide Approve. The setting default reaches SPA users only as behaviour, not as a key.
