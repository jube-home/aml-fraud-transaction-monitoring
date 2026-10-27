# Case Key Journal: key lookup index review (J6)

The journal, the peer rebuild and (later) network mapping all start from the same question: which Archive rows have this value in this payload field, for this model, newest first. This review measures how the current lookup behaves, compares the alternatives on the same data, and records what the jobs server does about it.

## What the lookup does today

`GetCaseKeyJournalRowsQuery` filters `Archive` by model, then by `"Json" @> {"payload": {key: value}}` (a string form and a typed number or boolean form joined by OR), then by window, elevation and activation count, and returns the newest 501 by `Id`. It is served by `IX_Archive_Json`, a GIN index with the default `jsonb_ops` operator class on the whole document. The other Archive indexes cannot help, because the key is a payload field, not a column. `IX_Archive_EntityAnalysisModelId_EntryKeyValue` helps only when the case key happens to be the model's entry key.

## How it was measured

PostgreSQL 17.11 in a throwaway container, one node, warm cache, a scratch copy of `Archive` (never the real table) with 1,000,000 rows (732 MB with its current indexes, a payload of about 700 bytes plus the abstraction, counter and activation facets), spread over one model and 50,016 keys of `AccountId`: 20 transactions on average, 3 at the least and 3,224 for the heaviest, with the 16 heaviest keys holding 5 percent of the rows between them. The existing btree indexes of `Archive` were built beside whatever was under test, and every query was the real one, with the same filters and `limit 501`, run twice with `EXPLAIN (ANALYZE, BUFFERS)` and a forced custom plan. Times are execution times, in milliseconds, from the second run.

The scripts are reproduced at the end so the numbers can be re-measured on a copy of real data, which will be larger and colder than this.

## Results

### Lookup time

| Access path | Light key (13 rows) | Heavy key (3,196 rows, 501 returned) | Plan for the heavy key |
|---|---|---|---|
| Today: GIN `jsonb_ops`, containment OR typed containment | 0.33 | 52 to 63 | Walks the primary key backwards and filters, about 15,000 buffers |
| GIN `jsonb_path_ops`, same query | 0.11 | 3.4 to 6.4 | Bitmap scan of the GIN index, then a sort |
| Btree on the expression `(Json -> 'payload' ->> 'AccountId')` | 0.03 | 3.1 to 5.4 | Bitmap scan, then a sort |
| Btree on `(EntityAnalysisModelId, expression, Id desc)` | 0.04 | 0.19 to 0.85 | Backward index scan that stops at 501 rows, about 500 buffers |
| `EntryKeyValue` index, where the case key is the entry key | 0.11 | 7.6 | Index scan, then a sort |
| `ArchiveKey` join, where the case key is a search key | 0.06 to 0.16 | 20 | Index scan, join and sort |

The surprise is the first row. The OR of two containment tests makes the planner prefer reading the primary key backwards and filtering, which touches most of the table for a heavy key. The GIN index is correct, but for the heaviest keys it is not what the planner uses, so the cost grows with the size of the Archive, not with the size of the key. Light keys are fast with every approach; the choice is about the heavy ones and about the cost of keeping the index.

### Cost of keeping the index

| Index set | Index size | Build time | Insert 100,000 rows |
|---|---|---|---|
| Btrees only, no JSON index | none | none | 1.0 s |
| Today: plus GIN `jsonb_ops` | 124 to 133 MB | 14.3 s | 3.2 s |
| Plus the model, expression and `Id` btree, no GIN | 39 MB | 1.0 s | 1.7 s |
| GIN `jsonb_ops` plus that btree | about 170 MB | | 4.2 s |
| GIN `jsonb_path_ops` plus that btree | 130 MB | 5.8 s | 3.2 s |
| Single expression btree | 8.3 MB | | |
| `ArchiveKey` (key and value index) | 56 MB, 150 MB with the table | | |

The GIN index is by far the most expensive part of an Archive write today (the insert is more than three times slower than with btrees alone). The model, expression and `Id` btree adds 0.7 s to the same insert, where the GIN index adds 2.2 s, and takes 39 MB where the GIN index takes about 130 MB.

### The scans the jobs need

| Work | Measured |
|---|---|
| One keyset page of 5,000 rows of the window, by `Id` descending, filtered on the key being present | 2.2 ms |
| One pass counting the rows of the window that carry the key | 129 ms (parallel sequential scan of the million rows) |
| Distinct keys of a model in the window | 1.6 s |

The peer rebuild is therefore bounded by transferring and reading the JSON, not by the database, and keyset paging by `Id` costs almost nothing per page.

## Findings and decisions

