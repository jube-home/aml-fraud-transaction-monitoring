---
layout: default
title: Smoke Test Seed Data
nav_order: 27
parent: Testing
---

# Smoke Test Seed Data

A demonstration installation ships with a deliberately awkward set of cases, so that a person smoke testing case management does not have to manufacture each state by hand. There are 191 named scenarios across 16 groups, 242 cases in all, on the example workflow and two purpose-built workflows, plus about 800 cases on a queue-volume workflow for paging and supervision. Every scenario is a row in a database migration, so the set is the same on every installation that has the demonstration model, and it is gone the moment the data is.

The set covers the gates a case can hit (a lock held by someone else, an incomplete checklist, an override that no longer applies, a role that cannot see the status), the awkward states a case can be left in (closed with no events, locked with no holder, suspended with a diary that has already come due) and awkward data (right-to-left keys, a 100,000 character note, a one megabyte payload). It reads like a checklist because it is one: work down a group, open each case, and compare what you see with the Expect column.

## How to find a scenario

Every seeded case has a case key value that starts with `SMK-` and ends with its scenario code, for example `SMK-CL-ALL-OPEN-01`. Type the scenario code into the search of the Cases page or the case key journal, or open the case directly by its key. A few scenarios deliberately have no searchable key (an empty key, a missing key); those are the `ED-KEY-EMPTY` and `ED-KEY-NULL` cases and are found by workflow and age instead. The queue-volume cases are `SMK-VOL-0001` upward.

The Expect column states what the code intends, drawn from the case management services and the Cases page. It is a smoke-test oracle, not a guarantee: where what you see differs, that difference is the finding.

## What the seed creates

| Item | Purpose |
|---|---|

| Workflow `Detailed Account Financial Transactions Cases` | The example workflow. It gains four statuses so visibility can be tested: `Smoke: Admin Only` (granted to Administrator only), `Smoke: Ungranted` (granted to no role), `Smoke: Inactive` (active off) and `Smoke: Deleted` (soft deleted). |
| Workflow `Smoke: Role Gated` | Granted to the analyst and supervisor roles but not to Administrator, so its cases are invisible to Administrator. |
| Workflow `Smoke: Queue Volume` | Two statuses, tight service level targets, a daily goal of 25 and about 800 cases, for the queue, Take next, Urgent and Supervision views. |
| Role `Smoke: Case Analyst` | Permissions 1 and 2 only. Sees every status of the example workflow except `Smoke: Admin Only`. |
| Role `Smoke: Case Supervisor` | Permissions 1, 2, 18, 62, 68, 70, 71, 75 to 79. |
| Two release hooks on the example workflow | One pointing at an address that refuses connections, so a release shows the hook failure path; one for ATM payloads with no release address. |
| Workflow filter `Smoke: Broken filter` | References a column that does not exist, so the filter rail shows its failed badge while the rest of the page keeps working. |
| A failed preservation snapshot | Shows the error state in the preservation page. |

The Administrator account holds every permission, so any gate that is about a permission being absent can only be tested with a second user. The seed deliberately creates no users, because a seeded account would carry a known password. Create two users yourself under Administration and give them the two smoke roles; then sign in as each to test the permission gates (a user with permission 1 and not 18 has no Supervision tab; a user without 71 cannot grant a checklist override; a user without 76 is blocked from closing a case with a report awaiting approval).

## Background workers and seeded rows

Several workers act on cases while the application runs, and a few of them rewrite what was seeded. The seed is built to survive them, and these are the cases where you will see them at work.

| Worker | Effect on the seed |
|---|---|
| Risk sweep | Every seeded risk figure is pinned by setting the risk checked date ten years ahead, so the sweep skips it. The exceptions are `RK-LANE-NULL`, which is meant to be scored by the sweep within minutes of boot. |
| Case automation | Acts on suspended cases whose diary has come due. `SUSPEND-DIARY-DUE` is reopened within about a minute of the first boot, and writes a process expired event. Reseed to see it again. |
| Checklist reconciler | Runs the moment a case is touched. It creates the missing items for `CL-NONE-DEFINED`, and hides the item with no definition in `CL-ORPHAN-ITEM`. Both are the intended behaviour. |
| Escalation sweep | No status on the example workflow has an escalation rule, so it moves nothing. The escalation scenarios carry the history of an escalation, not a pending one. |
| Service level and checklist SLA sweeps | Will stamp breaches on cases that are over target. The seed already carries some breach rows; the sweeps may add their own. |

