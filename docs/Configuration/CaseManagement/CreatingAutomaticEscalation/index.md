---
layout: default
title: Creating Automatic Escalation
nav_order: 27
parent: Case Management
grand_parent: Configuration
---

# Creating Automatic Escalation

Status elevation, documented in [Creating Status Elevation](../CreatingStatusElevation/index.html), raises the status of a case when a later rule match is more severe. Automatic escalation is the other half, in that it moves an open case on by itself when it has waited too long, breached its target or become urgent, without anyone having to notice. The same status transition is recorded either way, so the status history and the time in each status stay complete.

## Where it lives

Each status carries at most one escalation rule, which is held in the Escalation group of the [Cases Workflow Status](../CreatingCaseWorkflowStatus/index.html) that should escalate. Navigate through the menu to Models, open the model, then the Cases Workflows hub card, open the Case Workflow, then the Status hub card, and open the status. The Escalation group is collapsed at first.

## Fields

| Value                                  | Description                                                                                                                                                                         | Example                                         |
|----------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------|
| Escalation trigger                     | What makes a case in this status move. Left empty for no automatic escalation.                                                                                                      | Time in status reaches the threshold in minutes |
| Escalation threshold                   | The minutes, lane or Priority the trigger compares against. It is presented, and required, for the first, third and fourth triggers only.                                          | 240                                             |
| Escalation target case workflow status | The status of the same workflow the case moves to. It must be active and a stronger priority than this status, meaning a lower Priority number (1 is Ultra high, 5 is Ultra low).  | Senior Review                                   |
| Escalation release lock                | When on, the lock held on the case is released as it moves, so another analyst can take it. When off, the analyst who holds the case keeps it.                                      | True                                            |

## Triggers

| Trigger                                                  | Applies when                                                                                                                              | Threshold           |
|----------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|---------------------|
| Time in status reaches the threshold in minutes          | The case has been in this status for at least the threshold in minutes.                                                                   | 1 to 525600 minutes |
| Case is older than the workflow resolution target        | The case is older than the Resolution target minutes of its Case Workflow. Without a target on the workflow the rule never applies.       | Not used            |
| Case lane is at or more urgent than the threshold lane   | The latest ranking assessment of the case puts it in the threshold lane or a more urgent one: 1 Live money, 2 Mandatory, 3 Deadline near. | 1 to 3              |
| Case Priority is at or above the threshold               | The latest ranking assessment of the case has a Priority at or above the threshold.                                                       | 1 to 100            |
| The case diary deadline has passed                       | The latest ranking assessment of the case says its diary deadline has passed.                                                             | Not used            |

The lane, Priority and deadline triggers rely on the case ranking worker, which is documented under [Creating Case Workflows](../CreatingCaseWorkflows/index.html), so they only apply once a case has been assessed.

## What happens when a rule applies

The engine checks every escalation rule on each sweep, and acts only on open cases that are in the status of the rule. A case that is closed, or suspended, is never moved. For each case that matches, the engine:

* moves the case to the target status, and releases the lock if the rule says so;
* closes the stay in the old status and opens a stay in the target, recorded with the reason Escalated;
* writes an Automatic Escalation event to the case journal, in the name of Administrator, with the old and new status as Before and After;
* sends the Notification and HTTP Endpoint of the target status if they are enabled, exactly as for a case that arrives in that status, as described in [Creating Automation](../CreatingAutomation/index.html).

If the notification fails the move still stands, and the failure is logged. A move made by the engine is not held back by checklist items still open in the old status, the checklist instead being recorded as bypassed by automation.

## Why a rule can never loop

A rule is only ever applied when the target is a stronger priority than the source status, and the check is made again every sweep. A rule that points at an equal or weaker status, at an inactive status, or at a status of another workflow is refused when it is saved and, if the target changes afterwards, skipped with a warning in the engine log. Because every move goes to a strictly stronger priority, it follows that a case can travel A to B to C but can never return, and a chain of rules carries a case on at least one step per sweep while each rule applies.

## Validation

Validation takes place in the server, upon saving the Cases Workflow Status. The status is refused where:

* The Escalation trigger is not one of the five triggers.
* An Escalation target case workflow status is chosen without an Escalation trigger, or an Escalation trigger without a target.
* The Escalation threshold is outside the range given for the trigger in the table above.
* The target is not an active status of the same Case Workflow, or is the status itself.
* The target is not of a stronger priority than the status, which is to say that its Priority number is not lower.

The messages returned are as follows:

| Condition                                              | Message                                                                                                                 |
|--------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| Trigger not one of the five                            | The escalation trigger must be one of the listed triggers.                                                              |
| Target chosen without a trigger                        | An escalation trigger is required when an escalation target is chosen.                                                  |
| Trigger chosen without a target                        | An escalation target status is required when an escalation trigger is chosen.                                           |
| Threshold missing or outside the range for the trigger | The escalation threshold must be 1 to 525600 minutes for time in status, 1 to 3 for a lane, or 1 to 100 for a Priority. |
| Target inactive, of another workflow, or the status itself | The escalation target must be another active status of the same case workflow.                                      |
| Target not of a stronger priority                      | The escalation target must be a stronger priority than this status, meaning a lower Priority number.                    |

It follows from the last of these that a status of Priority Ultra high can never carry an escalation rule, there being nothing stronger for it to point at. The Escalation target case workflow status offers every status of the Case Workflow, the status itself and the weaker ones included, alongside an empty choice by which the rule is removed, and it is the server that refuses an unsuitable choice. For a new status the Escalation group is empty and Escalation release lock is off.

## Example

Suppose a Case Workflow has the statuses First Line Review, of Priority Ultra low, and Senior Review, of Priority High. To have a case that has waited four hours in First Line Review move to Senior Review, and be made free for another analyst to take, the Escalation group of First Line Review is completed as follows:

| Value                                  | Example                                         |
|----------------------------------------|-------------------------------------------------|
| Escalation trigger                     | Time in status reaches the threshold in minutes |
| Escalation threshold                   | 240                                             |
| Escalation target case workflow status | Senior Review                                   |
| Escalation release lock                | True                                            |

## Settings

The worker runs when the EnableCaseEscalation Environment Variable is True, which is the default, waits CaseEscalationWait milliseconds between sweeps, being 30000 by default, and moves at most CaseEscalationBatchSize cases per rule in one sweep, being 200 by default. A full list is under [Environment Variables](../../../Concepts/EnvironmentVariables/index.html).

## Permissions and security

The escalation rule is saved with the Cases Workflow Status, which requires permission 19, Read Write Cases Workflows Status. The moves themselves are made by the engine in the name of Administrator, and do not depend on the permissions or the roles of any user. Keep in mind that a case moved to a status allocated to fewer roles is thereafter isolated from the remaining roles, as described in [Case Management Security Policy](../SecurityPolicy/index.html).
