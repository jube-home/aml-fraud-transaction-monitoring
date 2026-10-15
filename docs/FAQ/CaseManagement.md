---
layout: default
title: Case Management FAQ
nav_order: 6
parent: FAQ
---

# Case Management

## Case creation and lifecycle

**Q: How are cases created?**

A: An activation rule can create a case with a case workflow, a status, and a case key plus value. Manual case creation
from a transaction report is also available.

**Q: Can multiple cases exist for the same key?**

A: Only one open case per case-key value per workflow. Further matches don't create new cases unless the case is closed.
A higher-priority status elevates the existing case; it is never lowered.

**Q: What is the difference between peek, fetch and skim?**

A: Fetch opens a specific case without locking it. Skim takes the top unlocked case from the queue and locks it to you.

**Q: What does "lock" mean on a case?**

A: Effectively case ownership. A forgotten lock needs supervisory management to release.

**Q: How is escalation implemented?**

A: By changing the case workflow status. Filters per role then present the right work to each level.

**Q: What are the diary and suspend statuses?**

A: A diary date with "suspend open" returns the case to open when the date passes. "Suspend closed" moves it to closed.
Both remove the case from the queue in the meantime.

## Visibility and isolation

**Q: Why don't I see my case in Skim or Peek?**

A: A missing default Case Workflow Filter is the usual cause. Define a filter for the workflow and status.

**Q: Who can see a case?**

A: Access is isolated by tenant and by role. A case is returned only when the user is in the tenant and their role is
allocated on both the case workflow and the case workflow status; without an allocation the request simply fails with
404 or 400, revealing nothing. Per-field role allocations additionally control which columns the case journal shows.
See [Case Management Security Policy](../Configuration/CaseManagement/SecurityPolicy/index.html).

**Q: Why does the Case Key Journal only show the trigger transaction, or look empty?**

A: The case canvas shows the trigger transaction only; the journal should show every archive record for the case key
value after the bulk insert (about 10 seconds). If it's empty, check that archive storage ("enable RDBMS archive") is
switched on in the model, and use the Sampling page to confirm records reached the archive. Text colourisation overlap
can also hide cells.

**Q: What are the case key journal and drill?**

A: The journal shows archive transactions for the case key. Fields marked "drill" let you pivot to all transactions for
that field across the portfolio, which needs an expression index. A "bold line match" highlights the original case
records.

## Decisions and outbound actions

**Q: Can I approve or decline a transaction from a case?**

A: No. The real-time decision is the response elevation returned at invocation. Cases are for investigation, and
outcomes (such as adding a key to a list or override, or tagging) influence future transactions.

**Q: Can case actions call back to my systems?**

A: Yes, via webhooks on case workflow status change, notes and actions, forms and macros.

**Q: What other case features exist?**

A: Notes with action codes, uploads, forms (also used as checklists), macros, displays (HTML with token substitution)
and embedded visualisation dashboards.

**Q: How are email notifications configured?**

A: Notifications fire on case creation and status change. SMTP settings are environment variables, including
`SMTPUseDefaultCredentials` and `SMTPEnableSsl`.
