---
layout: default
title: Visualisations Security Policy
nav_order: 9
parent: Visualisations
grand_parent: Configuration
---

# Role Assignment Overview

Before embarking on this section of documentation it is incumbent to explain that the functionality aims to provide for
a similar use case as Case isolation, and in the interests of being concise, the explanation of database duplication is
not repeated.  
It is suggested to review the core concepts under Configuration >>> Case Management >>> Security Policy beforehand.

Every Visualisation entity supports the allocation of roles and therefore isolation of data where that entity is
apportioned.

The role allocations are joined upon recall in the Visualisation page, and its embedding in the Case page, having the
effect of that data not being in existence unless it is possible to join back to the user, through the role, through the
allocation and to that Visualisation entity.

<!-- TODO(screenshot): file=visualisation-visualisation-administration.png route=/administration/visualisations state="Replace the Razor-era image VisualisationAdministration.png with the Vue Administration > Visualisations page showing the same state" -->
![VisualisationAdministration.png](VisualisationAdministration.png)

<!-- TODO(screenshot): file=visualisation-visualisation-administration-layout.png route=/administration/visualisations state="Replace the Razor-era image VisualisationAdministrationLayout.png with the Vue Administration > Visualisations page showing the same state" -->
![VisualisationAdministrationLayout.png](VisualisationAdministrationLayout.png)

Taking the Visualisation Entity, which is available by navigating Administration >>> Visualisation >>> Visualisation,
then clicking on the Visualisation entry link:

<!-- TODO(screenshot): file=visualisation-role-allocation.png route=/administration/visualisations state="Replace the Razor-era image RoleAllocation.png with the Vue Administration > Visualisations page showing the same state" -->
![RoleAllocation](RoleAllocation.png)

Note the Role Allocation widget below the Update and Delete buttons.

The Role Allocation widget is common to the following Visualisation entities:

* Visualisation.
* Visualisation Parameter.
* Visualisation Datasource.

The Role allocations have different implications for data Visualisation entity isolation, as is explained in the section
below.

The left-hand side of the Role Allocations widget shows available roles, while the right side shows roles that have been
allocated. In the above, Administrator is allocated (this happens in migrations for the Administrator role in the
Landlord Tenant only, and otherwise they need to be manually created).

# Visualisation Recall Page Isolation

The Visualisation Recall page is responsible for the invocation and full recall of datasources, and the rendering of
responses from the database as with grids or charting as embedded.

Visualisation Recall Page isolation exhibits the following behaviours:

| Recall                                 | Role Allocation Isolation               | Description                                                                                                                                                                                                                                                                                                                                                                                                          |
|----------------------------------------|-----------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Visualisation Recall                   | Visualisation.                          | The availability of a Visualisation to a role, where the link to Visualisation recall will be hidden if the Role Allocation is not available.                                                                                                                                                                                                                                                                        |
| Visualisation Parameter Entry          | Case Workflow and Case Workflow Filter. | The selective availability of Visualisation Parameters. In the event a Visualisation Parameter is hidden, the fallback value for that parameter is taken to be the default value configured. This functionality allows for a robust means to isolate data to given users, while providing wider search scope for administrators.                                                                                     |
| Visualisation Datasource (Card) Recall | Case Workflow and Case Workflow Status. | A Visualisation is a collection of individual reports that have a common parameter set, and can be restricted in their recall based upon Role Allocation. In the case of direct recall of the API endpoints given no Role Allocation, the Visualisation is filtered out of the result and is not returned (HTTP 200, with an empty or partial list); the existence of an unallocated Visualisation is not disclosed. |

All security policy is applied in the same manner to Visualisation where it is embedded in a Case Workflow, and it
follows that Role Allocations must be sympathetic to the Case Workflow Role Allocations.

