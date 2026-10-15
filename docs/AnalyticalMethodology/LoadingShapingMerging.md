---
layout: default
title: Loading, Shaping and Merging Data
nav_order: 6
parent: Analytical Methodology
---

# Loading, Shaping and Merging Data

The original Module 4 covered horizontal abstraction with numeric and string functions, regular expressions, dates,
importing CSV and pipe-separated files, connecting to and fetching from SQL Server, then sorting, selecting, mutating,
merging and deleting columns of a data frame with `dplyr`, finishing with a CSV export. Every procedure below is
condensed, re-run for real, and followed by its
`AdvancedAnalytics` equivalent where one exists.

One point applies to the whole module and is worth stating up front: **loading data in the product does not route
through R at all.** There is no analogue anywhere in `AdvancedAnalyticsTools` for "open a file"
or "connect to a database" — a caller already holds a table (rows of doubles from the platform's own Archive/model
payload data) before any tool in `DataPreparationAndResampling` runs, and hands it in as
`double[][]`, which the service packs into `AdvancedAnalyticsCache` via `AdvancedAnalyticsPacking.Pack`
(`Rows`/`Columns` shape, IEEE 754 binary64, little endian, row major — see
`docs/AnalyticalMethodology/Reference/FoundationData.md`). Every tool cited below operates on that already-packed array, never on a
CSV, a pipe-delimited file or a live database connection.

### Procedure 1: Using Numeric Functions to Create a Horizontal Abstraction

R's base numeric operators (`+ - * / %% ^`) and functions such as `round()` and `sqrt()` can be nested to build a
derived column from a raw one:

```R
Double <- c(1.22341,5.889988,6.9999890)
Double

DoubleRound <- round(Double,2)
DoubleRound

DoubleRoundSqrt <- sqrt(DoubleRound)
DoubleRoundSqrt

DoubleNested <- sqrt(round(Double,2))
DoubleNested
```

Console output:

```
[1] 1.223410 5.889988 6.999989
[1] 1.22 5.89 7.00
[1] 1.104536 2.426932 2.645751
[1] 1.104536 2.426932 2.645751
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`
/ tool `"ApplyColumnFunction"` covers the `sqrt()` step — it applies one fixed unary function (natural log, log10, log2,
log1p, signed log1p, square root, signed square root, cube root, reciprocal, square or absolute value) to every cell of
a table, with an optional additive offset before the function runs. Rounding to a fixed number of decimal places has no
tool equivalent: values stay full double precision throughout the packed array, and there is no "round to N decimals"
step anywhere in the tool layer — rounding is presentation, not a shaping operation the product performs.

### Procedure 2: Extracting a Substring, Testing Logically and Presenting for Machine Learning

`substr()` pulls a fixed-position slice out of a character vector; comparing it and converting the logical result with
`as.numeric()` turns a text pattern into a model-ready flag:

```R
Names <- c("Richard","Robert","Reinhard","Raymond","Richardino","Richardo")
Names

NamesSubstr <- substr(Names,1,7)
NamesSubstr

NamesSubstrLogical <- NamesSubstr == "Richard"
NamesSubstrLogical

NamesSubstrLogicalNumeric <- as.numeric(NamesSubstrLogical)
NamesSubstrLogicalNumeric

NamesSubstrLogicalNumericNested <- as.numeric(substr(Names,1,7) == "Richard")
NamesSubstrLogicalNumericNested
```

Console output:

```
[1] "Richard"    "Robert"     "Reinhard"   "Raymond"    "Richardino"
[6] "Richardo"  
[1] "Richard" "Robert"  "Reinhar" "Raymond" "Richard" "Richard"
[1]  TRUE FALSE FALSE FALSE  TRUE  TRUE
[1] 1 0 0 0 1 1
[1] 1 0 0 0 1 1
```

