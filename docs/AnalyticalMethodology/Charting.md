---
layout: default
title: Charting
nav_order: 12
parent: Analytical Methodology
---

# Charting

The original R training guide introduced `ggplot2` as a step up from base R's `plot()` and `hist()`, calling it "a more
powerful package... for producing stunning charts that will be at home in any presentation," and showed the `qplot()`
shortcut function producing a scatter plot, a line chart, a bar chart and a histogram. Read this page as procedure
recognition only, not guidance: the product does not chart with R, in any form, anywhere. There is no code path in Jube
that shells out to an R process, an Rscript runner, or a plotting device to produce a chart an analyst or a case ever
sees. Every chart the product renders — on a page, in a report, or handed to an agent tool — is produced by
`Jube.Charting`, the product's own backend SVG charting library: an immutable specification turned into one
self-contained, deterministic SVG string, with no JavaScript and no external dependency beyond the base class
library. `ggplot2` and `qplot()` have zero runtime footprint in Jube: no package reference, no service call, nothing.

The four `qplot()` procedures below are kept for exactly one reason: to recognise what a piece of
`qplot(...)` code is doing if it turns up somewhere — an old script, a colleague's notebook, a textbook example — and
map it to the real `Jube.Charting` builder that does the equivalent job in the product. Nothing here is an instruction
to write or run `qplot()` code as part of using Jube. Also worth knowing if you do meet this code in the wild: `qplot()`
itself has been deprecated since `ggplot2` 3.4.0 (every run below emits that deprecation warning) — one more reason it
is a recognition target and not a recommendation, even on its own terms.

The original guide's proprietary training datasets (`BostonHousing.csv`, `FDX.csv`, `CreditRisk.csv`,
`FraudRisk.csv`) are not available in this environment. Each procedure below uses a small synthetic stand-in with the
same column names and shape the original used, generated with a fixed seed so the run is reproducible; the numbers are
illustrative, not the original training data.

### Procedure 1: Quickly creating a scatter plot with `qplot()`

The original procedure compared a dependent variable to `PerCapitaCrimeRate` in the Boston housing set with
`qplot(Boston$Dependent, Boston$PerCapitaCrimeRate)` — `qplot()` given two vectors and no `geom`
defaults to a scatter of points, the same shape as base R's `plot()`.

```R
library(ggplot2)

set.seed(101)
n <- 200
Boston <- data.frame(
  PerCapitaCrimeRate = round(rexp(n, rate = 1.4), 4),
  Dependent = round(rnorm(n, mean = 22, sd = 9), 1)
)

png(tempfile(fileext = ".png"))
print(qplot(Boston$Dependent, Boston$PerCapitaCrimeRate))
invisible(dev.off())

cat("class:", class(qplot(Boston$Dependent, Boston$PerCapitaCrimeRate))[1], "\n")
cat("nrow(Boston):", nrow(Boston), "\n")
print(head(Boston, 3))
```

Console output:

```
Warning message:
`qplot()` was deprecated in ggplot2 3.4.0. 
class: ggplot2::ggplot 
nrow(Boston): 200 
  PerCapitaCrimeRate Dependent
1             0.8442      23.9
2             2.2678      29.3
3             0.2995      25.9
```

**Tool equivalent:** `RenderChart` (`Jube.AskJooby.Tools.Charting.RenderChartTool`), `chartType: "scatter"`, against a
table already cached with `RunSqlQuery` — the ad hoc path an agent actually draws this from, no training or
promotion involved. Underneath, `RenderChart` calls `Jube.Charting.Chart.Scatter()` — the fluent `ScatterChartBuilder`
(`Jube.Charting/Charts/ScatterChartBuilder.cs`). `Chart.Scatter().Series("Boston", dependent,
perCapitaCrimeRate).Render()` plots the same two vectors as points and returns a self-contained SVG string, which is
what `RenderChart` draws and attaches to the turn; `.TrendLines()` (a fitted trend line with R squared) and
`.Diagonal()` (an identity reference line) are both unavailable from bare `qplot()` too, but are not yet exposed as
`RenderChart` parameters either — reaching them today means calling `ScatterChartBuilder` directly rather than through
an ad hoc tool.

