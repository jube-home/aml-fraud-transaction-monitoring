# Slice review: case-record

Agent-mode review, 2026-11-08. Nothing was built and no .NET or DB-backed test was run. Two JS runs were made (vitest on
`src/evidence` and `src/caseRecord`, 31 passed; eslint and prettier clean on the changed Vue/TS). Rider `lint_files`
timed out twice, so no C# file is linted. Scratch: `scratchpad/case-record/` (resx generator `gen.py`, `tr_a.py`,
`tr_b.py`, SPA locale text files and `build.py`).

## 1. Summary and verdict

Verdict: READY AFTER ONE SERIAL TEST PASS (section 5). The three design questions in section 3 were turned into fixes in
the follow-up pass (section 12); one owner confirmation remains on the lock rule. Six defects fixed in code: bidi and
invisible characters survived in stored file names; the repository note list ignored action roles (a note, and files on
it, leaked to roles lacking the action); six mutating JSON endpoints skipped the antiforgery check the upload endpoints
have; JSON bodies on note, research, hold and override had no size cap below Kestrel's 30 MB; the hold lock compared
user names case-insensitively while every other lock check is ordinal; and the file and note repositories still had the
`|| !tenantRegistryId.HasValue` read escape. The external research request feature is gone from the working tree (see
3).

## 2. Files reviewed

Read in full or in the relevant body: `CaseFileEndpoints`, `CaseNoteEndpoints` (insert and routes),
`CaseResearchEndpoints` (routes), `CaseHoldEndpoints`, `CaseChecklistEndpoints` (routes); `CaseFileService`,
`CaseNoteService`, `CaseResearchService`, `CaseNoteByCaseKeyValueService` and mapper, `CaseHoldService` (list, decide,
call), `CaseChecklistService` (check, uncheck, override, snapshot), `CaseSummaryService` (visibility guard only),
`CaseWorkflowFormEntryService.InsertAsync` and `AcceptedAsync` (dependency, claimed by case-workflow-config); mappers
for file, note, research; `CaseNoteAttachmentLoader`; `CaseFileRepository`, `CaseNoteRepository`,
`CaseResearchRepository`, `CaseEventRepository` (null-tenant check); `GetCaseNoteByCaseKeyValueQuery`; validators for
note, research, source, `CitationAddress`; `HtmlSanitiser` (inherited); `RequestHardeningMiddleware`,
`EmptyBodyGuardMiddleware`, `AntiforgeryEndpointExtensions`; Vue: `CaseDetailPage.vue`, `EvidenceTree.vue`,
`evidence/model.ts`, and as dependencies (not mine, cases-workspace) `CaseNoteItem`, `CaseNoteFiles`,
`CaseResearchItem`, `DecideNoteDialog`, `operationsApi.ts`, `text.ts`, `researchSources.ts`, `download/file.ts`.
First-touch pull-ins: none. `Jube.Resources` Case* resource classes for the seven classes below.
Not read in full: `CaseChecklistReconciler`, `CaseChecklistGate`, `CaseChecklistSlaBreachRecorder`,
`CaseHoldService.LoadAsync`, the rest of `CaseSummaryService`, `DecideStatusDialog`, `DecideHoldDialog`,
`DecideFormsDialog`, `CaseFormEntries`, `CaseDecide`.

## 3. Findings

