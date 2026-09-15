---
layout: default
title: Postgres Database Statistics
nav_order: 8
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# Postgres Database Statistics

[Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html) samples the database's overall health once a minute and stores the history. This is different: general DBA information -- table and index statistics -- queried live, on demand, directly from Postgres' own system catalogs (`pg_stat_user_tables`, `pg_stat_user_indexes`, `pg_index`). Nothing here is stored by Jube; these numbers are Postgres' own running totals, so there is nothing to accumulate. The two endpoints exist specifically to answer "am I missing an index, and am I carrying indexes nobody uses" without connecting to the database with a separate SQL client.

Both are read live over the application's normal connection (not a dedicated diagnostic one, unlike the samplers above) via `LinqToDB`'s raw-SQL query support, following the same query pattern already used elsewhere for read-only, non-tenant-scoped data (`Jube.Data.Query`) rather than the repository-over-a-stored-table pattern the per-minute metrics use -- there's no Jube-owned table to repository over. Neither endpoint takes `take`/`from`/`to`/`search` parameters: the row count is naturally bounded by the number of tables or indexes in the schema (never an ever-growing log), so the full result set is returned every call and the UI grid's own filtering/sorting is enough to explore it.

## Table statistics -- `GET /api/PostgresTableStatistics`

One row per user table: sequential vs index scan counts, live/dead row estimates, table/index/total on-disk sizes, and vacuum/analyze history (`LastVacuum`, `LastAutoVacuum`, `LastAnalyze`, `LastAutoAnalyze`).

`LikelyMissingIndex` is a simple, transparent heuristic -- true when a table has more sequential scans than index scans and more than 1000 live rows. It is a signal to investigate, not a verdict: a small lookup table scanned sequentially is normal and expected; a large table scanned sequentially on every query usually means a `WHERE` clause has no index to use.

``` sql
select "SchemaName", "TableName", "SequentialScans", "IndexScans", "LiveTupleCount", "TotalSizeBytes"
from pg_stat_user_tables -- (the same source data the endpoint queries live)
```

`DeadTupleCount` relative to `LiveTupleCount` is the other thing worth watching here: a table with many more dead than live tuples means autovacuum isn't keeping up with its write rate, which degrades both query performance and disk usage over time.

## Index statistics -- `GET /api/PostgresIndexStatistics`

One row per user index: scan count, tuples read/fetched, on-disk size, and `IsUnique`/`IsPrimary`/`IsUnused` (`IsUnused` is true when `IndexScans` is zero). This is the direct complement to the table statistics' `LikelyMissingIndex` -- together they answer both "where am I missing an index" and "where am I carrying one nobody uses." An unused index that isn't unique or a primary key is close to a free removal candidate (it costs write overhead and disk space for no read benefit); an unused unique or primary-key index still enforces a constraint even at zero scans, so check those two flags before treating "unused" as "safe to drop."

## Access

Both require the **View Counter and Balance** permission and are not scoped to a tenant or Model -- a database's tables and indexes are shared infrastructure, not tenant data. Each is also browsable via its own page under **Performance** in the UI: **Postgres Table Statistics** and **Postgres Index Statistics**.