### Procedure 2: Quickly creating a line chart with `qplot()`

The original procedure plotted the FedEx stock close price over time with
`qplot(FDX$Interim_Buffer_Date, FDX$Interim_Close, geom="line")` — the same `qplot()` call as the scatter plot, but the
`geom` parameter switches the mark from points to a line.

```R
library(ggplot2)

set.seed(202)
n <- 120
FDX <- data.frame(
  Interim_Buffer_Date = seq(as.Date("2023-01-02"), by = "day", length.out = n),
  Interim_Close = round(140 + cumsum(rnorm(n, mean = 0, sd = 1.2)), 2)
)

png(tempfile(fileext = ".png"))
print(qplot(FDX$Interim_Buffer_Date, FDX$Interim_Close, geom = "line"))
invisible(dev.off())

cat("class:", class(qplot(FDX$Interim_Buffer_Date, FDX$Interim_Close, geom = "line"))[1], "\n")
cat("nrow(FDX):", nrow(FDX), "\n")
cat("range(Interim_Close):", range(FDX$Interim_Close), "\n")
```

Console output:

```
Warning message:
`qplot()` was deprecated in ggplot2 3.4.0. 
class: ggplot2::ggplot 
nrow(FDX): 120 
range(Interim_Close): 131.55 157.37 
```

**Tool equivalent:** `RenderChart` (`Jube.AskJooby.Tools.Charting.RenderChartTool`), `chartType: "line"`, against a
table already cached with `RunSqlQuery` — the ad hoc path an agent actually draws this from. Underneath, `RenderChart`
calls `Jube.Charting.Chart.Line()` — the fluent `LineChartBuilder`
(`Jube.Charting/Charts/LineChartBuilder.cs`). For a date x-axis specifically, its
`TimeSeries(name, times, values)` helper (declared on the shared `XyBuilderBase<TSelf>`) sets the axis to
`XAxisKind.Time` and takes `DateTime` values directly, so
`Chart.Line().XAxisTitle("Date").YAxisTitle("Close").TimeSeries("FDX", dates, closes).Render()` replaces the whole
procedure and is what `RenderChart` draws and attaches to the turn; smoothing (`.Smooth()`), a crosshair
(`.Crosshair()`) and reference bands are available on the same builder and unavailable from bare `qplot()` too, but
are not yet exposed as `RenderChart` parameters either — reaching them today means calling `LineChartBuilder` directly
rather than through an ad hoc tool.

### Procedure 3: Quickly creating a bar chart with `qplot()`

The original procedure first built a frequency table of loan `Purpose` with base R's `table()`, passed it to
`barplot()`, then showed that `qplot(CreditRisk$Purpose, geom="bar")` does the aggregation itself from the raw category
vector, without a separate `table()` step.

```R
library(ggplot2)

set.seed(303)
n <- 500
purposes <- c("car (new)", "car (used)", "furniture/equipment", "radio/television",
              "domestic appliances", "repairs", "education", "business", "other")
weights <- c(0.24, 0.11, 0.13, 0.28, 0.03, 0.02, 0.08, 0.07, 0.04)
CreditRisk <- data.frame(
  Purpose = sample(purposes, n, replace = TRUE, prob = weights)
)

Purpose <- table(CreditRisk$Purpose)
print(Purpose)

png(tempfile(fileext = ".png"))
barplot(Purpose)
invisible(dev.off())

png(tempfile(fileext = ".png"))
print(qplot(CreditRisk$Purpose, geom = "bar"))
invisible(dev.off())

cat("class(Purpose):", class(Purpose), "\n")
cat("sum(Purpose):", sum(Purpose), "\n")
```

Console output:

```

           business           car (new)          car (used) domestic appliances 
                 39                 117                  50                  16 
          education furniture/equipment               other    radio/television 
                 49                  61                  15                 147 
            repairs 
                  6 
Warning message:
`qplot()` was deprecated in ggplot2 3.4.0. 
class(Purpose): table 
sum(Purpose): 500 
```