## Scenarios

### State and closing

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `OPEN-FRESH` | 5 | Example | Newly opened, untouched, unlocked | In All cases and Free to take; first-view and ownership clocks running; a Take next candidate. |
| `OPEN-WORKED` | 3 | Example | Open, held by Administrator, first view and lock recorded | In Mine; Decide offers release and close. |
| `CLOSED-R1` | 2 | Example | Closed with reason 1: False positive known activity | Closed badge; reason and note shown; Reopen offered; counts in outcome analytics by the status outcome. |
| `CLOSED-R2` | 2 | Example | Closed with reason 2: False positive rule too broad | Closed badge; reason and note shown; Reopen offered; counts in outcome analytics by the status outcome. |
| `CLOSED-R3` | 2 | Example | Closed with reason 3: Duplicate | Closed badge; reason and note shown; Reopen offered; counts in outcome analytics by the status outcome. |
| `CLOSED-R4` | 2 | Example | Closed with reason 4: Insufficient evidence | Closed badge; reason and note shown; Reopen offered; counts in outcome analytics by the status outcome. |
| `CLOSED-R5` | 2 | Example | Closed with reason 5: Suspicion confirmed and reported | Closed badge; reason and note shown; Reopen offered; counts in outcome analytics by the status outcome. |
| `CLOSED-R6` | 2 | Example | Closed with reason 6: Fraud confirmed loss prevented | Closed badge; reason and note shown; Reopen offered; counts in outcome analytics by the status outcome. |
| `CLOSED-R7` | 2 | Example | Closed with reason 7: Customer explained | Closed badge; reason and note shown; Reopen offered; counts in outcome analytics by the status outcome. |
| `CLOSED-R8` | 2 | Example | Closed with reason 8: Other | Closed badge; reason and note shown; Reopen offered; counts in outcome analytics by the status outcome. |
| `CLOSED-NO-REASON` | 2 | Example | Closed with no reason and no note | Closes without a reason are legal; analytics count it as closed without an outcome reason. |
| `CLOSED-NOTE-1000` | 1 | Example | Closed note of exactly 1,000 characters | Longest note the contract allows renders without truncation. |
| `CLOSED-NOTE-BLANK` | 1 | Example | Closed note of whitespace only | Treated as no note when the case is next saved; renders as empty. |
| `CLOSED-NO-EVENTS` | 1 | Example | Closed with no events and no status transitions | Opens without error; time-in-status and first-view analytics show a dash. |
| `CLOSED-NULL-DATES` | 1 | Example | Closed with no closed date and no closed user | Excluded from closure throughput and median time to close; still counts as closed. |
| `CLOSED-NEGATIVE-DURATION` | 1 | Example | Closed before it was created | Durations clamp to zero; no negative bar or error on the case page. |
| `CLOSED-STATUS-NULL` | 1 | Example | ClosedStatusId is NULL | Invisible to every queue, view and sweep because the open test fails; reachable only by direct id. |
| `REOPENED` | 2 | Example | Closed then reopened | In All cases with a reopen counted in Quality; Returned to me for a viewer who saw it before. |
| `SUSPEND-OPEN` | 1 | Example | Suspend Open (1) with a future diary | Header shows Closed yet still counts open and appears in All cases; API-only state. |
| `SUSPEND-CLOSED` | 1 | Example | Suspend Closed (2) with a future diary | Excluded from every view; header shows Closed; automation closes it when the diary passes. |
| `SUSPEND-BYPASS` | 1 | Example | Suspend Bypass (4) with a future diary | Excluded from every view; automation closes it when the diary passes. |
| `SUSPEND-DIARY-DUE` | 1 | Example | Suspend Open with a diary already due | The automation sweep reopens it within about a minute and writes event 15; reseed to see it again. |
| `STATUS-SUPERVISOR` | 3 | Example | Open in Supervisor Review | Visible with the Supervisor Sign-off checklist; moves back to First Line Review. |
| `STATUS-RESTRICTED` | 2 | Example | Open in Restricted | Restricted status with no checklist and no dwell limit. |
| `STATUS-FRAUDULENT-OPEN` | 2 | Example | Open in Fraudulent | Status with the Fraud Confirmation checklist; outcome Confirmed counts at once. |

