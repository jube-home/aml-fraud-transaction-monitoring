---
layout: default
title: Data Structures
nav_order: 5
parent: Analytical Methodology
---

# Data Structures

The original guide's Module 3 opened R's basic data structures: vectors, matrices, factors, lists and data frames. Every
procedure is condensed below, run for real, and followed by its `AdvancedAnalytics`
equivalent where one exists.

The product's whole numeric-data story is one shape: a packed `double[]` with a `Rows`/`Columns` count, described in
`docs/AnalyticalMethodology/Reference/FoundationData.md`. `AdvancedAnalyticsPacking.Pack(double[] values)`
(`Jube.Data.Repository`) turns a flat, row-major array of IEEE 754 binary64 values into the bytes stored in
`AdvancedAnalyticsCache.Data` (`Jube.Data.Poco`), alongside its `Rows` and `Columns`; `Unpack(byte[]
bytes, int packingVersion)` reverses it. There is no vector/matrix/factor/list type distinction at that layer —
everything is one homogeneous numeric array with a shape, and the FoundationData contract is exact: "for packing version
1, `Rows * Columns * 8` must equal the data length." Most of this module is about what that means for each R structure
in turn.

### Procedure 1: Create a Vector with the c Function

`c()` combines values into a vector. A numeric vector:

```R
NumericVector <- c(1,2,3,4,5)
NumericVector
```

Console output:

```
[1] 1 2 3 4 5
```

R vectors cannot mix types: adding one character value coerces the whole vector to character.

```R
Mixed <- c(1,2,3,"four",5)
Mixed
```

Console output:

```
[1] "1"    "2"    "3"    "four" "5"   
```

**AdvancedAnalytics equivalent:** `Jube.Data.Repository.AdvancedAnalyticsPacking` packs only `double[]` — the
type-coercion problem in the second snippet cannot arise, because the packed array is always numeric. A categorical
value such as `"four"` never enters the packed array directly; it is coded to a number first (see Procedure 14).

### Procedure 2: Perform Vector Arithmetic

R vectorises `+ - * / ^ %%` over a vector, against a scalar or element-wise against another vector of the same length:

```R
Multiply <- c(1,2,3,4,5)
Multiply
Multiply * 2
MultiplyBy <- c(5,4,3,2,1)
Multiply * MultiplyBy
```

Console output:

```
[1] 1 2 3 4 5
[1]  2  4  6  8 10
[1] 5 8 9 8 5
```

**Tool equivalent:** none as a general-purpose operator. Once a caller has
`AdvancedAnalyticsPacking.Unpack`ed a cache entry back to `double[]`, arithmetic against a scalar or a second array is
ordinary C# array/LINQ code, not something routed through a tool. The nearest tool-layer citizen is
`ApplyColumnFunction` (`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`), but it
only applies one fixed unary function (log, square root, reciprocal, square, and similar) to every cell with an optional
additive offset — it does not multiply two tables together or by an arbitrary scalar the way `Multiply * 2` or
`Multiply * MultiplyBy` does.

### Procedure 3: Create a Vector via a Sequence

`:` and `seq()` both build arithmetic sequences; `seq()` additionally takes a step:

```R
SequenceBasic <- 1:10
SequenceBasic
SequenceFunction <- seq(1,10)
SequenceFunction
SequenceStep <- seq(1,10,0.25)
SequenceStep
```

Console output:

```
 [1]  1  2  3  4  5  6  7  8  9 10
 [1]  1  2  3  4  5  6  7  8  9 10
 [1]  1.00  1.25  1.50  1.75  2.00  2.25  2.50  2.75  3.00  3.25  3.50  3.75
[13]  4.00  4.25  4.50  4.75  5.00  5.25  5.50  5.75  6.00  6.25  6.50  6.75
[25]  7.00  7.25  7.50  7.75  8.00  8.25  8.50  8.75  9.00  9.25  9.50  9.75
[37] 10.00
```

(The original guide's script for the step case used `<=` rather than `<-`; that is corrected above, since
`<=` there would just compare `SequenceStep` against the `seq()` result rather than assign it.)

**Tool equivalent:** none — R/script-runner only. Tables reaching the tool layer are always supplied by the
caller from real data; there is no tool that manufactures a synthetic arithmetic sequence to seed one.

### Procedure 4: Create a Vector via Repetition