**Tool equivalent:** two steps replace the pair of R calls, and both are real tools, not just a chart. R's
`table()` step is `Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics
.QuantileRankAndFrequencyTools` tool `"FrequencyTableOfCategories"` (in
`Jube.Service/Agent/AdvancedAnalyticsTools/DescriptiveStatistics/QuantileRankAndFrequencyTools.cs`) — value counts,
proportion, percent and cumulative share per distinct label, ordered by count by default, which is a superset of what
`table()` prints. Rendering the result is
`Jube.Charting.Chart.Column()` (vertical, matching `qplot(..., geom="bar")`) or `Chart.Bar()` (the same
`BarChartBuilder` pre-set horizontal — `Jube.Charting/Charts/BarChartBuilder.cs`):
`Chart.Column().Categories(labels).Series("Purpose", counts).Render()`, with grouped, stacked, 100% stacked, value
labels and per-point colours available on the same builder if the tool's table needs more than a single series.

### Procedure 4: Quickly creating a histogram with `qplot()`

The original procedure called `qplot(Boston$PerCapitaCrimeRate)` with no second argument, which defaults to a histogram;
run against a right-skewed rate variable this produced a bin-width warning and a chart with excessive bars, resolved by
passing `binwidth=10` to widen the bins.

```R
library(ggplot2)

set.seed(404)
n <- 200
Boston <- data.frame(
  PerCapitaCrimeRate = round(rexp(n, rate = 1.4), 4)
)

cat("--- qplot(Boston$PerCapitaCrimeRate) ---\n")
png(tempfile(fileext = ".png"))
p1 <- qplot(Boston$PerCapitaCrimeRate)
print(p1)
invisible(dev.off())

cat("--- qplot(Boston$PerCapitaCrimeRate, binwidth = 10) ---\n")
png(tempfile(fileext = ".png"))
p2 <- qplot(Boston$PerCapitaCrimeRate, binwidth = 10)
print(p2)
invisible(dev.off())

cat("range(PerCapitaCrimeRate):", range(Boston$PerCapitaCrimeRate), "\n")
```

Console output:

```
--- qplot(Boston$PerCapitaCrimeRate) ---
Warning message:
`qplot()` was deprecated in ggplot2 3.4.0. 
`stat_bin()` using `bins = 30`. Pick better value `binwidth`.
--- qplot(Boston$PerCapitaCrimeRate, binwidth = 10) ---
range(PerCapitaCrimeRate): 0.0069 4.2598 
```

**Tool equivalent:** `Jube.Charting.Chart.Histogram()` — the fluent `HistogramChartBuilder`
(`Jube.Charting/Charts/HistogramChartBuilder.cs`). `Chart.Histogram().Values("Per capita crime rate",
values).Render()` bins raw values itself using a Freedman-Diaconis-derived rule (see `BinEdges` in that same file), so
the bin-width guessing `qplot()` needs a human to correct is not a step an analyst performs at all; `.BinCount(n)` pins
an exact bin count if one is wanted instead. For the bin *selection* itself as a first-class, inspectable result rather
than a side effect of drawing a chart, the tool layer has
`Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.HistogramAndDensityTools`, tool
`"HistogramByBinningRule"` (`Jube.Service/Agent/AdvancedAnalyticsTools/DistributionsAndHistograms
/HistogramAndDensityTools.cs`), which computes edges and counts under a named rule (Sturges, Scott, Freedman-Diaconis,
Rice, SquareRoot or Doane) and reports which rule and bin width were used; its output feeds
`Chart.Histogram().Bins(name, edges, counts).Render()` directly when the edges should be computed and shown rather than
chosen implicitly by the chart.

## The product's charting story in one line

The product does not use `ggplot2`, base R graphics, or any JavaScript charting library anywhere;
`Jube.Charting` is the single charting library, for every chart on every page, report and agent tool.

For the AskJooby agent tool specifically -- `RenderChart`, its eight chart types, how a cached table's columns map
onto a chart specification, and exactly how the finished SVG reaches the person's screen (a backend-rendered file
attached to the turn's reply, not anything the model itself sees or draws) -- see `docs/AskJooby/RenderChart.md`,
which deliberately does not repeat this page's own material.