### Locks and assignment

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `LOCK-MINE` | 3 | Example | Held by Administrator | In Mine; Take next skips it; release works. |
| `LOCK-OTHER` | 3 | Example | Held by another analyst | Edits and close return 409 locked to another user; supervisor can break the lock. |
| `LOCK-STUCK-3H` | 3 | Example | Held by another analyst, idle three hours | Listed under Supervision stuck locks at the 120 minute setting. |
| `LOCK-STUCK-30H` | 2 | Example | Held by another analyst, idle thirty hours | Listed at every idle setting up to 1,440 minutes. |
| `LOCK-FRESH` | 1 | Example | Locked ten minutes ago by another analyst | Not stuck at any idle setting. |
| `LOCK-ORPHAN` | 2 | Example | Locked with no lock holder | Not in Mine or Free to take; Take next skips it; counted unassigned; break lock still works. |
| `LOCK-NOBODY` | 2 | Example | Not locked but a lock user is recorded | Counts as nobody holds; included in Free to take. |
| `LOCK-NULL` | 2 | Example | Lock flag is NULL | Treated as free; included in Free to take. |
| `LOCK-GHOST` | 2 | Example | Held by a user who does not exist | Shows Held by the name; only break lock or reassign clears it. |
| `LOCK-NULL-DATE` | 1 | Example | Locked with no lock date | Stuck-lock age falls back to the newest event or the created date. |
| `LOCK-ON-CLOSED` | 2 | Example | Closed while held by Administrator | Not in Mine because it is not open; reads Held by you; Take is disabled. |
| `LOCK-RTL` | 1 | Example | Held by a user with an Arabic name | Right-to-left text in the lock badge and supervision workload without overlap. |

### Visibility by role

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `RV-ADMIN-ONLY` | 2 | Example | In a status granted to Administrator only | Visible to Administrator; hidden from the Smoke Case Analyst role. |
| `RV-UNGRANTED` | 2 | Example | In a status no role is granted | Hidden from every user; 404 on a direct link; still counted nowhere in the UI. |
| `RV-INACTIVE-STATUS` | 2 | Example | In an inactive status | Visible; the status is not offered as a move target. |
| `RV-DELETED-STATUS` | 2 | Example | In a deleted status | Dropped from lists and 404 on a direct link; the row remains. |
| `RV-GATED-WORKFLOW` | 3 | Role Gated | In a workflow Administrator is not granted | Absent from the workflow picker for Administrator; visible to the Smoke Case Analyst role. |

