---
layout: default
title: Data Frames vs the Packed Array
nav_order: 7
parent: Analytical Methodology
---

# Data Frames vs the Packed Array

This page does not carry over a procedure from the original R training guide. It looks directly at the analogy every
other page in this section leans on implicitly: an R `data.frame` versus the product's own
"DataFrame," and how the product's `AdvancedAnalytics` tools do the dplyr-shaped work of shaping one.
[Data Structures](DataStructures.md) (procedures 18-19) covers constructing a `data.frame` from vectors and factors,
and [Loading, Shaping and Merging Data](LoadingShapingMerging.md) (procedures 11-15) covers
`arrange`/`select`/`mutate`/merging/deleting columns on one — this page does not repeat that walkthrough. It takes the
concepts those procedures established as read and asks what survives the trip into the product.

## 1. What R's data.frame is, and what the product's array is

An R `data.frame` is a list of named vectors (and factors) of equal length, one vector per column, each with its own
type. A column can be numeric, character, logical or a factor; the whole object carries its own column names, and R
decides at each step whether an operation still returns something type-consistent. That flexibility is exactly what
module 3's data-frame procedures and module 4's shaping procedures rely on: `select()` and `mutate()` reference columns
by name, `arrange()` reorders rows and takes every column along with it, and a factor column stays a factor until
something explicitly coerces it.

The product's equivalent, described in full in
[Numeric Data Representation](Reference/FoundationData.md), is a different structure with a
narrower job. `AdvancedAnalyticsCache` (`Jube.Data/Repository/AdvancedAnalyticsCacheRepository.cs`, POCO in
`Jube.Data/Poco/AdvancedAnalyticsCache.cs`) stores one packed numeric table per cache entry:

- `Data` — a `bytea` holding a flat `double[]`, packed by
  `Jube.Data.Repository.AdvancedAnalyticsPacking.Pack(double[] values)` and reversed by
  `AdvancedAnalyticsPacking.Unpack(byte[] bytes, int packingVersion)`. Packing version 1 is IEEE 754 binary64, little
  endian, one value every 8 bytes, row major (a row's columns are contiguous, then the next row follows).
- `Rows` and `Columns` — plain `int` columns on the same cache row, carried beside the bytes because the bytes alone
  have no shape. `AdvancedAnalyticsCacheRepository` requires `Rows * Columns * 8` to equal the data length for packing
  version 1.

That is the whole structure: a positional, unnamed, all-numeric matrix flattened to one dimension, plus two integers
telling you how to re-fold it. Compared with a `data.frame`, three things are lost and one thing is gained:

- **Lost: column names.** The array has no header row. Column 0 is just column 0. Names live elsewhere — see below.
- **Lost: mixed types.** Every cell is a `double`. A categorical column (R's factor, or a plain character vector) cannot
  go into the array as text; it has to already be encoded to a number — a label index, a one-hot column, a target-mean
  or frequency value — before packing. `Jube.Service/Agent/
  AdvancedAnalyticsTools/DataPreparationAndResampling/EncodingTools.cs` is where that encoding happens
  (`FitLabelEncoder`, `FitOneHotEncoder`, `FitTargetMeanEncoder`, `FitWeightOfEvidenceEncoder`,
  `FitFrequencyEncoder` and their `Apply*` counterparts); it runs *before* a table is packed, never after.
- **Lost: the row/column duality of a list-of-vectors.** A `data.frame` is column-oriented (each column is one vector);
  the packed array is row-major bytes with no column boundaries of its own — `Columns` is metadata, not structure.
- **Gained: a byte-exact, storable, poolable representation.** A `data.frame` lives in one R session's memory. The
  packed array is bytes in a Postgres `bytea`, addressed by a `Guid` pointer (`AdvancedAnalyticsCache.Guid`), with a
  size limit, a tenant quota and an expiry (`AdvancedAnalyticsCacheTtlSeconds`, `AdvancedAnalyticsCacheMaxEntryBytes`,
  `AdvancedAnalyticsCacheMaxTenantBytes` — see FoundationData.md). It is built to be handed a `Guid` at a time, not held
  open as a live object across a whole analysis.

**Where the names live instead.** Variable names and their order are not part of the array — they are a separate
metadata table, `AdvancedAnalyticsTrainingRunVariable` (`VariableSequence`, `Name`, `Mean`,
`StandardDeviation`, `Minimum`, `Maximum`, `ImputationValue`, `MissingCount`, `RowCount`, `Importance`), recorded once a
training run has actually used the columns, and `VariablesJson` on
`AdvancedAnalyticsTrainedModelVersion`, recorded once a model version is trained. Neither is attached to the cache entry
itself — a cache row is pure positional data, and nothing stops two different training runs disagreeing about what
column 3 of the same cache pointer means, provided each run is honest about its own
`VariableSequence`.

**Where a data.frame's shape actually shows up before packing.** The agent tool layer does not receive the packed bytes
directly. Tools under `DataPreparationAndResampling` take a `double[][]` — one `double[]` per row — plus a separate
`string[] columnNames` parameter when a name is needed at all
(`FeatureTableResult(string[] ColumnNames, double[][] Table)` is the common return shape, for example from
`SelectTableColumns`). That `double[][]` plus a parallel name array is the closest the product gets to a
`data.frame` in memory: still no factors, still no mixed types, but at least named and shaped by construction rather
than by two accompanying integers. Packing to the flat `AdvancedAnalyticsCache` array is a separate, later step — the
format a table is put in to be stored or handed to a training run
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingInputResolver.cs` unpacks a cache entry and reshapes
it into a `double[][]` of predictors plus an `int[]` of labels by walking
`values[r * columns + c]`, splitting out one designated label column as it goes) — not the format the interactive tools
reason over call to call.

## 2. Worked example: a data.frame, by hand, into the packed array

A tiny `data.frame`, run for real in the validation container, in the same shape module 3's procedures 18 and 19 build
(numeric columns plus one factor):

```R
Transactions <- data.frame(
  TransactionId = 1:6,
  Amount = c(120.50, 45.00, 980.25, 12.75, 305.60, 77.10),
  Balance = c(500, 300, 1000, 50, 900, 200),
  Channel = factor(c("Card","Online","Card","Online","Card","Online"))
)

print(Transactions)
str(Transactions)
```

Console output:

```
  TransactionId Amount Balance Channel
1             1 120.50     500    Card
2             2  45.00     300  Online
3             3 980.25    1000    Card
4             4  12.75      50  Online
5             5 305.60     900    Card
6             6  77.10     200  Online
'data.frame':	6 obs. of  4 variables:
 $ TransactionId: int  1 2 3 4 5 6
 $ Amount       : num  120.5 45 980.2 12.8 305.6 ...
 $ Balance      : num  500 300 1000 50 900 200
 $ Channel      : Factor w/ 2 levels "Card","Online": 1 2 1 2 1 2
```

`str()` already shows the problem: three numeric vectors and one factor stored as an integer code (1 = Card, 2 = Online)
behind the label. To pack this table the factor has to be turned into a real number column first — the R side of that is
exactly what `as.numeric(Channel == "Online")` did in module 4's horizontal-abstraction procedures, and it is what
`EncodingTools` does on the product side. Take the frame after that encoding (`IsOnline` replacing `Channel`, four
numeric columns: `TransactionId`, `Amount`,
`Balance`, `IsOnline`) and flatten it row by row:

```
row 1: 1, 120.50, 500,  0
row 2: 2,  45.00, 300,  1
row 3: 3, 980.25, 1000, 0
row 4: 4,  12.75,  50,  1
row 5: 5, 305.60, 900,  0
row 6: 6,  77.10, 200,  1
```

Row major means those six rows are concatenated in order, giving a 24-element `double[]`:

```
[1, 120.50, 500, 0, 2, 45.00, 300, 1, 3, 980.25, 1000, 0, 4, 12.75, 50, 1, 5, 305.60, 900, 0, 6, 77.10, 200, 1]
```

`AdvancedAnalyticsPacking.Pack(double[] values)` writes each of those 24 doubles as 8 little-endian bytes
(`BinaryPrimitives.WriteInt64LittleEndian` over `BitConverter.DoubleToInt64Bits(values[i])`), giving a 192-byte array.
That byte array becomes `AdvancedAnalyticsCache.Data`; `Rows = 6` and `Columns = 4` are stored alongside it as plain
columns on the same row, and `Rows * Columns * 8 = 192` is exactly the byte count the repository checks for packing
version 1. `Unpack(byte[] bytes, int packingVersion)` reads the same 8-byte spans back through
`BinaryPrimitives.ReadInt64LittleEndian` and
`BitConverter.Int64BitsToDouble`, returning the flat 24-element `double[]` — at that point the caller still has to know
`Rows` and `Columns` to fold it back into anything resembling the original table, and it still has no column names or
factor: those came from `TransactionId`/`Amount`/`Balance`/`IsOnline`, remembered only by whichever
`AdvancedAnalyticsTrainingRunVariable` rows or `VariablesJson` a training run wrote down for this table, never by the
cache entry itself.

## 3. Filtering side by side: dplyr vs the packed-array tools

A full verb-by-verb accounting of what's covered, what's genuinely missing, and which gaps are worth shimming directly
on the array versus better left to the SQL layer upstream of `AdvancedAnalyticsCache` is
in [DplyrGapAnalysis.md](DplyrGapAnalysis.md).

The dplyr pipeline below runs `mutate()` (encode the factor), `filter()`, `select()` and `arrange()` in the order module
4's procedures introduce them (mutate: procedure 13; select: procedure 12; arrange: procedure 11), on the same
`Transactions` frame, actually run in the validation container:

```R
library(dplyr)

Result <- Transactions %>%
  mutate(IsOnline = as.numeric(Channel == "Online")) %>%
  filter(Amount > 50) %>%
  select(TransactionId, Amount, Balance, IsOnline) %>%
  arrange(desc(Amount))

print(Result)
```

Console output:

```
  TransactionId Amount Balance IsOnline
1             3 980.25    1000        0
2             5 305.60     900        0
3             1 120.50     500        0
4             6  77.10     200        1
```

Below each dplyr verb is the real tool (or honest gap) that does the equivalent work on a packed/positional table.

### mutate () — add a derived column

`mutate(Transactions, IsOnline = as.numeric(Channel == "Online"))` adds a computed column to the existing frame in
place. There is no single "add a column to this table" tool; instead each feature-engineering tool in
`Jube.Service/Agent/AdvancedAnalyticsTools/DataPreparationAndResampling/FeatureEngineeringTools.cs`
takes a `double[][] data` table and returns a *new* `FeatureTableResult` holding only the generated column (s) — the
caller concatenates that onto the columns it wants to keep. For this specific case (turning a category into a 0/1
indicator) the real mapping is one step earlier, in `EncodingTools.cs`:
`FitOneHotEncoder`/`ApplyOneHotEncoder` (or `FitLabelEncoder` for an ordinal code) does what
`as.numeric(Channel == "Online")` did by hand. For arithmetic on existing numeric columns —
`Interim_Close + (Interim_Close / 2)` in module 4's own example — the equivalent is
`GenerateColumnPairFeatures` (product, ratio, sum, difference, absolute difference, minimum, maximum of two named
columns) or `GeneratePolynomialFeatures`; for a value computed against an earlier row rather than another column,
`GenerateLagFeatures`; for splitting a date-time string into numeric parts,
`ExtractDateTimeParts` (or `ExtractEpochParts` for a Unix timestamp). None of them mutate the table that was passed in —
every tool call is a pure function from one table to a new one, so "adding a column" is
"call the tool, then concatenate its output columns onto the ones you kept," not "mutate the frame in place."

**AdvancedAnalyticsTool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.EncodingTools` / tools `"FitOneHotEncoder"`,
`"ApplyOneHotEncoder"` for this specific factor-to-number step; `FeatureEngineeringTools` / tools
`"GenerateColumnPairFeatures"`, `"GenerateLagFeatures"`, `"ExtractDateTimeParts"`, `"ExtractEpochParts"` for general
derived-column work.

### filter () — keep matching rows

`filter(Amount > 50)` drops rows on a logical test and keeps every column of the surviving rows together. The
packed-array tools split this into two steps that dplyr does in one. A row-selecting tool inspects the table and returns
which row indices qualify — for example `DataQualityTools.FindDuplicateRows` (rows that exactly repeat an earlier row)
or the sampling tools in `SamplingTools.cs` such as `SampleStratified`
(drawn indices per stratum) and `SampleSimpleRandom` — but returns an `int[]` of row numbers, not a reduced table.
Building the actual reduced table from a plain `Amount > 50`-style test is left to the caller constructing the next
`data` argument from those indices; there is no single tool that takes a table plus a threshold and returns "rows where
column X compares to a scalar" the way `filter()` does generically. The one row-filtering tool that *does* return a
reduced table directly is a specific case:
`MissingValueTools.DropSparseRowsAndColumns`, which drops columns then rows by missing-value fraction and hands back the
smaller `double[][]` plus which original row and column indices survived, so labels and other parallel arrays can be
filtered identically.

**AdvancedAnalyticsTool equivalent:** for missingness-driven row dropping,
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.MissingValueTools`
/ tool `"DropSparseRowsAndColumns"` returns the reduced table directly. For a general logical condition on one column
(`Amount > 50`), there is no single-call equivalent — the closest building blocks are the row-index-producing tools
(`DataQualityTools` / `"FindDuplicateRows"`, `SamplingTools` /
`"SampleStratified"`, `"SampleSimpleRandom"`, `"SampleWeighted"`), and the caller applies the returned indices itself
when it builds the next tool call's table.

### select () — keep named columns

`select(Transactions, TransactionId, Amount, Balance, IsOnline)` truncates the frame to named columns in the order
given. This one is a direct, exact match: `FeatureEngineeringTools.SelectTableColumns` takes the
`double[][] data`, the `string[] columnNames` naming every existing column, and a `string[] keep` naming the columns to
retain in the desired output order, rejecting an unknown name with the list of valid ones — the same contract as
`select()`, minus the ability to invoke it inline inside a longer expression.

**AdvancedAnalyticsTool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.FeatureEngineeringTools` / tool
`"SelectTableColumns"` — exact match for keeping and reordering named columns.

### Deleting a column (`AAPL$Target <- NULL`)

Module 4's procedure 15 deletes a column by assigning `NULL` to it. The packed-array equivalent is the same tool run the
other way: call `"SelectTableColumns"` with `keep` listing every column except the one to drop. There is no separate
"delete a column" tool, because `select()`'s keep-list and "delete everything else" are the same operation from the
array's side.

**AdvancedAnalyticsTool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.FeatureEngineeringTools` / tool
`"SelectTableColumns"`, called with the surviving column names.

### arrange () — sort the whole table by a column

`arrange(Result, desc(Amount))` reorders every row of the frame by one column's values, every other column moving with
it. This is the one dplyr verb with **no packed-array equivalent** for a whole table. The numerics tool group has real
sorting tools —
`Jube.Service/Agent/AdvancedAnalyticsTools/NumericsAndOptimisation/CombinatoricsAndUtilityTools.cs` defines
`"SortValuesWithOriginalIndices"` (stable sort of one `double[]`, returning the sorted values *and* the original
position of each so the order can be mapped elsewhere) and `"SortValuesByKeys"` (reorder one
`double[]` of values by the ascending order of a parallel `double[]` of keys) — but both work on a single vector, not a
`double[][]` table. Reordering a whole multi-column table by one column's sort order is not a single tool call: the
caller would sort the key column with `"SortValuesWithOriginalIndices"` to get
`OriginalIndices`, then apply that same index order to every other column by hand, column by column, since no tool takes
a table plus a row-index permutation and returns the reordered table. Where a genuine table-wide row order matters going
into training, it is established earlier and elsewhere — `SamplingTools`
/ `"SplitTimeOrdered"` assumes rows already arrive in chronological order rather than sorting them, and the
Repository-layer query that populates a cache entry in the first place is where an `ORDER BY` would actually run, at the
SQL/query layer, before the table is ever packed. Inside the packed array itself, nothing sorts.

**AdvancedAnalyticsTool equivalent:** none for a whole table — R/script-runner only, or arrange upstream at the
SQL/query layer before packing. The closest tool-layer building blocks,
`Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation.CombinatoricsAndUtilityTools` /
`"SortValuesWithOriginalIndices"` and `"SortValuesByKeys"`, sort a single column, not a table.

### inner_join () — merge two data frames

Module 4's procedure 14 merges two data frames on a shared key column, and immediately hits a real R inconsistency: the
join runs, but `Descriptions$Symbol` is a factor with different levels to
`AAPL$Symbol`, so despite no error the join silently drops and duplicates rows around the mismatched levels — that
fragility is a strong argument for offloading the join. There is no dedicated join tool in
`DataPreparationAndResampling`; the guide's own framing already puts this at the SQL layer ("Abstraction is offloaded to
Relational Database Management platforms" — module 4, opening paragraph), and nothing in the tool layer changes that:
joins across two sources happen in the query that builds a cache entry's rows, not after packing.

**AdvancedAnalyticsTool equivalent:** none — join upstream at the SQL/query layer, same as the original guide already
recommends; not a packed-array operation.
