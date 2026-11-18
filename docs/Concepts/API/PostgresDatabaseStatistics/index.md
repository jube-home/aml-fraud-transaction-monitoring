---
layout: default
title: Postgres Database Statistics
nav_order: 8
parent: API
grand_parent: Concepts
---

# Postgres Database Statistics

Whereas [Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html) samples the overall health of the database once a minute and stores the history, Postgres Database Statistics is general DBA information, being table and index statistics, queried live and on demand directly from the system catalogs of Postgres itself (`pg_stat_user_tables`, `pg_stat_user_indexes`, `pg_index`). Nothing is stored by Jube, as these numbers are the running totals kept by Postgres, and there is henceforth nothing to accumulate. The two endpoints exist for the purposes of answering whether an index is missing, and whether indexes are being carried that nothing uses, without connecting to the database with a separate SQL client.

Both are read live over the normal connection of the application (not a dedicated diagnostic one, unlike the samplers of Infrastructure Health Metrics) via the raw SQL query support of `LinqToDB`, following the query pattern already used elsewhere for read-only data that is not tenant-scoped (`Jube.Data.Query`), rather than the pattern of a repository over a stored table that the per-minute metrics use, there being no table owned by Jube to place a repository over. Neither endpoint takes `take`, `from`, `to` or `search` parameters. The row count is naturally bounded by the number of tables or indexes in the schema, and is never an ever-growing log, so the full result set is returned upon every call, and the filtering and sorting of the grid in the user interface is sufficient to explore it.

## Table statistics

`GET /api/PostgresTableStatistics` returns one row per user table, comprising the sequential and index scan counts, the live and dead row estimates, the table, index and total on-disk sizes, and the vacuum and analyze history (`LastVacuum`, `LastAutoVacuum`, `LastAnalyze`, `LastAutoAnalyze`).

`LikelyMissingIndex` is a transparent heuristic, being true when a table has more sequential scans than index scans and more than 1000 live rows. It is a signal to investigate, not a verdict, keeping in mind that a small lookup table scanned sequentially is normal and expected, whereas a large table scanned sequentially on every query usually means that a `WHERE` clause has no index to use.

The source data is the same as that which the endpoint queries live:

```sql
select "SchemaName", "TableName", "SequentialScans", "IndexScans", "LiveTupleCount", "TotalSizeBytes"
from pg_stat_user_tables
```

`DeadTupleCount` relative to `LiveTupleCount` is the other value worth watching, as a table with many more dead than live tuples implies that autovacuum is not keeping up with its write rate, which degrades both query performance and disk usage over time.

## Index statistics

`GET /api/PostgresIndexStatistics` returns one row per user index, comprising the scan count, the tuples read and fetched, the on-disk size, and `IsUnique`, `IsPrimary` and `IsUnused`, the last of which is true when `IndexScans` is zero. It is the direct complement to `LikelyMissingIndex` in the table statistics, and it can be seen that together they answer both where an index is missing and where one is carried that nothing uses. An unused index that is neither unique nor a primary key is close to a free candidate for removal, as it costs write overhead and disk space for no benefit in reading, whereas an unused unique or primary key index still enforces a constraint even at zero scans, henceforth those two flags are to be checked before taking unused to mean safe to drop.

## Permissions and security

Both endpoints are restricted to the landlord tenant, with any other caller receiving 403, and neither is scoped to a tenant or Model, the tables and indexes of a database being shared infrastructure and not tenant data. Each is also presented in the user interface by navigating through the menu to Administration, then Performance, where Postgres table statistics and Postgres index statistics are found in the Postgres family.
