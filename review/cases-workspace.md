# Slice review: cases-workspace

Agent-mode review, 2026-11-08. No build, test, Playwright or lint was run (Rider lint_files timed out on the first
batch). Scratch in `scratchpad/cases-workspace/`.

## 1. Summary and verdict

READY AFTER ONE SERIAL TEST PASS. Server code is largely sound: every query is tenant and role scoped, skim claims
atomically, analytics shows the caller only their own throughput. Real defects fixed: lock hand-over on the legacy case
update accepted any tenant user and was a read-modify-write race; diary set and clear did not re-check the lock holder
at write time; skip overflow (500) on the queue; unbounded supervision period; an unbounded-regex JSONPath. The slice
had no pen tests at all; one class added.

## 2. Files reviewed

Read: CaseSupervisionService, CaseDiaryService, CaseSearchService, CaseSearchViewSql, CaseViewRecorder,
CaseAnalyticsService (and facts query, WorkloadCalculator), CaseTagService/Repository, CaseSearchFilterRepository and
validator, CaseRepository.GetByIdActiveOnlyAsync, CaseService.Update (lock paths), CaseTriageSql,
GetCaseTriagePageQuery, endpoints for supervision, diary, search, analytics. Vue: pageState, lastUsed, api files,
CasesQueuePanel, CaseDiaryPanel, CaseSupervisionDialog, CasesSupervisionPanel (first 120 lines), RiskTimeline. Pull-ins:
none of the later-slice Vue files were re-reviewed. Case-journal files under src/cases/journal not touched.

## 3. Findings

| #  | Sev    | Where                           | What                                                                                                                                         | Status                                                                             |
|----|--------|---------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------|
| 1  | Med    | CaseService.Update              | LockedUser only had to be in the tenant: a holder could hand a case to someone with no permission or no workflow/status role (case vanishes) | Fixed: shared CaseEligibleAnalysts (same rule as supervisor reassign)              |
| 2  | Med    | CaseService.Update              | claim race: read, mutate, full-row write; two claimants both win                                                                             | Fixed: guarded conditional update of lock columns before the write, 409 if changed |
| 3  | Low    | CaseDiaryService                | lock holder checked on read only                                                                                                             | Fixed: write condition includes holder                                             |
| 4  | Med    | CaseSearchService.RunAsync      | Skip=int.MaxValue overflowed take+skip, 500                                                                                                  | Fixed: clamp 0..100000                                                             |
| 5  | Low    | CaseSupervisionService.Workload | period unbounded                                                                                                                             | Fixed: 366 days, new key PeriodInvalid                                             |
| 6  | Med    | ProcessCaseQuery                | plain SelectToken, ReDoS                                                                                                                     | Fixed                                                                              |
| 7  | Info   | null-tenant pattern             | case repositories use parameter-based `!tenantRegistryId.HasValue`, no row-level escape                                                      | none needed                                                                        |
| 8  | Low    | lastUsed.ts                     | storage key not per user                                                                                                                     | SHARED-TODO                                                                        |
| 9  | Design | queue UI                        | list shows first 100 only, offset API unused; Total is 0 on an empty over-skipped page                                                       | left                                                                               |
| 10 | Design | analytics                       | composite report recomputed by Queue, Quality and Health separately (<=50000 rows, correlated subqueries)                                    | left, doc TODO                                                                     |
| 11 | Design | CaseTag Merge                   | ignores lock and closed state                                                                                                                | SHARED-TODO                                                                        |

Escalation rule: supervision grants nothing (reassign changes the lock only; target must already hold perm 1 plus
workflow and status roles; supervisor needs both roles too via GetByIdActiveOnlyAsync), so privilege-escalation-rule.md
is not bypassed. QA and diary: no per-user verdicts or private diaries exist; both are case properties visible to anyone
with case access (design, doc TODO). My day uses only the caller's name; team figures need 3+ people.

## 4. Fixes made

CaseEligibleAnalysts.cs (new), CaseSupervisionService.cs, CaseService.cs, CaseDiaryService.cs, CaseSearchService.cs,
ProcessCaseQuery.cs, CaseSupervisionResources.cs/.resx (PeriodInvalid), CaseResources.resx (LockedUserInvalid wording).
No comments added.

## 5. Test and pen-test matrix

