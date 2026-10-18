---
layout: default
title: Tool Icons
nav_order: 12
parent: Ask Jooby
---

# Ask Jooby tool icons and groups

Every tool call Jooby makes shows as a status pill in the chat, and each pill carries an icon for the tool it narrates. The tools are grouped below for documentation. Core tools have an icon each. Analytics tools and entity operations share one icon per group, because there are over 1,500 of them and a single icon per tool would not be readable.

Icons come from Lucide (version 1.52.0, ISC licence), drawn as inline SVG. Unknown tools fall back to a wrench.

## How an icon is chosen

| Tool family | Where the mapping lives | Granularity |
| --- | --- | --- |
| Core agent tools | `Jube.AskJooby/Agents/AgentToolIcons.cs` | One icon per tool |
| Analytics tools | `Jube.Service/Agent/AdvancedAnalyticsTools/AdvancedAnalyticsGroupIcons.cs` | One icon per analytics group |
| Entity and platform operations | `Jube.Service/Agent/PageAgentToolBinder/ServiceOperationIcons.cs` | One icon per domain, first matching rule wins |

The name-to-icon map is filled when each tool family is discovered, so every tool resolves before its first call. Each icon is an `AgentToolIcon` enum value, the single hardcoded list of icons with fixed numeric ids. The id travels on the streamed `Step` item as `IconId`, is stored as `AskJoobyMessage.IconId` so replayed steps keep their icon, and the browser draws it from `Jube.App/wwwroot/js/AskJooby/AskJoobyToolIcons.js`, which is keyed by the same ids. Never renumber the enum: the stored ids depend on it. New icons are appended with the next free id.

Replayed conversations show the same icons, because the id is stored with each step. A step with no stored icon id keeps the 🔧 pill.

The unit test `AgentToolIconsTests` fails if a core tool, an analytics tool or an entity operation resolves to the fallback, or if the server names an icon the browser map does not contain.

## 1. Core agent tools

Each tool has its own icon.

| Group | Tool | Icon |
| --- | --- | --- |
| Research and memory | `SearchRag` | `book-search` |
| | `Remember` | `bookmark-plus` |
| | `Recall` | `bookmark` |
| | `SearchHistory` | `history` |
| | `SearchCompactedHistory` | `archive` |
| Data and SQL | `ListQueryableTables` | `table-properties` |
| | `RunSqlQuery` | `database` |
| | `ValidateSqlQuery` | `shield-check` |
| | `ExplainSqlQuery` | `file-search` |
| | `InspectTableStatistics` | `chart-column` |
| | `AnalyseQueryHistory` | `timer` |
| | `InspectDataSchema` | `columns-3` |
| | `PreviewDataRows` | `eye` |
| | `QueryCachedData` | `filter` |
| | `GetDemoDataset` | `flask-conical` |
| Output | `ExportCachedTableToCsv` | `file-down` |
| | `RenderChart` | `chart-line` |
| Workflows and planning | `FindGraphWorkflow` | `route` |
| | `GetGraphWorkflow` | `workflow` |
| | `GetWorkflowProgress` | `list-checks` |
| | `RecordWorkflowStep` | `square-check-big` |
| | `WritePlan` | `clipboard-list` |
| Delegation and control | `SpawnAgent` | `users-round` |
| | `SpawnAgents` | `users-round` |
| | `HandBack` | `undo-2` |
| | `AskClarifyingQuestion` | `message-circle-question` |

## 2. Analytics tools

The analytics surface has 46 groups. Each group is one icon, and the families below are how they read in the documentation.

| Family | Group | Icon |
| --- | --- | --- |
| Classification | `AdaBoostClassifierTrainingRuns` | `zap` |
| | `BaggingClassifierTrainingRuns` | `layers` |
| | `DecisionTreeClassifierTrainingRuns` | `git-branch` |
| | `GradientBoostingClassifierTrainingRuns` | `trending-up` |
| | `KNearestNeighboursClassifierTrainingRuns` | `network` |
| | `KernelSupportVectorClassifierTrainingRuns` | `slice` |
| | `LinearSupportVectorClassifierTrainingRuns` | `rows-3` |
| | `LogisticRegression` | `percent` |
| | `LogisticRegressionTrainingRuns` | `toggle-right` |
| | `NaiveBayesClassifierTrainingRuns` | `brain` |
| | `NeuralNetworkClassifierTrainingRuns` | `cpu` |
| | `RandomForestClassifierTrainingRuns` | `tree-deciduous` |
| | `TreeAugmentedNaiveBayesClassifierTrainingRuns` | `git-fork` |
| Regression | `DecisionTreeRegressorTrainingRuns` | `sprout` |
| | `GradientBoostingRegressorTrainingRuns` | `chart-spline` |
| | `KernelRidgeRegressorTrainingRuns` | `waypoints` |
| | `KNearestNeighboursRegressorTrainingRuns` | `scatter-chart` |
| | `LinearRegressionTrainingRuns` | `ruler` |
| | `NeuralNetworkRegressorTrainingRuns` | `brain-circuit` |
| | `RandomForestRegressorTrainingRuns` | `trees` |
| | `RelationshipsAndRegression` | `link` |
| Clustering | `BalancedKMeansClustererTrainingRuns` | `scale` |
| | `BinarySplitClustererTrainingRuns` | `split` |
| | `GaussianMixtureClustererTrainingRuns` | `blend` |
| | `KMeansClustererTrainingRuns` | `group` |
| | `KMedoidsClustererTrainingRuns` | `crosshair` |
| | `KModesClustererTrainingRuns` | `boxes` |
| | `MeanShiftClustererTrainingRuns` | `orbit` |
| | `MiniBatchKMeansClustererTrainingRuns` | `package` |
| Anomaly detection | `IsolationForestAnomalyTrainingRuns` | `shield-alert` |
| | `MahalanobisAnomalyTrainingRuns` | `radar` |
| | `OneClassSupportVectorAnomalyTrainingRuns` | `scan-eye` |
| Statistics and tests | `DescriptiveStatistics` | `sigma` |
| | `DistributionsAndHistograms` | `bar-chart-3` |
| | `HypothesisTests` | `test-tube` |
| | `CategoricalAndSurvival` | `hourglass` |
| | `NumericsAndOptimisation` | `calculator` |
| | `ModelEvaluationTools` | `clipboard-check` |
| | `ModelSelection` | `clipboard-check` |
| Multivariate and time series | `GaussianMixtureDensityTrainingRuns` | `waves` |
| | `PrincipalComponentTrainingRuns` | `rotate-3d` |
| | `MultivariateAndClustering` | `combine` |
| | `TimeSeriesAndSignal` | `audio-waveform` |
| Data and platform | `AdvancedAnalyticsTrainingRuns` | `sparkles` |
| | `DataPreparationAndResampling` | `shuffle` |
| | `RScript` | `file-code` |

