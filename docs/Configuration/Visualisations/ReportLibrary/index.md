---
layout: default
title: Report Library
nav_order: 21
parent: Visualisations
grand_parent: Configuration
---

# Report Library

Beside the executive dashboards a library of reports is installed, each of which is a [Visualisation Registry](../Registry/index.html) of a few Datasources answering one family of question. They are written for the people who tune rules, run the investigation floor, keep the configuration and watch the platform, and, as with the executive dashboards, each is independent, so that any of them can be changed or deleted without affecting another. Every report takes a From and To date, defaulting to the number of days stated, and the ones that break a figure down by a payload field take the name of the field as a parameter.

The reports that scan the archive are bounded by their dates, because the archive is the largest table, and the reports that expand the rule activations of every alert, such as the overlap of rules, default to a short window for the same reason. A report reads at most 5,000 rows and a report that returns nothing is reading something the installation has not recorded, as is the case for the Datasources that need the engine counters or the graph job, which read as empty until those have run.

## Definitions used throughout

The same definitions are used on every dashboard and report, so that the figures agree with one another. An alert is an archived transaction on which at least one rule fired, and a shadow match is not an alert. A case is confirmed when it is closed and either its close reason is Suspicion confirmed and reported or Fraud confirmed, or its closing status has the outcome Confirmed. A case is a false positive when it is closed and either its close reason is a false positive reason or its closing status has the outcome Not confirmed. Rates use closed cases only and the number of closed cases is shown beside them, and where nothing has been closed the rate is a dash rather than zero. A window is the From and To parameters, where a date parameter defaults to a number of days before now, and each key figure is compared with the equal period immediately before it.

Typology is the rule that opened the case, as no other typology attribute exists on a rule or a case. Jurisdiction is the country field of the payload named in a parameter, and BillingCountry is the default. Cost figures are estimates, produced from the hourly cost and minutes per case parameters, and are labelled as such.

## The reports

### Rules: Activation Rule Performance

How often each rule evaluates and fires, by day, week, hour and weekday, how much value it alerts, what response elevation it produces, the entities that alert most, the rules that did not fire and a league table of every rule.

Parameters:

| Parameter   | Type | Default            |
|-------------|------|--------------------|
| From        | date | 30 days before now |
| To          | date | now                |
| Value Field | text | AmountEUR          |
| Rule Name   | text | %                  |

Tiles:

| Tile                                | Shown as              | Reads                                                                              | Permissions needed (any one per table) |
|-------------------------------------|-----------------------|------------------------------------------------------------------------------------|----------------------------------------|
| Activations and hit rate by day     | combination chart     | EntityAnalysisModelActivationRuleCounterHistory, EntityAnalysisModelActivationRule | 17                                     |
| Hit rate by rule                    | bar chart and grid    | EntityAnalysisModelActivationRuleCounterHistory, EntityAnalysisModelActivationRule | 17                                     |
| Hit rate by rule and week           | heatmap               | EntityAnalysisModelActivationRuleCounterHistory, EntityAnalysisModelActivationRule | 17                                     |
| Alerts by rule and day              | stacked area chart    | Archive                                                                            | 1                                      |
| Prevailing rule against all firings | stacked bar chart     | Archive                                                                            | 1                                      |
| Response elevation by rule          | column chart and grid | Archive                                                                            | 1                                      |
| Value alerted by rule               | treemap               | Archive                                                                            | 1                                      |
| Alerts by weekday and hour          | heatmap               | Archive                                                                            | 1                                      |
| Entities with the most alerts       | grid                  | Archive                                                                            | 1                                      |
| Rules that did not fire             | grid                  | Archive, EntityAnalysisModelActivationRule                                         | 1, 17                                  |
| Rule league table                   | grid                  | Case, CaseWorkflow, CaseWorkflowStatus, Archive                                    | 1, 18, 19                              |

### Rules: Outcomes and Precision

What became of the alerts of each rule: the precision, the close reasons, the cases opened and closed, the days to close, the analyst hours lost to false positives, the confirmed value, the precision month by month for one rule and the cumulative gains by response elevation.