Read: CaseSupervisionServiceTests (8), CaseDiaryServiceTests, CaseSearchServiceTests (incl. TwoPeopleSkimmingAtOnce...,
paging), CaseQaSamplingTests, CaseAnalyticsServiceTests, CaseServiceTests, PenTestCase (lock test). Added:
CaseSupervisionServiceTests +2 (period, status-role boundary); CaseSearchServiceTests +AHostileSkipIsClamped...;
CaseServiceTests +UpdateCannotHandTheLockToAnAnalystWhoCouldNotSeeTheCase; new
Jube.Tests/Security/Case/PenTestCaseWorkspace.cs (7 tests: read matrix, role-gated indistinguishability, hostile params,
diary, supervision, saved searches, skim). Tests unrun and may need adjustment to real status codes. Catalogue updated
in docs/Testing (Engine, ServiceRepositories, SecurityAccessIsolation). Existing PenTestCase lock expectations still
hold.
Run: CaseSupervisionServiceTests, CaseSearchServiceTests, CaseDiaryServiceTests, CaseServiceTests, PenTestCaseWorkspace,
PenTestCase, vitest src/cases.

## 6. Translations

Created 49 resx (pl, ar, ka, id, sv, ur, hi for CaseAnalytics, CaseDiary, Case, CaseSearchFilter, CaseSearch,
CaseSupervision, CaseTag Resources); key and {n} parity verified by script; ar/ur wrap Latin tokens in isolates; .fr
untouched. SPA: pl/ar complete (audit); ka/id/sv/ur/hi proposals for all 362 cases.* keys in
review/cases-workspace.locale-proposals.json (placeholder parity checked). No new SPA keys. No physical left/right CSS
found in the files read; timeline uses logical properties.

## 7. Docs

Markers added to WorkingSkimLockNext, WorkingLockAllocationUnlock, WorkingCaseSupervision, WorkingQualityReviewSamples,
WorkingTypedDiaries, WorkingClosedDiarySuspend, Frontend/CaseManagementAnalytics (grep `TODO(` in those; about 50,
screenshots plus stale Razor-era text).

## 8. Shared-code TODOs

Three lines appended to review/SHARED-TODO.md.

## 9. Manual test sheet

Two analysts press Take next together; hand a locked case to an ineligible user (refused); supervisor break and reassign
with and without status role; diary set on a case locked by someone else; queue skip hostile via API; Supervision period
beyond a year; pl/ar/ur RTL tabs and rail; hostile text in saved filter name.

## 10. Commit pathspecs

slices.json paths plus the C# files in section 4, 49 new resx, the new/changed tests, docs pages, review files.

## 11. Not covered

Rider lint (all changed C# unlinted); Vue bodies not fully read (CasesRail, CasesList, CasesPulse, CasesQaCard,
CasesAnalyticsPanel/Stats, CasePeek, CaseSummaryBody, CaseSearchEditor, CasesApprovalsPanel, cells.ts, exportRows);
visual RTL/font checks; Playwright; Jube.Charting escaping trusted; engine escalation and risk sweep only grepped;
ProcessCaseQuery regex has no dedicated test.

## 12. Follow-up pass (coordinator brief)

Lint: `lint_files` worked on retry. At error severity no problems in: CaseEligibleAnalysts (one unused using removed),
CaseSupervisionService, CaseDiaryService, CaseSearchService, CaseTagService, CaseTagEndpoints, ConflictException,
CaseService, CaseRiskAssessor, CaseRepository, CaseAutomationStarter, ProcessCaseQuery, PenTestCaseWorkspace,
CaseTagServiceTests, CaseSupervisionServiceTests, CaseDiaryServiceTests. Not linted (timed out): CaseSearchServiceTests,
CaseServiceTests; no warning-level pass on any file beyond CaseEligibleAnalysts. No SSRF or hook execution file touched.