### Checklist gates

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `CL-NONE-DEFINED` | 2 | Example | Open in First Line Review with no checklist rows | Items appear on first touch from the definitions; the gate evaluates after reconciliation. |
| `CL-ALL-OPEN` | 2 | Example | All four items open, held by Administrator | Move and close return 409 ChecklistIncomplete naming the open items; Checklist items open chip. |
| `CL-ALL-DONE` | 2 | Example | All four items done | Gate passes; move and close succeed. |
| `CL-PARTIAL` | 1 | Example | Two done, two open | Blocks with the two open names. |
| `CL-SLA-BREACHED` | 2 | Example | Open item past due with the breach stamped | In Checklist overdue; breach event shown; Breached badge on the item. |
| `CL-DUE-NOT-STAMPED` | 1 | Example | Open item past due, breach not yet stamped | Still in Checklist overdue (due date test); the sweep stamps it later. |
| `CL-DONE-LATE` | 1 | Example | Item completed after it was due | Completed late badge; breach recorded. |
| `CL-NULL-DUE` | 1 | Example | Open item with no due date | No SLA badge; still blocks the gate. |
| `CL-OVERRIDE-LIVE` | 1 | Example | Open items and a live override granted by a supervisor | Administrator can move despite open items; the override is consumed on use. |
| `CL-OVERRIDE-OWN` | 1 | Example | Open items and an override granted by Administrator | Still blocked for Administrator; the grantor cannot use their own override. |
| `CL-OVERRIDE-STALE` | 1 | Example | Override older than the last status move | Ignored; the gate blocks again. |
| `CL-OVERRIDE-CONSUMED` | 1 | Example | Override already used | Ignored; the gate blocks again. |
| `CL-OVERRIDE-WRONG-STATUS` | 1 | Example | Override granted for a different status | Ignored; the gate blocks. |
| `CL-ORPHAN-ITEM` | 1 | Example | Shown item with no matching definition | Hidden by the reconciler the first time the case is touched. |
| `CL-WRONG-STATUS-ITEM` | 1 | Example | Item recorded against another status | Does not block moves from this status. |
| `CL-SUPERVISOR-OPEN` | 1 | Example | Supervisor Review with the sign-off item open | Blocks the move out of Supervisor Review. |
| `CL-FRAUD-FINDING` | 1 | Example | Fraudulent status with the finding item open | Blocks close until a finding note is recorded. |

### Legal hold

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `LH-NORMAL` | 2 | Example | Open case on legal hold | Hold banner with reason, user and date; erasure refused. |
| `LH-FILING` | 1 | Example | Closed case held by a filed report | Lifting needs the filing approve permission; read needs a filing view permission. |
| `LH-LIFTED` | 1 | Example | Hold placed then lifted | No hold shown; history has events 32 and 33. |
| `LH-NO-DETAIL` | 1 | Example | Hold flag with no reason, user or date | Banner shows a hold with no detail and does not break. |
| `LH-ON-CLOSED` | 1 | Example | Closed case on legal hold | Reopen is allowed; the retention purge never removes the transactions. |
| `LH-LIFT-EVENT-FLAG-ON` | 1 | Example | Lift event recorded but the flag is still on | History says lifted while the banner still shows a hold: the anomaly to confirm the page does not hide. |

### Diary and payment hold

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `DI-T1-PAST` | 1 | Example | Review diary, overdue | Lane 3 when due or within two days, otherwise 4; Returned to me when past and the viewer has seen it. |
| `DI-T1-SOON` | 1 | Example | Review diary, due within a day | Lane 3 when due or within two days, otherwise 4; Returned to me when past and the viewer has seen it. |
| `DI-T1-FAR` | 1 | Example | Review diary, far in the future | Lane 3 when due or within two days, otherwise 4; Returned to me when past and the viewer has seen it. |
| `DI-T2-PAST` | 1 | Example | Customer response diary, overdue | Lane 3 when due or within two days, otherwise 4; Returned to me when past and the viewer has seen it. |
| `DI-T2-SOON` | 1 | Example | Customer response diary, due within a day | Lane 3 when due or within two days, otherwise 4; Returned to me when past and the viewer has seen it. |
| `DI-T2-FAR` | 1 | Example | Customer response diary, far in the future | Lane 3 when due or within two days, otherwise 4; Returned to me when past and the viewer has seen it. |
| `DI-T3-PAST` | 1 | Example | Regulatory deadline diary, overdue | Lane 2 always; Returned to me when past and the viewer has seen it. |
| `DI-T3-SOON` | 1 | Example | Regulatory deadline diary, due within a day | Lane 2 always; Returned to me when past and the viewer has seen it. |
| `DI-T3-FAR` | 1 | Example | Regulatory deadline diary, far in the future | Lane 2 always; Returned to me when past and the viewer has seen it. |
| `DI-T4-PAST` | 1 | Example | Payment hold diary, overdue | Lane 1 always; Returned to me when past and the viewer has seen it. |
| `DI-T4-SOON` | 1 | Example | Payment hold diary, due within a day | Lane 1 always; Returned to me when past and the viewer has seen it. |
| `DI-T4-FAR` | 1 | Example | Payment hold diary, far in the future | Lane 1 always; Returned to me when past and the viewer has seen it. |
| `DI-NULL-DATE` | 1 | Example | Diary on with no diary date | Violates the contract; the next save returns DiaryDateNotEmpty (400). |
| `DI-DATE-NO-FLAG` | 1 | Example | Diary date set with the diary off | Date ignored by the lane; type resets to Review on the next update. |
| `DI-TYPE-INVALID` | 1 | Example | Diary type outside the defined values | Lane falls back to Review behaviour; label shows unknown. |
| `PH-HELD` | 1 | Example | Payment held, diary due in the future | Hold card with Release and Refuse; Live money lane; release needs permission 68. |
| `PH-HELD-OVERDUE` | 1 | Example | Payment held past its deadline | Live money lane with the deadline breached. |
| `PH-DECLINED` | 1 | Example | Held payment declined | Hold card shows Declined and the decision. |
| `PH-RELEASED` | 1 | Example | Held payment released | Hold card shows Released with the hook status. |
| `PH-REFUSED` | 1 | Example | Held payment refused | Hold card shows Refused. |
| `PH-NO-DIARY` | 1 | Example | Held payment but the diary is off | Hold card shows from the archive; lane is not Live money. |
| `PH-UNKNOWN-STATE` | 1 | Example | Hold with an unrecognised state id | Card renders without a decision control. |

