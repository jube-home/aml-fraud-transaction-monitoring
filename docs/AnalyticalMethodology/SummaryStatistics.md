---
layout: default
title: Summary Statistics
nav_order: 9
parent: Analytical Methodology
---

# Summary Statistics and Basic Plots in R

The original guide's Module 5 loaded ten years of AAPL closing prices and used R to derive the summary statistics and
rudimentary plots that Excel and StatTools users already know: histogram, range, quartiles, mean/median, box plot,
variance/standard deviation, Z score, range normalisation, skewness/kurtosis, normal probabilities and the log
transform.

The original training bundle's `AAPL.csv` (`Bundle\Data\Equity\Equity\AAPL.csv`) is not present in this validation
environment, so every snippet below runs against a synthetic ten-year daily series (`Interim_Buffer_Date`,
`Interim_Close`, 2,518 rows, generated with `set.seed(42)` from a drifting, noisy random walk rescaled to the same
50.67-to-702.10 range the original guide describes) rather than the real historical prices. The series keeps the same
character the guide discusses — a positive skew, a mean above the median — but the individual statistic values differ
from the original text and should be read as illustrative, not as the historical AAPL numbers. `readr::read_csv()` was
also swapped for base R's
`read.csv()`, which is already present in the container and returns an equivalent data frame; `readr`
itself failed to build in the shared validation container. The `moments` package (needed for
`skewness()`/`kurtosis()`, Procedure 10) was installed cleanly into the shared library.

### Procedure 1: Create a Histogram of Time Series Data in R

A data frame is loaded from the CSV and its `Interim_Close` column — the column of interest throughout this module — is
histogrammed with the base `hist()` function:

```R
AAPL <- read.csv("AAPL.csv")

head(AAPL)

hist(AAPL$Interim_Close)
```

Console output:

```
  Interim_Buffer_Date Interim_Close
1          2005-01-03         63.44
2          2005-01-04         62.80
3          2005-01-05         63.30
4          2005-01-06         64.16
5          2005-01-07         64.73
6          2005-01-08         64.65
```

`hist()` draws to the graphics device rather than the console, so the snippet's only console effect is the
`head()` call above it; the chart itself shows a strong positive skew, the bulk of the mass at low prices and a thinning
tail towards the historical maximum.

**Tool equivalent:** `Jube.Charting.Chart.Histogram()` — `Chart.Histogram().Values("Interim
Close", values).Render()` bins the same array with Freedman-Diaconis nice bins (or precomputed bins/an overlay of
several series) and renders it as a dependency-free SVG string. This, not `hist()` or ggplot2, is what the product
draws.

### Procedure 2: Establish Range in R

`min()` and `max()` give the extremes; `range()` returns both in one call, and `diff(range(x))` collapses that to a
single spread value:

```R
AAPL <- read.csv("AAPL.csv")

min(AAPL$Interim_Close)
max(AAPL$Interim_Close)
range(AAPL$Interim_Close)
diff(range(AAPL$Interim_Close))
```

Console output:

```
[1] 50.67
[1] 702.1
[1]  50.67 702.10
[1] 651.43
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics
.CentralTendencyTools` / tool `"MinimumMaximumRange"` — one call returns the minimum, maximum, their difference (the
range) and their midpoint (the midrange) together, explicitly flagging that the range is sensitive to outliers and
pointing at `InterquartileRange` for a robust alternative.

### Procedure 3: Calculate Quartiles and the Interquartile Range

`IQR()` gives the spread of the middle half directly; `quantile()` gives the full five-point breakdown (minimum, three
quartiles, maximum) that the IQR is derived from:

```R
AAPL <- read.csv("AAPL.csv")

IQR(AAPL$Interim_Close)
quantile(AAPL$Interim_Close)
```

Console output:

```
[1] 173.8625
      0%      25%      50%      75%     100% 
 50.6700  73.8900  98.8100 247.7525 702.1000 
```

**Tool equivalent:** `DescriptiveStatistics.SpreadAndShapeTools` / tool
`"InterquartileRange"` for the single IQR value (robust to outliers, the basis of the box-plot fence), and
`DescriptiveStatistics.QuantileRankAndFrequencyTools` / tool `"FiveNumberSummary"` for the full
minimum/Q1/median/Q3/maximum breakdown — it also returns the standard 1.5x and 3x IQR box-plot fences that
`quantile()` alone does not.

### Procedure 4: Establish the Mean and Median in R

The mean sums and divides by the count; the median is the middle value once the vector is sorted:

```R
AAPL <- read.csv("AAPL.csv")

mean(AAPL$Interim_Close)
median(AAPL$Interim_Close)
```

Console output:

```
[1] 169.0461
[1] 98.81
```

The mean sitting well above the median is itself a symptom of the positive skew visible in Procedure 1's histogram.