Parameters:

| Parameter        | Type    | Default            |
|------------------|---------|--------------------|
| From             | date    | 90 days before now |
| To               | date    | now                |
| Value Field      | text    | AmountEUR          |
| Rule Name        | text    | RapidVelocity      |
| Minutes Per Case | decimal | 30                 |

Tiles:

| Tile                                           | Shown as                     | Reads                                                                     | Permissions needed (any one per table) |
|------------------------------------------------|------------------------------|---------------------------------------------------------------------------|----------------------------------------|
| Precision by rule                              | bar chart and grid           | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Close reasons by rule                          | percentage stacked bar chart | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Cases opened and closed by rule                | combination chart            | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Median days to close by rule                   | bar chart                    | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Analyst hours spent on false positives by rule | bar chart                    | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Confirmed value by rule                        | bar chart                    | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Precision by month for one rule                | line chart and grid          | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Cumulative gains by response elevation         | line chart                   | Case, CaseWorkflow, CaseWorkflowStatus                                    | 1, 18, 19                              |
| Outcomes by rule                               | grid                         | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |

### Rules: Overlap and Redundancy

Which rules fire on the same transactions, the alerts only one rule raised, the share of each rule's alerts also caught by another rule, and the pairs of rules that overlap most, which are the candidates to retire.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 14 days before now |
| To        | date | now                |

Tiles:

| Tile                                                    | Shown as           | Reads   | Permissions needed (any one per table) |
|---------------------------------------------------------|--------------------|---------|----------------------------------------|
| Rules that fire together                                | heatmap            | Archive | 1                                      |
| Alerts only this rule raised                            | bar chart and grid | Archive | 1                                      |
| Share of each rule's alerts also caught by another rule | grid               | Archive | 1                                      |
| Rule pairs ranked by overlap                            | grid               | Archive | 1                                      |
| Alerts by number of rules fired                         | column chart       | Archive | 1                                      |

### Rules: Shadow and Tuning Evidence

The evidence for a threshold decision: the matches of shadow rules, their overlap with the live rules and whether the entities they matched were later confirmed, the register and history of backtests, the outcomes of alerts above the line and the near misses just below a threshold that a parameter sets.

Parameters:

| Parameter    | Type    | Default            |
|--------------|---------|--------------------|
| From         | date    | 30 days before now |
| To           | date    | now                |
| Rule Name    | text    | StructuringTuned   |
| Key          | text    | AmountEUR          |
| Threshold    | decimal | 3900               |
| Band Percent | decimal | 10                 |

Tiles:

| Tile                                       | Shown as                        | Reads                                                                     | Permissions needed (any one per table) |
|--------------------------------------------|---------------------------------|---------------------------------------------------------------------------|----------------------------------------|
| Shadow matches by day                      | line chart                      | Archive                                                                   | 1                                      |
| Shadow rules against live rules            | grid                            | Archive                                                                   | 1                                      |
| Shadow matches on entities later confirmed | grid                            | Case, CaseWorkflow, CaseWorkflowStatus, Archive                           | 1, 18, 19                              |
| Backtest register                          | grid                            | EntityAnalysisModelBacktestInstance, EntityAnalysisModelActivationRule    | 17, 61                                 |
| Backtest precision and recall for one rule | line chart                      | EntityAnalysisModelBacktestInstance, EntityAnalysisModelActivationRule    | 17, 61                                 |
| Above the line: alert outcomes by rule     | percentage stacked column chart | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Below the line: just under the threshold   | histogram                       | ArchiveKey, Archive                                                       | 1                                      |
| Near misses by day                         | column chart                    | ArchiveKey, Archive                                                       | 1                                      |

### Rules: Gateway and Sampling

The gateway rules, how often they match, and the sample executions run against the archive. The counters need the engine to be running, so these tiles read as empty on a database that has never processed a transaction.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 30 days before now |
| To        | date | now                |

Tiles:

| Tile                                   | Shown as          | Reads                                        | Permissions needed (any one per table) |
|----------------------------------------|-------------------|----------------------------------------------|----------------------------------------|
| Gateway rule inventory                 | grid              | EntityAnalysisModelGatewayRule               | 10                                     |
| Gateway evaluations and matches by day | combination chart | EntityAnalysisModelGatewayRuleCounterHistory | 10                                     |
| Sample executions                      | grid              | EntityAnalysisModelSampleExecutionLog        | 40                                     |

### Monitoring: Transaction Volumes

The transactions processed by day, weekday and hour, broken down by any payload field named in a parameter, valued by that field, their amount distribution, response elevation, country and the tags applied to them.

Parameters:

| Parameter     | Type | Default            |
|---------------|------|--------------------|
| From          | date | 30 days before now |
| To            | date | now                |
| Field         | text | ChannelId          |
| Value Field   | text | AmountEUR          |
| Country Field | text | BillingCountry     |

Tiles:

| Tile                             | Shown as             | Reads      | Permissions needed (any one per table) |
|----------------------------------|----------------------|------------|----------------------------------------|
| Transactions by day              | column chart         | Archive    | 1                                      |
| Transactions by weekday and hour | heatmap              | Archive    | 1                                      |
| Breakdown by a payload field     | bar chart and grid   | Archive    | 1                                      |
| Value by a payload field         | treemap              | Archive    | 1                                      |
| Amount distribution              | histogram            | Archive    | 1                                      |
| Response elevation by day        | stacked column chart | Archive    | 1                                      |
| Transactions by country          | map                  | Archive    | 1                                      |
| Alert rate by country            | bar chart and grid   | Archive    | 1                                      |
| Largest transactions             | grid                 | Archive    | 1                                      |
| Tags applied by day              | stacked column chart | ArchiveTag | 1                                      |

### Monitoring: Entities

The entities with the most alerts and cases, repeat entities, the statement of one entity and its alerts by month, the entities first seen in the window and the activation watcher feed and map.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |
| Key Value | text | ACC-S0042          |

Tiles:

| Tile                           | Shown as     | Reads                                  | Permissions needed (any one per table) |
|--------------------------------|--------------|----------------------------------------|----------------------------------------|
| Entities with the most alerts  | grid         | Archive                                | 1                                      |
| Entities with the most cases   | grid         | Case, CaseWorkflow, CaseWorkflowStatus | 1, 18, 19                              |
| Entities with repeat cases     | grid         | Case, CaseWorkflow, CaseWorkflowStatus | 1, 18, 19                              |
| Transactions for one entity    | grid         | Archive, ArchiveKey                    | 1                                      |
| Alerts for one entity by month | column chart | Archive, ArchiveKey                    | 1                                      |
| Entities first seen by day     | column chart | ArchiveKey, Archive                    | 1                                      |
| Activation watcher feed        | grid         | ActivationWatcher                      | 30                                     |
| Activation watcher map         | map          | ActivationWatcher                      | 30                                     |

### Cases: Registers

The open and closed case registers, the open cases by status, risk lane and rating, the cases opened and closed by day, the originating rule, diary entries, payment holds, legal holds and the cases sampled for quality review.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 30 days before now |
| To        | date | now                |

Tiles:

| Tile                           | Shown as          | Reads                                                                     | Permissions needed (any one per table) |
|--------------------------------|-------------------|---------------------------------------------------------------------------|----------------------------------------|
| Open cases by status           | donut chart       | Case, CaseWorkflow, CaseWorkflowStatus                                    | 1, 18, 19                              |
| Open cases by risk lane        | bar chart         | Case                                                                      | 1                                      |
| Rating of open cases           | column chart      | Case                                                                      | 1                                      |
| Cases opened and closed by day | combination chart | Case, CaseWorkflow, CaseWorkflowStatus                                    | 1, 18, 19                              |
| Cases by originating rule      | treemap           | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Open case register             | grid              | Case, CaseWorkflow, CaseWorkflowStatus                                    | 1, 18, 19                              |
| Closed case register           | grid              | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Diary entries due              | grid              | Case                                                                      | 1                                      |
| Payment holds in force         | grid              | Case                                                                      | 1                                      |
| Legal hold register            | grid              | Case                                                                      | 1                                      |
| Quality sample register        | grid              | Case                                                                      | 1                                      |

