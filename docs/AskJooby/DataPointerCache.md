---
layout: default
title: Data Pointer Cache
nav_order: 5
parent: Ask Jooby
---

# The data_pointer mechanic: caching large or tabular tool results

Several AskJooby tools -- `RunSqlQuery`, `GetDemoDataset`, `CacheAssembledTable`, and, on the general service-tool
surface, any bound operation whose result is large -- do not hand their rows back to the model. They store the rows
once, on the backend, and hand back a short string called a `data_pointer` instead. Everything that reads cached data
afterwards -- `InspectDataSchema`, `PreviewDataRows`, `QueryCachedData`, `RenderChart`, `ExportCachedTableToCsv`, and
`RunScript`'s `dataframes` argument -- takes that pointer, never the rows themselves. This page is the mechanic
behind all of them: what a pointer actually is, where the rows live, how big a table can get before it has to become
one, and what reading it back actually costs.

This is the single most load-bearing piece of plumbing in the whole AskJooby tool surface. Almost every tool that
returns more than a few rows goes through it, and almost every tool that lets a person see a chart, a CSV download,
or a filtered slice of a big table reads back through it.

## Why a pointer at all

A tool's return value becomes part of the model's context, and a model's context is not transient: once a result is
there, it is resent on every remaining model call for the rest of the turn, because the whole conversation so far is
what gets sent each time. A `RunSqlQuery` that returned ten thousand rows inline would not cost ten thousand rows'
worth of tokens once -- it would cost that many tokens again on every tool call and every reasoning step for the rest
of the turn, long after the model has stopped caring about ninety-nine percent of what it is carrying. Jube's own
`AskJoobyMaxToolResultCharacters` environment variable doc (`docs/Concepts/EnvironmentVariables/index.md`) puts the
general version of this precisely: "a tool result stays in the model's context once it is there, so it is resent on
every remaining model call of the turn: a single large listing is paid for repeatedly and can exhaust the context
window outright."

The fix is the same shape everywhere: a tool that would return a lot of structured data stores it once, keyed by a
pointer, and returns the pointer plus a short preview instead of the data. The model is told the shape of what it
got -- row count, column names, a handful of sample rows -- and has to come back with a second, narrower call
(`QueryCachedData`, a tighter SQL `WHERE`, a chart) to actually read more of it. This also teaches a better habit:
`CachedDataTool`'s `InspectDataSchema` description says plainly that part of its job is helping the model "judge
whether reading more of it is sensible or absurd before you ask for it" -- the pointer does not just save tokens, it
turns "give me everything" into a question the model has to answer for itself first.

## The two pointer families, and why there are two

There are, in fact, two independent pointer stores behind one shared read-side interface, `IToolResultCache`
(`Jube.AskJooby/DataCache/Interfaces/IToolResultCache.cs`):

```csharp
public interface IToolResultCache
{
    Task<string> StoreAsync(CachedTable table, string? label = null, TimeSpan? ttl = null,
        CancellationToken token = default);

    Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default);
}
```

`AskJoobyDataCacheClient` (`Jube.Service/AskJoobyDataCache/AskJoobyDataCacheClient.cs`) is the concrete
implementation every AskJooby chat tool is actually constructed with. Its `GetAsync` is deliberately polymorphic over
the shape of the key:

```csharp
public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
{
    if (advancedAnalyticsCache is not null && Guid.TryParse(pointerKey, out var pointer))
    {
        return advancedAnalyticsCache.GetAsync(pointer, token);
    }

    return GetFromOwnStoreAsync(pointerKey, token);
}
```

A pointer AskJooby itself minted always looks like `tbl_<32-hex-digit guid>` (see "Minting a pointer" below) and is
never a bare GUID, so it always falls through to `GetFromOwnStoreAsync`. A bare GUID -- no `tbl_` prefix -- is
instead routed to `IAdvancedAnalyticsCachedTableReader`, the read side of the separate cache that
`AdvancedAnalyticsTrainingDataPut` (`Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsTrainingData.cs`)
and the statistical/training tool layer under `Jube.Service/Agent/AdvancedAnalyticsTools` use for cached numeric
training data. The practical effect: a pointer produced by `AdvancedAnalyticsTrainingDataPut` can be read straight
back by `InspectDataSchema`, `PreviewDataRows`, `QueryCachedData`, `RenderChart` or `ExportCachedTableToCsv` -- the
chat-tool pointer family and the training-data pointer family share one read path even though they are stored in
different places, so a model never has to care which kind of pointer it is holding, only which tool handed it one.

