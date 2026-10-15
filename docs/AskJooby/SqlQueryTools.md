---
layout: default
title: SQL Query Tools
nav_order: 4
parent: Ask Jooby
---

# The RunSqlQuery family: ad hoc SQL as a last-resort fallback

`RunSqlQueryTool` (`Jube.AskJooby/Tools/SqlQuery/RunSqlQueryTool.cs`) gives every ordinary AskJooby agent six methods
for working with read-only SQL against Jube's own operational database: one that lists what may be queried, four
that inspect or sanity-check a statement without running it, and one, `RunSqlQuery` itself, that actually runs it and
caches the result. `RunSqlQuery`'s own description calls this tool "A FALLBACK, not your first reach" -- every
agent is told to prefer a typed, bound service operation (one of the several hundred catalogued operations
documented in `docs/AgentHierarchy/tool-inventory.md`) wherever one exists, and to reach for raw SQL only for an ad
hoc aggregate, a cross-area join, or a question no bound operation was written for.

This page covers the tool itself and the two-layer safety net underneath `RunSqlQuery` specifically: what it will
and will not let through, and why there are two independent checks rather than one.

## The six methods

* **`ListQueryableTables(nameContains = null)`** -- the full, permission-scoped list of tables this caller and
  tenant may reference in any of the other five methods. The tool's description is insistent that this is called
  *before* writing SQL, not after a guess fails: "a name that is not on this list fails the whole query, and a name
  you invented will not be on it." `nameContains` narrows a genuinely long list by substring, case-insensitively,
  which matters because the unfiltered list, once asked for, stays in the model's context for the rest of the turn.
* **`ValidateSqlQuery(sql)`** -- plans the statement (resolving every table, column, function and type) and throws
  the plan away: nothing is read, nothing is cached. This is the tool to reach for when the SQL was worked out
  against a guessed schema rather than one already queried successfully -- a misspelled column or an incompatible
  type comparison comes back as the real database error with no round trip spent fetching rows that were never
  coming back. It says nothing about whether the query answers the actual question, only that the database
  understands it.
* **`ExplainSqlQuery(sql)`** -- returns Postgres's own plan and cost estimate without running the query: total and
  startup cost, estimated row count, and which of the touched tables get a sequential scan versus an index scan.
  Worth calling before anything expected to be expensive (a wide date range, a multi-area join, an unfiltered
  aggregate); the tool's description is explicit that the costs are the planner's own arbitrary units, not seconds,
  so they are meaningful compared between two phrasings of the same query, not read as an absolute number.
