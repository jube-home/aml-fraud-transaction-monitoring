# Case Key Journal analytics: competitive analysis and server-side proposal

This note supports the planned redesign of the Case Key Journal. The journal is the grid of every archived transaction for a case key. Today it returns the newest rows and nothing else; the owner wants the endpoint to compute the statistics and histograms, so that the grid stays a plain view of data the server has already interpreted.

## What the journal does today

`GET /api/GetCaseJournalQuery` runs one SQL statement over `Archive`, filtered by the payload key and value, `ResponseElevation` and activation count, newest first with a limit. It returns a schema and rows. The fixed columns are `Id`, `Activation`, `EntityAnalysisModelInstanceEntryGuid`, `ReferenceDate`, `CreatedDate`, `ResponseElevation`, `EntryKeyValue` and `Tag`. The dynamic columns are one per Case Workflow XPath the caller's roles may see. It computes no statistics, parses the jsonb in .NET row by row, and only ever sees the newest `limit` rows, so any baseline drawn from it is biased towards recent history.

## What a service can already reuse

`DistributionStats`, `Histograms` and the Case Analytics calculators in `Jube.CaseRisk`; `RobustScaler`, `ZScoreScaler`, `Numerics`, `KolmogorovSmirnov`, `Gini` and the categorical counter in `Jube.Analytics`; the statistics helpers in `Jube.Charting` (`Pearson`, quantiles); and the server SVG charts in `Jube.Charting` (sparkline, histogram, box plot, violin, density, calendar, heatmap, small multiples, dot, line, area, Sankey, slope, correlation matrix). Nothing here needs a new statistics dependency.

## Competitor scan

Method: web search summaries only; most vendor interfaces are not publicly documented, so "unknown" means not verified, not absent. Do not use this for marketing claims without demonstrations.

| Capability | Evidence found |
|---|---|
| Transaction list inside a case | Oracle Investigation Hub, Unit21 (events in a case); partial for Featurespace, Feedzai, Hawk, ComplyAdvantage, Fenergo |
| Time series or timeline | Oracle (alerted transactions amount and count graph, investigation timeline); Unit21 graph analysis over time |
| Peer or baseline comparison | SAS AML (history and several peer groups, outliers above and below); partial for ComplyAdvantage, Hawk (unsupervised anomaly detection), Napier |
| Network or link graph | Oracle, SAS (funds tracker Sankey), Feedzai (Genome), Quantexa, Unit21, Sardine |
| Per row compared to normal, per field distributions, outlier badges | Not publicly documented by any vendor checked |

The gap is real: statistical context on each transaction row is where Jube can differ.

## Capabilities to compute on the server

1. Summary strip: count, first and last seen, active days, activations, sum, mean and median of numeric fields.
2. Field progression: each numeric field over time in day or hour buckets, as small multiple sparklines, with the latest point against a rolling median.
3. Per row robust z-score against the key's own history (median and MAD or IQR, clamped at 5 standard deviations to match Exhaustive), with a low confidence flag when there are fewer than about 20 rows.
4. Per field distribution: histogram with Freedman-Diaconis bins and the selected row's percentile rank.
5. Box or violin summary per field, optionally split by a categorical field, with Tukey fences.
6. Outlier flags per row combining per field flags and, when there are enough rows, a multivariate score; opt in and capped.
7. Velocity: transactions per hour and day, the maximum burst in a sliding window against the key's typical window, as a calendar or hour by weekday heatmap.
8. Counterparty profile: top counterparties by count and amount, new against repeat, concentration (Herfindahl).
9. Novelty flags: first occurrence of a counterparty, channel, country or device.
10. Activation and score overlay: response elevation and activations over time and the rule hit mix.
11. Gap and dormancy: log binned inter arrival histogram, longest dormancy, burst after dormancy.
12. Cross field correlation and drift between the recent and earlier halves of history, reporting effect size and n, not significance.

## Proposed response

Structured data first, SVG optional (`include=svg`), sections chosen by `sections=` and `fields=` so each is cheap: `meta` (key, row count, window, baseline rows, truncated, low confidence fields), `schema` with a role per field (measure, category, counterparty, time, geography), `rows` carrying `z`, `outlier`, `novel` and `velocity`, `summary`, `series`, `distributions`, `velocity`, `counterparties`, `gaps`, `correlation`, `drift` and `overlay`.

## Risks and decisions needed

- Baseline: the grid `limit` must not set the statistics window. Use a separate baseline query with its own cap, or SQL aggregates, and say when it is truncated.
- Query cost: the payload key filter has no index found and the query is not model scoped. Measure first; route through `ArchiveKey` or add an expression index.
- Permissions: statistics may only cover XPaths the caller's roles can see.
- Field typing: the schema type is currently the last non-null value, so numerics held as strings are mistyped. Fields need an explicit role, inferred or configured on the XPath.
- Small n: suppress or flag low confidence below about 20 to 30 rows.
- Peer baselines (SAS style) need a cross key population, a precomputed table; decide whether v1 stays with the key's own history.
- Explainability: reasons name the field and the value; prefer per field flags with a combined rank over opaque multivariate scores.
- Release and hold belong in the redesign too: the journal rows are the transactions a hold is stamped on.