**Tool equivalent:** none — R/script-runner only. `AdvancedAnalyticsCache` holds nothing but packed
doubles; no tool in the semantic layer accepts or inspects raw strings, so there is no genuine home for "slice a
substring and test it." A flag like this has to already be a number by the time it reaches
`AdvancedAnalyticsTools` — computed upstream, outside this service, before the table is ever packed.

### Procedure 3: Searching with Regular Expressions

`grepl()` replaces the fixed-position `substr()` test with a pattern match, here anchored to the start of the string
with `^`:

```R
Names <- c("Richard","Robert","Reinhard","Raymond","Richardino","Richardo")

NamesGrepl <- grepl("^Richard",Names)
NamesGrepl

NamesGreplNumeric <- as.numeric(NamesGrepl)
NamesGreplNumeric

NamesGreplNumericNested <- as.numeric(grepl("^Richard",Names))
NamesGreplNumericNested
```

Console output:

```
[1]  TRUE FALSE FALSE FALSE  TRUE  TRUE
[1] 1 0 0 0 1 1
[1] 1 0 0 0 1 1
```

**Tool equivalent:** none — R/script-runner only, for the same reason as Procedure 2:
regular-expression matching is string work, and the packed array the tool layer operates on is numeric-only.

### Procedure 4: Create a Date with a Specific Date and Time Format

A character vector of dates is converted to R's native `Date` type with `as.Date()`, given a format string that names
where the day, month and year tokens fall:

```R
DatesString <- c("22/02/1732","30/10/1735","13/04/1743","16/03/1751")
DatesString
class(DatesString)

Dates <- as.Date(DatesString,format="%d/%m/%Y")
Dates
class(Dates)
```

Console output:

```
[1] "22/02/1732" "30/10/1735" "13/04/1743" "16/03/1751"
[1] "character"
[1] "1732-02-22" "1735-10-30" "1743-04-13" "1751-03-16"
[1] "Date"
```

**Tool equivalent:** none directly — the product has no `Date` object at all. Dates reach
`AdvancedAnalyticsTools` already reduced to a number: either an ISO 8601 string or a Unix epoch value.
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.FeatureEngineeringTools` / tools
`"ExtractDateTimeParts"` (ISO 8601 strings) and `"ExtractEpochParts"` (epoch seconds or milliseconds) are the moment a
date-like input actually becomes usable numeric feature columns — the nearest thing the tool layer has to "create a
date," except that the output is year/month/day/etc. parts rather than a `Date`
type, because there is nothing downstream that would consume a `Date` type.

### Procedure 5: Perform Date Arithmetic

Once a vector is a `Date`, ordinary `+`/`-` shifts every element by that many days:

```R
DatesString <- c("22/02/1732","30/10/1735","13/04/1743","16/03/1751")
Dates <- as.Date(DatesString,format="%d/%m/%Y")

DatesPlusOne <- Dates + 1
DatesPlusOne
```

Console output:

```
[1] "1732-02-23" "1735-10-31" "1743-04-14" "1751-03-17"
```

**Tool equivalent:** `FeatureEngineeringTools` / tool `"ComputeElapsedTime"` is the realistic equivalent
for date arithmetic in the product — it takes two parallel lists of ISO 8601 date-time strings and returns `(to - from)`
in seconds, minutes, hours or days per row, which is how the product expresses account age or time-since-last-event. It
answers "how far apart are these two moments," not "shift this moment by a constant" — the toy `Dates + 1` example
itself (offsetting a date by a fixed amount rather than measuring an elapsed distance) has no direct tool.

### Procedure 6: Extract Reporting Periods from a Date

`weekdays()`, `months()` and `quarters()` each pull one calendar field out of a `Date` vector:

```R
DatesString <- c("22/02/1732","30/10/1735","13/04/1743","16/03/1751")
Dates <- as.Date(DatesString,format="%d/%m/%Y")

ReportingQuarters <- quarters(Dates)
ReportingQuarters

ReportingMonths <- months(Dates)
ReportingMonths

