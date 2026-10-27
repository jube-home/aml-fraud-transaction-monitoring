# Case Key Journal: port and extension design

Decided with the owner on 2026-10-25. Companion to `CaseKeyJournalAnalytics.md` (competitive scan and analytics catalogue) and `CasePageDesignAudit.md`.

## What the data holds

Each archived transaction (`Archive.Json`) carries far more than payload fields: `activation` (every activation that fired, each with `visible` and `forced` flags), `abstraction`, `abstractionCalculation`, `ttlCounter`, `dictionary`, `sanction`, `httpAdaptation`, `exhaustiveAdaptation` and its detail, `advancedAnalyticsAdaptation` and its detail, `responseElevation`, `tag`, `createCase`, `matchedGatewayRule` and the invocation performance record. The current journal shows the payload columns and a single activation label, and nothing else.

## Decisions

| Topic | Decision |
|---|---|
| Lenses | Payload, Rules and activations (all of them, with visible, forced and suppressed state and the history over time), Features (abstractions, calculations, counters, dictionaries), Models (Exhaustive and Advanced Analytics scores with their detail), Sanctions and HTTP adaptations. Each lens is a switchable column set with its own statistics. |
| Decision journeys | Each model adaptation on a transaction (Exhaustive, Advanced Analytics and HTTP) carries a full record: value, human label, narrative, model descriptor (family, version, artefact hash), result (threshold, activated), calibration bands, contributions (weight, direction, significance, exact or not, base value) and the decision journey (the path of feature, operator, threshold nodes). The Models lens shows score, label and activated; the transaction card shows the journey, the contribution waterfall and the calibration; the analytics add score trend per model, contribution trend per feature and how stable the decision path is over the key's history. |
| Rows | The key's transactions scored by the model the workflow belongs to. Pivot to any identifier flagged as a drill key. The same key mapping drives network analysis, so the key lookup must be indexed. |
| Normal | The key's own prior history (default 90 days, capped at 5,000 rows) and tenant peers from a precomputed table. Under about 20 rows the statistics are flagged low confidence. |
| Endpoints | Two. Rows first, so the grid fills at once. Then analytics, as data and server SVG, in sections that can be fetched alone. The client does presentation only. |
| Field roles | A Role (measure, category, counterparty, identifier, time, geography) and an optional Segment flag on each Case Workflow XPath, inferred from sampled values until set. The same role feeds network mapping. |
| Peers | A table of per-workflow, per-field quantiles and histogram bins, optionally per segment, rebuilt by the jobs server. The job is a TODO, and peers read as not yet available until it runs. |
| Interactions | Pivot and lenses, tags and bulk tagging, the transaction card and raw payload drawer (audited), Merge, Pin and Around this. |
| Tag all matching | Above what the grid loads, the tag bar offers Tag all matching: it counts the transactions of the current view (case key or pivot, window, activations only, merged lanes) up to 100,000, asks which Tags to add and remove, and queues a `CaseKeyJournalTagJob`. The jobs server runs it as the requesting user in batches of 500 through the same `CaseKeyJournalTagService` (tenant, workflow, case lock and allowed Tags all re-checked each batch), reports progress and counts what was left alone, keeps the previous Tags of every transaction it changed, and can be stopped between batches or undone as a job of its own. The journal finds a job in progress again after a reload. A running job whose node stops reporting for ten minutes is taken up by another. |
| Tags | Bulk tagging goes through `CaseKeyJournalTagService`, not the engine queue: tenant, workflow and tag-name checked, up to 500 transactions, adds and removes as a delta, with the previous sets returned so the step can be undone. A transaction whose case is locked to another user is left alone and counted. |
| Pin | Per case and shared. A pin is a `CaseKeyJournalPin` row (soft unpin, as the app role has no delete grant), at most 50 a case, only for transactions of the workflow's model, refused while the case is locked to another user. Pinned rows come first in every lens and window, marked Pinned, and a Pinned only view shows just them. They are the facts Compose will read. |
| Merge | Up to four keys on one timeline: the case key is lane A and up to three more, picked from a transaction's identifiers, are B to D. Each row says which lanes it belongs to (a row can be in several); the lane letters are the only categorical colours, as a label never a risk signal. Held in the address as `jm`, one rows query per lane, newest first. The analytics stay on the case key. |
| Around this | For one transaction, the other transactions of the same model created the same UTC day that share each of its drill identifiers (the ones the caller's roles may see), up to 25 for each, grouped by identifier, with Open on each. |
| Analytics order | Compared to normal per row and the summary strip; progression and distributions; velocity, gaps and novelty; counterparties, correlation and drift. |

## Stages and acceptance

| Stage | Delivers | Accepted when |
|---|---|---|
| J1b | Transaction card endpoint: archived row, keys, rules with versions, and every adaptation's decision journey, contributions, calibration and narrative, with the raw payload audited | Tests for each adaptation source, missing detail, visibility and the audit record |
| J1 | Rows endpoint: model scoped, window and limit, pivot, all five lenses read from the Archive JSON, visibility rules, typed schema with roles | Service tests on real Postgres cover each lens, visibility, pivot and scoping; mutation checks on the visibility and scoping guards; lint clean |
| J2 | Role and Segment on Case Workflow XPath: migration, DTO and form, inference, validation | Migration applied on a fresh database; round trip and validation tests; inference tested on typed samples |
| J3 | Analytics endpoint sections, with the calculators as pure code in `Jube.CaseRisk` | Calculator unit tests with mutation checks; endpoint tests for permissions, low confidence and truncation |
| J4 | Peer table, lookup and empty state; TODO marker for the job; indexing note | Migration with column comments; lookup tests |
| J5 | Vue journal: lens switch, aggregation strip, charts, z-shaded grid, pivot, window, tags, drawers, Merge, Pin, Around this | Vitest and Playwright with axe in both themes; checked in Chrome on real data |
| J6 | Index review for the key lookup and network mapping | Written recommendations with measured plans |

## Open TODOs

- Conditional styling for the journal grid and strip, with colours and a bold line, to be settled and built next session. Owner's wording: "we need to make sure that we have the conditional styling and colours and bold line tomorrow". Scope not yet pinned down: which cells or rows are styled (activations, elevation, stands-out values, lanes), which condition drives each colour, and what the bold line marks (a row, a day boundary or a threshold). Colour must carry state only, always with a word or icon, and stay within the brand contrast rules.
- Peer rebuild: a distribution that stops qualifying (fewer than 20 keys, or a field that is no longer a measure) is not removed, because the application role cannot delete; it ages until the next rebuild replaces it. `ComputedDate` shows how old it is.
- Tag jobs: the previous Tags are kept for ever with the job (nothing is deleted); an undo cannot be undone.
- Indexing: dropping an index for a key that is no longer used is manual.
- Peers are built per case key name, so a pivot to another identifier reads as not computed.
- Replay the journal (planned in `CaseKeyJournalReplayPlan.md`, which answers the questions below from the code and lists the decisions still the owner's; not built). Place the Watcher on the Case Key Journal, above the Network, replaying the key's transactions in time order on the map instead of activations, with the same controls (live is off, replay from and to, fade over, trails, heat, fit). The first use is improbable travel: a key whose consecutive transactions are too far apart for the time between them. To plan before building:
  - Where the position comes from: the Watcher plots activations by the location the engine resolved; whether every journal transaction carries a location, or only those with a configured geography field (role 6), and what to do with those without one.
  - Which transactions are replayed: the loaded view (key or pivot, window, merged lanes, pins), or the whole key; and how it follows the grid's selection, the transaction drawer and the lens.
  - Filters that are needed and do not exist yet: by speed or distance between consecutive transactions (the improbable-travel threshold, as the Watcher's 900 km/h), by lane, by activations, by elevation, by tag, by value range, and by hiding transactions with no location.
  - Whether the travel analysis is computed on the server (as the other analytics are) and returned with the rows, so that the map only draws, and whether it is also a notable reason in "stands out".
  - Reuse: the Watcher component is a page today, so it needs splitting into the map and replay engine, which the journal embeds, and the page.
  - One component, two sources: the Watcher plots a minimal set that arrives from the activations stream, and the journal replay plots a far more comprehensive set (every transaction of the key, with its lane, pin, tags, elevation and distance and speed from the one before) that arrives from the journal rows. Duplicating the component would be a shame, so plan it as one map and replay component that takes its points from a source (the activation stream or the journal rows), with the source deciding what a point carries, how it is filtered and what its popover shows. Decide the shape of a point and of a source before building either.
- Legacy `GET /api/GetCaseJournalQuery` stays until the Razor page is retired.

## Status, 2026-10-25

| Stage | State |
|---|---|
| J1 Rows endpoint | Built: `GET /api/CaseKeyJournal`, five lenses, model scoped, tenant and role checked, containment lookup through the Archive GIN index, roles on every column |
| J2 Field roles | Built: `RoleId` and `Segment` on the Case Workflow XPath, migration `AddFieldRoleToCaseWorkflowXPath`, inference when unset |
| J3 Analytics endpoint | Built: `GET /api/CaseKeyJournal/Analytics` with summary, notable rows (robust z-score), progression, distributions, velocity, gaps, novelty, counterparties, correlation, drift and a peers placeholder, and `GET /api/CaseKeyJournal/Chart/{kind}` for server SVG |
| J1b Transaction card | Built: `GET /api/CaseKeyJournal/Transaction` (activations, features, keys, and for every Exhaustive, Advanced Analytics and HTTP model the score, threshold, model, calibration, contributions and decision journey), `Transaction/Payload` (visible fields only, audited) and `Transaction/Chart/{kind}` (contribution waterfall or bars, calibration curve) |
| J4 Peers table and job | Table, lookup and empty state built; the job that fills the table is a TODO (see below). `CaseKeyJournalPeer` holds, per workflow, field and optional segment, the distribution of each case key's median of the field (key count, rows, median, robust scale, quartiles, range, mean, histogram bins, window and build date). The analytics `peers` section compares the key's median with it as a robust z-score and a percentile, uses the key's segment when the workflow has a segment field and falls back to all keys when that segment has no peers, and says `NotComputed` or `NoMeasures` when there is nothing to show. The Vue journal shows it as a Compared with peers card. |
| J5 Vue journal | Built in part: lens switch with counts, window and activations-only, pivot (kept in the address), aggregation strip, what stands out, server charts, a grid with an unusual and new-value column, a transaction drawer with each model's decision path and contribution chart, calibration and an audited payload read. Then tags and bulk tagging with undo, Merge (lettered lanes), Pin and Pinned only, and Around this |
| J6 Index review | Done: measured on a million-row copy of Archive, written up in `CaseKeyJournalIndexReview.md`. The decision is a btree on the model, the payload field value and the Archive identity, built per key by the jobs server, which the journal uses where it is ready |
| Jobs server | Built in `Jube.App` as `CaseKeyJournalJobs`: the peer rebuild (on by default, daily, `CaseKeyJournalPeerJob`) and the indexing task (off by default, hourly, `CaseKeyJournalIndexJob`, under an advisory lock so one node works at a time). Settings are in the environment variable reference |

### The analytics, as built

Every section is computed by pure calculators in `Jube.CaseRisk/Journal`, over up to 2,000 transactions of the key (cap 5,000) in the window, newest first. A field's normal is its own median and scaled median absolute deviation, falling back to the interquartile range and then the standard deviation. Under 5 values nothing is judged, under 20 the report is flagged low confidence, a constant history treats any difference as the maximum score (5), and every score is clamped to plus or minus 5, as the Exhaustive z-scores are. Measures are the numeric columns whose role is Measure, taken from the payload (as the caller's roles allow), the features lens and the models lens, up to forty. Categories for novelty are the category, counterparty, identifier and geography columns. Drift needs 40 transactions and 20 either side, and uses the two-sample Kolmogorov distance with the 5 percent critical value.

### Open questions for the owner

- Whether the key being pivoted to should be limited to the case key and drill-flagged fields.
- Whether features, models and sanctions should be gated by role, as payload fields are.
- Whether the first release should also draw the decision journey per model (needs J1b).

### The transaction card, as built

A payload input the caller's roles may not see is hidden everywhere it could be named: contributions and decision-path nodes whose source is Payload and whose name is not among the workflow's visible XPath names (or the last segment of their expression) come back as `hidden`, with no name, weight, threshold or label, and are counted. Feature, abstraction, counter and dictionary inputs are never hidden. A contribution is drawn as a true waterfall from the base value only when the model says it is exact; otherwise it is drawn as signed bars labelled not additive, because relative weights do not add up to a score. Model narratives are the model's own text and are not filtered, so a model owner should not put hidden field names in a narrative. The raw payload endpoint returns the visible XPath fields only, which is a deliberate narrowing of "raw".
