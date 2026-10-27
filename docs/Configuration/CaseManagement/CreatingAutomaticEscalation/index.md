---
layout: default
title: Creating Automatic Escalation
nav_order: 27
parent: Case Management
grand_parent: Configuration
---

# Creating Automatic Escalation
Status elevation, documented elsewhere, raises the status of a case when a later rule match is more severe. Automatic escalation is the other half: it moves an open case on by itself when it has waited too long, breached its target or become urgent, without anyone having to notice. The same status transition is recorded either way, so the status history and the time in each status stay complete.

Each status carries at most one escalation rule. Navigate to Models >> Cases Workflows >> Cases Workflow Status, open the status that should escalate and complete the Escalation group:

| Value                   | Description                                                                                                                                                                                             | Example                    |
|-------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------|
| Escalation trigger      | What makes a case in this status move. Leave empty for no automatic escalation.                                                                                                                         | Time in status             |
| Escalation threshold    | The minutes, lane or Priority the trigger compares against. Required for the first, third and fourth triggers.                                                                                          | 240                        |
| Escalation target       | The status of the same workflow the case moves to. It must be active and a stronger priority than this status, meaning a lower Priority number (1 is Ultra High, 5 is Ultra Low).                      | Senior Review              |
| Escalation release lock | When on, the lock held on the case is released as it moves, so another analyst can take it. When off, the analyst who holds the case keeps it.                                                          | On                         |

## Triggers

| Trigger                         | Applies when                                                                                                                                       | Threshold                            |
|---------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------|
| Time in status                  | The case has been in this status for at least the threshold in minutes.                                                                            | 1 to 525600 minutes                  |
| Resolution target breached      | The case is older than the Resolution target minutes of its Case Workflow. Without a target on the workflow the rule never applies.              | Not used                             |
| Lane at least urgent            | The latest ranking assessment of the case puts it in the threshold lane or a more urgent one: 1 Live money, 2 Mandatory, 3 Deadline near.         | 1 to 3                               |
| Priority at least               | The latest ranking assessment of the case has a Priority at or above the threshold.                                                                | 1 to 100                             |
| Deadline overdue                | The latest ranking assessment of the case says its diary deadline has passed.                                                                      | Not used                             |

The lane, Priority and deadline triggers rely on the case ranking worker, which is documented under Creating Case Workflows, so they only apply once a case has been assessed.

## What happens when a rule applies

The engine checks every escalation rule on each sweep, and acts only on open cases that are in the rule's status. A case that is closed, or suspended, is never moved. For each case that matches, the engine:

* moves the case to the target status, and releases the lock if the rule says so;
* closes the stay in the old status and opens a stay in the target, recorded with the reason Escalated;
* writes an Automatic Escalation event to the case journal, in the name of Administrator, with the old and new status as Before and After;
* sends the target status notification and HTTP endpoint if they are enabled, exactly as for a case that arrives in that status.

If the notification fails the move still stands, and the failure is logged.

## Why a rule can never loop

A rule is only ever applied when the target is a stronger priority than the source status, and the check is made again every sweep. A rule that points at an equal or weaker status, at an inactive status, or at a status of another workflow is refused when it is saved and, if the target changes afterwards, skipped with a warning in the engine log. Because every move goes to a strictly stronger priority, a case can travel A to B to C but can never return, and a chain of rules carries a case on at least one step per sweep while each rule applies.

## Settings

The worker runs when `EnableCaseEscalation` is True, waits `CaseEscalationWait` milliseconds between sweeps and moves at most `CaseEscalationBatchSize` cases per rule in one sweep. A full list is under Environment Variables.