### Cases: Analyst Activity

What analysts do: the activity by type and day, the actions by analyst, closures by week, notes by analyst and kind, closures against the daily goal, the locks held, the allocations and the activity log of one case.

Parameters:

| Parameter | Type         | Default            |
|-----------|--------------|--------------------|
| From      | date         | 30 days before now |
| To        | date         | now                |
| Case Id   | whole number | 5                  |

Tiles:

| Tile                                  | Shown as             | Reads                                  | Permissions needed (any one per table) |
|---------------------------------------|----------------------|----------------------------------------|----------------------------------------|
| Case activity by type and day         | stacked column chart | CaseEvent                              | 1                                      |
| Peeks against full opens              | column chart         | CaseEvent                              | 1                                      |
| Actions by analyst                    | heatmap              | CaseEvent                              | 1                                      |
| Closures by analyst by week           | line chart           | Case, CaseWorkflow, CaseWorkflowStatus | 1, 18, 19                              |
| Notes by analyst and kind             | stacked bar chart    | CaseNote                               | 1                                      |
| Notes by action                       | bar chart            | CaseNote, CaseWorkflowAction           | 1, 22                                  |
| Daily closures against the daily goal | bullet chart         | Case, CaseWorkflow, CaseWorkflowStatus | 1, 18, 19                              |
| Current locks                         | grid                 | Case                                   | 1                                      |
| Allocations                           | grid                 | CaseEvent                              | 1                                      |
| Activity log for one case             | grid                 | CaseEvent                              | 1                                      |

### Cases: Flow and Handoffs

How cases move: the transitions from status to status, the cases that visited each status, the time in each status, the automatic escalations, the lifetime of cases, suspensions and workflow changes.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |

Tiles:

| Tile                                  | Shown as             | Reads                                             | Permissions needed (any one per table) |
|---------------------------------------|----------------------|---------------------------------------------------|----------------------------------------|
| Status to status transitions          | heatmap              | CaseWorkflowStatus, CaseEvent                     | 1, 19                                  |
| Cases that visited each status        | funnel               | CaseWorkflowStatus, Case, CaseWorkflow, CaseEvent | 1, 18, 19                              |
| Time in each status                   | box plot and grid    | CaseEvent, Case, CaseWorkflowStatus               | 1, 19                                  |
| Automatic escalations by status       | bar chart            | CaseWorkflowStatus, CaseEvent                     | 1, 19                                  |
| Case lifetime                         | histogram            | Case, CaseWorkflow, CaseWorkflowStatus            | 1, 18, 19                              |
| Suspended and reopened cases by month | stacked column chart | CaseEvent                                         | 1                                      |
| Workflow changes                      | grid                 | CaseEvent                                         | 1                                      |

### Cases: Suppressions and Overrides

The suppression requests by state and month, the hours to decide them, those expiring within thirty days, their wider impact and the overrides in force and created by month.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |

Tiles:

| Tile                                  | Shown as             | Reads                                     | Permissions needed (any one per table) |
|---------------------------------------|----------------------|-------------------------------------------|----------------------------------------|
| Suppression requests by state         | donut chart          | CaseSuppression                           | 1                                      |
| Suppression requests by month         | stacked column chart | CaseSuppression                           | 1                                      |
| Hours to decide a suppression         | histogram            | CaseSuppression                           | 1                                      |
| Suppressions expiring in 30 days      | grid                 | CaseSuppression                           | 1                                      |
| Wider impact of suppressions          | grid                 | CaseSuppression                           | 1                                      |
| Overrides in force by rule and effect | grid                 | EntityAnalysisModelActivationRuleOverride | 2                                      |
| Overrides created by month            | column chart         | EntityAnalysisModelActivationRuleOverride | 2                                      |