## Minting a pointer: what `StoreAsync` actually does

`AskJoobyDataCacheClient.StoreAsync` (`Jube.Service/AskJoobyDataCache/AskJoobyDataCacheClient.cs`):

1. Refuses outright if the table has more rows than `maxRows` (default 20,000): "Cannot cache {n} rows -- the limit
   is {maxRows}. Aggregate, page, or filter the result down before caching it." A tool that would overflow this
   limit has to fail at the point of caching, not silently truncate -- there is no silent row-dropping anywhere in
   this path.
2. Mints the pointer key itself: `$"tbl_{Guid.NewGuid():N}"` -- always this exact shape, which is what lets
   `GetAsync` tell a chat-cache pointer apart from an Advanced Analytics training-data GUID without needing a second
   argument.
3. Serialises the table's columns and rows to JSON independently (`ColumnsJson`, `DataJson`).
4. Computes an expiry: `DateTime.UtcNow.Add(ttl ?? defaultTtl)`, where `defaultTtl` is **60 minutes** unless the
   caller overrides it. No AskJooby tool that calls `StoreAsync` passes a non-default `ttl`, so every pointer a
   person sees in practice expires an hour after it was minted.
5. Inserts one row into the `AskJoobyDataCache` table (`Jube.Data/Poco/AskJoobyDataCache.cs`) via
   `AskJoobyDataCacheRepository.InsertAsync`, carrying the pointer key, the two JSON blobs, the row count, an
   optional `Label` (the caller-supplied hint -- `RunSqlQuery`'s `label` parameter, or `CacheAssembledTable`'s), the
   `TenantRegistryId`/`AskJoobyConversationId` it belongs to, and the computed `ExpiresAt`.

Reading it back (`GetFromOwnStoreAsync`) looks the row up by pointer key, deserialises both JSON blobs, and
normalises any `JsonElement` values in a row back into plain CLR scalars (`NormalizeElement`) so that the rest of the
pointer-consuming tools can compare and format values without caring that they came out of a JSON column. A pointer
that has expired, was never minted, or belongs to a different conversation's row simply is not found -- every
pointer-consuming tool treats "not found" identically ("No cached table matches that pointer -- it may have expired,
or never existed."), without distinguishing why.

A `CachedTable` itself (`Jube.AskJooby/DataCache/Models/CachedTable.cs`) is nothing more than an ordered list of
`CachedColumn(Name, Type)` plus a list of rows, each row an `IReadOnlyDictionary<string, object?>` keyed by column
name -- there is no schema object richer than "a name and a .NET type name string" anywhere in this path. Every
pointer-producing tool infers that type string itself, from whichever row has the first non-null value for a given
key (`InferType` appears, nearly verbatim, in `RunSqlQueryTool`, `CacheAssembledTableTool`, and the general
`LargeResultPointer` helper below) -- a column that is null in every row it cached is reported as type `unknown`.

## The tools that mint a pointer

* **`RunSqlQuery`** (`Jube.AskJooby/Tools/SqlQuery/RunSqlQueryTool.cs`) -- runs the caller's read-only SQL, builds
  `CachedColumn`s from the first row's keys, and calls `cache.StoreAsync` with the caller's own `label`. See
  `docs/AskJooby/SqlQueryTools.md` for the rest of this tool's methods (listing tables, validating, explaining,
  inspecting table/query statistics) -- only `RunSqlQuery` itself produces a pointer; the others are all read-only
  sanity checks against SQL that is never executed for real.
* **`GetDemoDataset`** (`Jube.AskJooby/Tools/DemoDataset/DemoDatasetTool.cs`) -- loads a fixed, anonymous, built-in
  sample (`"transactions"`, a labelled card-transaction set with a `Dependent` fraud/genuine column) from
  `IDemoDatasetProvider` and caches it the same way, so a caller who wants to try analysis or model training without
  naming real data gets a pointer that works with every other tool in this family exactly as a real query result
  would.
