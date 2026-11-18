---
layout: default
title: Executive Dashboards
nav_order: 20
parent: Visualisations
grand_parent: Configuration
---

# Executive Dashboards

Six Visualisations are installed for the leadership of a compliance function, each of which is an ordinary [Visualisation Registry](../Registry/index.html) with ordinary [Datasources](../GenerateChartInDatasource/index.html), so that it can be opened from the directory, copied, edited in the Datasource builder or deleted without affecting the others. None of them depends on anything that is not already supported by Visualisation. Every Datasource is a single piece of SQL that reads only the tables that [Visualisations Security Policy](../SecurityPolicy/index.html) allows, and each tile is drawn in the server from the result.

Each dashboard is allocated to the Administrator role when installed and is available to any other role through the allocation described in [Visualisations Security Policy](../SecurityPolicy/index.html). A viewer needs, for every table that a tile reads, any one of the permissions listed in the final column, and a tile that reads a table the viewer may not read shows its own error while the others are drawn. The dashboards that read cases are also limited, as are all reads of cases, to the workflows and statuses that the role of the viewer is granted.

## Definitions used throughout

The same definitions are used on every dashboard and report, so that the figures agree with one another. An alert is an archived transaction on which at least one rule fired, and a shadow match is not an alert. A case is confirmed when it is closed and either its close reason is Suspicion confirmed and reported or Fraud confirmed, or its closing status has the outcome Confirmed. A case is a false positive when it is closed and either its close reason is a false positive reason or its closing status has the outcome Not confirmed. Rates use closed cases only and the number of closed cases is shown beside them, and where nothing has been closed the rate is a dash rather than zero. A window is the From and To parameters, where a date parameter defaults to a number of days before now, and each key figure is compared with the equal period immediately before it.

Typology is the rule that opened the case, as no other typology attribute exists on a rule or a case. Jurisdiction is the country field of the payload named in a parameter, and BillingCountry is the default. Cost figures are estimates, produced from the hourly cost and minutes per case parameters, and are labelled as such.

## The demonstration data

On an installation that carries the demonstration model, the demonstration data is extended so that the dashboards open onto a year of believable history, and an installation without the demonstration model receives the dashboards and nothing else, so that they read as dashes and empty grids until transactions have been processed. The history is generated with fixed arithmetic and not with random numbers, is relative to the date of installation, and stamps the work on six named analysts and two supervisors who are text on the rows and not users that can sign in.

The history is about twenty five thousand transactions over three hundred and sixty five days, of which about eight percent are alerts, and about two thousand cases with their events, status transitions, notes, close reasons, quality samples, suppression requests, legal holds and triage recommendations. It carries a story that can be walked through on the dashboards. One rule is too broad for six months, with about nine in ten of its cases closed as false positives, and a tuned replacement goes live in the seventh month after a shadow trial and a backtest, bringing the rate down. A staffing gap in the ninth month lengthens the backlog and the breaches of the service levels before it recovers, the distribution of the response elevation shifts in the last six weeks, one rule and one change await approval, and the recommendations of the triage agent are accepted about eighty five times in a hundred. Rules, versions, approvals, overrides, backtests, reprocessing runs and sanction list imports are seeded with distinct makers and approvers, none of whom approves their own work.

## The dashboards

### Executive: Board Compliance Overview

The Board Compliance Overview is the first page a board member or senior manager should open. A strip of eight tiles gives the number, the movement against the equal period before, and a red, amber or green badge for the alerts raised, the false-positive rate, the median days to close, the open backlog, the share of closed cases that missed their resolution target, the cases reported, the rule changes still awaiting approval and the alerts per thousand transactions, beneath which sit the detection funnel from transactions to cases reported, the year in months, and the efficiency scorecard, in which the cost per alert and per confirmed case is an estimate drawn from the two cost parameters. The cost parameters are the only figures that are assumptions and they are stated as such on the tile.

Parameters:

| Parameter           | Type    | Default            |
|---------------------|---------|--------------------|
| From                | date    | 90 days before now |
| To                  | date    | now                |
| Analyst Hourly Cost | decimal | 45                 |
| Minutes Per Case    | decimal | 30                 |

Tiles:

| Tile                                           | Shown as                        | Reads                                                                                              | Permissions needed (any one per table) |
|------------------------------------------------|---------------------------------|----------------------------------------------------------------------------------------------------|----------------------------------------|
| Health strip                                   | tiles from a template           | Case, CaseWorkflow, CaseWorkflowStatus, Archive, EntityAnalysisModelActivationRule, EntityApproval | 1, 17, 18, 19, 44, 45                  |
| Detection funnel                               | funnel                          | Archive, Case, CaseWorkflow, CaseWorkflowStatus                                                    | 1, 18, 19                              |
| Alerts, cases and false-positive rate by month | combination chart and grid      | Case, CaseWorkflow, CaseWorkflowStatus, Archive                                                    | 1, 18, 19                              |
| Backlog by month                               | combination chart               | Case, CaseWorkflow, CaseWorkflowStatus                                                             | 1, 18, 19                              |
| Outcome mix by month                           | percentage stacked column chart | Case, CaseWorkflow, CaseWorkflowStatus                                                             | 1, 18, 19                              |
| Efficiency scorecard                           | tiles from a template           | Case, CaseWorkflow, CaseWorkflowStatus, Archive, CaseEvent                                         | 1, 18, 19                              |
| Largest movements by rule                      | grid                            | Archive                                                                                            | 1                                      |

### Executive: SLA and Bottlenecks

The SLA and Bottlenecks dashboard shows where cases are waiting. It sets the time to first view, to ownership and to resolution, as a median and as a ninetieth percentile, against the targets of the Case Workflow, counts the targets missed by month, draws the time cases spend in each status as a box plot, lists the queue by assignee with a badge for those behind their targets, and carries the watch list of cases untouched for seven days, deadlines falling due, payment holds and legal holds. Time in status is rebuilt from the Status Change and Automatic Escalation events of the case, keeping in mind that the transition table is not available to Visualisation.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 30 days before now |
| To        | date | now                |

Tiles:

| Tile                                      | Shown as                       | Reads                                             | Permissions needed (any one per table) |
|-------------------------------------------|--------------------------------|---------------------------------------------------|----------------------------------------|
| Watch list                                | tiles from a template          | Case, CaseWorkflow, CaseWorkflowStatus, CaseEvent | 1, 18, 19                              |
| Open cases by age and status              | stacked bar chart              | Case, CaseWorkflow, CaseWorkflowStatus            | 1, 18, 19                              |
| Response times against targets            | bullet chart and grid          | Case, CaseWorkflow, CaseWorkflowStatus, CaseEvent | 1, 18, 19                              |
| Targets missed by month                   | stacked column chart           | Case, CaseWorkflow, CaseWorkflowStatus, CaseEvent | 1, 18, 19                              |
| Time in each status                       | box plot and grid              | CaseEvent, Case, CaseWorkflowStatus               | 1, 19                                  |
| Queue by assignee                         | tiles from a template and grid | Case, CaseWorkflow, CaseWorkflowStatus            | 1, 18, 19                              |
| Closures by analyst by week               | heatmap                        | Case, CaseWorkflow, CaseWorkflowStatus            | 1, 18, 19                              |
| Automatic escalations by month            | column chart                   | CaseEvent                                         | 1                                      |
| Checklist gates and SLA breaches by month | stacked column chart           | CaseEvent                                         | 1                                      |
| Deadlines falling due                     | grid                           | Case, CaseWorkflowStatus                          | 1, 19                                  |

### Executive: Detection Effectiveness and Geography

The Detection Effectiveness and Geography dashboard answers which rules earn their place. It sets the hit rate of each rule against the share of its closed cases that were confirmed, shows alert volume by rule and month, the confirmed value by typology and country where typology is the rule that opened the case, a map of confirmed cases by country, the shadow rules against the live rules, the false-positive rate over thirty, ninety and three hundred and sixty five days, and a review of every rule with its finding. The payload fields for the value and the country are parameters and default to AmountEUR and BillingCountry.

Parameters:

| Parameter     | Type | Default            |
|---------------|------|--------------------|
| From          | date | 90 days before now |
| To            | date | now                |
| Value Field   | text | AmountEUR          |
| Country Field | text | BillingCountry     |

Tiles:

| Tile                                                 | Shown as               | Reads                                                                              | Permissions needed (any one per table) |
|------------------------------------------------------|------------------------|------------------------------------------------------------------------------------|----------------------------------------|
| Detection summary                                    | tiles from a template  | Archive, Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| Hit rate against confirmed rate by rule              | scatter chart and grid | Archive, Case, CaseWorkflow, CaseWorkflowStatus                                    | 1, 18, 19                              |
| Alert volume by rule and month                       | heatmap                | Archive                                                                            | 1                                      |
| Confirmed value by typology and country              | heatmap                | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule          | 1, 17, 18, 19                          |
| Confirmed cases by country                           | map                    | Case, CaseWorkflow, CaseWorkflowStatus                                             | 1, 18, 19                              |
| Response elevation of alerts                         | histogram              | Archive                                                                            | 1                                      |
| Shadow rules against live rules                      | grid                   | Archive                                                                            | 1                                      |
| Rule review                                          | grid                   | Archive, Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule | 1, 17, 18, 19                          |
| False-positive rate by rule over 30, 90 and 365 days | grid                   | Case, CaseWorkflow, CaseWorkflowStatus, EntityAnalysisModelActivationRule          | 1, 17, 18, 19                          |

### Executive: Rule and Model Governance

The Rule and Model Governance dashboard is for the person who must show a regulator that the models are governed. The governance matrix lists every rule, model and Exhaustive model with its state, version, last approval, approver, days since approval against the review interval parameter, whether the current version is awaiting approval and the date of its last backtest. Beside it sit the maker-checker integrity tiles, which include the count of rule changes approved by the person who made them and which should read zero, the population stability index of the response elevation for the window and by week, the hit rate of each rule by week from the counter history, the machine-learning models, the backtests and the oversight of the triage agent.

Parameters:

| Parameter            | Type         | Default            |
|----------------------|--------------|--------------------|
| From                 | date         | 90 days before now |
| To                   | date         | now                |
| Review Interval Days | whole number | 180                |

Tiles:

| Tile                      | Shown as                       | Reads                                                                                                                                 | Permissions needed (any one per table) |
|---------------------------|--------------------------------|---------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------|
| Score stability           | tiles from a template          | Archive                                                                                                                               | 1                                      |
| Maker-checker integrity   | tiles from a template          | EntityAnalysisModelActivationRule, EntityAnalysisModelActivationRuleVersion, EntityApproval                                           | 17, 44, 45                             |
| Governance matrix         | tiles from a template and grid | EntityAnalysisModelActivationRule, EntityAnalysisModel, ExhaustiveSearchInstance, EntityAnalysisModelBacktestInstance, EntityApproval | 6, 16, 17, 44, 45, 61                  |
| Changes by month and kind | stacked column chart           | EntityApproval                                                                                                                        | 44, 45                                 |
| Score stability by week   | line chart                     | Archive                                                                                                                               | 1                                      |
| Rule hit rate by week     | line chart                     | EntityAnalysisModelActivationRuleCounterHistory, EntityAnalysisModelActivationRule                                                    | 17                                     |
| AI triage oversight       | combination chart              | CaseEvent                                                                                                                             | 1                                      |
| Machine-learning models   | grid                           | ExhaustiveSearchInstance, ExhaustiveSearchInstancePromotedTrialInstance, ExhaustiveSearchInstanceTrialInstance                        | 16                                     |
| Backtests                 | grid                           | EntityAnalysisModelBacktestInstance, EntityAnalysisModelActivationRule                                                                | 17, 61                                 |

### Executive: Screening Overview

The Screening Overview dashboard shows the sanction lists and what screening found. It counts the listed entries by source, shows the freshness of every source with a badge, the import results by month, the rows rejected by reason, the hits by match distance, the transactions screened and hits by month, the outcomes of the cases that arose from screening and the screening configuration of each model.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |

Tiles:

| Tile                                    | Shown as                       | Reads                                       | Permissions needed (any one per table) |
|-----------------------------------------|--------------------------------|---------------------------------------------|----------------------------------------|
| Screening summary                       | tiles from a template          | SanctionEntry, SanctionEntryImport, Archive | 1, 11, 29                              |
| Listed entries by source                | bar chart                      | SanctionEntry, SanctionEntrySource          | 11, 29                                 |
| Source freshness                        | tiles from a template and grid | SanctionEntryImport, SanctionEntrySource    | 11, 29                                 |
| Import results by month                 | stacked column chart           | SanctionEntryImport                         | 11, 29                                 |
| Rejected rows by reason                 | donut chart                    | SanctionEntryRejection                      | 11, 29                                 |
| Hits by match distance                  | column chart                   | Archive                                     | 1                                      |
| Transactions screened and hits by month | combination chart              | Archive                                     | 1                                      |
| Outcomes of screening cases by month    | percentage stacked bar chart   | Case, CaseWorkflow, CaseWorkflowStatus      | 1, 18, 19                              |
| Screening configuration                 | grid                           | EntityAnalysisModelSanction                 | 11                                     |