<!-- TODO(docs): State the viewer model precisely: the datasource SQL runs for the viewer with the tenant allow-list built from the VIEWER permissions, not the author; a table the viewer cannot query directly is refused at run time even if the author could. Owner to confirm this is the intended wording. -->
<!-- TODO(docs): Document that a viewer holding only 28 or 1 no longer receives the SQL text of a datasource (the Command field is blanked in the active-only list); only holders of 33 see it. -->
<!-- TODO(docs): Document ReportStatementTimeoutSeconds, ReportMaximumCellBytes and ReportMaximumResultBytes (card fails with a localised refusal) and recommend a SELECT-only ReportConnectionString role. -->
<!-- TODO(docs): Document the allow-list switch EnableVisualisationDatasourceTableAllowList (default True) and that deny mode has no tenant row filtering; link docs/Concepts/EnvironmentVariables. -->

## The reporting database role

The SQL of a datasource (and case SQL, filters and samples) is user supplied. Jube checks it with a SQL validator and
runs it in a read-only transaction with a statement timeout, but those are application controls. The database should
refuse writes too. That is the job of `ReportConnectionString`. When it is not set, or uses the same login as
`ConnectionString`, the SQL runs as the application login and Jube writes a warning to the log at startup (the warning
carries no connection detail). The quickstart `docker-compose.yml` does this, deliberately, for evaluation only.

For production set `ReportConnectionString` to a login that can only read. The house convention is `jube_reporting` in
`Jube.Cluster/database.sh` (the same statements appear for `reporting` in `docs/GettingStarted/CreateUsers.sql`): the
login is created with a password, owns nothing, and holds `SELECT` on the tables in the `public` schema, with default
privileges so that tables added by later migrations are readable. Jube never creates this login for you and no migration
does.

The statements below create a narrower role that holds `SELECT` only on the tables the datasource allow-list can reach
(the registry in `DynamicSqlTableAccess` plus the tenancy tables its templates join), and on only two columns of
`UserRegistry`, so password hashes are unreadable even if the validator were bypassed. Replace the password and the
database name. Because the same connection string also serves backtests, reprocessing, samples and case search, check
that every table those features read is in the list before you adopt it; if in doubt use the house convention instead.

```sql
CREATE ROLE jube_reporting_select LOGIN PASSWORD '<a long random password>'
    NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
ALTER ROLE jube_reporting_select SET search_path = public;
ALTER ROLE jube_reporting_select SET default_transaction_read_only = on;
GRANT CONNECT ON DATABASE <database> TO jube_reporting_select;
GRANT USAGE ON SCHEMA public TO jube_reporting_select;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM jube_reporting_select;
```

<!-- reporting-role-grants:start -->

