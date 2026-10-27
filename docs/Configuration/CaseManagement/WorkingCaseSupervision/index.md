---
layout: default
title: Working Case Supervision
nav_order: 28
parent: Case Management
grand_parent: Configuration
---

# Working Case Supervision
Case supervision gives a team lead a view over who is holding which cases, which locks have gone stale, and a way to hand a case from one analyst to another. It needs the Read Write Case Workflow permission, and a role granted the Case Workflow being supervised, so a supervisor sees only the workflows their role can already see. A user without the permission is not shown the Supervision tab on the Cases page, and the server refuses the request regardless.

Open the Cases page and press Supervision to reach the page, which is held in the address so that it can be linked to.

## Workload

One row for every analyst who can work the workflow, meaning a user in the tenant whose role holds the Read Write Case permission and is granted the workflow. Anyone else who held, took, peeked at or closed a case in it in the period is listed too. The columns are:

| Column                 | Meaning                                                                                                          |
|------------------------|------------------------------------------------------------------------------------------------------------------|
| Holding                | Open cases locked to the analyst now.                                                                            |
| Oldest hold            | How long the analyst's longest held case has been locked.                                                        |
| Taken                  | Cases the analyst took in the period by skimming or locking them.                                                |
| Peeks                  | Cases the analyst looked at in the period without taking them.                                                   |
| Closed                 | Cases the analyst closed in the period.                                                                          |
| Median time to close   | The median time from creation to closure of the cases the analyst closed in the period.                          |

The headline counts the open cases someone holds, the open cases nobody holds and the stuck locks. The period can be 7, 30 or 90 days.

## Stuck locks

An open case that is locked to someone and has had no activity for at least the chosen time, which can be one hour, two hours, four hours, eight hours or a day. Activity is any event recorded on the case, or the lock itself where nothing has happened since. The list is longest idle first, capped at 200, and shows the case, its key, status, holder, last activity, idle time and Priority.

## Break lock

Break lock releases the lock so that anyone may take the case. It asks for an optional reason, up to 500 characters, and writes a Lock Broken event to the case journal in the name of the supervisor, with the previous holder as Before and the reason as After. The case must be open and locked, and the request is refused if the lock changed while it was being made.

## Reassign

Reassign locks the case to another analyst, whether or not someone holds it now, and writes an Allocate Lock event with the old holder as Before and the new holder as After. The analyst must be able to see the case, which means all of the following hold:

* they are a user in the same tenant;
* their role holds the Read Write Case permission;
* their role is granted the Case Workflow;
* their role is granted the status the case is in.

Otherwise the case would disappear from their view the moment it was handed over, so the request is refused. A closed case cannot be reassigned.

## Case Supervision agent

The same four operations, workload, stuck locks, break lock and reassign, are available to Ask Jooby through the Case Supervision specialist under the Case Investigation Desk, with a confirmation required before either change runs.