ReportingWeekdays <- weekdays(Dates)
ReportingWeekdays
```

Console output:

```
[1] "Q1" "Q4" "Q2" "Q1"
[1] "February" "October"  "April"    "March"   
[1] "Friday"   "Sunday"   "Saturday" "Tuesday"
```

**Tool equivalent:** `FeatureEngineeringTools` / tools `"ExtractDateTimeParts"` and
`"ExtractEpochParts"` again — both return `quarter`, `month` and `dayOfWeek` (Monday 1 to Sunday 7, numeric rather than
a name) among their fourteen parts (year, quarter, month, day, dayOfWeek, dayOfYear, isoWeek, hour, minute, second,
secondsOfDay, isWeekend, isMonthStart, isMonthEnd), selectable individually through the `parts` argument. This is a
direct, better-than-like-for-like match: one call returns every reporting period R needed three separate functions for,
already as numbers instead of text a model cannot use.

### Procedure 7: Importing a CSV File with R Studio

RStudio's Import Dataset wizard is, under the hood, a code generator around `readr::read_csv()`. Reading the equity
price file directly (the wizard step itself is a GUI convenience, not separate mechanics):

```R
library(readr)

AAPL <- read_csv("/work/AAPL.csv")
AAPL
str(AAPL)
```

Console output:

```
Rows: 6 Columns: 3
── Column specification ────────────────────────────────────────────────────────
Delimiter: ","
chr  (1): Symbol
dbl  (1): Interim_Close
date (1): Interim_Buffer_Date

# A tibble: 6 × 3
  Symbol Interim_Buffer_Date Interim_Close
  <chr>  <date>                      <dbl>
1 AAPL   2024-01-02                   186.
2 AAPL   2024-01-03                   184.
3 AAPL   2024-01-04                   182.
4 AAPL   2024-01-05                   181.
5 AAPL   2024-01-08                   186.
6 AAPL   2024-01-09                   185.
spc_tbl_ [6 × 3] (S3: spec_tbl_df/tbl_df/tbl/data.frame)
 $ Symbol             : chr [1:6] "AAPL" "AAPL" "AAPL" "AAPL" ...
 $ Interim_Buffer_Date: Date[1:6], format: "2024-01-02" "2024-01-03" ...
 $ Interim_Close      : num [1:6] 186 184 182 181 186 ...
```

(Package installed beyond the shared set: `readr` 2.x, with its dependencies `tzdb`, `progress` and
`vroom`, into the shared library — not part of the base image.)

**Tool equivalent:** none — as stated at the top of this module, there is no file-import step in the
product's analytics path at all. A table reaches `AdvancedAnalyticsTools` as a `double[][]`
argument the caller already assembled from the platform's own data (Archive/model payload records), never by opening a
CSV.

### Procedure 8: Importing a Pipe-Separated File

`readr::read_delim()` takes an explicit delimiter argument, so the same data with `|` instead of `,` reads identically
apart from that one argument:

```R
library(readr)

AAPL <- read_delim("/work/AAPL.txt","|")
AAPL
str(AAPL)
```

Console output:

```
Rows: 6 Columns: 3
── Column specification ────────────────────────────────────────────────────────
Delimiter: "|"
chr  (1): Symbol
dbl  (1): Interim_Close
date (1): Interim_Buffer_Date

# A tibble: 6 × 3
  Symbol Interim_Buffer_Date Interim_Close
  <chr>  <date>                      <dbl>
1 AAPL   2024-01-02                   186.
2 AAPL   2024-01-03                   184.
3 AAPL   2024-01-04                   182.
4 AAPL   2024-01-05                   181.
5 AAPL   2024-01-08                   186.
6 AAPL   2024-01-09                   185.
spc_tbl_ [6 × 3] (S3: spec_tbl_df/tbl_df/tbl/data.frame)
 $ Symbol             : chr [1:6] "AAPL" "AAPL" "AAPL" "AAPL" ...
 $ Interim_Buffer_Date: Date[1:6], format: "2024-01-02" "2024-01-03" ...
 $ Interim_Close      : num [1:6] 186 184 182 181 186 ...