| #  | Sev  | Where                                                                                                         | What                                                                                                                                                                               | Status                                                                                 |
|----|------|---------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------|
| 1  | Med  | `CaseFileService.SafeFileName`                                                                                | Only U+202E was stripped; U+202A-202D, U+2066-2069, U+200E/F, U+061C, zero-width, BOM and line separators survived, enabling extension spoofing, and trailing dots/spaces survived | Fixed (`IsInvisibleOrBidiControl`, trim trailing)                                      |
| 2  | Med  | `CaseNoteRepository.GetByCaseKeyValueAsync` (`GET /api/CaseNote/ByCaseKeyValue`)                              | Did not apply action roles that `GetCaseNoteByCaseKeyValueQuery` applies, so notes under an action the caller lacks were listed                                                    | Fixed                                                                                  |
| 3  | Med  | `CaseFileRepository.GetByCaseKeyValueActiveOnlyAsync`, `CaseFileService.EnsureFileVisibleAsync`               | Files on a note whose action the caller lacks were listed, downloadable and removable                                                                                              | Fixed (`CaseNoteRepository.ActionGrantedToUserAsync`)                                  |
| 4  | Low  | `CaseNoteEndpoints` POST, `CaseHoldEndpoints` Release/Refuse, `CaseChecklistEndpoints` Check/Uncheck/Override | No `ValidateAntiforgeryForCookieSessions` (origin check in middleware still applied)                                                                                               | Fixed                                                                                  |
| 5  | Low  | same JSON endpoints plus `CaseResearchEndpoints` POST                                                         | No body cap below 30 MB                                                                                                                                                            | Fixed (1 MiB note and research, 64 KiB hold and override, `RequestSizeLimitAttribute`) |
| 6  | Low  | `CaseHoldService.DecideAsync`                                                                                 | Lock compared with `OrdinalIgnoreCase`; `CaseService` and checklist use `Ordinal`                                                                                                  | Fixed                                                                                  |
| 7  | Low  | `CaseFileRepository`, `CaseNoteRepository`                                                                    | `(== tenantRegistryId \|\| !tenantRegistryId.HasValue)` read escape (parameter based; tenantless constructors unused in production)                                                | Fixed: `tenantRegistryId.HasValue && ==`                                               |
| 8  | Low  | `CaseFileService.UploadAsync`                                                                                 | Three 25 MB copies per upload                                                                                                                                                      | Fixed (one `ToArray`)                                                                  |
| 9  | Low  | `CaseDetailPage.vue` status badge                                                                             | Raw `backColor` and `foreColor` interpolated into `style`; server validates `#RRGGBB` but a trailing newline passes .NET `$`                                                       | Fixed (`caseRecord/statusStyle.ts`, hex only)                                          |
| 10 | Low  | `evidence/model.ts`                                                                                           | Unbounded recursion on hostile nested JSON                                                                                                                                         | Fixed (`maxEvidenceDepth` 32)                                                          |
| 11 | Med  | `CaseWorkflowFormEntryRepository.GetByCaseKeyValueActiveOnlyAsync`                                            | Entries listed without the form's role grant; also the null-tenant escape                                                                                                          | Left, BUG line on SHARED-TODO (case-workflow-config)                                   |
| 12 | Low  | `CaseRepository.GetByIdActiveOnlyAsync`                                                                       | Same null-tenant escape, gate for every case child                                                                                                                                 | Left, BUG line (cases-workspace)                                                       |
| 13 | Low  | `src/cases/detail/*`                                                                                          | No `dir="auto"` or `<bdi>` for notes, research, file names; source title shown as sanitised HTML text; no client size check on attachments                                         | Left, SHARED-TODO (cases-workspace owns the folder)                                    |
| 14 | Info | checklist audit text                                                                                          | "Ticked by ..." strings are English and stored in `CaseChecklistEvent.Detail` (audit data, not UI); validation failures put `e.Message` into that detail                           | Left, note only                                                                        |

Design questions (code does what it was written to do; intent is the question):

- D1. Notes, research, files and form entries ignore the case lock; only status, checklist and hold changes consult it.
  Evidence: `CaseNoteService.InsertAsync`, `CaseFileService.UploadAsync`, `CaseResearchService.CreateAsync`,
  `CaseWorkflowFormEntryService.InsertAsync` never read `Locked`/`LockedUser`, while `CaseService.UpdateAsync` (697),
  `CaseHoldService` (237), `CaseChecklistService.LoadHeldCaseAsync` (616) do. Notes are append-only audit, so letting a
  supervisor annotate may be intended.
- D2. Any holder of permission 1 who can see a case can soft-remove any file on it, though upload is limited to the note
  author. Removal is audited. Possible rule: uploader or CaseSupervision (18).
- D3. No file type allow-list, magic number check or virus-scan hook; the declared type is a stored label. Natural hook:
  between the copy and the insert in `UploadAsync`. Inline types are limited to pdf/png/jpeg/gif/webp/text with nosniff
  and CSP sandbox; SVG and HTML download as octet-stream.