`rep(value, times)` repeats a value without spelling it out in `c()`:

```R
RepFunction <- rep(1,10)
RepFunction
```

Console output:

```
 [1] 1 1 1 1 1 1 1 1 1 1
```

**Tool equivalent:** none — R/script-runner only. The product has no tool that generates a repeated
constant column; where a real product column happens to be constant, `SelectByVarianceThreshold`
(`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.FeatureSelectionTools`) detects and drops it as
uninformative, which is the opposite job.

### Procedure 5: Selecting and Filtering from a Numeric Vector

Square brackets subscript a vector: by position, by negative position (everything except), or by a logical condition,
optionally combined with `&`/`|`:

```R
SequenceBasic[3]
SequenceBasic[-3]
SequenceBasic[SequenceBasic > 5]
SequenceBasic[SequenceBasic > 2 & SequenceBasic < 5]
```

Console output:

```
[1] 3
[1]  1  2  4  5  6  7  8  9 10
[1]  6  7  8  9 10
[1] 3 4
```

**Tool equivalent:** none dedicated. Once a cache entry has been `Unpack`ed to `double[]`, position,
negative-position and logical-mask selection are plain C# array indexing and LINQ `Where` over that array in caller
code — not exposed as an `AdvancedAnalyticsTool`. `DropSparseRowsAndColumns`
(same `DataPreparationAndResampling` namespace) is the closest tool-layer neighbour, but it filters rows and columns by
missingness, not by an arbitrary caller-supplied condition.

### Procedure 6: Setting Vector Labels / Names

`names()` attaches a label to each vector entry, similar to spreadsheet column headers:

```R
Ages <- c(22,23,28)
Ages
names(Ages) <- c("Tom","Harry","Dick")
Ages
```

Console output:

```
[1] 22 23 28
  Tom Harry  Dick 
   22    23    28 
```

**Tool equivalent:** the packed `double[]` carries no per-entry (per-row) name at all — only per-column
names, threaded through the tool layer as an explicit `string[] columnNames` parameter and defaulted by
`DataPreparationGuard.Names` (`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling`)
to `column_0, column_1, ...` when omitted. Labelling one entry of a vector, as opposed to one column of a table, has no
product equivalent.

### Procedure 7: Selecting and Filtering from a Character Vector

Once named, entries are selected by label instead of position, singly or as a vector of labels:

```R
Ages["Tom"]
Ages[c("Tom","Dick")]
```

Console output:

```
Tom 
 22 
 Tom Dick 
  22   28 
```

**Tool equivalent:** `SelectTableColumns`
(`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.FeatureEngineeringTools`)
is the same idea at the column level — it returns a table containing only the named columns, in the order requested,
rejecting an unknown name with the list of valid ones. It selects columns, not the individual row-entries this
procedure's vector represents.

### Procedure 8: Combine Vectors to Make a Matrix with cbind

`cbind()` stacks equal-length vectors as columns of a matrix:

```R
Column1 <- c(1,2,3,4,5,6)
Column2 <- c(10,20,30,40,50,60)
Column3 <- c(100,200,300,400,500,600)
Matrix3Col <- cbind(Column1,Column2,Column3)
Matrix3Col
```

Console output:

```
     Column1 Column2 Column3
[1,]       1      10     100
[2,]       2      20     200
[3,]       3      30     300
[4,]       4      40     400
[5,]       5      50     500
[6,]       6      60     600
```

**AdvancedAnalytics equivalent:** `AdvancedAnalyticsCache` (`Jube.Data.Poco`) is exactly this end state — one packed
`double[]` with a `Rows`/`Columns` shape, row-major, built by
`AdvancedAnalyticsPacking.Pack`. The product does not build it interactively column by column the way
`cbind()` does; the caller assembles the full row-major array and its shape up front, then packs and caches it in one
call.

### Procedure 9: Viewing a Matrix

RStudio's environment pane can expand a matrix into a grid, or the same view can be invoked from script:

```R
str(Matrix3Col)
```

Console output:

```
 num [1:6, 1:3] 1 2 3 4 5 6 10 20 30 40 ...
 - attr(*, "dimnames")=List of 2
  ..$ : NULL
  ..$ : chr [1:3] "Column1" "Column2" "Column3"
```

(`str()` is used here in place of the original's `View()`, which opens an interactive grid window and has no console
output to capture; `str()` shows the same shape and dimension names non-interactively.)