```

**Tool equivalent:** none, for the same reason as Procedure 7 — the delimiter is irrelevant because there
is no file-import path in the product to begin with.

### Procedure 9: Connect to an SQL Server Database

> **Not executed against a live database.** This procedure needs an external SQL Server instance
> (`(local)\SQLEXPRESS`, database `Training`) that the validation container does not have and cannot be
> given. The script below is kept as reference, exactly as the original guide wrote it, but its console
> output was never captured because it was never run here.

RODBC opens and holds a connection object for later queries:

```R
library(RODBC)

Connection <- odbcDriverConnect("driver={SQL Server};server=(local)\\SQLEXPRESS;database=Training;username=sa;password=Training12345")
```

**Tool equivalent:** none. The product does not open ad hoc database connections from the analytics tool
layer either — data already lives in the platform's own Postgres tables (Archive/model payload data) and is assembled
into a table by the caller before it ever reaches `AdvancedAnalyticsTools`. There is no tool anywhere in
`DataPreparationAndResampling` (or elsewhere in the semantic layer) that takes a connection string.

### Procedure 10: Fetch an Entire Table from an SQL Server Database

> **Not executed against a live database**, for the same reason as Procedure 9 — kept as reference only.

```R
AAPL <- sqlQuery(Connection,"select * from AAPL")
View(AAPL)
```

**Tool equivalent:** none, for the same reason as Procedure 9.

### Procedure 11: Sorting a Data Frame with the arrange () Function

`dplyr::arrange()` reorders a data frame's rows by one or more columns, ascending by default, `desc()` for descending,
later columns breaking ties in earlier ones:

```R
library(readr)
library(dplyr)

AAPL <- read_csv("/work/AAPL.csv")

AAPL <- arrange(AAPL,Interim_Buffer_Date)
AAPL

AAPL <- arrange(AAPL,desc(Interim_Buffer_Date))
AAPL

AAPL <- arrange(AAPL,desc(Interim_Buffer_Date),Interim_Close)
AAPL
```

Console output:

```
# A tibble: 6 × 3
  Symbol Interim_Buffer_Date Interim_Close
  <chr>  <date>                      <dbl>
1 AAPL   2024-01-02                   186.
2 AAPL   2024-01-03                   184.
3 AAPL   2024-01-04                   182.
4 AAPL   2024-01-05                   181.
5 AAPL   2024-01-08                   186.
6 AAPL   2024-01-09                   185.
# A tibble: 6 × 3
  Symbol Interim_Buffer_Date Interim_Close
  <chr>  <date>                      <dbl>
1 AAPL   2024-01-09                   185.
2 AAPL   2024-01-08                   186.
3 AAPL   2024-01-05                   181.
4 AAPL   2024-01-04                   182.
5 AAPL   2024-01-03                   184.
6 AAPL   2024-01-02                   186.
# A tibble: 6 × 3
  Symbol Interim_Buffer_Date Interim_Close
  <chr>  <date>                      <dbl>
1 AAPL   2024-01-09                   185.
2 AAPL   2024-01-08                   186.
3 AAPL   2024-01-05                   181.
4 AAPL   2024-01-04                   182.
5 AAPL   2024-01-03                   184.
6 AAPL   2024-01-02                   186.
```

**Tool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation.CombinatoricsAndUtilityTools`
/ tools `"SortValuesWithOriginalIndices"` and `"SortValuesByKeys"` are the closest genuine primitives, but neither is a
native multi-column table sort the way `arrange()` is. `SortValuesByKeys` reorders one array of values to follow the
ascending sort order of a parallel key array (for example ordering amounts by their timestamps);
`SortValuesWithOriginalIndices` stably sorts one array and hands back the original row positions so the same permutation
can be applied to every other column by hand. Multi-column, mixed-direction sorting such as the third `arrange()` call
above has no single-call equivalent — it would be composed from this primitive one key at a time.

### Procedure 12: Specifying Columns of a Data Frame to Return