External research: `CaseNoteResearchRequest`, `CaseNoteCitations`, `RateLimitedException`, `UnavailableException`
(CaseNote), `CaseNoteCitationDto`, `CaseNoteState`, `CaseResearchForm.vue` show as `AD` in git status (added in the
index, deleted in the working tree) and have no remaining references (grep over cs, ts, vue, json, resx, md). So there
is no SSRF, prompt-injection, rate-limit or data-egress surface in the case record; research is record-only and Jube
never fetches a source address. Owner: stage those deletions, do not commit the index copies. `RateLimitedException`
elsewhere (Parser, SyncSchedule) belongs to other slices.
SelectToken: none in any file of this slice or its dependencies read (grep of Service case folders, Jube.Case, case
repositories). The sites listed on SHARED-TODO line 127 are other slices'.
Escalation rule: `review/privilege-escalation-rule.md` covers roles, users and keys; nothing in the case record grants
permissions, so it does not apply. Checklist override (71) refuses the holder and requires the case not closed; checked
in `OverrideAsync`.
Checked and sound: file id tenant plus case role visibility (`EnsureFileVisibleAsync`), identical 403 for missing,
foreign and hidden; notes sanitised on write and read, plain text in Vue (`plainText`, no `v-html` anywhere in the
slice); research URLs validated server (`CitationAddress`) and client, links `rel=noopener noreferrer nofollow`; hold
release needs perm 68 path, exact 200, advisory-locked archive update, hook policy and credential by env var; form
submission re-validates server side (`FormSubmissionValidator`, validators, hidden fields, form role gate); checklist
items are scoped by case and tenant; each mutation writes `OperationScope` audit.

## 4. Fixes made (files)

`Jube.Service/Repository/CaseFile/CaseFileService.cs`, `Jube.Service/Query/CaseHold/CaseHoldService.cs`,
`Jube.Data/Repository/CaseFileRepository.cs`, `Jube.Data/Repository/CaseNoteRepository.cs`,
`Jube.App/Endpoints/Repository/CaseNoteEndpoints.cs`, `CaseResearchEndpoints.cs`,
`Jube.App/Endpoints/Query/CaseHoldEndpoints.cs`, `CaseChecklistEndpoints.cs`, `Jube.App/Vue/src/pages/CaseDetailPage.vue`,
`Jube.App/Vue/src/evidence/model.ts`, new `Jube.App/Vue/src/caseRecord/statusStyle.ts`. No code comments added, one class per
file.

## 5. Test and pen-test matrix

Existing, read: `CaseFileServiceTests` (33: create, upload, validation, tenant, audit, change events),
`CaseFileNoteServiceTests` (13), `CaseNoteExtensionServiceTests` (12), `CaseNoteByCaseKeyValueServiceTests`
(action/workflow/status gating), `CaseResearchServiceTests`, `CaseHoldServiceTests` (24, includes lock, single call,
policy), `PenTestCaseFile` (matrix, IDOR, names, spoofed types, size, replay), `PenTestCaseNote` (matrix, BOLA, stored
XSS, mass assignment, limits, verbs), `PenTestCaseResearch` (matrix, hostile URLs), `PenTestCaseHold` (matrix, CSRF
origin, credential, replay, SSRF policy), checklist service tests listed in project memory (not individually read:
`CaseChecklist*` under `Jube.Tests/Service/Query/CaseChecklist`).
Added (unrun):
| Test | Covers |
|---|---|
| `CaseFileServiceTests.UploadStripsBidiAndInvisibleCharactersFromTheStoredFileNameAsync` (6 cases) | finding 1 |
| `CaseFileNoteServiceTests.AFileOnANoteWhoseActionTheCallerLacksIsNeitherListedNorDownloadableNorRemovableAsync` |
finding 3 |
| `CaseNoteExtensionServiceTests.AnAnalystWithoutTheActionOfANoteDoesNotSeeItInTheRepositoryListAsync` | finding 2 |
| `CaseHoldServiceTests.ACaseLockedByAUserWhoseNameDiffersOnlyInCaseIsStillLockedAgainstTheCallerAsync` | finding 6 |
| `PenTestCaseNote.Insert_WithoutAnAntiforgeryTokenIsRefusedAndStoresNothingAsync`,
`PenTestCaseHold.Decision_CookiePostsWithoutAnAntiforgeryTokenAreRefusedAndNothingIsCalledAsync` | finding 4 |
| new `PenTestCaseChecklist` (4: matrix, BOLA indistinguishability, CSRF, hostile segments and reasons) | checklist had
no pen test |
| `PenTestCaseFile.Upload_FileNames_...` extended with bidi names and assertions | finding 1 |
| vitest `statusStyle.spec.ts`, `model.spec.ts` depth cases | findings 9, 10 |
Tests to run serially: `CaseFileServiceTests`, `CaseFileNoteServiceTests`, `CaseNoteExtensionServiceTests`,
`CaseNoteByCaseKeyValueServiceTests`, `CaseHoldServiceTests`, `CaseResearchServiceTests`, `CaseNoteServiceTests`,
`Category=PenTest` classes `PenTestCaseFile`, `PenTestCaseNote`, `PenTestCaseResearch`, `PenTestCaseHold`,
`PenTestCaseChecklist`, `PenTestUploadAntiforgery`, plus `PenTestCaseNoteNarrative`, `CaseNarrativeJobTests`,
`GraphServiceTests` (use the narrowed repositories), and the `Jube.Tests` documentation catalogue test. Watch:
`PenTestCaseChecklist` expectations for the authorised user (404 or 409 on a bogus item, 200 on reads) were inferred,
not observed; the 1 MiB cap against `Insert_LengthLimit_AndOversizeBodiesAsync` (5 MB and 3 MB bodies already accept
413).