### Executive: Audit Readiness

The Audit Readiness dashboard is the evidence for a regulatory examination. Its checks are written to be answered by zero: alerts from case-opening rules that have no case, closed cases with no close reason, closed cases with no note, cases closed by automation, live rules without an approval and rule changes approved by their maker. Beneath the checks are the alerts and the cases opened from them by rule, the rules awaiting approval, the overrides in force, the complete configuration change log with maker, approver and note, the case suppression requests, the legal holds placed and lifted, and the cases sampled for quality review by month.

Parameters:

| Parameter | Type | Default            |
|-----------|------|--------------------|
| From      | date | 90 days before now |
| To        | date | now                |

Tiles:

| Tile                                      | Shown as              | Reads                                                                                                                                                             | Permissions needed (any one per table) |
|-------------------------------------------|-----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------|
| Readiness checks                          | tiles from a template | Case, CaseWorkflow, CaseWorkflowStatus, Archive, EntityAnalysisModelActivationRule, CaseNote, EntityApproval, EntityAnalysisModelActivationRuleVersion, CaseEvent | 1, 17, 18, 19, 44, 45                  |
| Alerts and the cases opened from them     | grid                  | Archive, EntityAnalysisModelActivationRule, Case                                                                                                                  | 1, 17                                  |
| Rules awaiting approval                   | grid                  | EntityApproval, EntityAnalysisModelActivationRule                                                                                                                 | 17, 44, 45                             |
| Overrides in force                        | grid                  | EntityApproval, EntityAnalysisModelActivationRuleOverride                                                                                                         | 2, 44, 45                              |
| Configuration change log                  | grid                  | EntityAnalysisModelActivationRule, EntityApproval                                                                                                                 | 17, 44, 45                             |
| Case suppression requests                 | grid                  | CaseSuppression                                                                                                                                                   | 1                                      |
| Legal holds placed and lifted             | grid                  | CaseEvent                                                                                                                                                         | 1                                      |
| Cases sampled for quality review by month | column chart and grid | Case, CaseWorkflow, CaseWorkflowStatus                                                                                                                            | 1, 18, 19                              |

## The thresholds

The red, amber and green badges of the key figure tiles are written into the SQL of the tile, in the first lines of the query, and are set as follows. The false-positive rate is amber above seventy percent and red above eighty five percent, the median days to close is amber above two and red above three, the open backlog is amber above fifty and red above one hundred, the share of closed cases missing their resolution target is amber above ten percent and red above twenty five percent, the alert rate is amber above ninety and red above one hundred and twenty per thousand transactions, and the population stability index is amber from 0.10 and red from 0.25. A threshold is changed by editing the SQL of the tile in the Datasource builder, keeping in mind that the next installation of the demonstration data does not overwrite it.

## A briefing from Ask Jooby

Ask Jooby can summarise the dashboards for a board pack by running their Datasources, as it does any Visualisation, with the permissions of the person asking and so only the figures that person may see. A prompt that works well is as follows.

```text
Prepare a board pack summary for the last 90 days against the 90 days before. Run the Datasources of Executive: Board Compliance Overview, Executive: Detection Effectiveness and Geography, Executive: Rule and Model Governance and Executive: Audit Readiness. State the movement in alerts, the false-positive rate, the median days to close and the backlog, name the rules that moved most, list the changes awaiting approval and any check on the Audit Readiness dashboard that is not zero, and name the dashboard and the Datasource that each figure came from.
```

## What is not provided

The Visualisation viewer in this release has no drill-through from a tile, no filter that acts across tiles, no refresh that runs on a timer, no print or export of a whole dashboard and no scheduled delivery. Tables that Visualisation is not permitted to read are not used, which is to say the quality review verdicts, the regulatory reports, the stored service level breaches, the drift observations, the governance records, the entity risk ratings, the sanction rescreening and exemption tables, the erasure requests and the inbound queue, and so the dashboards use the proxies described on this page, such as the close reason Suspicion confirmed and reported as the measure of reporting. Sign in, lockout and role reporting is not offered because the identity tables are never chartable. Training completion, audit findings and examination findings have no source in Jube and are not shown. The names and the text of the seeded tiles are in English.