**AdvancedAnalytics equivalent:** `AdvancedAnalyticsCacheRepository.GetMetadata` / `Get`
(`Jube.Data.Repository`) return `Rows`, `Columns` and `Data` for a cache entry. There is no interactive grid viewer —
inspecting a table's shape is a repository read, and inspecting its values means unpacking the bytes.

### Procedure 10: Combine Vectors to Make a Matrix with rbind

`rbind()` stacks equal-length vectors as rows instead of columns:

```R
Row1 <- c(1,2,3,4,5,6,7,8,9,10)
Row2 <- c(10,20,30,40,50,60,70,80,90,100)
Row3 <- c(100,200,300,400,500,600,700,800,900,1000)
Matrix3Row <- rbind(Row1,Row2,Row3)
Matrix3Row
```

Console output:

```
     [,1] [,2] [,3] [,4] [,5] [,6] [,7] [,8] [,9] [,10]
Row1    1    2    3    4    5    6    7    8    9    10
Row2   10   20   30   40   50   60   70   80   90   100
Row3  100  200  300  400  500  600  700  800  900  1000
```

(The original guide's script called `c()` here, not `rbind()`, despite describing row-binding immediately above it —
`c()` on three ten-element vectors would just concatenate them into one thirty-element vector.
`rbind()` is the correct call for what the procedure describes and is what was actually run.)

**AdvancedAnalytics equivalent:** the same `AdvancedAnalyticsCache` row-major packing as Procedure 8. Row major is
precisely `rbind()`'s orientation — rows are laid out one after another in the flat array — so a caller building the
packed array is already assembling data "by row" the way `rbind()` does, rather than
"by column" the way `cbind()` does; both end up in the same `Data`/`Rows`/`Columns` structure.

### Procedure 11: Create a Matrix of Defined Size with a Vector

`matrix(vector, nrow, ncol)` fills a matrix column-wise from one vector, recycling it if it runs out:

```R
LongVector <- c(1,2,3,4,5,6)
OverspillMatrix <- matrix(LongVector,nrow = 3,ncol = 4)
OverspillMatrix
```

Console output:

```
     [,1] [,2] [,3] [,4]
[1,]    1    4    1    4
[2,]    2    5    2    5
[3,]    3    6    3    6
```

**AdvancedAnalytics equivalent:** none for the recycling behaviour. `AdvancedAnalyticsCache`'s contract is exact, not
permissive: per `docs/AnalyticalMethodology/Reference/FoundationData.md`, "for packing version 1, `Rows *
Columns * 8` must equal the data length" — a caller cannot hand over a six-value array for a 3x4 (twelve value) shape
and have the product silently repeat it to fill the gap, the way `matrix()` does here.

### Procedure 12: Labelling a Matrix

`colnames()` and `rownames()` label a matrix's columns and rows:

```R
colnames(OverspillMatrix) <- c("Example1","Example2","Example3","Example4")
rownames(OverspillMatrix) <- c("Row1","Row2","Row3")
OverspillMatrix
```

Console output:

```
     Example1 Example2 Example3 Example4
