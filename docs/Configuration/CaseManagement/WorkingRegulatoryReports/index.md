---
layout: default
title: Working Regulatory Reports
nav_order: 35
parent: Case Management
grand_parent: Configuration
---

# Working Regulatory Reports

A regulatory report (a suspicious activity or transaction report) is filed about a case and exists only as part of that
case. It is built by the report engine, `Jube.RegulatoryReport`, from a **profile** (see Regulatory Reporting), approved
by a second person, and then filed with the financial intelligence unit (FIU) outside Jube. Jube records the filing; it
does not transmit it.

## Who can do what

Three permissions, none of them held by anyone but the administrator role until you grant them:

| Permission                | Lets you                                                                                                                                                                                                                 |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| File Case Reports (75)    | Start a report, edit its fields, validate it, request approval, withdraw your own, download it once approved, record that it was filed, record what the FIU said, and correct a rejected report as a new version.        |
| Approve Case Reports (76) | Approve or reject a report that someone else prepared, import report profiles, see the list awaiting approval, withdraw any unfiled report, and close a case that has a report awaiting approval. This is the MLRO role. |
| View Case Reports (77)    | See that reports exist and read them.                                                                                                                                                                                    |

You also need to be able to open the case. A person who holds none of the three sees nothing: the report card does not
appear, the calls answer forbidden, and report events are not on the case timeline.

## Tipping-off

Reports are kept out of every place a person without the permissions could stumble on them. The report, its versions,
approvals and FIU feedback live in their own tables and are read only through the report calls. The generated XML is
held on the version, not attached to the case as a file or a note, because case files and notes are visible to everyone
who can read the case. Report steps are not written to the case timeline. The tables are blocked from AskJooby's SQL and
no AskJooby tool reaches them. Notifications about reports go only to holders of the permissions.

## The life of a report

```
Draft -> Validated -> Pending approval -> Approved -> Submitted -> Acknowledged
  ^          |               |                           |
  |          v               v                           +-> Rejected by the FIU -> new version (Draft)
  +---- edit makes a        Rejected by the approver -> Draft
        new version        Withdrawn (from Draft, Validated or Pending approval)
```

1. **Start.** Choose a profile and give the FIU's report type. The report starts as a draft, version 1.
2. **Fields.** Enter the report-specific fields as a JSON object. The engine is also given the case (its key, key value,
   workflow, dates), the latest narrative note and your user name; a profile template reads all of these. Saving a draft
   that has never been validated updates it in place; saving a validated one makes a new version, so what was validated
   is never altered.
3. **Validate.** The engine builds the XML and checks it against the FIU's schema. Problems come back with the data path
   or the XML element path, line and position, and the report stays a draft. A clean run stores the XML and its SHA-256
   and moves the report to Validated.
4. **Request approval.** Holders of the approve permission are notified. The version is now frozen.
5. **Approve or reject.** A different person approves, or rejects with a note; a rejection returns the report to Draft.
   The preparer, and whoever last edited the version, can never decide it.
6. **Download.** Once approved, the stored XML can be downloaded. Jube checks the bytes against the recorded hash on the
   way out and refuses if they differ.
7. **Record the filing.** After uploading the XML to the FIU, record the FIU's reference. The report becomes Submitted,
   and the case is placed under legal hold (reason "Regulatory report filed") so that retention can never remove it.
8. **Record the FIU's answer.** Acknowledged ends the report. Rejected returns it to the preparer, who corrects it as a
   new version that supersedes the rejected one; both are kept. Information requested is recorded and the report stays
   submitted.

## Closing the case

A case cannot be closed while one of its reports is awaiting approval or approved but not yet submitted. Holders of the
approve permission may close it anyway. A person who holds one of the report permissions is told why; anyone else is
only told that the case cannot be closed yet and to ask a supervisor to review it; the reason is withheld so the
existence of a report is not disclosed. Every other close refusal (a checklist item outstanding, the case locked by
someone else) already says specifically what is wrong.

<!-- TODO(screenshot): The Regulatory reports card on a case, showing a validated report with its Request approval button. -->
<!-- TODO(screenshot): The card for an approved report: Download XML and the FIU reference box. -->

<!-- TODO(docs): not built: the data the engine receives is the case facts, the narrative and the fields you type; transactions chosen from the Case Key Journal, field roles and values from a case workflow form are not yet offered. The approval queue is available through the API (GET /api/CaseReport/AwaitingApproval) but is not yet in the Approvals panel. FIU rejection does not yet set a diary. Profiles can be imported through the API only; there is no page for them. -->