1. **A btree on the payload expression is the right index for the journal lookup.** With the model and `Id desc` as its other columns it answers the heaviest key in under a millisecond, reads about thirty times fewer pages than today, costs about a third of the space and of the write overhead of the GIN index, and builds in a second. It also serves the peer sampling and the network lookup, which ask the same question for other keys.
2. **It must be built per key name.** An expression index only serves a query written with the same literal key. The set of case keys and drill fields is configuration (the case key of the activation rules, the drill-flagged Case Workflow XPaths), so it cannot be fixed in a migration. The jobs server therefore creates one index per key name, on demand, with `CREATE INDEX CONCURRENTLY IF NOT EXISTS`, using the migration connection because the application role does not own the table. This is the automated indexing task.
3. **The query only uses the expression form where the index is ready.** Without the index, the expression predicate would scan the table (the 1.6 s above), which is worse than the GIN fallback, so the journal asks `CaseKeyJournalKeyIndex` which keys are ready and uses containment for every other key. A key is also written as containment when its value could mean something else as text: a boolean ("True" against JSON `true`) or a decimal whose text differs from its stored form.
4. **Indexing is off until someone turns it on, and is bounded.** It creates nothing under 100,000 Archive rows (the measured GIN path is fine there), never more than 16 indexes, and one at a time. Building takes seconds per million rows here but holds a long-running transaction, and storage and write cost are real, so the default is `False`.
5. **GIN `jsonb_path_ops` is the cheaper fallback and a separate decision.** It is 30 percent smaller, builds in a third of the time and, for the same containment query, fixed the plan for the heavy key (3.4 to 6.4 ms against 52 to 63). It cannot serve the other operators that `jsonb_ops` supports (key-exists and the like), and `IX_Archive_Json` is also used by other features, so replacing it is not part of this change. It is worth its own measurement on production data, because it would lower write cost for every model.
6. **`ArchiveKey` and `EntryKeyValue` are not general.** `ArchiveKey` holds only the keys the model searches on, so it cannot be relied on for an arbitrary case key, and it costs a further 150 MB and a write for every key per transaction. `EntryKeyValue` is fast, but only when the case key is the model's entry key. Both are used where they happen to apply and neither is a substitute.

## What was built

- `CaseKeyJournalJobs` in `Jube.App` runs both jobs on their own timers, only where they are enabled.
- `CaseKeyJournalKeyIndex` records, per key name, the index the jobs server created or tried to create, its state (building, ready or failed), when, and the error.
- The indexing task takes an advisory lock so that only one node works at a time, checks that every index recorded as ready still exists and is valid (a missing one is marked failed and rebuilt after the retry time), finds the keys in use, skips those that are not safe to put in an index definition, builds missing indexes on the migration connection and records the result. A failed concurrent build leaves an invalid index; the task drops it and marks the key failed, to be tried again on a later cycle.
- The rows query takes the list of ready keys and, for a key in it with an ordinary string or integer value, filters on the expression so that the composite index is used.
- Settings: `EnableCaseKeyJournalIndexing` (False), `WaitCaseKeyJournalIndexing`, `CaseKeyJournalIndexMinimumRows` (100000) and `CaseKeyJournalIndexMaximum` (16).

## What to re-measure on real data

- Cold cache lookups and the plan chosen once the table is far larger than memory.
- A table with several models and a skew across them, since the model leads the composite index.
- The effect of `jsonb_path_ops` on the real mix of queries that use `IX_Archive_Json`.
- Build time and lock behaviour of `CREATE INDEX CONCURRENTLY` on a table under write load.

## Reproducing the measurements

```sql
create table "ArchiveScratch" (like "Archive");
alter table "ArchiveScratch" alter column "Id" add generated always as identity;
insert into "ArchiveScratch" ("EntityAnalysisModelInstanceEntryGuid","Json","EntryKeyValue","ResponseElevation",
  "EntityAnalysisModelId","ActivationRuleCount","CreatedDate","ReferenceDate")
select gen_random_uuid(),
  jsonb_build_object('payload', jsonb_build_object('AccountId','ACC-'||acct, 'Amount', round((random()*5000)::numeric,2)),
    'abstraction', jsonb_build_object('Volume', round((random()*100)::numeric,1)), 'activation','{}'::jsonb, 'tag','[]'::jsonb),
  'ACC-'||acct, random()*10, 1, (g%50=0)::int, now()-(g*interval '4 seconds'), now()-(g*interval '4 seconds')
from (select g, case when g%20=0 then 1+((hashtext('h'||g)::bigint & 2147483647) % 16)
                     else 100+((hashtext('a'||g)::bigint & 2147483647) % 50000) end as acct
      from generate_series(1,1000000) g) s;

create index on "ArchiveScratch" using gin ("Json");
create index "IX_Composite" on "ArchiveScratch" ("EntityAnalysisModelId", (("Json"->'payload'->>'AccountId')), "Id" desc);

explain (analyze, buffers, costs off)
select a."Id", a."Json" from "ArchiveScratch" a
where a."EntityAnalysisModelId" = 1 and (a."Json"->'payload'->>'AccountId') = 'ACC-7'
and a."CreatedDate" >= now() - interval '90 days' and a."ResponseElevation" >= 0 and a."ActivationRuleCount" >= 0
order by a."Id" desc limit 501;
```