Row1        1        4        1        4
Row2        2        5        2        5
Row3        3        6        3        6
```

**AdvancedAnalytics equivalent:** column labelling maps to the `string[] columnNames` parameter carried through
`DataPreparationAndResampling` tools (`DataPreparationGuard.Names`, as in Procedure 6). Row labelling has no equivalent:
a row in a packed table is just a case at a given index, with no name attached to it anywhere in
`AdvancedAnalyticsCache`.

### Procedure 13: Selecting from a Matrix

A matrix takes two subscripts, row then column:

```R
OverspillMatrix[2,3]
```

Console output:

```
[1] 2
```

**Tool equivalent:** none for single-cell `[row, column]` indexing as a tool. Column-only selection is
`SelectTableColumns` (Procedure 7); selecting one cell out of an unpacked row-major
`double[]` is caller-side arithmetic (`data[row * Columns + column]`) against the array
`AdvancedAnalyticsPacking.Unpack` returns, not something the tool layer exposes.

### Procedure 14: Creating a Factor from a Vector

`factor()` turns a character vector into a categorical structure, inferring its distinct levels:

```R
Gender <- c("Male","Female","Female","Male")
Gender
GenderFactor <- factor(Gender)
GenderFactor
```

Console output:

```
[1] "Male"   "Female" "Female" "Male"  
[1] Male   Female Female Male  
Levels: Female Male
```

(The original guide's script for the second line lower-cased the variable, `factor(gender)`, which would not resolve to
the `Gender` vector just created; the correctly-cased call is what was actually run.)

**Tool equivalent:** `FitLabelEncoder`
(`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.EncodingTools`) is the direct counterpart — it
learns an integer code for every distinct category of a text column and returns the coded table plus the category list
(the position in the list is the code), with a `CategoryOrder` choice of
`Sorted`, `FirstAppearance` or `MostFrequentFirst` standing in for how `factor()` infers its levels.
`FitOneHotEncoder` is the alternative for models (linear, distance-based) that would otherwise misread the factor's
integer codes as having magnitude.

### Procedure 15: Creating a Factor from a Vector with Levels and Ordering

Passing an explicit, ordered level vector and `ordered=TRUE` gives the factor a precedence, enabling ordinal
comparisons:

```R
Temps <- c("High","Medium","Low","Low","Medium")
TempsDistinctOrder <- c("Low","Medium","High")
TempsFactor <- factor(Temps,TempsDistinctOrder,ordered=TRUE)
TempsFactor
TempsFactor > "Low"
```

Console output:

```
[1] High   Medium Low    Low    Medium
Levels: Low < Medium < High
[1]  TRUE  TRUE FALSE FALSE  TRUE
```

**Tool equivalent:** `FitLabelEncoder` again, this time with `categories` supplied explicitly (rather than
learned) in rank order — passing `["Low","Medium","High"]` as the category list for a column produces exactly this
ordinal encoding, where a category's code is its position in the list. The resulting integer codes support ordinary
numeric comparison (`>`) in place of R's `<` on ordered factor levels.

### Procedure 16: Creating a List with a Variety of Objects

`list()` aggregates objects of different types under names, unlike a vector which must be one type:

```R
BucketList <- list(TempsExample = TempsFactor, GenderExample = GenderFactor,
                    MatrixExample = Matrix3Row, VectorExample = Ages)
BucketList
```

Console output:

```
$TempsExample
[1] High   Medium Low    Low    Medium
Levels: Low < Medium < High

$GenderExample
[1] Male   Female Female Male  
Levels: Female Male

$MatrixExample
     [,1] [,2] [,3] [,4] [,5] [,6] [,7] [,8] [,9] [,10]
Row1    1    2    3    4    5    6    7    8    9    10
Row2   10   20   30   40   50   60   70   80   90   100
Row3  100  200  300  400  500  600  700  800  900  1000

$VectorExample
  Tom Harry  Dick 
   22    23    28 
```

(The original guide's script referenced `TempsFactors`/`GenderFactors`, plural, which were never created — the singular
objects from Procedures 14 and 15 are what was actually available and are what was passed here.)

**Tool equivalent:** none in the open-ended sense — the product has no dynamic, arbitrary-type named
container. The closest analogue is a fixed C# result record returned by a tool call, such as
`LabelEncodingResult(string[] ColumnNames, string[][] Categories, double[][] Table)`
(`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.EncodingTools`) or
`FeatureTableResult(string[] ColumnNames, double[][] Table)` (`...FeatureEngineeringTools`) — several named,
differently-typed fields bundled together and accessed by property name. Unlike an R list, the set of fields and their
types are fixed at compile time for a given tool, never assembled ad hoc at runtime.

### Procedure 17: Subsetting and Referencing Objects with a Name

A list entry is retrieved with `$name`, and if that entry is itself a vector it can be subset further:

```R
BucketList$VectorExample
BucketList$VectorExample["Tom"]
```

Console output:

```
  Tom Harry  Dick 
   22    23    28 
Tom 
 22 
```

**Tool equivalent:** none beyond ordinary C# property access on a result record (see Procedure 16)
followed by ordinary array indexing on one of its `double[]`/`string[]` fields — there is no tool-layer concept of
"subset a named field of a returned bundle," it is just how the calling code reads the record.

### Procedure 18: Create a Data Frame from Vectors

A data frame combines equal-length vectors and factors — described here as "a hybrid of a matrix and a list":

```R
FullNames <- c("Donald Trump","Hilary Clinton","Gary Johnson")
FullAges <- c(70,69,50)
FullGender <- factor(c("Male","Female","Male"))
FullDataFrame <- data.frame(FullNames,FullAges,FullGender)
FullDataFrame
```

Console output:

```
       FullNames FullAges FullGender
