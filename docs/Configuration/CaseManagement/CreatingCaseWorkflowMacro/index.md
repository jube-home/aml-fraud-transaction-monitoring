---
layout: default
title: Case Workflow Macro
nav_order: 9
parent: Case Management
grand_parent: Configuration
---

# Case Workflow Macro

There are a multitude of functions in the case page for the purposes of updating and annotating a case. Each of these
update functions have their own user interface controls. For example, to update the closed status, it is a simple matter
of just changing the value in the status bar drop down:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-01.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era TypicalOptionsToUpdateCases.png" -->
![Image](TypicalOptionsToUpdateCases.png)

The case page can be extended by Macros which may invoke various Javascript functions on the case page to automate a
series of common click sequences to expedite the case working process for an analyst.

The case pages Javascript functions are undocumented. The documentation as follows serves only to illustrate the
functionality in general term.

To create a macro, start by navigating to Models >> Cases Workflows >> Cases Workflow Macro:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-02.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era CasesWorkflowsMacroTopOfTree.png" -->
![Image](CasesWorkflowsMacroTopOfTree.png)

Expand to the second level of the tree for a model, clicking on the Cases Workflow to create new Cases Workflow Macro:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-03.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era EmptyCasesWorkflowMacro.png" -->
![Image](EmptyCasesWorkflowMacro.png)

The Cases Workflow Macro form takes the following parameters:

| Value      | Description                                                                                                                                                                                            | Example                      |
|------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------|
| Javascript | Javascript that will be invoked upon a click of the icon button. Do not include script tags as this will be handled by the Case page via the onClick event and then the eval() function of Javascript. | alert('This is an example'); |
| Image      | The image to be included in the image button. The name will represent the tool-tip of the button.                                                                                                      | Image                        |

Complete the form as above parameters or as below (use any image):

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-04.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era ExampleFieldsForCasesWorkflowMacro.png" -->
![Image](ExampleFieldsForCasesWorkflowMacro.png)

Click add to create a version of the Cases Workflow Macro entry:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-05.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era VersionOfCasesWorkflowMacros.png" -->
![Image](VersionOfCasesWorkflowMacros.png)

To invoke the Cases Workflow Macro, navigate to a case record:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-06.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era NavigateToCase.png" -->
![Image](NavigateToCase.png)

Notice in the top left hand corner an image button has been created, which will contain the Javascript function payload:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-07.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era LocationOfNewMacro.png" -->
![Image](LocationOfNewMacro.png)

To invoke the Cases Workflow Macro, simply click on the icon:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-08.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era AlertOnClick.png" -->
![Image](AlertOnClick.png)

Keeping in mind that the alert is part of the Javascript, the execution on conclusion will be another alert to the
effect of Done:

<!-- TODO(screenshot): file=case-workflow-config-case-workflow-macro-09.png route=/models state="Models page, Case Workflows tab, case-workflow-macro screen replacing the Razor-era Done.png" -->
![Image](Done.png)

Macros become especially powerful when used in conjunction with the common HTTP Endpoint or Notification functionality.

<!-- TODO(docs): Document that a macro runs only against a case of the macro's own Case Workflow (CaseWorkflowMacroExecutionService now uses GetByIdActiveForCaseWorkflowGuidAsync). -->
<!-- TODO(docs): Document that macro HTTP endpoints in payload (legacy) mode are called without the outbound host policy: owner decision pending, see SHARED-TODO BUG. -->
<!-- TODO(docs): Document the audit written to the case's messages for every macro call (CaseWorkflowDispatch). -->