### Screening: Sanctions Lists

The listed entries by source, the import runs and their results, the rejected rows, the stop tokens, the hits by distance and month and the screening configuration.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |

Tiles:

| Tile                                | Shown as          | Reads                                       | Permissions needed (any one per table) |
|-------------------------------------|-------------------|---------------------------------------------|----------------------------------------|
| Listed entries by source            | bar chart         | SanctionEntry, SanctionEntrySource          | 11, 29                                 |
| Entries added and removed by import | combination chart | SanctionEntryImport                         | 11, 29                                 |
| Import runs                         | grid              | SanctionEntryImport, SanctionEntrySource    | 11, 29                                 |
| Rejected rows by reason             | donut chart       | SanctionEntryRejection                      | 11, 29                                 |
| Rejected rows                       | grid              | SanctionEntryRejection, SanctionEntrySource | 11, 29                                 |
| Stop tokens                         | grid              | SanctionStopToken                           | 11, 29                                 |
| Hits by match distance              | column chart      | Archive                                     | 1                                      |
| Hits by month                       | line chart        | Archive                                     | 1                                      |
| Screening configuration             | grid              | EntityAnalysisModelSanction                 | 11                                     |

### Configuration: Inventory and Change Audit

The configuration of every model by kind, the change log across the version tables, the changes by user and month, the approvals register, what awaits approval, the rejected and returned changes, the turnaround of rule approvals, any change approved by its maker, inactive and locked configuration, the request fields and whether each is available to reports, the TTL counters and the lists.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |

Tiles:

| Tile                              | Shown as          | Reads                                                                                                                                                                                                                                                                                                                                                                                                      | Permissions needed (any one per table) |
|-----------------------------------|-------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------|
| Configuration by model and kind   | stacked bar chart | EntityAnalysisModelActivationRule, EntityAnalysisModelGatewayRule, EntityAnalysisModelAbstractionRule, EntityAnalysisModelAbstractionCalculation, EntityAnalysisModelTtlCounter, EntityAnalysisModelTag, EntityAnalysisModelRequestXpath, EntityAnalysisModelInlineScript, EntityAnalysisModelSanction, EntityAnalysisModel                                                                                | 6, 7, 9, 10, 11, 12, 13, 14, 17, 37    |
| Changes by user and month         | heatmap           | EntityAnalysisModelActivationRuleVersion, EntityAnalysisModelActivationRule, EntityAnalysisModelGatewayRuleVersion, EntityAnalysisModelGatewayRule, EntityAnalysisModelAbstractionRuleVersion, EntityAnalysisModelAbstractionRule, EntityAnalysisModelTtlCounterVersion, EntityAnalysisModelTagVersion, EntityAnalysisModelRequestXpathVersion, EntityAnalysisModelListVersion, EntityAnalysisModelVersion | 3, 6, 7, 10, 12, 13, 17, 37            |
| Change log                        | grid              | EntityAnalysisModelActivationRuleVersion, EntityAnalysisModelActivationRule, EntityAnalysisModelGatewayRuleVersion, EntityAnalysisModelGatewayRule, EntityAnalysisModelAbstractionRuleVersion, EntityAnalysisModelAbstractionRule, EntityAnalysisModelTtlCounterVersion, EntityAnalysisModelTagVersion, EntityAnalysisModelRequestXpathVersion, EntityAnalysisModelListVersion, EntityAnalysisModelVersion | 3, 6, 7, 10, 12, 13, 17, 37            |
| Approvals register                | grid              | EntityApproval                                                                                                                                                                                                                                                                                                                                                                                             | 44, 45                                 |
| Awaiting approval by kind         | grid              | EntityAnalysisModelActivationRule, EntityAnalysisModelGatewayRule, EntityAnalysisModelAbstractionRule, EntityAnalysisModelTtlCounter, EntityAnalysisModelTag, EntityAnalysisModelList, EntityApproval                                                                                                                                                                                                      | 3, 10, 12, 13, 17, 37, 44, 45          |
| Rejected and returned changes     | grid              | EntityApproval                                                                                                                                                                                                                                                                                                                                                                                             | 44, 45                                 |
| Rule approval turnaround by month | box plot and grid | EntityApproval, EntityAnalysisModelActivationRuleVersion                                                                                                                                                                                                                                                                                                                                                   | 17, 44, 45                             |
| Changes approved by their maker   | grid              | EntityApproval, EntityAnalysisModelActivationRule, EntityAnalysisModelActivationRuleVersion                                                                                                                                                                                                                                                                                                                | 17, 44, 45                             |
| Inactive and locked configuration | grid              | EntityAnalysisModelActivationRule, EntityAnalysisModelGatewayRule, EntityAnalysisModelAbstractionRule, EntityAnalysisModelTtlCounter                                                                                                                                                                                                                                                                       | 10, 12, 13, 17                         |
| Request fields and reporting      | grid              | EntityAnalysisModelRequestXpath                                                                                                                                                                                                                                                                                                                                                                            | 7                                      |
| TTL counter inventory             | grid              | EntityAnalysisModelTtlCounter                                                                                                                                                                                                                                                                                                                                                                              | 12                                     |
| Lists and values expiring         | grid              | EntityAnalysisModelList, EntityAnalysisModelListValue                                                                                                                                                                                                                                                                                                                                                      | 3                                      |