### Service levels

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `SL-ON-TRACK` | 2 | Example | Well inside every target | On track on all three clocks. |
| `SL-AT-RISK` | 2 | Example | Resolution target 85% elapsed | At risk badge; in the pulse at risk count. |
| `SL-BREACHED` | 2 | Example | Resolution target exceeded | Breached; in Overdue; breach row on record. |
| `SL-FIRST-VIEW-BREACH` | 1 | Example | Never viewed after the first-view target | First view breached; no first-view event. |
| `SL-OWNERSHIP-BREACH` | 1 | Example | Viewed but never owned after the ownership target | Ownership breached; first view met. |
| `SL-DWELL-BREACH` | 1 | Example | In First Line Review past the dwell limit | Status dwell breached; resolution not yet breached. |

### Risk and ranking

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `RK-LANE-1` | 1 | Example | Risk lane 1 with priority 75 | Sorted by lane then priority in the triage order; Urgent chip for lanes 1 to 3. |
| `RK-LANE-2` | 1 | Example | Risk lane 2 with priority 60 | Sorted by lane then priority in the triage order; Urgent chip for lanes 1 to 3. |
| `RK-LANE-3` | 1 | Example | Risk lane 3 with priority 45 | Sorted by lane then priority in the triage order; Urgent chip for lanes 1 to 3. |
| `RK-LANE-4` | 1 | Example | Risk lane 4 with priority 30 | Sorted by lane then priority in the triage order; Urgent chip for lanes 1 to 3. |
| `RK-LANE-NULL` | 1 | Example | Not yet assessed (lane NULL) | Shows Not yet assessed; the risk sweep scores it shortly after boot. |
| `RK-PRIORITY-NULL` | 1 | Example | Lane set, priority NULL | Sorts last on priority. |
| `RK-PRIORITY-NEGATIVE` | 1 | Example | Negative priority | Sorts below zero without error. |
| `RK-LIKELIHOOD-ZERO` | 1 | Example | Likelihood 0 | Calibration bucket zero. |
| `RK-LIKELIHOOD-ONE` | 1 | Example | Likelihood 1 | Calibration bucket one. |
| `RK-VALUE-HUGE` | 1 | Example | Value of one trillion | Value column formats without overflow. |
| `RK-VALUE-NULL` | 1 | Example | Value NULL | Dash in the value column; sorts last. |
| `RK-LANE-INVALID` | 1 | Example | Lane outside 1 to 4 | Treated as unassessed or standard without error. |
| `RK-ASSESSMENT-MISSING` | 1 | Example | Risk columns set but no assessment history | Risk panel shows current figures with no history. |
| `RK-RISING` | 1 | Example | Priority rose by 40 since last viewed | Returned to me and Priority rose attention for Administrator. |

