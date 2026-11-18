---
layout: default
title: Case Workflow Dashboard
nav_order: 22
parent: Visualisations
grand_parent: Configuration
---

# Case Workflow Dashboard

The dashboard shown in the case page is a Visualisation Registry whose Shown in is Case, and so each of its Datasources receives the built-in SQL parameter @Id, which is the id of the case. Every other fact about the case is traced from that id in the SQL, and, as the SQL reads the Case Workflow of the case, one registry serves every workflow while showing the targets, the statuses and the escalation of the workflow the case belongs to. It replaces the demonstration that read only static example tables, which never read the case and never read the Case Workflow, and it is attached to the Case Workflow of the demonstration in place of that example, and only where that workflow still uses the example. To attach it to another workflow, set the Visualisation of the Case Workflow, as described in [Creating Case Workflows](../../CaseManagement/CreatingCaseWorkflows/index.html).

The dashboard is drawn once, when the case is opened, and is not refreshed when the status changes or a note is added, so every figure on it is as at the opening of the page. A tile cannot link to another page or react to a click. A tile reading a Case Workflow table needs the permission of that table, which is 18 for the Case Workflow and 19 for its statuses, and an analyst role holding only permission 1 sees those tiles as an error while the tiles drawn from the case itself are shown.

### Case: Workflow Dashboard

The tiles, in the order in which they are drawn, are as follows.

Tiles:

| Tile                                     | Shown as              | Reads                                                   | Permissions needed (any one per table) |
|------------------------------------------|-----------------------|---------------------------------------------------------|----------------------------------------|
| Case at a glance                         | tiles from a template | Case, CaseWorkflowStatus, CaseWorkflow                  | 1, 18, 19                              |
| Service levels of the workflow           | tiles from a template | CaseEvent, Case, CaseWorkflow                           | 1, 18                                  |
| Where the case is on the workflow        | tiles from a template | Case, CaseWorkflow, CaseWorkflowStatus                  | 1, 18, 19                              |
| Time in the status and what escalates it | tiles from a template | CaseEvent, Case, CaseWorkflowStatus                     | 1, 19                                  |
| Queue position                           | tiles from a template | Case                                                    | 1                                      |
| Rule that opened the case                | tiles from a template | Case, EntityAnalysisModelActivationRule, EntityApproval | 1, 17, 44, 45                          |
| Transaction that opened the case         | tiles from a template | Case                                                    | 1                                      |
| What happened to the case                | tiles from a template | CaseEvent                                               | 1                                      |
| Notes                                    | tiles from a template | CaseNote                                                | 1                                      |
| History of this key                      | column chart          | Case, ArchiveKey, Archive                               | 1                                      |
| Fields shown by the workflow             | tiles from a template | Case, CaseWorkflow, CaseWorkflowXPath                   | 1, 18, 20                              |
| Other cases for this key                 | grid                  | Case, CaseWorkflowStatus                                | 1, 19                                  |
| Network signals for this key             | grid                  | Case, GraphNode                                         | 1, 49                                  |
| Suppressions on this case                | grid                  | CaseSuppression                                         | 1                                      |

## How the Case Workflow shapes the tiles

The service level tile reads the first view, ownership and resolution targets of the Case Workflow and shows the time elapsed against each, as a bar and a badge that is green below eighty percent of the target, amber from eighty percent and red from one hundred percent, in the manner that the case analytics of the Cases page use. The first view is the first Skim, Full Case Fetch or Peek event of the case and ownership is the first Skim, Automatic Lock, Manual Lock or Allocate Lock event. The ladder tile lists every active status of the workflow with the current status marked and the outcome and dwell limit of each. The time in status tile uses the dwell limit of the status and names the escalation configured on it, which is the trigger, its threshold and the status the case moves to. The queue position tile ranks the case against the open cases in the same status by risk priority.

Keeping in mind that there is no column giving the order of the statuses, the ladder lists them in the order in which they were created, and the priority of a status is its urgency and not its position.