**Tool equivalent:** `DescriptiveStatistics.CentralTendencyTools` / tools `"Mean"` and
`"Median"` — the same default-versus-robust framing carries over: `Mean` is the default measure of typical level for
roughly symmetric data, and the tool description itself recommends `Median` once extreme outliers are present.

### Procedure 5: Create a Box Plot

`boxplot()` draws the five-point summary (whiskers at the observed minimum and maximum, box at Q1 and Q3, thick line at
the median) in one call:

```R
AAPL <- read.csv("AAPL.csv")

boxplot(AAPL$Interim_Close)
```

`boxplot()`, like `hist()`, only draws to the graphics device — there is no console output to capture. The box is
visibly compressed towards the lower end of the range with a long upper whisker, the same skew already read off the
histogram and the mean/median gap.

**AdvancedAnalytics equivalent:** `Jube.Charting.Chart.BoxPlot()` — `Chart.BoxPlot().FromValues("Interim
Close", values).Render()` computes the Tukey fences and outliers from the raw array (or accepts stated statistics
directly) and renders the box plot as SVG. As with the histogram, this is the product's only charting path — never
ggplot2, never a base R graphics device.

### Procedure 6: Navigate Plots and Export Visualisations

This procedure is pure RStudio interface touring: paging back and forth between the Plots pane's history with the
back/forward arrows, and using the Export button to copy a plot to the clipboard as a bitmap for pasting into Word,
PowerPoint or Excel. It has no statistical content and no Tool equivalent to discuss — a chart built with
`Jube.Charting` is an SVG string returned to the caller (or embedded in a page), not an image copied between desktop
applications.

### Procedure 7: Create the Variance and Standard Deviation

`summary()` gives the min/Q1/median/mean/Q3/max in one call but omits variance and standard deviation, which need
`var()` and `sd()` directly:

```R
AAPL <- read.csv("AAPL.csv")

summary(AAPL$Interim_Close)
var(AAPL$Interim_Close)
sd(AAPL$Interim_Close)
mean(AAPL$Interim_Close) + sd(AAPL$Interim_Close)
```

Console output:

```
   Min. 1st Qu.  Median    Mean 3rd Qu.    Max. 
  50.67   73.89   98.81  169.05  247.75  702.10 
[1] 19018.95
[1] 137.9092
[1] 306.9553
```

Adding the standard deviation to the mean gives an upper one-sigma band of 306.96; because the equivalent lower band
(mean minus one standard deviation) falls below the series' own minimum, the "68% within one standard deviation" rule of
thumb for a normal distribution does not really hold here, consistent with the skew already seen in Procedures 1 and 5.

**Tool equivalent:** `DescriptiveStatistics.TableAndTransformTools` / tool `"DescribeColumn"`
for the `summary()` step — a single richer profile (count, missing, mean, median, spread, quartiles, skewness, kurtosis,
percentiles 1-99, outlier counts by three different rules and a plain-language reading of the shape) rather than six
bare numbers. `DescriptiveStatistics.SpreadAndShapeTools` / tools
`"Variance"` and `"StandardDeviation"` cover `var()` and `sd()` directly.

### Procedure 8: Calculate a Z Score

A Z score expresses how many standard deviations a value sits from the mean: `(value - mean) / sd`. Taking 150 as the
test value:

```R
AAPL <- read.csv("AAPL.csv")

(150 - mean(AAPL$Interim_Close)) / sd(AAPL$Interim_Close)
```

Console output:

```
[1] -0.1381057
```

150 sits a little under one-seventh of a standard deviation below the mean — close to average by this measure, though
the same reservations about non-normal shape from Procedure 7 apply to how much that should be trusted.

**Tool equivalent:** `DescriptiveStatistics.TableAndTransformTools` / tool `"ZScores"` — standardises every
value in a series to `(value - mean) / standard deviation` in one call and returns the centre and scale it used, rather
than requiring the formula to be written out by hand for a single value.

### Procedure 9: Create a Range Normalisation for a Value

Range (min-max) normalisation places a value on a 0-to-1 scale between the observed minimum and maximum:
`(value - min) / (max - min)`. Taking the same test value of 150:

```R
AAPL <- read.csv("AAPL.csv")

(150 - min(AAPL$Interim_Close)) / (max(AAPL$Interim_Close) - min(AAPL$Interim_Close))
```

Console output:

```
[1] 0.1524799
```

150 sits at about 15% of the way from the observed minimum to the observed maximum.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
.ScalingTools` / tool `"FitTableScaler"` with `TableScalingMethod.MinMax` — learns the same `(value -
minimum) / range` rescaling per column from a batch of rows and returns the centres/scales so
`"ApplyTableScaler"` can transform new data identically later; this lives in `DataPreparationAndResampling`
rather than `DescriptiveStatistics`, since range normalisation is a data-preparation transform rather than a descriptive
measurement.

### Procedure 10: Create the Skewness and Kurtosis Statistics

Skewness and kurtosis are not in base R; they come from the `moments` package. Skewness measures the lean towards or
away from the y-axis, and kurtosis measures how tall or flat the distribution is relative to normal:

```R
library(moments)