1   Donald Trump       70       Male
2 Hilary Clinton       69     Female
3   Gary Johnson       50       Male
```

**Tool equivalent:** see `DataFramesVsAccordArrays.md` for the full comparison between an R data frame and
the product's array shape, including the dplyr-style filter/select/mutate equivalents; not duplicated here.

### Procedure 19: Create a Data Frame from Names and stringsAsFactors

`data.frame()` also accepts explicit column names and a `stringsAsFactors` switch to stop character columns
auto-converting to factors:

```R
LabeledDataFrame <- data.frame(ExampleFullNames = FullNames, ExampleFullAges = FullAges,
                                ExampleFullGender = FullGender, stringsAsFactors = FALSE)
LabeledDataFrame
str(LabeledDataFrame)
```

Console output:

```
  ExampleFullNames ExampleFullAges ExampleFullGender
1     Donald Trump              70              Male
2   Hilary Clinton              69            Female
3     Gary Johnson              50              Male
'data.frame':	3 obs. of  3 variables:
 $ ExampleFullNames : chr  "Donald Trump" "Hilary Clinton" "Gary Johnson"
 $ ExampleFullAges  : num  70 69 50
 $ ExampleFullGender: Factor w/ 2 levels "Female","Male": 2 1 2
```

(The original guide's script wrapped the call in a redundant `data.frame(data.frame(...))`; the single call above is
equivalent and is what was actually run.)

**Tool equivalent:** as Procedure 18 — see `DataFramesVsAccordArrays.md`, not duplicated here.

### Procedure 20: Saving .RData to File

`save()` persists named objects to a binary `.RData` file in the working directory, for reuse without recomputing them:

```R
save(LabeledDataFrame, BucketList, file = "/work/Example.RData")
getwd()
file.exists("/work/Example.RData")
```

Console output:

```
[1] "/"
[1] TRUE
```

(`getwd()` reports the validation container's root rather than a Windows working directory, and the file path is written
explicitly to the mounted `/work` volume rather than relying on the ambient working directory, but the mechanism
demonstrated — `save()` writing a `.RData` file that `file.exists()` then confirms — is unchanged from the original.)

**AdvancedAnalytics equivalent:** `AdvancedAnalyticsTrainedModelVersion` (`Jube.Data.Poco`, via
`AdvancedAnalyticsTrainedModelVersionRepository` in `Jube.Data.Repository`) is the product's persistence of a trained
model's state — `ModelJson` (`json`, not `jsonb`, so key order and number formatting are kept verbatim),
`VariablesJson`, `MetricsJson` (`jsonb`) and `CalibratorJson` (`json`, same verbatim reasoning as
`ModelJson`). It is `Insert`-only and immutable — there is no update or delete, and a new version number is assigned per
model — unlike `save()`, which can be called repeatedly to overwrite the same `.RData` file. The format is also
fundamentally different: JSON text held as Postgres columns, never R's binary serialisation format, and there is no
equivalent of saving an arbitrary named bag of R objects — only a model's own JSON-shaped state (model, variables,
metrics, calibrator) is ever persisted this way.

### Procedure 21: Loading .RData from File

`load()` restores objects from a `.RData` file into a fresh session:

```R
rm(list = ls())
ls()
load("/work/Example.RData")
ls()
BucketList$VectorExample
```

Console output:

```
character(0)
[1] "BucketList"       "LabeledDataFrame"
  Tom Harry  Dick 
   22    23    28 
```

(`rm(list = ls())` stands in for the original's "quit RStudio, don't save the session, reopen it" — both leave an empty
session immediately before the `load()` call, without needing an interactive restart inside the validation container.)

**AdvancedAnalytics equivalent:** reading a version back is a repository call rather than a file load —
`AdvancedAnalyticsTrainedModelVersionRepository.GetById`, `GetByModelIdAndVersionNumber` or
`GetActiveByModelId` (`Jube.Data.Repository`) return the stored `ModelJson`/`VariablesJson`/`MetricsJson`/
`CalibratorJson` for a specific version or a model's currently active one. There is no working-directory convention or
ambient file to look for — the version is addressed by id, by model id and version number, or by "whichever version is
active for this model," never by a file path.
