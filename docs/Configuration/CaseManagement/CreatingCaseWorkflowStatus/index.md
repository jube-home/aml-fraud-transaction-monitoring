---
layout: default
title: Cases Workflow Status
nav_order: 2
parent: Case Management
grand_parent: Configuration
---

# Cases Workflow Status

When a case is created, it can be allocated a status, which is helpful for filtering. Furthermore, when a case is being
worked, changing the status allows for the sub allocation of the record (for example to a money laundering officer for
review). Statuses are separate from the closed status (i.e. Open, Closed etc).

A common example for case status use is to escalate cases through a hierarchy in an money laundering team, such as:

* Front Line Defence (FLD).
* Deputy Anti Money Laundering Compliance Officer (DAMLCO).
* Anti Money Laundering Compliance Officer (AMLCO).

To begin the addition of Cases Workflow Status, navigate to Administration >> Models >> Cases Workflows >> Cases
Workflow Status:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-status-01.png route=/models state="Models page, Case Workflows tab, case-workflow-status screen replacing the Razor-era TopOfTreeForCasesWorkflows.png" -->
![Image](TopOfTreeForCasesWorkflows.png)

Cases Workflows Status is a child of Cases Workflows, itself a Child of Models. It follows that the tree to administer
models and cases workflows is now of three levels deep.

It is incumbent to expand upon the model to reveal the Cases Workflows entry:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-status-02.png route=/models state="Models page, Case Workflows tab, case-workflow-status screen replacing the Razor-era ExpandEntityModel.png" -->
![Image](ExpandEntityModel.png)

Exposing the newly created Cases Workflow, which can be used to create new children of the Cases Workflow:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-status-03.png route=/models state="Models page, Case Workflows tab, case-workflow-status screen replacing the Razor-era CasesWorkflowsStatusTopOfTree.png" -->
![Image](CasesWorkflowsStatusTopOfTree.png)

Click on the Case Workflow entry to begin the process of adding Case Workflow Status:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-status-04.png route=/models state="Models page, Case Workflows tab, case-workflow-status screen replacing the Razor-era CasesWorkflowsStatusTopOfTree.png" -->
![Image](CasesWorkflowsStatusTopOfTree.png)

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-status-05.png route=/models state="Models page, Case Workflows tab, case-workflow-status screen replacing the Razor-era TemplateAddForCasesStatusWorkflow.png" -->
![Image](TemplateAddForCasesStatusWorkflow.png)

The parameters are as follows:

| Value              | Description                                                                                                                                                                                                                 | Example |
|--------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------|
| Priority           | The priority of the workflow flow entry.  A status can be upwardly classified, such that if a case record already exists, the priority can be raised for that record, yet the priority of a case record may never decrease. | High    |
| Status Fore Colour | The colour of the text for this status.  The colour is used in the main bar on the status page.                                                                                                                             | #0000FF |
| Status Back Colour | The colour of the background for this status,  the colour is the main bar on the status page.                                                                                                                               | #880808 |

Validation: Name is required (256 characters max) and must be unique within the parent Cases Workflow. Priority must be
one of the five values above (Ultra High to Ultra Low). Status Fore Colour and Status Back Colour are required and must
be a 6-digit hex colour code, e.g. `#0000FF`.

Notifications and HTTP Endpoint as common among the following section of Cases Workflow configuration, are documented
separately and not covered in detail in this section, being common in several sections of the Cases Workflows
configuration such as:

* Cases Workflow Form.
* Cases Workflow Action.
* Cases Workflow Macro.

Complete the page as described above and as below:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-status-06.png route=/models state="Models page, Case Workflows tab, case-workflow-status screen replacing the Razor-era ExampleValuesForCasesWorkflowStatus.png" -->
![Image](ExampleValuesForCasesWorkflowStatus.png)

Scroll down the page and click the Add button to create a version of the Cases Workflow:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-status-07.png route=/models state="Models page, Case Workflows tab, case-workflow-status screen replacing the Razor-era VersionAddedCasesWorkflowStatus.png" -->
![Image](VersionAddedCasesWorkflowStatus.png)

The Cases Workflow Status are available for allocation on case creation or by the end user while navigating the case.

For confirmatory purposes, navigate to the Activation Rule definition for Volume1DayUSDForIPOver100 in the main menu,
expand using the Cases switch, and select the newly created Cases Workflow:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-status-08.png route=/models state="Models page, Case Workflows tab, case-workflow-status screen replacing the Razor-era CasesWorkflowsStatusNowAvailable.png" -->
![Image](CasesWorkflowsStatusNowAvailable.png)

It can be seen that the newly created Cases Workflow Status is now available as a child of the newly created Cases
Workflows.

At this stage, no case record has been created, but Cases Workflow Status are available for filtering via the Cases
Workflow filter, described separately.

<!-- TODO(docs): Verify escalation fields (trigger 1 to 5, threshold ranges, target must be an active status of the same workflow with a stronger Priority) against CaseWorkflowStatusDtoValidator. -->
<!-- TODO(docs): Document that a status cannot be moved to another Case Workflow by an update (CaseWorkflowId pinned). -->
<!-- TODO(docs): State the SQL field: needs permission 67, single SELECT, parameters @Id and @CaseKeyValue only, 8000 characters, see Datasource Template dialect page. -->
<!-- TODO(docs): The HTTP endpoint and notification fields: owner to decide how to describe the outbound host policy for status HTTP endpoints (see the SHARED-TODO BUG about unguarded legacy dispatch). -->
<!-- TODO(docs): Colours must be #RRGGBB; confirm the example colours in the screenshots. -->