### Quality review

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `QA-PICKED-OPEN` | 2 | Example | Picked for quality review, still open | In the Quality review chip. |
| `QA-PICKED-CLOSED-OTHER` | 2 | Example | Picked and closed by another analyst | Administrator can record a review. |
| `QA-PICKED-CLOSED-OWN` | 2 | Example | Picked and closed by Administrator | Review refused: the reviewer cannot be the closer (409). |
| `QA-REVIEWED` | 1 | Example | Picked, closed and already reviewed twice | Review history shows two reviews; another is allowed. |
| `QA-REVIEWED-AGENT` | 1 | Example | Reviewed with an agent verdict | Review shows both analyst and agent verdicts against a triage run. |
| `QA-NOT-PICKED` | 1 | Example | Closed and not picked | Review not offered. |
| `QA-PICKED-SUSPENDED` | 1 | Example | Picked while suspended | Leaves the Quality review chip because the case is not open. |

### Triage recommendations

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `TR-PENDING` | 1 | Example | Triage recommendation pending | Recommendation card shows the pending state. |
| `TR-ACCEPTED` | 1 | Example | Triage recommendation accepted | Recommendation card shows the accepted state. |
| `TR-OVERRIDDEN` | 1 | Example | Triage recommendation overridden | Recommendation card shows the overridden state. |
| `TR-SUPERSEDED` | 1 | Example | Triage recommendation superseded | Recommendation card shows the superseded state. Superseded shows no card. |
| `TR-INCONCLUSIVE` | 1 | Example | Pending recommendation with outcome inconclusive | Accept does not open the close dialog. |
| `TR-NOT-CONFIRMED` | 1 | Example | Pending recommendation not confirmed | Accept prefills the close note and opens the close dialog, so every close gate applies. |
| `TR-ON-CLOSED` | 1 | Example | Pending recommendation on a closed case | Accept must not open close. |
| `TR-LONG-EVIDENCE` | 1 | Example | Ten evidence items and a long rationale | Card wraps evidence; nothing overflows. |

### Suppression requests

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `SU-PENDING` | 1 | Example | Suppression request pending | Request shows the pending state with its approval history. |
| `SU-APPROVED` | 1 | Example | Suppression request approved | Request shows the approved state with its approval history. |
| `SU-REJECTED` | 1 | Example | Suppression request rejected | Request shows the rejected state with its approval history. |
| `SU-RETRACTED` | 1 | Example | Suppression request retracted | Request shows the retracted state with its approval history. |
| `SU-PENDING-LAPSED` | 1 | Example | Pending request created 15 days ago | Shown as lapsed; absent from the Approvals panel. |
| `SU-PENDING-EXPIRED` | 1 | Example | Pending request whose expiry has passed | Cannot be approved; refused as expired. |
| `SU-OWN-REQUEST` | 1 | Example | Pending request raised by Administrator | Administrator cannot approve their own request (409). |
| `SU-APPROVED-REMOVED` | 1 | Example | Approved but the applied override is gone | Standing resolves to Removed. |
| `SU-APPROVED-EXPIRED` | 1 | Example | Approved and since expired | Standing resolves to Expired. |
| `SU-FORCE-TARGET` | 1 | Example | Pending request with a force target | Approval needs permission 62. |
| `SU-NO-TARGETS` | 1 | Example | Approved request with no targets | Renders with an empty target list. |

