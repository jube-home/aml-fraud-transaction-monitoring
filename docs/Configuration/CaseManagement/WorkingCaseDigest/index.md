---
layout: default
title: Working the Case Digest
nav_order: 32
parent: Case Management
grand_parent: Configuration
---

# Working the Case Digest

A supervisor should not have to open the queue to know whether it is healthy. The digest tells each supervisor, once a
day,
how each workflow they supervise stands.

Set EnableCaseDigest to True. At CaseDigestHourUtc (7 by default, UTC) on the days in CaseDigestDays (Monday to Friday
by default,
where 0 is Sunday), the engine sends each workflow's supervisors one in-app notification with four counts:

| Count                | Meaning                                                                            |
|----------------------|------------------------------------------------------------------------------------|
| Open                 | Open cases in the workflow.                                                        |
| Unheld               | Open cases nobody holds.                                                           |
| Past a service level | Open cases the service level sweep has found in breach of any of the four targets. |
| Closed               | Cases closed in the last day.                                                      |

A supervisor is a user whose role holds the supervision permission and is granted the workflow. A workflow with nothing
open and nothing
closed in the last day is not reported. The notification is high priority when anything is past a service level.

When CaseDigestEmail is also True, each supervisor with an email address is sent the same counts by email, using the
SMTP settings. The email
never carries a case key or value: it says how many, and tells the reader to sign in.

It is sent once per workflow per supervisor per day however many nodes run the engine, because the notification is
created with
a key made of the workflow and the date and only the node that creates it sends the email.

<!-- TODO(docs): the digest time and days are one setting for the whole deployment, in UTC; the plan was a schedule per tenant in the tenant's time zone, and the analytics backlog still lists a per-tenant time zone. -->
<!-- TODO(docs): the Past a service level count is zero unless EnableCaseServiceLevelSweep is on, because the digest reads the breaches the sweep records. Say so on the sweep page, or make the digest count them itself. -->
<!-- TODO(docs): the digest has no inflow spike alert (the control chart limits idea in the analytics backlog); the same schedule and recipients could carry it. -->
<!-- TODO(docs): an hour that the engine is down is missed, not caught up; the digest for that day is lost. A catch-up window would fix it. -->
<!-- TODO(docs): the email is HTML with counts only and has no per-user unsubscribe; a supervisor who does not want it must have their email address cleared or the setting turned off. -->
<!-- TODO(screenshot): file=case-digest-notification.png route=/ state="the notification bell showing a case digest line" -->
