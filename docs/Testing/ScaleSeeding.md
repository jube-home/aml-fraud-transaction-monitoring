---
layout: default
title: Scale Seeding
nav_order: 28
parent: Testing
---

# Scale Seeding

The demonstration data is sized for a person to read, not for a database to sweat. To build confidence that the archive, the case tables and their indexes hold up as volume grows, there is an opt-in script that seeds a million transactions and a hundred and fifty thousand cases, and a benchmark file that times the paths the product actually uses. Neither runs as part of a normal migration: they are for a test database, chosen by you, when you want the answer.

## Why it is a script and not a migration

A migration runs on every installation that has the demonstration model. Adding a million archive rows and over three million archive keys to every one of them would turn a minute of migration into several, and add about four gigabytes of storage to a demonstration. So the volume seed lives beside the migrations as plain SQL, takes its sizes as arguments, and refuses to run twice. The smoke test seed, by contrast, is small and is a migration (see Smoke Test Seed Data).

## Running it

The script is `Jube.Migrations/Scripts/ScaleSeed.sql`. It needs the example model, tenant 1 and role 1, which any demonstration database has. Never point it at a database that holds real data.

```
PGOPTIONS="-c jube.scale_archive=1000000 -c jube.scale_cases=150000" psql -d <database> -v ON_ERROR_STOP=1 -f Jube.Migrations/Scripts/ScaleSeed.sql
```

Both settings are optional and default to the values above; the settings are the house convention for the scripts in that folder, so a runner other than psql can set them with `set_config` and send the steps one at a time. It works in chunks of 100,000 rows with a commit after each, prints a progress line per chunk, and finishes by analysing the tables so the planner has current statistics. If the scale data is already present it says so and stops. It creates its own workflow, `Scale: Case Volume`, granted to the Administrator role, so the cases appear in the workflow picker and do not mix with the demonstration cases or the smoke test cases.

| What | How much (defaults) |
|---|---|
| Archived transactions | 1,000,000 across the last 365 days, with a coordinate, a country (weighted towards the United Kingdom) and a channel on each |
| Archive keys | About 3.1 million: the account, the amount and the country for every transaction |
| Accounts | About 150,000 distinct, with a heavy skew so a few accounts have thousands of transactions and most have a handful |
| Alerts | One transaction in every 6 or 7 fires a rule, enough to open the requested number of cases |
| Cases | 150,000, of which 15% are open (about 25,000) and a quarter of the open ones are held by one of seven analysts |
| Case history | A first-view event, an optional lock event and a close event; one status transition and one risk assessment per case; a closing note on 60% of closed cases |

On the machine used for the measurements below (a laptop, PostgreSQL 17 in a container, default settings) the default run took 247 seconds and the tables and indexes occupied this much:

| Table | Rows | Size with indexes |
|---|---|---|
| Archive | 1,026,571 | 1.8 GB (the jsonb index alone is 378 MB) |
| ArchiveKey | 3,106,038 | 1.7 GB across seven indexes |
| Case | 169,806 | 343 MB |
| CaseEvent | 367,075 | 84 MB |
| CaseRiskAssessment | 167,741 | 52 MB |
| CaseStatusTransition | 170,838 | 44 MB |
| CaseNote | 77,258 | 21 MB |

## Measuring it

`Jube.Migrations/Scripts/ScaleBenchmark.sql` runs 25 statements under explain analyse. The first is the real Cases page query (the triage list, taken from the case triage SQL, for the All cases view of the scale workflow). The rest are the queue chips, Take next, stuck locks, the supervision workload, the case by key guard that stops duplicate cases, the key journal's path through the archive key table, single-transaction lookups, and the date-bounded archive analytics that the dashboards depend on.

```
psql -d <database> -f Jube.Migrations/Scripts/ScaleBenchmark.sql
```

Read the Execution Time line under each heading, and look for a Seq Scan on `Archive`, `ArchiveKey`, `Case` or `CaseEvent`: on these tables that is the signature of an index that is missing or is not being chosen.

## How the seeds are tested

Both seeds are covered by tests that run in the normal pipeline, and are written so a slow build agent cannot make them fail: they assert counts, shapes and relationships and never a duration.

- The smoke test seed tests read the catalogue page and the database together. They fail if a scenario promises a different number of cases than the seed created, if the seed holds a case the page does not list, if a scenario lacks the data that drives its gate (an open checklist item, a stale override, an ungranted status), or if the database stops refusing the states the catalogue says it refuses. They return without asserting on a database that has no smoke seed.
- The scale seed tests run the real script at 2,000 transactions and 300 cases, check the volumes and links and that a second run does nothing, then remove what they created. They never touch larger scale data that is already in the database. A second test plans every statement of the benchmark file against the current schema, without running them, so a renamed column fails in the pipeline instead of the next time somebody benchmarks.
- The seeded dashboard library test renders every datasource through the card service, with the product's own 30 second limit. The migrations end with an analyse of the large tables because a database that has just been seeded has no planner statistics, and the tenancy rewrite of the case table can choose a plan that is slow enough to hit that limit until statistics exist. That was seen once, on a database that had just been restored, and went away after an analyse.

