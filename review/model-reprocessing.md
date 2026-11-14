# Slice model-reprocessing

## 1. Summary and verdict

Verdict: fixed, needs the owner's serial test pass. Resx key parity of all 14 files re-verified after the shared
scratchpad incident. The engine core (keyset paging, page batches, replay suppression of cache, TTL, idempotency claims,
cases, holds) is sound and tested. The lifecycle around it had real bugs, now fixed: nothing was resumable after a crash
or shutdown, any unexpected error left a run "Allocated" forever, and in each case the rule was blocked from further
runs. Nothing was built or run; Rider lint timed out (not done).

## 2. Files reviewed (read in full or the relevant parts)

Engine: ReprocessingTaskStarter.cs, Reprocessing/*, Archiver/ReprocessingArchiveBatch.cs,
SyncEntityAnalysisModelRequestXPathExtensions.cs (keyset SQL, 640-660),
Postgres.ExecuteReturnPayloadFromArchiveAfterAsync. Invoke gates: TtlCounterExtensions,
AbstractionRulesWithSearchKeysExtensions, ExecuteCacheEntriesExtensions, ActivationRuleTtlCounterExtensions,
ActivationRuleActivationWatcherExtensions, ActivationRuleCreateCaseObjectExtensions, ResponseElevationRangeExtensions,
IterateActivationRulesExtensions, WriteResponseJson..., ReferenceDateExtensions, SanctionsExtensions (upsert,
idempotent). Data: both repositories, GetNext query. Service: instance service (create/update/trigger/delete), rule
service (permissions, execute), mappers, validators, endpoints (instance), DTO, tool catalogue. Resx, docs (4 pages).
Pull-ins: none outside the slice area.

## 3. Findings

| #  | Sev  | Where                                                     | What                                                                                                                                                                     | Status                                                                                                                                           |
|----|------|-----------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | High | ReprocessingTaskStarter.RunNextAsync                      | Cancellation (shutdown) or exception after pick left status 1-3 forever; rule blocked (insert refuses status not 4/5). No resume.                                        | Fixed: cancel requeues (status 0, restart from window start, replay-safe); exception marks Failed                                                |
| 2  | High | GetNext query                                             | No reclaim of abandoned runs after hard crash                                                                                                                            | Fixed: stale (no heartbeat 30 min) 1-3 rows reset; pickup stamps UpdatedDate; count phase heartbeats                                             |
| 3  | Med  | GetNext query                                             | Soft-deleted rule's queued instance still ran                                                                                                                            | Fixed (Deleted filter)                                                                                                                           |
| 4  | Med  | Rule repo Delete/Update                                   | Editing or deleting a rule left its running/queued instances on the old script                                                                                           | Fixed: uncompleted instances soft-deleted (running stops via existing KeyNotFound path)                                                          |
| 5  | High | Instance service Create (agent tool) + repo Insert/Update | Plain create bypassed the one-uncompleted guard and kept client StatusId/counts/dates; Update let a client forge Completed or counts (DTO says server-assigned)          | Fixed: Create uses the guarded path (409); Insert resets run state; Update preserves server state and requeues a running one                     |
| 6  | Med  | Both repos GetAsync                                       | Returned soft-deleted rows, unbounded, paged in memory                                                                                                                   | Fixed (Deleted filter, ordered)                                                                                                                  |
| 7  | Low  | Instance repo                                             | `                                                                                                                                                                        |                                                                                                                                                  | !tenantRegistryId.HasValue` returned all tenants for a tenantless caller (service guard masked it) | Fixed (removed) |
| 8  | Low  | Engine                                                    | `int.Parse(ReprocessingBulkLimit)`: 0 silently "completed" with nothing done; huge values unbounded memory                                                               | Fixed: ReprocessingPageLimit (default 10000, max 100000)                                                                                         |
| 10 | Med  | Rule repo Update                                          | Versioned update (new id, old soft-deleted) left finished runs on the deleted old id, so the new version's grid was empty. Not deliberate (nothing reads them by old id) | Fixed: non-deleted Completed/Failed runs are repointed to the new rule id (uncompleted ones are cancelled, finding 4); runs themselves unchanged |
| 9  | Low  | DTO                                                       | Status doc omitted 5 Failed                                                                                                                                              | Fixed                                                                                                                                            |

Follow-up pass: finding 10 above; replay hold/notification gap closed with
ResponseElevationRangeExtensionsTests.AReplayNamesTheElevationButStampsNoHoldAndDispatchesNothingAsync (Unit;
unit-level, hits ClaimLegs only if the gate is missing as Services is unset); Rider lint rerun in single files.

Verified correct: keyset (ReferenceDate, Guid) tuple compare, order and limit; tenant isolation via the model guid; one
transaction per page; sampling; stop on delete; replay suppression (cache, reference date, TTL increments, idempotency
claims never reached on reprocessing, so the model-abstraction SetAddAsync fix is not exercised and cannot be
double-claimed; case, watcher, hold and notification dispatch gated); archive update replace-in-place with version
history.

Design questions (not bugs): (a) one permission (26) covers edit rule, trigger, delete/stop; (b) replay reads cache as
now, not as at the transaction's date; (c) editing a rule orphans finished-run history on the old rule id; (d)
reprocessing overwrites archived responses (history kept in versions). Evidence: services use `permissions = [26]` only;
invoke gates read cache live.

## 4. Fixes made

Files: Jube.Data/Repository/EntityAnalysisModelReprocessingRule{,Instance}Repository.cs, Query/GetNext...Query.cs,
Jube.Engine/.../ReprocessingTaskStarter.cs, Reprocessing/ReprocessingPageLimit.cs (new),
Jube.Service/.../EntityAnalysisModelReprocessingRuleInstanceService.cs, instance DTO description.

## 5. Test and pen-test matrix

Existing read: ReprocessingTaskStarterTests (12), ReprocessingDateRangeTests, ArchiveReprocessingWriteTests (7),
ReprocessingVolumeTests, both service test files, PenTestModelReprocessingRule (Instance) (trigger tenant, forged state,
race of 8, plain POST bypass, bounds, hijack), invoke gate tests (DoesNothingWhenThisIsAReprocessingRunAsync x3,
ExecuteCacheDbStorageWritesNothingWhenReprocessingAsync).
Added (unrun): ReprocessingPageLimitTests (Unit, 11 cases); in ReprocessingTaskStarterTests: ACancelledRunIsRequeued...,
AnInstanceAbandonedByACrashedProcess..., AQueuedInstanceOfADeletedOrInactiveRule...; instance service:
InsertQueuesServerAssignedState..., UpdateIgnoresClientSuppliedRunState..., ListsNeverContainSoftDeletedRowsAsync; rule
service: DeletingARuleCancelsItsUncompletedInstances..., ListsNeverContainSoftDeletedRulesAsync. Changed:
UpdateSupersedes... now expects status 0 and counts 0; ListAsyncClamps... uses three rules (create is now guarded).
Tests to run: ResponseElevationRangeExtensionsTests, those classes, plus PenTestModelReprocessingRule and
PenTestModelReprocessingRuleInstance.

## 6. Translations

Created 14 resx (pl, ar, ka, id, sv, ur, hi for EntityAnalysisModelReprocessingRuleResources and ...InstanceResources),
key parity checked by script against neutral, XML parsed, {0} preserved, machine quality. .fr untouched. SPA: no keys
owned; proposals file empty. Gap: DTO ListColumn titles hard-coded English (shared TODO). Not audited visually: RTL,
Nastaliq/Devanagari/Georgian rendering, fonts.

## 7. Docs

Updated docs/Configuration/Reprocessing/*: screenshot markers on all 14 images (new names, old PNGs now unreferenced),
corrected stale cache statements, added permission, restart, crash, page-size sections. TODO (docs) markers: Eligibility
(replay semantics, side-effect list), Instances (permission 26, 30 minute window, history after edit), Filters (routes,
intervals). Test catalogue updated in docs/Testing/Engine.md and ServiceModelConfiguration.md.

## 8. Shared TODOs raised

Three lines appended to review/SHARED-TODO.md.

## 9. Manual test sheet

Trigger a run, trigger again (409); delete mid-run (stops); stop the service mid-run and restart (back to Not Allocated,
completes once, counts equal available); edit/delete a rule with a waiting request; invalid rule (Failed, can
retrigger); PUT forged status ignored; tenant B cannot see or trigger; en/pl/ar/ur/hi messages.

## 10. Commit pathspecs

The listed files, new resx and tests, docs/Configuration/Reprocessing, docs/Testing hunks, the two endpoint files
(unchanged).

## 11. Not covered

No build, tests, Rider lint (timed out), Playwright. Rider lint: warnings-level clean for ReprocessingPageLimit and the
three Data files (only allocation hints, house pattern; one redundant cast fixed); errors-level clean for
ReprocessingTaskStarter and the two new unit test files; EntityAnalysisModelReprocessingRuleInstanceService.cs timed out
twice and is UNLINTED, as are the edited service and engine test files and the warning level of ReprocessingTaskStarter.
Rule endpoint file, rule service tests beyond names, Vue UI, Ask Jooby tool behaviour not read. Multi-node stale reclaim
not exercised.