## 6. Translations

Server: 49 new resx (`CaseFile`, `CaseNote`, `CaseResearch`, `CaseNoteByCaseKeyValue`, `CaseHold`, `CaseChecklist`,
`CaseSummary` x pl, ar, ka, id, sv, ur, hi), generated by `gen.py` from the neutral files; independent script verified
keys, `{n}` placeholders, `JUBE_HOOK_ALLOWED_*` tokens, non-empty, none identical to English: 0 problems. `.fr.resx`
untouched. SPA: en/pl/ar parity for `caseDetail.` (307) and `evidence.` (13) is complete. Proposals for ka, id, sv, ur,
hi for all 320 keys are in `review/case-record.locale-proposals.json` (machine translated, placeholders, plurals and the
newlines of the `help.*` texts verified by `build.py`). RTL/Latin issues: see finding 13; Urdu digits stay Western.

## 7. Docs written and TODO markers

Edited: `docs/Configuration/CaseManagement/WorkingUploadingRecallingFiles/index.md` (new section with limits and
enforcement; TODO docs x3, screenshot x2), `WorkingCaseNote`, `WorkingCaseStatus`, `WorkingCaseRating`,
`WorkingNavigatingCaseRecord`, `WorkingCaseFormJournal` (each: appended section, TODO docs x1, TODO screenshot x1),
`docs/Frontend/CaseNotes.md` (review notes, TODO docs x3, screenshot x1), `docs/Frontend/CaseResearch.md` (TODO docs x1,
screenshot x1). Test catalogue updated in `ServiceRepositories.md`, `ServiceQueries.md`, `SecurityAccessIsolation.md`.
Find all with
`grep -rn "TODO(" docs/Configuration/CaseManagement docs/Frontend/CaseNotes.md docs/Frontend/CaseResearch.md`.

## 8. Shared-code TODOs raised

Six lines appended to `review/SHARED-TODO.md` (two BUG: form entry role gate, `CaseRepository` null-tenant escape; three
Vue notes for cases-workspace; `HtmlSanitiser` partial class).

## 9. Manual test sheet

Permission 1 denied and granted on every case child; open a case you cannot see (other tenant, gated workflow, hidden
status) and see the same refusal on notes, files, research, hold, checklist. Upload: 26 MB refused; names with RTL
override, `../x`, `a.svg`, `x.html`; download SVG and HTML arrive as downloads, PDF and PNG open; file on a note under
an action your role lacks is not listed. Notes: hostile HTML, 100000 and 100001 characters, Arabic note in the English
UI. Research: javascript:, data:, credentials in URL, 11 sources. Status: move while checklist open is refused, override
by non-holder with perm 71, holder cannot override. Hold: release as non-holder, case locked to another user (including
a name differing in case), non-200 hook. Forms: submit with hidden or unknown field. Dark theme, ar and ur layout,
narrow width, keyboard only, three locales.

## 10. Commit pathspecs