### Models: Machine Learning

The Exhaustive models, the confusion matrix, ROC curve, variable correlation, trial scores and predicted against actual of the promoted model, and the training runs, metrics and variable importance of Advanced Analytics.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |

Tiles:

| Tile                                   | Shown as           | Reads                                                        | Permissions needed (any one per table) |
|----------------------------------------|--------------------|--------------------------------------------------------------|----------------------------------------|
| Exhaustive models                      | grid               | ExhaustiveSearchInstance                                     | 16                                     |
| Promoted model confusion matrix        | heatmap            | ExhaustiveSearchInstancePromotedTrialInstance                | 16                                     |
| ROC curve of the promoted model        | scatter chart      | ExhaustiveSearchInstancePromotedTrialInstanceRoc             | 16                                     |
| Variable correlation with the outcome  | bar chart and grid | ExhaustiveSearchInstanceVariable                             | 16                                     |
| Score of each trial over the search    | line chart         | ExhaustiveSearchInstanceTrialInstance                        | 16                                     |
| Predicted against actual               | scatter chart      | ExhaustiveSearchInstancePromotedTrialInstancePredictedActual | 16                                     |
| Advanced analytics training runs       | grid               | AdvancedAnalyticsTrainingRun                                 | 48                                     |
| Advanced analytics metrics by run      | bar chart          | AdvancedAnalyticsTrainingRunMetric                           | 48                                     |
| Advanced analytics variable importance | bar chart          | AdvancedAnalyticsTrainingRunVariable                         | 48                                     |

### Network: Graph

The communities by size and confirmed rate, the community league table, the patterns by kind, state and week, the most connected nodes, the nodes near confirmed cases, the edges by kind and the component sizes. These need the graph job to have run.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |

Tiles:

| Tile                                   | Shown as           | Reads          | Permissions needed (any one per table) |
|----------------------------------------|--------------------|----------------|----------------------------------------|
| Communities by size and confirmed rate | scatter chart      | GraphCommunity | 49                                     |
| Community league table                 | grid               | GraphCommunity | 49                                     |
| Patterns by kind and state             | stacked bar chart  | GraphPattern   | 49                                     |
| Patterns detected by week              | line chart         | GraphPattern   | 49                                     |
| Pattern register                       | grid               | GraphPattern   | 49                                     |
| Most connected nodes by PageRank       | grid               | GraphNode      | 49                                     |
| Nodes near confirmed cases             | column chart       | GraphNode      | 49                                     |
| Edges by kind                          | bar chart and grid | GraphEdge      | 49                                     |
| Component sizes                        | histogram          | GraphNode      | 49                                     |

### Platform: Health and Performance