`dplyr::select()` keeps only the named columns, in the order given:

```R
library(readr)
library(dplyr)

AAPL <- read_csv("/work/AAPL.csv")

AAPL <- select(AAPL,Symbol,Interim_Buffer_Date,Interim_Close)
AAPL
```

Console output:

```
# A tibble: 6 × 3
  Symbol Interim_Buffer_Date Interim_Close
  <chr>  <date>                      <dbl>
1 AAPL   2024-01-02                   186.
2 AAPL   2024-01-03                   184.
3 AAPL   2024-01-04                   182.
4 AAPL   2024-01-05                   181.
5 AAPL   2024-01-08                   186.
6 AAPL   2024-01-09                   185.
```

**Tool equivalent:** `FeatureEngineeringTools` / tool `"SelectTableColumns"` — returns a table containing
only the named columns, in the order requested; an unknown name is rejected with the list of valid names. A direct,
exact match for what `select()` does here.

### Procedure 13: Adding Vectors to an Existing Data Frame

`dplyr::mutate()` adds a computed vector as a new column. Here, a 50% price-uplift target is built from
`Interim_Close`:

```R
library(readr)
library(dplyr)

AAPL <- read_csv("/work/AAPL.csv")

Target = AAPL$Interim_Close + (AAPL$Interim_Close / 2)
Target

AAPL <- mutate(AAPL,Target)
AAPL
```

Console output:

```
[1] 278.460 276.375 272.865 271.770 278.340 277.710
# A tibble: 6 × 4
  Symbol Interim_Buffer_Date Interim_Close Target
  <chr>  <date>                      <dbl>  <dbl>
1 AAPL   2024-01-02                   186.   278.
2 AAPL   2024-01-03                   184.   276.
3 AAPL   2024-01-04                   182.   273.
4 AAPL   2024-01-05                   181.   272.
5 AAPL   2024-01-08                   186.   278.
6 AAPL   2024-01-09                   185.   278.
```

**Tool equivalent:** there is no single generic `mutate()` — every derived column in
`FeatureEngineeringTools` is its own explicit, typed tool rather than an arbitrary formula. The nearest family for this
specific pattern (arithmetic between two numbers) is tool `"GenerateColumnPairFeatures"`, which builds one new column
per named pair of existing columns combined by product, ratio, sum, difference, absolute difference, minimum or maximum;
tool `"ApplyColumnFunction"` (Procedure 1) covers a fixed nonlinear function of a single column;
`"GeneratePolynomialFeatures"` covers powers and interaction terms. None of the three takes an arbitrary scalar
multiplier the way `Interim_Close / 2` does here — a business constant like that has no dedicated tool and is expected
to already be baked into the column before it reaches the tool layer.

### Procedure 14: Merging a Data Frame

`dplyr::inner_join()` brings two data frames together on a shared key, duplicating the right-hand row across every
left-hand match:

```R
library(readr)
library(dplyr)

AAPL <- read_csv("/work/AAPL.csv")
Descriptions <- read_csv("/work/Descriptions.csv")
Descriptions

AAPL <- inner_join(AAPL,Descriptions,by = "Symbol")
AAPL
```

Console output:

```
# A tibble: 1 × 2
  Symbol Description           
  <chr>  <chr>                 
1 AAPL   Apple Inc Common Stock
# A tibble: 6 × 4
  Symbol Interim_Buffer_Date Interim_Close Description           
  <chr>  <date>                      <dbl> <chr>                 
1 AAPL   2024-01-02                   186. Apple Inc Common Stock
2 AAPL   2024-01-03                   184. Apple Inc Common Stock
3 AAPL   2024-01-04                   182. Apple Inc Common Stock
4 AAPL   2024-01-05                   181. Apple Inc Common Stock
5 AAPL   2024-01-08                   186. Apple Inc Common Stock
6 AAPL   2024-01-09                   185. Apple Inc Common Stock
```

