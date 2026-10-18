---
layout: default
title: Tool Inventory
nav_order: 2
parent: Agent Hierarchy
grand_parent: Ask Jooby
---

# Tool inventory

Every operation `ServiceToolCatalogue` carries, one entry each, with the agent that owns it, the permission the owning
service enforces, and the file it is declared in. This is the exhaustive backstop for the hierarchy
in [index.md](index.md): where routing by meaning fails, an exact tool name, a service name or a permission number
resolves here.

Owning agent means the agent the operation is filed under, and for a change it is the only agent holding it. A read is
also held by the Author of the same slice, which is not recorded row by row here because it follows from the slice: an
agent that can change an object holds every read of that object, its validation included. [index.md](index.md) lists
each agent's whole toolbox.

|                                                               |     |
|---------------------------------------------------------------|-----|
| operations                                                    | 699 |
| read                                                          | 520 |
| write                                                         | 126 |
| delete                                                        | 53  |
| landlord only                                                 | 86  |
| owning agents                                                 | 162 |
| read assignments including an Author's reads of its own slice | 841 |

## Index

| Operation                                                                           | Kind   | Owning agent                                                  | Permissions                                                                                     |
|-------------------------------------------------------------------------------------|--------|---------------------------------------------------------------|-------------------------------------------------------------------------------------------------|
| `ActivationWatcherReplay`                                                           | Read   | `activation-watcher-and-archive`                              | 30                                                                                              |
| `AdvancedAnalyticsInstanceAllocateToModel`                                          | Write  | `advanced-analytics-instance-author`                          | 47                                                                                              |
| `AdvancedAnalyticsInstanceCount`                                                    | Read   | `advanced-analytics-instance-inspector`                       | 48                                                                                              |
| `AdvancedAnalyticsInstanceCreate`                                                   | Write  | `advanced-analytics-instance-author`                          | 48                                                                                              |
| `AdvancedAnalyticsInstanceDeallocate`                                               | Write  | `advanced-analytics-instance-author`                          | 47                                                                                              |
| `AdvancedAnalyticsInstanceDelete`                                                   | Delete | `advanced-analytics-instance-author`                          | 48                                                                                              |
| `AdvancedAnalyticsInstanceFilter`                                                   | Read   | `advanced-analytics-instance-inspector`                       | 48                                                                                              |
| `AdvancedAnalyticsInstanceFilterFields`                                             | Read   | `advanced-analytics-instance-inspector`                       | 48                                                                                              |
| `AdvancedAnalyticsInstanceGet`                                                      | Read   | `advanced-analytics-instance-inspector`                       | 48                                                                                              |
| `AdvancedAnalyticsInstanceGetByGuid`                                                | Read   | `advanced-analytics-instance-inspector`                       | 48                                                                                              |
| `AdvancedAnalyticsInstanceList`                                                     | Read   | `advanced-analytics-instance-inspector`                       | 48                                                                                              |
| `AdvancedAnalyticsInstanceResubmit`                                                 | Write  | `advanced-analytics-instance-author`                          | 47                                                                                              |
| `AdvancedAnalyticsInstanceRetract`                                                  | Write  | `advanced-analytics-instance-author`                          | 47                                                                                              |
| `AdvancedAnalyticsInstanceUpdate`                                                   | Write  | `advanced-analytics-instance-author`                          | 48                                                                                              |
| `AdvancedAnalyticsInstanceValidate`                                                 | Read   | `advanced-analytics-instance-test-bench`                      | 48                                                                                              |
| `AdvancedAnalyticsModelCompareVersions`                                             | Read   | `advanced-analytics-model-inspector`                          | 48                                                                                              |
| `AdvancedAnalyticsModelGet`                                                         | Read   | `advanced-analytics-model-inspector`                          | 48                                                                                              |
| `AdvancedAnalyticsModelGetPromotionEvidence`                                        | Read   | `advanced-analytics-model-inspector`                          | 48                                                                                              |
| `AdvancedAnalyticsModelGetVersion`                                                  | Read   | `advanced-analytics-model-inspector`                          | 48                                                                                              |
| `AdvancedAnalyticsModelList`                                                        | Read   | `advanced-analytics-model-inspector`                          | 48                                                                                              |
| `AdvancedAnalyticsModelListVersions`                                                | Read   | `advanced-analytics-model-inspector`                          | 48                                                                                              |
| `AdvancedAnalyticsModelPromoteVersion`                                              | Write  | `advanced-analytics-model-author`                             | 47                                                                                              |
| `AdvancedAnalyticsModelRollback`                                                    | Write  | `advanced-analytics-model-author`                             | 47                                                                                              |
| `AdvancedAnalyticsTrainingDataGet`                                                  | Read   | `advanced-analytics-training-data`                            | 48                                                                                              |
| `AdvancedAnalyticsTrainingDataPut`                                                  | Write  | `advanced-analytics-training-data`                            | 66                                                                                              |
| `ApplicationLogEntryList`                                                           | Read   | `application-and-container-logs`                              | landlord                                                                                        |
| `ArchiveTag`                                                                        | Write  | `activation-watcher-and-archive`                              | 1                                                                                               |
| `ArchiverStagePerformanceCounterList`                                               | Read   | `activation-watcher-and-archive`                              | landlord                                                                                        |
| `ArchiverWarningList`                                                               | Read   | `activation-watcher-and-archive`                              | landlord                                                                                        |
| `AskJoobyConversationCreate`                                                        | Write  | `ask-jooby-observability`                                     | 65                                                                                              |
| `AskJoobyConversationGetMessages`                                                   | Read   | `ask-jooby-observability`                                     | 65                                                                                              |
| `AskJoobyConversationList`                                                          | Read   | `ask-jooby-observability`                                     | 65                                                                                              |
| `AskJoobyConversationSendMessage`                                                   | Write  | `ask-jooby-observability`                                     | 65                                                                                              |
| `AskJoobyRagChunkSearch`                                                            | Read   | `ask-jooby-observability`                                     | 65                                                                                              |
| `CancelAdvancedAnalyticsTrainingRun`                                                | Write  | `advanced-analytics-training-classifier-trees`                | 66                                                                                              |
| `CaptureQueueHealthList`                                                            | Read   | `engine-queue-and-throughput-counters`                        | landlord                                                                                        |
| `CaseByCaseKeyValueGet`                                                             | Read   | `case-lookup-and-notes`                                       | 1                                                                                               |
| `CaseByIdGet`                                                                       | Read   | `case-lookup-and-notes`                                       | 1                                                                                               |
| `CaseBySessionCaseSearchCompileGet`                                                 | Write  | `case-lookup-and-notes`                                       | 1                                                                                               |
| `CaseCreateFromCaseKeyValue`                                                        | Write  | `case-record-and-files`                                       | 1                                                                                               |
| `CaseCreationStagePerformanceCounterList`                                           | Read   | `case-lookup-and-notes`                                       | landlord                                                                                        |
| `CaseCreationWarningList`                                                           | Read   | `case-journal-and-events`                                     | landlord                                                                                        |
| `CaseEventByCaseKeyValueGet`                                                        | Read   | `case-journal-and-events`                                     | 1                                                                                               |
| `CaseFileListByCaseKeyValue`                                                        | Read   | `case-record-and-files`                                       | 1                                                                                               |
| `CaseFileRemove`                                                                    | Delete | `case-record-and-files`                                       | 1                                                                                               |
| `CaseFileUpload`                                                                    | Write  | `case-record-and-files`                                       | 1                                                                                               |
| `CaseGet`                                                                           | Read   | `case-record-and-files`                                       | 1                                                                                               |
| `CaseJournalGet`                                                                    | Read   | `case-journal-and-events`                                     | 1                                                                                               |
| `CaseList`                                                                          | Read   | `case-record-and-files`                                       | 1                                                                                               |
| `CaseNoteByCaseKeyValueGet`                                                         | Read   | `case-journal-and-events`                                     | 1                                                                                               |
| `CaseNoteInsert`                                                                    | Write  | `case-lookup-and-notes`                                       | 1                                                                                               |
| `CaseNoteListByCaseKeyValue`                                                        | Read   | `case-lookup-and-notes`                                       | 1                                                                                               |
| `CaseNoteValidate`                                                                  | Read   | `case-lookup-and-notes`                                       | 1                                                                                               |
| `CaseUpdate`                                                                        | Write  | `case-record-and-files`                                       | 1                                                                                               |
| `CaseWorkflowActionCount`                                                           | Read   | `case-workflow-action-inspector`                              | 22                                                                                              |
| `CaseWorkflowActionCreate`                                                          | Write  | `case-workflow-action-author`                                 | 22                                                                                              |
| `CaseWorkflowActionDelete`                                                          | Delete | `case-workflow-action-author`                                 | 22                                                                                              |
| `CaseWorkflowActionFilter`                                                          | Read   | `case-workflow-action-inspector`                              | 22                                                                                              |
| `CaseWorkflowActionFilterFields`                                                    | Read   | `case-workflow-action-inspector`                              | 22                                                                                              |
| `CaseWorkflowActionGet`                                                             | Read   | `case-workflow-action-inspector`                              | 22                                                                                              |
| `CaseWorkflowActionGetByGuid`                                                       | Read   | `case-workflow-action-inspector`                              | 22                                                                                              |
| `CaseWorkflowActionList`                                                            | Read   | `case-workflow-action-inspector`                              | 22                                                                                              |
| `CaseWorkflowActionListByWorkflowGuidActiveOnly`                                    | Read   | `case-workflow-action-inspector`                              | 22, 1                                                                                           |
| `CaseWorkflowActionListByWorkflowIdActiveOnly`                                      | Read   | `case-workflow-action-inspector`                              | 22, 1                                                                                           |
| `CaseWorkflowActionRoleCreate`                                                      | Write  | `action-and-display-role-grants`                              | 22                                                                                              |
| `CaseWorkflowActionRoleDelete`                                                      | Delete | `action-and-display-role-grants`                              | 22                                                                                              |
| `CaseWorkflowActionRoleListByCaseWorkflowActionGuid`                                | Read   | `action-and-display-role-grants`                              | 22                                                                                              |
| `CaseWorkflowActionUpdate`                                                          | Write  | `case-workflow-action-author`                                 | 22                                                                                              |
| `CaseWorkflowActionValidate`                                                        | Read   | `case-workflow-action-test-bench`                             | 22                                                                                              |
| `CaseWorkflowCount`                                                                 | Read   | `case-workflow-inspector`                                     | 18                                                                                              |
| `CaseWorkflowCreate`                                                                | Write  | `case-workflow-author`                                        | 18                                                                                              |
| `CaseWorkflowDelete`                                                                | Delete | `case-workflow-author`                                        | 18                                                                                              |
| `CaseWorkflowDisplayCount`                                                          | Read   | `case-workflow-display-inspector`                             | 23                                                                                              |
| `CaseWorkflowDisplayCreate`                                                         | Write  | `case-workflow-display-author`                                | 23                                                                                              |
| `CaseWorkflowDisplayDelete`                                                         | Delete | `case-workflow-display-author`                                | 23                                                                                              |
| `CaseWorkflowDisplayExecutionExecute`                                               | Read   | `form-entries-and-macro-executions`                           | 1                                                                                               |
| `CaseWorkflowDisplayFilter`                                                         | Read   | `case-workflow-display-inspector`                             | 23                                                                                              |
| `CaseWorkflowDisplayFilterFields`                                                   | Read   | `case-workflow-display-inspector`                             | 23                                                                                              |
| `CaseWorkflowDisplayGet`                                                            | Read   | `case-workflow-display-inspector`                             | 23                                                                                              |
| `CaseWorkflowDisplayGetByGuid`                                                      | Read   | `case-workflow-display-inspector`                             | 23                                                                                              |
| `CaseWorkflowDisplayList`                                                           | Read   | `case-workflow-display-inspector`                             | 23                                                                                              |
| `CaseWorkflowDisplayListByWorkflowGuidActiveOnly`                                   | Read   | `case-workflow-display-inspector`                             | 23, 1                                                                                           |
| `CaseWorkflowDisplayListByWorkflowIdActiveOnly`                                     | Read   | `case-workflow-display-inspector`                             | 23, 1                                                                                           |
| `CaseWorkflowDisplayRoleCreate`                                                     | Write  | `action-and-display-role-grants`                              | 23                                                                                              |
| `CaseWorkflowDisplayRoleDelete`                                                     | Delete | `action-and-display-role-grants`                              | 23                                                                                              |
| `CaseWorkflowDisplayRoleListByCaseWorkflowDisplayGuid`                              | Read   | `action-and-display-role-grants`                              | 23                                                                                              |
| `CaseWorkflowDisplayUpdate`                                                         | Write  | `case-workflow-display-author`                                | 23                                                                                              |
| `CaseWorkflowDisplayValidate`                                                       | Read   | `case-workflow-display-test-bench`                            | 23                                                                                              |
| `CaseWorkflowFilter`                                                                | Read   | `case-workflow-inspector`                                     | 18                                                                                              |
| `CaseWorkflowFilterCount`                                                           | Read   | `case-workflow-filter-inspector-1`                            | 25, 1                                                                                           |
| `CaseWorkflowFilterCreate`                                                          | Write  | `case-workflow-filter-author`                                 | 25                                                                                              |
| `CaseWorkflowFilterDelete`                                                          | Delete | `case-workflow-filter-author`                                 | 25                                                                                              |
| `CaseWorkflowFilterFields`                                                          | Read   | `case-workflow-inspector`                                     | 18                                                                                              |
| `CaseWorkflowFilterFilter`                                                          | Read   | `case-workflow-filter-inspector-1`                            | 25, 1                                                                                           |
| `CaseWorkflowFilterFilterFields`                                                    | Read   | `case-workflow-filter-inspector-1`                            | 25, 1                                                                                           |
| `CaseWorkflowFilterGet`                                                             | Read   | `case-workflow-filter-inspector-1`                            | 25, 1                                                                                           |
| `CaseWorkflowFilterGetByGuid`                                                       | Read   | `case-workflow-filter-inspector-2`                            | 25, 1                                                                                           |
| `CaseWorkflowFilterList`                                                            | Read   | `case-workflow-filter-inspector-1`                            | 25, 1                                                                                           |
| `CaseWorkflowFilterListByWorkflowGuidActiveOnly`                                    | Read   | `case-workflow-filter-inspector-1`                            | 25, 1                                                                                           |
| `CaseWorkflowFilterListByWorkflowIdActiveOnly`                                      | Read   | `case-workflow-filter-inspector-1`                            | 25, 1                                                                                           |
| `CaseWorkflowFilterRoleCreate`                                                      | Write  | `filter-and-form-role-grants`                                 | 25                                                                                              |
| `CaseWorkflowFilterRoleDelete`                                                      | Delete | `filter-and-form-role-grants`                                 | 25                                                                                              |
| `CaseWorkflowFilterRoleListByCaseWorkflowFilterGuid`                                | Read   | `filter-and-form-role-grants`                                 | 25                                                                                              |
| `CaseWorkflowFilterUpdate`                                                          | Write  | `case-workflow-filter-author`                                 | 25                                                                                              |
| `CaseWorkflowFilterValidate`                                                        | Read   | `case-workflow-filter-test-bench`                             | 25                                                                                              |
| `CaseWorkflowFormCount`                                                             | Read   | `case-workflow-form-inspector`                                | 21                                                                                              |
| `CaseWorkflowFormCreate`                                                            | Write  | `case-workflow-form-author`                                   | 21                                                                                              |
| `CaseWorkflowFormDelete`                                                            | Delete | `case-workflow-form-author`                                   | 21                                                                                              |
| `CaseWorkflowFormEntryByCaseKeyValueGet`                                            | Read   | `form-entries-and-macro-executions`                           | 1                                                                                               |
| `CaseWorkflowFormEntryInsert`                                                       | Write  | `form-entries-and-macro-executions`                           | 1                                                                                               |
| `CaseWorkflowFormEntryListByCaseKeyValue`                                           | Read   | `form-entries-and-macro-executions`                           | 1                                                                                               |
| `CaseWorkflowFormEntryValidate`                                                     | Read   | `form-entries-and-macro-executions`                           | 1                                                                                               |
| `CaseWorkflowFormEntryValueListByCaseWorkflowFormEntryId`                           | Read   | `form-entries-and-macro-executions`                           | 1                                                                                               |
| `CaseWorkflowFormFilter`                                                            | Read   | `case-workflow-form-inspector`                                | 21                                                                                              |
| `CaseWorkflowFormFilterFields`                                                      | Read   | `case-workflow-form-inspector`                                | 21                                                                                              |
| `CaseWorkflowFormGet`                                                               | Read   | `case-workflow-form-inspector`                                | 21                                                                                              |
| `CaseWorkflowFormGetByGuid`                                                         | Read   | `case-workflow-form-inspector`                                | 21                                                                                              |
| `CaseWorkflowFormList`                                                              | Read   | `case-workflow-form-inspector`                                | 21                                                                                              |
| `CaseWorkflowFormListByWorkflowGuidActiveOnly`                                      | Read   | `case-workflow-form-inspector`                                | 21, 1                                                                                           |
| `CaseWorkflowFormListByWorkflowIdActiveOnly`                                        | Read   | `case-workflow-form-inspector`                                | 21, 1                                                                                           |
| `CaseWorkflowFormRoleCreate`                                                        | Write  | `filter-and-form-role-grants`                                 | 21                                                                                              |
| `CaseWorkflowFormRoleDelete`                                                        | Delete | `filter-and-form-role-grants`                                 | 21                                                                                              |
| `CaseWorkflowFormRoleListByCaseWorkflowFormGuid`                                    | Read   | `filter-and-form-role-grants`                                 | 21                                                                                              |
| `CaseWorkflowFormUpdate`                                                            | Write  | `case-workflow-form-author`                                   | 21                                                                                              |
| `CaseWorkflowFormValidate`                                                          | Read   | `case-workflow-form-test-bench`                               | 21                                                                                              |
| `CaseWorkflowGet`                                                                   | Read   | `case-workflow-inspector`                                     | 18                                                                                              |
| `CaseWorkflowGetByGuid`                                                             | Read   | `case-workflow-inspector`                                     | 18                                                                                              |
| `CaseWorkflowList`                                                                  | Read   | `case-workflow-inspector`                                     | 18                                                                                              |
| `CaseWorkflowListByModelActiveOnly`                                                 | Read   | `case-workflow-inspector`                                     | 18, 1                                                                                           |
| `CaseWorkflowMacroCount`                                                            | Read   | `case-workflow-macro-inspector`                               | 24                                                                                              |
| `CaseWorkflowMacroCreate`                                                           | Write  | `case-workflow-macro-author`                                  | 24                                                                                              |
| `CaseWorkflowMacroDelete`                                                           | Delete | `case-workflow-macro-author`                                  | 24                                                                                              |
| `CaseWorkflowMacroExecutionExecute`                                                 | Write  | `form-entries-and-macro-executions`                           | 1                                                                                               |
| `CaseWorkflowMacroFilter`                                                           | Read   | `case-workflow-macro-inspector`                               | 24                                                                                              |
| `CaseWorkflowMacroFilterFields`                                                     | Read   | `case-workflow-macro-inspector`                               | 24                                                                                              |
| `CaseWorkflowMacroGet`                                                              | Read   | `case-workflow-macro-inspector`                               | 24                                                                                              |
| `CaseWorkflowMacroGetByGuid`                                                        | Read   | `case-workflow-macro-inspector`                               | 24                                                                                              |
| `CaseWorkflowMacroList`                                                             | Read   | `case-workflow-macro-inspector`                               | 24                                                                                              |
| `CaseWorkflowMacroListByWorkflowGuidActiveOnly`                                     | Read   | `case-workflow-macro-inspector`                               | 24, 1                                                                                           |
| `CaseWorkflowMacroListByWorkflowIdActiveOnly`                                       | Read   | `case-workflow-macro-inspector`                               | 24, 1                                                                                           |
| `CaseWorkflowMacroRoleCreate`                                                       | Write  | `macro-and-workflow-role-grants`                              | 24                                                                                              |
| `CaseWorkflowMacroRoleDelete`                                                       | Delete | `macro-and-workflow-role-grants`                              | 24                                                                                              |
| `CaseWorkflowMacroRoleListByCaseWorkflowMacroGuid`                                  | Read   | `macro-and-workflow-role-grants`                              | 24                                                                                              |
| `CaseWorkflowMacroUpdate`                                                           | Write  | `case-workflow-macro-author`                                  | 24                                                                                              |
| `CaseWorkflowMacroValidate`                                                         | Read   | `case-workflow-macro-test-bench`                              | 24                                                                                              |
| `CaseWorkflowPriorityList`                                                          | Read   | `case-workflow-priority`                                      | 1                                                                                               |
| `CaseWorkflowRoleCreate`                                                            | Write  | `macro-and-workflow-role-grants`                              | 18                                                                                              |
| `CaseWorkflowRoleDelete`                                                            | Delete | `macro-and-workflow-role-grants`                              | 18                                                                                              |
| `CaseWorkflowRoleListByCaseWorkflowGuid`                                            | Read   | `macro-and-workflow-role-grants`                              | 18                                                                                              |
| `CaseWorkflowStatusCount`                                                           | Read   | `case-workflow-status-inspector-1`                            | 19                                                                                              |
| `CaseWorkflowStatusCreate`                                                          | Write  | `case-workflow-status-author`                                 | 19                                                                                              |
| `CaseWorkflowStatusDelete`                                                          | Delete | `case-workflow-status-author`                                 | 19                                                                                              |
| `CaseWorkflowStatusFilter`                                                          | Read   | `case-workflow-status-inspector-1`                            | 19                                                                                              |
| `CaseWorkflowStatusFilterFields`                                                    | Read   | `case-workflow-status-inspector-1`                            | 19                                                                                              |
| `CaseWorkflowStatusGet`                                                             | Read   | `case-workflow-status-inspector-2`                            | 19                                                                                              |
| `CaseWorkflowStatusGetByGuid`                                                       | Read   | `case-workflow-status-inspector-2`                            | 19                                                                                              |
| `CaseWorkflowStatusList`                                                            | Read   | `case-workflow-status-inspector-1`                            | 19                                                                                              |
| `CaseWorkflowStatusListByWorkflowGuid`                                              | Read   | `case-workflow-status-inspector-1`                            | 17, 19, 25, 1                                                                                   |
| `CaseWorkflowStatusListByWorkflowGuidActiveOnly`                                    | Read   | `case-workflow-status-inspector-1`                            | 19, 1                                                                                           |
| `CaseWorkflowStatusListByWorkflowIdActiveOnly`                                      | Read   | `case-workflow-status-inspector-1`                            | 19, 1                                                                                           |
| `CaseWorkflowStatusRoleCreate`                                                      | Write  | `status-and-xpath-role-grants`                                | 19                                                                                              |
| `CaseWorkflowStatusRoleDelete`                                                      | Delete | `status-and-xpath-role-grants`                                | 19                                                                                              |
| `CaseWorkflowStatusRoleListByCaseWorkflowStatusGuid`                                | Read   | `status-and-xpath-role-grants`                                | 19                                                                                              |
| `CaseWorkflowStatusUpdate`                                                          | Write  | `case-workflow-status-author`                                 | 19                                                                                              |
| `CaseWorkflowStatusValidate`                                                        | Read   | `case-workflow-status-test-bench`                             | 19                                                                                              |
| `CaseWorkflowUpdate`                                                                | Write  | `case-workflow-author`                                        | 18                                                                                              |
| `CaseWorkflowValidate`                                                              | Read   | `case-workflow-test-bench`                                    | 18                                                                                              |
| `CaseWorkflowXPathCount`                                                            | Read   | `case-workflow-x-path-inspector`                              | 20                                                                                              |
| `CaseWorkflowXPathCreate`                                                           | Write  | `case-workflow-x-path-author`                                 | 20                                                                                              |
| `CaseWorkflowXPathDelete`                                                           | Delete | `case-workflow-x-path-author`                                 | 20                                                                                              |
| `CaseWorkflowXPathFilter`                                                           | Read   | `case-workflow-x-path-inspector`                              | 20                                                                                              |
| `CaseWorkflowXPathFilterFields`                                                     | Read   | `case-workflow-x-path-inspector`                              | 20                                                                                              |
| `CaseWorkflowXPathGet`                                                              | Read   | `case-workflow-x-path-inspector`                              | 20                                                                                              |
| `CaseWorkflowXPathGetByGuid`                                                        | Read   | `case-workflow-x-path-inspector`                              | 20                                                                                              |
| `CaseWorkflowXPathList`                                                             | Read   | `case-workflow-x-path-inspector`                              | 20                                                                                              |
| `CaseWorkflowXPathListByWorkflowGuidActiveDrillOnly`                                | Read   | `case-workflow-x-path-inspector`                              | 20, 1                                                                                           |
| `CaseWorkflowXPathListByWorkflowIdActiveDrillOnly`                                  | Read   | `case-workflow-x-path-inspector`                              | 20, 1                                                                                           |
| `CaseWorkflowXPathRoleCreate`                                                       | Write  | `status-and-xpath-role-grants`                                | 20                                                                                              |
| `CaseWorkflowXPathRoleDelete`                                                       | Delete | `status-and-xpath-role-grants`                                | 20                                                                                              |
| `CaseWorkflowXPathRoleListByCaseWorkflowXPathGuid`                                  | Read   | `status-and-xpath-role-grants`                                | 20                                                                                              |
| `CaseWorkflowXPathUpdate`                                                           | Write  | `case-workflow-x-path-author`                                 | 20                                                                                              |
| `CaseWorkflowXPathValidate`                                                         | Read   | `case-workflow-x-path-test-bench`                             | 20                                                                                              |
| `CompletionsGetByCaseWorkflowGuidIncludingDeleted`                                  | Read   | `completions`                                                 | 1                                                                                               |
| `CompletionsGetByCaseWorkflowId`                                                    | Read   | `completions`                                                 | 25, 17, 20                                                                                      |
| `CompletionsGetByCaseWorkflowIdIncludingDeleted`                                    | Read   | `completions`                                                 | 1                                                                                               |
| `CompletionsGetByEntityAnalysisModelId`                                             | Read   | `completions`                                                 | 16                                                                                              |
| `CompletionsGetByEntityAnalysisModelIdParseTypeId`                                  | Read   | `completions`                                                 | 8, 10, 13, 14, 17, 20, 26                                                                       |
| `CompletionsGetByExhaustiveInstanceDataset`                                         | Read   | `completions`                                                 | 16                                                                                              |
| `ContainerLogEntryList`                                                             | Read   | `application-and-container-logs`                              | landlord                                                                                        |
| `DockerContainerListNow`                                                            | Read   | `containers-load-balancer-and-overlay-network`                | landlord                                                                                        |
| `DockerContainerMetricList`                                                         | Read   | `containers-load-balancer-and-overlay-network`                | landlord                                                                                        |
| `DockerEventList`                                                                   | Read   | `containers-load-balancer-and-overlay-network`                | landlord                                                                                        |
| `DockerHostInfoNow`                                                                 | Read   | `containers-load-balancer-and-overlay-network`                | landlord                                                                                        |
| `DockerHostMetricList`                                                              | Read   | `containers-load-balancer-and-overlay-network`                | landlord                                                                                        |
| `DotNetRuntimeMetricList`                                                           | Read   | `telemetry-log-counters-and-runtime-metrics`                  | landlord                                                                                        |
| `EntityAnalysisAsynchronousQueueBalanceList`                                        | Read   | `engine-queue-and-throughput-counters`                        | landlord                                                                                        |
| `EntityAnalysisInlineScriptCount`                                                   | Read   | `entity-analysis-observability`                               | 9                                                                                               |
| `EntityAnalysisInlineScriptFilter`                                                  | Read   | `entity-analysis-observability`                               | 9                                                                                               |
| `EntityAnalysisInlineScriptFilterFields`                                            | Read   | `entity-analysis-observability`                               | 9                                                                                               |
| `EntityAnalysisInlineScriptList`                                                    | Read   | `entity-analysis-observability`                               | 9                                                                                               |
| `EntityAnalysisModelAbstractionCalculationCount`                                    | Read   | `entity-analysis-model-abstraction-calculation-inspector`     | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationCreate`                                   | Write  | `entity-analysis-model-abstraction-calculation-author`        | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationDelete`                                   | Delete | `entity-analysis-model-abstraction-calculation-author`        | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationExecute`                                  | Read   | `entity-analysis-model-abstraction-calculation-test-bench`    | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationFilter`                                   | Read   | `entity-analysis-model-abstraction-calculation-inspector`     | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationFilterFields`                             | Read   | `entity-analysis-model-abstraction-calculation-inspector`     | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationGet`                                      | Read   | `entity-analysis-model-abstraction-calculation-inspector`     | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationGetByEntityAnalysisModelId`               | Read   | `entity-analysis-model-abstraction-calculation-inspector`     | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationGetByGuid`                                | Read   | `entity-analysis-model-abstraction-calculation-inspector`     | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationList`                                     | Read   | `entity-analysis-model-abstraction-calculation-inspector`     | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationParseRule`                                | Read   | `entity-analysis-model-abstraction-calculation-test-bench`    | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationUpdate`                                   | Write  | `entity-analysis-model-abstraction-calculation-author`        | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationValidate`                                 | Read   | `entity-analysis-model-abstraction-calculation-test-bench`    | 14                                                                                              |
| `EntityAnalysisModelAbstractionRuleBuildRuleFromBuilderJson`                        | Read   | `entity-analysis-model-abstraction-rule-test-bench`           | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleCount`                                           | Read   | `entity-analysis-model-abstraction-rule-inspector`            | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleCreate`                                          | Write  | `entity-analysis-model-abstraction-rule-author`               | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleDelete`                                          | Delete | `entity-analysis-model-abstraction-rule-author`               | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleExecute`                                         | Read   | `entity-analysis-model-abstraction-rule-test-bench`           | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleFilter`                                          | Read   | `entity-analysis-model-abstraction-rule-inspector`            | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleFilterFields`                                    | Read   | `entity-analysis-model-abstraction-rule-inspector`            | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleGet`                                             | Read   | `entity-analysis-model-abstraction-rule-inspector`            | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId`                      | Read   | `entity-analysis-model-abstraction-rule-inspector`            | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleGetByGuid`                                       | Read   | `entity-analysis-model-abstraction-rule-inspector`            | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleList`                                            | Read   | `entity-analysis-model-abstraction-rule-inspector`            | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleParseRule`                                       | Read   | `entity-analysis-model-abstraction-rule-test-bench`           | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleUpdate`                                          | Write  | `entity-analysis-model-abstraction-rule-author`               | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleValidate`                                        | Read   | `entity-analysis-model-abstraction-rule-test-bench`           | 13                                                                                              |
| `EntityAnalysisModelActivationRuleBuildRuleFromBuilderJson`                         | Read   | `entity-analysis-model-activation-rule-test-bench`            | 17                                                                                              |
| `EntityAnalysisModelActivationRuleCount`                                            | Read   | `entity-analysis-model-activation-rule-inspector`             | 17                                                                                              |
| `EntityAnalysisModelActivationRuleCreate`                                           | Write  | `entity-analysis-model-activation-rule-author`                | 17                                                                                              |
| `EntityAnalysisModelActivationRuleDelete`                                           | Delete | `entity-analysis-model-activation-rule-author`                | 17                                                                                              |
| `EntityAnalysisModelActivationRuleExecute`                                          | Read   | `entity-analysis-model-activation-rule-test-bench`            | 17                                                                                              |
| `EntityAnalysisModelActivationRuleFilter`                                           | Read   | `entity-analysis-model-activation-rule-inspector`             | 17                                                                                              |
| `EntityAnalysisModelActivationRuleFilterFields`                                     | Read   | `entity-analysis-model-activation-rule-inspector`             | 17                                                                                              |
| `EntityAnalysisModelActivationRuleGet`                                              | Read   | `entity-analysis-model-activation-rule-inspector`             | 17                                                                                              |
| `EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId`                       | Read   | `entity-analysis-model-activation-rule-inspector`             | 17                                                                                              |
| `EntityAnalysisModelActivationRuleGetByGuid`                                        | Read   | `entity-analysis-model-activation-rule-inspector`             | 17                                                                                              |
| `EntityAnalysisModelActivationRuleList`                                             | Read   | `entity-analysis-model-activation-rule-inspector`             | 17                                                                                              |
| `EntityAnalysisModelActivationRuleOverrideCount`                                    | Read   | `entity-analysis-model-activation-rule-override-inspector`    | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideCreate`                                   | Write  | `entity-analysis-model-activation-rule-override-author`       | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideDelete`                                   | Delete | `entity-analysis-model-activation-rule-override-author`       | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideFilter`                                   | Read   | `entity-analysis-model-activation-rule-override-inspector`    | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideFilterFields`                             | Read   | `entity-analysis-model-activation-rule-override-inspector`    | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideGet`                                      | Read   | `entity-analysis-model-activation-rule-override-inspector`    | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideGetByEntityAnalysisModelGuid`             | Read   | `entity-analysis-model-activation-rule-override-inspector`    | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideList`                                     | Read   | `entity-analysis-model-activation-rule-override-inspector`    | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideQueryGet`                                 | Read   | `entity-analysis-model-activation-rule-override-query`        | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideUpdate`                                   | Write  | `entity-analysis-model-activation-rule-override-author`       | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideUpdateDeleteExpiryDate`                   | Write  | `entity-analysis-model-activation-rule-override-author`       | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideUpdateOverrideKind`                       | Write  | `entity-analysis-model-activation-rule-override-author`       | 2                                                                                               |
| `EntityAnalysisModelActivationRuleOverrideValidate`                                 | Read   | `entity-analysis-model-activation-rule-override-test-bench`   | 2                                                                                               |
| `EntityAnalysisModelActivationRuleParseRule`                                        | Read   | `entity-analysis-model-activation-rule-test-bench`            | 17                                                                                              |
| `EntityAnalysisModelActivationRuleReset`                                            | Write  | `entity-analysis-model-activation-rule-author`                | 17                                                                                              |
| `EntityAnalysisModelActivationRuleUpdate`                                           | Write  | `entity-analysis-model-activation-rule-author`                | 17                                                                                              |
| `EntityAnalysisModelActivationRuleValidate`                                         | Read   | `entity-analysis-model-activation-rule-test-bench`            | 17                                                                                              |
| `EntityAnalysisModelAsynchronousQueueBalanceList`                                   | Read   | `engine-queue-and-throughput-counters`                        | landlord                                                                                        |
| `EntityAnalysisModelBacktestExplain`                                                | Read   | `entity-analysis-model-backtest-test-bench`                   | 61                                                                                              |
| `EntityAnalysisModelBacktestFields`                                                 | Read   | `entity-analysis-model-backtest-inspector`                    | 61                                                                                              |
| `EntityAnalysisModelBacktestList`                                                   | Read   | `entity-analysis-model-backtest-inspector`                    | 61                                                                                              |
| `EntityAnalysisModelBacktestResult`                                                 | Read   | `entity-analysis-model-backtest-inspector`                    | 61                                                                                              |
| `EntityAnalysisModelBacktestRunRule`                                                | Read   | `entity-analysis-model-backtest-inspector`                    | 61                                                                                              |
| `EntityAnalysisModelBacktestStatus`                                                 | Read   | `entity-analysis-model-backtest-inspector`                    | 61                                                                                              |
| `EntityAnalysisModelBacktestStop`                                                   | Write  | `entity-analysis-model-backtest-author`                       | 61                                                                                              |
| `EntityAnalysisModelBacktestSubmit`                                                 | Write  | `entity-analysis-model-backtest-author`                       | 61                                                                                              |
| `EntityAnalysisModelCount`                                                          | Read   | `entity-analysis-model-inspector`                             | 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 37, 3, 4, 1, 40 |
| `EntityAnalysisModelCreate`                                                         | Write  | `entity-analysis-model-author`                                | 6                                                                                               |
| `EntityAnalysisModelDelete`                                                         | Delete | `entity-analysis-model-author`                                | 6                                                                                               |
| `EntityAnalysisModelDependencyDependencies`                                         | Read   | `model-invocation-context-and-dependencies`                   | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelDependencyDependents`                                           | Read   | `model-invocation-context-and-dependencies`                   | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelDependencyIssues`                                               | Read   | `model-invocation-context-and-dependencies`                   | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelDictionaryCount`                                                | Read   | `entity-analysis-model-dictionary-inspector`                  | 4                                                                                               |
| `EntityAnalysisModelDictionaryCreate`                                               | Write  | `entity-analysis-model-dictionary-author`                     | 4                                                                                               |
| `EntityAnalysisModelDictionaryDelete`                                               | Delete | `entity-analysis-model-dictionary-author`                     | 4                                                                                               |
| `EntityAnalysisModelDictionaryFilter`                                               | Read   | `entity-analysis-model-dictionary-inspector`                  | 4                                                                                               |
| `EntityAnalysisModelDictionaryFilterFields`                                         | Read   | `entity-analysis-model-dictionary-inspector`                  | 4                                                                                               |
| `EntityAnalysisModelDictionaryGet`                                                  | Read   | `entity-analysis-model-dictionary-inspector`                  | 4                                                                                               |
| `EntityAnalysisModelDictionaryGetByEntityAnalysisModelId`                           | Read   | `entity-analysis-model-dictionary-inspector`                  | 4                                                                                               |
| `EntityAnalysisModelDictionaryGetByGuid`                                            | Read   | `entity-analysis-model-dictionary-inspector`                  | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpCount`                                             | Read   | `entity-analysis-model-dictionary-kvp-inspector`              | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpCreate`                                            | Write  | `entity-analysis-model-dictionary-kvp-author`                 | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpDelete`                                            | Delete | `entity-analysis-model-dictionary-kvp-author`                 | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpFilter`                                            | Read   | `entity-analysis-model-dictionary-kvp-inspector`              | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpFilterFields`                                      | Read   | `entity-analysis-model-dictionary-kvp-inspector`              | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpGet`                                               | Read   | `entity-analysis-model-dictionary-kvp-inspector`              | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId`              | Read   | `entity-analysis-model-dictionary-kvp-inspector`              | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpGetByGuid`                                         | Read   | `entity-analysis-model-dictionary-kvp-inspector`              | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpList`                                              | Read   | `entity-analysis-model-dictionary-kvp-inspector`              | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpUpdate`                                            | Write  | `entity-analysis-model-dictionary-kvp-author`                 | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpUploadCsv`                                         | Write  | `entity-analysis-model-dictionary-kvp-author`                 | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpValidate`                                          | Read   | `entity-analysis-model-dictionary-kvp-test-bench`             | 4                                                                                               |
| `EntityAnalysisModelDictionaryList`                                                 | Read   | `entity-analysis-model-dictionary-inspector`                  | 4                                                                                               |
| `EntityAnalysisModelDictionaryUpdate`                                               | Write  | `entity-analysis-model-dictionary-author`                     | 4                                                                                               |
| `EntityAnalysisModelDictionaryValidate`                                             | Read   | `entity-analysis-model-dictionary-test-bench`                 | 4                                                                                               |
| `EntityAnalysisModelFilter`                                                         | Read   | `entity-analysis-model-inspector`                             | 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 37, 3, 4, 1, 40 |
| `EntityAnalysisModelFilterFields`                                                   | Read   | `entity-analysis-model-inspector`                             | 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 37, 3, 4, 1, 40 |
| `EntityAnalysisModelGatewayRuleBuildRuleFromBuilderJson`                            | Read   | `entity-analysis-model-gateway-rule-test-bench`               | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleCount`                                               | Read   | `entity-analysis-model-gateway-rule-inspector`                | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleCreate`                                              | Write  | `entity-analysis-model-gateway-rule-author`                   | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleDelete`                                              | Delete | `entity-analysis-model-gateway-rule-author`                   | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleExecute`                                             | Read   | `entity-analysis-model-gateway-rule-test-bench`               | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleFilter`                                              | Read   | `entity-analysis-model-gateway-rule-inspector`                | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleFilterFields`                                        | Read   | `entity-analysis-model-gateway-rule-inspector`                | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleGet`                                                 | Read   | `entity-analysis-model-gateway-rule-inspector`                | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleGetByEntityAnalysisModelId`                          | Read   | `entity-analysis-model-gateway-rule-inspector`                | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleGetByGuid`                                           | Read   | `entity-analysis-model-gateway-rule-inspector`                | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleList`                                                | Read   | `entity-analysis-model-gateway-rule-inspector`                | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleParseRule`                                           | Read   | `entity-analysis-model-gateway-rule-test-bench`               | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleResetCounter`                                        | Write  | `entity-analysis-model-gateway-rule-author`                   | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleUpdate`                                              | Write  | `entity-analysis-model-gateway-rule-author`                   | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleValidate`                                            | Read   | `entity-analysis-model-gateway-rule-test-bench`               | 10                                                                                              |
| `EntityAnalysisModelGet`                                                            | Read   | `entity-analysis-model-inspector`                             | 6                                                                                               |
| `EntityAnalysisModelHistorySimulationAbstractionAggregation`                        | Read   | `model-integrity-and-sampling`                                | 1, 40                                                                                           |
| `EntityAnalysisModelHistorySimulationTtlCounterValue`                               | Read   | `model-integrity-and-sampling`                                | 1, 40                                                                                           |
| `EntityAnalysisModelHttpAdaptationCount`                                            | Read   | `entity-analysis-model-http-adaptation-inspector`             | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationCreate`                                           | Write  | `entity-analysis-model-http-adaptation-author`                | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationDelete`                                           | Delete | `entity-analysis-model-http-adaptation-author`                | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationFilter`                                           | Read   | `entity-analysis-model-http-adaptation-inspector`             | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationFilterFields`                                     | Read   | `entity-analysis-model-http-adaptation-inspector`             | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationGet`                                              | Read   | `entity-analysis-model-http-adaptation-inspector`             | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationGetByEntityAnalysisModelId`                       | Read   | `entity-analysis-model-http-adaptation-inspector`             | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationGetByGuid`                                        | Read   | `entity-analysis-model-http-adaptation-inspector`             | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationList`                                             | Read   | `entity-analysis-model-http-adaptation-inspector`             | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationUpdate`                                           | Write  | `entity-analysis-model-http-adaptation-author`                | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationValidate`                                         | Read   | `entity-analysis-model-http-adaptation-test-bench`            | 15                                                                                              |
| `EntityAnalysisModelInlineFunctionCount`                                            | Read   | `entity-analysis-model-inline-function-inspector`             | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionCreate`                                           | Write  | `entity-analysis-model-inline-function-author`                | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionDelete`                                           | Delete | `entity-analysis-model-inline-function-author`                | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionExecute`                                          | Read   | `entity-analysis-model-inline-function-test-bench`            | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionFilter`                                           | Read   | `entity-analysis-model-inline-function-inspector`             | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionFilterFields`                                     | Read   | `entity-analysis-model-inline-function-inspector`             | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionGet`                                              | Read   | `entity-analysis-model-inline-function-inspector`             | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionGetByEntityAnalysisModelId`                       | Read   | `entity-analysis-model-inline-function-inspector`             | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionGetByGuid`                                        | Read   | `entity-analysis-model-inline-function-inspector`             | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionList`                                             | Read   | `entity-analysis-model-inline-function-inspector`             | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionParseRule`                                        | Read   | `entity-analysis-model-inline-function-test-bench`            | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionUpdate`                                           | Write  | `entity-analysis-model-inline-function-author`                | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionValidate`                                         | Read   | `entity-analysis-model-inline-function-test-bench`            | 8                                                                                               |
| `EntityAnalysisModelInlineScriptCount`                                              | Read   | `entity-analysis-model-inline-script-inspector`               | 9                                                                                               |
| `EntityAnalysisModelInlineScriptCreate`                                             | Write  | `entity-analysis-model-inline-script-author`                  | 9                                                                                               |
| `EntityAnalysisModelInlineScriptDelete`                                             | Delete | `entity-analysis-model-inline-script-author`                  | 9                                                                                               |
| `EntityAnalysisModelInlineScriptExecute`                                            | Read   | `entity-analysis-model-inline-script-test-bench`              | 9                                                                                               |
| `EntityAnalysisModelInlineScriptFilter`                                             | Read   | `entity-analysis-model-inline-script-inspector`               | 9                                                                                               |
| `EntityAnalysisModelInlineScriptFilterFields`                                       | Read   | `entity-analysis-model-inline-script-inspector`               | 9                                                                                               |
| `EntityAnalysisModelInlineScriptGet`                                                | Read   | `entity-analysis-model-inline-script-inspector`               | 9                                                                                               |
| `EntityAnalysisModelInlineScriptGetByEntityAnalysisModelId`                         | Read   | `entity-analysis-model-inline-script-inspector`               | 9                                                                                               |
| `EntityAnalysisModelInlineScriptGetByGuid`                                          | Read   | `entity-analysis-model-inline-script-inspector`               | 9                                                                                               |
| `EntityAnalysisModelInlineScriptList`                                               | Read   | `entity-analysis-model-inline-script-inspector`               | 9                                                                                               |
| `EntityAnalysisModelInlineScriptUpdate`                                             | Write  | `entity-analysis-model-inline-script-author`                  | 9                                                                                               |
| `EntityAnalysisModelInlineScriptValidate`                                           | Read   | `entity-analysis-model-inline-script-test-bench`              | 9                                                                                               |
| `EntityAnalysisModelIntegrityCheck`                                                 | Read   | `model-integrity-and-sampling`                                | 60                                                                                              |
| `EntityAnalysisModelIntegrityEngineState`                                           | Read   | `model-integrity-and-sampling`                                | 60                                                                                              |
| `EntityAnalysisModelIntegrityGraph`                                                 | Read   | `model-integrity-and-sampling`                                | 60                                                                                              |
| `EntityAnalysisModelIntegrityModels`                                                | Read   | `model-integrity-and-sampling`                                | 60                                                                                              |
| `EntityAnalysisModelInvocationContextBlank`                                         | Read   | `model-invocation-context-and-dependencies`                   | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelInvocationContextFromArchive`                                   | Read   | `model-invocation-context-and-dependencies`                   | 1, 40                                                                                           |
| `EntityAnalysisModelInvocationContextFromRequestJson`                               | Read   | `model-invocation-context-and-dependencies`                   | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelInvocationContextOverlay`                                       | Read   | `model-invocation-context-and-dependencies`                   | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelList`                                                           | Read   | `entity-analysis-model-inspector`                             | 6                                                                                               |
| `EntityAnalysisModelListCount`                                                      | Read   | `entity-analysis-model-list-inspector`                        | 3                                                                                               |
| `EntityAnalysisModelListCreate`                                                     | Write  | `entity-analysis-model-list-author`                           | 3                                                                                               |
| `EntityAnalysisModelListDelete`                                                     | Delete | `entity-analysis-model-list-author`                           | 3                                                                                               |
| `EntityAnalysisModelListFilter`                                                     | Read   | `entity-analysis-model-list-inspector`                        | 3                                                                                               |
| `EntityAnalysisModelListFilterFields`                                               | Read   | `entity-analysis-model-list-inspector`                        | 3                                                                                               |
| `EntityAnalysisModelListGet`                                                        | Read   | `entity-analysis-model-list-inspector`                        | 3                                                                                               |
| `EntityAnalysisModelListGetByEntityAnalysisModelId`                                 | Read   | `entity-analysis-model-list-inspector`                        | 3                                                                                               |
| `EntityAnalysisModelListGetByGuid`                                                  | Read   | `entity-analysis-model-list-inspector`                        | 3                                                                                               |
| `EntityAnalysisModelListList`                                                       | Read   | `entity-analysis-model-list-inspector`                        | 3                                                                                               |
| `EntityAnalysisModelListUpdate`                                                     | Write  | `entity-analysis-model-list-author`                           | 3                                                                                               |
| `EntityAnalysisModelListValidate`                                                   | Read   | `entity-analysis-model-list-test-bench`                       | 3                                                                                               |
| `EntityAnalysisModelListValueCount`                                                 | Read   | `entity-analysis-model-list-value-inspector`                  | 3                                                                                               |
| `EntityAnalysisModelListValueCreate`                                                | Write  | `entity-analysis-model-list-value-author`                     | 3                                                                                               |
| `EntityAnalysisModelListValueDelete`                                                | Delete | `entity-analysis-model-list-value-author`                     | 3                                                                                               |
| `EntityAnalysisModelListValueFilter`                                                | Read   | `entity-analysis-model-list-value-inspector`                  | 3                                                                                               |
| `EntityAnalysisModelListValueFilterFields`                                          | Read   | `entity-analysis-model-list-value-inspector`                  | 3                                                                                               |
| `EntityAnalysisModelListValueGet`                                                   | Read   | `entity-analysis-model-list-value-inspector`                  | 3                                                                                               |
| `EntityAnalysisModelListValueGetByEntityAnalysisModelListId`                        | Read   | `entity-analysis-model-list-value-inspector`                  | 3                                                                                               |
| `EntityAnalysisModelListValueGetByGuid`                                             | Read   | `entity-analysis-model-list-value-inspector`                  | 3                                                                                               |
| `EntityAnalysisModelListValueList`                                                  | Read   | `entity-analysis-model-list-value-inspector`                  | 3                                                                                               |
| `EntityAnalysisModelListValueUpdate`                                                | Write  | `entity-analysis-model-list-value-author`                     | 3                                                                                               |
| `EntityAnalysisModelListValueUploadCsv`                                             | Write  | `entity-analysis-model-list-value-author`                     | 3                                                                                               |
| `EntityAnalysisModelListValueValidate`                                              | Read   | `entity-analysis-model-list-value-test-bench`                 | 3                                                                                               |
| `EntityAnalysisModelOverrideCount`                                                  | Read   | `entity-analysis-model-override-inspector`                    | 2                                                                                               |
| `EntityAnalysisModelOverrideCreate`                                                 | Write  | `entity-analysis-model-override-author`                       | 2                                                                                               |
| `EntityAnalysisModelOverrideDelete`                                                 | Delete | `entity-analysis-model-override-author`                       | 2                                                                                               |
| `EntityAnalysisModelOverrideFilter`                                                 | Read   | `entity-analysis-model-override-inspector`                    | 2                                                                                               |
| `EntityAnalysisModelOverrideFilterFields`                                           | Read   | `entity-analysis-model-override-inspector`                    | 2                                                                                               |
| `EntityAnalysisModelOverrideGet`                                                    | Read   | `entity-analysis-model-override-inspector`                    | 2                                                                                               |
| `EntityAnalysisModelOverrideGetByEntityAnalysisModelId`                             | Read   | `entity-analysis-model-override-inspector`                    | 2                                                                                               |
| `EntityAnalysisModelOverrideList`                                                   | Read   | `entity-analysis-model-override-inspector`                    | 2                                                                                               |
| `EntityAnalysisModelOverrideQueryGet`                                               | Read   | `entity-analysis-model-override-query`                        | 2                                                                                               |
| `EntityAnalysisModelOverrideQueryGetKeys`                                           | Read   | `entity-analysis-model-override-query`                        | 2                                                                                               |
| `EntityAnalysisModelOverrideQueryGetValues`                                         | Read   | `entity-analysis-model-override-query`                        | 2                                                                                               |
| `EntityAnalysisModelOverrideUpdate`                                                 | Write  | `entity-analysis-model-override-author`                       | 2                                                                                               |
| `EntityAnalysisModelOverrideUpdateDeleteExpiryDate`                                 | Write  | `entity-analysis-model-override-author`                       | 2                                                                                               |
| `EntityAnalysisModelOverrideValidate`                                               | Read   | `entity-analysis-model-override-test-bench`                   | 2                                                                                               |
| `EntityAnalysisModelProcessingCounterList`                                          | Read   | `engine-queue-and-throughput-counters`                        | landlord                                                                                        |
| `EntityAnalysisModelReprocessingRuleBuildRuleFromBuilderJson`                       | Read   | `entity-analysis-model-reprocessing-rule-test-bench`          | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleCount`                                          | Read   | `entity-analysis-model-reprocessing-rule-inspector`           | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleCreate`                                         | Write  | `entity-analysis-model-reprocessing-rule-author`              | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleDelete`                                         | Delete | `entity-analysis-model-reprocessing-rule-author`              | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleExecute`                                        | Read   | `entity-analysis-model-reprocessing-rule-test-bench`          | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleFilter`                                         | Read   | `entity-analysis-model-reprocessing-rule-inspector`           | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleFilterFields`                                   | Read   | `entity-analysis-model-reprocessing-rule-inspector`           | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleGet`                                            | Read   | `entity-analysis-model-reprocessing-rule-inspector`           | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleGetByEntityAnalysisModelId`                     | Read   | `entity-analysis-model-reprocessing-rule-inspector`           | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceCount`                                  | Read   | `entity-analysis-model-reprocessing-rule-instance-inspector`  | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceCreate`                                 | Write  | `entity-analysis-model-reprocessing-rule-instance-author`     | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceDelete`                                 | Delete | `entity-analysis-model-reprocessing-rule-instance-author`     | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceFilter`                                 | Read   | `entity-analysis-model-reprocessing-rule-instance-inspector`  | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceFilterFields`                           | Read   | `entity-analysis-model-reprocessing-rule-instance-inspector`  | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceGet`                                    | Read   | `entity-analysis-model-reprocessing-rule-instance-inspector`  | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceGetByEntityAnalysisModelReprocessingId` | Read   | `entity-analysis-model-reprocessing-rule-instance-inspector`  | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceInsertByExistingUpdateUncompleted`      | Write  | `entity-analysis-model-reprocessing-rule-instance-author`     | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceList`                                   | Read   | `entity-analysis-model-reprocessing-rule-instance-inspector`  | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceUpdate`                                 | Write  | `entity-analysis-model-reprocessing-rule-instance-author`     | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleInstanceValidate`                               | Read   | `entity-analysis-model-reprocessing-rule-instance-test-bench` | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleList`                                           | Read   | `entity-analysis-model-reprocessing-rule-inspector`           | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleParseRule`                                      | Read   | `entity-analysis-model-reprocessing-rule-test-bench`          | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleUpdate`                                         | Write  | `entity-analysis-model-reprocessing-rule-author`              | 26                                                                                              |
| `EntityAnalysisModelReprocessingRuleValidate`                                       | Read   | `entity-analysis-model-reprocessing-rule-test-bench`          | 26                                                                                              |
| `EntityAnalysisModelRequestXPathCount`                                              | Read   | `entity-analysis-model-request-x-path-inspector-1`            | 7                                                                                               |
| `EntityAnalysisModelRequestXPathCreate`                                             | Write  | `entity-analysis-model-request-x-path-author`                 | 7                                                                                               |
| `EntityAnalysisModelRequestXPathDelete`                                             | Delete | `entity-analysis-model-request-x-path-author`                 | 7                                                                                               |
| `EntityAnalysisModelRequestXPathFilter`                                             | Read   | `entity-analysis-model-request-x-path-inspector-1`            | 7                                                                                               |
| `EntityAnalysisModelRequestXPathFilterFields`                                       | Read   | `entity-analysis-model-request-x-path-inspector-1`            | 7                                                                                               |
| `EntityAnalysisModelRequestXPathGet`                                                | Read   | `entity-analysis-model-request-x-path-inspector-1`            | 7                                                                                               |
| `EntityAnalysisModelRequestXPathGetByCasesWorkflowId`                               | Read   | `entity-analysis-model-request-x-path-inspector-1`            | 7                                                                                               |
| `EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId`                         | Read   | `entity-analysis-model-request-x-path-inspector-1`            | 7, 13                                                                                           |
| `EntityAnalysisModelRequestXPathGetByEntityAnalysisModelIdByDataType`               | Read   | `entity-analysis-model-request-x-path-inspector-2`            | 7, 12                                                                                           |
| `EntityAnalysisModelRequestXPathGetByOverrideKey`                                   | Read   | `entity-analysis-model-request-x-path-inspector-2`            | 2                                                                                               |
| `EntityAnalysisModelRequestXPathList`                                               | Read   | `entity-analysis-model-request-x-path-inspector-1`            | 7                                                                                               |
| `EntityAnalysisModelRequestXPathUpdate`                                             | Write  | `entity-analysis-model-request-x-path-author`                 | 7                                                                                               |
| `EntityAnalysisModelRequestXPathValidate`                                           | Read   | `entity-analysis-model-request-x-path-test-bench`             | 7                                                                                               |
| `EntityAnalysisModelResponseTimePipelineCounterList`                                | Read   | `engine-queue-and-throughput-counters`                        | landlord                                                                                        |
| `EntityAnalysisModelRoleCreate`                                                     | Write  | `authoring-aids-and-model-role-grants`                        | 6                                                                                               |
| `EntityAnalysisModelRoleDelete`                                                     | Delete | `authoring-aids-and-model-role-grants`                        | 6                                                                                               |
| `EntityAnalysisModelRoleListByEntityAnalysisModelGuid`                              | Read   | `authoring-aids-and-model-role-grants`                        | 6                                                                                               |
| `EntityAnalysisModelSampleExecute`                                                  | Read   | `model-integrity-and-sampling`                                | 40                                                                                              |
| `EntityAnalysisModelSanctionCount`                                                  | Read   | `entity-analysis-model-sanction-inspector`                    | 11                                                                                              |
| `EntityAnalysisModelSanctionCreate`                                                 | Write  | `entity-analysis-model-sanction-author`                       | 11                                                                                              |
| `EntityAnalysisModelSanctionDelete`                                                 | Delete | `entity-analysis-model-sanction-author`                       | 11                                                                                              |
| `EntityAnalysisModelSanctionFilter`                                                 | Read   | `entity-analysis-model-sanction-inspector`                    | 11                                                                                              |
| `EntityAnalysisModelSanctionFilterFields`                                           | Read   | `entity-analysis-model-sanction-inspector`                    | 11                                                                                              |
| `EntityAnalysisModelSanctionGet`                                                    | Read   | `entity-analysis-model-sanction-inspector`                    | 11                                                                                              |
| `EntityAnalysisModelSanctionGetByEntityAnalysisModelId`                             | Read   | `entity-analysis-model-sanction-inspector`                    | 11                                                                                              |
| `EntityAnalysisModelSanctionGetByGuid`                                              | Read   | `entity-analysis-model-sanction-inspector`                    | 11                                                                                              |
| `EntityAnalysisModelSanctionList`                                                   | Read   | `entity-analysis-model-sanction-inspector`                    | 11                                                                                              |
| `EntityAnalysisModelSanctionUpdate`                                                 | Write  | `entity-analysis-model-sanction-author`                       | 11                                                                                              |
| `EntityAnalysisModelSanctionValidate`                                               | Read   | `entity-analysis-model-sanction-test-bench`                   | 11                                                                                              |
| `EntityAnalysisModelStagePerformanceCounterList`                                    | Read   | `engine-queue-and-throughput-counters`                        | landlord                                                                                        |
| `EntityAnalysisModelSynchronisationNodeStatusEntriesGet`                            | Read   | `engine-queue-and-throughput-counters`                        | 5                                                                                               |
| `EntityAnalysisModelSynchronisationScheduleCount`                                   | Read   | `entity-analysis-model-synchronisation-schedule`              | 5                                                                                               |
| `EntityAnalysisModelSynchronisationScheduleCreate`                                  | Write  | `entity-analysis-model-synchronisation-schedule`              | 5                                                                                               |
| `EntityAnalysisModelSynchronisationScheduleFilter`                                  | Read   | `entity-analysis-model-synchronisation-schedule`              | 5                                                                                               |
| `EntityAnalysisModelSynchronisationScheduleFilterFields`                            | Read   | `entity-analysis-model-synchronisation-schedule`              | 5                                                                                               |
| `EntityAnalysisModelSynchronisationScheduleGetCurrent`                              | Read   | `entity-analysis-model-synchronisation-schedule`              | 5                                                                                               |
| `EntityAnalysisModelSynchronisationScheduleList`                                    | Read   | `entity-analysis-model-synchronisation-schedule`              | 5                                                                                               |
| `EntityAnalysisModelSynchronisationScheduleValidate`                                | Read   | `entity-analysis-model-synchronisation-schedule`              | 5                                                                                               |
| `EntityAnalysisModelTagCount`                                                       | Read   | `entity-analysis-model-tag-inspector`                         | 37                                                                                              |
| `EntityAnalysisModelTagCreate`                                                      | Write  | `entity-analysis-model-tag-author`                            | 37                                                                                              |
| `EntityAnalysisModelTagDelete`                                                      | Delete | `entity-analysis-model-tag-author`                            | 37                                                                                              |
| `EntityAnalysisModelTagFilter`                                                      | Read   | `entity-analysis-model-tag-inspector`                         | 37                                                                                              |
| `EntityAnalysisModelTagFilterFields`                                                | Read   | `entity-analysis-model-tag-inspector`                         | 37                                                                                              |
| `EntityAnalysisModelTagGet`                                                         | Read   | `entity-analysis-model-tag-inspector`                         | 37                                                                                              |
| `EntityAnalysisModelTagGetByEntityAnalysisModelId`                                  | Read   | `entity-analysis-model-tag-inspector`                         | 37                                                                                              |
| `EntityAnalysisModelTagGetByGuid`                                                   | Read   | `entity-analysis-model-tag-inspector`                         | 37                                                                                              |
| `EntityAnalysisModelTagList`                                                        | Read   | `entity-analysis-model-tag-inspector`                         | 37                                                                                              |
| `EntityAnalysisModelTagUpdate`                                                      | Write  | `entity-analysis-model-tag-author`                            | 37                                                                                              |
| `EntityAnalysisModelTagValidate`                                                    | Read   | `entity-analysis-model-tag-test-bench`                        | 37                                                                                              |
| `EntityAnalysisModelTaskPerformanceCounterList`                                     | Read   | `invoke-path-performance-and-warnings`                        | landlord                                                                                        |
| `EntityAnalysisModelTimeWindowAbstractionRule`                                      | Read   | `entity-analysis-model-time-window`                           | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelTimeWindowCalculate`                                            | Read   | `entity-analysis-model-time-window`                           | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelTimeWindowIntervals`                                            | Read   | `entity-analysis-model-time-window`                           | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelTimeWindowTtlCounter`                                           | Read   | `entity-analysis-model-time-window`                           | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `EntityAnalysisModelTtlCounterCount`                                                | Read   | `entity-analysis-model-ttl-counter-inspector`                 | 12                                                                                              |
| `EntityAnalysisModelTtlCounterCreate`                                               | Write  | `entity-analysis-model-ttl-counter-author`                    | 12                                                                                              |
| `EntityAnalysisModelTtlCounterDelete`                                               | Delete | `entity-analysis-model-ttl-counter-author`                    | 12                                                                                              |
| `EntityAnalysisModelTtlCounterFilter`                                               | Read   | `entity-analysis-model-ttl-counter-inspector`                 | 12                                                                                              |
| `EntityAnalysisModelTtlCounterFilterFields`                                         | Read   | `entity-analysis-model-ttl-counter-inspector`                 | 12                                                                                              |
| `EntityAnalysisModelTtlCounterGet`                                                  | Read   | `entity-analysis-model-ttl-counter-inspector`                 | 12                                                                                              |
| `EntityAnalysisModelTtlCounterGetByEntityAnalysisModelGuid`                         | Read   | `entity-analysis-model-ttl-counter-inspector`                 | 12, 17                                                                                          |
| `EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId`                           | Read   | `entity-analysis-model-ttl-counter-inspector`                 | 12, 17                                                                                          |
| `EntityAnalysisModelTtlCounterGetByGuid`                                            | Read   | `entity-analysis-model-ttl-counter-inspector`                 | 12                                                                                              |
| `EntityAnalysisModelTtlCounterList`                                                 | Read   | `entity-analysis-model-ttl-counter-inspector`                 | 12                                                                                              |
| `EntityAnalysisModelTtlCounterUpdate`                                               | Write  | `entity-analysis-model-ttl-counter-author`                    | 12                                                                                              |
| `EntityAnalysisModelTtlCounterValidate`                                             | Read   | `entity-analysis-model-ttl-counter-test-bench`                | 12                                                                                              |
| `EntityAnalysisModelUpdate`                                                         | Write  | `entity-analysis-model-author`                                | 6                                                                                               |
| `EntityAnalysisModelValidate`                                                       | Read   | `entity-analysis-model-test-bench`                            | 6                                                                                               |
| `EntityAnalysisPotentialMultiPartStringNamesGetByGuid`                              | Read   | `entity-analysis-observability`                               | 11, 17, 4, 1                                                                                    |
| `EntityAnalysisPotentialMultiPartStringNamesGetById`                                | Read   | `entity-analysis-observability`                               | 11, 17, 4, 1                                                                                    |
| `EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeGet`        | Read   | `entity-analysis-observability`                               | 11, 17, 4, 12                                                                                   |
| `EtcdClearAlarm`                                                                    | Write  | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `EtcdClusterEventList`                                                              | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `EtcdClusterStatusNow`                                                              | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `EtcdDefragment`                                                                    | Write  | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `EtcdHashKv`                                                                        | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `EtcdMaintenanceStatus`                                                             | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `EtcdMemberList`                                                                    | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `EtcdMemberStatusList`                                                              | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `ExhaustiveSearchInstanceCount`                                                     | Read   | `exhaustive-search-instance-inspector`                        | 16                                                                                              |
| `ExhaustiveSearchInstanceCreate`                                                    | Write  | `exhaustive-search-instance-author`                           | 16                                                                                              |
| `ExhaustiveSearchInstanceDelete`                                                    | Delete | `exhaustive-search-instance-author`                           | 16                                                                                              |
| `ExhaustiveSearchInstanceFilter`                                                    | Read   | `exhaustive-search-instance-inspector`                        | 16                                                                                              |
| `ExhaustiveSearchInstanceFilterFields`                                              | Read   | `exhaustive-search-instance-inspector`                        | 16                                                                                              |
| `ExhaustiveSearchInstanceGet`                                                       | Read   | `exhaustive-search-instance-inspector`                        | 16                                                                                              |
| `ExhaustiveSearchInstanceGetByEntityAnalysisModelId`                                | Read   | `exhaustive-search-instance-inspector`                        | 16                                                                                              |
| `ExhaustiveSearchInstanceGetByGuid`                                                 | Read   | `exhaustive-search-instance-inspector`                        | 16                                                                                              |
| `ExhaustiveSearchInstanceList`                                                      | Read   | `exhaustive-search-instance-inspector`                        | 16                                                                                              |
| `ExhaustiveSearchInstancePromotedTrialInstanceConfusionGet`                         | Read   | `exhaustive-trials-and-confusion`                             | 16                                                                                              |
| `ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramGet`                    | Read   | `exhaustive-trials-and-confusion`                             | 16                                                                                              |
| `ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveGet`                     | Read   | `exhaustive-promotion-curves-and-history`                     | 16                                                                                              |
| `ExhaustiveSearchInstancePromotedTrialInstancePredictedActualGet`                   | Read   | `exhaustive-promotion-curves-and-history`                     | 16                                                                                              |
| `ExhaustiveSearchInstancePromotedTrialInstanceQueryGet`                             | Read   | `exhaustive-promotion-curves-and-history`                     | 16                                                                                              |
| `ExhaustiveSearchInstancePromotedTrialInstanceRocGet`                               | Read   | `exhaustive-promotion-curves-and-history`                     | 16                                                                                              |
| `ExhaustiveSearchInstancePromotedTrialInstanceUpdate`                               | Write  | `exhaustive-trials-and-confusion`                             | 14                                                                                              |
| `ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionGet`              | Read   | `exhaustive-promotion-curves-and-history`                     | 16                                                                                              |
| `ExhaustiveSearchInstancePromotionHistoryGet`                                       | Read   | `exhaustive-promotion-curves-and-history`                     | 16                                                                                              |
| `ExhaustiveSearchInstanceSetActive`                                                 | Write  | `exhaustive-search-instance-author`                           | 16                                                                                              |
| `ExhaustiveSearchInstanceStop`                                                      | Write  | `exhaustive-search-instance-author`                           | 16                                                                                              |
| `ExhaustiveSearchInstanceTrialInstanceDetailGet`                                    | Read   | `exhaustive-trials-and-confusion`                             | 16                                                                                              |
| `ExhaustiveSearchInstanceTrialInstanceListQueryGet`                                 | Read   | `exhaustive-trials-and-confusion`                             | 16                                                                                              |
| `ExhaustiveSearchInstanceTrialInstanceVariableGet`                                  | Read   | `exhaustive-promotion-curves-and-history`                     | 16                                                                                              |
| `ExhaustiveSearchInstanceTrialInstanceVariableVarianceGet`                          | Read   | `exhaustive-variable-statistics`                              | 16                                                                                              |
| `ExhaustiveSearchInstanceUpdate`                                                    | Write  | `exhaustive-search-instance-author`                           | 16                                                                                              |
| `ExhaustiveSearchInstanceValidate`                                                  | Read   | `exhaustive-search-instance-test-bench`                       | 16                                                                                              |
| `ExhaustiveSearchInstanceVariableGet`                                               | Read   | `exhaustive-trials-and-confusion`                             | 16                                                                                              |
| `ExhaustiveSearchInstanceVariableReallocate`                                        | Write  | `exhaustive-trials-and-confusion`                             | 16                                                                                              |
| `ExhaustiveSearchInstanceVariableStatisticsGet`                                     | Read   | `exhaustive-variable-statistics`                              | 16                                                                                              |
| `GetAdvancedAnalyticsTrainingRunResult`                                             | Read   | `advanced-analytics-training-classifier-trees`                | 48                                                                                              |
| `GetAdvancedAnalyticsTrainingRunStatus`                                             | Read   | `advanced-analytics-training-classifier-trees`                | 48                                                                                              |
| `HAProxyReachabilityProbeList`                                                      | Read   | `containers-load-balancer-and-overlay-network`                | landlord                                                                                        |
| `HAProxyServerStatusList`                                                           | Read   | `containers-load-balancer-and-overlay-network`                | landlord                                                                                        |
| `HttpProcessingCounterList`                                                         | Read   | `invoke-path-performance-and-warnings`                        | landlord                                                                                        |
| `IconsGet`                                                                          | Read   | `authoring-aids-and-model-role-grants`                        | 24                                                                                              |
| `ModelInvokeWarningList`                                                            | Read   | `invoke-path-performance-and-warnings`                        | landlord                                                                                        |
| `OpenTelemetryExcludeCreate`                                                        | Write  | `open-telemetry-exclude`                                      | landlord                                                                                        |
| `OpenTelemetryExcludeDelete`                                                        | Delete | `open-telemetry-exclude`                                      | landlord                                                                                        |
| `OpenTelemetryExcludeGet`                                                           | Read   | `open-telemetry-exclude`                                      | landlord                                                                                        |
| `OpenTelemetryExcludeList`                                                          | Read   | `open-telemetry-exclude`                                      | landlord                                                                                        |
| `OpenTelemetryExcludeUpdate`                                                        | Write  | `open-telemetry-exclude`                                      | landlord                                                                                        |
| `OpenTelemetryExcludeValidate`                                                      | Read   | `open-telemetry-exclude`                                      | landlord                                                                                        |
| `OpenTelemetryLogCounterCreate`                                                     | Write  | `telemetry-log-counters-and-runtime-metrics`                  | landlord                                                                                        |
| `OpenTelemetryLogCounterDelete`                                                     | Delete | `telemetry-log-counters-and-runtime-metrics`                  | landlord                                                                                        |
| `OpenTelemetryLogCounterGet`                                                        | Read   | `telemetry-log-counters-and-runtime-metrics`                  | landlord                                                                                        |
| `OpenTelemetryLogCounterList`                                                       | Read   | `telemetry-log-counters-and-runtime-metrics`                  | landlord                                                                                        |
| `OpenTelemetryLogCounterUpdate`                                                     | Write  | `telemetry-log-counters-and-runtime-metrics`                  | landlord                                                                                        |
| `OpenTelemetryLogCounterValidate`                                                   | Read   | `telemetry-log-counters-and-runtime-metrics`                  | landlord                                                                                        |
| `OpenTelemetryMetricList`                                                           | Read   | `telemetry-metrics-and-otlp-dispatch`                         | landlord                                                                                        |
| `OtlpDispatchCounterList`                                                           | Read   | `telemetry-metrics-and-otlp-dispatch`                         | landlord                                                                                        |
| `OverlayNetworkTaskDriftList`                                                       | Read   | `containers-load-balancer-and-overlay-network`                | landlord                                                                                        |
| `ParserParse`                                                                       | Read   | `authoring-aids-and-model-role-grants`                        | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `PatroniClusterEventList`                                                           | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `PatroniClusterStatusNow`                                                           | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `PatroniConfigGet`                                                                  | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `PatroniMemberStatusList`                                                           | Read   | `etcd-and-patroni-cluster-state`                              | landlord                                                                                        |
| `PermissionSpecificationList`                                                       | Read   | `permission-specification`                                    | 36                                                                                              |
| `PostgresActivityList`                                                              | Read   | `postgres-observability`                                      | landlord                                                                                        |
| `PostgresIndexStatisticsList`                                                       | Read   | `postgres-observability`                                      | landlord                                                                                        |
| `PostgresLockList`                                                                  | Read   | `postgres-observability`                                      | landlord                                                                                        |
| `PostgresLogEntryList`                                                              | Read   | `postgres-observability`                                      | landlord                                                                                        |
| `PostgresMetricList`                                                                | Read   | `postgres-observability`                                      | landlord                                                                                        |
| `PostgresReplicationStatusList`                                                     | Read   | `postgres-observability`                                      | landlord                                                                                        |
| `PostgresStatementStatisticsList`                                                   | Read   | `postgres-observability`                                      | landlord                                                                                        |
| `PostgresTableStatisticsList`                                                       | Read   | `postgres-observability`                                      | landlord                                                                                        |
| `QueryBuilderOperatorsList`                                                         | Read   | `completions`                                                 | **none**                                                                                        |
| `RedisCallCounterList`                                                              | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisConnectionEventList`                                                          | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisConnectionMultiplexerMetricList`                                              | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisKeyActivityList`                                                              | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisMetricList`                                                                   | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisSentinelCheckQuorum`                                                          | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisSentinelClusterStatusNow`                                                     | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisSentinelEventList`                                                            | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisSentinelFailover`                                                             | Write  | `redis-observability`                                         | landlord                                                                                        |
| `RedisSentinelGetMasterAddress`                                                     | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisSentinelReset`                                                                | Write  | `redis-observability`                                         | landlord                                                                                        |
| `RedisSentinelStatusList`                                                           | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RedisSlowOperationList`                                                            | Read   | `redis-observability`                                         | landlord                                                                                        |
| `RoleRegistryCount`                                                                 | Read   | `role-registry-inspector`                                     | 34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33                                       |
| `RoleRegistryCreate`                                                                | Write  | `role-registry-author`                                        | 34                                                                                              |
| `RoleRegistryDelete`                                                                | Delete | `role-registry-author`                                        | 34                                                                                              |
| `RoleRegistryFilter`                                                                | Read   | `role-registry-inspector`                                     | 34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33                                       |
| `RoleRegistryFilterFields`                                                          | Read   | `role-registry-inspector`                                     | 34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33                                       |
| `RoleRegistryGet`                                                                   | Read   | `role-registry-inspector`                                     | 34                                                                                              |
| `RoleRegistryGetByGuid`                                                             | Read   | `role-registry-inspector`                                     | 34                                                                                              |
| `RoleRegistryList`                                                                  | Read   | `role-registry-inspector`                                     | 34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33                                       |
| `RoleRegistryPermissionCount`                                                       | Read   | `role-registry-permission-inspector`                          | 36                                                                                              |
| `RoleRegistryPermissionCreate`                                                      | Write  | `role-registry-permission-author`                             | 36                                                                                              |
| `RoleRegistryPermissionDelete`                                                      | Delete | `role-registry-permission-author`                             | 36                                                                                              |
| `RoleRegistryPermissionFilter`                                                      | Read   | `role-registry-permission-inspector`                          | 36                                                                                              |
| `RoleRegistryPermissionFilterFields`                                                | Read   | `role-registry-permission-inspector`                          | 36                                                                                              |
| `RoleRegistryPermissionGet`                                                         | Read   | `role-registry-permission-inspector`                          | 36                                                                                              |
| `RoleRegistryPermissionGetByGuid`                                                   | Read   | `role-registry-permission-inspector`                          | 36                                                                                              |
| `RoleRegistryPermissionList`                                                        | Read   | `role-registry-permission-inspector`                          | 36                                                                                              |
| `RoleRegistryPermissionUpdate`                                                      | Write  | `role-registry-permission-author`                             | 36                                                                                              |
| `RoleRegistryPermissionValidate`                                                    | Read   | `role-registry-permission-test-bench`                         | 36                                                                                              |
| `RoleRegistryUpdate`                                                                | Write  | `role-registry-author`                                        | 34                                                                                              |
| `RoleRegistryValidate`                                                              | Read   | `role-registry-test-bench`                                    | 34                                                                                              |
| `RuleVocabularyDescribe`                                                            | Read   | `authoring-aids-and-model-role-grants`                        | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `RuleVocabularyList`                                                                | Read   | `authoring-aids-and-model-role-grants`                        | 8, 10, 13, 14, 16, 17, 25, 26                                                                   |
| `SanctionEntrySourceCount`                                                          | Read   | `sanctions-entry-source`                                      | landlord                                                                                        |
| `SanctionEntrySourceFilter`                                                         | Read   | `sanctions-entry-source`                                      | landlord                                                                                        |
| `SanctionEntrySourceFilterFields`                                                   | Read   | `sanctions-entry-source`                                      | landlord                                                                                        |
| `SanctionEntrySourceList`                                                           | Read   | `sanctions-entry-source`                                      | landlord                                                                                        |
| `SessionCaseJournalCreate`                                                          | Write  | `session-case-observability`                                  | 1                                                                                               |
| `SessionCaseJournalGetByCaseWorkflowGuid`                                           | Read   | `session-case-observability`                                  | 1                                                                                               |
| `SessionCaseSearchCompiledSqlGetLast`                                               | Read   | `session-case-observability`                                  | 1                                                                                               |
| `SubmitAdaBoostClassifierTraining`                                                  | Write  | `advanced-analytics-training-classifier-trees`                | 66                                                                                              |
| `SubmitBaggingClassifierTraining`                                                   | Write  | `advanced-analytics-training-classifier-trees`                | 66                                                                                              |
| `SubmitBalancedKMeansClustererTraining`                                             | Write  | `advanced-analytics-training-clusterer-centroid`              | 66                                                                                              |
| `SubmitBinarySplitClustererTraining`                                                | Write  | `advanced-analytics-training-clusterer-density`               | 66                                                                                              |
| `SubmitDecisionTreeClassifierTraining`                                              | Write  | `advanced-analytics-training-classifier-trees`                | 66                                                                                              |
| `SubmitDecisionTreeRegressorTraining`                                               | Write  | `advanced-analytics-training-regressor`                       | 66                                                                                              |
| `SubmitGaussianMixtureClustererTraining`                                            | Write  | `advanced-analytics-training-clusterer-density`               | 66                                                                                              |
| `SubmitGaussianMixtureDensityTraining`                                              | Write  | `advanced-analytics-training-latent-structure`                | 66                                                                                              |
| `SubmitGradientBoostingClassifierTraining`                                          | Write  | `advanced-analytics-training-classifier-trees`                | 66                                                                                              |
| `SubmitGradientBoostingRegressorTraining`                                           | Write  | `advanced-analytics-training-regressor`                       | 66                                                                                              |
| `SubmitIsolationForestAnomalyTraining`                                              | Write  | `advanced-analytics-training-anomaly`                         | 66                                                                                              |
| `SubmitKMeansClustererTraining`                                                     | Write  | `advanced-analytics-training-clusterer-centroid`              | 66                                                                                              |
| `SubmitKMedoidsClustererTraining`                                                   | Write  | `advanced-analytics-training-clusterer-centroid`              | 66                                                                                              |
| `SubmitKModesClustererTraining`                                                     | Write  | `advanced-analytics-training-clusterer-centroid`              | 66                                                                                              |
| `SubmitKNearestNeighboursClassifierTraining`                                        | Write  | `advanced-analytics-training-classifier-margin`               | 66                                                                                              |
| `SubmitKNearestNeighboursRegressorTraining`                                         | Write  | `advanced-analytics-training-regressor`                       | 66                                                                                              |
| `SubmitKernelRidgeRegressorTraining`                                                | Write  | `advanced-analytics-training-regressor`                       | 66                                                                                              |
| `SubmitKernelSupportVectorClassifierTraining`                                       | Write  | `advanced-analytics-training-classifier-margin`               | 66                                                                                              |
| `SubmitLinearRegressionTraining`                                                    | Write  | `advanced-analytics-training-regressor`                       | 66                                                                                              |
| `SubmitLinearSupportVectorClassifierTraining`                                       | Write  | `advanced-analytics-training-classifier-margin`               | 66                                                                                              |
| `SubmitLogisticRegressionTraining`                                                  | Write  | `advanced-analytics-training-classifier-probabilistic`        | 66                                                                                              |
| `SubmitMahalanobisAnomalyTraining`                                                  | Write  | `advanced-analytics-training-anomaly`                         | 66                                                                                              |
| `SubmitMeanShiftClustererTraining`                                                  | Write  | `advanced-analytics-training-clusterer-density`               | 66                                                                                              |
| `SubmitMiniBatchKMeansClustererTraining`                                            | Write  | `advanced-analytics-training-clusterer-centroid`              | 66                                                                                              |
| `SubmitNaiveBayesClassifierTraining`                                                | Write  | `advanced-analytics-training-classifier-probabilistic`        | 66                                                                                              |
| `SubmitNeuralNetworkClassifierTraining`                                             | Write  | `advanced-analytics-training-classifier-margin`               | 66                                                                                              |
| `SubmitNeuralNetworkRegressorTraining`                                              | Write  | `advanced-analytics-training-regressor`                       | 66                                                                                              |
| `SubmitOneClassSupportVectorAnomalyTraining`                                        | Write  | `advanced-analytics-training-anomaly`                         | 66                                                                                              |
| `SubmitPrincipalComponentTraining`                                                  | Write  | `advanced-analytics-training-latent-structure`                | 66                                                                                              |
| `SubmitRandomForestClassifierTraining`                                              | Write  | `advanced-analytics-training-classifier-trees`                | 66                                                                                              |
| `SubmitRandomForestRegressorTraining`                                               | Write  | `advanced-analytics-training-regressor`                       | 66                                                                                              |
| `SubmitTreeAugmentedNaiveBayesClassifierTraining`                                   | Write  | `advanced-analytics-training-classifier-probabilistic`        | 66                                                                                              |
| `TenantRegistryCount`                                                               | Read   | `tenant-registry-inspector`                                   | landlord                                                                                        |
| `TenantRegistryCreate`                                                              | Write  | `tenant-registry-author`                                      | landlord                                                                                        |
| `TenantRegistryDelete`                                                              | Delete | `tenant-registry-author`                                      | landlord                                                                                        |
| `TenantRegistryFilter`                                                              | Read   | `tenant-registry-inspector`                                   | landlord                                                                                        |
| `TenantRegistryFilterFields`                                                        | Read   | `tenant-registry-inspector`                                   | landlord                                                                                        |
| `TenantRegistryGet`                                                                 | Read   | `tenant-registry-inspector`                                   | landlord                                                                                        |
| `TenantRegistryList`                                                                | Read   | `tenant-registry-inspector`                                   | landlord                                                                                        |
| `TenantRegistryUpdate`                                                              | Write  | `tenant-registry-author`                                      | landlord                                                                                        |
| `TenantRegistryValidate`                                                            | Read   | `tenant-registry-test-bench`                                  | landlord                                                                                        |
| `TreeChildrenAbstractionCalculationGet`                                             | Read   | `tree-children-inspector-2`                                   | 14                                                                                              |
| `TreeChildrenAbstractionRuleGet`                                                    | Read   | `tree-children-inspector-2`                                   | 13, 14                                                                                          |
| `TreeChildrenActivationRuleGet`                                                     | Read   | `tree-children-inspector-3`                                   | 17                                                                                              |
| `TreeChildrenAdaptationGet`                                                         | Read   | `tree-children-inspector-2`                                   | 15                                                                                              |
| `TreeChildrenCaseWorkflowActionGet`                                                 | Read   | `tree-children-inspector-4`                                   | 22                                                                                              |
| `TreeChildrenCaseWorkflowDisplayGet`                                                | Read   | `tree-children-inspector-4`                                   | 23                                                                                              |
| `TreeChildrenCaseWorkflowFilterGet`                                                 | Read   | `tree-children-inspector-4`                                   | 25                                                                                              |
| `TreeChildrenCaseWorkflowFormGet`                                                   | Read   | `tree-children-inspector-4`                                   | 21                                                                                              |
| `TreeChildrenCaseWorkflowGet`                                                       | Read   | `tree-children-inspector-2`                                   | 18, 19, 20, 21, 22, 23, 24, 25                                                                  |
| `TreeChildrenCaseWorkflowMacroGet`                                                  | Read   | `tree-children-inspector-4`                                   | 24                                                                                              |
| `TreeChildrenCaseWorkflowStatusGet`                                                 | Read   | `tree-children-inspector-4`                                   | 19                                                                                              |
| `TreeChildrenCaseWorkflowXPathGet`                                                  | Read   | `tree-children-inspector-3`                                   | 20                                                                                              |
| `TreeChildrenDictionaryGet`                                                         | Read   | `tree-children-inspector-3`                                   | 4                                                                                               |
| `TreeChildrenExhaustiveGet`                                                         | Read   | `tree-children-inspector-2`                                   | 16                                                                                              |
| `TreeChildrenGatewayRuleGet`                                                        | Read   | `tree-children-inspector-2`                                   | 10                                                                                              |
| `TreeChildrenInlineFunctionGet`                                                     | Read   | `tree-children-inspector-1`                                   | 8                                                                                               |
| `TreeChildrenInlineScriptGet`                                                       | Read   | `tree-children-inspector-3`                                   | 9                                                                                               |
| `TreeChildrenListGet`                                                               | Read   | `tree-children-inspector-3`                                   | 3                                                                                               |
| `TreeChildrenReprocessingGet`                                                       | Read   | `tree-children-inspector-2`                                   | 26                                                                                              |
| `TreeChildrenRequestXPathGet`                                                       | Read   | `tree-children-inspector-1`                                   | 7                                                                                               |
| `TreeChildrenRoleRegistryPermissionGet`                                             | Read   | `tree-children-inspector-1`                                   | 36                                                                                              |
| `TreeChildrenSanctionsGet`                                                          | Read   | `tree-children-inspector-3`                                   | 11                                                                                              |
| `TreeChildrenTTLCounterGet`                                                         | Read   | `tree-children-inspector-3`                                   | 12                                                                                              |
| `TreeChildrenTagGet`                                                                | Read   | `tree-children-inspector-1`                                   | 37                                                                                              |
| `TreeChildrenUserRegistryGet`                                                       | Read   | `tree-children-inspector-1`                                   | 35                                                                                              |
| `TreeChildrenVisualisationRegistryDatasourceGet`                                    | Read   | `tree-children-inspector-1`                                   | 33                                                                                              |
| `TreeChildrenVisualisationRegistryParameterGet`                                     | Read   | `tree-children-inspector-1`                                   | 32                                                                                              |
| `UserInTenantGetCurrentTenantRegistry`                                              | Read   | `user-observability`                                          | 1                                                                                               |
| `UserInTenantList`                                                                  | Read   | `user-observability`                                          | 1                                                                                               |
| `UserInTenantSwitchTenant`                                                          | Write  | `user-observability`                                          | landlord                                                                                        |
| `UserLoginList`                                                                     | Read   | `user-observability`                                          | 35                                                                                              |
| `UserLogoutList`                                                                    | Read   | `user-observability`                                          | 35                                                                                              |
| `UserRegistryApiKeyDelete`                                                          | Delete | `user-observability`                                          | 35                                                                                              |
| `UserRegistryApiKeyListByUserRegistryId`                                            | Read   | `user-observability`                                          | 35                                                                                              |
| `UserRegistryCount`                                                                 | Read   | `user-registry-inspector`                                     | 35                                                                                              |
| `UserRegistryCreate`                                                                | Write  | `user-registry-author`                                        | 35                                                                                              |
| `UserRegistryDelete`                                                                | Delete | `user-registry-author`                                        | 35                                                                                              |
| `UserRegistryFilter`                                                                | Read   | `user-registry-inspector`                                     | 35                                                                                              |
| `UserRegistryFilterFields`                                                          | Read   | `user-registry-inspector`                                     | 35                                                                                              |
| `UserRegistryGet`                                                                   | Read   | `user-registry-inspector`                                     | 35                                                                                              |
| `UserRegistryGetByGuid`                                                             | Read   | `user-registry-inspector`                                     | 35                                                                                              |
| `UserRegistryList`                                                                  | Read   | `user-registry-inspector`                                     | 35                                                                                              |
| `UserRegistryListByRoleRegistryGuid`                                                | Read   | `user-registry-inspector`                                     | 35                                                                                              |
| `UserRegistryUpdate`                                                                | Write  | `user-registry-author`                                        | 35                                                                                              |
| `UserRegistryValidate`                                                              | Read   | `user-registry-test-bench`                                    | 35                                                                                              |
| `VisualisationRegistryCount`                                                        | Read   | `visualisation-registry-inspector`                            | 18, 31, 32, 33                                                                                  |
| `VisualisationRegistryCreate`                                                       | Write  | `visualisation-registry-author`                               | 31                                                                                              |
| `VisualisationRegistryDatasourceCommandExecutionExecute`                            | Read   | `visualisation-role-grants-and-execution`                     | 28, 1                                                                                           |
| `VisualisationRegistryDatasourceCount`                                              | Read   | `visualisation-registry-datasource-inspector`                 | 33                                                                                              |
| `VisualisationRegistryDatasourceCreate`                                             | Write  | `visualisation-registry-datasource-author`                    | 33                                                                                              |
| `VisualisationRegistryDatasourceDelete`                                             | Delete | `visualisation-registry-datasource-author`                    | 33                                                                                              |
| `VisualisationRegistryDatasourceFilter`                                             | Read   | `visualisation-registry-datasource-inspector`                 | 33                                                                                              |
| `VisualisationRegistryDatasourceFilterFields`                                       | Read   | `visualisation-registry-datasource-inspector`                 | 33                                                                                              |
| `VisualisationRegistryDatasourceGet`                                                | Read   | `visualisation-registry-datasource-inspector`                 | 33                                                                                              |
| `VisualisationRegistryDatasourceGetByGuid`                                          | Read   | `visualisation-registry-datasource-inspector`                 | 33                                                                                              |
| `VisualisationRegistryDatasourceGetByVisualisationRegistryId`                       | Read   | `visualisation-registry-datasource-inspector`                 | 33, 28, 1                                                                                       |
| `VisualisationRegistryDatasourceGetByVisualisationRegistryIdActiveOnly`             | Read   | `visualisation-registry-datasource-inspector`                 | 33, 28, 1                                                                                       |
| `VisualisationRegistryDatasourceList`                                               | Read   | `visualisation-registry-datasource-inspector`                 | 33                                                                                              |
| `VisualisationRegistryDatasourceRoleCreate`                                         | Write  | `datasource-and-parameter-role-grants`                        | 33                                                                                              |
| `VisualisationRegistryDatasourceRoleDelete`                                         | Delete | `datasource-and-parameter-role-grants`                        | 33                                                                                              |
| `VisualisationRegistryDatasourceRoleListByVisualisationRegistryDatasourceGuid`      | Read   | `datasource-and-parameter-role-grants`                        | 33                                                                                              |
| `VisualisationRegistryDatasourceSeriesListByVisualisationRegistryDatasourceId`      | Read   | `visualisation-role-grants-and-execution`                     | 28, 1                                                                                           |
| `VisualisationRegistryDatasourceUpdate`                                             | Write  | `visualisation-registry-datasource-author`                    | 33                                                                                              |
| `VisualisationRegistryDatasourceValidate`                                           | Read   | `visualisation-registry-datasource-test-bench`                | 33                                                                                              |
| `VisualisationRegistryDelete`                                                       | Delete | `visualisation-registry-author`                               | 31                                                                                              |
| `VisualisationRegistryFilter`                                                       | Read   | `visualisation-registry-inspector`                            | 18, 31, 32, 33                                                                                  |
| `VisualisationRegistryFilterFields`                                                 | Read   | `visualisation-registry-inspector`                            | 18, 31, 32, 33                                                                                  |
| `VisualisationRegistryGet`                                                          | Read   | `visualisation-registry-inspector`                            | 31, 28, 1                                                                                       |
| `VisualisationRegistryGetByGuid`                                                    | Read   | `visualisation-registry-inspector`                            | 31, 28, 1                                                                                       |
| `VisualisationRegistryGetByGuidActiveOnly`                                          | Read   | `visualisation-registry-inspector`                            | 31, 28, 1                                                                                       |
| `VisualisationRegistryList`                                                         | Read   | `visualisation-registry-inspector`                            | 18, 31, 32, 33                                                                                  |
| `VisualisationRegistryListByShowInDirectory`                                        | Read   | `visualisation-registry-inspector`                            | 28                                                                                              |
| `VisualisationRegistryParameterCount`                                               | Read   | `visualisation-registry-parameter-inspector`                  | 32                                                                                              |
| `VisualisationRegistryParameterCreate`                                              | Write  | `visualisation-registry-parameter-author`                     | 32                                                                                              |
| `VisualisationRegistryParameterDelete`                                              | Delete | `visualisation-registry-parameter-author`                     | 32                                                                                              |
| `VisualisationRegistryParameterFilter`                                              | Read   | `visualisation-registry-parameter-inspector`                  | 32                                                                                              |
| `VisualisationRegistryParameterFilterFields`                                        | Read   | `visualisation-registry-parameter-inspector`                  | 32                                                                                              |
| `VisualisationRegistryParameterGet`                                                 | Read   | `visualisation-registry-parameter-inspector`                  | 32                                                                                              |
| `VisualisationRegistryParameterGetByGuid`                                           | Read   | `visualisation-registry-parameter-inspector`                  | 32                                                                                              |
| `VisualisationRegistryParameterGetByVisualisationRegistryId`                        | Read   | `visualisation-registry-parameter-inspector`                  | 32, 28, 1                                                                                       |
| `VisualisationRegistryParameterGetByVisualisationRegistryIdActiveOnly`              | Read   | `visualisation-registry-parameter-inspector`                  | 32, 28, 1                                                                                       |
| `VisualisationRegistryParameterList`                                                | Read   | `visualisation-registry-parameter-inspector`                  | 32                                                                                              |
| `VisualisationRegistryParameterRoleCreate`                                          | Write  | `datasource-and-parameter-role-grants`                        | 32                                                                                              |
| `VisualisationRegistryParameterRoleDelete`                                          | Delete | `datasource-and-parameter-role-grants`                        | 32                                                                                              |
| `VisualisationRegistryParameterRoleListByVisualisationRegistryParameterGuid`        | Read   | `datasource-and-parameter-role-grants`                        | 32                                                                                              |
| `VisualisationRegistryParameterUpdate`                                              | Write  | `visualisation-registry-parameter-author`                     | 32                                                                                              |
| `VisualisationRegistryParameterValidate`                                            | Read   | `visualisation-registry-parameter-test-bench`                 | 32                                                                                              |
| `VisualisationRegistryRoleCreate`                                                   | Write  | `visualisation-role-grants-and-execution`                     | 31                                                                                              |
| `VisualisationRegistryRoleDelete`                                                   | Delete | `visualisation-role-grants-and-execution`                     | 31                                                                                              |
| `VisualisationRegistryRoleListByVisualisationRegistryGuid`                          | Read   | `visualisation-role-grants-and-execution`                     | 31                                                                                              |
| `VisualisationRegistryUpdate`                                                       | Write  | `visualisation-registry-author`                               | 31                                                                                              |
| `VisualisationRegistryValidate`                                                     | Read   | `visualisation-registry-test-bench`                           | 31                                                                                              |
| `WafAttackList`                                                                     | Read   | `application-and-container-logs`                              | landlord                                                                                        |

## Operations

| `EntityAnalysisModelResubmit`                                                       | Write  | `entity-analysis-model-author`                                | 6                                                                                               |
| `EntityAnalysisModelRetract`                                                        | Write  | `entity-analysis-model-author`                                | 6                                                                                               |
| `EntityAnalysisModelAbstractionCalculationResubmit`                                 | Write  | `entity-analysis-model-abstraction-calculation-author`        | 14                                                                                              |
| `EntityAnalysisModelAbstractionCalculationRetract`                                  | Write  | `entity-analysis-model-abstraction-calculation-author`        | 14                                                                                              |
| `EntityAnalysisModelAbstractionRuleResubmit`                                        | Write  | `entity-analysis-model-abstraction-rule-approvals`            | 13                                                                                              |
| `EntityAnalysisModelAbstractionRuleRetract`                                         | Write  | `entity-analysis-model-abstraction-rule-approvals`            | 13                                                                                              |
| `EntityAnalysisModelActivationRuleResubmit`                                         | Write  | `entity-analysis-model-activation-rule-approvals`             | 17                                                                                              |
| `EntityAnalysisModelActivationRuleRetract`                                          | Write  | `entity-analysis-model-activation-rule-approvals`             | 17                                                                                              |
| `EntityAnalysisModelDictionaryResubmit`                                             | Write  | `entity-analysis-model-dictionary-author`                     | 4                                                                                               |
| `EntityAnalysisModelDictionaryRetract`                                              | Write  | `entity-analysis-model-dictionary-author`                     | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpResubmit`                                          | Write  | `entity-analysis-model-dictionary-kvp-author`                 | 4                                                                                               |
| `EntityAnalysisModelDictionaryKvpRetract`                                           | Write  | `entity-analysis-model-dictionary-kvp-author`                 | 4                                                                                               |
| `EntityAnalysisModelGatewayRuleResubmit`                                            | Write  | `entity-analysis-model-gateway-rule-approvals`                | 10                                                                                              |
| `EntityAnalysisModelGatewayRuleRetract`                                             | Write  | `entity-analysis-model-gateway-rule-approvals`                | 10                                                                                              |
| `EntityAnalysisModelHttpAdaptationResubmit`                                         | Write  | `entity-analysis-model-http-adaptation-author`                | 15                                                                                              |
| `EntityAnalysisModelHttpAdaptationRetract`                                          | Write  | `entity-analysis-model-http-adaptation-author`                | 15                                                                                              |
| `EntityAnalysisModelInlineFunctionResubmit`                                         | Write  | `entity-analysis-model-inline-function-author`                | 8                                                                                               |
| `EntityAnalysisModelInlineFunctionRetract`                                          | Write  | `entity-analysis-model-inline-function-author`                | 8                                                                                               |
| `EntityAnalysisModelInlineScriptResubmit`                                           | Write  | `entity-analysis-model-inline-script-author`                  | 9                                                                                               |
| `EntityAnalysisModelInlineScriptRetract`                                            | Write  | `entity-analysis-model-inline-script-author`                  | 9                                                                                               |
| `EntityAnalysisModelListResubmit`                                                   | Write  | `entity-analysis-model-list-author`                           | 3                                                                                               |
| `EntityAnalysisModelListRetract`                                                    | Write  | `entity-analysis-model-list-author`                           | 3                                                                                               |
| `EntityAnalysisModelListValueResubmit`                                              | Write  | `entity-analysis-model-list-value-author`                     | 3                                                                                               |
| `EntityAnalysisModelListValueRetract`                                               | Write  | `entity-analysis-model-list-value-author`                     | 3                                                                                               |
| `EntityAnalysisModelRequestXPathResubmit`                                           | Write  | `entity-analysis-model-request-x-path-author`                 | 7                                                                                               |
| `EntityAnalysisModelRequestXPathRetract`                                            | Write  | `entity-analysis-model-request-x-path-author`                 | 7                                                                                               |
| `EntityAnalysisModelSanctionResubmit`                                               | Write  | `entity-analysis-model-sanction-author`                       | 11                                                                                              |
| `EntityAnalysisModelSanctionRetract`                                                | Write  | `entity-analysis-model-sanction-author`                       | 11                                                                                              |
| `EntityAnalysisModelTagResubmit`                                                    | Write  | `entity-analysis-model-tag-author`                            | 37                                                                                              |
| `EntityAnalysisModelTagRetract`                                                     | Write  | `entity-analysis-model-tag-author`                            | 37                                                                                              |
| `EntityAnalysisModelTtlCounterResubmit`                                             | Write  | `entity-analysis-model-ttl-counter-author`                    | 12                                                                                              |
| `EntityAnalysisModelTtlCounterRetract`                                              | Write  | `entity-analysis-model-ttl-counter-author`                    | 12                                                                                              |
| `ExhaustiveSearchInstanceResubmit`                                                  | Write  | `exhaustive-search-instance-author`                           | 16                                                                                              |
| `ExhaustiveSearchInstanceRetract`                                                   | Write  | `exhaustive-search-instance-author`                           | 16                                                                                              |
### ActivationWatcherReplay

`ActivationWatcherReplay` is a **read** operation, idempotent, non-destructive.

Replays ActivationWatcher rows created within a date range, scoped to the caller's tenant. Read-only.

- **Owning agent:** `activation-watcher-and-archive` (Activation Watcher and Archive) in the Case Investigation Desk
  domain
- **Access:** permission 30
- **Permission source:** `ActivationWatcherService.RequiredPermissions`
- **Service:** `ActivationWatcherService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ActivationWatcher.cs`

### AdvancedAnalyticsInstanceAllocateToModel

`AdvancedAnalyticsInstanceAllocateToModel` is a **write** operation, idempotent, non-destructive.

Allocates an instance to a Jube Model of the caller's tenant; needs the Promote Model permission.

- **Owning agent:** `advanced-analytics-instance-author` (Advanced Analytics Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 47
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsInstanceCount

`AdvancedAnalyticsInstanceCount` is a **read** operation, idempotent, non-destructive.

Counts the Advanced Analytics instances matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `advanced-analytics-instance-inspector` (Advanced Analytics Instance Inspector) in the Model Risk
  and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`

### AdvancedAnalyticsInstanceCreate

`AdvancedAnalyticsInstanceCreate` is a **write** operation, not idempotent, non-destructive.

Creates an unallocated Advanced Analytics instance in the caller's tenant.

- **Owning agent:** `advanced-analytics-instance-author` (Advanced Analytics Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsTrainingDataGet

`AdvancedAnalyticsTrainingDataGet` is a **read** operation, idempotent, non-destructive.

Reports the shape, size and expiry of training data already put in the cache, or nothing when it has expired.

- **Owning agent:** `advanced-analytics-training-data` (Advanced Analytics Training Data) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsTrainingData.cs`

### AdvancedAnalyticsTrainingDataPut

`AdvancedAnalyticsTrainingDataPut` is a **write** operation, not idempotent, non-destructive.

Turns a cached table into the numeric training data a run reads, by named columns, and returns the cache pointer to
submit against.

- **Owning agent:** `advanced-analytics-training-data` (Advanced Analytics Training Data) in the Model Risk and
  Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsTrainingData.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsInstanceDeallocate

`AdvancedAnalyticsInstanceDeallocate` is a **write** operation, idempotent, non-destructive.

Removes an instance's allocation to a Jube Model; needs the Promote Model permission.

- **Owning agent:** `advanced-analytics-instance-author` (Advanced Analytics Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 47
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsInstanceDelete

`AdvancedAnalyticsInstanceDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an Advanced Analytics instance, taking it out of the live pipeline.

- **Owning agent:** `advanced-analytics-instance-author` (Advanced Analytics Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsInstanceFilter

`AdvancedAnalyticsInstanceFilter` is a **read** operation, idempotent, non-destructive.

Returns the Advanced Analytics instances matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `advanced-analytics-instance-inspector` (Advanced Analytics Instance Inspector) in the Model Risk
  and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`

### AdvancedAnalyticsInstanceFilterFields

`AdvancedAnalyticsInstanceFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Advanced Analytics instances may use.

- **Owning agent:** `advanced-analytics-instance-inspector` (Advanced Analytics Instance Inspector) in the Model Risk
  and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`

### AdvancedAnalyticsInstanceGet

`AdvancedAnalyticsInstanceGet` is a **read** operation, idempotent, non-destructive.

Returns one Advanced Analytics instance by id, scoped to the caller's tenant.

- **Owning agent:** `advanced-analytics-instance-inspector` (Advanced Analytics Instance Inspector) in the Model Risk
  and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`

### AdvancedAnalyticsInstanceGetByGuid

`AdvancedAnalyticsInstanceGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Advanced Analytics instance by Guid, scoped to the caller's tenant.

- **Owning agent:** `advanced-analytics-instance-inspector` (Advanced Analytics Instance Inspector) in the Model Risk
  and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`

### AdvancedAnalyticsInstanceList

`AdvancedAnalyticsInstanceList` is a **read** operation, idempotent, non-destructive.

Lists Advanced Analytics instances for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `advanced-analytics-instance-inspector` (Advanced Analytics Instance Inspector) in the Model Risk
  and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`

### AdvancedAnalyticsInstanceResubmit

`AdvancedAnalyticsInstanceResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected change to an instance for approval again; needs the Promote Model permission.

- **Owning agent:** `advanced-analytics-instance-author` (Advanced Analytics Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 47
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsInstanceRetract

`AdvancedAnalyticsInstanceRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected change to an instance, restoring its last approved state; needs the Promote Model permission.

- **Owning agent:** `advanced-analytics-instance-author` (Advanced Analytics Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 47
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsInstanceUpdate

`AdvancedAnalyticsInstanceUpdate` is a **write** operation, idempotent, non-destructive.

Renames an instance or changes its output flags; never changes its allocation.

- **Owning agent:** `advanced-analytics-instance-author` (Advanced Analytics Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsInstanceValidate

`AdvancedAnalyticsInstanceValidate` is a **read** operation, idempotent, non-destructive.

Validates an Advanced Analytics instance without saving it and returns every failure.

- **Owning agent:** `advanced-analytics-instance-test-bench` (Advanced Analytics Instance Test Bench) in the Model Risk
  and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsInstance.cs`

### AdvancedAnalyticsModelCompareVersions

`AdvancedAnalyticsModelCompareVersions` is a **read** operation, idempotent, non-destructive.

Compares two model versions on held-out metrics and says whether the comparison is fair.

- **Owning agent:** `advanced-analytics-model-inspector` (Advanced Analytics Model Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsModel.cs`

### AdvancedAnalyticsModelGet

`AdvancedAnalyticsModelGet` is a **read** operation, idempotent, non-destructive.

Returns one trained model with all its versions and their headline metrics.

- **Owning agent:** `advanced-analytics-model-inspector` (Advanced Analytics Model Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsModel.cs`

### AdvancedAnalyticsModelGetPromotionEvidence

`AdvancedAnalyticsModelGetPromotionEvidence` is a **read** operation, idempotent, non-destructive.

Returns the evidence needed to decide whether a model version deserves promotion.

- **Owning agent:** `advanced-analytics-model-inspector` (Advanced Analytics Model Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsModel.cs`

### AdvancedAnalyticsModelGetVersion

`AdvancedAnalyticsModelGetVersion` is a **read** operation, idempotent, non-destructive.

Returns one model version with its complete stored evidence.

- **Owning agent:** `advanced-analytics-model-inspector` (Advanced Analytics Model Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsModel.cs`

### AdvancedAnalyticsModelList

`AdvancedAnalyticsModelList` is a **read** operation, idempotent, non-destructive.

Lists trained models for the caller's tenant with their live version, keyset-paged and capped.

- **Owning agent:** `advanced-analytics-model-inspector` (Advanced Analytics Model Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsModel.cs`

### AdvancedAnalyticsModelListVersions

`AdvancedAnalyticsModelListVersions` is a **read** operation, idempotent, non-destructive.

Lists the versions of a trained model with headline metrics and warning codes.

- **Owning agent:** `advanced-analytics-model-inspector` (Advanced Analytics Model Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsModel.cs`

### AdvancedAnalyticsModelPromoteVersion

`AdvancedAnalyticsModelPromoteVersion` is a **write** operation, idempotent, non-destructive.

Makes a model version live, recording its evidence; needs the Promote Model permission.

- **Owning agent:** `advanced-analytics-model-author` (Advanced Analytics Model Author) in the Model Risk and Analytics
  domain
- **Access:** permission 47
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsModel.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AdvancedAnalyticsModelRollback

`AdvancedAnalyticsModelRollback` is a **write** operation, idempotent, non-destructive.

Puts a model back on an earlier version; needs the Promote Model permission.

- **Owning agent:** `advanced-analytics-model-author` (Advanced Analytics Model Author) in the Model Risk and Analytics
  domain
- **Access:** permission 47
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsModel.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ApplicationLogEntryList

`ApplicationLogEntryList` is a **read** operation, idempotent, non-destructive.

Lists application-wide WARN/ERROR/FATAL log entries, most recent first, capped at 100000, with optional date-range and
substring search (Message/LoggerName/Exception) filters. Not scoped to any Model or tenant. Also accepts
samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `application-and-container-logs` (Application and Container Logs) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `ApplicationLogEntryService.RequiredPermissions`
- **Service:** `ApplicationLogEntryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ApplicationLogEntry.cs`

### ArchiveTag

`ArchiveTag` is a **write** operation, idempotent, non-destructive.

Replaces the full Tag set on an archived transaction, scoped to the caller's tenant.

- **Owning agent:** `activation-watcher-and-archive` (Activation Watcher and Archive) in the Case Investigation Desk
  domain
- **Access:** permission 1
- **Permission source:** `ArchiveService.RequiredPermissions`
- **Service:** `ArchiveService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Archive.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ArchiverStagePerformanceCounterList

`ArchiverStagePerformanceCounterList` is a **read** operation, idempotent, non-destructive.

Lists background archiver stage performance rows across every Model -- BuildArchiveJson, CaseCreationDispatch,
RdbmsArchiveWrite, BulkCopyArchiveBuffer -- most recent first, capped at 100000, with optional date-range and
exact-match (StageId, e.g. 1=BuildArchiveJson) filters. Landlord only: every other caller is refused with a 403. Also
accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `activation-watcher-and-archive` (Activation Watcher and Archive) in the Case Investigation Desk
  domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `ArchiverStagePerformanceCounterService.RequiredPermissions`
- **Service:** `ArchiverStagePerformanceCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ArchiverStagePerformanceCounter.cs`

### ArchiverWarningList

`ArchiverWarningList` is a **read** operation, idempotent, non-destructive.

Lists captured archiver warn-threshold breaches -- the specific invocation (when the stage operates on one item; null
for BulkCopyArchiveBuffer), the model, and the slow stage -- most recent first, capped at 100000, with optional
date-range and exact-match (StageId, e.g. 1=BuildArchiveJson) filters. Landlord only: every other caller is refused with
a 403. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most
recent ones.

- **Owning agent:** `activation-watcher-and-archive` (Activation Watcher and Archive) in the Case Investigation Desk
  domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `ArchiverWarningService.RequiredPermissions`
- **Service:** `ArchiverWarningService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ArchiverWarning.cs`

### AskJoobyConversationCreate

`AskJoobyConversationCreate` is a **write** operation, not idempotent, non-destructive.

Starts a new, empty, untitled AskJooby conversation in the caller's tenant.

- **Owning agent:** `ask-jooby-observability` (Ask Jooby Observability) in the Assistant Conversation Administration
  domain
- **Access:** permission 65
- **Permission source:** `AskJoobyConversationService.RequiredPermissions`
- **Service:** `AskJoobyConversationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AskJoobyConversation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AskJoobyConversationGetMessages

`AskJoobyConversationGetMessages` is a **read** operation, idempotent, non-destructive.

Returns the full transcript of one conversation, oldest first.

- **Owning agent:** `ask-jooby-observability` (Ask Jooby Observability) in the Assistant Conversation Administration
  domain
- **Access:** permission 65
- **Permission source:** `AskJoobyConversationService.RequiredPermissions`
- **Service:** `AskJoobyConversationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AskJoobyConversation.cs`

### AskJoobyConversationList

`AskJoobyConversationList` is a **read** operation, idempotent, non-destructive.

Lists AskJooby conversations for the caller's tenant, most recently active first.

- **Owning agent:** `ask-jooby-observability` (Ask Jooby Observability) in the Assistant Conversation Administration
  domain
- **Access:** permission 65
- **Permission source:** `AskJoobyConversationService.RequiredPermissions`
- **Service:** `AskJoobyConversationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AskJoobyConversation.cs`

### AskJoobyConversationSendMessage

`AskJoobyConversationSendMessage` is a **write** operation, not idempotent, non-destructive.

Sends a message to Jooby and streams the reply, persisting both sides of the exchange.

- **Owning agent:** `ask-jooby-observability` (Ask Jooby Observability) in the Assistant Conversation Administration
  domain
- **Access:** permission 65
- **Permission source:** `AskJoobyConversationService.RequiredPermissions`
- **Service:** `AskJoobyConversationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AskJoobyConversation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### AskJoobyRagChunkSearch

`AskJoobyRagChunkSearch` is a **read** operation, idempotent, non-destructive.

Searches the Pasture and returns the most relevant Bites, ranked by reciprocal-rank fusion of full-text and vector
search.

- **Owning agent:** `ask-jooby-observability` (Ask Jooby Observability) in the Assistant Conversation Administration
  domain
- **Access:** permission 65
- **Permission source:** `AskJoobyRagChunkService.RequiredPermissions`
- **Service:** `AskJoobyRagChunkService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AskJoobyRagChunk.cs`

### CancelAdvancedAnalyticsTrainingRun

`CancelAdvancedAnalyticsTrainingRun` is a **write** operation, idempotent, non-destructive.

Cancels a training run submitted by any family's Submit tool; a finished run is returned unchanged.

- **Owning agent:** `advanced-analytics-training-classifier-trees` (Advanced Analytics Training Classifier Trees) in the
  Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/AdvancedAnalyticsTrainingRunTools.cs`
- **Confirmation:** none; the run it stops is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### CaptureQueueHealthList

`CaptureQueueHealthList` is a **read** operation, idempotent, non-destructive.

Lists per-cycle depth and dropped-count snapshots for every bounded in-memory capture queue in the observability suite
(ModelInvokeWarning, CaseCreationWarning, ArchiverWarning, RedisSentinelEvent, RedisConnectionEvent,
OpenTelemetryMetric), one row per queue every ~60 seconds regardless of activity. Any non-zero DroppedCount means real
data was silently discarded because a queue hit capacity -- the first thing to check when an expected event or warning
seems to be missing. Not scoped to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset
of matching rows instead of the most recent ones.

- **Owning agent:** `engine-queue-and-throughput-counters` (Engine Queue and Throughput Counters) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `CaptureQueueHealthService.RequiredPermissions`
- **Service:** `CaptureQueueHealthService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaptureQueueHealth.cs`

### CaseByCaseKeyValueGet

`CaseByCaseKeyValueGet` is a **read** operation, idempotent, non-destructive.

Gets the Cases matching a Case Key and Case Key Value that the caller's role may see.

- **Owning agent:** `case-lookup-and-notes` (Case Lookup and Notes) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseByCaseKeyValueService.RequiredPermissions`
- **Service:** `CaseByCaseKeyValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseByCaseKeyValue.cs`

### CaseByIdGet

`CaseByIdGet` is a **read** operation, idempotent, non-destructive.

Gets one case by Id with its status colours, formatted payload and visible activations; records a case-viewed event.

- **Owning agent:** `case-lookup-and-notes` (Case Lookup and Notes) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseByIdService.RequiredPermissions`
- **Service:** `CaseByIdService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseById.cs`

### CaseBySessionCaseSearchCompileGet

`CaseBySessionCaseSearchCompileGet` is a **write** operation, not idempotent, non-destructive.

Retrieves the next case for a compiled Session Case Search, recording a case-opened event and locking the case to the
calling user.

- **Owning agent:** `case-lookup-and-notes` (Case Lookup and Notes) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseBySessionCaseSearchCompileService.RequiredPermissions`
- **Service:** `CaseBySessionCaseSearchCompileService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseBySessionCaseSearchCompile.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseCreateFromCaseKeyValue

`CaseCreateFromCaseKeyValue` is a **write** operation, not idempotent, non-destructive.

Raises a new case (or promotes an existing one) from a matching archived transaction; dispatches the target status's
notification/webhook.

- **Owning agent:** `case-record-and-files` (Case Record and Files) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseService.RequiredPermissions`
- **Service:** `CaseService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Case.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseCreationStagePerformanceCounterList

`CaseCreationStagePerformanceCounterList` is a **read** operation, idempotent, non-destructive.

Lists background case creation stage performance rows -- ExistingCasePriorityLookup, WorkflowStatusLookupAndPersist,
Notification, HttpEndpoint -- most recent first, capped at 100000, with optional date-range and exact-match (StageId,
e.g. 1=ExistingCasePriorityLookup) filters. Not scoped to any tenant or Model -- case creation is a single global queue
shared by every model. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead
of the most recent ones.

- **Owning agent:** `case-lookup-and-notes` (Case Lookup and Notes) in the Case Investigation Desk domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `CaseCreationStagePerformanceCounterService.RequiredPermissions`
- **Service:** `CaseCreationStagePerformanceCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseCreationStagePerformanceCounter.cs`

### CaseCreationWarningList

`CaseCreationWarningList` is a **read** operation, idempotent, non-destructive.

Lists captured case creation warn-threshold breaches -- the specific case (workflow, key, key value), the slow stage,
and the notification/HTTP destination when relevant -- most recent first, capped at 100000, with optional date-range,
exact-match (StageId, e.g. 1=ExistingCasePriorityLookup), and substring (CaseKeyValue) filters. Landlord only: every
other caller is refused with a 403. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching
rows instead of the most recent ones.

- **Owning agent:** `case-journal-and-events` (Case Journal and Events) in the Case Investigation Desk domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `CaseCreationWarningService.RequiredPermissions`
- **Service:** `CaseCreationWarningService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseCreationWarning.cs`

### CaseEventByCaseKeyValueGet

`CaseEventByCaseKeyValueGet` is a **read** operation, idempotent, non-destructive.

Lists the audit events of a case identified by case key and case key value, newest first.

- **Owning agent:** `case-journal-and-events` (Case Journal and Events) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseEventByCaseKeyValueService.RequiredPermissions`
- **Service:** `CaseEventByCaseKeyValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseEventByCaseKeyValue.cs`

### CaseFileListByCaseKeyValue

`CaseFileListByCaseKeyValue` is a **read** operation, idempotent, non-destructive.

Lists the files attached to the Case for a given key field name and value, scoped to the caller's tenant and Case
Workflow roles.

- **Owning agent:** `case-record-and-files` (Case Record and Files) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseFileService.RequiredPermissions`
- **Service:** `CaseFileService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseFile.cs`

### CaseFileRemove

`CaseFileRemove` is a **delete** operation, idempotent, destructive.

Removes (soft-deletes) a file attached to a Case.

- **Owning agent:** `case-record-and-files` (Case Record and Files) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseFileService.RequiredPermissions`
- **Service:** `CaseFileService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseFile.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseFileUpload

`CaseFileUpload` is a **write** operation, not idempotent, non-destructive.

Uploads and attaches a file to a Case in the caller's tenant.

- **Owning agent:** `case-record-and-files` (Case Record and Files) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseFileService.RequiredPermissions`
- **Service:** `CaseFileService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseFile.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseGet

`CaseGet` is a **read** operation, idempotent, non-destructive.

Returns a single active case by Id, scoped to the caller's tenant and role.

- **Owning agent:** `case-record-and-files` (Case Record and Files) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseService.RequiredPermissions`
- **Service:** `CaseService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Case.cs`

### CaseJournalGet

`CaseJournalGet` is a **read** operation, idempotent, non-destructive.

Returns the tenant-scoped Case journal of Archive entries matching a payload key and value.

- **Owning agent:** `case-journal-and-events` (Case Journal and Events) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseJournalService.RequiredPermissions`
- **Service:** `CaseJournalService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseJournal.cs`

### CaseList

`CaseList` is a **read** operation, idempotent, non-destructive.

Lists active cases for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-record-and-files` (Case Record and Files) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseService.RequiredPermissions`
- **Service:** `CaseService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Case.cs`

### CaseNoteByCaseKeyValueGet

`CaseNoteByCaseKeyValueGet` is a **read** operation, idempotent, non-destructive.

Lists the case notes for cases matching a case key and value, newest first, limited to workflows, statuses and actions
the caller's roles may see within the caller's tenant.

- **Owning agent:** `case-journal-and-events` (Case Journal and Events) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseNoteByCaseKeyValueService.RequiredPermissions`
- **Service:** `CaseNoteByCaseKeyValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseNoteByCaseKeyValue.cs`

### CaseNoteInsert

`CaseNoteInsert` is a **write** operation, not idempotent, non-destructive.

Adds a note to a case, filed against a CaseWorkflowAction; dispatches that action's notification/webhook if enabled.
Notes are append-only.

- **Owning agent:** `case-lookup-and-notes` (Case Lookup and Notes) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseNoteService.RequiredPermissions`
- **Service:** `CaseNoteService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseNote.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseNoteListByCaseKeyValue

`CaseNoteListByCaseKeyValue` is a **read** operation, idempotent, non-destructive.

Lists case notes for a Case Key/Value pair, newest first, scoped to the caller's tenant.

- **Owning agent:** `case-lookup-and-notes` (Case Lookup and Notes) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseNoteService.RequiredPermissions`
- **Service:** `CaseNoteService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseNote.cs`

### CaseNoteValidate

`CaseNoteValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Note without saving it and returns every failure.

- **Owning agent:** `case-lookup-and-notes` (Case Lookup and Notes) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseNoteService.RequiredPermissions`
- **Service:** `CaseNoteService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseNote.cs`

### CaseUpdate

`CaseUpdate` is a **write** operation, idempotent, non-destructive.

Updates a case's closed status, lock, diary, rating and workflow status; a status change dispatches that status's
notification/webhook.

- **Owning agent:** `case-record-and-files` (Case Record and Files) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `CaseService.RequiredPermissions`
- **Service:** `CaseService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Case.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowActionCount

`CaseWorkflowActionCount` is a **read** operation, idempotent, non-destructive.

Counts the Case Workflow Actions matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `case-workflow-action-inspector` (Case Workflow Action Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowActionCreate

`CaseWorkflowActionCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Case Workflow Action; calling twice creates two.

- **Owning agent:** `case-workflow-action-author` (Case Workflow Action Author) in the Case Workflow Governance domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowActionDelete

`CaseWorkflowActionDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Case Workflow Action; reversible only by direct database action.

- **Owning agent:** `case-workflow-action-author` (Case Workflow Action Author) in the Case Workflow Governance domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowActionFilter

`CaseWorkflowActionFilter` is a **read** operation, idempotent, non-destructive.

Returns the Case Workflow Actions matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `case-workflow-action-inspector` (Case Workflow Action Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowActionFilterFields

`CaseWorkflowActionFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Case Workflow Actions may use.

- **Owning agent:** `case-workflow-action-inspector` (Case Workflow Action Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowActionGet

`CaseWorkflowActionGet` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Action by Id.

- **Owning agent:** `case-workflow-action-inspector` (Case Workflow Action Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowActionGetByGuid

`CaseWorkflowActionGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Action by Id.

- **Owning agent:** `case-workflow-action-inspector` (Case Workflow Action Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowActionList

`CaseWorkflowActionList` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow Actions visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-workflow-action-inspector` (Case Workflow Action Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowActionListByWorkflowGuidActiveOnly

`CaseWorkflowActionListByWorkflowGuidActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Actions for a parent Case Workflow (by Guid) that the caller holds a role grant on.

- **Owning agent:** `case-workflow-action-inspector` (Case Workflow Action Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 22, 1
- **Permission source:** `CaseWorkflowActionService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowActionListByWorkflowIdActiveOnly

`CaseWorkflowActionListByWorkflowIdActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Actions for a parent Case Workflow that the caller holds a role grant on.

- **Owning agent:** `case-workflow-action-inspector` (Case Workflow Action Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 22, 1
- **Permission source:** `CaseWorkflowActionService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowActionRoleCreate

`CaseWorkflowActionRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Case Workflow Action; calling twice creates two grants.

- **Owning agent:** `action-and-display-role-grants` (Action and Display Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowActionRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowActionRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowActionRoleDelete

`CaseWorkflowActionRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Case Workflow Action; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `action-and-display-role-grants` (Action and Display Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowActionRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowActionRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowActionRoleListByCaseWorkflowActionGuid

`CaseWorkflowActionRoleListByCaseWorkflowActionGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Case Workflow Action.

- **Owning agent:** `action-and-display-role-grants` (Action and Display Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowActionRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowActionRole.cs`

### CaseWorkflowActionUpdate

`CaseWorkflowActionUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Case Workflow Action; overwrites, writes a version-audit row.

- **Owning agent:** `case-workflow-action-author` (Case Workflow Action Author) in the Case Workflow Governance domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowActionValidate

`CaseWorkflowActionValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Workflow Action without saving it and returns every failure.

- **Owning agent:** `case-workflow-action-test-bench` (Case Workflow Action Test Bench) in the Case Workflow Governance
  domain
- **Access:** permission 22
- **Permission source:** `CaseWorkflowActionService.RequiredPermissions`
- **Service:** `CaseWorkflowActionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowAction.cs`

### CaseWorkflowCount

`CaseWorkflowCount` is a **read** operation, idempotent, non-destructive.

Counts the Case Workflows matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `case-workflow-inspector` (Case Workflow Inspector) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`

### CaseWorkflowCreate

`CaseWorkflowCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Case Workflow; calling twice creates two.

- **Owning agent:** `case-workflow-author` (Case Workflow Author) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowDelete

`CaseWorkflowDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Case Workflow; reversible only by direct database action.

- **Owning agent:** `case-workflow-author` (Case Workflow Author) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowDisplayCount

`CaseWorkflowDisplayCount` is a **read** operation, idempotent, non-destructive.

Counts the Case Workflow Displays matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `case-workflow-display-inspector` (Case Workflow Display Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowDisplayCreate

`CaseWorkflowDisplayCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Case Workflow Display; calling twice creates two.

- **Owning agent:** `case-workflow-display-author` (Case Workflow Display Author) in the Case Workflow Governance domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowDisplayDelete

`CaseWorkflowDisplayDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Case Workflow Display; reversible only by direct database action.

- **Owning agent:** `case-workflow-display-author` (Case Workflow Display Author) in the Case Workflow Governance domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowDisplayExecutionExecute

`CaseWorkflowDisplayExecutionExecute` is a **read** operation, idempotent, non-destructive.

Renders a case workflow display HTML template with the case's stored payload tokens replaced.

- **Owning agent:** `form-entries-and-macro-executions` (Form Entries and Macro Executions) in the Case Workflow
  Governance domain
- **Access:** permission 1
- **Permission source:** `CaseWorkflowDisplayExecutionService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayExecutionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplayExecution.cs`

### CaseWorkflowDisplayFilter

`CaseWorkflowDisplayFilter` is a **read** operation, idempotent, non-destructive.

Returns the Case Workflow Displays matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `case-workflow-display-inspector` (Case Workflow Display Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowDisplayFilterFields

`CaseWorkflowDisplayFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Case Workflow Displays may use.

- **Owning agent:** `case-workflow-display-inspector` (Case Workflow Display Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowDisplayGet

`CaseWorkflowDisplayGet` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Display by Id.

- **Owning agent:** `case-workflow-display-inspector` (Case Workflow Display Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowDisplayGetByGuid

`CaseWorkflowDisplayGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Display by Id.

- **Owning agent:** `case-workflow-display-inspector` (Case Workflow Display Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowDisplayList

`CaseWorkflowDisplayList` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow Displays visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-workflow-display-inspector` (Case Workflow Display Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowDisplayListByWorkflowGuidActiveOnly

`CaseWorkflowDisplayListByWorkflowGuidActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Displays for a parent Case Workflow (by Guid) that the caller holds a role grant on.

- **Owning agent:** `case-workflow-display-inspector` (Case Workflow Display Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 23, 1
- **Permission source:** `CaseWorkflowDisplayService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowDisplayListByWorkflowIdActiveOnly

`CaseWorkflowDisplayListByWorkflowIdActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Displays for a parent Case Workflow that the caller holds a role grant on.

- **Owning agent:** `case-workflow-display-inspector` (Case Workflow Display Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 23, 1
- **Permission source:** `CaseWorkflowDisplayService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowDisplayRoleCreate

`CaseWorkflowDisplayRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Case Workflow Display; calling twice creates two grants.

- **Owning agent:** `action-and-display-role-grants` (Action and Display Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplayRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowDisplayRoleDelete

`CaseWorkflowDisplayRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Case Workflow Display; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `action-and-display-role-grants` (Action and Display Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplayRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowDisplayRoleListByCaseWorkflowDisplayGuid

`CaseWorkflowDisplayRoleListByCaseWorkflowDisplayGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Case Workflow Display.

- **Owning agent:** `action-and-display-role-grants` (Action and Display Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplayRole.cs`

### CaseWorkflowDisplayUpdate

`CaseWorkflowDisplayUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Case Workflow Display; overwrites, writes a version-audit row.

- **Owning agent:** `case-workflow-display-author` (Case Workflow Display Author) in the Case Workflow Governance domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowDisplayValidate

`CaseWorkflowDisplayValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Workflow Display without saving it and returns every failure.

- **Owning agent:** `case-workflow-display-test-bench` (Case Workflow Display Test Bench) in the Case Workflow
  Governance domain
- **Access:** permission 23
- **Permission source:** `CaseWorkflowDisplayService.RequiredPermissions`
- **Service:** `CaseWorkflowDisplayService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowDisplay.cs`

### CaseWorkflowFilter

`CaseWorkflowFilter` is a **read** operation, idempotent, non-destructive.

Returns the Case Workflows matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `case-workflow-inspector` (Case Workflow Inspector) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`

### CaseWorkflowFilterCount

`CaseWorkflowFilterCount` is a **read** operation, idempotent, non-destructive.

Counts the Case Workflow Filters matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `case-workflow-filter-inspector-1` (Case Workflow Filter Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 25, 1
- **Permission source:** `CaseWorkflowFilterService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFilterCreate

`CaseWorkflowFilterCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Case Workflow Filter; calling twice creates two.

- **Owning agent:** `case-workflow-filter-author` (Case Workflow Filter Author) in the Case Workflow Governance domain
- **Access:** permission 25
- **Permission source:** `CaseWorkflowFilterService.RequiredPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFilterDelete

`CaseWorkflowFilterDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Case Workflow Filter; reversible only by direct database action.

- **Owning agent:** `case-workflow-filter-author` (Case Workflow Filter Author) in the Case Workflow Governance domain
- **Access:** permission 25
- **Permission source:** `CaseWorkflowFilterService.RequiredPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFilterFields

`CaseWorkflowFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Case Workflows may use.

- **Owning agent:** `case-workflow-inspector` (Case Workflow Inspector) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`

### CaseWorkflowFilterFilter

`CaseWorkflowFilterFilter` is a **read** operation, idempotent, non-destructive.

Returns the Case Workflow Filters matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `case-workflow-filter-inspector-1` (Case Workflow Filter Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 25, 1
- **Permission source:** `CaseWorkflowFilterService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFilterFilterFields

`CaseWorkflowFilterFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Case Workflow Filters may use.

- **Owning agent:** `case-workflow-filter-inspector-1` (Case Workflow Filter Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 25, 1
- **Permission source:** `CaseWorkflowFilterService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFilterGet

`CaseWorkflowFilterGet` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Filter by Id.

- **Owning agent:** `case-workflow-filter-inspector-1` (Case Workflow Filter Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 25, 1
- **Permission source:** `CaseWorkflowFilterService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFilterGetByGuid

`CaseWorkflowFilterGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Filter by Guid.

- **Owning agent:** `case-workflow-filter-inspector-2` (Case Workflow Filter Inspector 2) in the Case Workflow
  Governance domain
- **Access:** permission 25, 1
- **Permission source:** `CaseWorkflowFilterService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFilterList

`CaseWorkflowFilterList` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow Filters visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-workflow-filter-inspector-1` (Case Workflow Filter Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 25, 1
- **Permission source:** `CaseWorkflowFilterService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFilterListByWorkflowGuidActiveOnly

`CaseWorkflowFilterListByWorkflowGuidActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Filters for a parent Case Workflow (by Guid) that the caller holds a role grant on.

- **Owning agent:** `case-workflow-filter-inspector-1` (Case Workflow Filter Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 25, 1
- **Permission source:** `CaseWorkflowFilterService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFilterListByWorkflowIdActiveOnly

`CaseWorkflowFilterListByWorkflowIdActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Filters for a parent Case Workflow that the caller holds a role grant on.

- **Owning agent:** `case-workflow-filter-inspector-1` (Case Workflow Filter Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 25, 1
- **Permission source:** `CaseWorkflowFilterService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFilterRoleCreate

`CaseWorkflowFilterRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Case Workflow Filter; calling twice creates two grants.

- **Owning agent:** `filter-and-form-role-grants` (Filter and Form Role Grants) in the Case Workflow Governance domain
- **Access:** permission 25
- **Permission source:** `CaseWorkflowFilterRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowFilterRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilterRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFilterRoleDelete

`CaseWorkflowFilterRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Case Workflow Filter; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `filter-and-form-role-grants` (Filter and Form Role Grants) in the Case Workflow Governance domain
- **Access:** permission 25
- **Permission source:** `CaseWorkflowFilterRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowFilterRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilterRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFilterRoleListByCaseWorkflowFilterGuid

`CaseWorkflowFilterRoleListByCaseWorkflowFilterGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Case Workflow Filter.

- **Owning agent:** `filter-and-form-role-grants` (Filter and Form Role Grants) in the Case Workflow Governance domain
- **Access:** permission 25
- **Permission source:** `CaseWorkflowFilterRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowFilterRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilterRole.cs`

### CaseWorkflowFilterUpdate

`CaseWorkflowFilterUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Case Workflow Filter; overwrites, writes a version-audit row.

- **Owning agent:** `case-workflow-filter-author` (Case Workflow Filter Author) in the Case Workflow Governance domain
- **Access:** permission 25
- **Permission source:** `CaseWorkflowFilterService.RequiredPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFilterValidate

`CaseWorkflowFilterValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Workflow Filter without saving it and returns every failure.

- **Owning agent:** `case-workflow-filter-test-bench` (Case Workflow Filter Test Bench) in the Case Workflow Governance
  domain
- **Access:** permission 25
- **Permission source:** `CaseWorkflowFilterService.RequiredPermissions`
- **Service:** `CaseWorkflowFilterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFilter.cs`

### CaseWorkflowFormCount

`CaseWorkflowFormCount` is a **read** operation, idempotent, non-destructive.

Counts the Case Workflow Forms matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `case-workflow-form-inspector` (Case Workflow Form Inspector) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowFormCreate

`CaseWorkflowFormCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Case Workflow Form; calling twice creates two.

- **Owning agent:** `case-workflow-form-author` (Case Workflow Form Author) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFormDelete

`CaseWorkflowFormDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Case Workflow Form; reversible only by direct database action.

- **Owning agent:** `case-workflow-form-author` (Case Workflow Form Author) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFormEntryByCaseKeyValueGet

`CaseWorkflowFormEntryByCaseKeyValueGet` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow Form Entries for a Case Key and Case Key Value, newest case first, limited to the caller's tenant
and to Case Workflows and Statuses the caller's role holds.

- **Owning agent:** `form-entries-and-macro-executions` (Form Entries and Macro Executions) in the Case Workflow
  Governance domain
- **Access:** permission 1
- **Permission source:** `CaseWorkflowFormEntryByCaseKeyValueService.RequiredPermissions`
- **Service:** `CaseWorkflowFormEntryByCaseKeyValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFormEntryByCaseKeyValue.cs`

### CaseWorkflowFormEntryInsert

`CaseWorkflowFormEntryInsert` is a **write** operation, not idempotent, non-destructive.

Submits a CaseWorkflowForm entry for a case; dispatches that form's notification/webhook if enabled.

- **Owning agent:** `form-entries-and-macro-executions` (Form Entries and Macro Executions) in the Case Workflow
  Governance domain
- **Access:** permission 1
- **Permission source:** `CaseWorkflowFormEntryService.RequiredPermissions`
- **Service:** `CaseWorkflowFormEntryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFormEntry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFormEntryListByCaseKeyValue

`CaseWorkflowFormEntryListByCaseKeyValue` is a **read** operation, idempotent, non-destructive.

Lists case workflow form entries for a Case Key/Value pair, newest first, scoped to the caller's tenant and role grants.

- **Owning agent:** `form-entries-and-macro-executions` (Form Entries and Macro Executions) in the Case Workflow
  Governance domain
- **Access:** permission 1
- **Permission source:** `CaseWorkflowFormEntryService.RequiredPermissions`
- **Service:** `CaseWorkflowFormEntryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFormEntry.cs`

### CaseWorkflowFormEntryValidate

`CaseWorkflowFormEntryValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Workflow Form Entry without saving it and returns every failure.

- **Owning agent:** `form-entries-and-macro-executions` (Form Entries and Macro Executions) in the Case Workflow
  Governance domain
- **Access:** permission 1
- **Permission source:** `CaseWorkflowFormEntryService.RequiredPermissions`
- **Service:** `CaseWorkflowFormEntryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFormEntry.cs`

### CaseWorkflowFormEntryValueListByCaseWorkflowFormEntryId

`CaseWorkflowFormEntryValueListByCaseWorkflowFormEntryId` is a **read** operation, idempotent, non-destructive.

Lists the submitted field name/value rows for a CaseWorkflowFormEntry, newest first, scoped to the caller's tenant and
role grants.

- **Owning agent:** `form-entries-and-macro-executions` (Form Entries and Macro Executions) in the Case Workflow
  Governance domain
- **Access:** permission 1
- **Permission source:** `CaseWorkflowFormEntryValueService.RequiredPermissions`
- **Service:** `CaseWorkflowFormEntryValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFormEntryValue.cs`

### CaseWorkflowFormFilter

`CaseWorkflowFormFilter` is a **read** operation, idempotent, non-destructive.

Returns the Case Workflow Forms matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `case-workflow-form-inspector` (Case Workflow Form Inspector) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowFormFilterFields

`CaseWorkflowFormFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Case Workflow Forms may use.

- **Owning agent:** `case-workflow-form-inspector` (Case Workflow Form Inspector) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowFormGet

`CaseWorkflowFormGet` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Form by Id.

- **Owning agent:** `case-workflow-form-inspector` (Case Workflow Form Inspector) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowFormGetByGuid

`CaseWorkflowFormGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Form by Id.

- **Owning agent:** `case-workflow-form-inspector` (Case Workflow Form Inspector) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowFormList

`CaseWorkflowFormList` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow Forms visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-workflow-form-inspector` (Case Workflow Form Inspector) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowFormListByWorkflowGuidActiveOnly

`CaseWorkflowFormListByWorkflowGuidActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Forms for a parent Case Workflow (by Guid) that the caller holds a role grant on.

- **Owning agent:** `case-workflow-form-inspector` (Case Workflow Form Inspector) in the Case Workflow Governance domain
- **Access:** permission 21, 1
- **Permission source:** `CaseWorkflowFormService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowFormListByWorkflowIdActiveOnly

`CaseWorkflowFormListByWorkflowIdActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Forms for a parent Case Workflow that the caller holds a role grant on.

- **Owning agent:** `case-workflow-form-inspector` (Case Workflow Form Inspector) in the Case Workflow Governance domain
- **Access:** permission 21, 1
- **Permission source:** `CaseWorkflowFormService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowFormRoleCreate

`CaseWorkflowFormRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Case Workflow Form; calling twice creates two grants.

- **Owning agent:** `filter-and-form-role-grants` (Filter and Form Role Grants) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowFormRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFormRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFormRoleDelete

`CaseWorkflowFormRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Case Workflow Form; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `filter-and-form-role-grants` (Filter and Form Role Grants) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowFormRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFormRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFormRoleListByCaseWorkflowFormGuid

`CaseWorkflowFormRoleListByCaseWorkflowFormGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Case Workflow Form.

- **Owning agent:** `filter-and-form-role-grants` (Filter and Form Role Grants) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowFormRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowFormRole.cs`

### CaseWorkflowFormUpdate

`CaseWorkflowFormUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Case Workflow Form; overwrites, writes a version-audit row.

- **Owning agent:** `case-workflow-form-author` (Case Workflow Form Author) in the Case Workflow Governance domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowFormValidate

`CaseWorkflowFormValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Workflow Form without saving it and returns every failure.

- **Owning agent:** `case-workflow-form-test-bench` (Case Workflow Form Test Bench) in the Case Workflow Governance
  domain
- **Access:** permission 21
- **Permission source:** `CaseWorkflowFormService.RequiredPermissions`
- **Service:** `CaseWorkflowFormService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowForm.cs`

### CaseWorkflowGet

`CaseWorkflowGet` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow by Id.

- **Owning agent:** `case-workflow-inspector` (Case Workflow Inspector) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`

### CaseWorkflowGetByGuid

`CaseWorkflowGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow by Id.

- **Owning agent:** `case-workflow-inspector` (Case Workflow Inspector) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`

### CaseWorkflowList

`CaseWorkflowList` is a **read** operation, idempotent, non-destructive.

Lists Case Workflows visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-workflow-inspector` (Case Workflow Inspector) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`

### CaseWorkflowListByModelActiveOnly

`CaseWorkflowListByModelActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflows for a parent model that the caller holds a role grant on.

- **Owning agent:** `case-workflow-inspector` (Case Workflow Inspector) in the Case Workflow Governance domain
- **Access:** permission 18, 1
- **Permission source:** `CaseWorkflowService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`

### CaseWorkflowMacroCount

`CaseWorkflowMacroCount` is a **read** operation, idempotent, non-destructive.

Counts the Case Workflow Macros matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `case-workflow-macro-inspector` (Case Workflow Macro Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowMacroCreate

`CaseWorkflowMacroCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Case Workflow Macro; calling twice creates two.

- **Owning agent:** `case-workflow-macro-author` (Case Workflow Macro Author) in the Case Workflow Governance domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowMacroDelete

`CaseWorkflowMacroDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Case Workflow Macro; reversible only by direct database action.

- **Owning agent:** `case-workflow-macro-author` (Case Workflow Macro Author) in the Case Workflow Governance domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowMacroExecutionExecute

`CaseWorkflowMacroExecutionExecute` is a **write** operation, not idempotent, non-destructive.

Executes an active case workflow macro against a case, sending the macro's configured notification and/or HTTP endpoint
call. Does not modify stored data.

- **Owning agent:** `form-entries-and-macro-executions` (Form Entries and Macro Executions) in the Case Workflow
  Governance domain
- **Access:** permission 1
- **Permission source:** `CaseWorkflowMacroExecutionService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroExecutionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacroExecution.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowMacroFilter

`CaseWorkflowMacroFilter` is a **read** operation, idempotent, non-destructive.

Returns the Case Workflow Macros matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `case-workflow-macro-inspector` (Case Workflow Macro Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowMacroFilterFields

`CaseWorkflowMacroFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Case Workflow Macros may use.

- **Owning agent:** `case-workflow-macro-inspector` (Case Workflow Macro Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowMacroGet

`CaseWorkflowMacroGet` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Macro by Id.

- **Owning agent:** `case-workflow-macro-inspector` (Case Workflow Macro Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowMacroGetByGuid

`CaseWorkflowMacroGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Macro by Id.

- **Owning agent:** `case-workflow-macro-inspector` (Case Workflow Macro Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowMacroList

`CaseWorkflowMacroList` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow Macros visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-workflow-macro-inspector` (Case Workflow Macro Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowMacroListByWorkflowGuidActiveOnly

`CaseWorkflowMacroListByWorkflowGuidActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Macros for a parent Case Workflow (by Guid) that the caller holds a role grant on.

- **Owning agent:** `case-workflow-macro-inspector` (Case Workflow Macro Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 24, 1
- **Permission source:** `CaseWorkflowMacroService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowMacroListByWorkflowIdActiveOnly

`CaseWorkflowMacroListByWorkflowIdActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Macros for a parent Case Workflow that the caller holds a role grant on.

- **Owning agent:** `case-workflow-macro-inspector` (Case Workflow Macro Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 24, 1
- **Permission source:** `CaseWorkflowMacroService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowMacroRoleCreate

`CaseWorkflowMacroRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Case Workflow Macro; calling twice creates two grants.

- **Owning agent:** `macro-and-workflow-role-grants` (Macro and Workflow Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacroRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowMacroRoleDelete

`CaseWorkflowMacroRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Case Workflow Macro; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `macro-and-workflow-role-grants` (Macro and Workflow Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacroRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowMacroRoleListByCaseWorkflowMacroGuid

`CaseWorkflowMacroRoleListByCaseWorkflowMacroGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Case Workflow Macro.

- **Owning agent:** `macro-and-workflow-role-grants` (Macro and Workflow Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacroRole.cs`

### CaseWorkflowMacroUpdate

`CaseWorkflowMacroUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Case Workflow Macro; overwrites, writes a version-audit row.

- **Owning agent:** `case-workflow-macro-author` (Case Workflow Macro Author) in the Case Workflow Governance domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowMacroValidate

`CaseWorkflowMacroValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Workflow Macro without saving it and returns every failure.

- **Owning agent:** `case-workflow-macro-test-bench` (Case Workflow Macro Test Bench) in the Case Workflow Governance
  domain
- **Access:** permission 24
- **Permission source:** `CaseWorkflowMacroService.RequiredPermissions`
- **Service:** `CaseWorkflowMacroService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowMacro.cs`

### CaseWorkflowPriorityList

`CaseWorkflowPriorityList` is a **read** operation, idempotent, non-destructive.

Lists the fixed Case priority levels (High, Medium, Low) -- the same for every Case Workflow and every tenant.

- **Owning agent:** `case-workflow-priority` (Case Workflow Priority) in the Case Workflow Governance domain
- **Access:** permission 1
- **Permission source:** `CaseWorkflowPriorityService.RequiredPermissions`
- **Service:** `CaseWorkflowPriorityService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowPriority.cs`

### CaseWorkflowRoleCreate

`CaseWorkflowRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Case Workflow; calling twice creates two grants.

- **Owning agent:** `macro-and-workflow-role-grants` (Macro and Workflow Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowRoleDelete

`CaseWorkflowRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Case Workflow; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `macro-and-workflow-role-grants` (Macro and Workflow Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowRoleListByCaseWorkflowGuid

`CaseWorkflowRoleListByCaseWorkflowGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Case Workflow.

- **Owning agent:** `macro-and-workflow-role-grants` (Macro and Workflow Role Grants) in the Case Workflow Governance
  domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowRole.cs`

### CaseWorkflowStatusCount

`CaseWorkflowStatusCount` is a **read** operation, idempotent, non-destructive.

Counts the Case Workflow Statuses matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `case-workflow-status-inspector-1` (Case Workflow Status Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusCreate

`CaseWorkflowStatusCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Case Workflow Status; calling twice creates two.

- **Owning agent:** `case-workflow-status-author` (Case Workflow Status Author) in the Case Workflow Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowStatusDelete

`CaseWorkflowStatusDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Case Workflow Status; reversible only by direct database action.

- **Owning agent:** `case-workflow-status-author` (Case Workflow Status Author) in the Case Workflow Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowStatusFilter

`CaseWorkflowStatusFilter` is a **read** operation, idempotent, non-destructive.

Returns the Case Workflow Statuses matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `case-workflow-status-inspector-1` (Case Workflow Status Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusFilterFields

`CaseWorkflowStatusFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Case Workflow Statuses may use.

- **Owning agent:** `case-workflow-status-inspector-1` (Case Workflow Status Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusGet

`CaseWorkflowStatusGet` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Status by Id.

- **Owning agent:** `case-workflow-status-inspector-2` (Case Workflow Status Inspector 2) in the Case Workflow
  Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusGetByGuid

`CaseWorkflowStatusGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow Status by Id.

- **Owning agent:** `case-workflow-status-inspector-2` (Case Workflow Status Inspector 2) in the Case Workflow
  Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusList

`CaseWorkflowStatusList` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow Statuses visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-workflow-status-inspector-1` (Case Workflow Status Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusListByWorkflowGuid

`CaseWorkflowStatusListByWorkflowGuid` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow Statuses for a parent Case Workflow (by Guid), including inactive ones.

- **Owning agent:** `case-workflow-status-inspector-1` (Case Workflow Status Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 17, 19, 25, 1
- **Permission source:** `CaseWorkflowStatusService.ListByWorkflowPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusListByWorkflowGuidActiveOnly

`CaseWorkflowStatusListByWorkflowGuidActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Statuses for a parent Case Workflow (by Guid) that the caller holds a role grant on.

- **Owning agent:** `case-workflow-status-inspector-1` (Case Workflow Status Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 19, 1
- **Permission source:** `CaseWorkflowStatusService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusListByWorkflowIdActiveOnly

`CaseWorkflowStatusListByWorkflowIdActiveOnly` is a **read** operation, idempotent, non-destructive.

Lists active Case Workflow Statuses for a parent Case Workflow that the caller holds a role grant on.

- **Owning agent:** `case-workflow-status-inspector-1` (Case Workflow Status Inspector 1) in the Case Workflow
  Governance domain
- **Access:** permission 19, 1
- **Permission source:** `CaseWorkflowStatusService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowStatusRoleCreate

`CaseWorkflowStatusRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Case Workflow Status; calling twice creates two grants.

- **Owning agent:** `status-and-xpath-role-grants` (Status and XPath Role Grants) in the Case Workflow Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatusRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowStatusRoleDelete

`CaseWorkflowStatusRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Case Workflow Status; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `status-and-xpath-role-grants` (Status and XPath Role Grants) in the Case Workflow Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatusRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowStatusRoleListByCaseWorkflowStatusGuid

`CaseWorkflowStatusRoleListByCaseWorkflowStatusGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Case Workflow Status.

- **Owning agent:** `status-and-xpath-role-grants` (Status and XPath Role Grants) in the Case Workflow Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatusRole.cs`

### CaseWorkflowStatusUpdate

`CaseWorkflowStatusUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Case Workflow Status; overwrites, writes a version-audit row.

- **Owning agent:** `case-workflow-status-author` (Case Workflow Status Author) in the Case Workflow Governance domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowStatusValidate

`CaseWorkflowStatusValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Workflow Status without saving it and returns every failure.

- **Owning agent:** `case-workflow-status-test-bench` (Case Workflow Status Test Bench) in the Case Workflow Governance
  domain
- **Access:** permission 19
- **Permission source:** `CaseWorkflowStatusService.RequiredPermissions`
- **Service:** `CaseWorkflowStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowStatus.cs`

### CaseWorkflowUpdate

`CaseWorkflowUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Case Workflow; overwrites, writes a version-audit row.

- **Owning agent:** `case-workflow-author` (Case Workflow Author) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowValidate

`CaseWorkflowValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case Workflow without saving it and returns every failure.

- **Owning agent:** `case-workflow-test-bench` (Case Workflow Test Bench) in the Case Workflow Governance domain
- **Access:** permission 18
- **Permission source:** `CaseWorkflowService.RequiredPermissions`
- **Service:** `CaseWorkflowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflow.cs`

### CaseWorkflowXPathCount

`CaseWorkflowXPathCount` is a **read** operation, idempotent, non-destructive.

Counts the Case WorkflowXPaths matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `case-workflow-x-path-inspector` (Case Workflow X Path Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CaseWorkflowXPathCreate

`CaseWorkflowXPathCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Case Workflow XPath extraction; calling twice creates two.

- **Owning agent:** `case-workflow-x-path-author` (Case Workflow X Path Author) in the Case Workflow Governance domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowXPathDelete

`CaseWorkflowXPathDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Case Workflow XPath extraction; reversible only by direct database action.

- **Owning agent:** `case-workflow-x-path-author` (Case Workflow X Path Author) in the Case Workflow Governance domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowXPathFilter

`CaseWorkflowXPathFilter` is a **read** operation, idempotent, non-destructive.

Returns the Case WorkflowXPaths matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `case-workflow-x-path-inspector` (Case Workflow X Path Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CaseWorkflowXPathFilterFields

`CaseWorkflowXPathFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Case WorkflowXPaths may use.

- **Owning agent:** `case-workflow-x-path-inspector` (Case Workflow X Path Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CaseWorkflowXPathGet

`CaseWorkflowXPathGet` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow XPath extraction by Id.

- **Owning agent:** `case-workflow-x-path-inspector` (Case Workflow X Path Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CaseWorkflowXPathGetByGuid

`CaseWorkflowXPathGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Case Workflow XPath extraction by Id.

- **Owning agent:** `case-workflow-x-path-inspector` (Case Workflow X Path Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CaseWorkflowXPathList

`CaseWorkflowXPathList` is a **read** operation, idempotent, non-destructive.

Lists Case Workflow XPath extractions visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `case-workflow-x-path-inspector` (Case Workflow X Path Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CaseWorkflowXPathListByWorkflowGuidActiveDrillOnly

`CaseWorkflowXPathListByWorkflowGuidActiveDrillOnly` is a **read** operation, idempotent, non-destructive.

Lists active, Drill-enabled Case Workflow XPath extractions for a parent Case Workflow (by Guid) that the caller holds a
role grant on.

- **Owning agent:** `case-workflow-x-path-inspector` (Case Workflow X Path Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 20, 1
- **Permission source:** `CaseWorkflowXPathService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CaseWorkflowXPathListByWorkflowIdActiveDrillOnly

`CaseWorkflowXPathListByWorkflowIdActiveDrillOnly` is a **read** operation, idempotent, non-destructive.

Lists active, Drill-enabled Case Workflow XPath extractions for a parent Case Workflow that the caller holds a role
grant on.

- **Owning agent:** `case-workflow-x-path-inspector` (Case Workflow X Path Inspector) in the Case Workflow Governance
  domain
- **Access:** permission 20, 1
- **Permission source:** `CaseWorkflowXPathService.ActiveOnlyPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CaseWorkflowXPathRoleCreate

`CaseWorkflowXPathRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Case Workflow XPath; calling twice creates two grants.

- **Owning agent:** `status-and-xpath-role-grants` (Status and XPath Role Grants) in the Case Workflow Governance domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPathRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowXPathRoleDelete

`CaseWorkflowXPathRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Case Workflow XPath; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `status-and-xpath-role-grants` (Status and XPath Role Grants) in the Case Workflow Governance domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPathRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowXPathRoleListByCaseWorkflowXPathGuid

`CaseWorkflowXPathRoleListByCaseWorkflowXPathGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Case Workflow XPath.

- **Owning agent:** `status-and-xpath-role-grants` (Status and XPath Role Grants) in the Case Workflow Governance domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathRoleService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPathRole.cs`

### CaseWorkflowXPathUpdate

`CaseWorkflowXPathUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Case Workflow XPath extraction; overwrites, writes a version-audit row.

- **Owning agent:** `case-workflow-x-path-author` (Case Workflow X Path Author) in the Case Workflow Governance domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### CaseWorkflowXPathValidate

`CaseWorkflowXPathValidate` is a **read** operation, idempotent, non-destructive.

Validates a Case WorkflowXPath without saving it and returns every failure.

- **Owning agent:** `case-workflow-x-path-test-bench` (Case Workflow X Path Test Bench) in the Case Workflow Governance
  domain
- **Access:** permission 20
- **Permission source:** `CaseWorkflowXPathService.RequiredPermissions`
- **Service:** `CaseWorkflowXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.CaseWorkflowXPath.cs`

### CompletionsGetByCaseWorkflowGuidIncludingDeleted

`CompletionsGetByCaseWorkflowGuidIncludingDeleted` is a **read** operation, idempotent, non-destructive.

Gets the payload field completions for the model behind a case workflow, by workflow Guid, including deleted workflows.

- **Owning agent:** `completions` (Completions) in the Model Supporting Structures domain
- **Access:** permission 1
- **Permission source:** `CompletionsService.IncludingDeletedPermissions`
- **Service:** `CompletionsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Completions.cs`

### CompletionsGetByCaseWorkflowId

`CompletionsGetByCaseWorkflowId` is a **read** operation, idempotent, non-destructive.

Gets the payload field completions for the model behind a case workflow, by workflow Id.

- **Owning agent:** `completions` (Completions) in the Model Supporting Structures domain
- **Access:** permission 25, 17, 20
- **Permission source:** `CompletionsService.ByCaseWorkflowIdPermissions`
- **Service:** `CompletionsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Completions.cs`

### CompletionsGetByCaseWorkflowIdIncludingDeleted

`CompletionsGetByCaseWorkflowIdIncludingDeleted` is a **read** operation, idempotent, non-destructive.

Gets the payload field completions for the model behind a case workflow, by workflow Id, including deleted workflows.

- **Owning agent:** `completions` (Completions) in the Model Supporting Structures domain
- **Access:** permission 1
- **Permission source:** `CompletionsService.IncludingDeletedPermissions`
- **Service:** `CompletionsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Completions.cs`

### CompletionsGetByEntityAnalysisModelId

`CompletionsGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Gets the payload field completions for an entity analysis model, for reporting.

- **Owning agent:** `completions` (Completions) in the Model Supporting Structures domain
- **Access:** permission 16
- **Permission source:** `CompletionsService.ByEntityAnalysisModelIdPermissions`
- **Service:** `CompletionsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Completions.cs`

### CompletionsGetByEntityAnalysisModelIdParseTypeId

`CompletionsGetByEntityAnalysisModelIdParseTypeId` is a **read** operation, idempotent, non-destructive.

Gets the payload field completions for an entity analysis model and parse type.

- **Owning agent:** `completions` (Completions) in the Model Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 17, 20, 26
- **Permission source:** `CompletionsService.ByParseTypePermissions`
- **Service:** `CompletionsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Completions.cs`

### CompletionsGetByExhaustiveInstanceDataset

`CompletionsGetByExhaustiveInstanceDataset` is a **read** operation, idempotent, non-destructive.

Gets filter builder completions for every column of a confirmed uploaded Exhaustive dataset.

- **Owning agent:** `completions` (Completions) in the Model Supporting Structures domain
- **Access:** permission 16
- **Permission source:** `CompletionsService.ByExhaustiveInstanceDatasetPermissions`
- **Service:** `CompletionsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Completions.cs`

### ContainerLogEntryList

`ContainerLogEntryList` is a **read** operation, idempotent, non-destructive.

Lists every container's own stdout/stderr on the Docker host (no name filtering -- covers etcd/Patroni/Redis/Sentinel
and every other container), most recent first, capped at 100000, with optional date-range and substring search
(ContainerName/Message) filters. Not scoped to any Model or tenant. Also accepts samplePercentage (0-100) to draw an
unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `application-and-container-logs` (Application and Container Logs) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `ContainerLogEntryService.RequiredPermissions`
- **Service:** `ContainerLogEntryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ContainerLogEntry.cs`

### DockerContainerListNow

`DockerContainerListNow` is a **read** operation, idempotent, non-destructive.

Lists every container Docker currently knows about RIGHT NOW (running or not) -- id, name, image, state, status -- not
the background sampler's last-stored row (see DockerContainerMetricList), which can be up to a minute stale. Read-only.

- **Owning agent:** `containers-load-balancer-and-overlay-network` (Containers, Load Balancer and Overlay Network) in
  the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `DockerAdministratorService.RequiredPermissions`
- **Service:** `DockerAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.DockerAdministrator.cs`

### DockerContainerMetricList

`DockerContainerMetricList` is a **read** operation, idempotent, non-destructive.

Lists per-minute, per-container Docker metrics (state, health, restart count, CPU/memory/network/block-io usage, pids)
captured by the Jube.Monitoring sidecar, across every host, most recent first, capped at 100000, with optional
date-range and substring search (Name/Image/State/Status) filters, not scoped to any tenant. Also accepts
samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `containers-load-balancer-and-overlay-network` (Containers, Load Balancer and Overlay Network) in
  the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `DockerContainerMetricService.RequiredPermissions`
- **Service:** `DockerContainerMetricService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.DockerContainerMetric.cs`

### DockerEventList

`DockerEventList` is a **read** operation, idempotent, non-destructive.

Lists Docker Engine lifecycle events on the host (container die/kill/oom/restart/health_status, network/volume/image
events, docker exec audit trail), most recent first, capped at 100000, with optional date-range and substring search
(EventType/Action/ActorName) filters. Not scoped to any Model or tenant. Also accepts samplePercentage (0-100) to draw
an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `containers-load-balancer-and-overlay-network` (Containers, Load Balancer and Overlay Network) in
  the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `DockerEventService.RequiredPermissions`
- **Service:** `DockerEventService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.DockerEvent.cs`

### DockerHostInfoNow

`DockerHostInfoNow` is a **read** operation, idempotent, non-destructive.

Reads the Docker host's own live info/version -- container/image counts, CPU, memory, kernel, OS, Docker/API version --
RIGHT NOW, not the background sampler's last-stored row (see DockerHostMetricList). Read-only.

- **Owning agent:** `containers-load-balancer-and-overlay-network` (Containers, Load Balancer and Overlay Network) in
  the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `DockerAdministratorService.RequiredPermissions`
- **Service:** `DockerAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.DockerAdministrator.cs`

### DockerHostMetricList

`DockerHostMetricList` is a **read** operation, idempotent, non-destructive.

Lists per-minute, per-host Docker host metrics (container/image counts, daemon version,
image/layer/container/volume/build-cache disk usage) captured by the Jube.Monitoring sidecar, across every host, most
recent first, capped at 100000, with optional date-range and substring search (Instance/OperatingSystem) filters, not
scoped to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead
of the most recent ones.

- **Owning agent:** `containers-load-balancer-and-overlay-network` (Containers, Load Balancer and Overlay Network) in
  the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `DockerHostMetricService.RequiredPermissions`
- **Service:** `DockerHostMetricService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.DockerHostMetric.cs`

### DotNetRuntimeMetricList

`DotNetRuntimeMetricList` is a **read** operation, idempotent, non-destructive.

Lists per-minute .NET runtime metric snapshots across every node, most recent first, capped at 100000, with optional
date-range and substring search (Instance) filters, not scoped to any tenant. Also accepts samplePercentage (0-100) to
draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `telemetry-log-counters-and-runtime-metrics` (Telemetry Log Counters and Runtime Metrics) in the
  Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `DotNetRuntimeMetricService.RequiredPermissions`
- **Service:** `DotNetRuntimeMetricService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.DotNetRuntimeMetric.cs`

### EntityAnalysisAsynchronousQueueBalanceList

`EntityAnalysisAsynchronousQueueBalanceList` is a **read** operation, idempotent, non-destructive.

Lists platform-wide asynchronous processing queue balance rows, most recent first, capped at 100000, with optional
date-range filters. Each row is a roughly one-minute interval's queue depth for asynchronous CaseCreation, Tagging and
Notification dispatch. Not scoped to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset
of matching rows instead of the most recent ones.

- **Owning agent:** `engine-queue-and-throughput-counters` (Engine Queue and Throughput Counters) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EntityAnalysisAsynchronousQueueBalanceService.RequiredPermissions`
- **Service:** `EntityAnalysisAsynchronousQueueBalanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisAsynchronousQueueBalance.cs`

### EntityAnalysisInlineScriptCount

`EntityAnalysisInlineScriptCount` is a **read** operation, idempotent, non-destructive.

Counts the Entity Analysis Inline Scripts matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-observability` (Entity Analysis Observability) in the Detection Model Authoring
  domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisInlineScriptService.ListPermissions`
- **Service:** `EntityAnalysisInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisInlineScript.cs`

### EntityAnalysisInlineScriptFilter

`EntityAnalysisInlineScriptFilter` is a **read** operation, idempotent, non-destructive.

Returns the Entity Analysis Inline Scripts matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-observability` (Entity Analysis Observability) in the Detection Model Authoring
  domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisInlineScriptService.ListPermissions`
- **Service:** `EntityAnalysisInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisInlineScript.cs`

### EntityAnalysisInlineScriptFilterFields

`EntityAnalysisInlineScriptFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Entity Analysis Inline Scripts may use.

- **Owning agent:** `entity-analysis-observability` (Entity Analysis Observability) in the Detection Model Authoring
  domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisInlineScriptService.ListPermissions`
- **Service:** `EntityAnalysisInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisInlineScript.cs`

### EntityAnalysisInlineScriptList

`EntityAnalysisInlineScriptList` is a **read** operation, idempotent, non-destructive.

Lists registered Inline Scripts from the system-wide catalogue, keyset-paged and capped. Not tenant-scoped.

- **Owning agent:** `entity-analysis-observability` (Entity Analysis Observability) in the Detection Model Authoring
  domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisInlineScriptService.ListPermissions`
- **Service:** `EntityAnalysisInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisInlineScript.cs`

### EntityAnalysisModelAbstractionCalculationCount

`EntityAnalysisModelAbstractionCalculationCount` is a **read** operation, idempotent, non-destructive.

Counts the Abstraction Calculations matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-inspector` (Entity Analysis Model Abstraction
  Calculation Inspector) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.ListPermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationCreate

`EntityAnalysisModelAbstractionCalculationCreate` is a **write** operation, not idempotent, non-destructive.

Registers an Abstraction Calculation under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-author` (Entity Analysis Model Abstraction
  Calculation Author) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionCalculationDelete

`EntityAnalysisModelAbstractionCalculationDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an Abstraction Calculation in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-author` (Entity Analysis Model Abstraction
  Calculation Author) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionCalculationExecute

`EntityAnalysisModelAbstractionCalculationExecute` is a **read** operation, idempotent, non-destructive.

Runs an Abstraction Calculation against an invocation context as the engine would and returns the result.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-test-bench` (Entity Analysis Model Abstraction
  Calculation Test Bench) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationFilter

`EntityAnalysisModelAbstractionCalculationFilter` is a **read** operation, idempotent, non-destructive.

Returns the Abstraction Calculations matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-inspector` (Entity Analysis Model Abstraction
  Calculation Inspector) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.ListPermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationFilterFields

`EntityAnalysisModelAbstractionCalculationFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Abstraction Calculations may use.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-inspector` (Entity Analysis Model Abstraction
  Calculation Inspector) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.ListPermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationGet

`EntityAnalysisModelAbstractionCalculationGet` is a **read** operation, idempotent, non-destructive.

Returns one Abstraction Calculation by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-inspector` (Entity Analysis Model Abstraction
  Calculation Inspector) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationGetByEntityAnalysisModelId

`EntityAnalysisModelAbstractionCalculationGetByEntityAnalysisModelId` is a **read** operation, idempotent,
non-destructive.

Lists Abstraction Calculations belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-inspector` (Entity Analysis Model Abstraction
  Calculation Inspector) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationGetByGuid

`EntityAnalysisModelAbstractionCalculationGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Abstraction Calculation by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-inspector` (Entity Analysis Model Abstraction
  Calculation Inspector) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationList

`EntityAnalysisModelAbstractionCalculationList` is a **read** operation, idempotent, non-destructive.

Lists Abstraction Calculations for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-inspector` (Entity Analysis Model Abstraction
  Calculation Inspector) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.ListPermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationParseRule

`EntityAnalysisModelAbstractionCalculationParseRule` is a **read** operation, idempotent, non-destructive.

Parses and compiles an Abstraction Calculation's rule text as the engine would and returns each located error.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-test-bench` (Entity Analysis Model Abstraction
  Calculation Test Bench) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionCalculationUpdate

`EntityAnalysisModelAbstractionCalculationUpdate` is a **write** operation, idempotent, non-destructive.

Updates an Abstraction Calculation in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-author` (Entity Analysis Model Abstraction
  Calculation Author) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionCalculationValidate

`EntityAnalysisModelAbstractionCalculationValidate` is a **read** operation, idempotent, non-destructive.

Validates an Abstraction Calculation without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-test-bench` (Entity Analysis Model Abstraction
  Calculation Test Bench) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`

### EntityAnalysisModelAbstractionRuleBuildRuleFromBuilderJson

`EntityAnalysisModelAbstractionRuleBuildRuleFromBuilderJson` is a **read** operation, idempotent, non-destructive.

Turns query builder JSON into an Abstraction Rule's rule text as the browser builder would, and parses it.

- **Owning agent:** `entity-analysis-model-abstraction-rule-test-bench` (Entity Analysis Model Abstraction Rule Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleCount

`EntityAnalysisModelAbstractionRuleCount` is a **read** operation, idempotent, non-destructive.

Counts the Abstraction Rules matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-abstraction-rule-inspector` (Entity Analysis Model Abstraction Rule
  Inspector) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleCreate

`EntityAnalysisModelAbstractionRuleCreate` is a **write** operation, not idempotent, non-destructive.

Registers an Abstraction Rule under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-abstraction-rule-author` (Entity Analysis Model Abstraction Rule Author) in
  the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionRuleDelete

`EntityAnalysisModelAbstractionRuleDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an Abstraction Rule in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-abstraction-rule-author` (Entity Analysis Model Abstraction Rule Author) in
  the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionRuleExecute

`EntityAnalysisModelAbstractionRuleExecute` is a **read** operation, idempotent, non-destructive.

Runs an Abstraction Rule against an invocation context as the engine would and returns the result.

- **Owning agent:** `entity-analysis-model-abstraction-rule-test-bench` (Entity Analysis Model Abstraction Rule Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleFilter

`EntityAnalysisModelAbstractionRuleFilter` is a **read** operation, idempotent, non-destructive.

Returns the Abstraction Rules matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-abstraction-rule-inspector` (Entity Analysis Model Abstraction Rule
  Inspector) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleFilterFields

`EntityAnalysisModelAbstractionRuleFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Abstraction Rules may use.

- **Owning agent:** `entity-analysis-model-abstraction-rule-inspector` (Entity Analysis Model Abstraction Rule
  Inspector) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleGet

`EntityAnalysisModelAbstractionRuleGet` is a **read** operation, idempotent, non-destructive.

Returns one Abstraction Rule by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-abstraction-rule-inspector` (Entity Analysis Model Abstraction Rule
  Inspector) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId

`EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Abstraction Rules belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-abstraction-rule-inspector` (Entity Analysis Model Abstraction Rule
  Inspector) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleGetByGuid

`EntityAnalysisModelAbstractionRuleGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Abstraction Rule by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-abstraction-rule-inspector` (Entity Analysis Model Abstraction Rule
  Inspector) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleList

`EntityAnalysisModelAbstractionRuleList` is a **read** operation, idempotent, non-destructive.

Lists Abstraction Rules for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-abstraction-rule-inspector` (Entity Analysis Model Abstraction Rule
  Inspector) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleParseRule

`EntityAnalysisModelAbstractionRuleParseRule` is a **read** operation, idempotent, non-destructive.

Parses and compiles an Abstraction Rule's rule text as the engine would and returns each located error.

- **Owning agent:** `entity-analysis-model-abstraction-rule-test-bench` (Entity Analysis Model Abstraction Rule Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelAbstractionRuleUpdate

`EntityAnalysisModelAbstractionRuleUpdate` is a **write** operation, idempotent, non-destructive.

Updates an Abstraction Rule in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-abstraction-rule-author` (Entity Analysis Model Abstraction Rule Author) in
  the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionRuleValidate

`EntityAnalysisModelAbstractionRuleValidate` is a **read** operation, idempotent, non-destructive.

Validates an Abstraction Rule without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-abstraction-rule-test-bench` (Entity Analysis Model Abstraction Rule Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`

### EntityAnalysisModelActivationRuleBuildRuleFromBuilderJson

`EntityAnalysisModelActivationRuleBuildRuleFromBuilderJson` is a **read** operation, idempotent, non-destructive.

Turns query builder JSON into an Activation Rule's rule text as the browser builder would, and parses it.

- **Owning agent:** `entity-analysis-model-activation-rule-test-bench` (Entity Analysis Model Activation Rule Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleCount

`EntityAnalysisModelActivationRuleCount` is a **read** operation, idempotent, non-destructive.

Counts the Activation Rules matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-activation-rule-inspector` (Entity Analysis Model Activation Rule Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleCreate

`EntityAnalysisModelActivationRuleCreate` is a **write** operation, not idempotent, non-destructive.

Registers an Activation Rule under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-activation-rule-author` (Entity Analysis Model Activation Rule Author) in the
  Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleDelete

`EntityAnalysisModelActivationRuleDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an Activation Rule in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-activation-rule-author` (Entity Analysis Model Activation Rule Author) in the
  Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleExecute

`EntityAnalysisModelActivationRuleExecute` is a **read** operation, idempotent, non-destructive.

Runs an Activation Rule against an invocation context as the engine would and returns the result.

- **Owning agent:** `entity-analysis-model-activation-rule-test-bench` (Entity Analysis Model Activation Rule Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleFilter

`EntityAnalysisModelActivationRuleFilter` is a **read** operation, idempotent, non-destructive.

Returns the Activation Rules matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-activation-rule-inspector` (Entity Analysis Model Activation Rule Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleFilterFields

`EntityAnalysisModelActivationRuleFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Activation Rules may use.

- **Owning agent:** `entity-analysis-model-activation-rule-inspector` (Entity Analysis Model Activation Rule Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleGet

`EntityAnalysisModelActivationRuleGet` is a **read** operation, idempotent, non-destructive.

Returns one Activation Rule by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-activation-rule-inspector` (Entity Analysis Model Activation Rule Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId

`EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Activation Rules belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-activation-rule-inspector` (Entity Analysis Model Activation Rule Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleGetByGuid

`EntityAnalysisModelActivationRuleGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Activation Rule by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-activation-rule-inspector` (Entity Analysis Model Activation Rule Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleList

`EntityAnalysisModelActivationRuleList` is a **read** operation, idempotent, non-destructive.

Lists Activation Rules for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-activation-rule-inspector` (Entity Analysis Model Activation Rule Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleOverrideCount

`EntityAnalysisModelActivationRuleOverrideCount` is a **read** operation, idempotent, non-destructive.

Counts the Activation Rule Overrides matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-activation-rule-override-inspector` (Entity Analysis Model Activation Rule
  Override Inspector) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`

### EntityAnalysisModelActivationRuleOverrideCreate

`EntityAnalysisModelActivationRuleOverrideCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Override against a Model/Activation Rule in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-activation-rule-override-author` (Entity Analysis Model Activation Rule
  Override Author) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleOverrideDelete

`EntityAnalysisModelActivationRuleOverrideDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Override in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-activation-rule-override-author` (Entity Analysis Model Activation Rule
  Override Author) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleOverrideFilter

`EntityAnalysisModelActivationRuleOverrideFilter` is a **read** operation, idempotent, non-destructive.

Returns the Activation Rule Overrides matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-activation-rule-override-inspector` (Entity Analysis Model Activation Rule
  Override Inspector) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`

### EntityAnalysisModelActivationRuleOverrideFilterFields

`EntityAnalysisModelActivationRuleOverrideFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Activation Rule Overrides may use.

- **Owning agent:** `entity-analysis-model-activation-rule-override-inspector` (Entity Analysis Model Activation Rule
  Override Inspector) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`

### EntityAnalysisModelActivationRuleOverrideGet

`EntityAnalysisModelActivationRuleOverrideGet` is a **read** operation, idempotent, non-destructive.

Returns one Activation-Rule-scoped Override by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-activation-rule-override-inspector` (Entity Analysis Model Activation Rule
  Override Inspector) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`

### EntityAnalysisModelActivationRuleOverrideGetByEntityAnalysisModelGuid

`EntityAnalysisModelActivationRuleOverrideGetByEntityAnalysisModelGuid` is a **read** operation, idempotent,
non-destructive.

Lists Activation-Rule-scoped Overrides belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-activation-rule-override-inspector` (Entity Analysis Model Activation Rule
  Override Inspector) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`

### EntityAnalysisModelActivationRuleOverrideList

`EntityAnalysisModelActivationRuleOverrideList` is a **read** operation, idempotent, non-destructive.

Lists Activation-Rule-scoped Overrides for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-activation-rule-override-inspector` (Entity Analysis Model Activation Rule
  Override Inspector) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`

### EntityAnalysisModelActivationRuleOverrideQueryGet

`EntityAnalysisModelActivationRuleOverrideQueryGet` is a **read** operation, idempotent, non-destructive.

Lists the override-enabled Activation Rules of an Entity Analysis Model and whether each is currently overridden for a
override key and value.

- **Owning agent:** `entity-analysis-model-activation-rule-override-query` (Entity Analysis Model Activation Rule
  Override Query) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideQueryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideQueryService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverrideQuery.cs`

### EntityAnalysisModelActivationRuleOverrideUpdate

`EntityAnalysisModelActivationRuleOverrideUpdate` is a **write** operation, not idempotent, non-destructive.

Toggles a Override on or off by row existence; NOT idempotent -- repeat calls flip state.

- **Owning agent:** `entity-analysis-model-activation-rule-override-author` (Entity Analysis Model Activation Rule
  Override Author) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleOverrideUpdateDeleteExpiryDate

`EntityAnalysisModelActivationRuleOverrideUpdateDeleteExpiryDate` is a **write** operation, idempotent, non-destructive.

Updates the Delete Expiry Date of an existing active Override in the caller's tenant.

- **Owning agent:** `entity-analysis-model-activation-rule-override-author` (Entity Analysis Model Activation Rule
  Override Author) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleOverrideUpdateOverrideKind

`EntityAnalysisModelActivationRuleOverrideUpdateOverrideKind` is a **write** operation, idempotent, non-destructive.

Updates the Override Kind of an active Activation Rule Override in the caller's tenant.

- **Owning agent:** `entity-analysis-model-activation-rule-override-author` (Entity Analysis Model Activation Rule
  Override Author) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleOverrideValidate

`EntityAnalysisModelActivationRuleOverrideValidate` is a **read** operation, idempotent, non-destructive.

Validates an Activation Rule Override without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-activation-rule-override-test-bench` (Entity Analysis Model Activation Rule
  Override Test Bench) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelActivationRuleOverrideService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRuleOverride.cs`

### EntityAnalysisModelActivationRuleParseRule

`EntityAnalysisModelActivationRuleParseRule` is a **read** operation, idempotent, non-destructive.

Parses and compiles an Activation Rule's rule text as the engine would and returns each located error.

- **Owning agent:** `entity-analysis-model-activation-rule-test-bench` (Entity Analysis Model Activation Rule Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelActivationRuleReset

`EntityAnalysisModelActivationRuleReset` is a **write** operation, idempotent, destructive.

Resets the ActivationCounter and EvaluationCounter of an Activation Rule to zero.

- **Owning agent:** `entity-analysis-model-activation-rule-author` (Entity Analysis Model Activation Rule Author) in the
  Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleUpdate

`EntityAnalysisModelActivationRuleUpdate` is a **write** operation, idempotent, non-destructive.

Updates an Activation Rule in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-activation-rule-author` (Entity Analysis Model Activation Rule Author) in the
  Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleValidate

`EntityAnalysisModelActivationRuleValidate` is a **read** operation, idempotent, non-destructive.

Validates an Activation Rule without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-activation-rule-test-bench` (Entity Analysis Model Activation Rule Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`

### EntityAnalysisModelAsynchronousQueueBalanceList

`EntityAnalysisModelAsynchronousQueueBalanceList` is a **read** operation, idempotent, non-destructive.

Lists per-Model asynchronous processing queue balance rows, most recent first, capped at 100000, with optional
date-range and Model-name substring filters. Each row is a roughly one-minute interval's queue depth for asynchronous
archiving and activation-watcher dispatch for one Entity Analysis Model. Landlord only: every other caller is refused
with a 403. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most
recent ones.

- **Owning agent:** `engine-queue-and-throughput-counters` (Engine Queue and Throughput Counters) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EntityAnalysisModelAsynchronousQueueBalanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelAsynchronousQueueBalanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAsynchronousQueueBalance.cs`

### EntityAnalysisModelBacktestExplain

`EntityAnalysisModelBacktestExplain` is a **read** operation, idempotent, non-destructive.

Explains why one archived transaction was or was not counted, with the values read.

- **Owning agent:** `entity-analysis-model-backtest-test-bench` (Entity Analysis Model Backtest Test Bench) in the Model
  Risk and Analytics domain
- **Access:** permission 61
- **Permission source:** `EntityAnalysisModelBacktestService.RequiredPermissions`
- **Service:** `EntityAnalysisModelBacktestService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelBacktest.cs`

### EntityAnalysisModelBacktestFields

`EntityAnalysisModelBacktestFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a backtest's filter and class may use, including Tag.<Name> for the class.

- **Owning agent:** `entity-analysis-model-backtest-inspector` (Entity Analysis Model Backtest Inspector) in the Model
  Risk and Analytics domain
- **Access:** permission 61
- **Permission source:** `EntityAnalysisModelBacktestService.RequiredPermissions`
- **Service:** `EntityAnalysisModelBacktestService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelBacktest.cs`

### EntityAnalysisModelBacktestList

`EntityAnalysisModelBacktestList` is a **read** operation, idempotent, non-destructive.

Lists a model's submitted backtests, optionally for one rule.

- **Owning agent:** `entity-analysis-model-backtest-inspector` (Entity Analysis Model Backtest Inspector) in the Model
  Risk and Analytics domain
- **Access:** permission 61
- **Permission source:** `EntityAnalysisModelBacktestService.RequiredPermissions`
- **Service:** `EntityAnalysisModelBacktestService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelBacktest.cs`

### EntityAnalysisModelBacktestResult

`EntityAnalysisModelBacktestResult` is a **read** operation, idempotent, non-destructive.

Returns a finished backtest's counts and examples.

- **Owning agent:** `entity-analysis-model-backtest-inspector` (Entity Analysis Model Backtest Inspector) in the Model
  Risk and Analytics domain
- **Access:** permission 61
- **Permission source:** `EntityAnalysisModelBacktestService.RequiredPermissions`
- **Service:** `EntityAnalysisModelBacktestService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelBacktest.cs`

### EntityAnalysisModelBacktestRunRule

`EntityAnalysisModelBacktestRunRule` is a **read** operation, idempotent, non-destructive.

Backtests a rule immediately over a filtered archive against a class; returns raw counts.

- **Owning agent:** `entity-analysis-model-backtest-inspector` (Entity Analysis Model Backtest Inspector) in the Model
  Risk and Analytics domain
- **Access:** permission 61
- **Permission source:** `EntityAnalysisModelBacktestService.RequiredPermissions`
- **Service:** `EntityAnalysisModelBacktestService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelBacktest.cs`

### EntityAnalysisModelBacktestStatus

`EntityAnalysisModelBacktestStatus` is a **read** operation, idempotent, non-destructive.

Returns a submitted backtest's status, progress and headline counts.

- **Owning agent:** `entity-analysis-model-backtest-inspector` (Entity Analysis Model Backtest Inspector) in the Model
  Risk and Analytics domain
- **Access:** permission 61
- **Permission source:** `EntityAnalysisModelBacktestService.RequiredPermissions`
- **Service:** `EntityAnalysisModelBacktestService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelBacktest.cs`

### EntityAnalysisModelBacktestStop

`EntityAnalysisModelBacktestStop` is a **write** operation, idempotent, non-destructive.

Stops a submitted backtest.

- **Owning agent:** `entity-analysis-model-backtest-author` (Entity Analysis Model Backtest Author) in the Model Risk
  and Analytics domain
- **Access:** permission 61
- **Permission source:** `EntityAnalysisModelBacktestService.RequiredPermissions`
- **Service:** `EntityAnalysisModelBacktestService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelBacktest.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelBacktestSubmit

`EntityAnalysisModelBacktestSubmit` is a **write** operation, not idempotent, non-destructive.

Submits a large backtest to the engine's backtest threads; returns the run to poll.

- **Owning agent:** `entity-analysis-model-backtest-author` (Entity Analysis Model Backtest Author) in the Model Risk
  and Analytics domain
- **Access:** permission 61
- **Permission source:** `EntityAnalysisModelBacktestService.RequiredPermissions`
- **Service:** `EntityAnalysisModelBacktestService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelBacktest.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelCount

`EntityAnalysisModelCount` is a **read** operation, idempotent, non-destructive.

Counts the Entity Analysis Models matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-inspector` (Entity Analysis Model Inspector) in the Detection Model Authoring
  domain
- **Access:** permission 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 37, 3, 4, 1, 40
- **Permission source:** `EntityAnalysisModelService.ListPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`

### EntityAnalysisModelCreate

`EntityAnalysisModelCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-author` (Entity Analysis Model Author) in the Detection Model Authoring
  domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelService.RequiredPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDelete

`EntityAnalysisModelDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Model in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-author` (Entity Analysis Model Author) in the Detection Model Authoring
  domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelService.RequiredPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDependencyDependencies

`EntityAnalysisModelDependencyDependencies` is a **read** operation, idempotent, non-destructive.

Lists what an entity uses, resolved to the entities it names.

- **Owning agent:** `model-invocation-context-and-dependencies` (Model Invocation Context and Dependencies) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelDependencyService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDependencyService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDependency.cs`

### EntityAnalysisModelDependencyDependents

`EntityAnalysisModelDependencyDependents` is a **read** operation, idempotent, non-destructive.

Lists every entity that uses an entity, directly or indirectly, and whether deleting it would be refused.

- **Owning agent:** `model-invocation-context-and-dependencies` (Model Invocation Context and Dependencies) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelDependencyService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDependencyService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDependency.cs`

### EntityAnalysisModelDependencyIssues

`EntityAnalysisModelDependencyIssues` is a **read** operation, idempotent, non-destructive.

Finds dangling names, uses of inactive entities and unused entities in a model.

- **Owning agent:** `model-invocation-context-and-dependencies` (Model Invocation Context and Dependencies) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelDependencyService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDependencyService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDependency.cs`

### EntityAnalysisModelDictionaryCount

`EntityAnalysisModelDictionaryCount` is a **read** operation, idempotent, non-destructive.

Counts the Dictionarys matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-dictionary-inspector` (Entity Analysis Model Dictionary Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`

### EntityAnalysisModelDictionaryCreate

`EntityAnalysisModelDictionaryCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Dictionary under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-dictionary-author` (Entity Analysis Model Dictionary Author) in the Model
  Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryDelete

`EntityAnalysisModelDictionaryDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Dictionary in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-dictionary-author` (Entity Analysis Model Dictionary Author) in the Model
  Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryFilter

`EntityAnalysisModelDictionaryFilter` is a **read** operation, idempotent, non-destructive.

Returns the Dictionarys matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-dictionary-inspector` (Entity Analysis Model Dictionary Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`

### EntityAnalysisModelDictionaryFilterFields

`EntityAnalysisModelDictionaryFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Dictionarys may use.

- **Owning agent:** `entity-analysis-model-dictionary-inspector` (Entity Analysis Model Dictionary Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`

### EntityAnalysisModelDictionaryGet

`EntityAnalysisModelDictionaryGet` is a **read** operation, idempotent, non-destructive.

Returns one Dictionary by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-dictionary-inspector` (Entity Analysis Model Dictionary Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`

### EntityAnalysisModelDictionaryGetByEntityAnalysisModelId

`EntityAnalysisModelDictionaryGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists the Dictionaries belonging to a given Model, ordered by Id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-dictionary-inspector` (Entity Analysis Model Dictionary Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`

### EntityAnalysisModelDictionaryGetByGuid

`EntityAnalysisModelDictionaryGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Dictionary by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-dictionary-inspector` (Entity Analysis Model Dictionary Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`

### EntityAnalysisModelDictionaryKvpCount

`EntityAnalysisModelDictionaryKvpCount` is a **read** operation, idempotent, non-destructive.

Counts the Dictionary KVPs matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-inspector` (Entity Analysis Model Dictionary Kvp Inspector) in
  the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`

### EntityAnalysisModelDictionaryKvpCreate

`EntityAnalysisModelDictionaryKvpCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Key Value Pair under a Dictionary in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-author` (Entity Analysis Model Dictionary Kvp Author) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryKvpDelete

`EntityAnalysisModelDictionaryKvpDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Key Value Pair in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-author` (Entity Analysis Model Dictionary Kvp Author) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryKvpFilter

`EntityAnalysisModelDictionaryKvpFilter` is a **read** operation, idempotent, non-destructive.

Returns the Dictionary KVPs matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-inspector` (Entity Analysis Model Dictionary Kvp Inspector) in
  the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`

### EntityAnalysisModelDictionaryKvpFilterFields

`EntityAnalysisModelDictionaryKvpFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Dictionary KVPs may use.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-inspector` (Entity Analysis Model Dictionary Kvp Inspector) in
  the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`

### EntityAnalysisModelDictionaryKvpGet

`EntityAnalysisModelDictionaryKvpGet` is a **read** operation, idempotent, non-destructive.

Returns one Key Value Pair, matched on the parent Dictionary's id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-inspector` (Entity Analysis Model Dictionary Kvp Inspector) in
  the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`

### EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId

`EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId` is a **read** operation, idempotent,
non-destructive.

Lists the Key Value Pairs belonging to a given Dictionary, ordered by Id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-inspector` (Entity Analysis Model Dictionary Kvp Inspector) in
  the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`

### EntityAnalysisModelDictionaryKvpGetByGuid

`EntityAnalysisModelDictionaryKvpGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Key Value Pair, matched on the parent Dictionary's id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-inspector` (Entity Analysis Model Dictionary Kvp Inspector) in
  the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`

### EntityAnalysisModelDictionaryKvpList

`EntityAnalysisModelDictionaryKvpList` is a **read** operation, idempotent, non-destructive.

Lists Key Value Pairs for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-inspector` (Entity Analysis Model Dictionary Kvp Inspector) in
  the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`

### EntityAnalysisModelDictionaryKvpUpdate

`EntityAnalysisModelDictionaryKvpUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Key Value Pair in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-author` (Entity Analysis Model Dictionary Kvp Author) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryKvpUploadCsv

`EntityAnalysisModelDictionaryKvpUploadCsv` is a **write** operation, not idempotent, non-destructive.

Imports CSV files of key,value[,deleteExpiryDate] lines into a Dictionary in the caller's tenant; existing keys are
updated, lines that fail are skipped, and one upload record is written per file.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-author` (Entity Analysis Model Dictionary Kvp Author) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryKvpValidate

`EntityAnalysisModelDictionaryKvpValidate` is a **read** operation, idempotent, non-destructive.

Validates a Dictionary KVP without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-test-bench` (Entity Analysis Model Dictionary Kvp Test Bench)
  in the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`

### EntityAnalysisModelDictionaryList

`EntityAnalysisModelDictionaryList` is a **read** operation, idempotent, non-destructive.

Lists Dictionaries for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-dictionary-inspector` (Entity Analysis Model Dictionary Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`

### EntityAnalysisModelDictionaryUpdate

`EntityAnalysisModelDictionaryUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Dictionary in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-dictionary-author` (Entity Analysis Model Dictionary Author) in the Model
  Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryValidate

`EntityAnalysisModelDictionaryValidate` is a **read** operation, idempotent, non-destructive.

Validates a Dictionary without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-dictionary-test-bench` (Entity Analysis Model Dictionary Test Bench) in the
  Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`

### EntityAnalysisModelFilter

`EntityAnalysisModelFilter` is a **read** operation, idempotent, non-destructive.

Returns the Entity Analysis Models matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-inspector` (Entity Analysis Model Inspector) in the Detection Model Authoring
  domain
- **Access:** permission 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 37, 3, 4, 1, 40
- **Permission source:** `EntityAnalysisModelService.ListPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`

### EntityAnalysisModelFilterFields

`EntityAnalysisModelFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Entity Analysis Models may use.

- **Owning agent:** `entity-analysis-model-inspector` (Entity Analysis Model Inspector) in the Detection Model Authoring
  domain
- **Access:** permission 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 37, 3, 4, 1, 40
- **Permission source:** `EntityAnalysisModelService.ListPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`

### EntityAnalysisModelGatewayRuleBuildRuleFromBuilderJson

`EntityAnalysisModelGatewayRuleBuildRuleFromBuilderJson` is a **read** operation, idempotent, non-destructive.

Turns query builder JSON into a Gateway Rule's rule text as the browser builder would, and parses it.

- **Owning agent:** `entity-analysis-model-gateway-rule-test-bench` (Entity Analysis Model Gateway Rule Test Bench) in
  the Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleCount

`EntityAnalysisModelGatewayRuleCount` is a **read** operation, idempotent, non-destructive.

Counts the Gateway Rules matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-gateway-rule-inspector` (Entity Analysis Model Gateway Rule Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleCreate

`EntityAnalysisModelGatewayRuleCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Gateway Rule under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-gateway-rule-author` (Entity Analysis Model Gateway Rule Author) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelGatewayRuleDelete

`EntityAnalysisModelGatewayRuleDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Gateway Rule in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-gateway-rule-author` (Entity Analysis Model Gateway Rule Author) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelGatewayRuleExecute

`EntityAnalysisModelGatewayRuleExecute` is a **read** operation, idempotent, non-destructive.

Runs a Gateway Rule against an invocation context as the engine would and returns the result.

- **Owning agent:** `entity-analysis-model-gateway-rule-test-bench` (Entity Analysis Model Gateway Rule Test Bench) in
  the Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleFilter

`EntityAnalysisModelGatewayRuleFilter` is a **read** operation, idempotent, non-destructive.

Returns the Gateway Rules matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-gateway-rule-inspector` (Entity Analysis Model Gateway Rule Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleFilterFields

`EntityAnalysisModelGatewayRuleFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Gateway Rules may use.

- **Owning agent:** `entity-analysis-model-gateway-rule-inspector` (Entity Analysis Model Gateway Rule Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleGet

`EntityAnalysisModelGatewayRuleGet` is a **read** operation, idempotent, non-destructive.

Returns one Gateway Rule by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-gateway-rule-inspector` (Entity Analysis Model Gateway Rule Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleGetByEntityAnalysisModelId

`EntityAnalysisModelGatewayRuleGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Gateway Rules belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-gateway-rule-inspector` (Entity Analysis Model Gateway Rule Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleGetByGuid

`EntityAnalysisModelGatewayRuleGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Gateway Rule by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-gateway-rule-inspector` (Entity Analysis Model Gateway Rule Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.ReadPermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleList

`EntityAnalysisModelGatewayRuleList` is a **read** operation, idempotent, non-destructive.

Lists Gateway Rules for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-gateway-rule-inspector` (Entity Analysis Model Gateway Rule Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.ListPermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleParseRule

`EntityAnalysisModelGatewayRuleParseRule` is a **read** operation, idempotent, non-destructive.

Parses and compiles a Gateway Rule's rule text as the engine would and returns each located error.

- **Owning agent:** `entity-analysis-model-gateway-rule-test-bench` (Entity Analysis Model Gateway Rule Test Bench) in
  the Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGatewayRuleResetCounter

`EntityAnalysisModelGatewayRuleResetCounter` is a **write** operation, idempotent, destructive.

Resets the activation and evaluation counters for a Gateway Rule in the caller's tenant to zero.

- **Owning agent:** `entity-analysis-model-gateway-rule-author` (Entity Analysis Model Gateway Rule Author) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelGatewayRuleUpdate

`EntityAnalysisModelGatewayRuleUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Gateway Rule in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-gateway-rule-author` (Entity Analysis Model Gateway Rule Author) in the
  Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelGatewayRuleValidate

`EntityAnalysisModelGatewayRuleValidate` is a **read** operation, idempotent, non-destructive.

Validates a Gateway Rule without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-gateway-rule-test-bench` (Entity Analysis Model Gateway Rule Test Bench) in
  the Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`

### EntityAnalysisModelGet

`EntityAnalysisModelGet` is a **read** operation, idempotent, non-destructive.

Returns one Model by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-inspector` (Entity Analysis Model Inspector) in the Detection Model Authoring
  domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelService.RequiredPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`

### EntityAnalysisModelHistorySimulationAbstractionAggregation

`EntityAnalysisModelHistorySimulationAbstractionAggregation` is a **read** operation, idempotent, non-destructive.

Works out an abstraction rule's value for a transaction from cached history, as the engine does.

- **Owning agent:** `model-integrity-and-sampling` (Model Integrity and Sampling) in the Model Risk and Analytics domain
- **Access:** permission 1, 40
- **Permission source:** `EntityAnalysisModelHistorySimulationService.RequiredPermissions`
- **Service:** `EntityAnalysisModelHistorySimulationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHistorySimulation.cs`

### EntityAnalysisModelHistorySimulationTtlCounterValue

`EntityAnalysisModelHistorySimulationTtlCounterValue` is a **read** operation, idempotent, non-destructive.

Reads a TTL counter's value for a transaction from the cache, as the engine does; increments nothing.

- **Owning agent:** `model-integrity-and-sampling` (Model Integrity and Sampling) in the Model Risk and Analytics domain
- **Access:** permission 1, 40
- **Permission source:** `EntityAnalysisModelHistorySimulationService.RequiredPermissions`
- **Service:** `EntityAnalysisModelHistorySimulationService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHistorySimulation.cs`

### EntityAnalysisModelHttpAdaptationCount

`EntityAnalysisModelHttpAdaptationCount` is a **read** operation, idempotent, non-destructive.

Counts the HTTP Adaptations matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-http-adaptation-inspector` (Entity Analysis Model Http Adaptation Inspector)
  in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.ListPermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`

### EntityAnalysisModelHttpAdaptationCreate

`EntityAnalysisModelHttpAdaptationCreate` is a **write** operation, not idempotent, non-destructive.

Registers an HTTP Adaptation under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-http-adaptation-author` (Entity Analysis Model Http Adaptation Author) in the
  Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.WritePermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelHttpAdaptationDelete

`EntityAnalysisModelHttpAdaptationDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an HTTP Adaptation in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-http-adaptation-author` (Entity Analysis Model Http Adaptation Author) in the
  Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.WritePermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelHttpAdaptationFilter

`EntityAnalysisModelHttpAdaptationFilter` is a **read** operation, idempotent, non-destructive.

Returns the HTTP Adaptations matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-http-adaptation-inspector` (Entity Analysis Model Http Adaptation Inspector)
  in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.ListPermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`

### EntityAnalysisModelHttpAdaptationFilterFields

`EntityAnalysisModelHttpAdaptationFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over HTTP Adaptations may use.

- **Owning agent:** `entity-analysis-model-http-adaptation-inspector` (Entity Analysis Model Http Adaptation Inspector)
  in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.ListPermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`

### EntityAnalysisModelHttpAdaptationGet

`EntityAnalysisModelHttpAdaptationGet` is a **read** operation, idempotent, non-destructive.

Returns one HTTP Adaptation by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-http-adaptation-inspector` (Entity Analysis Model Http Adaptation Inspector)
  in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.ReadPermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`

### EntityAnalysisModelHttpAdaptationGetByEntityAnalysisModelId

`EntityAnalysisModelHttpAdaptationGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists HTTP Adaptations belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-http-adaptation-inspector` (Entity Analysis Model Http Adaptation Inspector)
  in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.ReadPermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`

### EntityAnalysisModelHttpAdaptationGetByGuid

`EntityAnalysisModelHttpAdaptationGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one HTTP Adaptation by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-http-adaptation-inspector` (Entity Analysis Model Http Adaptation Inspector)
  in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.ReadPermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`

### EntityAnalysisModelHttpAdaptationList

`EntityAnalysisModelHttpAdaptationList` is a **read** operation, idempotent, non-destructive.

Lists HTTP Adaptations for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-http-adaptation-inspector` (Entity Analysis Model Http Adaptation Inspector)
  in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.ListPermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`

### EntityAnalysisModelHttpAdaptationUpdate

`EntityAnalysisModelHttpAdaptationUpdate` is a **write** operation, idempotent, non-destructive.

Updates an HTTP Adaptation in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-http-adaptation-author` (Entity Analysis Model Http Adaptation Author) in the
  Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.WritePermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelHttpAdaptationValidate

`EntityAnalysisModelHttpAdaptationValidate` is a **read** operation, idempotent, non-destructive.

Validates a HTTP Adaptation without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-http-adaptation-test-bench` (Entity Analysis Model Http Adaptation Test
  Bench) in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.WritePermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`

### EntityAnalysisModelInlineFunctionCount

`EntityAnalysisModelInlineFunctionCount` is a **read** operation, idempotent, non-destructive.

Counts the Inline Functions matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-inline-function-inspector` (Entity Analysis Model Inline Function Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.ListPermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionCreate

`EntityAnalysisModelInlineFunctionCreate` is a **write** operation, not idempotent, non-destructive.

Creates an Inline Function under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-inline-function-author` (Entity Analysis Model Inline Function Author) in the
  Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineFunctionDelete

`EntityAnalysisModelInlineFunctionDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an Inline Function in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-inline-function-author` (Entity Analysis Model Inline Function Author) in the
  Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineFunctionExecute

`EntityAnalysisModelInlineFunctionExecute` is a **read** operation, idempotent, non-destructive.

Runs an Inline Function against an invocation context as the engine would and returns the result.

- **Owning agent:** `entity-analysis-model-inline-function-test-bench` (Entity Analysis Model Inline Function Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionFilter

`EntityAnalysisModelInlineFunctionFilter` is a **read** operation, idempotent, non-destructive.

Returns the Inline Functions matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-inline-function-inspector` (Entity Analysis Model Inline Function Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.ListPermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionFilterFields

`EntityAnalysisModelInlineFunctionFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Inline Functions may use.

- **Owning agent:** `entity-analysis-model-inline-function-inspector` (Entity Analysis Model Inline Function Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.ListPermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionGet

`EntityAnalysisModelInlineFunctionGet` is a **read** operation, idempotent, non-destructive.

Returns one Inline Function by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-inline-function-inspector` (Entity Analysis Model Inline Function Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.ReadPermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionGetByEntityAnalysisModelId

`EntityAnalysisModelInlineFunctionGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Inline Functions belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-inline-function-inspector` (Entity Analysis Model Inline Function Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.ReadPermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionGetByGuid

`EntityAnalysisModelInlineFunctionGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Inline Function by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-inline-function-inspector` (Entity Analysis Model Inline Function Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.ReadPermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionList

`EntityAnalysisModelInlineFunctionList` is a **read** operation, idempotent, non-destructive.

Lists Inline Functions for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-inline-function-inspector` (Entity Analysis Model Inline Function Inspector)
  in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.ListPermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionParseRule

`EntityAnalysisModelInlineFunctionParseRule` is a **read** operation, idempotent, non-destructive.

Parses and compiles an Inline Function's rule text as the engine would and returns each located error.

- **Owning agent:** `entity-analysis-model-inline-function-test-bench` (Entity Analysis Model Inline Function Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineFunctionUpdate

`EntityAnalysisModelInlineFunctionUpdate` is a **write** operation, idempotent, non-destructive.

Updates an Inline Function in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-inline-function-author` (Entity Analysis Model Inline Function Author) in the
  Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineFunctionValidate

`EntityAnalysisModelInlineFunctionValidate` is a **read** operation, idempotent, non-destructive.

Validates an Inline Function without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-inline-function-test-bench` (Entity Analysis Model Inline Function Test
  Bench) in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`

### EntityAnalysisModelInlineScriptCount

`EntityAnalysisModelInlineScriptCount` is a **read** operation, idempotent, non-destructive.

Counts the Inline Scripts matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-inline-script-inspector` (Entity Analysis Model Inline Script Inspector) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.ListPermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelInlineScriptCreate

`EntityAnalysisModelInlineScriptCreate` is a **write** operation, not idempotent, non-destructive.

Registers an Inline Script under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-inline-script-author` (Entity Analysis Model Inline Script Author) in the
  Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineScriptDelete

`EntityAnalysisModelInlineScriptDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an Inline Script registration in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-inline-script-author` (Entity Analysis Model Inline Script Author) in the
  Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineScriptExecute

`EntityAnalysisModelInlineScriptExecute` is a **read** operation, idempotent, non-destructive.

Runs a model's inline script against an invocation context when enabled; returns its properties.

- **Owning agent:** `entity-analysis-model-inline-script-test-bench` (Entity Analysis Model Inline Script Test Bench) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelInlineScriptFilter

`EntityAnalysisModelInlineScriptFilter` is a **read** operation, idempotent, non-destructive.

Returns the Inline Scripts matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-inline-script-inspector` (Entity Analysis Model Inline Script Inspector) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.ListPermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelInlineScriptFilterFields

`EntityAnalysisModelInlineScriptFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Inline Scripts may use.

- **Owning agent:** `entity-analysis-model-inline-script-inspector` (Entity Analysis Model Inline Script Inspector) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.ListPermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelInlineScriptGet

`EntityAnalysisModelInlineScriptGet` is a **read** operation, idempotent, non-destructive.

Returns one Inline Script registration by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-inline-script-inspector` (Entity Analysis Model Inline Script Inspector) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.ReadPermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelInlineScriptGetByEntityAnalysisModelId

`EntityAnalysisModelInlineScriptGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Inline Script registrations belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-inline-script-inspector` (Entity Analysis Model Inline Script Inspector) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.ReadPermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelInlineScriptGetByGuid

`EntityAnalysisModelInlineScriptGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Inline Script registration by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-inline-script-inspector` (Entity Analysis Model Inline Script Inspector) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.ReadPermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelInlineScriptList

`EntityAnalysisModelInlineScriptList` is a **read** operation, idempotent, non-destructive.

Lists Inline Script registrations for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-inline-script-inspector` (Entity Analysis Model Inline Script Inspector) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.ListPermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelInlineScriptUpdate

`EntityAnalysisModelInlineScriptUpdate` is a **write** operation, idempotent, non-destructive.

Updates an Inline Script registration in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-inline-script-author` (Entity Analysis Model Inline Script Author) in the
  Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineScriptValidate

`EntityAnalysisModelInlineScriptValidate` is a **read** operation, idempotent, non-destructive.

Validates an Inline Script without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-inline-script-test-bench` (Entity Analysis Model Inline Script Test Bench) in
  the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`

### EntityAnalysisModelIntegrityCheck

`EntityAnalysisModelIntegrityCheck` is a **read** operation, idempotent, non-destructive.

Checks a model's dependencies, compilation, configuration and engine state; returns coded findings.

- **Owning agent:** `model-integrity-and-sampling` (Model Integrity and Sampling) in the Model Risk and Analytics domain
- **Access:** permission 60
- **Permission source:** `EntityAnalysisModelIntegrityService.RequiredPermissions`
- **Service:** `EntityAnalysisModelIntegrityService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelIntegrity.cs`

### EntityAnalysisModelIntegrityEngineState

`EntityAnalysisModelIntegrityEngineState` is a **read** operation, idempotent, non-destructive.

Returns what the engine instances have loaded and started for a model.

- **Owning agent:** `model-integrity-and-sampling` (Model Integrity and Sampling) in the Model Risk and Analytics domain
- **Access:** permission 60
- **Permission source:** `EntityAnalysisModelIntegrityService.RequiredPermissions`
- **Service:** `EntityAnalysisModelIntegrityService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelIntegrity.cs`

### EntityAnalysisModelIntegrityGraph

`EntityAnalysisModelIntegrityGraph` is a **read** operation, idempotent, non-destructive.

Returns a model's dependency graph as nodes and edges, optionally centred on one entity.

- **Owning agent:** `model-integrity-and-sampling` (Model Integrity and Sampling) in the Model Risk and Analytics domain
- **Access:** permission 60
- **Permission source:** `EntityAnalysisModelIntegrityService.RequiredPermissions`
- **Service:** `EntityAnalysisModelIntegrityService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelIntegrity.cs`

### EntityAnalysisModelIntegrityModels

`EntityAnalysisModelIntegrityModels` is a **read** operation, idempotent, non-destructive.

Lists the tenant's models that can be checked.

- **Owning agent:** `model-integrity-and-sampling` (Model Integrity and Sampling) in the Model Risk and Analytics domain
- **Access:** permission 60
- **Permission source:** `EntityAnalysisModelIntegrityService.RequiredPermissions`
- **Service:** `EntityAnalysisModelIntegrityService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelIntegrity.cs`

### EntityAnalysisModelInvocationContextBlank

`EntityAnalysisModelInvocationContextBlank` is a **read** operation, idempotent, non-destructive.

Returns a blank rule-evaluation context for a model, with every name a rule can use and its defaults.

- **Owning agent:** `model-invocation-context-and-dependencies` (Model Invocation Context and Dependencies) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelInvocationContextService.AuthoringPermissions`
- **Service:** `EntityAnalysisModelInvocationContextService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInvocationContext.cs`

### EntityAnalysisModelInvocationContextFromArchive

`EntityAnalysisModelInvocationContextFromArchive` is a **read** operation, idempotent, non-destructive.

Builds a rule-evaluation context from an archived transaction, with every value as it was.

- **Owning agent:** `model-invocation-context-and-dependencies` (Model Invocation Context and Dependencies) in the Model
  Supporting Structures domain
- **Access:** permission 1, 40
- **Permission source:** `EntityAnalysisModelInvocationContextService.ArchivePermissions`
- **Service:** `EntityAnalysisModelInvocationContextService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInvocationContext.cs`

### EntityAnalysisModelInvocationContextFromRequestJson

`EntityAnalysisModelInvocationContextFromRequestJson` is a **read** operation, idempotent, non-destructive.

Builds a rule-evaluation context from request JSON with the engine's own extraction; stores nothing.

- **Owning agent:** `model-invocation-context-and-dependencies` (Model Invocation Context and Dependencies) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelInvocationContextService.AuthoringPermissions`
- **Service:** `EntityAnalysisModelInvocationContextService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInvocationContext.cs`

### EntityAnalysisModelInvocationContextOverlay

`EntityAnalysisModelInvocationContextOverlay` is a **read** operation, idempotent, non-destructive.

Sets values in a rule-evaluation context by completion name, checking each against its data type.

- **Owning agent:** `model-invocation-context-and-dependencies` (Model Invocation Context and Dependencies) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelInvocationContextService.AuthoringPermissions`
- **Service:** `EntityAnalysisModelInvocationContextService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInvocationContext.cs`

### EntityAnalysisModelList

`EntityAnalysisModelList` is a **read** operation, idempotent, non-destructive.

Lists Models for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-inspector` (Entity Analysis Model Inspector) in the Detection Model Authoring
  domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelService.RequiredPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`

### EntityAnalysisModelListCount

`EntityAnalysisModelListCount` is a **read** operation, idempotent, non-destructive.

Counts the Lists matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-list-inspector` (Entity Analysis Model List Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`

### EntityAnalysisModelListCreate

`EntityAnalysisModelListCreate` is a **write** operation, not idempotent, non-destructive.

Registers a List under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-list-author` (Entity Analysis Model List Author) in the Model Supporting
  Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListDelete

`EntityAnalysisModelListDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a List in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-list-author` (Entity Analysis Model List Author) in the Model Supporting
  Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListFilter

`EntityAnalysisModelListFilter` is a **read** operation, idempotent, non-destructive.

Returns the Lists matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-list-inspector` (Entity Analysis Model List Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`

### EntityAnalysisModelListFilterFields

`EntityAnalysisModelListFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Lists may use.

- **Owning agent:** `entity-analysis-model-list-inspector` (Entity Analysis Model List Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`

### EntityAnalysisModelListGet

`EntityAnalysisModelListGet` is a **read** operation, idempotent, non-destructive.

Returns one List by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-list-inspector` (Entity Analysis Model List Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`

### EntityAnalysisModelListGetByEntityAnalysisModelId

`EntityAnalysisModelListGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists the Lists belonging to a given Model, ordered by Id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-list-inspector` (Entity Analysis Model List Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`

### EntityAnalysisModelListGetByGuid

`EntityAnalysisModelListGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one List by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-list-inspector` (Entity Analysis Model List Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`

### EntityAnalysisModelListList

`EntityAnalysisModelListList` is a **read** operation, idempotent, non-destructive.

Lists Lists for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-list-inspector` (Entity Analysis Model List Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`

### EntityAnalysisModelListUpdate

`EntityAnalysisModelListUpdate` is a **write** operation, idempotent, non-destructive.

Updates a List in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-list-author` (Entity Analysis Model List Author) in the Model Supporting
  Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListValidate

`EntityAnalysisModelListValidate` is a **read** operation, idempotent, non-destructive.

Validates a List without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-list-test-bench` (Entity Analysis Model List Test Bench) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`

### EntityAnalysisModelListValueCount

`EntityAnalysisModelListValueCount` is a **read** operation, idempotent, non-destructive.

Counts the List Values matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-list-value-inspector` (Entity Analysis Model List Value Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`

### EntityAnalysisModelListValueCreate

`EntityAnalysisModelListValueCreate` is a **write** operation, not idempotent, non-destructive.

Registers a value under a List in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-list-value-author` (Entity Analysis Model List Value Author) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListValueDelete

`EntityAnalysisModelListValueDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a value in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-list-value-author` (Entity Analysis Model List Value Author) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListValueFilter

`EntityAnalysisModelListValueFilter` is a **read** operation, idempotent, non-destructive.

Returns the List Values matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-list-value-inspector` (Entity Analysis Model List Value Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`

### EntityAnalysisModelListValueFilterFields

`EntityAnalysisModelListValueFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over List Values may use.

- **Owning agent:** `entity-analysis-model-list-value-inspector` (Entity Analysis Model List Value Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`

### EntityAnalysisModelListValueGet

`EntityAnalysisModelListValueGet` is a **read** operation, idempotent, non-destructive.

Returns one List Value matched against its parent List's id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-list-value-inspector` (Entity Analysis Model List Value Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`

### EntityAnalysisModelListValueGetByEntityAnalysisModelListId

`EntityAnalysisModelListValueGetByEntityAnalysisModelListId` is a **read** operation, idempotent, non-destructive.

Lists the values belonging to a given List, ordered by Id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-list-value-inspector` (Entity Analysis Model List Value Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`

### EntityAnalysisModelListValueGetByGuid

`EntityAnalysisModelListValueGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one List Value matched against its parent List's id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-list-value-inspector` (Entity Analysis Model List Value Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`

### EntityAnalysisModelListValueList

`EntityAnalysisModelListValueList` is a **read** operation, idempotent, non-destructive.

Lists List Values for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-list-value-inspector` (Entity Analysis Model List Value Inspector) in the
  Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`

### EntityAnalysisModelListValueUpdate

`EntityAnalysisModelListValueUpdate` is a **write** operation, idempotent, non-destructive.

Updates a value in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-list-value-author` (Entity Analysis Model List Value Author) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListValueUploadCsv

`EntityAnalysisModelListValueUploadCsv` is a **write** operation, not idempotent, non-destructive.

Loads one or more CSV files of values into a List in the caller's tenant in a single transaction, recording an upload
entry per file; calling twice loads the values twice.

- **Owning agent:** `entity-analysis-model-list-value-author` (Entity Analysis Model List Value Author) in the Model
  Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListValueValidate

`EntityAnalysisModelListValueValidate` is a **read** operation, idempotent, non-destructive.

Validates a List Value without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-list-value-test-bench` (Entity Analysis Model List Value Test Bench) in the
  Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`

### EntityAnalysisModelOverrideCount

`EntityAnalysisModelOverrideCount` is a **read** operation, idempotent, non-destructive.

Counts the Overrides matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-override-inspector` (Entity Analysis Model Override Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`

### EntityAnalysisModelOverrideCreate

`EntityAnalysisModelOverrideCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Override against a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-override-author` (Entity Analysis Model Override Author) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelOverrideDelete

`EntityAnalysisModelOverrideDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Override in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-override-author` (Entity Analysis Model Override Author) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelOverrideFilter

`EntityAnalysisModelOverrideFilter` is a **read** operation, idempotent, non-destructive.

Returns the Overrides matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-override-inspector` (Entity Analysis Model Override Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`

### EntityAnalysisModelOverrideFilterFields

`EntityAnalysisModelOverrideFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Overrides may use.

- **Owning agent:** `entity-analysis-model-override-inspector` (Entity Analysis Model Override Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`

### EntityAnalysisModelOverrideGet

`EntityAnalysisModelOverrideGet` is a **read** operation, idempotent, non-destructive.

Returns one Override by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-override-inspector` (Entity Analysis Model Override Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`

### EntityAnalysisModelOverrideGetByEntityAnalysisModelId

`EntityAnalysisModelOverrideGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Overrides belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-override-inspector` (Entity Analysis Model Override Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`

### EntityAnalysisModelOverrideList

`EntityAnalysisModelOverrideList` is a **read** operation, idempotent, non-destructive.

Lists Overrides for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-override-inspector` (Entity Analysis Model Override Inspector) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`

### EntityAnalysisModelOverrideQueryGet

`EntityAnalysisModelOverrideQueryGet` is a **read** operation, idempotent, non-destructive.

Lists tenant models supporting override on a key, flagging active overrides for a key value.

- **Owning agent:** `entity-analysis-model-override-query` (Entity Analysis Model Override Query) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideQueryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideQueryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverrideQuery.cs`

### EntityAnalysisModelOverrideQueryGetKeys

`EntityAnalysisModelOverrideQueryGetKeys` is a **read** operation, idempotent, non-destructive.

Lists the override keys available for a Model in the caller's tenant.

- **Owning agent:** `entity-analysis-model-override-query` (Entity Analysis Model Override Query) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideQueryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideQueryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverrideQuery.cs`

### EntityAnalysisModelOverrideQueryGetValues

`EntityAnalysisModelOverrideQueryGetValues` is a **read** operation, idempotent, non-destructive.

Lists the values held against an override key for a Model in the caller's tenant.

- **Owning agent:** `entity-analysis-model-override-query` (Entity Analysis Model Override Query) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideQueryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideQueryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverrideQuery.cs`

### EntityAnalysisModelOverrideUpdate

`EntityAnalysisModelOverrideUpdate` is a **write** operation, not idempotent, non-destructive.

Toggles a Override on or off by row existence; NOT idempotent -- repeat calls flip state.

- **Owning agent:** `entity-analysis-model-override-author` (Entity Analysis Model Override Author) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelOverrideUpdateDeleteExpiryDate

`EntityAnalysisModelOverrideUpdateDeleteExpiryDate` is a **write** operation, idempotent, non-destructive.

Updates the Delete Expiry Date of an existing active Override in the caller's tenant.

- **Owning agent:** `entity-analysis-model-override-author` (Entity Analysis Model Override Author) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelOverrideValidate

`EntityAnalysisModelOverrideValidate` is a **read** operation, idempotent, non-destructive.

Validates a Override without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-override-test-bench` (Entity Analysis Model Override Test Bench) in the Model
  Supporting Structures domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelOverrideService.RequiredPermissions`
- **Service:** `EntityAnalysisModelOverrideService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelOverride.cs`

### EntityAnalysisModelProcessingCounterList

`EntityAnalysisModelProcessingCounterList` is a **read** operation, idempotent, non-destructive.

Lists per-Model processing counter rows, most recent first, capped at 100000, with optional date-range and Model-name
substring filters. Each row is a roughly one-minute interval's counts for one Entity Analysis Model -- synchronous
invocations, Gateway Rule matches, response-elevation triggers, activation-watcher triggers, response times, and pending
archive WAL entries. Landlord only: every other caller is refused with a 403. Also accepts samplePercentage (0-100) to
draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `engine-queue-and-throughput-counters` (Engine Queue and Throughput Counters) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EntityAnalysisModelProcessingCounterService.RequiredPermissions`
- **Service:** `EntityAnalysisModelProcessingCounterService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelProcessingCounter.cs`

### EntityAnalysisModelReprocessingRuleBuildRuleFromBuilderJson

`EntityAnalysisModelReprocessingRuleBuildRuleFromBuilderJson` is a **read** operation, idempotent, non-destructive.

Turns query builder JSON into a Reprocessing Rule's rule text as the browser builder would, and parses it.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-test-bench` (Entity Analysis Model Reprocessing Rule Test
  Bench) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleCount

`EntityAnalysisModelReprocessingRuleCount` is a **read** operation, idempotent, non-destructive.

Counts the Reprocessing Rules matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-inspector` (Entity Analysis Model Reprocessing Rule
  Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleCreate

`EntityAnalysisModelReprocessingRuleCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Reprocessing Rule under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-author` (Entity Analysis Model Reprocessing Rule Author) in
  the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelReprocessingRuleDelete

`EntityAnalysisModelReprocessingRuleDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Reprocessing Rule in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-author` (Entity Analysis Model Reprocessing Rule Author) in
  the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelReprocessingRuleExecute

`EntityAnalysisModelReprocessingRuleExecute` is a **read** operation, idempotent, non-destructive.

Runs a Reprocessing Rule against an invocation context as the engine would and returns the result.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-test-bench` (Entity Analysis Model Reprocessing Rule Test
  Bench) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleFilter

`EntityAnalysisModelReprocessingRuleFilter` is a **read** operation, idempotent, non-destructive.

Returns the Reprocessing Rules matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-inspector` (Entity Analysis Model Reprocessing Rule
  Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleFilterFields

`EntityAnalysisModelReprocessingRuleFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Reprocessing Rules may use.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-inspector` (Entity Analysis Model Reprocessing Rule
  Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleGet

`EntityAnalysisModelReprocessingRuleGet` is a **read** operation, idempotent, non-destructive.

Returns one Reprocessing Rule by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-inspector` (Entity Analysis Model Reprocessing Rule
  Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleGetByEntityAnalysisModelId

`EntityAnalysisModelReprocessingRuleGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Reprocessing Rules belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-inspector` (Entity Analysis Model Reprocessing Rule
  Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleInstanceCount

`EntityAnalysisModelReprocessingRuleInstanceCount` is a **read** operation, idempotent, non-destructive.

Counts the Reprocessing Rule Instances matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-inspector` (Entity Analysis Model Reprocessing
  Rule Instance Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`

### EntityAnalysisModelReprocessingRuleInstanceCreate

`EntityAnalysisModelReprocessingRuleInstanceCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Reprocessing Rule instance under a Reprocessing Rule in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-author` (Entity Analysis Model Reprocessing Rule
  Instance Author) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelReprocessingRuleInstanceDelete

`EntityAnalysisModelReprocessingRuleInstanceDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Reprocessing Rule instance in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-author` (Entity Analysis Model Reprocessing Rule
  Instance Author) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelReprocessingRuleInstanceFilter

`EntityAnalysisModelReprocessingRuleInstanceFilter` is a **read** operation, idempotent, non-destructive.

Returns the Reprocessing Rule Instances matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-inspector` (Entity Analysis Model Reprocessing
  Rule Instance Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`

### EntityAnalysisModelReprocessingRuleInstanceFilterFields

`EntityAnalysisModelReprocessingRuleInstanceFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Reprocessing Rule Instances may use.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-inspector` (Entity Analysis Model Reprocessing
  Rule Instance Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`

### EntityAnalysisModelReprocessingRuleInstanceGet

`EntityAnalysisModelReprocessingRuleInstanceGet` is a **read** operation, idempotent, non-destructive.

Returns one Reprocessing Rule instance by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-inspector` (Entity Analysis Model Reprocessing
  Rule Instance Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`

### EntityAnalysisModelReprocessingRuleInstanceGetByEntityAnalysisModelReprocessingId

`EntityAnalysisModelReprocessingRuleInstanceGetByEntityAnalysisModelReprocessingId` is a **read** operation, idempotent,
non-destructive.

Lists Reprocessing Rule instances belonging to a given Reprocessing Rule, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-inspector` (Entity Analysis Model Reprocessing
  Rule Instance Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`

### EntityAnalysisModelReprocessingRuleInstanceInsertByExistingUpdateUncompleted

`EntityAnalysisModelReprocessingRuleInstanceInsertByExistingUpdateUncompleted` is a **write** operation, not idempotent,
non-destructive.

Triggers a new reprocessing run for a Reprocessing Rule; fails if one is already uncompleted.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-author` (Entity Analysis Model Reprocessing Rule
  Instance Author) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelReprocessingRuleInstanceList

`EntityAnalysisModelReprocessingRuleInstanceList` is a **read** operation, idempotent, non-destructive.

Lists Reprocessing Rule instances for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-inspector` (Entity Analysis Model Reprocessing
  Rule Instance Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`

### EntityAnalysisModelReprocessingRuleInstanceUpdate

`EntityAnalysisModelReprocessingRuleInstanceUpdate` is a **write** operation, not idempotent, non-destructive.

Supersedes a Reprocessing Rule instance in the caller's tenant by id, assigning it a new id.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-author` (Entity Analysis Model Reprocessing Rule
  Instance Author) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelReprocessingRuleInstanceValidate

`EntityAnalysisModelReprocessingRuleInstanceValidate` is a **read** operation, idempotent, non-destructive.

Validates a Reprocessing Rule Instance without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-instance-test-bench` (Entity Analysis Model Reprocessing
  Rule Instance Test Bench) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRuleInstance.cs`

### EntityAnalysisModelReprocessingRuleList

`EntityAnalysisModelReprocessingRuleList` is a **read** operation, idempotent, non-destructive.

Lists Reprocessing Rules for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-inspector` (Entity Analysis Model Reprocessing Rule
  Inspector) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleParseRule

`EntityAnalysisModelReprocessingRuleParseRule` is a **read** operation, idempotent, non-destructive.

Parses and compiles a Reprocessing Rule's rule text as the engine would and returns each located error.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-test-bench` (Entity Analysis Model Reprocessing Rule Test
  Bench) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelReprocessingRuleUpdate

`EntityAnalysisModelReprocessingRuleUpdate` is a **write** operation, not idempotent, non-destructive.

Supersedes a Reprocessing Rule in the caller's tenant by id, assigning it a new id.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-author` (Entity Analysis Model Reprocessing Rule Author) in
  the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelReprocessingRuleValidate

`EntityAnalysisModelReprocessingRuleValidate` is a **read** operation, idempotent, non-destructive.

Validates a Reprocessing Rule without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-reprocessing-rule-test-bench` (Entity Analysis Model Reprocessing Rule Test
  Bench) in the Model Supporting Structures domain
- **Access:** permission 26
- **Permission source:** `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelReprocessingRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs`

### EntityAnalysisModelRequestXPathCount

`EntityAnalysisModelRequestXPathCount` is a **read** operation, idempotent, non-destructive.

Counts the RequestXPaths matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-1` (Entity Analysis Model Request X Path Inspector
  1) in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.ListPermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathCreate

`EntityAnalysisModelRequestXPathCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Request XPath under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-request-x-path-author` (Entity Analysis Model Request X Path Author) in the
  Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.WritePermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelRequestXPathDelete

`EntityAnalysisModelRequestXPathDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Request XPath in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-request-x-path-author` (Entity Analysis Model Request X Path Author) in the
  Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.WritePermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelRequestXPathFilter

`EntityAnalysisModelRequestXPathFilter` is a **read** operation, idempotent, non-destructive.

Returns the RequestXPaths matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-1` (Entity Analysis Model Request X Path Inspector
  1) in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.ListPermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathFilterFields

`EntityAnalysisModelRequestXPathFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over RequestXPaths may use.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-1` (Entity Analysis Model Request X Path Inspector
  1) in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.ListPermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathGet

`EntityAnalysisModelRequestXPathGet` is a **read** operation, idempotent, non-destructive.

Returns one Request XPath by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-1` (Entity Analysis Model Request X Path Inspector
  1) in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.ReadPermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathGetByCasesWorkflowId

`EntityAnalysisModelRequestXPathGetByCasesWorkflowId` is a **read** operation, idempotent, non-destructive.

Lists Request XPaths reachable from a given Case Workflow's Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-1` (Entity Analysis Model Request X Path Inspector
  1) in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.ReadPermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId

`EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Request XPaths belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-1` (Entity Analysis Model Request X Path Inspector
  1) in the Detection Model Authoring domain
- **Access:** permission 7, 13
- **Permission source:** `EntityAnalysisModelRequestXPathService.ReadByEntityAnalysisModelPermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathGetByEntityAnalysisModelIdByDataType

`EntityAnalysisModelRequestXPathGetByEntityAnalysisModelIdByDataType` is a **read** operation, idempotent,
non-destructive.

Lists String, Integer or Float Request XPaths belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-2` (Entity Analysis Model Request X Path Inspector
  2) in the Detection Model Authoring domain
- **Access:** permission 7, 12
- **Permission source:** `EntityAnalysisModelRequestXPathService.ReadByDataTypePermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathGetByOverrideKey

`EntityAnalysisModelRequestXPathGetByOverrideKey` is a **read** operation, idempotent, non-destructive.

Lists Request XPaths flagged for Override, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-2` (Entity Analysis Model Request X Path Inspector
  2) in the Detection Model Authoring domain
- **Access:** permission 2
- **Permission source:** `EntityAnalysisModelRequestXPathService.OverridePermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathList

`EntityAnalysisModelRequestXPathList` is a **read** operation, idempotent, non-destructive.

Lists Request XPaths for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-request-x-path-inspector-1` (Entity Analysis Model Request X Path Inspector
  1) in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.ListPermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelRequestXPathUpdate

`EntityAnalysisModelRequestXPathUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Request XPath in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-request-x-path-author` (Entity Analysis Model Request X Path Author) in the
  Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.WritePermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelRequestXPathValidate

`EntityAnalysisModelRequestXPathValidate` is a **read** operation, idempotent, non-destructive.

Validates a RequestXPath without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-request-x-path-test-bench` (Entity Analysis Model Request X Path Test Bench)
  in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.WritePermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`

### EntityAnalysisModelResponseTimePipelineCounterList

`EntityAnalysisModelResponseTimePipelineCounterList` is a **read** operation, idempotent, non-destructive.

Lists per-minute Response Time Pipeline Counter rows for a Model, most recent first, capped at 100000, with optional
date-range and substring search (StageName) filters, scoped to the caller's tenant. Also accepts samplePercentage
(0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `engine-queue-and-throughput-counters` (Engine Queue and Throughput Counters) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EntityAnalysisModelResponseTimePipelineCounterService.RequiredPermissions`
- **Service:** `EntityAnalysisModelResponseTimePipelineCounterService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelResponseTimePipelineCounter.cs`

### EntityAnalysisModelRoleCreate

`EntityAnalysisModelRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to an Entity Analysis Model; calling twice creates two grants.

- **Owning agent:** `authoring-aids-and-model-role-grants` (Authoring Aids and Model Role Grants) in the Model
  Supporting Structures domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelRoleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelRoleDelete

`EntityAnalysisModelRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on an Entity Analysis Model; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `authoring-aids-and-model-role-grants` (Authoring Aids and Model Role Grants) in the Model
  Supporting Structures domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelRoleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelRoleListByEntityAnalysisModelGuid

`EntityAnalysisModelRoleListByEntityAnalysisModelGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for an Entity Analysis Model.

- **Owning agent:** `authoring-aids-and-model-role-grants` (Authoring Aids and Model Role Grants) in the Model
  Supporting Structures domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelRoleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRole.cs`

### EntityAnalysisModelSampleExecute

`EntityAnalysisModelSampleExecute` is a **read** operation, idempotent, non-destructive.

Samples archived transactions of an Entity Analysis Model over a date window and returns them as a CSV file.

- **Owning agent:** `model-integrity-and-sampling` (Model Integrity and Sampling) in the Model Risk and Analytics domain
- **Access:** permission 40
- **Permission source:** `EntityAnalysisModelSampleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSampleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSample.cs`

### EntityAnalysisModelSanctionCount

`EntityAnalysisModelSanctionCount` is a **read** operation, idempotent, non-destructive.

Counts the Sanctions matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-sanction-inspector` (Entity Analysis Model Sanction Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.ListPermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`

### EntityAnalysisModelSanctionCreate

`EntityAnalysisModelSanctionCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Sanction check under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-sanction-author` (Entity Analysis Model Sanction Author) in the Detection
  Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.WritePermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelSanctionDelete

`EntityAnalysisModelSanctionDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Sanction check in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-sanction-author` (Entity Analysis Model Sanction Author) in the Detection
  Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.WritePermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelSanctionFilter

`EntityAnalysisModelSanctionFilter` is a **read** operation, idempotent, non-destructive.

Returns the Sanctions matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-sanction-inspector` (Entity Analysis Model Sanction Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.ListPermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`

### EntityAnalysisModelSanctionFilterFields

`EntityAnalysisModelSanctionFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Sanctions may use.

- **Owning agent:** `entity-analysis-model-sanction-inspector` (Entity Analysis Model Sanction Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.ListPermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`

### EntityAnalysisModelSanctionGet

`EntityAnalysisModelSanctionGet` is a **read** operation, idempotent, non-destructive.

Returns one Sanction check by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-sanction-inspector` (Entity Analysis Model Sanction Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.ReadPermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`

### EntityAnalysisModelSanctionGetByEntityAnalysisModelId

`EntityAnalysisModelSanctionGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Sanction checks belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-sanction-inspector` (Entity Analysis Model Sanction Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.ReadPermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`

### EntityAnalysisModelSanctionGetByGuid

`EntityAnalysisModelSanctionGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Sanction check by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-sanction-inspector` (Entity Analysis Model Sanction Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.ReadPermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`

### EntityAnalysisModelSanctionList

`EntityAnalysisModelSanctionList` is a **read** operation, idempotent, non-destructive.

Lists Sanction checks for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-sanction-inspector` (Entity Analysis Model Sanction Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.ListPermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`

### EntityAnalysisModelSanctionUpdate

`EntityAnalysisModelSanctionUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Sanction check in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-sanction-author` (Entity Analysis Model Sanction Author) in the Detection
  Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.WritePermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelSanctionValidate

`EntityAnalysisModelSanctionValidate` is a **read** operation, idempotent, non-destructive.

Validates a Sanction without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-sanction-test-bench` (Entity Analysis Model Sanction Test Bench) in the
  Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.WritePermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`

### EntityAnalysisModelStagePerformanceCounterList

`EntityAnalysisModelStagePerformanceCounterList` is a **read** operation, idempotent, non-destructive.

Lists per-minute Stage Performance Counter rows for a Model, most recent first, capped at 100000, with optional
date-range and substring search (StageName) filters, scoped to the caller's tenant. Also accepts samplePercentage
(0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `engine-queue-and-throughput-counters` (Engine Queue and Throughput Counters) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EntityAnalysisModelStagePerformanceCounterService.RequiredPermissions`
- **Service:** `EntityAnalysisModelStagePerformanceCounterService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelStagePerformanceCounter.cs`

### EntityAnalysisModelSynchronisationNodeStatusEntriesGet

`EntityAnalysisModelSynchronisationNodeStatusEntriesGet` is a **read** operation, idempotent, non-destructive.

Lists the tenant's model-synchronisation node statuses seen in the last hour, with pending and availability flags.

- **Owning agent:** `engine-queue-and-throughput-counters` (Engine Queue and Throughput Counters) in the Platform
  Reliability domain
- **Access:** permission 5
- **Permission source:** `EntityAnalysisModelSynchronisationNodeStatusEntriesService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSynchronisationNodeStatusEntriesService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSynchronisationNodeStatusEntries.cs`

### EntityAnalysisModelSynchronisationScheduleCount

`EntityAnalysisModelSynchronisationScheduleCount` is a **read** operation, idempotent, non-destructive.

Counts the Synchronisation Schedules matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-synchronisation-schedule` (Entity Analysis Model Synchronisation Schedule) in
  the Platform Reliability domain
- **Access:** permission 5
- **Permission source:** `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSynchronisationScheduleService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSynchronisationSchedule.cs`

### EntityAnalysisModelSynchronisationScheduleCreate

`EntityAnalysisModelSynchronisationScheduleCreate` is a **write** operation, not idempotent, non-destructive.

Schedules model synchronisation across the cluster; calling twice creates two rows.

- **Owning agent:** `entity-analysis-model-synchronisation-schedule` (Entity Analysis Model Synchronisation Schedule) in
  the Platform Reliability domain
- **Access:** permission 5
- **Permission source:** `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSynchronisationScheduleService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSynchronisationSchedule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelSynchronisationScheduleFilter

`EntityAnalysisModelSynchronisationScheduleFilter` is a **read** operation, idempotent, non-destructive.

Returns the Synchronisation Schedules matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-synchronisation-schedule` (Entity Analysis Model Synchronisation Schedule) in
  the Platform Reliability domain
- **Access:** permission 5
- **Permission source:** `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSynchronisationScheduleService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSynchronisationSchedule.cs`

### EntityAnalysisModelSynchronisationScheduleFilterFields

`EntityAnalysisModelSynchronisationScheduleFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Synchronisation Schedules may use.

- **Owning agent:** `entity-analysis-model-synchronisation-schedule` (Entity Analysis Model Synchronisation Schedule) in
  the Platform Reliability domain
- **Access:** permission 5
- **Permission source:** `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSynchronisationScheduleService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSynchronisationSchedule.cs`

### EntityAnalysisModelSynchronisationScheduleGetCurrent

`EntityAnalysisModelSynchronisationScheduleGetCurrent` is a **read** operation, idempotent, non-destructive.

Returns the most recently scheduled model-synchronisation row for the caller's tenant.

- **Owning agent:** `entity-analysis-model-synchronisation-schedule` (Entity Analysis Model Synchronisation Schedule) in
  the Platform Reliability domain
- **Access:** permission 5
- **Permission source:** `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSynchronisationScheduleService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSynchronisationSchedule.cs`

### EntityAnalysisModelSynchronisationScheduleList

`EntityAnalysisModelSynchronisationScheduleList` is a **read** operation, idempotent, non-destructive.

Lists scheduled model-synchronisation rows for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-synchronisation-schedule` (Entity Analysis Model Synchronisation Schedule) in
  the Platform Reliability domain
- **Access:** permission 5
- **Permission source:** `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSynchronisationScheduleService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSynchronisationSchedule.cs`

### EntityAnalysisModelSynchronisationScheduleValidate

`EntityAnalysisModelSynchronisationScheduleValidate` is a **read** operation, idempotent, non-destructive.

Validates a Synchronisation Schedule without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-synchronisation-schedule` (Entity Analysis Model Synchronisation Schedule) in
  the Platform Reliability domain
- **Access:** permission 5
- **Permission source:** `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`
- **Service:** `EntityAnalysisModelSynchronisationScheduleService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSynchronisationSchedule.cs`

### EntityAnalysisModelTagCount

`EntityAnalysisModelTagCount` is a **read** operation, idempotent, non-destructive.

Counts the Tags matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-tag-inspector` (Entity Analysis Model Tag Inspector) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`

### EntityAnalysisModelTagCreate

`EntityAnalysisModelTagCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Tag under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-tag-author` (Entity Analysis Model Tag Author) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTagDelete

`EntityAnalysisModelTagDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Tag in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-tag-author` (Entity Analysis Model Tag Author) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTagFilter

`EntityAnalysisModelTagFilter` is a **read** operation, idempotent, non-destructive.

Returns the Tags matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-tag-inspector` (Entity Analysis Model Tag Inspector) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`

### EntityAnalysisModelTagFilterFields

`EntityAnalysisModelTagFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Tags may use.

- **Owning agent:** `entity-analysis-model-tag-inspector` (Entity Analysis Model Tag Inspector) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`

### EntityAnalysisModelTagGet

`EntityAnalysisModelTagGet` is a **read** operation, idempotent, non-destructive.

Returns one Tag by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-tag-inspector` (Entity Analysis Model Tag Inspector) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`

### EntityAnalysisModelTagGetByEntityAnalysisModelId

`EntityAnalysisModelTagGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists the Active Tags belonging to a given Model, ordered by Name, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-tag-inspector` (Entity Analysis Model Tag Inspector) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`

### EntityAnalysisModelTagGetByGuid

`EntityAnalysisModelTagGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Tag by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-tag-inspector` (Entity Analysis Model Tag Inspector) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`

### EntityAnalysisModelTagList

`EntityAnalysisModelTagList` is a **read** operation, idempotent, non-destructive.

Lists Tags for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-tag-inspector` (Entity Analysis Model Tag Inspector) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`

### EntityAnalysisModelTagUpdate

`EntityAnalysisModelTagUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Tag in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-tag-author` (Entity Analysis Model Tag Author) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTagValidate

`EntityAnalysisModelTagValidate` is a **read** operation, idempotent, non-destructive.

Validates a Tag without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-tag-test-bench` (Entity Analysis Model Tag Test Bench) in the Detection Model
  Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`

### EntityAnalysisModelTaskPerformanceCounterList

`EntityAnalysisModelTaskPerformanceCounterList` is a **read** operation, idempotent, non-destructive.

Lists per-minute Task Performance Counter rows for a Model, most recent first, capped at 100000, with optional
date-range and substring search (TaskName/Direction) filters, scoped to the caller's tenant -- useful for cloud
compute-cost attribution. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows
instead of the most recent ones.

- **Owning agent:** `invoke-path-performance-and-warnings` (Invoke Path Performance and Warnings) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EntityAnalysisModelTaskPerformanceCounterService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTaskPerformanceCounterService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTaskPerformanceCounter.cs`

### EntityAnalysisModelTimeWindowAbstractionRule

`EntityAnalysisModelTimeWindowAbstractionRule` is a **read** operation, idempotent, non-destructive.

Works out the window an abstraction rule aggregates over for a reference date.

- **Owning agent:** `entity-analysis-model-time-window` (Entity Analysis Model Time Window) in the Detection Model
  Authoring domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelTimeWindowService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTimeWindowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTimeWindow.cs`

### EntityAnalysisModelTimeWindowCalculate

`EntityAnalysisModelTimeWindowCalculate` is a **read** operation, idempotent, non-destructive.

Works out a time window's exact start and end as the engine does.

- **Owning agent:** `entity-analysis-model-time-window` (Entity Analysis Model Time Window) in the Detection Model
  Authoring domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelTimeWindowService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTimeWindowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTimeWindow.cs`

### EntityAnalysisModelTimeWindowIntervals

`EntityAnalysisModelTimeWindowIntervals` is a **read** operation, idempotent, non-destructive.

Lists the kinds of time window and the interval codes each allows.

- **Owning agent:** `entity-analysis-model-time-window` (Entity Analysis Model Time Window) in the Detection Model
  Authoring domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelTimeWindowService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTimeWindowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTimeWindow.cs`

### EntityAnalysisModelTimeWindowTtlCounter

`EntityAnalysisModelTimeWindowTtlCounter` is a **read** operation, idempotent, non-destructive.

Works out the window a TTL counter counts over for a reference date.

- **Owning agent:** `entity-analysis-model-time-window` (Entity Analysis Model Time Window) in the Detection Model
  Authoring domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `EntityAnalysisModelTimeWindowService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTimeWindowService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTimeWindow.cs`

### EntityAnalysisModelTtlCounterCount

`EntityAnalysisModelTtlCounterCount` is a **read** operation, idempotent, non-destructive.

Counts the TTL Counters matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `entity-analysis-model-ttl-counter-inspector` (Entity Analysis Model Ttl Counter Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.ListPermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelTtlCounterCreate

`EntityAnalysisModelTtlCounterCreate` is a **write** operation, not idempotent, non-destructive.

Registers a TTL Counter under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `entity-analysis-model-ttl-counter-author` (Entity Analysis Model Ttl Counter Author) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.WritePermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTtlCounterDelete

`EntityAnalysisModelTtlCounterDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a TTL Counter in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-ttl-counter-author` (Entity Analysis Model Ttl Counter Author) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.WritePermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTtlCounterFilter

`EntityAnalysisModelTtlCounterFilter` is a **read** operation, idempotent, non-destructive.

Returns the TTL Counters matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-ttl-counter-inspector` (Entity Analysis Model Ttl Counter Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.ListPermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelTtlCounterFilterFields

`EntityAnalysisModelTtlCounterFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over TTL Counters may use.

- **Owning agent:** `entity-analysis-model-ttl-counter-inspector` (Entity Analysis Model Ttl Counter Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.ListPermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelTtlCounterGet

`EntityAnalysisModelTtlCounterGet` is a **read** operation, idempotent, non-destructive.

Returns one TTL Counter by id, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-ttl-counter-inspector` (Entity Analysis Model Ttl Counter Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.ReadPermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelTtlCounterGetByEntityAnalysisModelGuid

`EntityAnalysisModelTtlCounterGetByEntityAnalysisModelGuid` is a **read** operation, idempotent, non-destructive.

Lists TTL Counters belonging to the Model with the given Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-ttl-counter-inspector` (Entity Analysis Model Ttl Counter Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 12, 17
- **Permission source:** `EntityAnalysisModelTtlCounterService.ReadByModelPermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId

`EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists TTL Counters belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-ttl-counter-inspector` (Entity Analysis Model Ttl Counter Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 12, 17
- **Permission source:** `EntityAnalysisModelTtlCounterService.ReadByModelPermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelTtlCounterGetByGuid

`EntityAnalysisModelTtlCounterGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one TTL Counter by Guid, scoped to the caller's tenant.

- **Owning agent:** `entity-analysis-model-ttl-counter-inspector` (Entity Analysis Model Ttl Counter Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.ReadPermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelTtlCounterList

`EntityAnalysisModelTtlCounterList` is a **read** operation, idempotent, non-destructive.

Lists TTL Counters for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `entity-analysis-model-ttl-counter-inspector` (Entity Analysis Model Ttl Counter Inspector) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.ListPermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelTtlCounterUpdate

`EntityAnalysisModelTtlCounterUpdate` is a **write** operation, idempotent, non-destructive.

Updates a TTL Counter in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-ttl-counter-author` (Entity Analysis Model Ttl Counter Author) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.WritePermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTtlCounterValidate

`EntityAnalysisModelTtlCounterValidate` is a **read** operation, idempotent, non-destructive.

Validates a TTL Counter without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-ttl-counter-test-bench` (Entity Analysis Model Ttl Counter Test Bench) in the
  Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.WritePermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`

### EntityAnalysisModelUpdate

`EntityAnalysisModelUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Model in the caller's tenant by id.

- **Owning agent:** `entity-analysis-model-author` (Entity Analysis Model Author) in the Detection Model Authoring
  domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelService.RequiredPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EntityAnalysisModelValidate

`EntityAnalysisModelValidate` is a **read** operation, idempotent, non-destructive.

Validates an Entity Analysis Model without saving it and returns every failure.

- **Owning agent:** `entity-analysis-model-test-bench` (Entity Analysis Model Test Bench) in the Detection Model
  Authoring domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelService.RequiredPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`

### EntityAnalysisPotentialMultiPartStringNamesGetByGuid

`EntityAnalysisPotentialMultiPartStringNamesGetByGuid` is a **read** operation, idempotent, non-destructive.

Lists the names of potential multi-part string fields for an entity analysis model, by model Guid.

- **Owning agent:** `entity-analysis-observability` (Entity Analysis Observability) in the Detection Model Authoring
  domain
- **Access:** permission 11, 17, 4, 1
- **Permission source:** `EntityAnalysisPotentialMultiPartStringNamesService.RequiredPermissions`
- **Service:** `EntityAnalysisPotentialMultiPartStringNamesService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisPotentialMultiPartStringNames.cs`

### EntityAnalysisPotentialMultiPartStringNamesGetById

`EntityAnalysisPotentialMultiPartStringNamesGetById` is a **read** operation, idempotent, non-destructive.

Lists the names of potential multi-part string fields for an entity analysis model, by model Id.

- **Owning agent:** `entity-analysis-observability` (Entity Analysis Observability) in the Detection Model Authoring
  domain
- **Access:** permission 11, 17, 4, 1
- **Permission source:** `EntityAnalysisPotentialMultiPartStringNamesService.RequiredPermissions`
- **Service:** `EntityAnalysisPotentialMultiPartStringNamesService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisPotentialMultiPartStringNames.cs`

### EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeGet

`EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeGet` is a **read** operation, idempotent,
non-destructive.

Lists the String and Float fields of an Entity Analysis Model usable as search keys.

- **Owning agent:** `entity-analysis-observability` (Entity Analysis Observability) in the Detection Model Authoring
  domain
- **Access:** permission 11, 17, 4, 12
- **Permission source:**
  `EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService.RequiredPermissions`
- **Service:** `EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.cs`

### EtcdClearAlarm

`EtcdClearAlarm` is a **write** operation, not idempotent, destructive.

Clears every currently-raised etcd alarm on the given endpoint -- acknowledges the flag, does not fix whatever raised it
(e.g. free the disk first for NOSPACE). Mutating.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EtcdAdministratorService.RequiredPermissions`
- **Service:** `EtcdAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EtcdAdministrator.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EtcdClusterEventList

`EtcdClusterEventList` is a **read** operation, idempotent, non-destructive.

Lists captured etcd cluster events (LeaderChanged, HealthChanged, AlarmRaised, AlarmCleared), detected by diffing
consecutive one-minute EtcdMemberStatus samples, most recent first, capped at 100000. Supports optional date-range and
substring search (Endpoint/Name/EventType/PreviousValue/NewValue) filters. Only populated when etcd is configured or
discoverable, not scoped to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of
matching rows instead of the most recent ones.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EtcdClusterEventService.RequiredPermissions`
- **Service:** `EtcdClusterEventService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EtcdClusterEvent.cs`

### EtcdClusterStatusNow

`EtcdClusterStatusNow` is a **read** operation, idempotent, non-destructive.

Reads etcd's current member status, health, alarms and version RIGHT NOW over its own REST API for every discoverable
endpoint -- not the background sampler's last-stored row (see EtcdMemberStatusList), which can be up to a minute stale.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EtcdAdministratorService.RequiredPermissions`
- **Service:** `EtcdAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EtcdAdministrator.cs`

### EtcdDefragment

`EtcdDefragment` is a **write** operation, not idempotent, destructive.

Defragments one etcd member's own backend database file to reclaim freed space. Briefly blocks that member -- call it on
one member at a time, never the whole cluster at once. Mutating.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EtcdAdministratorService.RequiredPermissions`
- **Service:** `EtcdAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EtcdAdministrator.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### EtcdHashKv

`EtcdHashKv` is a **read** operation, idempotent, non-destructive.

Reads one etcd member's own keyspace consistency hash at its current revision, for comparing across members to catch a
diverged member -- two members at the same revision reporting different hashes have disagreed about the keyspace.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EtcdAdministratorService.RequiredPermissions`
- **Service:** `EtcdAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EtcdAdministrator.cs`

### EtcdMaintenanceStatus

`EtcdMaintenanceStatus` is a **read** operation, idempotent, non-destructive.

Reads one etcd member's raft term/index, current believed leader, and backend database size both total and in-use -- the
figure to check before running EtcdDefragment, rather than running it blind.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EtcdAdministratorService.RequiredPermissions`
- **Service:** `EtcdAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EtcdAdministrator.cs`

### EtcdMemberList

`EtcdMemberList` is a **read** operation, idempotent, non-destructive.

Reads etcd's own authoritative membership list from the given endpoint -- member id, name, peer/client URLs, and voting
vs learner status. Any one reachable member answers for the whole cluster, since membership is Raft-replicated state.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EtcdAdministratorService.RequiredPermissions`
- **Service:** `EtcdAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EtcdAdministrator.cs`

### EtcdMemberStatusList

`EtcdMemberStatusList` is a **read** operation, idempotent, non-destructive.

Lists per-minute etcd cluster member snapshots, one row per discovered/configured member, most recent first, capped at
100000. Covers raft consensus health (leader agreement, term, raft indices), disk/proposal performance, database size
and active alarms (NOSPACE/CORRUPT). Supports optional date-range and substring search
(Endpoint/Name/HealthReason/Alarms) filters. Only populated when etcd is configured or discoverable, not scoped to any
tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most
recent ones.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `EtcdMemberStatusService.RequiredPermissions`
- **Service:** `EtcdMemberStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EtcdMemberStatus.cs`

### ExhaustiveSearchInstanceCount

`ExhaustiveSearchInstanceCount` is a **read** operation, idempotent, non-destructive.

Counts the Exhaustive Search Instances matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `exhaustive-search-instance-inspector` (Exhaustive Search Instance Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.ListPermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`

### ExhaustiveSearchInstanceCreate

`ExhaustiveSearchInstanceCreate` is a **write** operation, not idempotent, non-destructive.

Registers an Exhaustive Adaptation under a Model in the caller's tenant; calling twice creates two.

- **Owning agent:** `exhaustive-search-instance-author` (Exhaustive Search Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstanceDelete

`ExhaustiveSearchInstanceDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an Exhaustive Adaptation in the caller's tenant by id.

- **Owning agent:** `exhaustive-search-instance-author` (Exhaustive Search Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstanceFilter

`ExhaustiveSearchInstanceFilter` is a **read** operation, idempotent, non-destructive.

Returns the Exhaustive Search Instances matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `exhaustive-search-instance-inspector` (Exhaustive Search Instance Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.ListPermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`

### ExhaustiveSearchInstanceFilterFields

`ExhaustiveSearchInstanceFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Exhaustive Search Instances may use.

- **Owning agent:** `exhaustive-search-instance-inspector` (Exhaustive Search Instance Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.ListPermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`

### ExhaustiveSearchInstanceGet

`ExhaustiveSearchInstanceGet` is a **read** operation, idempotent, non-destructive.

Returns one Exhaustive Adaptation by id, scoped to the caller's tenant.

- **Owning agent:** `exhaustive-search-instance-inspector` (Exhaustive Search Instance Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.ReadPermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`

### ExhaustiveSearchInstanceGetByEntityAnalysisModelId

`ExhaustiveSearchInstanceGetByEntityAnalysisModelId` is a **read** operation, idempotent, non-destructive.

Lists Exhaustive Adaptations belonging to a given Model, scoped to the caller's tenant.

- **Owning agent:** `exhaustive-search-instance-inspector` (Exhaustive Search Instance Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.ReadPermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`

### ExhaustiveSearchInstanceGetByGuid

`ExhaustiveSearchInstanceGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Exhaustive Adaptation by Guid, scoped to the caller's tenant.

- **Owning agent:** `exhaustive-search-instance-inspector` (Exhaustive Search Instance Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.ReadPermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`

### ExhaustiveSearchInstanceList

`ExhaustiveSearchInstanceList` is a **read** operation, idempotent, non-destructive.

Lists Exhaustive Adaptations for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `exhaustive-search-instance-inspector` (Exhaustive Search Instance Inspector) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.ListPermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`

### ExhaustiveSearchInstancePromotedTrialInstanceConfusionGet

`ExhaustiveSearchInstancePromotedTrialInstanceConfusionGet` is a **read** operation, idempotent, non-destructive.

Returns the confusion matrix and derived ratios for the active promoted trial instance of an Exhaustive Search Instance.

- **Owning agent:** `exhaustive-trials-and-confusion` (Exhaustive Trials and Confusion) in the Model Risk and Analytics
  domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstancePromotedTrialInstanceConfusionService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstancePromotedTrialInstanceConfusionService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotedTrialInstanceConfusion.cs`

### ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramGet

`ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramGet` is a **read** operation, idempotent, non-destructive.

Returns a ten-bin histogram of prediction errors of the active promoted trial instance of an exhaustive search instance.

- **Owning agent:** `exhaustive-trials-and-confusion` (Exhaustive Trials and Confusion) in the Model Risk and Analytics
  domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.cs`

### ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveGet

`ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveGet` is a **read** operation, idempotent, non-destructive.

Returns the score-over-time learning curve of an exhaustive search instance's promoted trial instances.

- **Owning agent:** `exhaustive-promotion-curves-and-history` (Exhaustive Promotion Curves and History) in the Model
  Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve.cs`

### ExhaustiveSearchInstancePromotedTrialInstancePredictedActualGet

`ExhaustiveSearchInstancePromotedTrialInstancePredictedActualGet` is a **read** operation, idempotent, non-destructive.

Returns predicted, actual and error values for the promoted trial instance of an exhaustive search instance.

- **Owning agent:** `exhaustive-promotion-curves-and-history` (Exhaustive Promotion Curves and History) in the Model
  Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual.cs`

### ExhaustiveSearchInstancePromotedTrialInstanceQueryGet

`ExhaustiveSearchInstancePromotedTrialInstanceQueryGet` is a **read** operation, idempotent, non-destructive.

Lists the promoted trial instances of one Exhaustive Search Instance, newest first.

- **Owning agent:** `exhaustive-promotion-curves-and-history` (Exhaustive Promotion Curves and History) in the Model
  Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstancePromotedTrialInstanceQueryService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstancePromotedTrialInstanceQueryService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotedTrialInstanceQuery.cs`

### ExhaustiveSearchInstancePromotedTrialInstanceRocGet

`ExhaustiveSearchInstancePromotedTrialInstanceRocGet` is a **read** operation, idempotent, non-destructive.

Returns the ROC curve points (score, false positive rate, true positive rate) of an Exhaustive Search Instance's active
promoted trial instance.

- **Owning agent:** `exhaustive-promotion-curves-and-history` (Exhaustive Promotion Curves and History) in the Model
  Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstancePromotedTrialInstanceRocService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstancePromotedTrialInstanceRocService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotedTrialInstanceRoc.cs`

### ExhaustiveSearchInstancePromotedTrialInstanceUpdate

`ExhaustiveSearchInstancePromotedTrialInstanceUpdate` is a **write** operation, idempotent, non-destructive.

Sets whether a promoted trial instance is promoted for production use; setting the same value twice has no further
effect.

- **Owning agent:** `exhaustive-trials-and-confusion` (Exhaustive Trials and Confusion) in the Model Risk and Analytics
  domain
- **Access:** permission 14
- **Permission source:** `ExhaustiveSearchInstancePromotedTrialInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstancePromotedTrialInstanceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotedTrialInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionGet

`ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionGet` is a **read** operation, idempotent,
non-destructive.

Lists the variables of a promoted trial instance with variable and prescription statistics and sensitivity, ordered by
sensitivity descending.

- **Owning agent:** `exhaustive-promotion-curves-and-history` (Exhaustive Promotion Curves and History) in the Model
  Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.cs`

### ExhaustiveSearchInstancePromotionHistoryGet

`ExhaustiveSearchInstancePromotionHistoryGet` is a **read** operation, idempotent, non-destructive.

Returns the promotion history of an exhaustive search instance: score, topology complexity and trials explored at each
promotion.

- **Owning agent:** `exhaustive-promotion-curves-and-history` (Exhaustive Promotion Curves and History) in the Model
  Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstancePromotionHistoryService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstancePromotionHistoryService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstancePromotionHistory.cs`

### ExhaustiveSearchInstanceSetActive

`ExhaustiveSearchInstanceSetActive` is a **write** operation, idempotent, non-destructive.

Sets whether an Exhaustive Adaptation is Active in the caller's tenant.

- **Owning agent:** `exhaustive-search-instance-author` (Exhaustive Search Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstanceStop

`ExhaustiveSearchInstanceStop` is a **write** operation, idempotent, non-destructive.

Requests that an in-progress or queued training run stop, by Guid.

- **Owning agent:** `exhaustive-search-instance-author` (Exhaustive Search Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstanceTrialInstanceDetailGet

`ExhaustiveSearchInstanceTrialInstanceDetailGet` is a **read** operation, idempotent, non-destructive.

Returns the detail of one Exhaustive trial attempt, scoped to the caller's tenant.

- **Owning agent:** `exhaustive-trials-and-confusion` (Exhaustive Trials and Confusion) in the Model Risk and Analytics
  domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceTrialInstanceListQueryService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstanceTrialInstanceListQueryService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstanceTrialInstanceListQuery.cs`

### ExhaustiveSearchInstanceTrialInstanceListQueryGet

`ExhaustiveSearchInstanceTrialInstanceListQueryGet` is a **read** operation, idempotent, non-destructive.

Lists every trial attempt of one Exhaustive Search Instance, scoped to the caller's tenant.

- **Owning agent:** `exhaustive-trials-and-confusion` (Exhaustive Trials and Confusion) in the Model Risk and Analytics
  domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceTrialInstanceListQueryService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstanceTrialInstanceListQueryService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstanceTrialInstanceListQuery.cs`

### ExhaustiveSearchInstanceTrialInstanceVariableGet

`ExhaustiveSearchInstanceTrialInstanceVariableGet` is a **read** operation, idempotent, non-destructive.

Lists the variables of the promoted trial instance of an Exhaustive Search Instance with normalisation statistics,
ordered by variable sequence.

- **Owning agent:** `exhaustive-promotion-curves-and-history` (Exhaustive Promotion Curves and History) in the Model
  Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceTrialInstanceVariableService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstanceTrialInstanceVariableService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstanceTrialInstanceVariable.cs`

### ExhaustiveSearchInstanceTrialInstanceVariableVarianceGet

`ExhaustiveSearchInstanceTrialInstanceVariableVarianceGet` is a **read** operation, idempotent, non-destructive.

Lists the multi-collinearity correlations of one Exhaustive Search Instance Variable against its test variables, ordered
by absolute correlation rank.

- **Owning agent:** `exhaustive-variable-statistics` (Exhaustive Variable Statistics) in the Model Risk and Analytics
  domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceTrialInstanceVariableVarianceService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstanceTrialInstanceVariableVarianceService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstanceTrialInstanceVariableVariance.cs`

### ExhaustiveSearchInstanceUpdate

`ExhaustiveSearchInstanceUpdate` is a **write** operation, idempotent, non-destructive.

Updates an Exhaustive Adaptation in the caller's tenant by id, only while training has not started.

- **Owning agent:** `exhaustive-search-instance-author` (Exhaustive Search Instance Author) in the Model Risk and
  Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstanceValidate

`ExhaustiveSearchInstanceValidate` is a **read** operation, idempotent, non-destructive.

Validates an Exhaustive Search Instance without saving it and returns every failure.

- **Owning agent:** `exhaustive-search-instance-test-bench` (Exhaustive Search Instance Test Bench) in the Model Risk
  and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`

### ExhaustiveSearchInstanceVariableGet

`ExhaustiveSearchInstanceVariableGet` is a **read** operation, idempotent, non-destructive.

Lists the variables of an Exhaustive Search Instance with distribution statistics and histogram bins.

- **Owning agent:** `exhaustive-trials-and-confusion` (Exhaustive Trials and Confusion) in the Model Risk and Analytics
  domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceVariableService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstanceVariableService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstanceVariable.cs`

### ExhaustiveSearchInstanceVariableReallocate

`ExhaustiveSearchInstanceVariableReallocate` is a **write** operation, idempotent, non-destructive.

Reallocates a trained variable's live name and processing type, without retraining.

- **Owning agent:** `exhaustive-trials-and-confusion` (Exhaustive Trials and Confusion) in the Model Risk and Analytics
  domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceVariableService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstanceVariableService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstanceVariable.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstanceVariableStatisticsGet

`ExhaustiveSearchInstanceVariableStatisticsGet` is a **read** operation, idempotent, non-destructive.

Lists the variables of an Exhaustive Search Instance, or one variable, with the population, positive class, negative
class and Monte Carlo prescription histograms.

- **Owning agent:** `exhaustive-variable-statistics` (Exhaustive Variable Statistics) in the Model Risk and Analytics
  domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceVariableStatisticsService.RequiredPermissions`
- **Service:** `ExhaustiveSearchInstanceVariableStatisticsService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstanceVariableStatistics.cs`

### GetAdvancedAnalyticsTrainingRunResult

`GetAdvancedAnalyticsTrainingRunResult` is a **read** operation, idempotent, non-destructive.

Reads the outcome of a finished training run: its final status and, once succeeded, the trained model and version plus
family-specific evidence.

- **Owning agent:** `advanced-analytics-training-classifier-trees` (Advanced Analytics Training Classifier Trees) in the
  Model Risk and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/AdvancedAnalyticsTrainingRunTools.cs`

### GetAdvancedAnalyticsTrainingRunStatus

`GetAdvancedAnalyticsTrainingRunStatus` is a **read** operation, idempotent, non-destructive.

Reads where a queued training run has got to: Pending, Running, then Succeeded, Failed or Cancelled; works for a run
submitted by any family's Submit tool.

- **Owning agent:** `advanced-analytics-training-classifier-trees` (Advanced Analytics Training Classifier Trees) in the
  Model Risk and Analytics domain
- **Access:** permission 48
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/AdvancedAnalyticsTrainingRunTools.cs`

### HAProxyReachabilityProbeList

`HAProxyReachabilityProbeList` is a **read** operation, idempotent, non-destructive.

Lists per-minute synthetic HAProxy reachability probes. Landlord only.

- **Owning agent:** `containers-load-balancer-and-overlay-network` (Containers, Load Balancer and Overlay Network) in
  the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `HaProxyReachabilityProbeService.RequiredPermissions`
- **Service:** `HaProxyReachabilityProbeService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.HaProxyReachabilityProbe.cs`

### HAProxyServerStatusList

`HAProxyServerStatusList` is a **read** operation, idempotent, non-destructive.

Lists per-minute HAProxy backend server status snapshots. Landlord only.

- **Owning agent:** `containers-load-balancer-and-overlay-network` (Containers, Load Balancer and Overlay Network) in
  the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `HaProxyServerStatusService.RequiredPermissions`
- **Service:** `HaProxyServerStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.HaProxyServerStatus.cs`

### HttpProcessingCounterList

`HttpProcessingCounterList` is a **read** operation, idempotent, non-destructive.

Lists platform-wide HTTP processing counters, most recent first, capped at 100000, with optional date-range filters.
Each row is a roughly one-minute interval's counts of HTTP requests by how they were routed -- Model (synchronous),
AsynchronousModel, Tag, Error, Sanction (direct recall), Exhaustive (direct recall), and All (every request received).
Not scoped to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows
instead of the most recent ones.

- **Owning agent:** `invoke-path-performance-and-warnings` (Invoke Path Performance and Warnings) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `HttpProcessingCounterService.RequiredPermissions`
- **Service:** `HttpProcessingCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.HttpProcessingCounter.cs`

### IconsGet

`IconsGet` is a **read** operation, idempotent, non-destructive.

Lists the icon files available to the user interface.

- **Owning agent:** `authoring-aids-and-model-role-grants` (Authoring Aids and Model Role Grants) in the Model
  Supporting Structures domain
- **Access:** permission 24
- **Permission source:** `IconsService.RequiredPermissions`
- **Service:** `IconsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Icons.cs`

### ModelInvokeWarningList

`ModelInvokeWarningList` is a **read** operation, idempotent, non-destructive.

Lists captured invocation warn-threshold breaches -- the trace point message and timing gap that exceeded a model's Warn
Threshold Milliseconds, most recent first, capped at 100000, with optional model/date-range/substring filters. Only
populated for models with Enable Logs and Enable Logs: Warn Threshold both on. Also accepts samplePercentage (0-100) to
draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `invoke-path-performance-and-warnings` (Invoke Path Performance and Warnings) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `ModelInvokeWarningService.RequiredPermissions`
- **Service:** `ModelInvokeWarningService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ModelInvokeWarning.cs`

### OpenTelemetryExcludeCreate

`OpenTelemetryExcludeCreate` is a **write** operation, not idempotent, non-destructive.

Registers an OpenTelemetry instrument/counter name to stop exporting; calling twice creates two rows.

- **Owning agent:** `open-telemetry-exclude` (Open Telemetry Exclude) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryExcludeService.RequiredPermissions`
- **Service:** `OpenTelemetryExcludeService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryExclude.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### OpenTelemetryExcludeDelete

`OpenTelemetryExcludeDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes an exclusion row by id; the named instrument becomes eligible for export again.

- **Owning agent:** `open-telemetry-exclude` (Open Telemetry Exclude) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryExcludeService.RequiredPermissions`
- **Service:** `OpenTelemetryExcludeService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryExclude.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### OpenTelemetryExcludeGet

`OpenTelemetryExcludeGet` is a **read** operation, idempotent, non-destructive.

Returns one OpenTelemetryExclude row by id.

- **Owning agent:** `open-telemetry-exclude` (Open Telemetry Exclude) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryExcludeService.RequiredPermissions`
- **Service:** `OpenTelemetryExcludeService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryExclude.cs`

### OpenTelemetryExcludeList

`OpenTelemetryExcludeList` is a **read** operation, idempotent, non-destructive.

Lists every OpenTelemetryExclude row, most recent first. Not scoped to any tenant.

- **Owning agent:** `open-telemetry-exclude` (Open Telemetry Exclude) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryExcludeService.RequiredPermissions`
- **Service:** `OpenTelemetryExcludeService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryExclude.cs`

### OpenTelemetryExcludeUpdate

`OpenTelemetryExcludeUpdate` is a **write** operation, idempotent, non-destructive.

Updates an existing exclusion row in place.

- **Owning agent:** `open-telemetry-exclude` (Open Telemetry Exclude) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryExcludeService.RequiredPermissions`
- **Service:** `OpenTelemetryExcludeService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryExclude.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### OpenTelemetryExcludeValidate

`OpenTelemetryExcludeValidate` is a **read** operation, idempotent, non-destructive.

Validates an Open Telemetry Exclude without saving it and returns every failure.

- **Owning agent:** `open-telemetry-exclude` (Open Telemetry Exclude) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryExcludeService.RequiredPermissions`
- **Service:** `OpenTelemetryExcludeService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryExclude.cs`

### OpenTelemetryLogCounterCreate

`OpenTelemetryLogCounterCreate` is a **write** operation, not idempotent, non-destructive.

Registers a rule that increments an OpenTelemetry counter named Name whenever Regex matches an incoming unstructured log
line; calling twice creates two rows.

- **Owning agent:** `telemetry-log-counters-and-runtime-metrics` (Telemetry Log Counters and Runtime Metrics) in the
  Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryLogCounterService.RequiredPermissions`
- **Service:** `OpenTelemetryLogCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryLogCounter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### OpenTelemetryLogCounterDelete

`OpenTelemetryLogCounterDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a rule by id; it immediately stops being checked against incoming log lines.

- **Owning agent:** `telemetry-log-counters-and-runtime-metrics` (Telemetry Log Counters and Runtime Metrics) in the
  Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryLogCounterService.RequiredPermissions`
- **Service:** `OpenTelemetryLogCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryLogCounter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### OpenTelemetryLogCounterGet

`OpenTelemetryLogCounterGet` is a **read** operation, idempotent, non-destructive.

Returns one OpenTelemetryLogCounter rule by id.

- **Owning agent:** `telemetry-log-counters-and-runtime-metrics` (Telemetry Log Counters and Runtime Metrics) in the
  Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryLogCounterService.RequiredPermissions`
- **Service:** `OpenTelemetryLogCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryLogCounter.cs`

### OpenTelemetryLogCounterList

`OpenTelemetryLogCounterList` is a **read** operation, idempotent, non-destructive.

Lists every OpenTelemetryLogCounter rule (Name + Regex), most recent first. Not scoped to any tenant.

- **Owning agent:** `telemetry-log-counters-and-runtime-metrics` (Telemetry Log Counters and Runtime Metrics) in the
  Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryLogCounterService.RequiredPermissions`
- **Service:** `OpenTelemetryLogCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryLogCounter.cs`

### OpenTelemetryLogCounterUpdate

`OpenTelemetryLogCounterUpdate` is a **write** operation, idempotent, non-destructive.

Updates an existing rule in place.

- **Owning agent:** `telemetry-log-counters-and-runtime-metrics` (Telemetry Log Counters and Runtime Metrics) in the
  Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryLogCounterService.RequiredPermissions`
- **Service:** `OpenTelemetryLogCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryLogCounter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### OpenTelemetryLogCounterValidate

`OpenTelemetryLogCounterValidate` is a **read** operation, idempotent, non-destructive.

Validates an Open Telemetry Log Counter without saving it and returns every failure.

- **Owning agent:** `telemetry-log-counters-and-runtime-metrics` (Telemetry Log Counters and Runtime Metrics) in the
  Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryLogCounterService.RequiredPermissions`
- **Service:** `OpenTelemetryLogCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryLogCounter.cs`

### OpenTelemetryMetricList

`OpenTelemetryMetricList` is a **read** operation, idempotent, non-destructive.

Lists locally-aggregated OpenTelemetry metrics -- everything already recorded onto ServiceDiagnostics/EngineDiagnostics'
Meters (jube.service.operation. *, jube.engine.stage.duration, jube.engine.sampler.*, ...), summarised once a minute
into Count/Sum/Min/Max per distinct (MetricName, Tags) combination, available regardless of whether
EnableOpenTelemetry/an external collector is configured. Most recent first, capped at 100000, with optional date-range
and substring search (MetricName/Tags) filters, not scoped to any tenant. Also accepts samplePercentage (0-100) to draw
an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `telemetry-metrics-and-otlp-dispatch` (Telemetry Metrics and OTLP Dispatch) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OpenTelemetryMetricService.RequiredPermissions`
- **Service:** `OpenTelemetryMetricService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OpenTelemetryMetric.cs`

### OtlpDispatchCounterList

`OtlpDispatchCounterList` is a **read** operation, idempotent, non-destructive.

Lists per-minute OTLP export dispatch counters -- one row per distinct Signal ("traces"/"metrics"/"logs") per minute,
with Count/SuccessCount/FailureCount/ItemCount/DroppedCount and Total/Min/MaxMicroseconds for that window, most recent
first, capped at 100000. Answers how OTLP export to OpenTelemetryBackendEndpoint is behaving -- dispatch volume,
response times, outright failures, and drops due to a full export queue. Supports optional date-range and substring
search (Signal) filters, not scoped to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random
subset of matching rows instead of the most recent ones.

- **Owning agent:** `telemetry-metrics-and-otlp-dispatch` (Telemetry Metrics and OTLP Dispatch) in the Platform
  Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OtlpDispatchCounterService.RequiredPermissions`
- **Service:** `OtlpDispatchCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OtlpDispatchCounter.cs`

### OverlayNetworkTaskDriftList

`OverlayNetworkTaskDriftList` is a **read** operation, idempotent, non-destructive.

Lists per-minute overlay network and DNS reconciliation checks. Landlord only.

- **Owning agent:** `containers-load-balancer-and-overlay-network` (Containers, Load Balancer and Overlay Network) in
  the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `OverlayNetworkTaskDriftService.RequiredPermissions`
- **Service:** `OverlayNetworkTaskDriftService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.OverlayNetworkTaskDrift.cs`

### ParserParse

`ParserParse` is a **read** operation, idempotent, non-destructive.

Parses and compiles a user-authored rule against an Entity Analysis Model and returns any errors.

- **Owning agent:** `authoring-aids-and-model-role-grants` (Authoring Aids and Model Role Grants) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `ParserService.RequiredPermissions`
- **Service:** `ParserService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.Parser.cs`

### PatroniClusterEventList

`PatroniClusterEventList` is a **read** operation, idempotent, non-destructive.

Lists captured Patroni cluster events: RoleChanged/StateChanged/TimelineChanged (detected by diffing consecutive
one-minute PatroniMemberStatus samples), and Failover (read directly from Patroni's own DCS 'history' key in etcd, with
Reason/TimelineId/LsnBytes -- the authoritative record). Most recent first, capped at 100000. Supports optional
date-range and substring search (Scope/Name/EventType/PreviousValue/NewValue/Reason) filters. Only populated when
Patroni is configured or discoverable (Failover rows additionally require etcd as the DCS), not scoped to any tenant.
Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most recent
ones.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PatroniClusterEventService.RequiredPermissions`
- **Service:** `PatroniClusterEventService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PatroniClusterEvent.cs`

### PatroniClusterStatusNow

`PatroniClusterStatusNow` is a **read** operation, idempotent, non-destructive.

Reads every discoverable Patroni member's status RIGHT NOW over its own REST API -- role, state, timeline, lag, pending
restart -- not the background sampler's last-stored row (see PatroniMemberStatusList), which can be up to a minute
stale.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PatroniAdministratorService.RequiredPermissions`
- **Service:** `PatroniAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PatroniAdministrator.cs`

### PatroniConfigGet

`PatroniConfigGet` is a **read** operation, idempotent, non-destructive.

Reads a Patroni member's own resolved dynamic configuration (DCS-stored config merged with whatever is local to that
node) as plain JSON text. Read-only.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PatroniAdministratorService.RequiredPermissions`
- **Service:** `PatroniAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PatroniAdministrator.cs`

### PatroniMemberStatusList

`PatroniMemberStatusList` is a **read** operation, idempotent, non-destructive.

Lists per-minute Patroni-managed Postgres member snapshots, one row per discovered/configured member, most recent first,
capped at 100000. Covers role/state, Patroni's own computed replication lag, timeline, pending restarts, and
version/identity fields useful for spotting a diverged member. Supports optional date-range and substring search
(Name/Host/Role/State/Scope) filters. Only populated when Patroni is configured or discoverable, not scoped to any
tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most
recent ones.

- **Owning agent:** `etcd-and-patroni-cluster-state` (Etcd and Patroni Cluster State) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PatroniMemberStatusService.RequiredPermissions`
- **Service:** `PatroniMemberStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PatroniMemberStatus.cs`

### PermissionSpecificationList

`PermissionSpecificationList` is a **read** operation, idempotent, non-destructive.

Lists rows in the global Permission Specification catalogue, keyset-paged and capped.

- **Owning agent:** `permission-specification` (Permission Specification) in the Access Control and Tenancy domain
- **Access:** permission 36
- **Permission source:** `PermissionSpecificationService.RequiredPermissions`
- **Service:** `PermissionSpecificationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PermissionSpecification.cs`

### PostgresActivityList

`PostgresActivityList` is a **read** operation, idempotent, non-destructive.

Lists every backend currently known to PostgreSQL (pg_stat_activity), live -- not a stored snapshot, so results reflect
only the instant this is called. Includes each row's blocking chain (pg_blocking_pids) and current query/transaction
duration. Optional case-insensitive substring search against username, application name or query text. Read-only.

- **Owning agent:** `postgres-observability` (Postgres Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PostgresActivityService.RequiredPermissions`
- **Service:** `PostgresActivityService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PostgresActivity.cs`

### PostgresIndexStatisticsList

`PostgresIndexStatisticsList` is a **read** operation, idempotent, non-destructive.

Lists live per-index Postgres statistics for every user index (scan counts, size, unique/primary-key/unused flags),
queried live -- not stored history, not scoped to any tenant.

- **Owning agent:** `postgres-observability` (Postgres Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PostgresIndexStatisticsService.RequiredPermissions`
- **Service:** `PostgresIndexStatisticsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PostgresIndexStatistics.cs`

### PostgresLockList

`PostgresLockList` is a **read** operation, idempotent, non-destructive.

Lists every lock PostgreSQL currently knows about (pg_locks), live. Ungranted rows -- a backend waiting on another
one -- sort first. Complements PostgresActivityList's BlockedByPids (who is blocked) with what is actually being
contended (which relation, which lock mode). Optional case-insensitive substring search against username, query text or
relation name. Read-only.

- **Owning agent:** `postgres-observability` (Postgres Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PostgresLockService.RequiredPermissions`
- **Service:** `PostgresLockService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PostgresLock.cs`

### PostgresLogEntryList

`PostgresLogEntryList` is a **read** operation, idempotent, non-destructive.

Lists Postgres's own server log lines (checkpoints, connection issues, deadlocks, crashes, slow autovacuum, ...), most
recent first, capped at 100000, with optional date-range and substring search (Message/Level) filters. Requires
logging_collector = on server-side, otherwise always empty. Not scoped to any Model or tenant. Also accepts
samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `postgres-observability` (Postgres Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PostgresLogEntryService.RequiredPermissions`
- **Service:** `PostgresLogEntryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PostgresLogEntry.cs`

### PostgresMetricList

`PostgresMetricList` is a **read** operation, idempotent, non-destructive.

Lists per-minute Postgres metric snapshots across every node, most recent first, capped at 100000, with optional
date-range and substring search (Instance) filters, not scoped to any tenant. Also accepts samplePercentage (0-100) to
draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `postgres-observability` (Postgres Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PostgresMetricService.RequiredPermissions`
- **Service:** `PostgresMetricService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PostgresMetric.cs`

### PostgresReplicationStatusList

`PostgresReplicationStatusList` is a **read** operation, idempotent, non-destructive.

Lists per-minute pg_stat_replication snapshots, one row per connected standby, most recent first, capped at 100000, with
optional date-range and substring search (UserName/ApplicationName/ClientAddress/State/SyncState) filters, not scoped to
any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most
recent ones.

- **Owning agent:** `postgres-observability` (Postgres Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PostgresReplicationStatusService.RequiredPermissions`
- **Service:** `PostgresReplicationStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PostgresReplicationStatus.cs`

### PostgresStatementStatisticsList

`PostgresStatementStatisticsList` is a **read** operation, idempotent, non-destructive.

Lists per-query-shape aggregate statistics from pg_stat_statements -- calls, rows, execution time min/mean/max/stddev,
cache-hit vs disk-read bytes, temp-file spill bytes and WAL bytes, accumulated across all history since the extension
was created or last reset. Complements PostgresActivityList: that is 'what is running right now', this is 'which query
shape is worst overall'. Optional case-insensitive substring search against username, database name or query text.
Read-only.

- **Owning agent:** `postgres-observability` (Postgres Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PostgresStatementStatisticsService.RequiredPermissions`
- **Service:** `PostgresStatementStatisticsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PostgresStatementStatistics.cs`

### PostgresTableStatisticsList

`PostgresTableStatisticsList` is a **read** operation, idempotent, non-destructive.

Lists live per-table Postgres statistics for every user table (scans, row estimates, sizes, vacuum/analyze history, a
LikelyMissingIndex heuristic), queried live -- not stored history, not scoped to any tenant.

- **Owning agent:** `postgres-observability` (Postgres Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `PostgresTableStatisticsService.RequiredPermissions`
- **Service:** `PostgresTableStatisticsService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.PostgresTableStatistics.cs`

### QueryBuilderOperatorsList

`QueryBuilderOperatorsList` is a **read** operation, idempotent, non-destructive.

Lists every operator the rule builder offers, with the rule text each produces, the values it takes and the field data
types it applies to.

- **Owning agent:** `completions` (Completions) in the Model Supporting Structures domain
- **Access:** **none**
- **Permission source:** `QueryBuilderService.RequiredPermissions`
- **Service:** `QueryBuilderService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.QueryBuilder.cs`

### RedisCallCounterList

`RedisCallCounterList` is a **read** operation, idempotent, non-destructive.

Lists per-minute Redis call counters -- one row per distinct "call" label (a Jube.Cache repository method, e.g.
"CachePayloadRepository.InsertAsync") per minute, with Count/TotalMicroseconds/MinMicroseconds/MaxMicroseconds for that
window, most recent first, capped at 100000. The dedicated, reset-each-minute counterpart to the live
jube.cache.redis.call.count/.duration OpenTelemetry instruments, answering exactly where Redis load is coming from by
area of the system. Supports optional date-range and substring search (Call) filters, not scoped to any tenant. Also
accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisCallCounterService.RequiredPermissions`
- **Service:** `RedisCallCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisCallCounter.cs`

### RedisConnectionEventList

`RedisConnectionEventList` is a **read** operation, idempotent, non-destructive.

Lists captured CacheService ConnectionMultiplexer events (ConnectionFailed, ConnectionRestored, ErrorMessage,
InternalError, ConfigurationChanged, ConfigurationChangedBroadcast), most recent first, capped at 100000.
RedisConnectionMultiplexerMetric's per-minute counts of these same six event types only answer whether one tripped; this
is the detail (endpoint, connection type, failure type, exception) behind it. Supports optional date-range and substring
search (EventType/EndPoint/FailureType/Message/Exception) filters, not scoped to any tenant. Also accepts
samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisConnectionEventService.RequiredPermissions`
- **Service:** `RedisConnectionEventService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisConnectionEvent.cs`

### RedisConnectionMultiplexerMetricList

`RedisConnectionMultiplexerMetricList` is a **read** operation, idempotent, non-destructive.

Lists per-minute snapshots of the application's own live Redis connection health and queue depth across every node, most
recent first, capped at 100000, with optional date-range and substring search (Instance/ClientName) filters, not scoped
to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the
most recent ones.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisConnectionMultiplexerMetricService.RequiredPermissions`
- **Service:** `RedisConnectionMultiplexerMetricService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisConnectionMultiplexerMetric.cs`

### RedisKeyActivityList

`RedisKeyActivityList` is a **read** operation, idempotent, non-destructive.

Samples up to 100 keys from the live Redis keyspace (SCAN, never KEYS) and reports each one's type, idle time, and --
only under an LFU maxmemory-policy -- its approximate access frequency. The 100-key cap is fixed. Optional glob pattern
narrows the sample. For spotting a hot/cold key design problem, never for enumerating the keyspace. Read-only.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisKeyActivityService.RequiredPermissions`
- **Service:** `RedisKeyActivityService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisKeyActivity.cs`

### RedisMetricList

`RedisMetricList` is a **read** operation, idempotent, non-destructive.

Lists per-minute Redis metric snapshots across every node, most recent first, capped at 100000, with optional date-range
and substring search (Instance) filters, not scoped to any tenant. Also accepts samplePercentage (0-100) to draw an
unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisMetricService.RequiredPermissions`
- **Service:** `RedisMetricService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisMetric.cs`

### RedisSentinelCheckQuorum

`RedisSentinelCheckQuorum` is a **read** operation, idempotent, non-destructive.

Reads whether enough Sentinels currently agree a failover of the given master could actually succeed right now (SENTINEL
CKQUORUM) -- the real pre-flight check before anyone reaches for RedisSentinelFailover. Returns Sentinel's own reply
text verbatim, e.g. "OK 3 usable Sentinels" or a NOQUORUM message explaining the gap.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisSentinelAdministratorService.RequiredPermissions`
- **Service:** `RedisSentinelAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisSentinelAdministrator.cs`

### RedisSentinelClusterStatusNow

`RedisSentinelClusterStatusNow` is a **read** operation, idempotent, non-destructive.

Reads every master/slave/sentinel Sentinel currently reports RIGHT NOW (SENTINEL MASTERS/SLAVES/SENTINELS), not the
background sampler's last-stored row (see RedisSentinelStatusList), which can be up to a minute stale. Empty when
Sentinel is not configured (RedisConnectionString carries no ServiceName).

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisSentinelAdministratorService.RequiredPermissions`
- **Service:** `RedisSentinelAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisSentinelAdministrator.cs`

### RedisSentinelEventList

`RedisSentinelEventList` is a **read** operation, idempotent, non-destructive.

Lists captured Redis Sentinel pub/sub events (every channel Sentinel publishes -- +sdown, -sdown, +odown,
+switch-master, +failover-state, +slave, +sentinel, and more), most recent first, capped at 100000, with optional
date-range and substring search (Channel/Message) filters. Only populated when Sentinel is actually in use, not scoped
to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the
most recent ones.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisSentinelEventService.RequiredPermissions`
- **Service:** `RedisSentinelEventService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisSentinelEvent.cs`

### RedisSentinelFailover

`RedisSentinelFailover` is a **write** operation, not idempotent, destructive.

Forces a manual failover of the given master right now (SENTINEL FAILOVER) -- the same action Sentinel's own design
exists to let an operator trigger under a real incident. Check RedisSentinelCheckQuorum first: a failover attempted
without quorum will not succeed. Sentinel answers once the failover has been started, not once it has finished -- follow
up with RedisSentinelClusterStatusNow to confirm the new master. Mutating.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisSentinelAdministratorService.RequiredPermissions`
- **Service:** `RedisSentinelAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisSentinelAdministrator.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### RedisSentinelGetMasterAddress

`RedisSentinelGetMasterAddress` is a **read** operation, idempotent, non-destructive.

Reads Sentinel's own current, authoritative address for the given master (SENTINEL GET-MASTER-ADDR-BY-NAME), as "host:
port" -- null if Sentinel does not know this master name.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisSentinelAdministratorService.RequiredPermissions`
- **Service:** `RedisSentinelAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisSentinelAdministrator.cs`

### RedisSentinelReset

`RedisSentinelReset` is a **write** operation, not idempotent, destructive.

Forgets Sentinel's current state of every master matching the given glob pattern (SENTINEL RESET), forcing Sentinel to
rediscover it from scratch. Self-healing and non-destructive to data -- Sentinel simply re-learns what it already
knew -- but still a real, mutating call against a live Sentinel. Returns how many masters were reset.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisSentinelAdministratorService.RequiredPermissions`
- **Service:** `RedisSentinelAdministratorService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisSentinelAdministrator.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### RedisSentinelStatusList

`RedisSentinelStatusList` is a **read** operation, idempotent, non-destructive.

Lists per-minute Redis Sentinel topology snapshots (SENTINEL MASTERS/SLAVES/SENTINELS), one row per
master/slave/sentinel entity, most recent first, capped at 100000. Slave rows carry ReplicationLagSeconds (seconds since
last ACK, read from the master's own INFO reply since Sentinel itself has no lag figure). Supports optional date-range
and substring search (EntityType/Name/Ip/Flags/MasterLinkStatus) filters. Only populated when Sentinel is actually in
use, not scoped to any tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows
instead of the most recent ones.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisSentinelStatusService.RequiredPermissions`
- **Service:** `RedisSentinelStatusService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisSentinelStatus.cs`

### RedisSlowOperationList

`RedisSlowOperationList` is a **read** operation, idempotent, non-destructive.

Lists Redis SLOWLOG entries across every node, most recent first, capped at 100000, with optional date-range and
substring search (Command/CommandName/KeyName/ClientAddress/ClientName) filters, not scoped to any tenant. Also accepts
samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `redis-observability` (Redis Observability) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `RedisSlowOperationService.RequiredPermissions`
- **Service:** `RedisSlowOperationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RedisSlowOperation.cs`

### RoleRegistryCount

`RoleRegistryCount` is a **read** operation, idempotent, non-destructive.

Counts the Role Registrys matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `role-registry-inspector` (Role Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33
- **Permission source:** `RoleRegistryService.ListPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`

### RoleRegistryCreate

`RoleRegistryCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Role; calling twice creates two.

- **Owning agent:** `role-registry-author` (Role Registry Author) in the Access Control and Tenancy domain
- **Access:** permission 34
- **Permission source:** `RoleRegistryService.RequiredPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### RoleRegistryDelete

`RoleRegistryDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Role; reversible only by direct database action.

- **Owning agent:** `role-registry-author` (Role Registry Author) in the Access Control and Tenancy domain
- **Access:** permission 34
- **Permission source:** `RoleRegistryService.RequiredPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### RoleRegistryFilter

`RoleRegistryFilter` is a **read** operation, idempotent, non-destructive.

Returns the Role Registrys matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `role-registry-inspector` (Role Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33
- **Permission source:** `RoleRegistryService.ListPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`

### RoleRegistryFilterFields

`RoleRegistryFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Role Registrys may use.

- **Owning agent:** `role-registry-inspector` (Role Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33
- **Permission source:** `RoleRegistryService.ListPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`

### RoleRegistryGet

`RoleRegistryGet` is a **read** operation, idempotent, non-destructive.

Returns a single Role by Id.

- **Owning agent:** `role-registry-inspector` (Role Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 34
- **Permission source:** `RoleRegistryService.RequiredPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`

### RoleRegistryGetByGuid

`RoleRegistryGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Role by Id.

- **Owning agent:** `role-registry-inspector` (Role Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 34
- **Permission source:** `RoleRegistryService.RequiredPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`

### RoleRegistryList

`RoleRegistryList` is a **read** operation, idempotent, non-destructive.

Lists Roles visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `role-registry-inspector` (Role Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33
- **Permission source:** `RoleRegistryService.ListPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`

### RoleRegistryPermissionCount

`RoleRegistryPermissionCount` is a **read** operation, idempotent, non-destructive.

Counts the Role Registry Permissions matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `role-registry-permission-inspector` (Role Registry Permission Inspector) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`

### RoleRegistryPermissionCreate

`RoleRegistryPermissionCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Permission Specification to a Role Registry; calling twice creates two grants.

- **Owning agent:** `role-registry-permission-author` (Role Registry Permission Author) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### RoleRegistryPermissionDelete

`RoleRegistryPermissionDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Role Registry Permission grant; reversible only by direct database action.

- **Owning agent:** `role-registry-permission-author` (Role Registry Permission Author) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### RoleRegistryPermissionFilter

`RoleRegistryPermissionFilter` is a **read** operation, idempotent, non-destructive.

Returns the Role Registry Permissions matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `role-registry-permission-inspector` (Role Registry Permission Inspector) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`

### RoleRegistryPermissionFilterFields

`RoleRegistryPermissionFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Role Registry Permissions may use.

- **Owning agent:** `role-registry-permission-inspector` (Role Registry Permission Inspector) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`

### RoleRegistryPermissionGet

`RoleRegistryPermissionGet` is a **read** operation, idempotent, non-destructive.

Returns a single Role Registry Permission grant by Id.

- **Owning agent:** `role-registry-permission-inspector` (Role Registry Permission Inspector) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`

### RoleRegistryPermissionGetByGuid

`RoleRegistryPermissionGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Role Registry Permission grant by Id.

- **Owning agent:** `role-registry-permission-inspector` (Role Registry Permission Inspector) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`

### RoleRegistryPermissionList

`RoleRegistryPermissionList` is a **read** operation, idempotent, non-destructive.

Lists Role Registry Permission grants visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `role-registry-permission-inspector` (Role Registry Permission Inspector) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`

### RoleRegistryPermissionUpdate

`RoleRegistryPermissionUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Role Registry Permission grant; writes a version-audit row.

- **Owning agent:** `role-registry-permission-author` (Role Registry Permission Author) in the Access Control and
  Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### RoleRegistryPermissionValidate

`RoleRegistryPermissionValidate` is a **read** operation, idempotent, non-destructive.

Validates a Role Registry Permission without saving it and returns every failure.

- **Owning agent:** `role-registry-permission-test-bench` (Role Registry Permission Test Bench) in the Access Control
  and Tenancy domain
- **Access:** permission 36
- **Permission source:** `RoleRegistryPermissionService.RequiredPermissions`
- **Service:** `RoleRegistryPermissionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistryPermission.cs`

### RoleRegistryUpdate

`RoleRegistryUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Role; overwrites, writes a version-audit row.

- **Owning agent:** `role-registry-author` (Role Registry Author) in the Access Control and Tenancy domain
- **Access:** permission 34
- **Permission source:** `RoleRegistryService.RequiredPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### RoleRegistryValidate

`RoleRegistryValidate` is a **read** operation, idempotent, non-destructive.

Validates a Role Registry without saving it and returns every failure.

- **Owning agent:** `role-registry-test-bench` (Role Registry Test Bench) in the Access Control and Tenancy domain
- **Access:** permission 34
- **Permission source:** `RoleRegistryService.RequiredPermissions`
- **Service:** `RoleRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RoleRegistry.cs`

### RuleVocabularyDescribe

`RuleVocabularyDescribe` is a **read** operation, idempotent, non-destructive.

Describes a rule word, with every function signature.

- **Owning agent:** `authoring-aids-and-model-role-grants` (Authoring Aids and Model Role Grants) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `RuleVocabularyService.RequiredPermissions`
- **Service:** `RuleVocabularyService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RuleVocabulary.cs`

### RuleVocabularyList

`RuleVocabularyList` is a **read** operation, idempotent, non-destructive.

Lists the keywords, allowed words and rule functions rule text may use.

- **Owning agent:** `authoring-aids-and-model-role-grants` (Authoring Aids and Model Role Grants) in the Model
  Supporting Structures domain
- **Access:** permission 8, 10, 13, 14, 16, 17, 25, 26
- **Permission source:** `RuleVocabularyService.RequiredPermissions`
- **Service:** `RuleVocabularyService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.RuleVocabulary.cs`

### SanctionEntrySourceCount

`SanctionEntrySourceCount` is a **read** operation, idempotent, non-destructive.

Counts the Sanction Entry Sources matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `sanctions-entry-source` (Sanctions Entry Source) in the Case Investigation Desk domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `SanctionEntrySourceService.RequiredPermissions`
- **Service:** `SanctionEntrySourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.SanctionsEntrySource.cs`

### SanctionEntrySourceFilter

`SanctionEntrySourceFilter` is a **read** operation, idempotent, non-destructive.

Returns the Sanction Entry Sources matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `sanctions-entry-source` (Sanctions Entry Source) in the Case Investigation Desk domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `SanctionEntrySourceService.RequiredPermissions`
- **Service:** `SanctionEntrySourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.SanctionsEntrySource.cs`

### SanctionEntrySourceFilterFields

`SanctionEntrySourceFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Sanction Entry Sources may use.

- **Owning agent:** `sanctions-entry-source` (Sanctions Entry Source) in the Case Investigation Desk domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `SanctionEntrySourceService.RequiredPermissions`
- **Service:** `SanctionEntrySourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.SanctionsEntrySource.cs`

### SanctionEntrySourceList

`SanctionEntrySourceList` is a **read** operation, idempotent, non-destructive.

Lists the configured Sanction Entry Sources (e.g. SDN, BOE, EU) the Sanctions Loader can poll or manually import from,
each identified by Id and Name. Landlord-only. Capped at 'take' rows (max 200); use 'afterId' to page.

- **Owning agent:** `sanctions-entry-source` (Sanctions Entry Source) in the Case Investigation Desk domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `SanctionEntrySourceService.RequiredPermissions`
- **Service:** `SanctionEntrySourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.SanctionsEntrySource.cs`

### SessionCaseJournalCreate

`SessionCaseJournalCreate` is a **write** operation, idempotent, non-destructive.

Saves the calling user's own journal for a CaseWorkflow, replacing any earlier save.

- **Owning agent:** `session-case-observability` (Session Case Observability) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `SessionCaseJournalService.RequiredPermissions`
- **Service:** `SessionCaseJournalService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.SessionCaseJournal.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### SessionCaseJournalGetByCaseWorkflowGuid

`SessionCaseJournalGetByCaseWorkflowGuid` is a **read** operation, idempotent, non-destructive.

Gets the calling user's own saved journal (e.g. case grid column layout) for a CaseWorkflow.

- **Owning agent:** `session-case-observability` (Session Case Observability) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `SessionCaseJournalService.RequiredPermissions`
- **Service:** `SessionCaseJournalService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.SessionCaseJournal.cs`

### SessionCaseSearchCompiledSqlGetLast

`SessionCaseSearchCompiledSqlGetLast` is a **read** operation, idempotent, non-destructive.

Returns the caller's own most recently compiled Session Case Search (or a NotFound placeholder); a search flagged for
rebuild is recompiled first.

- **Owning agent:** `session-case-observability` (Session Case Observability) in the Case Investigation Desk domain
- **Access:** permission 1
- **Permission source:** `SessionCaseSearchCompiledSqlService.RequiredPermissions`
- **Service:** `SessionCaseSearchCompiledSqlService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.SessionCaseSearchCompiledSql.cs`

### SubmitAdaBoostClassifierTraining

`SubmitAdaBoostClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues an AdaBoost classification training run: fits shallow trees one after another, each reweighted onto the previous
rounds' mistakes, and Platt-calibrates the alpha-weighted vote margin.

- **Owning agent:** `advanced-analytics-training-classifier-trees` (Advanced Analytics Training Classifier Trees) in the
  Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/AdaBoostClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitBaggingClassifierTraining

`SubmitBaggingClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a bagging classification training run: averages many unrestricted decision trees, each grown on its own bootstrap
sample, into a Platt-calibrated probability.

- **Owning agent:** `advanced-analytics-training-classifier-trees` (Advanced Analytics Training Classifier Trees) in the
  Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/BaggingClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitBalancedKMeansClustererTraining

`SubmitBalancedKMeansClustererTraining` is a **write** operation, not idempotent, non-destructive.

Queues a balanced k-means clustering training run, constraining cluster sizes to as close to equal as the row count
allows via an optimal-assignment step.

- **Owning agent:** `advanced-analytics-training-clusterer-centroid` (Advanced Analytics Training Clusterer Centroid) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/BalancedKMeansClustererTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitBinarySplitClustererTraining

`SubmitBinarySplitClustererTraining` is a **write** operation, not idempotent, non-destructive.

Queues a binary split clustering training run, repeatedly bisecting whichever existing cluster has the largest
distortion until k clusters exist.

- **Owning agent:** `advanced-analytics-training-clusterer-density` (Advanced Analytics Training Clusterer Density) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/BinarySplitClustererTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitDecisionTreeClassifierTraining

`SubmitDecisionTreeClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a decision tree classification training run: fits nested yes/no threshold splits that best separate the event of
interest, with the leaf's calibrated share of positive rows as the recalled probability.

- **Owning agent:** `advanced-analytics-training-classifier-trees` (Advanced Analytics Training Classifier Trees) in the
  Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/DecisionTreeClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitDecisionTreeRegressorTraining

`SubmitDecisionTreeRegressorTraining` is a **write** operation, not idempotent, non-destructive.

Queues a decision tree regression training run, fitting nested threshold splits that most reduce the outcome's variance.

- **Owning agent:** `advanced-analytics-training-regressor` (Advanced Analytics Training Regressor) in the Model Risk
  and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/DecisionTreeRegressorTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitGaussianMixtureClustererTraining

`SubmitGaussianMixtureClustererTraining` is a **write** operation, not idempotent, non-destructive.

Queues a Gaussian mixture clustering training run, fitting overlapping bell-shaped components and reporting each row's
per-component membership probability.

- **Owning agent:** `advanced-analytics-training-clusterer-density` (Advanced Analytics Training Clusterer Density) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/GaussianMixtureClustererTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitGaussianMixtureDensityTraining

`SubmitGaussianMixtureDensityTraining` is a **write** operation, not idempotent, non-destructive.

Queues a Gaussian mixture density training run, describing the rows' shape as a weighted sum of components and scoring a
row by its log-density.

- **Owning agent:** `advanced-analytics-training-latent-structure` (Advanced Analytics Training Latent Structure) in the
  Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/GaussianMixtureDensityTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitGradientBoostingClassifierTraining

`SubmitGradientBoostingClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a gradient boosting classification training run: fits shallow trees successively to the current log-odds residual
and Platt-calibrates the resulting margin.

- **Owning agent:** `advanced-analytics-training-classifier-trees` (Advanced Analytics Training Classifier Trees) in the
  Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/GradientBoostingClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitGradientBoostingRegressorTraining

`SubmitGradientBoostingRegressorTraining` is a **write** operation, not idempotent, non-destructive.

Queues a gradient boosting regression training run, fitting shallow trees successively to the current residual with no
calibration needed.

- **Owning agent:** `advanced-analytics-training-regressor` (Advanced Analytics Training Regressor) in the Model Risk
  and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/GradientBoostingRegressorTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitIsolationForestAnomalyTraining

`SubmitIsolationForestAnomalyTraining` is a **write** operation, not idempotent, non-destructive.

Queues an isolation forest anomaly training run, scoring a row by how few random splits it takes to isolate it;
unsupervised, with labels only optionally calibrating the score afterwards.

- **Owning agent:** `advanced-analytics-training-anomaly` (Advanced Analytics Training Anomaly) in the Model Risk and
  Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/IsolationForestAnomalyTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitKMeansClustererTraining

`SubmitKMeansClustererTraining` is a **write** operation, not idempotent, non-destructive.

Queues a k-means clustering training run, assigning rows to the nearest of k iteratively-recentred means with no labels.

- **Owning agent:** `advanced-analytics-training-clusterer-centroid` (Advanced Analytics Training Clusterer Centroid) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/KMeansClustererTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitKMedoidsClustererTraining

`SubmitKMedoidsClustererTraining` is a **write** operation, not idempotent, non-destructive.

Queues a k-medoids clustering training run, requiring each cluster centre to be an actual training row rather than an
averaged point.

- **Owning agent:** `advanced-analytics-training-clusterer-centroid` (Advanced Analytics Training Clusterer Centroid) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/KMedoidsClustererTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitKModesClustererTraining

`SubmitKModesClustererTraining` is a **write** operation, not idempotent, non-destructive.

Queues a k-modes clustering training run over categorical, integer-coded columns, clustering by per-cluster modal
category rather than an averaged centre.

- **Owning agent:** `advanced-analytics-training-clusterer-centroid` (Advanced Analytics Training Clusterer Centroid) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/KModesClustererTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitKNearestNeighboursClassifierTraining

`SubmitKNearestNeighboursClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a k nearest neighbours classifier training run, scoring a row by the Platt-calibrated vote share of its k nearest
standardised training rows.

- **Owning agent:** `advanced-analytics-training-classifier-margin` (Advanced Analytics Training Classifier Margin) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/KNearestNeighboursClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitKNearestNeighboursRegressorTraining

`SubmitKNearestNeighboursRegressorTraining` is a **write** operation, not idempotent, non-destructive.

Queues a k nearest neighbours regression training run, predicting the mean target of a row's k nearest standardised
training rows.

- **Owning agent:** `advanced-analytics-training-regressor` (Advanced Analytics Training Regressor) in the Model Risk
  and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/KNearestNeighboursRegressorTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitKernelRidgeRegressorTraining

`SubmitKernelRidgeRegressorTraining` is a **write** operation, not idempotent, non-destructive.

Queues a kernel ridge regression training run, solving a ridge-penalised fit in a kernel-reshaped space by a single
closed-form linear solve.

- **Owning agent:** `advanced-analytics-training-regressor` (Advanced Analytics Training Regressor) in the Model Risk
  and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/KernelRidgeRegressorTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitKernelSupportVectorClassifierTraining

`SubmitKernelSupportVectorClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a kernel support vector machine classifier training run, fitting a maximum-margin boundary in a kernel-reshaped
space by sequential minimal optimisation and Platt-calibrating its signed distance.

- **Owning agent:** `advanced-analytics-training-classifier-margin` (Advanced Analytics Training Classifier Margin) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/KernelSupportVectorClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitLinearRegressionTraining

`SubmitLinearRegressionTraining` is a **write** operation, not idempotent, non-destructive.

Queues an ordinary least squares linear regression training run against inline rows.

- **Owning agent:** `advanced-analytics-training-regressor` (Advanced Analytics Training Regressor) in the Model Risk
  and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/LinearRegressionTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitLinearSupportVectorClassifierTraining

`SubmitLinearSupportVectorClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a linear support vector machine classifier training run, fitting a maximum-margin linear boundary by dual
coordinate descent and Platt-calibrating its signed distance.

- **Owning agent:** `advanced-analytics-training-classifier-margin` (Advanced Analytics Training Classifier Margin) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/LinearSupportVectorClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitLogisticRegressionTraining

`SubmitLogisticRegressionTraining` is a **write** operation, not idempotent, non-destructive.

Queues a logistic regression training run against inline rows or a cache pointer, fitting a ridge-penalised coefficient
per predictor.

- **Owning agent:** `advanced-analytics-training-classifier-probabilistic` (Advanced Analytics Training Classifier
  Probabilistic) in the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/LogisticRegressionTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitMahalanobisAnomalyTraining

`SubmitMahalanobisAnomalyTraining` is a **write** operation, not idempotent, non-destructive.

Queues a Mahalanobis-distance anomaly training run, scoring a row by its correlation-aware distance from the fitted
mean.

- **Owning agent:** `advanced-analytics-training-anomaly` (Advanced Analytics Training Anomaly) in the Model Risk and
  Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/MahalanobisAnomalyTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitMeanShiftClustererTraining

`SubmitMeanShiftClustererTraining` is a **write** operation, not idempotent, non-destructive.

Queues a mean shift clustering training run, letting rows climb to the nearest density peak so the cluster count is
discovered rather than chosen.

- **Owning agent:** `advanced-analytics-training-clusterer-density` (Advanced Analytics Training Clusterer Density) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/MeanShiftClustererTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitMiniBatchKMeansClustererTraining

`SubmitMiniBatchKMeansClustererTraining` is a **write** operation, not idempotent, non-destructive.

Queues a mini-batch k-means clustering training run: a faster k-means approximation for large row counts that nudges
centres from small random batches.

- **Owning agent:** `advanced-analytics-training-clusterer-centroid` (Advanced Analytics Training Clusterer Centroid) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/MiniBatchKMeansClustererTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitNaiveBayesClassifierTraining

`SubmitNaiveBayesClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a Gaussian naive Bayes classifier training run, fitting one distribution per predictor per class and
Platt-calibrating the resulting posterior.

- **Owning agent:** `advanced-analytics-training-classifier-probabilistic` (Advanced Analytics Training Classifier
  Probabilistic) in the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/NaiveBayesClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitNeuralNetworkClassifierTraining

`SubmitNeuralNetworkClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a small feedforward neural network classifier training run, fitting one hidden layer and Platt-calibrating its
sigmoid output.

- **Owning agent:** `advanced-analytics-training-classifier-margin` (Advanced Analytics Training Classifier Margin) in
  the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/NeuralNetworkClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitNeuralNetworkRegressorTraining

`SubmitNeuralNetworkRegressorTraining` is a **write** operation, not idempotent, non-destructive.

Queues a small feedforward neural network regression training run, mapping its bounded output back onto the outcome's
original scale.

- **Owning agent:** `advanced-analytics-training-regressor` (Advanced Analytics Training Regressor) in the Model Risk
  and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/NeuralNetworkRegressorTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitOneClassSupportVectorAnomalyTraining

`SubmitOneClassSupportVectorAnomalyTraining` is a **write** operation, not idempotent, non-destructive.

Queues a one-class support vector machine anomaly training run, fitting a boundary that wraps snugly around the bulk of
unlabelled rows in a kernel-reshaped space.

- **Owning agent:** `advanced-analytics-training-anomaly` (Advanced Analytics Training Anomaly) in the Model Risk and
  Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/OneClassSupportVectorAnomalyTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitPrincipalComponentTraining

`SubmitPrincipalComponentTraining` is a **write** operation, not idempotent, non-destructive.

Queues a principal component training run, re-expressing correlated variables as a smaller set of uncorrelated,
variance-ranked components.

- **Owning agent:** `advanced-analytics-training-latent-structure` (Advanced Analytics Training Latent Structure) in the
  Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/PrincipalComponentTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitRandomForestClassifierTraining

`SubmitRandomForestClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a random forest classification training run: averages many bootstrap-sampled decision trees' votes into a
steadier, Platt-calibrated probability than any single tree's.

- **Owning agent:** `advanced-analytics-training-classifier-trees` (Advanced Analytics Training Classifier Trees) in the
  Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/RandomForestClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitRandomForestRegressorTraining

`SubmitRandomForestRegressorTraining` is a **write** operation, not idempotent, non-destructive.

Queues a random forest regression training run, averaging many bootstrap-sampled regression trees into a steadier
continuous prediction.

- **Owning agent:** `advanced-analytics-training-regressor` (Advanced Analytics Training Regressor) in the Model Risk
  and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/RandomForestRegressorTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### SubmitTreeAugmentedNaiveBayesClassifierTraining

`SubmitTreeAugmentedNaiveBayesClassifierTraining` is a **write** operation, not idempotent, non-destructive.

Queues a tree-augmented naive Bayes training run: a Chow-Liu-discovered one-parent-per-predictor structure relaxes naive
Bayes' independence assumption, with the posterior Platt-calibrated.

- **Owning agent:** `advanced-analytics-training-classifier-probabilistic` (Advanced Analytics Training Classifier
  Probabilistic) in the Model Risk and Analytics domain
- **Access:** permission 66
- **Declared in:** `Jube.Service/Agent/AdvancedAnalyticsTools/TreeAugmentedNaiveBayesClassifierTrainingRunTools.cs`
- **Confirmation:** none; the run it queues is a transient execution with no persisted business entity behind it, so
  there is nothing for a preservation snapshot to protect

### TenantRegistryCount

`TenantRegistryCount` is a **read** operation, idempotent, non-destructive.

Counts the Tenant Registrys matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `tenant-registry-inspector` (Tenant Registry Inspector) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`

### TenantRegistryCreate

`TenantRegistryCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Tenant; calling twice creates two. Landlord-only.

- **Owning agent:** `tenant-registry-author` (Tenant Registry Author) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### TenantRegistryDelete

`TenantRegistryDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Tenant; reversible only by direct database action. Landlord-only.

- **Owning agent:** `tenant-registry-author` (Tenant Registry Author) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### TenantRegistryFilter

`TenantRegistryFilter` is a **read** operation, idempotent, non-destructive.

Returns the Tenant Registrys matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `tenant-registry-inspector` (Tenant Registry Inspector) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`

### TenantRegistryFilterFields

`TenantRegistryFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Tenant Registrys may use.

- **Owning agent:** `tenant-registry-inspector` (Tenant Registry Inspector) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`

### TenantRegistryGet

`TenantRegistryGet` is a **read** operation, idempotent, non-destructive.

Returns a single Tenant by Id. Landlord-only.

- **Owning agent:** `tenant-registry-inspector` (Tenant Registry Inspector) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`

### TenantRegistryList

`TenantRegistryList` is a **read** operation, idempotent, non-destructive.

Lists registered Tenants, Id-ascending, keyset-paged and capped. Landlord-only.

- **Owning agent:** `tenant-registry-inspector` (Tenant Registry Inspector) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`

### TenantRegistryUpdate

`TenantRegistryUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Tenant; overwrites, writes a version-audit row. Landlord-only.

- **Owning agent:** `tenant-registry-author` (Tenant Registry Author) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### TenantRegistryValidate

`TenantRegistryValidate` is a **read** operation, idempotent, non-destructive.

Validates a Tenant Registry without saving it and returns every failure.

- **Owning agent:** `tenant-registry-test-bench` (Tenant Registry Test Bench) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `TenantRegistryService.RequiredPermissions`
- **Service:** `TenantRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TenantRegistry.cs`

### TreeChildrenAbstractionCalculationGet

`TreeChildrenAbstractionCalculationGet` is a **read** operation, idempotent, non-destructive.

Lists the abstraction calculation nodes of an Entity Analysis Model as tree children.

- **Owning agent:** `tree-children-inspector-2` (Tree Children Inspector 2) in the Model Supporting Structures domain
- **Access:** permission 14
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenAbstractionRuleGet

`TreeChildrenAbstractionRuleGet` is a **read** operation, idempotent, non-destructive.

Lists the abstraction rule nodes of an Entity Analysis Model as tree children.

- **Owning agent:** `tree-children-inspector-2` (Tree Children Inspector 2) in the Model Supporting Structures domain
- **Access:** permission 13, 14
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenActivationRuleGet

`TreeChildrenActivationRuleGet` is a **read** operation, idempotent, non-destructive.

Lists the activation rule nodes of an Entity Analysis Model as tree children, ordered by priority.

- **Owning agent:** `tree-children-inspector-3` (Tree Children Inspector 3) in the Model Supporting Structures domain
- **Access:** permission 17
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenAdaptationGet

`TreeChildrenAdaptationGet` is a **read** operation, idempotent, non-destructive.

Lists the HTTP adaptation nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-2` (Tree Children Inspector 2) in the Model Supporting Structures domain
- **Access:** permission 15
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenCaseWorkflowActionGet

`TreeChildrenCaseWorkflowActionGet` is a **read** operation, idempotent, non-destructive.

Lists the action nodes of a Case Workflow as tree children.

- **Owning agent:** `tree-children-inspector-4` (Tree Children Inspector 4) in the Model Supporting Structures domain
- **Access:** permission 22
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenCaseWorkflowDisplayGet

`TreeChildrenCaseWorkflowDisplayGet` is a **read** operation, idempotent, non-destructive.

Lists the display nodes of a Case Workflow as tree children.

- **Owning agent:** `tree-children-inspector-4` (Tree Children Inspector 4) in the Model Supporting Structures domain
- **Access:** permission 23
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenCaseWorkflowFilterGet

`TreeChildrenCaseWorkflowFilterGet` is a **read** operation, idempotent, non-destructive.

Lists the filter nodes of a Case Workflow as tree children.

- **Owning agent:** `tree-children-inspector-4` (Tree Children Inspector 4) in the Model Supporting Structures domain
- **Access:** permission 25
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenCaseWorkflowFormGet

`TreeChildrenCaseWorkflowFormGet` is a **read** operation, idempotent, non-destructive.

Lists the form nodes of a Case Workflow as tree children.

- **Owning agent:** `tree-children-inspector-4` (Tree Children Inspector 4) in the Model Supporting Structures domain
- **Access:** permission 21
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenCaseWorkflowGet

`TreeChildrenCaseWorkflowGet` is a **read** operation, idempotent, non-destructive.

Lists the case workflow nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-2` (Tree Children Inspector 2) in the Model Supporting Structures domain
- **Access:** permission 18, 19, 20, 21, 22, 23, 24, 25
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenCaseWorkflowMacroGet

`TreeChildrenCaseWorkflowMacroGet` is a **read** operation, idempotent, non-destructive.

Lists the macro nodes of a Case Workflow as tree children.

- **Owning agent:** `tree-children-inspector-4` (Tree Children Inspector 4) in the Model Supporting Structures domain
- **Access:** permission 24
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenCaseWorkflowStatusGet

`TreeChildrenCaseWorkflowStatusGet` is a **read** operation, idempotent, non-destructive.

Lists the status nodes of a Case Workflow as tree children.

- **Owning agent:** `tree-children-inspector-4` (Tree Children Inspector 4) in the Model Supporting Structures domain
- **Access:** permission 19
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenCaseWorkflowXPathGet

`TreeChildrenCaseWorkflowXPathGet` is a **read** operation, idempotent, non-destructive.

Lists the xpath nodes of a Case Workflow as tree children.

- **Owning agent:** `tree-children-inspector-3` (Tree Children Inspector 3) in the Model Supporting Structures domain
- **Access:** permission 20
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenDictionaryGet

`TreeChildrenDictionaryGet` is a **read** operation, idempotent, non-destructive.

Lists the dictionary nodes of an Entity Analysis Model, addressed by model Guid, as tree children.

- **Owning agent:** `tree-children-inspector-3` (Tree Children Inspector 3) in the Model Supporting Structures domain
- **Access:** permission 4
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenExhaustiveGet

`TreeChildrenExhaustiveGet` is a **read** operation, idempotent, non-destructive.

Lists the exhaustive search instance nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-2` (Tree Children Inspector 2) in the Model Supporting Structures domain
- **Access:** permission 16
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenGatewayRuleGet

`TreeChildrenGatewayRuleGet` is a **read** operation, idempotent, non-destructive.

Lists the gateway rule nodes of an Entity Analysis Model as tree children, ordered by priority.

- **Owning agent:** `tree-children-inspector-2` (Tree Children Inspector 2) in the Model Supporting Structures domain
- **Access:** permission 10
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenInlineFunctionGet

`TreeChildrenInlineFunctionGet` is a **read** operation, idempotent, non-destructive.

Lists the inline function nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-1` (Tree Children Inspector 1) in the Model Supporting Structures domain
- **Access:** permission 8
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenInlineScriptGet

`TreeChildrenInlineScriptGet` is a **read** operation, idempotent, non-destructive.

Lists the inline script nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-3` (Tree Children Inspector 3) in the Model Supporting Structures domain
- **Access:** permission 9
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenListGet

`TreeChildrenListGet` is a **read** operation, idempotent, non-destructive.

Lists the list nodes of an Entity Analysis Model, addressed by model Guid, as tree children.

- **Owning agent:** `tree-children-inspector-3` (Tree Children Inspector 3) in the Model Supporting Structures domain
- **Access:** permission 3
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenReprocessingGet

`TreeChildrenReprocessingGet` is a **read** operation, idempotent, non-destructive.

Lists the reprocessing rule nodes of an Entity Analysis Model as tree children.

- **Owning agent:** `tree-children-inspector-2` (Tree Children Inspector 2) in the Model Supporting Structures domain
- **Access:** permission 26
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenRequestXPathGet

`TreeChildrenRequestXPathGet` is a **read** operation, idempotent, non-destructive.

Lists the request XPath nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-1` (Tree Children Inspector 1) in the Model Supporting Structures domain
- **Access:** permission 7
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenRoleRegistryPermissionGet

`TreeChildrenRoleRegistryPermissionGet` is a **read** operation, idempotent, non-destructive.

Lists the permission nodes granted to a Role Registry as tree children; the name is the permission specification name.

- **Owning agent:** `tree-children-inspector-1` (Tree Children Inspector 1) in the Model Supporting Structures domain
- **Access:** permission 36
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenSanctionsGet

`TreeChildrenSanctionsGet` is a **read** operation, idempotent, non-destructive.

Lists the sanction nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-3` (Tree Children Inspector 3) in the Model Supporting Structures domain
- **Access:** permission 11
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenTTLCounterGet

`TreeChildrenTTLCounterGet` is a **read** operation, idempotent, non-destructive.

Lists the TTL counter nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-3` (Tree Children Inspector 3) in the Model Supporting Structures domain
- **Access:** permission 12
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenTagGet

`TreeChildrenTagGet` is a **read** operation, idempotent, non-destructive.

Lists the tag nodes of an Entity Analysis Model as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-1` (Tree Children Inspector 1) in the Model Supporting Structures domain
- **Access:** permission 37
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenUserRegistryGet

`TreeChildrenUserRegistryGet` is a **read** operation, idempotent, non-destructive.

Lists the users of a Role Registry as tree children.

- **Owning agent:** `tree-children-inspector-1` (Tree Children Inspector 1) in the Model Supporting Structures domain
- **Access:** permission 35
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenVisualisationRegistryDatasourceGet

`TreeChildrenVisualisationRegistryDatasourceGet` is a **read** operation, idempotent, non-destructive.

Lists the datasource nodes of a Visualisation Registry as tree children, ordered by priority.

- **Owning agent:** `tree-children-inspector-1` (Tree Children Inspector 1) in the Model Supporting Structures domain
- **Access:** permission 33
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### TreeChildrenVisualisationRegistryParameterGet

`TreeChildrenVisualisationRegistryParameterGet` is a **read** operation, idempotent, non-destructive.

Lists the parameter nodes of a Visualisation Registry as tree children, ordered by Id.

- **Owning agent:** `tree-children-inspector-1` (Tree Children Inspector 1) in the Model Supporting Structures domain
- **Access:** permission 32
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.TreeChildren.cs`

### UserInTenantGetCurrentTenantRegistry

`UserInTenantGetCurrentTenantRegistry` is a **read** operation, idempotent, non-destructive.

Returns the caller's own tenant allocation, or nothing if unallocated.

- **Owning agent:** `user-observability` (User Observability) in the Access Control and Tenancy domain
- **Access:** permission 1
- **Permission source:** `UserInTenantService.RequiredPermissions`
- **Service:** `UserInTenantService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserInTenant.cs`

### UserInTenantList

`UserInTenantList` is a **read** operation, idempotent, non-destructive.

Lists the users allocated to the caller's own tenant, keyset-paged and capped.

- **Owning agent:** `user-observability` (User Observability) in the Access Control and Tenancy domain
- **Access:** permission 1
- **Permission source:** `UserInTenantService.RequiredPermissions`
- **Service:** `UserInTenantService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserInTenant.cs`

### UserInTenantSwitchTenant

`UserInTenantSwitchTenant` is a **write** operation, idempotent, non-destructive.

Switches the caller to a different tenant; restricted to a Landlord caller.

- **Owning agent:** `user-observability` (User Observability) in the Access Control and Tenancy domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `UserInTenantService.RequiredPermissions`
- **Service:** `UserInTenantService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserInTenant.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### UserLoginList

`UserLoginList` is a **read** operation, idempotent, non-destructive.

Lists the Login Audit Trail -- every sign-in attempt, successful or not, across all three schemes (Username/Password,
Negotiate, OAuth/OpenID Connect), most recent first, capped at 100000, with optional date-range and substring search
(CreatedUser, RemoteIp, UserAgent, FailureMessage) filters, not scoped to any tenant. Also accepts samplePercentage
(0-100) to draw an unbiased random subset of matching rows instead of the most recent ones.

- **Owning agent:** `user-observability` (User Observability) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserLoginService.RequiredPermissions`
- **Service:** `UserLoginService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserLogin.cs`

### UserLogoutList

`UserLogoutList` is a **read** operation, idempotent, non-destructive.

Lists the Logout Audit Trail -- every time a session was cut (the user logged out, an administrator revoked the user's
tokens, or a password change revoked the earlier sessions), most recent first, capped at 100000, with optional
date-range and substring search (CreatedUser, CutByUser, RemoteIp, UserAgent, Message) filters, scoped to the caller's
own tenant (a landlord sees every tenant). Also accepts samplePercentage (0-100) to draw an unbiased random subset of
matching rows instead of the most recent ones.

- **Owning agent:** `user-observability` (User Observability) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserLogoutService.RequiredPermissions`
- **Service:** `UserLogoutService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserLogout.cs`

### UserRegistryApiKeyDelete

`UserRegistryApiKeyDelete` is a **delete** operation, idempotent, destructive.

Revokes an API key; reversible only by direct database action. A repeat call on an already-revoked key succeeds as a
no-op if it was fully removed, or is reported not-found if it belongs to another tenant.

- **Owning agent:** `user-observability` (User Observability) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryApiKeyService.RequiredPermissions`
- **Service:** `UserRegistryApiKeyService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistryApiKey.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### UserRegistryApiKeyListByUserRegistryId

`UserRegistryApiKeyListByUserRegistryId` is a **read** operation, idempotent, non-destructive.

Lists the API keys issued against a UserRegistry, masked to an 8-character display prefix.

- **Owning agent:** `user-observability` (User Observability) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryApiKeyService.RequiredPermissions`
- **Service:** `UserRegistryApiKeyService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistryApiKey.cs`

### UserRegistryCount

`UserRegistryCount` is a **read** operation, idempotent, non-destructive.

Counts the User Registrys matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `user-registry-inspector` (User Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`

### UserRegistryCreate

`UserRegistryCreate` is a **write** operation, not idempotent, non-destructive.

Registers a user account under a Role Registry in the caller's tenant; calling twice creates two.

- **Owning agent:** `user-registry-author` (User Registry Author) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### UserRegistryDelete

`UserRegistryDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a user account in the caller's tenant by id.

- **Owning agent:** `user-registry-author` (User Registry Author) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### UserRegistryFilter

`UserRegistryFilter` is a **read** operation, idempotent, non-destructive.

Returns the User Registrys matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `user-registry-inspector` (User Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`

### UserRegistryFilterFields

`UserRegistryFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over User Registrys may use.

- **Owning agent:** `user-registry-inspector` (User Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`

### UserRegistryGet

`UserRegistryGet` is a **read** operation, idempotent, non-destructive.

Returns one user account by id, scoped to the caller's tenant.

- **Owning agent:** `user-registry-inspector` (User Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`

### UserRegistryGetByGuid

`UserRegistryGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one user account by Guid, scoped to the caller's tenant.

- **Owning agent:** `user-registry-inspector` (User Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`

### UserRegistryList

`UserRegistryList` is a **read** operation, idempotent, non-destructive.

Lists user accounts for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `user-registry-inspector` (User Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`

### UserRegistryListByRoleRegistryGuid

`UserRegistryListByRoleRegistryGuid` is a **read** operation, idempotent, non-destructive.

Lists the user accounts assigned to a given Role Registry, scoped to the caller's tenant.

- **Owning agent:** `user-registry-inspector` (User Registry Inspector) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`

### UserRegistryUpdate

`UserRegistryUpdate` is a **write** operation, idempotent, non-destructive.

Updates a user account in the caller's tenant by id. Never touches the password.

- **Owning agent:** `user-registry-author` (User Registry Author) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### UserRegistryValidate

`UserRegistryValidate` is a **read** operation, idempotent, non-destructive.

Validates an User Registry without saving it and returns every failure.

- **Owning agent:** `user-registry-test-bench` (User Registry Test Bench) in the Access Control and Tenancy domain
- **Access:** permission 35
- **Permission source:** `UserRegistryService.RequiredPermissions`
- **Service:** `UserRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.UserRegistry.cs`

### VisualisationRegistryCount

`VisualisationRegistryCount` is a **read** operation, idempotent, non-destructive.

Counts the Visualisation Registrys matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `visualisation-registry-inspector` (Visualisation Registry Inspector) in the Analytics Reporting
  domain
- **Access:** permission 18, 31, 32, 33
- **Permission source:** `VisualisationRegistryService.ListPermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### VisualisationRegistryCreate

`VisualisationRegistryCreate` is a **write** operation, not idempotent, non-destructive.

Creates a Visualisation Registry; calling twice creates two.

- **Owning agent:** `visualisation-registry-author` (Visualisation Registry Author) in the Analytics Reporting domain
- **Access:** permission 31
- **Permission source:** `VisualisationRegistryService.WritePermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryDatasourceCommandExecutionExecute

`VisualisationRegistryDatasourceCommandExecutionExecute` is a **read** operation, idempotent, non-destructive.

Executes a Visualisation Registry Datasource command with caller-supplied parameter values and returns its rows.

- **Owning agent:** `visualisation-role-grants-and-execution` (Visualisation Role Grants and Execution) in the Analytics
  Reporting domain
- **Access:** permission 28, 1
- **Permission source:** `VisualisationRegistryDatasourceCommandExecutionService.RequiredPermissions`
- **Service:** `VisualisationRegistryDatasourceCommandExecutionService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasourceCommandExecution.cs`

### VisualisationRegistryDatasourceCount

`VisualisationRegistryDatasourceCount` is a **read** operation, idempotent, non-destructive.

Counts the Visualisation Registry Datasources matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `visualisation-registry-datasource-inspector` (Visualisation Registry Datasource Inspector) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.ListPermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDatasourceCreate

`VisualisationRegistryDatasourceCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Datasource under a Visualisation Registry in the caller's tenant, validating its SQL Command; calling twice
creates two.

- **Owning agent:** `visualisation-registry-datasource-author` (Visualisation Registry Datasource Author) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.WritePermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryDatasourceDelete

`VisualisationRegistryDatasourceDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Datasource in the caller's tenant by id.

- **Owning agent:** `visualisation-registry-datasource-author` (Visualisation Registry Datasource Author) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.WritePermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryDatasourceFilter

`VisualisationRegistryDatasourceFilter` is a **read** operation, idempotent, non-destructive.

Returns the Visualisation Registry Datasources matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `visualisation-registry-datasource-inspector` (Visualisation Registry Datasource Inspector) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.ListPermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDatasourceFilterFields

`VisualisationRegistryDatasourceFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Visualisation Registry Datasources may use.

- **Owning agent:** `visualisation-registry-datasource-inspector` (Visualisation Registry Datasource Inspector) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.ListPermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDatasourceGet

`VisualisationRegistryDatasourceGet` is a **read** operation, idempotent, non-destructive.

Returns one Datasource by id, scoped to the caller's tenant.

- **Owning agent:** `visualisation-registry-datasource-inspector` (Visualisation Registry Datasource Inspector) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.ReadPermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDatasourceGetByGuid

`VisualisationRegistryDatasourceGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Datasource by Guid, scoped to the caller's tenant.

- **Owning agent:** `visualisation-registry-datasource-inspector` (Visualisation Registry Datasource Inspector) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.ReadPermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDatasourceGetByVisualisationRegistryId

`VisualisationRegistryDatasourceGetByVisualisationRegistryId` is a **read** operation, idempotent, non-destructive.

Lists Datasources belonging to a given Visualisation Registry, scoped to the caller's tenant.

- **Owning agent:** `visualisation-registry-datasource-inspector` (Visualisation Registry Datasource Inspector) in the
  Analytics Reporting domain
- **Access:** permission 33, 28, 1
- **Permission source:** `VisualisationRegistryDatasourceService.ReadByParentPermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDatasourceGetByVisualisationRegistryIdActiveOnly

`VisualisationRegistryDatasourceGetByVisualisationRegistryIdActiveOnly` is a **read** operation, idempotent,
non-destructive.

Lists active, role-visible Datasources belonging to a given Visualisation Registry for the caller.

- **Owning agent:** `visualisation-registry-datasource-inspector` (Visualisation Registry Datasource Inspector) in the
  Analytics Reporting domain
- **Access:** permission 33, 28, 1
- **Permission source:** `VisualisationRegistryDatasourceService.ReadByParentPermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDatasourceList

`VisualisationRegistryDatasourceList` is a **read** operation, idempotent, non-destructive.

Lists Datasources for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `visualisation-registry-datasource-inspector` (Visualisation Registry Datasource Inspector) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.ListPermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDatasourceRoleCreate

`VisualisationRegistryDatasourceRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Visualisation Registry Datasource; calling twice creates two grants.

- **Owning agent:** `datasource-and-parameter-role-grants` (Datasource and Parameter Role Grants) in the Analytics
  Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryDatasourceRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasourceRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryDatasourceRoleDelete

`VisualisationRegistryDatasourceRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Visualisation Registry Datasource; reversible only by direct database action. A repeat call
on an already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `datasource-and-parameter-role-grants` (Datasource and Parameter Role Grants) in the Analytics
  Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryDatasourceRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasourceRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryDatasourceRoleListByVisualisationRegistryDatasourceGuid

`VisualisationRegistryDatasourceRoleListByVisualisationRegistryDatasourceGuid` is a **read** operation, idempotent,
non-destructive.

Lists the Role grants for a Visualisation Registry Datasource.

- **Owning agent:** `datasource-and-parameter-role-grants` (Datasource and Parameter Role Grants) in the Analytics
  Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryDatasourceRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasourceRole.cs`

### VisualisationRegistryDatasourceSeriesListByVisualisationRegistryDatasourceId

`VisualisationRegistryDatasourceSeriesListByVisualisationRegistryDatasourceId` is a **read** operation, idempotent,
non-destructive.

Lists the series definitions (name and inferred data type of each SQL result column) of a Visualisation Registry
Datasource; server-derived, never user-created.

- **Owning agent:** `visualisation-role-grants-and-execution` (Visualisation Role Grants and Execution) in the Analytics
  Reporting domain
- **Access:** permission 28, 1
- **Permission source:** `VisualisationRegistryDatasourceSeriesService.RequiredPermissions`
- **Service:** `VisualisationRegistryDatasourceSeriesService`
- **Declared in:**
  `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasourceSeries.cs`

### VisualisationRegistryDatasourceUpdate

`VisualisationRegistryDatasourceUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Datasource in the caller's tenant by id, re-validating its SQL Command.

- **Owning agent:** `visualisation-registry-datasource-author` (Visualisation Registry Datasource Author) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.WritePermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryDatasourceValidate

`VisualisationRegistryDatasourceValidate` is a **read** operation, idempotent, non-destructive.

Validates a Visualisation Registry Datasource without saving it and returns every failure.

- **Owning agent:** `visualisation-registry-datasource-test-bench` (Visualisation Registry Datasource Test Bench) in the
  Analytics Reporting domain
- **Access:** permission 33
- **Permission source:** `VisualisationRegistryDatasourceService.WritePermissions`
- **Service:** `VisualisationRegistryDatasourceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryDatasource.cs`

### VisualisationRegistryDelete

`VisualisationRegistryDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Visualisation Registry; reversible only by direct database action.

- **Owning agent:** `visualisation-registry-author` (Visualisation Registry Author) in the Analytics Reporting domain
- **Access:** permission 31
- **Permission source:** `VisualisationRegistryService.WritePermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryFilter

`VisualisationRegistryFilter` is a **read** operation, idempotent, non-destructive.

Returns the Visualisation Registrys matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `visualisation-registry-inspector` (Visualisation Registry Inspector) in the Analytics Reporting
  domain
- **Access:** permission 18, 31, 32, 33
- **Permission source:** `VisualisationRegistryService.ListPermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### VisualisationRegistryFilterFields

`VisualisationRegistryFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Visualisation Registrys may use.

- **Owning agent:** `visualisation-registry-inspector` (Visualisation Registry Inspector) in the Analytics Reporting
  domain
- **Access:** permission 18, 31, 32, 33
- **Permission source:** `VisualisationRegistryService.ListPermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### VisualisationRegistryGet

`VisualisationRegistryGet` is a **read** operation, idempotent, non-destructive.

Returns a single Visualisation Registry by Id.

- **Owning agent:** `visualisation-registry-inspector` (Visualisation Registry Inspector) in the Analytics Reporting
  domain
- **Access:** permission 31, 28, 1
- **Permission source:** `VisualisationRegistryService.ReadPermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### VisualisationRegistryGetByGuid

`VisualisationRegistryGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns a single Visualisation Registry by Id.

- **Owning agent:** `visualisation-registry-inspector` (Visualisation Registry Inspector) in the Analytics Reporting
  domain
- **Access:** permission 31, 28, 1
- **Permission source:** `VisualisationRegistryService.ReadPermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### VisualisationRegistryGetByGuidActiveOnly

`VisualisationRegistryGetByGuidActiveOnly` is a **read** operation, idempotent, non-destructive.

Returns a single active Visualisation Registry by Guid, for embedded recall, provided the caller holds a role grant on
it.

- **Owning agent:** `visualisation-registry-inspector` (Visualisation Registry Inspector) in the Analytics Reporting
  domain
- **Access:** permission 31, 28, 1
- **Permission source:** `VisualisationRegistryService.ReadPermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### VisualisationRegistryList

`VisualisationRegistryList` is a **read** operation, idempotent, non-destructive.

Lists Visualisation Registries visible to the caller's tenant, keyset-paged and capped.

- **Owning agent:** `visualisation-registry-inspector` (Visualisation Registry Inspector) in the Analytics Reporting
  domain
- **Access:** permission 18, 31, 32, 33
- **Permission source:** `VisualisationRegistryService.ListPermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### VisualisationRegistryListByShowInDirectory

`VisualisationRegistryListByShowInDirectory` is a **read** operation, idempotent, non-destructive.

Lists active, Show In Directory Visualisation Registries that the caller holds a role grant on.

- **Owning agent:** `visualisation-registry-inspector` (Visualisation Registry Inspector) in the Analytics Reporting
  domain
- **Access:** permission 28
- **Permission source:** `VisualisationRegistryService.ShowInDirectoryPermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### VisualisationRegistryParameterCount

`VisualisationRegistryParameterCount` is a **read** operation, idempotent, non-destructive.

Counts the Visualisation Registry Parameters matching query builder JSON, optionally grouped by a field.

- **Owning agent:** `visualisation-registry-parameter-inspector` (Visualisation Registry Parameter Inspector) in the
  Analytics Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryParameterCreate

`VisualisationRegistryParameterCreate` is a **write** operation, not idempotent, non-destructive.

Registers a Parameter under a Visualisation Registry in the caller's tenant; calling twice creates two.

- **Owning agent:** `visualisation-registry-parameter-author` (Visualisation Registry Parameter Author) in the Analytics
  Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryParameterDelete

`VisualisationRegistryParameterDelete` is a **delete** operation, idempotent, destructive.

Soft-deletes a Parameter in the caller's tenant by id.

- **Owning agent:** `visualisation-registry-parameter-author` (Visualisation Registry Parameter Author) in the Analytics
  Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryParameterFilter

`VisualisationRegistryParameterFilter` is a **read** operation, idempotent, non-destructive.

Returns the Visualisation Registry Parameters matching query builder JSON, keyset-paged and capped.

- **Owning agent:** `visualisation-registry-parameter-inspector` (Visualisation Registry Parameter Inspector) in the
  Analytics Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryParameterFilterFields

`VisualisationRegistryParameterFilterFields` is a **read** operation, idempotent, non-destructive.

Lists the fields a query builder JSON filter over Visualisation Registry Parameters may use.

- **Owning agent:** `visualisation-registry-parameter-inspector` (Visualisation Registry Parameter Inspector) in the
  Analytics Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryParameterGet

`VisualisationRegistryParameterGet` is a **read** operation, idempotent, non-destructive.

Returns one Visualisation Registry Parameter by its id, scoped to the caller's tenant.

- **Owning agent:** `visualisation-registry-parameter-inspector` (Visualisation Registry Parameter Inspector) in the
  Analytics Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryParameterGetByGuid

`VisualisationRegistryParameterGetByGuid` is a **read** operation, idempotent, non-destructive.

Returns one Visualisation Registry Parameter by its id, scoped to the caller's tenant.

- **Owning agent:** `visualisation-registry-parameter-inspector` (Visualisation Registry Parameter Inspector) in the
  Analytics Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryParameterGetByVisualisationRegistryId

`VisualisationRegistryParameterGetByVisualisationRegistryId` is a **read** operation, idempotent, non-destructive.

Lists the Parameters belonging to a given Visualisation Registry, ordered by Id, scoped to the caller's tenant.

- **Owning agent:** `visualisation-registry-parameter-inspector` (Visualisation Registry Parameter Inspector) in the
  Analytics Reporting domain
- **Access:** permission 32, 28, 1
- **Permission source:** `VisualisationRegistryParameterService.ActiveOnlyPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryParameterGetByVisualisationRegistryIdActiveOnly

`VisualisationRegistryParameterGetByVisualisationRegistryIdActiveOnly` is a **read** operation, idempotent,
non-destructive.

Lists the active Parameters belonging to a given Visualisation Registry that the caller holds a role grant on, scoped to
the caller's tenant.

- **Owning agent:** `visualisation-registry-parameter-inspector` (Visualisation Registry Parameter Inspector) in the
  Analytics Reporting domain
- **Access:** permission 32, 28, 1
- **Permission source:** `VisualisationRegistryParameterService.ActiveOnlyPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryParameterList

`VisualisationRegistryParameterList` is a **read** operation, idempotent, non-destructive.

Lists Visualisation Registry Parameters for the caller's tenant, keyset-paged and capped.

- **Owning agent:** `visualisation-registry-parameter-inspector` (Visualisation Registry Parameter Inspector) in the
  Analytics Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryParameterRoleCreate

`VisualisationRegistryParameterRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Visualisation Registry Parameter; calling twice creates two grants.

- **Owning agent:** `datasource-and-parameter-role-grants` (Datasource and Parameter Role Grants) in the Analytics
  Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameterRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryParameterRoleDelete

`VisualisationRegistryParameterRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Visualisation Registry Parameter; reversible only by direct database action. A repeat call
on an already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `datasource-and-parameter-role-grants` (Datasource and Parameter Role Grants) in the Analytics
  Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameterRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryParameterRoleListByVisualisationRegistryParameterGuid

`VisualisationRegistryParameterRoleListByVisualisationRegistryParameterGuid` is a **read** operation, idempotent,
non-destructive.

Lists the Role grants for a Visualisation Registry Parameter.

- **Owning agent:** `datasource-and-parameter-role-grants` (Datasource and Parameter Role Grants) in the Analytics
  Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameterRole.cs`

### VisualisationRegistryParameterUpdate

`VisualisationRegistryParameterUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Parameter in the caller's tenant by id.

- **Owning agent:** `visualisation-registry-parameter-author` (Visualisation Registry Parameter Author) in the Analytics
  Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryParameterValidate

`VisualisationRegistryParameterValidate` is a **read** operation, idempotent, non-destructive.

Validates a Visualisation Registry Parameter without saving it and returns every failure.

- **Owning agent:** `visualisation-registry-parameter-test-bench` (Visualisation Registry Parameter Test Bench) in the
  Analytics Reporting domain
- **Access:** permission 32
- **Permission source:** `VisualisationRegistryParameterService.RequiredPermissions`
- **Service:** `VisualisationRegistryParameterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryParameter.cs`

### VisualisationRegistryRoleCreate

`VisualisationRegistryRoleCreate` is a **write** operation, not idempotent, non-destructive.

Grants a Role access to a Visualisation Registry; calling twice creates two grants.

- **Owning agent:** `visualisation-role-grants-and-execution` (Visualisation Role Grants and Execution) in the Analytics
  Reporting domain
- **Access:** permission 31
- **Permission source:** `VisualisationRegistryRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryRoleDelete

`VisualisationRegistryRoleDelete` is a **delete** operation, not idempotent, destructive.

Removes a Role's grant on a Visualisation Registry; reversible only by direct database action. A repeat call on an
already-removed grant throws rather than succeeding as a no-op, so retries are not safe to assume succeeded.

- **Owning agent:** `visualisation-role-grants-and-execution` (Visualisation Role Grants and Execution) in the Analytics
  Reporting domain
- **Access:** permission 31
- **Permission source:** `VisualisationRegistryRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryRole.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryRoleListByVisualisationRegistryGuid

`VisualisationRegistryRoleListByVisualisationRegistryGuid` is a **read** operation, idempotent, non-destructive.

Lists the Role grants for a Visualisation Registry.

- **Owning agent:** `visualisation-role-grants-and-execution` (Visualisation Role Grants and Execution) in the Analytics
  Reporting domain
- **Access:** permission 31
- **Permission source:** `VisualisationRegistryRoleService.RequiredPermissions`
- **Service:** `VisualisationRegistryRoleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistryRole.cs`

### VisualisationRegistryUpdate

`VisualisationRegistryUpdate` is a **write** operation, idempotent, non-destructive.

Updates a Visualisation Registry; overwrites, writes a version-audit row.

- **Owning agent:** `visualisation-registry-author` (Visualisation Registry Author) in the Analytics Reporting domain
- **Access:** permission 31
- **Permission source:** `VisualisationRegistryService.WritePermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through
  `SnapshotOnWriteAiFunction`

### VisualisationRegistryValidate

`VisualisationRegistryValidate` is a **read** operation, idempotent, non-destructive.

Validates a Visualisation Registry without saving it and returns every failure.

- **Owning agent:** `visualisation-registry-test-bench` (Visualisation Registry Test Bench) in the Analytics Reporting
  domain
- **Access:** permission 31
- **Permission source:** `VisualisationRegistryService.WritePermissions`
- **Service:** `VisualisationRegistryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.VisualisationRegistry.cs`

### WafAttackList

`WafAttackList` is a **read** operation, idempotent, non-destructive.

Lists captured Web Application Firewall signature matches, most recent first, capped at 100000, with optional date-range
and substring search (MatchedValue/Route/SignatureName/Category/MatchedField/UserName/RemoteIp) filters. Not scoped to
any Model or tenant. Also accepts samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of
the most recent ones.

- **Owning agent:** `application-and-container-logs` (Application and Container Logs) in the Platform Reliability domain
- **Access:** landlord only; no tenant permission grants it
- **Permission source:** `WafAttackService.RequiredPermissions`
- **Service:** `WafAttackService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.WafAttack.cs`

### EntityAnalysisModelResubmit

`EntityAnalysisModelResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Model for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-author` (Entity Analysis Model Author) in the Detection Model Authoring domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelService.RequiredPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelRetract

`EntityAnalysisModelRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Model, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-author` (Entity Analysis Model Author) in the Detection Model Authoring domain
- **Access:** permission 6
- **Permission source:** `EntityAnalysisModelService.RequiredPermissions`
- **Service:** `EntityAnalysisModelService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModel.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionCalculationResubmit

`EntityAnalysisModelAbstractionCalculationResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Abstraction Calculation for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-author` (Entity Analysis Model Abstraction Calculation Author) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionCalculationRetract

`EntityAnalysisModelAbstractionCalculationRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Abstraction Calculation, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-abstraction-calculation-author` (Entity Analysis Model Abstraction Calculation Author) in the Detection Model Authoring domain
- **Access:** permission 14
- **Permission source:** `EntityAnalysisModelAbstractionCalculationService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionCalculationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionCalculation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionRuleResubmit

`EntityAnalysisModelAbstractionRuleResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Abstraction Rule for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-abstraction-rule-approvals` (Entity Analysis Model Abstraction Rule Approvals) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelAbstractionRuleRetract

`EntityAnalysisModelAbstractionRuleRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Abstraction Rule, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-abstraction-rule-approvals` (Entity Analysis Model Abstraction Rule Approvals) in the Detection Model Authoring domain
- **Access:** permission 13
- **Permission source:** `EntityAnalysisModelAbstractionRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelAbstractionRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelAbstractionRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleResubmit

`EntityAnalysisModelActivationRuleResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Activation Rule for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-activation-rule-approvals` (Entity Analysis Model Activation Rule Approvals) in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelActivationRuleRetract

`EntityAnalysisModelActivationRuleRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Activation Rule, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-activation-rule-approvals` (Entity Analysis Model Activation Rule Approvals) in the Detection Model Authoring domain
- **Access:** permission 17
- **Permission source:** `EntityAnalysisModelActivationRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelActivationRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelActivationRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryResubmit

`EntityAnalysisModelDictionaryResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Dictionary for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-dictionary-author` (Entity Analysis Model Dictionary Author) in the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryRetract

`EntityAnalysisModelDictionaryRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Dictionary, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-dictionary-author` (Entity Analysis Model Dictionary Author) in the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionary.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryKvpResubmit

`EntityAnalysisModelDictionaryKvpResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Key Value Pair for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-author` (Entity Analysis Model Dictionary Kvp Author) in the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelDictionaryKvpRetract

`EntityAnalysisModelDictionaryKvpRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Key Value Pair, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-dictionary-kvp-author` (Entity Analysis Model Dictionary Kvp Author) in the Model Supporting Structures domain
- **Access:** permission 4
- **Permission source:** `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`
- **Service:** `EntityAnalysisModelDictionaryKvpService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelDictionaryKvp.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelGatewayRuleResubmit

`EntityAnalysisModelGatewayRuleResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Gateway Rule for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-gateway-rule-approvals` (Entity Analysis Model Gateway Rule Approvals) in the Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelGatewayRuleRetract

`EntityAnalysisModelGatewayRuleRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Gateway Rule, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-gateway-rule-approvals` (Entity Analysis Model Gateway Rule Approvals) in the Detection Model Authoring domain
- **Access:** permission 10
- **Permission source:** `EntityAnalysisModelGatewayRuleService.WritePermissions`
- **Service:** `EntityAnalysisModelGatewayRuleService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelGatewayRule.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelHttpAdaptationResubmit

`EntityAnalysisModelHttpAdaptationResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected HTTP Adaptation for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-http-adaptation-author` (Entity Analysis Model Http Adaptation Author) in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.WritePermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelHttpAdaptationRetract

`EntityAnalysisModelHttpAdaptationRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected HTTP Adaptation, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-http-adaptation-author` (Entity Analysis Model Http Adaptation Author) in the Model Risk and Analytics domain
- **Access:** permission 15
- **Permission source:** `EntityAnalysisModelHttpAdaptationService.WritePermissions`
- **Service:** `EntityAnalysisModelHttpAdaptationService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelHttpAdaptation.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineFunctionResubmit

`EntityAnalysisModelInlineFunctionResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Inline Function for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-inline-function-author` (Entity Analysis Model Inline Function Author) in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineFunctionRetract

`EntityAnalysisModelInlineFunctionRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Inline Function, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-inline-function-author` (Entity Analysis Model Inline Function Author) in the Detection Model Authoring domain
- **Access:** permission 8
- **Permission source:** `EntityAnalysisModelInlineFunctionService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineFunctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineFunction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineScriptResubmit

`EntityAnalysisModelInlineScriptResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Inline Script registration for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-inline-script-author` (Entity Analysis Model Inline Script Author) in the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelInlineScriptRetract

`EntityAnalysisModelInlineScriptRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Inline Script registration, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-inline-script-author` (Entity Analysis Model Inline Script Author) in the Detection Model Authoring domain
- **Access:** permission 9
- **Permission source:** `EntityAnalysisModelInlineScriptService.WritePermissions`
- **Service:** `EntityAnalysisModelInlineScriptService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelInlineScript.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListResubmit

`EntityAnalysisModelListResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected List for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-list-author` (Entity Analysis Model List Author) in the Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListRetract

`EntityAnalysisModelListRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected List, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-list-author` (Entity Analysis Model List Author) in the Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelList.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListValueResubmit

`EntityAnalysisModelListValueResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected List Value for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-list-value-author` (Entity Analysis Model List Value Author) in the Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelListValueRetract

`EntityAnalysisModelListValueRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected List Value, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-list-value-author` (Entity Analysis Model List Value Author) in the Model Supporting Structures domain
- **Access:** permission 3
- **Permission source:** `EntityAnalysisModelListValueService.RequiredPermissions`
- **Service:** `EntityAnalysisModelListValueService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelListValue.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelRequestXPathResubmit

`EntityAnalysisModelRequestXPathResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Request XPath for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-request-x-path-author` (Entity Analysis Model Request X Path Author) in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.WritePermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelRequestXPathRetract

`EntityAnalysisModelRequestXPathRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Request XPath, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-request-x-path-author` (Entity Analysis Model Request X Path Author) in the Detection Model Authoring domain
- **Access:** permission 7
- **Permission source:** `EntityAnalysisModelRequestXPathService.WritePermissions`
- **Service:** `EntityAnalysisModelRequestXPathService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelRequestXPath.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelSanctionResubmit

`EntityAnalysisModelSanctionResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Sanction check for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-sanction-author` (Entity Analysis Model Sanction Author) in the Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.WritePermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelSanctionRetract

`EntityAnalysisModelSanctionRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Sanction check, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-sanction-author` (Entity Analysis Model Sanction Author) in the Detection Model Authoring domain
- **Access:** permission 11
- **Permission source:** `EntityAnalysisModelSanctionService.WritePermissions`
- **Service:** `EntityAnalysisModelSanctionService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelSanction.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTagResubmit

`EntityAnalysisModelTagResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Tag for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-tag-author` (Entity Analysis Model Tag Author) in the Detection Model Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTagRetract

`EntityAnalysisModelTagRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Tag, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-tag-author` (Entity Analysis Model Tag Author) in the Detection Model Authoring domain
- **Access:** permission 37
- **Permission source:** `EntityAnalysisModelTagService.RequiredPermissions`
- **Service:** `EntityAnalysisModelTagService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTag.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTtlCounterResubmit

`EntityAnalysisModelTtlCounterResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected TTL Counter for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `entity-analysis-model-ttl-counter-author` (Entity Analysis Model Ttl Counter Author) in the Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.WritePermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### EntityAnalysisModelTtlCounterRetract

`EntityAnalysisModelTtlCounterRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected TTL Counter, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `entity-analysis-model-ttl-counter-author` (Entity Analysis Model Ttl Counter Author) in the Detection Model Authoring domain
- **Access:** permission 12
- **Permission source:** `EntityAnalysisModelTtlCounterService.WritePermissions`
- **Service:** `EntityAnalysisModelTtlCounterService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.EntityAnalysisModelTtlCounter.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstanceResubmit

`ExhaustiveSearchInstanceResubmit` is a **write** operation, not idempotent, non-destructive.

Resubmits a rejected Exhaustive Adaptation for approval again, unchanged, opening a fresh approval cycle.

- **Owning agent:** `exhaustive-search-instance-author` (Exhaustive Search Instance Author) in the Model Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`

### ExhaustiveSearchInstanceRetract

`ExhaustiveSearchInstanceRetract` is a **write** operation, not idempotent, non-destructive.

Retracts a rejected Exhaustive Adaptation, restoring it to its last approved version without a new approval cycle.

- **Owning agent:** `exhaustive-search-instance-author` (Exhaustive Search Instance Author) in the Model Risk and Analytics domain
- **Access:** permission 16
- **Permission source:** `ExhaustiveSearchInstanceService.WritePermissions`
- **Service:** `ExhaustiveSearchInstanceService`
- **Declared in:** `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.ExhaustiveSearchInstance.cs`
- **Confirmation:** required before the call runs; `AgentToolDefinition.RequiresSnapshot` wraps it through `SnapshotOnWriteAiFunction`