```sql
GRANT SELECT ON
    "ActivationWatcher",
    "AdvancedAnalyticsCache",
    "AdvancedAnalyticsTrainingRun",
    "AdvancedAnalyticsTrainingRunCurvePoint",
    "AdvancedAnalyticsTrainingRunIteration",
    "AdvancedAnalyticsTrainingRunMetric",
    "AdvancedAnalyticsTrainingRunStep",
    "AdvancedAnalyticsTrainingRunTrial",
    "AdvancedAnalyticsTrainingRunVariable",
    "Archive",
    "ArchiveEntityAnalysisModelAbstractionEntry",
    "ArchiveKey",
    "ArchiveKeyVersion",
    "ArchiveTag",
    "ArchiveTagVersion",
    "ArchiveVersion",
    "ArchiverStagePerformanceCounter",
    "ArchiverWarning",
    "AskJoobyAnalyticsInstance",
    "AskJoobyAnalyticsInstanceVersion",
    "AskJoobyAnalyticsTrainedModel",
    "AskJoobyAnalyticsTrainedModelVersion",
    "AskJoobyEndpointFee",
    "AskJoobyErrata",
    "AskJoobyRagChunk",
    "AskJoobyRerankFee",
    "Case",
    "CaseCreationWarning",
    "CaseEvent",
    "CaseFile",
    "CaseNote",
    "CaseResearch",
    "CaseResearchCitation",
    "CaseSuppression",
    "CaseSuppressionApproval",
    "CaseSuppressionTarget",
    "CaseSuppressionTransaction",
    "CaseSuppressionVersion",
    "CaseWorkflow",
    "CaseWorkflowAction",
    "CaseWorkflowActionRole",
    "CaseWorkflowActionVersion",
    "CaseWorkflowDisplay",
    "CaseWorkflowDisplayRole",
    "CaseWorkflowDisplayVersion",
    "CaseWorkflowFilter",
    "CaseWorkflowFilterRole",
    "CaseWorkflowFilterVersion",
    "CaseWorkflowForm",
    "CaseWorkflowFormEntry",
    "CaseWorkflowFormEntryValue",
    "CaseWorkflowFormRole",
    "CaseWorkflowFormVersion",
    "CaseWorkflowMacro",
    "CaseWorkflowMacroRole",
    "CaseWorkflowMacroVersion",
    "CaseWorkflowRole",
    "CaseWorkflowStatus",
    "CaseWorkflowStatusRole",
    "CaseWorkflowStatusVersion",
    "CaseWorkflowVersion",
    "CaseWorkflowXPath",
    "CaseWorkflowXPathRole",
    "CaseWorkflowXPathVersion",
    "Country",
    "Currency",
    "DictionaryEvalExpression",
    "DynamicSqlTokens",
    "EntityAnalysisInlineScript",
    "EntityAnalysisModel",
    "EntityAnalysisModelAbstractionCalculation",
    "EntityAnalysisModelAbstractionCalculationVersion",
    "EntityAnalysisModelAbstractionRule",
    "EntityAnalysisModelAbstractionRuleVersion",
    "EntityAnalysisModelActivationRule",
    "EntityAnalysisModelActivationRuleCounterHistory",
    "EntityAnalysisModelActivationRuleOverride",
    "EntityAnalysisModelActivationRuleOverrideVersion",
    "EntityAnalysisModelActivationRuleVersion",
    "EntityAnalysisModelAsynchronousQueueBalance",
    "EntityAnalysisModelBacktestInstance",
    "EntityAnalysisModelDictionary",
    "EntityAnalysisModelDictionaryCsvFileUpload",
    "EntityAnalysisModelDictionaryKvp",
    "EntityAnalysisModelDictionaryKvpVersion",
    "EntityAnalysisModelDictionaryVersion",
    "EntityAnalysisModelGatewayRule",
    "EntityAnalysisModelGatewayRuleCounterHistory",
    "EntityAnalysisModelGatewayRuleVersion",
    "EntityAnalysisModelInlineFunction",
    "EntityAnalysisModelInlineFunctionVersion",
    "EntityAnalysisModelInlineScript",
    "EntityAnalysisModelInlineScriptVersion",
    "EntityAnalysisModelInstance",
    "EntityAnalysisModelList",
    "EntityAnalysisModelListCsvFileUpload",
    "EntityAnalysisModelListValue",
    "EntityAnalysisModelListValueVersion",
    "EntityAnalysisModelListVersion",
    "EntityAnalysisModelOverride",
    "EntityAnalysisModelOverrideVersion",
    "EntityAnalysisModelProcessingCounter",
    "EntityAnalysisModelReprocessingRule",
    "EntityAnalysisModelReprocessingRuleInstance",
    "EntityAnalysisModelRequestXpath",
    "EntityAnalysisModelRequestXpathVersion",
    "EntityAnalysisModelResponseElevationRange",
    "EntityAnalysisModelResponseElevationRangeVersion",
    "EntityAnalysisModelResponseTimePipelineCounter",
    "EntityAnalysisModelRole",
    "EntityAnalysisModelSampleExecutionLog",
    "EntityAnalysisModelSanction",
    "EntityAnalysisModelSanctionVersion",
    "EntityAnalysisModelSearchKeyCalculationInstance",
    "EntityAnalysisModelSearchKeyDistinctValueCalculationInstance",
    "EntityAnalysisModelStagePerformanceCounter",
    "EntityAnalysisModelSynchronisationNodeStatusEntry",
    "EntityAnalysisModelSynchronisationSchedule",
    "EntityAnalysisModelTag",
    "EntityAnalysisModelTagVersion",
    "EntityAnalysisModelTaskPerformanceCounter",
    "EntityAnalysisModelTtlCounter",
    "EntityAnalysisModelTtlCounterVersion",
    "EntityAnalysisModelVersion",
    "EntityApproval",
    "ExampleCaseVolumeEntry",
    "ExampleCustomerCaseManagement",
    "ExampleGeoCoded",
    "ExhaustiveSearchInstance",
    "ExhaustiveSearchInstanceCategoricalField",
    "ExhaustiveSearchInstanceData",
    "ExhaustiveSearchInstancePrescriptionHistogram",
    "ExhaustiveSearchInstancePromotedTrialInstance",
    "ExhaustiveSearchInstancePromotedTrialInstancePredictedActual",
    "ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVar",
    "ExhaustiveSearchInstancePromotedTrialInstanceRoc",
    "ExhaustiveSearchInstancePromotedTrialInstanceSensitivity",
    "ExhaustiveSearchInstancePromotedTrialInstanceVariable",
    "ExhaustiveSearchInstanceTrialInstance",
    "ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial",
    "ExhaustiveSearchInstanceTrialInstanceSensitivity",
    "ExhaustiveSearchInstanceTrialInstanceTopologyTrial",
    "ExhaustiveSearchInstanceTrialInstanceVariable",
    "ExhaustiveSearchInstanceVariable",
    "ExhaustiveSearchInstanceVariableAnomaly",
    "ExhaustiveSearchInstanceVariableClassification",
    "ExhaustiveSearchInstanceVariableHistogram",
    "ExhaustiveSearchInstanceVariableHistogramAnomaly",
    "ExhaustiveSearchInstanceVariableHistogramClass",
    "ExhaustiveSearchInstanceVariableHistogramClassification",
    "ExhaustiveSearchInstanceVariableMultiCollinearity",
    "ExhaustiveSearchInstanceVersion",
    "Export",
    "GraphCommunity",
    "GraphEdge",
    "GraphNode",
    "GraphNodeTag",
    "GraphPattern",
    "GraphRegistry",
    "GraphRegistryEdge",
    "GraphRegistryRole",
    "Import",
    "Language",
    "LanguageString",
    "MockArchive",
    "PermissionSpecification",
    "PreservationDiff",
    "PreservationSnapshot",
    "RoleRegistry",
    "RuleScriptToken",
    "SanctionEntry",
    "SanctionEntryImport",
    "SanctionEntryRejection",
    "SanctionEntrySource",
    "SanctionStopToken",
    "SessionCaseJournal",
    "TenantRegistry",
    "UserInTenant",
    "VisualisationRegistry",
    "VisualisationRegistryDatasource",
    "VisualisationRegistryDatasourceExecutionLog",
    "VisualisationRegistryDatasourceExecutionLogParameter",
    "VisualisationRegistryDatasourceRole",
    "VisualisationRegistryDatasourceSeries",
    "VisualisationRegistryDatasourceVersion",
    "VisualisationRegistryParameter",
    "VisualisationRegistryParameterRole",
    "VisualisationRegistryParameterVersion",
    "VisualisationRegistryRole",
    "VisualisationRegistryVersion"
TO jube_reporting_select;
GRANT SELECT ("Name", "RoleRegistryGuid") ON "UserRegistry" TO jube_reporting_select;
```

<!-- reporting-role-grants:end -->

A table added to the allow-list later needs its own `GRANT SELECT`: the test
`TheDocumentedSelectOnlyRoleGrantsExactlyTheTablesTheAllowListCanReach` fails until this list is updated. Then set
`ReportConnectionString` to a connection string that uses `jube_reporting_select`. On a cluster, `jube_reporting` is
already wired this way.

<!-- TODO(docs): Owner to confirm the narrow role against backtest, reprocessing, sample and case-search reads before recommending it over the house convention. -->
<!-- TODO(docs): Screenshot of the startup warning in the application log. -->