(The original guide's script passed `ID = "Symbol"`, which is not a valid argument of `inner_join()`; the correct
argument is `by`, used above. It also names the source table `EOD_Desccriptions`, a typo for
`EOD_Descriptions`, carried over unchanged since it is only prose here.)

**Tool equivalent:** none — R/script-runner only. No tool in `DataPreparationAndResampling`, or anywhere
else in the semantic layer, joins two tables on a key column. A table reaching
`AdvancedAnalyticsTools` is expected to already be one assembled, aligned set of rows; combining separate sources on a
shared key happens upstream of the tool layer, not inside it.

### Procedure 15: Delete a Vector from a Data Frame

Assigning `NULL` to a column removes it from a data frame:

```R
library(readr)
library(dplyr)

AAPL <- read_csv("/work/AAPL.csv")
Target = AAPL$Interim_Close + (AAPL$Interim_Close / 2)
AAPL <- mutate(AAPL,Target)
AAPL

AAPL$Target <- NULL
AAPL
```

Console output:

```
# A tibble: 6 × 4
  Symbol Interim_Buffer_Date Interim_Close Target
  <chr>  <date>                      <dbl>  <dbl>
1 AAPL   2024-01-02                   186.   278.
2 AAPL   2024-01-03                   184.   276.
3 AAPL   2024-01-04                   182.   273.
4 AAPL   2024-01-05                   181.   272.
5 AAPL   2024-01-08                   186.   278.
6 AAPL   2024-01-09                   185.   278.
# A tibble: 6 × 3
  Symbol Interim_Buffer_Date Interim_Close
  <chr>  <date>                      <dbl>
1 AAPL   2024-01-02                   186.
2 AAPL   2024-01-03                   184.
3 AAPL   2024-01-04                   182.
4 AAPL   2024-01-05                   181.
5 AAPL   2024-01-08                   186.
6 AAPL   2024-01-09                   185.
```

**Tool equivalent:** `FeatureEngineeringTools` / tool `"SelectTableColumns"` again — there is no dedicated
"drop this column" tool, but naming every column except the one to discard in the `keep`
argument is exactly equivalent to `AAPL$Target <- NULL`. Where a column is dropped because it is mostly missing rather
than by deliberate choice, `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.MissingValueTools`
/ tool `"DropSparseRowsAndColumns"` is the data-quality-driven equivalent instead.

### Procedure 16: Exporting a CSV File

`write.csv()` writes a data frame back out to the file system; `getwd()` reports where:

```R
library(readr)

AAPL <- read_csv("/work/AAPL.csv")

write.csv(AAPL,file="/work/out/AAPL.csv")

getwd()

list.files("/work/out")
readLines("/work/out/AAPL.csv")
```

Console output:

```
[1] "/"
[1] "AAPL.csv"
[1] "\"\",\"Symbol\",\"Interim_Buffer_Date\",\"Interim_Close\""
[2] "\"1\",\"AAPL\",2024-01-02,185.64"                         
[3] "\"2\",\"AAPL\",2024-01-03,184.25"                         
[4] "\"3\",\"AAPL\",2024-01-04,181.91"                         
[5] "\"4\",\"AAPL\",2024-01-05,181.18"                         
[6] "\"5\",\"AAPL\",2024-01-08,185.56"                         
[7] "\"6\",\"AAPL\",2024-01-09,185.14"
```

(`getwd()` reports `/`, the validation container's working directory rather than an RStudio project folder — the
original guide's screenshot showed a Windows desktop path; the mechanism demonstrated is identical.)

**Tool equivalent:** none — R/script-runner only, and deliberately the mirror image of Procedures 7 and 8:
the product never writes a shaped table back out to a CSV file either. Results leave
`AdvancedAnalyticsTools` as data returned to the caller (a `FeatureTableResult`, a trained-model record, a metric),
which is then persisted through `AdvancedAnalyticsTrainedModel`/`AdvancedAnalyticsTrainingRun` and its child tables in
Postgres, or serialised as JSON — never as a file on disk.