### Notes, tags, links and assessments

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `NT-NONE` | 1 | Example | No notes | Empty notes state. |
| `NT-MANY` | 1 | Example | Thirty notes | Notes list paginates or scrolls; newest first. |
| `NT-LONG` | 1 | Example | A note of exactly 100,000 characters | Longest legal note renders; the page stays responsive. |
| `NT-UNICODE` | 1 | Example | Arabic, Hebrew, emoji and zero-width text | Direction and wrapping correct; no overlap. |
| `NT-NARRATIVE` | 1 | Example | Narrative with three versions | Latest version is current; history shows the chain. |
| `NT-DOCUMENT` | 1 | Example | Document note | Document kind label. |
| `NT-FINDING` | 1 | Example | Note marked as a finding | Finding badge; satisfies the Record the finding item. |
| `NT-HTML` | 1 | Example | Note containing HTML and script | Rendered sanitised; the script never runs. |
| `NT-AGENT-SYSTEM` | 1 | Example | Notes written by an agent and by the system | Source badges differ from a user note. |
| `NT-NO-ACTION` | 1 | Example | Note with no action | Renders with a blank action label. |
| `TG-VOCABULARY` | 1 | Example | All five vocabulary tags | Tag chips for every tag. |
| `TG-DELETED` | 1 | Example | A soft-deleted tag | Deleted tag is not shown. |
| `TG-UNICODE-DUPLICATE` | 1 | Example | A unicode tag and a duplicate tag | Chips render; duplicate collapses on merge. |
| `TG-FIFTY` | 1 | Example | Fifty tags | Tag area wraps without overflow; merge refuses past 50. |
| `LK-PAIR` | 2 | Example | Two cases linked | Link shown from both sides. |
| `LK-HIDDEN` | 1 | Example | Linked to a case Administrator cannot see | Link silently omitted. |
| `LK-ORPHAN` | 1 | Example | Linked to a case id that does not exist | Link omitted; no error. |
| `LK-REVERSED` | 1 | Example | Link stored with high before low | Still shown correctly. |
| `LK-SELF` | 1 | Example | A case linked to itself | Anomaly: renders once or not at all without error. |
| `AS-LOW-MEDIUM-HIGH` | 1 | Example | Current analyst assessment of High | Assessment shown beside the computed likelihood. |
| `AS-WITHDRAWN` | 1 | Example | Assessment recorded then withdrawn | No current assessment; history shows both. |
| `AS-HISTORY-21` | 1 | Example | Twenty-one assessments | History capped at twenty. |

### Regulatory reports and forms

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `RP-DRAFT` | 1 | Example | Report in state 1 | Report panel shows the state. Closing is blocked for users without approve permission. |
| `RP-VALIDATED` | 1 | Example | Report in state 2 | Report panel shows the state. Closing is blocked for users without approve permission. |
| `RP-PENDING-APPROVAL` | 1 | Example | Report in state 3 | Report panel shows the state. Closing is blocked for users without approve permission. |
| `RP-APPROVED` | 1 | Example | Report in state 4 | Report panel shows the state. Closing is blocked for users without approve permission. |
| `RP-SUBMITTED` | 1 | Example | Report in state 5 | Report panel shows the state. |
| `RP-ACKNOWLEDGED` | 1 | Example | Report in state 6 | Report panel shows the state. |
| `RP-REJECTED` | 1 | Example | Report in state 7 | Report panel shows the state. |
| `RP-WITHDRAWN` | 1 | Example | Report in state 8 | Report panel shows the state. |
| `FM-ENTRIES` | 1 | Example | Form entries with values | Form history lists entries and values. |
| `FM-NO-VALUES` | 1 | Example | Form entry with no values | Entry renders with an empty body. |
| `FM-ORPHAN` | 1 | Example | Form entry with no case id | Visible through the case key journal; absent from the case form list. |

### Returned to me and attention

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `RT-REOPENED` | 1 | Example | Reopened since the viewer last looked | In Returned to me with a reopen attention flag. |
| `RT-ESCALATED` | 1 | Example | Escalated automatically since last viewed | In Returned to me with an escalated flag. |
| `RT-MOVED-BY-OTHER` | 1 | Example | Status moved by someone else since last viewed | In Returned to me. |
| `RT-NEW-EVENT` | 1 | Example | Newer note event by someone else | In Returned to me. |
| `RT-DIARY-DUE` | 1 | Example | Diary due and the viewer has seen it | In Returned to me through the diary rule. |
| `RT-NOT-VIEWED` | 1 | Example | Same changes but never viewed | Not in Returned to me. |

### Retention and erasure

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `ER-OPEN-CASE` | 1 | Example | Subject with an open case | Erasure refused: open case. |
| `ER-LEGAL-HOLD` | 1 | Example | Subject with a case on legal hold | Erasure refused: legal hold (checked first). |
| `ER-RETENTION` | 1 | Example | Subject inside the retention minimum | Erasure refused: retention minimum. |
| `ER-ERASABLE` | 1 | Example | Closed subject with transactions older than five years | Erasure can proceed and null the case payload. |