`python3 -I .claude/skills/jube-slice-review/scripts/slice_audit.py case-record --paths`, plus: the 49
`Jube.Resources/Case{File,Note,Research,NoteByCaseKeyValue,Hold,Checklist,Summary}Resources.{pl,ar,ka,id,sv,ur,hi}.resx`,
`Jube.App/Vue/src/caseRecord/**` (add to `slices.json` vue list), the four edited and one new test file under
`Jube.Tests/Security/Case`, the four edited service test files, the docs and `review/` files above, and the staged
deletions of the research-request files.

## 11. Not covered

Rider lint of all changed C# (timed out; unlinted: every C# file listed in section 4 and the changed tests). No test of
the new action-role predicate against a real database (written, unrun). `CaseChecklistReconciler`, gate and SLA
recorder, the rest of `CaseSummaryService`, `CaseHoldService.LoadAsync`, `DecideStatusDialog`, `DecideHoldDialog`,
`DecideFormsDialog`, `CaseFormEntries`, `CaseDecide` and the Playwright specs were not read. Rating (`Case.Rating`)
update path is in `CaseService`, owned by cases-workspace, not reviewed. `Jube.Svirfd` and virus scanning not
applicable. SPA pl/ar translations not judged. Docs not verified against a running UI.

## 12. Follow-up pass (coordinator request, after the session restart)

State was re-checked from `git status` first; everything below was verified present in the tree, nothing redone. Some of
my new files now show as staged (`A`): I never ran git add, someone else did.

Research deletions (item 6): confirmed pre-existing. `CaseResearchForm.vue`, `CaseNoteResearchRequestDto`,
`CaseNoteCitations`, `CaseNarrativeCitations`, `CaseNoteCitationDto`, `CaseNoteState`,
`CaseNoteResearchRequestDtoValidator` and the two CaseNote exceptions were `AD` (added in the index, absent from the
working tree) in the first `git status` listing of this task, before any edit of mine. I ran no delete, git or rm
command. The only tracked files currently deleted in the working tree that I see are `Jube.App/wire.generated.stamp`,
`Jube.Validations/Authentication/ChangePasswordRequestDtoValidator.cs` and `Jube.App/Vue/src/askjooby/pageContext.ts`, none
of them mine; I deleted no tracked file.

1. Form entry role grant: `CaseWorkflowFormEntryRepository.GetByCaseKeyValueActiveOnlyAsync` and
   `GetCaseWorkflowFormEntryByCaseKeyValueQuery` now require a live `CaseWorkflowFormRole` for a role of the caller on
   the entry's form; the repository tenant test is strict. Insert already checked the grant. Tests:
   `CaseWorkflowFormEntryServiceTests.GetByCaseKeyValueHidesEntriesOfAFormTheCallersRolesLoseAsync`,
   `CaseWorkflowFormEntryByCaseKeyValueServiceTests.GetAsyncHidesAnEntryWhoseFormTheCallersRolesHoldNoGrantOnAsync`; the
   query test fixture and `QueryCaseSeeder.FormEntryAsync` now grant the form role (their cleanup deletes it),
   `CaseWorkflowDisplayExecutionServiceTests` and `PenTestCaseWorkflowSeeder` already did.
2. `CaseRepository`: `GetByIdActiveOnlyAsync` is strict (`tenantRegistryId.HasValue && ==`); `GetAsyncActiveOnlyAsync`
   and `GetByExpiredAsync` keep the tenantless engine mode only when constructed with `CaseRepository(dbContext)`
   (`userName == null`), so a user with no tenant row sees nothing. Callers of the tenantless constructor are the engine
   (`CaseProcessing`, `CaseAutomationStarter`, `TaggingStarter`, `HoldLegRecorder`, `ArchiveService`). Test:
   `CaseNoteExtensionServiceTests.AUserWithRolesButNoTenantMappingSeesNoCaseThroughTheCaseRepositoryAsync`. File touched
   immediately after re-reading it; no other agent had modified it (git status clean for it).