## 3. Entity and platform operations

Each entity operation takes the icon of its domain. Domains are checked in the order below, so the first match wins. Tree navigation operations (`TreeChildren…`) take the icon of the entity they navigate.

| Domain | Icon | Covers |
| --- | --- | --- |
| Warnings and alerts | `triangle-alert` | `*Warning`, WAF attacks |
| Logs and telemetry | `scroll-text` | OpenTelemetry, OTLP, container and Postgres log entries, activation watcher replay |
| Performance and counters | `gauge` | `*Metric`, performance and processing counters, Redis call and HTTP processing counters, queue balances |
| Platform infrastructure | `server` | Redis, Postgres tables, statements, indexes and activity, Patroni, etcd, HAProxy, Docker, overlay network, .NET runtime, capture queue |
| Ask Jooby | `bot` | Conversations, memory, RAG chunks, files |
| Case management | `briefcase` | Cases, case workflows, session case journals |
| Data preservation and archive | `archive` | Preservation snapshots and recovery, archive tags |
| Access and tenancy | `key-round` | Users, roles, tenants, permission specifications |
| Visualisation | `layout-dashboard` | Visualisation registries, parameters and datasources |
| Model testing and analytics | `flask-conical` | Advanced analytics instances and models, exhaustive search, backtests, history simulations, invocation contexts, integrity checks, samples |
| Authoring helpers | `book-open-text` | Rule vocabulary, query builder operators, parser, icons, completions |
| Rules and models | `sliders-horizontal` | Entity analysis models and their rules, lists, dictionaries, tags, scripts, overrides, synchronisation and dependencies, sanction entries |
| Tree root | `list-tree` | The bare tree navigation root |

## 4. Function-tool groups

The 22 `FunctionTool` groups under `Jube.Service/Extensions` and `Jube.Service/Helpers` each have one icon, in `Jube.Service/Agent/FunctionToolGroupIcons.cs`. They are not discovered or bound by any tool binder, so no tool call uses them. A test fails if any group is added without an icon.

| Group | Icon | Group | Icon |
| --- | --- | --- | --- |
| CalendarFacts | `calendar-days` | TextCleaning | `eraser` |
| ClockAndIdentifiers | `clock` | TextEditing | `pencil` |
| CurrencyAndCountry | `coins` | TextExtraction | `scissors` |
| DateArithmetic | `calendar-plus` | TextInspection | `text-search` |
| EncodingAndHashing | `fingerprint` | TextLists | `list` |
| FinancialIdentifiers | `landmark` | ThresholdsAndLimits | `gauge-circle` |
| Geospatial | `map-pin` | TimeZonesAndFormats | `globe-2` |
| ListScreening | `list-filter` | UnitsAndNumerals | `binary` |
| NameSimilarity | `spell-check` | RoundingAndPowers | `superscript` |
| OnlineIdentifiers | `globe` | StructuredData | `braces` |
| PatternMatching | `regex` | PercentagesAndChanges | `trending-down` |

## Attribution

Icons are from [Lucide](https://lucide.dev), `lucide-static` 1.52.0. Lucide is ISC licensed. Five of the icons used here are derived from the Feather project and carry Feather's MIT licence as well: `crosshair`, `database`, `link`, `percent` and `server`. Both notices are reproduced below, as the licence package states them.

ISC licence (Lucide):

> Copyright (c) 2026 Lucide Icons and Contributors
>
> Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted, provided that the above copyright notice and this permission notice appear in all copies.
>
> THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

MIT licence (the icons derived from Feather):

> Copyright (c) 2013-present Cole Bemis
>
> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