AAPL <- read.csv("AAPL.csv")

skewness(AAPL$Interim_Close)
kurtosis(AAPL$Interim_Close)
```

Console output:

```
[1] 1.639552
[1] 5.197058
```

The positive skewness confirms the right-lean already seen in the histogram; the kurtosis well above 3 means the
distribution is more sharply peaked with heavier tails than a normal distribution of the same variance.

**Tool equivalent:** `DescriptiveStatistics.SpreadAndShapeTools` / tools `"Skewness"` and
`"Kurtosis"` — `Kurtosis` is the Pearson (non-excess) form scoring 3 for a normal distribution, matching what
`moments::kurtosis()` returns (as opposed to the tool `"ExcessKurtosis"`, which is centred on 0). Both tools state their
rule-of-thumb thresholds (skewness beyond +/-1 is "strongly skewed") directly in their descriptions.

### Procedure 11: Create Probabilities from a Test Value in a Normal Distribution

`dnorm()` gives a value's height on the standard normal density curve; `pnorm()` gives the cumulative probability of
drawing a value at or below it. Both take a Z score directly:

```R
dnorm(1.5)
pnorm(1.5)
```

Console output:

```
[1] 0.1295176
[1] 0.9331928
```

A Z score of 1.5 sits at about 93.3% cumulative probability under a standard normal curve.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
.UnivariateDistributionTools` / tools `"DistributionDensity"` and `"DistributionCumulativeProbability"` — both take a
`DistributionKind` (`Normal`, with `parameters = [mean, standardDeviation]`) and an array of points, so `dnorm(1.5)`
becomes `DistributionDensity(Normal, [0, 1], [1.5])` and `pnorm(1.5)` becomes
`DistributionCumulativeProbability(Normal, [0, 1], [1.5])` — evaluated over many points and many distribution families
in one call, not just the normal.

### Procedure 12: Create a Log Transformation

A natural log transform compresses a right-skewed series like this one towards symmetry, which is why it is a common
first step before methods that assume roughly normal input:

```R
AAPL <- read.csv("AAPL.csv")

AAPL$Log_Close <- log(AAPL$Interim_Close)

head(AAPL)

skewness_before <- moments::skewness(AAPL$Interim_Close)
skewness_after <- moments::skewness(AAPL$Log_Close)
skewness_before
skewness_after
```

Console output:

```
  Interim_Buffer_Date Interim_Close Log_Close
1          2005-01-03         63.44  4.150095
2          2005-01-04         62.80  4.139955
3          2005-01-05         63.30  4.147885
4          2005-01-06         64.16  4.161380
5          2005-01-07         64.73  4.170225
6          2005-01-08         64.65  4.168988
[1] 1.639552
[1] 0.7134221
```

The log-transformed column's skewness (0.71) is markedly lower than the original series' skewness (1.64):
the transform has genuinely pulled the distribution's shape towards symmetry, not just changed its scale.

**Tool equivalent:** `DataPreparationAndResampling.ScalingTools` / tool
`"ApplyColumnFunction"` with `ColumnFunction.NaturalLog` — applies `ln(x + offset)` to every cell of a table in one call
(offset handles zeros), alongside log10, log2, log1p, signed-log1p and several other fixed functions, and reports how
many cells were undefined for the function chosen.

### Procedure 13: Reverse a Log Transformation

The natural log is undone with `exp()`; applying it to the logged column should recover the original values exactly,
subject to floating-point rounding:

```R
AAPL <- read.csv("AAPL.csv")

AAPL$Log_Close <- log(AAPL$Interim_Close)

AAPL$Restored_Close <- exp(AAPL$Log_Close)

head(AAPL)

all.equal(AAPL$Interim_Close, AAPL$Restored_Close)
```

Console output:

```
  Interim_Buffer_Date Interim_Close Log_Close Restored_Close
1          2005-01-03         63.44  4.150095          63.44
2          2005-01-04         62.80  4.139955          62.80
3          2005-01-05         63.30  4.147885          63.30
4          2005-01-06         64.16  4.161380          64.16
5          2005-01-07         64.73  4.170225          64.73
6          2005-01-08         64.65  4.168988          64.65
[1] TRUE
```

**Tool equivalent:** none found — `ApplyColumnFunction`'s `ColumnFunction` enum (`NaturalLog`,
`Log10`, `Log2`, `Log1P`, `SignedLog1P`, `Sqrt`, `SignedSqrt`, `CubeRoot`, `Reciprocal`, `Square`,
`AbsoluteValue`) has no exponential entry to reverse a log transform it applied; `FitTableScaler` /
`InvertTableScaler` (Procedure 9) invert a *scaler* they fitted, not an arbitrary column function. Reversing
`ApplyColumnFunction(NaturalLog)` today has no dedicated tool and would need to be done by the caller.
