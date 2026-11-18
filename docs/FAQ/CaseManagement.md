---
layout: default
title: Case Management FAQ
nav_order: 6
parent: FAQ
---

# Case Management

## Case creation and lifecycle

**Q: How are cases created?**

A: An activation rule can create a case with a case workflow, a status, and a case key plus value, as described in [Creating a Case Entry with an Activation Rule](../Configuration/CaseManagement/CreatingCaseEntryWithActivationRule/index.html). A case can also be raised by hand against a case key and value, although at present only through the API (`POST /api/Case/CreateFromCaseKeyValue`) or by Ask Jooby, the Cases page having no control for it. See [Working Navigating the Case Record](../Configuration/CaseManagement/WorkingNavigatingCaseRecord/index.html).

**Q: Can multiple cases exist for the same key?**

A: Only one open case per case-key value per workflow. Further matches don't create new cases unless the case is closed. A higher-priority status elevates the existing case; it is never lowered.

**Q: What is the difference between peek, opening a case and Take next (formerly skim)?**

A: A peek, from a row of the queue, presents a summary of the case without opening it, and is recorded in the events of the case as Peeked. Opening a case presents the case page. Neither locks the case. Take next, which was known as Skim in earlier versions, takes the very first case of the queue in view that is free and locks it to the user, which is the advisable manner of working where several users share a queue. Next case, in the header of the case page, does the same without returning to the Cases page. See [Working Skim, Lock and Next](../Configuration/CaseManagement/WorkingSkimLockNext/index.html).

**Q: What does "lock" mean on a case?**

A: Effectively case ownership, shown in the Decide pane of the case page as Held by you, Held by another user, or Not held, with Take or Release as is appropriate. Locked cases do not unlock by themselves, hence a forgotten lock needs a supervisor to break or reassign it. See [Working Lock, Allocation and Unlock](../Configuration/CaseManagement/WorkingLockAllocationUnlock/index.html) and [Working Case Supervision](../Configuration/CaseManagement/WorkingCaseSupervision/index.html).

**Q: How is escalation implemented?**

A: By changing the case workflow status, from the Status tile of the Decide pane. Filters per role then present the right work to each level.

**Q: What are the diary and suspend statuses?**

A: A diary date with Suspend Open returns the case to Open when the date passes, and Suspend Closed moves it on to Closed. Both remove the case from the queue in the meantime, and both rely upon `EnableCasesAutomation` being True. A diary is set on an open case with the Diary tile of the Decide pane, whereas the case page does not at present offer the means to suspend a case, which is done through the API or by Ask Jooby. See [Working Closed, Diary Date and Suspend](../Configuration/CaseManagement/WorkingClosedDiarySuspend/index.html) and [Working Typed Diaries](../Configuration/CaseManagement/WorkingTypedDiaries/index.html).

## Visibility and isolation

**Q: Why don't I see my case in the queue?**

A: In earlier versions a missing default Case Workflow Filter was the usual cause. That is no longer so, as the Cases page has filters built in, All cases being the default. The usual causes are now that the role is not allocated to the case workflow or to the status the case is in, that the wrong Workflow is selected at the top of the Cases page, or that the case is not Open or Suspend Open, which is all that the built in filters return. Keep in mind also that only the first 100 cases matching a filter are listed.

**Q: Who can see a case?**

A: Access is isolated by tenant and by role. A case is returned only when the user is in the tenant and their role is allocated on both the case workflow and the case workflow status; without an allocation the request simply fails with 404 or 400, revealing nothing. Per-field role allocations additionally control which columns the case journal shows. See [Case Management Security Policy](../Configuration/CaseManagement/SecurityPolicy/index.html).

**Q: Why does the Case Key Journal only show the trigger transaction, or look empty?**

A: The Case detail views section of the case page shows only the transaction that created the case; the Journal section should show every archive record for the case key value, once the archive has been written, which takes place in bulk a matter of seconds after the transaction. If it is empty, check that the model has its RDBMS archive switched on, that the case key is flagged Archive Key in the Request XPath, and use the Sample card of the model to confirm that records reached the archive.

**Q: What are the case key journal and drill?**

A: The journal shows the archived transactions for the case key value, as statistics, charts, activations, a grid and, where the model carries a location, a map. The drill fields of the case workflow, being those flagged Archive Key, can be added to the journal as further keys, each being a lane merged into the one history in date order, which is the means of pivoting to the transactions of another key. Bold Line Matching highlights the rows matching the record that created the case. See [Working Case Key Journal](../Configuration/CaseManagement/WorkingCaseKeyJournal/index.html).

## Decisions and outbound actions

**Q: Can I approve or decline a transaction from a case?**

A: Not the transaction that has already been answered. The real-time decision is the response elevation returned at invocation. Cases are for investigation, and outcomes (such as adding a key to a list, asking for a model override, or tagging) influence future transactions. The exception is a payment that has been held by the case, which is released or refused from the Held payment tile.

**Q: Can case actions call back to my systems?**

A: Yes, via the HTTP Endpoint that can be set on a case workflow status, action, form, macro and release hook. Everything sent for a case key is listed in the Messages section of the case page. See [Creating Automation](../Configuration/CaseManagement/CreatingAutomation/index.html).

**Q: What other case features exist?**

A: Notes and research with files, forms, checklists, macros, displays (HTML with token substitution), typed diaries, linked and similar cases, a triage recommendation, the analyst's own assessment, quality review, regulatory reports, legal hold and embedded visualisation dashboards. Checklists were at one time approximated with forms; they are now a feature of their own, configured beneath a case workflow status, and their items must be ticked in the Decide pane before a case in that status can be moved, escalated or closed. See [Creating a Case Workflow Status](../Configuration/CaseManagement/CreatingCaseWorkflowStatus/index.html).

**Q: How are email notifications configured?**

A: A Notification can be set wherever an HTTP Endpoint can, and for a status it is dispatched upon a case arriving in that status, whether on creation or on change. SMTP settings are environment variables (`SMTPHost`, `SMTPPort`, `SMTPUser`, `SMTPPassword`, `SMTPFrom`), including `SMTPUseDefaultCredentials` and `SMTPEnableSsl`.