### Edge data

| Code | Cases | Workflow | What it is | Expect |
|---|---|---|---|---|
| `ED-KEY-RTL` | 1 | Example | Arabic case key value | Key renders right to left in lists and the header. |
| `ED-KEY-EMOJI` | 1 | Example | Emoji case key value | Renders without truncation. |
| `ED-KEY-LONG` | 1 | Example | Case key value of 600 characters | Truncates in the list; full in the header tooltip. |
| `ED-KEY-EMPTY` | 1 | Example | Empty case key value | Risk key history is skipped; page renders. |
| `ED-KEY-NULL` | 1 | Example | NULL case key value | Page renders; journal empty. |
| `ED-DUPLICATE-OPEN` | 2 | Example | Two open cases for one key | The creation guard would block this; both open and listed. |
| `ED-NO-ARCHIVE` | 1 | Example | Case with no archived transaction | Journal and transaction tiles empty; page renders. |
| `ED-JSON-EMPTY` | 1 | Example | Case payload is an empty object | Fields tiles show nothing. |
| `ED-JSON-BIG` | 1 | Example | Case payload of about one megabyte | Opens within a few seconds. |
| `ED-DELETED-RULE` | 1 | Example | Originating rule no longer exists | Rule tile shows the unknown rule. |
| `ED-FUTURE-CREATED` | 1 | Example | Created ten days in the future | Sorts as newest; ages clamp to zero. |
| `ED-MANY-EVENTS` | 1 | Example | One thousand events on one case | Timeline stays responsive. |
| `ED-TRANSITIONS-NONE` | 1 | Example | No status transitions | No dwell; time in status shows a dash. |
| `ED-TRANSITION-INVERTED` | 1 | Example | Transition exited before it was entered | Dwell clamps to zero. |
| `ED-EVENT-TYPE-UNKNOWN` | 1 | Example | An event of an undefined type | Renders as Unknown. |

## Queue volume

`Smoke: Queue Volume` holds 480 open cases, 120 closed cases and 210 cases whose locks have been idle between three and sixty hours. Lanes are spread to roughly five percent live money, five percent mandatory, ten percent deadline near and the remainder standard, with a small share not yet scored. About a third of the open cases are held, spread across the six analyst names and Administrator, and some carry diaries and quality review picks. Use it to check paging (page size 100), Take next with a deep queue, the Urgent and Overdue chips, the Supervision workload and the stuck locks list (which is capped at 200, so the 210 stuck cases prove the cap), and the pulse cards. Every case has a transaction with coordinates, so the journal map has points.

## States the database refuses

Some awkward states from the research are impossible to seed because the schema forbids them, which is itself a result worth knowing.

- Two open status transitions for one case: a unique index allows one open row per case.
- Two view rows for one case and user: a unique index.
- Two checklist items for one definition on one case: a unique index.
- Case events, notes, suppressions, reports and views pointing at a case that does not exist: a foreign key. A note with no case id is allowed, and is seeded (`SMK-ED-ORPHAN-NOTE`), and so is a link to a case id that does not exist.
- A status with a malformed payment hold object in the archive: the seed would break the case page, so it is left out. The page reads the hold with a strict parser, and a malformed value fails the whole list of holds for that case.

## Not seeded

- Users and passwords, for the reason above.
- A workflow whose model is deleted, and a second tenant, which need configuration the demonstration model does not have.
- Regulatory report content: the seeded reports have a minimal profile and empty data, enough to show every report state and the close gate but not to validate or file.
- Graph and sanction evidence per case; the graph and sanctions seeds cover those areas.
- Cases beyond a single tenant.

## Effect on the dashboards

The seeded cases are ordinary cases, so the executive dashboards and the report library count them. They are recent and a small share of the history, so the twelve month story is still visible, but a presenter who wants the dashboards exactly as designed should filter the case key value by the `SMK-` prefix or demonstrate on a database without them.