3. Lock (assignment is the lock). Evidence read: `CaseService.UpdateAsync` refuses any edit by a non-holder of a locked
   case (line ~698, 409); `CaseHoldService` refuses release or refusal when another user holds it;
   `CaseChecklistService.LoadHeldCaseAsync` requires the holder, and override requires someone who is not the holder;
   `WorkingLockAllocationUnlock` and memory `case-assignment-is-the-lock`: locked means owned, unlocked is free for all,
   supervisors (permission 18) break or reassign. I found no deliberate exception for notes, files, research or forms:
   no document, test, comment or service text allows an annotation on another user's lock. The engine, narrative job and
   escalation write through repositories, not these services, so they are unaffected; AskJooby tool calls run as the
   caller and now follow the lock. So I enforced: `CaseNoteService.InsertAsync` and `SetFindingAsync`,
   `CaseFileService.UploadAsync` and `RemoveAsync`, `CaseResearchService.CreateAsync`,
   `CaseWorkflowFormEntryService.InsertAsync` throw a new `CaseLockedException` (code `CaseLocked`, one per
   `Exceptions/Repository/<Area>/` folder, message from a new localised `CaseLocked` key) when
   `CaseLockCheck.HeldByAnother` (`Jube.Service/Security/CaseLockCheck.cs`, ordinal compare, same condition as
   `CaseService`). Endpoints answer 409 `{code, message}`. Checklists: left as they are (holder only, already stricter).
   Tags: cases-workspace. Still an owner confirmation (docs TODO): that no reviewer or QA role is meant to annotate a
   case locked to someone else; if one is, it is a one-line exception in `CaseLockCheck`. Tests:
   `CaseNoteExtensionServiceTests.ANoteCannotBeAddedOrMarkedOnACaseLockedToSomeoneElseButCanOnYourOwnOrAFreeOneAsync`,
   `CaseFileNoteServiceTests.AFileCannotBeUploadedToACaseLockedToSomeoneElseAsync`,
   `CaseResearchServiceTests.ResearchCannotBeRecordedOnACaseLockedToSomeoneElseAsync`,
   `CaseWorkflowFormEntryServiceTests.InsertOnACaseLockedToSomeoneElseIsRefusedAndStoresNothingAsync`;
   `CaseNoteSeed.LockToAsync` helper added.
4. File removal: `CaseFileService.EnsureMayRemove`: a holder of permission 18 may always remove; otherwise a case locked
   to another user gives 409 `CaseLocked`; otherwise the uploader or the lock holder may remove; anyone else gets 403
   (`RemovalRestricted`, localised). This follows the supervisor definition in `CaseSupervisionService` (permission 18);
   `CaseEligibleAnalysts` lists who may be assigned a case (permission 1 plus workflow and status roles), which every
   remover already satisfies through `EnsureFileVisibleAsync`, so it adds nothing here. Test:
   `CaseFileNoteServiceTests.AFileCanOnlyBeRemovedByItsUploaderTheLockHolderOrASupervisorAndNeverAgainstAnotherHoldersLockAsync`.
   Existing `PenTestCaseFile` remove tests remove files the same user uploaded, so they are unaffected.
5. File type policy and scanner: `CaseFileTypePolicy` (settings `CaseFileAllowedExtensions` and
   `CaseFileAllowedContentTypes` in `DynamicEnvironment`, env-var overridable, `*` accepts anything),
   `CaseFileContentSniffer` (magic numbers for pdf, images, zip family, OLE, 7z, gzip, tar, rtf, mp3, mp4, wav, plus
   text, and detection of MZ/ELF/Mach-O, shebang, HTML, SVG, PHP, with a UTF-16 BOM path), `ICaseFileScanner` in
   `Interfaces/`, `CaseFileScanRequest`/`CaseFileScanResult`/`CaseFileTypeVerdict` in `Models/`, `NoOpCaseFileScanner`
   default. `CaseFileService.CreateAsync` has a public overload taking the environment and scanner (the endpoint passes
   both; the scanner is resolved with `RequestServices.GetService<ICaseFileScanner>()`, so a deployment registers its
   own in DI). Order in `UploadAsync`: size, extension, copy, sniff, scan, then the note and file are written; refusals
   are 400 with `FileTypeNotAllowed`, `FileContentNotAllowed`, `FileRejectedByScanner`, `FileScanUnavailable` (scanner
   exception fails closed). Defaults: documents, spreadsheets, presentations, text and data, images, archives, email
   formats, common audio and video; executables, scripts, HTML and SVG refused by extension and by content. Documented
   in `docs/Concepts/EnvironmentVariables/index.md` (two rows, defaults equal the code defaults so
   `EnvironmentVariablesDocumentationTests` should hold) and `WorkingUploadingRecallingFiles/index.md`. Tests:
   `CaseFileTypePolicyTests` (unit, 4), `CaseFileServiceTests` (4 new), `DelegateCaseFileScanner` helper,
   `PenTestCaseFile.Upload_ExecutablesScriptsHtmlAndSvg_...`; `Download_SpoofedContentTypes...` now uploads `spoof.pdf`
   with PDF-prefixed content so the download hardening is still exercised; the bidi file name tests now use `.txt`
   names. No scanner implementation ships, so by default no malware scan happens.
   Localisation: new keys `CaseLocked` (CaseNote, CaseFile, CaseResearch), `RemovalRestricted`, `FileTypeNotAllowed`,
   `FileContentNotAllowed`, `FileRejectedByScanner`, `FileScanUnavailable` (CaseFile) in the neutral and all seven
   locale resx, key parity checked by script; `CaseWorkflowFormEntryResources` got `CaseLocked` in the neutral file and
   constants only (SHARED-TODO).
   Also fixed on lint: `CaseLockCheck` used an ambiguous `Case`; the five-argument `CaseFileService.CreateAsync`
   forwarded its token positionally into the new signature.