The invocations, response times, queue balances and timeouts of the engine, the time and memory spent by pipeline stage, the archiver and case creation warnings, the nodes and their synchronisation, the reprocessing runs, and the exports, imports, snapshots and comparisons.

Parameters:

| Parameter | Type | Default           |
|-----------|------|-------------------|
| From      | date | 7 days before now |
| To        | date | now               |

Tiles:

| Tile                                        | Shown as          | Reads                                                  | Permissions needed (any one per table) |
|---------------------------------------------|-------------------|--------------------------------------------------------|----------------------------------------|
| Invocations by hour                         | line chart        | EntityAnalysisModelProcessingCounter                   | AskJoobyPermissions.Access             |
| Response time                               | combination chart | EntityAnalysisModelProcessingCounter                   | AskJoobyPermissions.Access             |
| Time spent by stage                         | stacked bar chart | EntityAnalysisModelStagePerformanceCounter             | AskJoobyPermissions.Access             |
| Allocated memory by stage                   | bar chart         | EntityAnalysisModelResponseTimePipelineCounter         | AskJoobyPermissions.Access             |
| Queue balances                              | line chart        | EntityAnalysisModelAsynchronousQueueBalance            | AskJoobyPermissions.Access             |
| Implicit asynchronous timeouts              | column chart      | EntityAnalysisModelProcessingCounter                   | AskJoobyPermissions.Access             |
| Archiver stage timings                      | stacked bar chart | ArchiverStagePerformanceCounter                        | AskJoobyPermissions.Access             |
| Archiver and case creation warnings         | grid              | ArchiverWarning, CaseCreationWarning                   | 27, AskJoobyPermissions.Access         |
| Nodes and synchronisation                   | grid              | EntityAnalysisModelSynchronisationNodeStatusEntry      | 5                                      |
| Synchronisation schedule                    | grid              | EntityAnalysisModelSynchronisationSchedule             | 5                                      |
| Reprocessing runs                           | grid              | none                                                   |                                        |
| Exports, imports, snapshots and comparisons | grid              | Export, Import, PreservationSnapshot, PreservationDiff | 38                                     |

### Platform: Reporting Usage

How Visualisation itself is used: the reports run by day and user, the failures, the slowest datasources and the datasources never run in the window.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 30 days before now |
| To        | date | now                |

Tiles:

| Tile                                | Shown as     | Reads                                                                                               | Permissions needed (any one per table) |
|-------------------------------------|--------------|-----------------------------------------------------------------------------------------------------|----------------------------------------|
| Reports run by day                  | column chart | VisualisationRegistryDatasourceExecutionLog                                                         | 1, 28, 33                              |
| Reports run by user                 | bar chart    | VisualisationRegistryDatasourceExecutionLog                                                         | 1, 28, 33                              |
| Failures by datasource              | bar chart    | VisualisationRegistryDatasourceExecutionLog, VisualisationRegistryDatasource                        | 1, 28, 33                              |
| Slowest datasources                 | grid         | VisualisationRegistryDatasourceExecutionLog, VisualisationRegistryDatasource, VisualisationRegistry | 1, 28, 31, 33                          |
| Datasources never run in the window | grid         | VisualisationRegistryDatasource, VisualisationRegistry, VisualisationRegistryDatasourceExecutionLog | 1, 28, 31, 33                          |

## What is not provided

The Visualisation viewer in this release has no drill-through from a tile, no filter that acts across tiles, no refresh that runs on a timer, no print or export of a whole dashboard and no scheduled delivery. Tables that Visualisation is not permitted to read are not used, which is to say the quality review verdicts, the regulatory reports, the stored service level breaches, the drift observations, the governance records, the entity risk ratings, the sanction rescreening and exemption tables, the erasure requests and the inbound queue, and so the dashboards use the proxies described on this page, such as the close reason Suspicion confirmed and reported as the measure of reporting. Sign in, lockout and role reporting is not offered because the identity tables are never chartable. Training completion, audit findings and examination findings have no source in Jube and are not shown. The names and the text of the seeded tiles are in English.