* **`CacheAssembledTable`** (`Jube.AskJooby/Tools/Cache/CacheAssembledTableTool.cs`) -- the pointer-producing tool
  for data the model itself assembled, rather than data a query returned directly: facts gathered across several of
  its own tool calls, or numbers pulled out of several specialists' separately-returned hand-back answers. It takes
  `rowsJson`, a JSON array of same-shaped objects (every row must carry exactly the same set of keys, checked by
  comparing each row's sorted key list to the first row's and throwing on any mismatch), parses each JSON scalar down
  to a plain CLR value (`ToScalar`: string, `long`-or-`double` number, `bool`, or, for anything else, the raw JSON
  text), and caches the result precisely as `RunSqlQuery` would. This exists specifically because `RenderChart` and
  `ExportCachedTableToCsv` only ever read from a cached table, never from values sitting in the model's own
  reasoning -- without `CacheAssembledTable`, a number the model derived by combining several tool results could be
  *stated* in prose but never charted or exported. `HandBackTool` and `SpawnAgentTool`'s own descriptions both tell
  the model to reach for `CacheAssembledTable` before handing several gathered facts upward, for exactly this reason
  -- see `docs/AskJooby/OrchestrationTools.md`. `Jube.AskJooby/Prompts/DataPointerPreference.cs` states the general
  rule this tool exists to serve: "Data that might end up charted or exported should be reached through a
  data_pointer, not carried as numbers in prose... CacheAssembledTable is there for the rest -- facts you have
  gathered yourself, or assembled from several specialists' separate hand-back answers."