* **`InspectTableStatistics(sql)`** -- reads each referenced table's approximate row count and historical index-scan
  versus sequential-scan counts (from Postgres's own running statistics, not a fresh `COUNT`), and flags any table
  at or above a million estimated rows as one not to read unfiltered. A table whose sequential scans vastly outnumber
  its index scans is called out as "a warning about the table rather than about your query" -- nothing has ever
  reached it selectively. Tables the caller is not permitted to query are left out of the result rather than
  reported as empty, so an absence here is never mistaken for an empty table.
* **`AnalyseQueryHistory(sql)`** -- reads what Postgres has already measured for statements against the same
  tables: call count, average/worst execution time, average rows returned, and cache-hit percentage. The method
  applies three judgement thresholds itself and appends a warning sentence for each that fires: a worst case over
  ten times the average latency ("some parameter values fall off a cliff"), a cache-hit rate under 85% over more
  than 100 calls ("these tables are being read from disk rather than memory"), and an average return over 5,000 rows
  ("put an explicit LIMIT on yours"). When nothing has been recorded for these tables yet, the tool says so and
  points back to `ExplainSqlQuery` for a cold-start estimate instead.
* **`RunSqlQuery(sql, label = null)`** -- the only one of the six that actually executes anything. It runs the
  statement, infers a `CachedColumn` list from the first returned row's keys, and caches the full result through
  `IToolResultCache.StoreAsync` exactly as `CacheAssembledTable` and `GetDemoDataset` do -- see
  `docs/AskJooby/DataPointerCache.md` for the pointer mechanic this feeds into, and `docs/AskJooby/RenderChart.md`
  for what happens to a pointer afterwards. The method returns only a `data_pointer`, the row/column counts, and a
  three-row preview; the model is told to use `InspectDataSchema`/`PreviewDataRows`/`QueryCachedData` against the
  pointer to actually look at what came back, "never assume you can see the raw rows here."

## Two independent gates, not one

`RunSqlQuery` (and the four inspection methods, which all route through the same client) is backed by
`AskJoobySqlQueryClient` (`Jube.Service/DynamicSql/AskJoobySqlQueryClient.cs`), which enforces two checks that have
nothing to do with each other and catch different classes of problem:

1. **`PostgresSqlValidator`** (`Jube.Data/Reporting/PostgresSqlValidator.cs`) parses the statement with a real
   Postgres grammar (`PgSqlParser`) and asserts it is a single, side-effect-free `SELECT` -- `AssertSelectOnly`
   rejects any DDL/DML, any multi-statement input (a trailing `;` followed by more SQL), and any reference to a
   denied function, a denied schema (`pg_catalog`, `information_schema`, `pg_toast`, `pg_temp`, `pg_global`), or one
   of a fixed list of security-sensitive system relations (`UserRegistry`, `TenantRegistry`, `RoleRegistry`, and
   their siblings) -- regardless of whether a caller would otherwise be allowed to see that relation through the
   second gate below. This is the validator `docs/AskJooby/AgentGuard.md` refers to when it explains why
   `RunSqlQuery`'s `sql` argument is exempted from the generic tool-call-argument injection guard: "that tool
   already has its own stronger, AST-level SELECT-only validator... double-guarding it with a generic regex would
   only risk breaking a legitimate analytical query this layer cannot tell from an attack."
2. **`DynamicSqlTableAccess`** (`Jube.Service/DynamicSql/DynamicSqlTableAccess.cs`) is the actual allow-list:
   `RunSqlQuery`'s own description calls it "most tenant-scoped tables in the system," and `ListQueryableTables`
   reflects exactly this list, scoped to the caller's own permissions. For every table on the list,
   `DynamicSqlTableAccess` carries the real permissions required to read it and a SQL *rewrite template* -- not a
   bare table name, but the query Jube actually runs when that table is referenced, pre-joined to
   `TenantRegistry`/`UserInTenant` (and, for model-scoped tables, `EntityAnalysisModelRole`) so that every row
   returned is already filtered to rows this caller's tenant, and this caller's own model-role grants, can see.
   `{{ESCAPED_USER}}` and `{{CONVERSATION_ID}}` are substituted into the template at query time -- the caller's SQL
   never supplies or controls either value, which is exactly why the tool's description says a caller "never needs
   to, and never can, filter by tenant or user yourself." A table referenced in the caller's SQL that is not a key
   in this dictionary fails the whole query with a clear error naming the table, whether or not it exists in the
   real schema -- there is no partial result and no fallback to an unscoped read.

A table can therefore be syntactically legal SQL (passes `PostgresSqlValidator`) and still be refused outright
because it is not in `DynamicSqlTableAccess`'s dictionary, and the reverse never happens: nothing in the allow-list
dictionary is reachable without first passing the AST-level SELECT-only check, because every real query runs through
both in sequence. This is also the practical meaning of `RunSqlQuery`'s advice about soft deletion: the rewrite
templates already apply `("Deleted" = 0 OR "Deleted" IS NULL)` to every joined ownership table on the caller's
behalf, and the tool's own description repeats the same pattern as advice for whatever columns of the target table
itself the caller's own `WHERE` clause touches, because a bare `"Deleted" = 0` on a column that is usually `NULL`
rather than `0` silently returns nothing on perfectly live data.

## Cross-reference

* `docs/AskJooby/DataPointerCache.md` -- what `RunSqlQuery`'s `data_pointer` actually is, how long it lives, and
  every other tool that reads one back.
* `docs/AskJooby/RenderChart.md` -- the most common thing a `RunSqlQuery` pointer is used for next.
* `docs/AskJooby/AgentGuard.md` -- why `RunSqlQuery`'s own `sql` argument is exempt from the generic tool-call
  injection guard, and what else in this tool's own pipeline (`ToolCallAndResultGuardChatClient`) still runs.
* `docs/AgentHierarchy/tool-inventory.md` -- the catalogue of bound, typed operations `RunSqlQuery` exists as a
  fallback for.