1. **localStorage key scoped.** `cases/lastUsed.ts` now keys `jube.cases.last:<tenantId>:<user>`; load and save take a
   scope from `/api/Me` (no scope, no storage), all access try/catch. `CasesPage.vue` watches the scope so restore waits
   for Me; `CaseDetailPage.vue` (case-record's file) got a one-call change to pass the scope. No sign-out clearing: the
   sign-out hook lives in shell-auth `auth/session.ts` and scoping makes it unnecessary. Vitest: lastUsed.spec (+2).
2. **Tags and the lock.** Journal tag add/remove already skips cases locked by another
   (`CaseKeyJournalTagService.ApplyAsync`), and the tag job runner goes through it, so no change there (no edits to
   journal files). The gap was `CaseTagService.MergeAsync` (case-level tags): now a 409 `ConflictException` (new class,
   endpoint mapping, key `LockedByAnotherUser` in 8 resx). New `CaseTagServiceTests` (1).
3. **Bugs found while verifying the pen test.** A saved search on an unknown field raised InvalidOperationException from
   the compiler and returned 500: now a 400 `SearchInvalid` (new key, 8 resx; service test added; pen test asserts 400).
   Field names are only interpolated after a whitelist match, so no injection.
4. **Engine sweeps.** CaseAutomation expiry: update was `Id` only, so an analyst's concurrent close or reopen was
   overwritten, and the audit event was written even when nothing changed; now guarded on the status read, event and
   transition only on success; expired read bounded to 5000 (CaseRepository, CaseAutomationStarter; test
   `TheExpiryUpdateOnlyChangesACaseThatIsStillInTheStatusItWasReadIn...`). CaseRiskAssessor cleared the recheck marker
   unconditionally, losing a diary or settings re-rank requested during assessment; now conditional on the values read.
   Escalation sweep read: tenant scoped through status/workflow guids, bounded by batch size, guarded move; one low
   issue left: `CaseStatusMover` records the checklist-bypass audit before the guarded update, so a lost race writes a
   bypass note for a move that did not happen (cannot reorder without changing what the bypass reads; left, low). Risk
   sweep is global by design and bounded by batch size.
5. **Vue and RTL.** Read CasesRail, CasesQaCard, CasesPulse, TrendLine, StayBar, RiskTimeline, CasesQueuePanel, dialogs;
   no physical left/right classes anywhere. Fixed: tab arrow keys were not mirrored in RTL (`tabStep(key, rtl)`,
   vitest); user names, case labels and status names interpolated into translated sentences are now bidi isolated
   (`cases/isolate.ts`: lock cell, stay bar, supervision dialog; cells.spec updated, isolate.spec). Left: sparklines and
   the ring keep a left-to-right time axis (neutral shapes); rail filter names are not wrapped in bdi. CasePeek,
   CaseSummaryBody, CaseSearchEditor, CasesList, CasesAnalyticsPanel/Stats, CasesApprovalsPanel only grepped (no v-html,
   no physical CSS, no raw Intl).
6. **PenTestCaseWorkspace re-read against endpoints.** Verified from the endpoint catch blocks and services: analytics
   and search map NotFound 404, DtoValidation 400, Forbid 403, Conflict 409 (supervision, diary); saved-search insert
   returns 200, update validator runs before the owner lookup so another tenant's PUT may be 400 (test already accepts
   400 or 404); `Counts/not-a-guid` is a route miss (404); Chart/99 an enum bind failure (400). Corrected: hostile
   filter now includes the `id` the parser reads and asserts 400. Still unverified by running: seeded user permissions
   (18 for WithPermission and TenantB), World role-gating of the Gated and HiddenStatus graphs, JSON casing.

Tests to run in the serial pass: CaseSupervisionServiceTests, CaseDiaryServiceTests, CaseSearchServiceTests,
CaseServiceTests, CaseTagServiceTests, CaseRiskSweepTests, CaseRiskRecheckTriggerTests, CaseEscalationSweepTests,
PenTestCaseWorkspace, PenTestCase; vitest `src/cases`; Playwright `cases` spec (tab keyboard, last-used restore).

## 13. Second follow-up (coordinator brief)

1. **Checklist-bypass audit.** `CaseStatusMover.MoveAsync` now checks the case is still open and in the from-status
   before recording the bypass, and returns false without recording otherwise. The bypass evaluation reads the case's
   current status, so it cannot move after the update; a window between the check and the guarded update remains but the
   common lost-race case no longer writes a false audit. Test `AutomationThatLostTheRaceMovesNothingAndRecordsNoBypass`
   in `CaseChecklistGateTests`.
2. **Six Vue bodies read in full.** CasePeek, CaseSummaryBody, CaseSearchEditor, CasesList (with cells.ts),
   CasesAnalyticsPanel, CasesAnalyticsStats, CasesApprovalsPanel. No `v-html`, all cells built with `textContent`, rows
   capped at 100 per page, no physical CSS. Fixed: user-controlled text (notes, lane reason, driver labels, user names,
   case key values, saved-search name in the delete prompt) is bidi-isolated via `isolate`/`joinIsolated` and notes wrap
   (`break-words whitespace-pre-wrap`); list key and driver cells carry `dir="auto"`; approvals panel uses `caseLabel`
   instead of a hard-coded `C-` prefix. Vitest: isolate.spec (+1 for `joinIsolated`). Left as design: each analytics
   chart requests the whole report server-side (about 12 requests per canvas), list actions say only Peek/Open with no
   case id in the accessible name (DataGrid), `now` is fixed at mount in the analytics panel.
3. **Lint.** Rider `lint_files` timed out again on CaseSearchServiceTests, CaseServiceTests, CaseStatusMover and
   CaseChecklistGateTests (the earlier error-level passes in section 12 stand). Unlinted: those four.
   Tests to run in addition to section 12: CaseChecklistGateTests, vitest `src/cases` (isolate, cells, lastUsed,
   pageState).