7. Lint (Rider, `min_severity=error`, small batches; the server timed out often): clean: `CaseFileService`,
   `CaseLockCheck`, `CaseNoteService`, `CaseResearchService`, `CaseNoteRepository`, `CaseFileEndpoints`,
   `CaseNoteEndpoints`, `CaseFileContentSniffer`, `CaseFileTypePolicy` (warnings only: allocation hints, `params byte[]`
   replaced by `params ReadOnlySpan<byte>`; UTF-8 literal hints left). Unlinted (timed out or not attempted after last
   edit): `CaseRepository`, `CaseFileRepository`, `CaseWorkflowFormEntryService` (its only earlier error, the ambiguous
   `Case`, was fixed), `CaseWorkflowFormEntryRepository`, `GetCaseWorkflowFormEntryByCaseKeyValueQuery`,
   `CaseHoldService` (clean in an earlier batch), `CaseResearchEndpoints`, `CaseHoldEndpoints`,
   `CaseChecklistEndpoints`, `CaseWorkflowFormEntryEndpoints`, `DynamicEnvironment.cs`, the four `CaseLockedException`
   files, `NoOpCaseFileScanner`, `ICaseFileScanner`, the three Models records, `CaseNoteSeed` and every changed or new
   test file.

Additional tests to run serially (on top of section 5): `CaseFileTypePolicyTests`, `CaseFileServiceTests`,
`CaseFileNoteServiceTests`, `CaseNoteExtensionServiceTests`, `CaseResearchServiceTests`,
`CaseWorkflowFormEntryServiceTests`, `CaseWorkflowFormEntryByCaseKeyValueServiceTests`,
`CaseWorkflowDisplayExecutionServiceTests`, `CaseWorkflowFormEntryValueServiceTests`, `CaseSuppression*`/`CaseDiary*`/
`CaseSupervision*`/`CaseEscalation*` (they use `CaseRepository`), `EnvironmentVariablesDocumentationTests`,
`TestDocumentationTests`, `PenTestCaseFile`, `PenTestCaseWorkflowFormEntry`,
`PenTestCaseWorkflowFormEntryByCaseKeyValue`, `PenTestCaseByCaseKeyValue`, `PenTestCase`, `PenTestUploadAntiforgery`.
Risks to watch: pen tests that upload files with names or contents outside the new allow-list (I found and adjusted
`PenTestCaseFile` only); seeds that create form entries without a form role (I adjusted the two query seeders;
`ModelScaffold` and `PreservationModelTreeCoverageTests` also insert form entries and were not read).

Translations of the one new form entry string, for the integration pass: pl "Sprawa jest zablokowana przez innego
użytkownika.", ar "القضية مقفلة من قبل مستخدم آخر.", ka "საქმე დაბლოკილია სხვა მომხმარებლის მიერ.", id "Kasus dikunci
oleh pengguna lain.", sv "Ärendet är låst av en annan användare.", ur "کیس کسی دوسرے صارف نے مقفل کر رکھا ہے۔", hi "केस
किसी अन्य उपयोगकर्ता द्वारा लॉक किया गया है।".