* **`AdvancedAnalyticsTrainingDataPut`** (`Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.AdvancedAnalyticsTrainingData.cs`)
  -- outside the AskJooby chat-tool namespace, but shares the same read side: turns a cached table into the numeric
  training data a training run reads, keyed by a bare GUID pointer rather than a `tbl_`-prefixed one (see "The two
  pointer families" above).
* **General bound service operations, via `LargeResultPointer`** (`Jube.Service/Agent/PageAgentToolBinder/LargeResultPointer.cs`,
  internal) -- this is the mechanism described, in passing, by the `AskJoobyMaxToolResultCharacters` row of
  `docs/Concepts/EnvironmentVariables/index.md`: any one of the several hundred catalogued service tool operations
  (list/filter/count operations across the whole platform, not only AskJooby's own chat tools) whose serialised
  result would exceed `AskJoobyMaxToolResultCharacters` (default 6000 characters) has that result intercepted,
  stored through the same `IToolResultCache` as a `CachedTable`, and replaced with a short description plus a
  `data_pointer` -- in the same shape a `RunSqlQuery` result arrives in, so `InspectDataSchema`, `PreviewDataRows`
  and `QueryCachedData` read and filter it exactly as they would a query result. This is how, for example, a
  `WafAttackList` call capped at a hundred thousand rows never actually lands a hundred thousand rows in a model's
  context: the operation itself still runs and still returns everything it was asked for underneath, only what
  travels into the model's context is reduced. Setting `AskJoobyMaxToolResultCharacters` to `0` disables this
  mechanism entirely and restores the previous behaviour of returning every bound-tool result inline, however large.

## The tools that read a pointer back

All three live in `CachedDataTool` (`Jube.AskJooby/Tools/DataCache/CachedDataTool.cs`), constructed with nothing but
the shared `IToolResultCache`:

* **`InspectDataSchema(dataPointer)`** -- returns row/column counts, each column's inferred type and null count, and
  a cost estimate: the table's JSON-serialised character length and that length divided by 4 (`CharactersPerToken`)
  as a rough token count, with an explicit nudge to filter or aggregate rather than read a table "whose cost is out
  of proportion to the question being answered." Every pointer-producing tool's own description tells the model to
  call this first, or to already know the schema from when the table was created, before reaching for either of the
  next two tools.
* **`PreviewDataRows(dataPointer, limit = 5, offset = 0)`** -- a windowed look at real rows, capped at 20 per call
  (`MaxPreviewRows`) regardless of what `limit` asks for, with `offset` for stepping through a table that does not
  filter cleanly. The tool's own description is explicit that walking a large table window by window will typically
  cost more than every row of it would in one shot, and that a tighter filter or a narrower SQL query is nearly
  always the better move.
* **`QueryCachedData(dataPointer, filterExpression, limit = 20)`** -- a small filter language evaluated entirely on
  the backend, never pulling the full table into the model's context to filter it there. `filterExpression` is one
  or more `column op value` clauses joined with `&&`; supported operators are `=`, `!=`, `>`, `<`, `>=`, `<=` and
  `contains` (substring, case-insensitive, text columns only), checked in that order against each clause's text so
  that `>=`/`<=` are tried before the bare `>`/`<` they would otherwise be mistaken for. A clause's right-hand side
  is parsed as a number and compared numerically whenever the column's actual value is numeric and the text parses
  as a number; otherwise both sides are compared as ordinal, case-insensitive strings -- so a column typed as text
  that happens to hold digits is compared lexically unless the row value itself is a numeric CLR type. An unknown
  column name fails the whole call with the real column list rather than silently matching nothing. Results are
  capped at 50 rows (`MaxQueryRows`) however high `limit` is set.

`ExportCachedTableToCsv` and `RenderChart` also read a pointer, but they do not hand rows back to the model at all --
see "Where the rows actually go" below and `docs/AskJooby/RenderChart.md`.

## Where the rows actually go: export and charting

`CachedDataTool`'s three methods are the only pointer-consuming path that puts cached rows back into the model's own
context, and even then only a capped slice at a time. `ExportCachedTableTool.ExportCachedTableToCsv`
(`Jube.AskJooby/Tools/Export/ExportCachedTableTool.cs`) and `RenderChartTool.RenderChart`
(`Jube.AskJooby/Tools/Charting/RenderChartTool.cs`) both read a cached table but never return its contents to the
model -- they build a file (a CSV, or an SVG chart) straight from the cached rows on the backend, store that file
through `IFileExportClient`, and record it with `IExportSink` so it is attached to the turn's reply for the person
to see directly. The model is told only that the export or chart succeeded, how many rows/columns it covered, and
that it should describe what it shows in words rather than try to reproduce the data -- it never sees the file's
bytes and cannot inspect them. `ExportCachedTableToCsv` additionally refuses anything over 10MB of CSV
(`MaxExportBytes`) with a instruction to narrow the table with `QueryCachedData` first.

`RenderChart`'s own mechanic -- chart types, how `categoryColumn`/`valueColumns` map onto a pointer's columns, and
exactly how the resulting SVG reaches the person's screen -- is documented in full in `docs/AskJooby/RenderChart.md`,
since it is substantial enough, and specific enough to charting, to deserve its own page; this page stops at "it
reads a pointer and never hands rows back."

## Pointers persist across turns, within their TTL

A `data_pointer` quoted in one of AskJooby's own replies is not scoped to the single tool call that produced it --
it stays valid, and resolvable by `GetAsync`, for the rest of its TTL (an hour by default), which in practice usually
outlives the single turn that minted it. `Jube.AskJooby/Prompts/DataPointerPreference.cs`, part of every agent's
system instructions, says this explicitly: when a caller's message refers back to "the table", "that chart", or
"export it", the model is told to look for a `data_pointer` already quoted in one of its own earlier replies in the
same conversation before concluding no data exists or re-deriving it from scratch, and to reuse it directly with
`QueryCachedData`, `ExportCachedTableToCsv` or `RenderChart`. A pointer surviving across turns inside one conversation
is the normal case, not an edge case -- it is what lets a caller ask for a table, then a chart of it, then a CSV of
it, as three separate turns without the model re-querying the data three times.

That same instruction also draws the line against the wrong tool for the job: "'Show me a chart/report of X' is a
request for a chart in this reply, not for a saved dashboard" -- a one-off chart goes through `RenderChart` against
a freshly cached or reused pointer, never through creating a `VisualisationRegistry`/`VisualisationRegistryDatasource`,
which is a heavier, persistent, separately-permissioned object reserved for when a caller explicitly asks for
something to be built, saved, or embedded rather than merely shown once.

## Cross-reference

* `docs/AskJooby/SqlQueryTools.md` -- `RunSqlQuery`'s sibling read-only SQL tools (listing, validating, explaining,
  table/query statistics), and the allow-list/AST validator that scopes what `RunSqlQuery` itself may touch.
* `docs/AskJooby/RenderChart.md` -- the chart-rendering tool and the turn-wide attachment mechanic it shares with
  `ExportCachedTableToCsv`.
* `docs/AskJooby/OrchestrationTools.md` -- `SpawnAgent`/`SpawnAgents`/`HandBack`, whose own descriptions tell the
  model to reach for `CacheAssembledTable` before passing several gathered numbers upward.
* `docs/Concepts/EnvironmentVariables/index.md` -- `AskJoobyMaxToolResultCharacters`, the general-service-tool side
  of this same mechanism (`LargeResultPointer`).