## What was measured

All times are milliseconds on a warm cache with one connection. They show how the data volume scales, not how concurrent load scales (see What this does not show).

| Path | Time | Plan |
|---|---|---|
| Cases page, triage list of 100 over about 25,000 open cases | 129 | Index scans on the case, event, assessment, transition and view tables; scans only the small configuration tables sequentially |
| Chip: open | 4 | Partial index on open cases |
| Chip: Mine | 0.1 | Index on workflow and lock holder (open) |
| Chip: Free to take | 30 | Partial index on open cases, filtered |
| Chip: Urgent | 2 | Partial index on open cases |
| Chip: Quality review | 29 | Partial index on open cases, filtered |
| Take next | 31 | Index on the triage order, first six rows |
| Stuck locks (200) | 5 | Index on workflow and lock holder (open) |
| Supervision workload | 4 | Index on workflow and lock holder (open) |
| Open one case by id | 0.04 | Primary key |
| Case by key, the duplicate guard | 0.1 | Partial index on workflow, key and value for open states |
| Other cases for the busiest account (all statuses) | 1 | Index on key, value, workflow and closed state |
| Events of one case, a thousand events | 58 | Index on case and event id |
| One transaction by entry guid | 0.03 | Index on the entry guid |
| One transaction by entry key value | 0.02 | Index on the entry key value |
| Archive, last 24 hours | 0.4 | Index on model and reference date |
| Archive, last 7 days, alerts by rule | 55 | Bitmap scan on model and activation rule |
| Archive, 90 days, transactions per day | 74 | Parallel sequential scan (the 90 days is a quarter of the table, so the planner prefers it) |
| Archive, a full year, transactions per month | 290 | Sequential scan (the whole table) |
| Archive, rule league over a year | 118 | Parallel sequential scan |
| Key journal, busiest account (4,490 transactions) | 20 | Bitmap scan on the key value string index, then the entry guid index |
| Key journal, an ordinary account | 0.2 | The same indexes |
| Archive, jsonb containment on payload country over 30 days | 2 | Bitmap scan on the jsonb index |
| Cases opened per day, 90 days | 8 | Sequential scan (a quarter of the table) |
| Cases closed per day, 90 days | 7 | Sequential scan (a quarter of the table) |

Every dashboard and report datasource seeded for the executive and library packs was then run against the same database with its parameters substituted: 197 datasources at their default windows, then again with every window stretched to a year. There were no errors and none exceeded the 30 second limit.

| Datasource windows | Slowest | Over one second | Over five seconds |
|---|---|---|---|
| Defaults (7 to 90 days) | 1.2 s (Detection summary) | 2 of 197 | none |
| Stretched to 365 days | 2.1 s (Value by a payload field) | 9 of 197 | none |

For comparison the same datasources on the demonstration database, with about 26,000 transactions, had a slowest of 0.5 s. That is a forty fold increase in archive volume for roughly a two and a half fold increase in the slowest dashboard, so the growth is well below linear over this range.

## Reading the results

The indexes that matter for the day to day paths are all being used, and nothing on the hot paths scans a large table. The things worth knowing as volume grows further are these.

- **Chips and Take next scale with the open backlog, not the history.** They were 30 to 50 ms at 25,000 open cases and are served from partial indexes on open cases, so a backlog ten times the size would be expected to take about ten times as long for the filtered chips. The closed history does not affect them.
- **The date-bounded archive analytics become sequential scans once a window covers a quarter or more of the table.** That is the planner choosing correctly at this size, and it is why every library report defaults to a short window. It is also the point at which partitioning the archive by reference date would start to pay for itself, well beyond a million rows.
- **The archive key table is half the archive storage.** Seven indexes are kept on it, and only the string value and entry guid indexes appeared in any measured plan; the float, integer, date and boolean value indexes (165 to 185 MB each at this size) were not used by these paths. They may be used by aggregation over numeric keys, which was not measured here, and each one is maintained on every insert.
- **The jsonb index is the largest single index on the archive** and the one that makes payload field searches fast; it is used for the containment query measured above.

## What this does not show

- **Concurrent load.** Every figure is one connection at a time. Contention between the engine writing transactions, the sweeps updating cases and many analysts reading the queue is not measured. A load test with a connection pool against this data set would be the next step.
- **Cold caches.** The figures are warm. A first read after a restart is slower, on the first measurement pass by up to a factor of two.
- **Larger volumes.** The script takes the sizes as arguments, so ten million is a command line away, but only one million was run. Extrapolation beyond that is an estimate.
- **Realistic skew and churn.** The distributions are synthetic, with no updates or deletes over time, so index bloat and vacuum behaviour under churn are not exercised.
- **The product's own query path.** The datasources were run as SQL with their parameters substituted, not through the card service, so the tenancy rewrite and rendering time are not included. The triage query was reproduced from the source rather than captured from a running application.
