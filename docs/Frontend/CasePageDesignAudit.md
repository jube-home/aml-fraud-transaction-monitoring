# Case page: audit against the v2 design

Written 2026-10-25 from `design_handoff_case_management_v2` (the "What is being built" document, sections 4 to 20 and journeys J1 to J8, and the case-page figures 04 to 34), read against what is built in `Jube.Vue`. Details of sections 16 to 20 were read from headings and figures only, so verify each when it is built. "Planned" means a labelled placeholder is on the page.

## Owner decisions that change the design

| Decision | Effect on the design |
|---|---|
| Built (2026-10-26): hold is identified from the response elevation by a Response Elevation Range with Hold or Decline, which stamps a `hold` element in the Archive payload with its own versioning; no separate table. Release and Refuse are done in the Held payment lightbox of the Decide pane through a Case Workflow Release Hook (a web hook chosen by a payload field, literal host, credential in a `JUBE_HOOK_` environment variable, only a 200 releases), every call audited and stamped into the `hold` legs. The journal and the transaction drawer show the state read-only. | Replaces the per-market `PaymentHoldPolicy` row (D-V18) and the step-up and core-banking round trip in 16.2 to 16.4 for now. Release and Refuse step-up, "above your limit" approval and hold expiry email are deferred. |
| Activations move under the Case Key Journal and cover the full history from the Archive JSON, not only the activations that created the case. | Section 2 stops being "the case's activations"; the journal gains an activation lens. The case's own activations remain as the reason the case is in the queue. |
| Case Displays are an alternative case detail view, chosen beside the payload fields. | Built: a view chooser in section 1; a display renders in a sandboxed frame with scripts off. |
| Macros sit under Decide. | Built. The server runs the macro's notification and HTTP call. The stored JavaScript the old page ran with `eval` is not run. |
| Context-aware checklists are their own feature. | Placeholder card in the right column. |
| Forms will be lifted as Visualisation was, to allow Vue embedding. | Placeholder section. |
| The Case Key Journal is ported properly and greatly extended, with analytics computed by the endpoint. | Interim plain grid now; see `CaseKeyJournalAnalytics.md`. |

## Page structure

| Design item (section) | Status |
|---|---|
| Three columns: key panel, sections, Decide (8.1) | Built; the key panel sits above a section index |
| Six numbered sections as one scroll (4, 8) | Built as seven (Forms added), collapsible, URL deep link, scrolls inside the card |
| Sticky header: risk chip, lock, diaries, presence (8.1) | Partial: status, lane and priority, lock. Diary meter is in Decide. Presence missing |
| Header variants: live money countdown, mandatory floor, cold start versus calibrated, priority override (8.3) | Not yet |
| Case detail views: Why, At stake, Similar cases, Ranked (8.1) | Built except Similar cases, which counts the key's earlier closed cases and awaits the AskJooby version |
| Case-level drivers with Journal links and feedback flags (8.1) | Partial: contributions and waterfall; no chips, no flags |
| Risk popover on `R` and "your assessment" (8.2) | Planned |
| Keyboard shortcuts and command palette (7.3) | Not yet |
| Live state: new activation banner, changed-while-open lightbox, connection lost (8.5) | Not yet; the change stream exists and the page refetches on it |
| First paint from one `Anchor` read under 300 ms (8.1, CH-00) | Not yet; the page assembles several existing reads |
| State catalogue: loading, empty, failed, stale, restricted (15.2) | Loading, empty and failed in each section; stale and restricted not yet |

## Sections

| Section | Design | Status |
|---|---|---|
| 1 Case detail views | Narrative lines, drivers, payload table with conditional colours | Built, plus the view chooser for Case Displays |
| 2 Activations | A card per activation: anchor transaction, calibrated probability, exact waterfall, sensitivity, health, rule path, sanctions detail, suppress (9, 21) | Names only; planned as cards, and moving under the Journal as above |
| 3 Journal | Aggregation strip, daily chart, sparkline, grid; view by Rows, Beneficiary, Device, IP; All, Top, Activations only; Around this, Merge, Pin lens; tags and bulk tagging; transaction card and payload drawers (10, 12) | Built: lenses, window, charts and strip from the server, grid with export, pivot, transaction card and payload drawer, tags and Tag all matching with undo, Merge lanes, Pin and Pinned only, Around this and peer comparison. Not built: the daily chart's normal line, View by (Explode) and the Network |
| 4 Network | Graph of at most 150 nodes with a shared selection with the Journal; Explode list with sparklines (11) | Placeholder; graph analysis is being specified separately |
| 5 Notes | Action code, result chips with diary effect, undo, quiet system notes; Compose with facts tray and scorecard; AskJooby; web research (13, 20.4) | Plain notes list and a composer in Decide (action, priority, text). Result chips, undo, Compose and research planned |
| 6 Case history | Typed events with hash chain and verify, risk history, prior cases, read audit with sensitive-account reason (14) | Events, earlier cases, stays and risk history built. Hash chain and read audit planned |

## Decide column

| Design item | Status |
|---|---|
| Suggested next step with AskJooby and "why this suggestion" (8.1, 13.3) | Planned |
| Action code with note | Built in simplified form |
| Close lightbox with server Preview, outcome, typology, rationale, undo toast (15.1, J3) | Simplified: confirm, then close. Preview, outcome, typology and undo not yet |
| Escalate, change status | Built as a status move |
| Take, release, break lock with typed reason and step-up (J6, 15.1) | Take and release built; break lock exists for supervisors on the Supervision tab, not yet here |
| Reassign | Supervisors only, on the Supervision tab |
| Checklist (8.1) | Placeholder, standalone feature |
| Diaries with six states (8.4) | Typed diary editor built; breached, paused and hold-expiring states not yet |
| Macros | Built |
| File a report with maker-checker approval, approver view, Inbox, report status (J4, 19) | Planned |
| Held payment card and decision (16, J5) | Planned, on the owner's elevation-inference design |
| Request for information (20.3) | Planned |
| AskJooby docked panel on `Alt+J` (13.3) | Not yet |

## Stand-out features not yet on the page

Each is placed where it will appear, as a labelled placeholder: similar cases (AskJooby), the risk working and your assessment, activation cards and suppress from an activation, model facts, journal lenses, tags and drawers, Explode, Compose, web research, hash chain, read audit, suggested next step, held payment, file a report, request information and the checklist.

## Beyond the case page, in the same design

Team view and wall (7.1, partly the Supervision tab), Inbox (7.2), command palette (7.3), inbound holds, recall and payee name checks (17), APP claims and support needs (18), referrals and report status (19), agent triage with a work log, QA sampling and review, automation recipes and rule tuning (20), suppressions and lists (21), configuration hub (22). QA sampling and part of supervision already exist on the Cases page.

## Order of work from here

1. Journal: port properly with the server analytics, the activation lens and the full activation history.
2. Activation cards and the Anchor read.
3. Hold and release on the inference-table design.
4. The Close operation with preview and outcome, and break lock here.
5. Header states and live case state.
6. Compose, narrative and approvals.
