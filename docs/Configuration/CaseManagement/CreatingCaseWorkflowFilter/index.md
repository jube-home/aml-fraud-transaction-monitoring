---
layout: default
title: Case Workflow Filter
nav_order: 3
parent: Case Management
grand_parent: Configuration
---

# Case Workflow Filter

Throughout a case life cycle, a case record can move through a multitude of dispositions. The most simple is the
hard-coded closed status, as follows:

| Value          | Description                                                                                                                                                    |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Open           | A new case with a status of Open, pending review.                                                                                                              | 
| Suspend Open   | A case which has been suspended that will move to an Open status again once the Diary Date has lapsed.                                                         |
| Suspend Closed | A case which has been suspended that will moved to a Closed status once the Diary Date has lapsed.                                                             |
| Closed         | A closed case whereby the Case Key and Case Key Value will be eligible for reopening.                                                                          |
| Suspend Bypass | A status indicating that a case would have been created,  except it was sampled out.  The case will move to a closed status up on the lapse of the Diary Date. |

Cases Workflows Filters are a means to access Cases via the Cases page. Case Workflow Filters allows for the filtering
of cases based on a customisable filter. To create a Cases Workflow Filter, start by navigating to Models >>  Cases
Workflows >> Cases Workflow Filter:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-filter-01.png route=/models state="Models page, Case Workflows tab, case-workflow-filter screen replacing the Razor-era CasesWorkflowsFilterTopOfTree.png" -->
![Image](CasesWorkflowsFilterTopOfTree.png)

For the Cases Workflow Detailed Financial Transaction Cases, Click on the tree node to add a new Cases Workflow Filter:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-filter-02.png route=/models state="Models page, Case Workflows tab, case-workflow-filter screen replacing the Razor-era EmptyCasesWorkflowFilter.png" -->
![Image](EmptyCasesWorkflowFilter.png)

The cases workflow filter accepts the following parameters:

| Value  | Description                                                              | Example                                                       |
|--------|--------------------------------------------------------------------------|---------------------------------------------------------------|
| Filter | A query builder that allows both filtering and sorting of created cases. | Sort: Created Date,  oldest first. Rule: Closed Status = Open |

Complete the form as above or as follows:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-filter-03.png route=/models state="Models page, Case Workflows tab, case-workflow-filter screen replacing the Razor-era ExampleCasesWorkflowFilter.png" -->
![Image](ExampleCasesWorkflowFilter.png)

Scroll down to add a version of the Cases Workflow Filter:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-filter-04.png route=/models state="Models page, Case Workflows tab, case-workflow-filter screen replacing the Razor-era AddedVersionCasesWorkflowsFilter.png" -->
![Image](AddedVersionCasesWorkflowsFilter.png)

The Cases Workflow Filter is available in the Cases page:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-filter-05.png route=/models state="Models page, Case Workflows tab, case-workflow-filter screen replacing the Razor-era LocationOfCasesPage.png" -->
![Image](LocationOfCasesPage.png)

Navigate to the Cases page:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-filter-06.png route=/models state="Models page, Case Workflows tab, case-workflow-filter screen replacing the Razor-era NewCasesWorkflowFilter.png" -->
![Image](NewCasesWorkflowFilter.png)

Upon navigation to the Cases page the new Cases Workflow Filter can be seen immediately to afford access to the cases.
Clicking on the entry for the Cases Workflow Filter will return cases if available and also make the filter available
for peeking by the end user:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-filter-07.png route=/models state="Models page, Case Workflows tab, case-workflow-filter screen replacing the Razor-era CaseWorkflowFilterSelected.png" -->
![Image](CaseWorkflowFilterSelected.png)

The availability of cases and the peeking of cases is explained in more detail elsewhere in the documentation, as part
of case working.

<!-- TODO(docs): Verify the SelectJson, FilterJson and FilterTokens fields against CaseWorkflowFilterDto; these are produced by the case search builder, not typed by hand. -->
<!-- TODO(docs): Document that a filter cannot be moved to another Case Workflow by an update. -->
