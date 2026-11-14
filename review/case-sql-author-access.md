# Case SQL: author access note and approval gate check

## Summary and verdict

Definer rights kept (owner decision). A visible, tokenised note now sits beside the SQL box in `CaseSqlEditor.vue`. Docs
updated with TODO markers. Approval gate check result: NO case workflow child kind is gated by maker-checker; every
SQL-carrying child goes live on save. Closing it is a large design change (about eleven new approval kinds across data,
service, engine/case loading, UI, tests, migrations), so it was NOT built; plan below. Nothing was built or run (owner
builds).

## What changed

- `Jube.App/Vue/src/components/CaseSqlEditor.vue`: one `<p role="note" class="alert alert-info alert-soft">` using the
  pattern from `AskJoobyPage.vue`, key `caseSql.authorAccess`, testid `case-sql-author-access`. The editor is shared by
  the Display, Form, Macro, Action and Status pages (`EditorField` kind `caseSql`), so the note shows on all five;
  wording ("everyone who can open a case in this workflow") is true for each. FormValidator and checklist-item SQL have
  no editor of this component.
- `Jube.App/Vue/e2e/crud.spec.ts`: one assertion that the note is visible, beside the existing `case-sql-run` assertions (no
  existing expectation changed).
- `review/case-sql-note.locale-proposals.json`: key `caseSql.authorAccess` in en, pl, ar, ka, id, sv, ur, hi and fr
  (formal register). No plurals, no `{n}` placeholders; `{'@'}` escapes as in the earlier proposals. Shared locale JSON
  untouched; `fr.json` does not exist yet, so the integration pass creates it. No server resx keys added (no `.fr.resx`
  work).
- Docs: `docs/Frontend/DatasourceTemplateDialect.md` (replaced the open TODO with the definer-rights statement and "who
  should author case SQL"), appended a "Case SQL runs with the author's access" section to
  `docs/Configuration/CaseManagement/CreatingCaseWorkflowDisplay/index.md` and `CreatingCaseWorkflowForm/index.md`. TODO
  (docs) and TODO (screenshot) markers are in each.

## Gate matrix (from code)

Evidence: `EntityApprovalKind` (Jube.Data/Repository/Models) has 21 kinds, all model-side (EntityAnalysisModel and its
children, Exhaustive, AskJooby analytics, Svirfd); none is a CaseWorkflow kind. No case workflow service, repository,
DTO or the Jube.Case/Jube.Engine case paths reference approval (grep). The case-side repositories load live rows by
`Active == 1` and `Deleted` only (for example `CaseWorkflowDisplayRepository.GetByIdActiveOnlyAsync`); versioning is the
`*Version` history plus optimistic concurrency, not approval. `CaseSuppression` has its own approval but carries no SQL.

| Child kind            | Carries SQL                | Gated by second-person approval |
|-----------------------|----------------------------|---------------------------------|
| Display               | Sql                        | No                              |
| Form                  | Sql                        | No                              |
| Macro                 | Sql                        | No                              |
| Action                | Sql                        | No                              |
| Status                | Sql                        | No                              |
| FormValidator         | Sql                        | No                              |
| ChecklistItem         | ShowSql, ValidateSql       | No                              |
| Filter                | no SQL (filter expression) | No                              |
| ReleaseHook           | hook endpoint, no SQL      | No                              |
| XPath                 | no SQL                     | No                              |
| Checklist             | no SQL                     | No                              |
| CaseWorkflow (parent) | no                         | No                              |

Only control today: permission 67 to write or test SQL, plus save-time vetting against the author's tables. Hooks,
macros and status/action SQL also drive outbound email, SMS and HTTP.

## Not closed, and why

Each kind would need: new `EntityApprovalKind` values, approval state per version (the model kinds use
`EntityApprovalRepository` rows keyed by kind, entity id, version), versioned repositories that expose "approved
version" reads, service changes in about eleven services, the Jube.Case and Engine loaders and the display/form
execution services switching to approved versions, approval subject projections and pipeline order
(`EntityApprovalSubjectProjection`, `EntityApprovalPipelineOrder`, `EntityApprovalGraphKind`,
`EntityApprovalChangeSources`), the approvals UI, migrations and backfill. This is not a small change following one
sibling pattern.

## Recommended plan (owner decision)

1. Scope first to the seven SQL-carrying kinds (Display, Form, Macro, Action, Status, FormValidator, ChecklistItem);
   then Filter, ReleaseHook as they dispatch outbound.
2. Add kinds 22 onward to `EntityApprovalKind`; add them to `EntityApprovalSubjectProjection`, `GetApprovedEntityQuery`,
   pending/rejected id queries, preflight in `EntityApprovalService.CheckPreflightAsync`.
3. Repositories: add approved-version reads (mirror `EntityAnalysisModelSvirfdRepository`); switch `GetBy...ActiveOnly`
   callers in `CaseWorkflowDisplayExecutionService`, case creation/escalation (`CaseStatusNotifier`),
   `FormSubmissionValidator`, `CaseWorkflowChecklistService` to them.
4. Services and endpoints: sibling of `EntityAnalysisModelSvirfdService` approval wiring and `VersionHistoryEndpoints`
   entries.
5. Migration (range 20263108640001 to 20263108640099, empty Down ()): backfill every existing live row's current version
   as Approved with the seeded-administrator CreatedUser convention.
6. Tests: service tests beside `EntityApprovalServiceTests` per kind, `MakerCheckerEngineTests` equivalents, Case
   execution tests proving an unapproved edit is not executed, PenTest bases per kind. Existing tests that save SQL and
   then execute it will need an approval step or `ApprovalsRequired` set low.
7. UI: approvals page entries, editor shows "pending approval" state; update the docs TODOs added here.

## Tests

No existing test changes expectation. Added: e2e assertion in `Jube.App/Vue/e2e/crud.spec.ts` (next to the case-sql-run
checks). No unit test for the component exists (only `caseSqlApi.spec.ts`), none added. To run: Playwright
`crud.spec.ts`.

## Risks

- Note shows on Macro, Action, Status editors too (shared component); intentional.
- The e2e test and the new key need the integration pass (en.json, other locales, fr.json) or the key renders raw.
- Until approval is built, one person with permission 67 can publish SQL live to all roles in a workflow.

## Docs TODO markers

Added in `docs/Frontend/DatasourceTemplateDialect.md` (near the replaced TODO) and at the end of both Display and Form
docs pages (wording check, approval statement, screenshot).
