---
layout: default
title: Monte Carlo Simulation
nav_order: 1
parent: Prescriptive Analytics
grand_parent: Analytical Methodology
---

# Monte Carlo Simulation

## What Monte Carlo simulation does

Monte Carlo simulation runs a trained model against millions of synthetic, statistically-shaped records rather than real
transactions. It serves three distinct purposes: simulating a model's behaviour before
it ever reaches production, identifying which scenarios are optimal against which are merely speculative extremes, and
manufacturing a dataset for predictive analytics work in the first place where real data is not otherwise available. The
worked procedures below demonstrate the first purpose directly (scoring a simulated population through an
already-trained model to see how it behaves) and, in Procedure 3's filter-and-summarise step, the second (describing
what the model's activated scenarios actually look like, the same judgement a "which of these is optimal, which is
merely speculative" ranking would apply to a real prescriptive exercise); the third purpose — using simulation to
manufacture training data where none exists — is a natural extension of the same three-step pattern rather than a
different one, and is not separately demonstrated here.

The procedure has three steps:
draw a triangular-distribution vector for every input variable of a model, assemble those vectors into a simulated data
frame the same shape as the training data, score that data frame through the model, and then summarise only the rows the
model activated.

The worked example below uses a small synthetic fraud data set (`FraudRisk`, six input
variables, an `Activation` label) and a plain R
`neuralnet` model, since H2O deep learning has no place in `AdvancedAnalytics`. The pattern — triangular vectors, a
simulated data frame, a recall pass, a filter-and-summarise pass — is set out in the three procedures below.

### Procedure 1: Create discrete vectors with a triangular distribution for each model parameter

For each input variable of the model to be simulated, a triangular distribution is fitted from the real training data's
minimum, maximum and mean, and 100,000 values are drawn from it. The `rtriangle()` function (package `triangle`) takes
the simulation count, the minimum, the maximum and the mean or mode. Doing this for every input variable and binding the
results into a data frame produces a simulated record set the same shape as the model's training data, ready to be
scored.

One limitation worth naming up front: `rtriangle()` does not know that a variable such as `Authenticated`
or `Foreign` is really a 0/1 flag rather than a continuous quantity — fitting a triangular distribution to a binary
column's min, max and mean produces fractional values spread across the whole `[0, 1]` range, not a mix of exact zeros
and ones. This limitation is called out here rather than silently corrected.

```R
suppressPackageStartupMessages(library(triangle))

set.seed(2026)
n <- 5000
Count_Transactions_1_Day <- rpois(n, lambda = 6)
Authenticated <- rbinom(n, 1, 0.7)
Transaction_Amt <- round(rexp(n, rate = 1 / 250), 2)
Foreign <- rbinom(n, 1, 0.08)
Different_Merchant_Types_1_Week <- rpois(n, lambda = 3)
High_Risk_Country <- rbinom(n, 1, 0.05)

logit <- -3 + 0.05 * Transaction_Amt / 100 + 1.8 * Foreign + 2.1 * High_Risk_Country +
  0.35 * Different_Merchant_Types_1_Week - 1.1 * Authenticated
probability <- 1 / (1 + exp(-logit))
Activation <- rbinom(n, 1, probability)

FraudRisk <- data.frame(Count_Transactions_1_Day, Authenticated, Transaction_Amt, Foreign,
                         Different_Merchant_Types_1_Week, High_Risk_Country, Activation)

cat("FraudRisk training rows:", nrow(FraudRisk), " base rate:", mean(FraudRisk$Activation), "\n")

Count_Transactions_1_Day <- rtriangle(100000, min(FraudRisk$Count_Transactions_1_Day),
                                       max(FraudRisk$Count_Transactions_1_Day),
                                       mean(FraudRisk$Count_Transactions_1_Day))

cat("First variable simulated - Count_Transactions_1_Day summary:\n")
print(summary(Count_Transactions_1_Day))

cat("\nHistogram bin counts (text form, in place of the console histogram):\n")
print(hist(Count_Transactions_1_Day, plot = FALSE)$counts)

Authenticated <- rtriangle(100000, min(FraudRisk$Authenticated), max(FraudRisk$Authenticated),
                            mean(FraudRisk$Authenticated))
Transaction_Amt <- rtriangle(100000, min(FraudRisk$Transaction_Amt), max(FraudRisk$Transaction_Amt),
                              mean(FraudRisk$Transaction_Amt))
Foreign <- rtriangle(100000, min(FraudRisk$Foreign), max(FraudRisk$Foreign), mean(FraudRisk$Foreign))
Different_Merchant_Types_1_Week <- rtriangle(100000, min(FraudRisk$Different_Merchant_Types_1_Week),
                                              max(FraudRisk$Different_Merchant_Types_1_Week),
                                              mean(FraudRisk$Different_Merchant_Types_1_Week))
High_Risk_Country <- rtriangle(100000, min(FraudRisk$High_Risk_Country), max(FraudRisk$High_Risk_Country),
                                mean(FraudRisk$High_Risk_Country))

SimulatedDataFrame <- data.frame(Count_Transactions_1_Day, Authenticated, Transaction_Amt, Foreign,
                                  Different_Merchant_Types_1_Week, High_Risk_Country)

cat("\nSimulatedDataFrame dimensions:", nrow(SimulatedDataFrame), "rows,",
    ncol(SimulatedDataFrame), "columns\n")
cat("\nSimulatedDataFrame head:\n")
print(head(SimulatedDataFrame, 3))
```

Console output:

```
FraudRisk training rows: 5000  base rate: 0.1302 
First variable simulated - Count_Transactions_1_Day summary:
    Min.  1st Qu.   Median     Mean  3rd Qu.     Max. 
 0.05358  4.88365  7.05692  7.33074  9.67364 15.95760 

Histogram bin counts (text form, in place of the console histogram):
 [1]  1095  3069  5180  7365  9507 11440 11724 10591  9407  8041  6844  5670
[13]  4450  3165  1792   660

SimulatedDataFrame dimensions: 100000 rows, 6 columns

SimulatedDataFrame head:
  Count_Transactions_1_Day Authenticated Transaction_Amt   Foreign
1                 3.790573    0.30703535       1150.6833 0.3173495
2                 6.787687    0.86862863        967.3515 0.5267901
3                 6.852522    0.08304967        123.6996 0.2986538
  Different_Merchant_Types_1_Week High_Risk_Country
1                        3.566598         0.2525322
2                        4.206525         0.1765564
3                        4.864761         0.7089584
```

**Tool equivalent:** none for the distribution-fitting step itself — `AdvancedAnalyticsTools`
has no tool that "reads a training column's min/max/mean and builds a matching triangular fit" the way this procedure
does by hand. It does have the underlying random-draw primitive:
`Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.UnivariateDistributionTools`, tool
`"DistributionRandomSample"`, called with `DistributionKind.Triangular` and `parameters = [minimum, maximum,
mode]` (see `DistributionCatalogue.cs`'s `Triangular` entry), draws the same shaped values from a fixed seed in one call
per variable — the caller still supplies the minimum, maximum and mode, typically read off the model's own stored
training-time variable statistics (`AdvancedAnalyticsTrainingRunVariable.Minimum/Maximum/Mean`, see
`docs/AnalyticalMethodology/Reference/FoundationData.md`)
rather than recomputed from a raw data frame. The simulated columns are assembled into the packed `double[]`
`Rows`/`Columns` shape used everywhere in the product (`AdvancedAnalyticsCache`,
`AdvancedAnalyticsPacking.Pack`), not an R data frame — see
[DataFramesVsAccordArrays.md](../DataFramesVsAccordArrays.md) for that structural comparison in full. There is no
product tool that performs `data.frame()`'s column bind or `View()`'s interactive grid; a caller (agent or test) builds
the packed array directly.

### Procedure 2: Process the random data frame against a neural network model

The simulated data frame is scored through the same model that was trained on the real data, exactly as if it were a
batch of real transactions. In place of H2O's `as.h2o()`/`h2o.predict()`, a plain R `neuralnet`
model plays that role here: it is trained on `FraudRisk` (min-max scaled, as `neuralnet` expects), then its
`compute()` recall is run over the simulated data frame (scaled the same way) to give one activation probability per
simulated row. Thresholding that probability at 0.5 gives a 0/1 predicted activation, which is appended back onto the
simulated data frame as its own column.

```R
suppressPackageStartupMessages(library(dplyr))
suppressPackageStartupMessages(library(neuralnet))

load("/work/monte_carlo_state1.RData")

scale_cols <- setdiff(names(FraudRisk), "Activation")
mins <- sapply(FraudRisk[scale_cols], min)
maxs <- sapply(FraudRisk[scale_cols], max)

scale_with <- function(df) {
  scaled <- df
  for (col in scale_cols) {
    scaled[[col]] <- (df[[col]] - mins[col]) / (maxs[col] - mins[col])
  }
  scaled
}

FraudRiskScaled <- scale_with(FraudRisk)
FraudRiskScaled$Activation <- FraudRisk$Activation

set.seed(2026)
Model <- neuralnet(Activation ~ Count_Transactions_1_Day + Authenticated + Transaction_Amt +
                      Foreign + Different_Merchant_Types_1_Week + High_Risk_Country,
                    data = FraudRiskScaled, hidden = 4, linear.output = FALSE, threshold = 0.05,
                    stepmax = 1e6)

cat("Neural network converged in", Model$result.matrix["steps", 1], "steps\n")

SimulatedDataFrameScaled <- scale_with(SimulatedDataFrame)

SimulatedHex <- SimulatedDataFrameScaled
SimulatedScores <- compute(Model, SimulatedHex)

SimulatedActivationProbability <- as.vector(SimulatedScores$net.result)
SimulatedActivations <- as.integer(SimulatedActivationProbability >= 0.5)

cat("\nSimulated activation probability summary:\n")
print(summary(SimulatedActivationProbability))

cat("\nSimulated activation count (0/1):\n")
print(table(SimulatedActivations))

SimulatedDataFrame <- mutate(SimulatedDataFrame, SimulatedActivations)

cat("\nSimulatedDataFrame last column, first rows:\n")
print(tail(names(SimulatedDataFrame), 1))
print(head(SimulatedDataFrame[, c("Transaction_Amt", "Foreign", "High_Risk_Country", "SimulatedActivations")], 5))
```

Console output:

```
Neural network converged in 3058 steps

Simulated activation probability summary:
   Min. 1st Qu.  Median    Mean 3rd Qu.    Max. 
0.04936 0.47759 0.69856 0.65127 0.80771 0.96276 

Simulated activation count (0/1):
SimulatedActivations
    0     1 
26880 73120 

SimulatedDataFrame last column, first rows:
[1] "SimulatedActivations"
  Transaction_Amt    Foreign High_Risk_Country SimulatedActivations
1       1150.6833 0.31734953         0.2525322                    1
2        967.3515 0.52679015         0.1765564                    0
3        123.6996 0.29865384         0.7089584                    1
4        248.2528 0.08857475         0.2815015                    0
5        667.1251 0.65774615         0.2656655                    1
```

(The elevated activation rate above the training base rate of 13% is a direct consequence of the Procedure 1 limitation
noted above: the simulated `Foreign` and `High_Risk_Country` columns spend most of their mass in fractional territory
around 0.2–0.7 rather than being mostly-zero flags, which pushes more simulated rows into the model's higher-risk region
than real transactions ever occupy.)

**AdvancedAnalytics equivalent:** this is exactly the product's own recall path, not something bespoke to simulation. A
trained model's stored `ModelJson` snapshot is turned into a live scorer by
`Jube.Engine.AdvancedAnalytics.Recall.AdvancedAnalyticsModelRegistry.Build(modelJson)`, which returns an
`AdvancedAnalyticsRecallModel` carrying the variable order and a `Predict` delegate of type
`Func<double[], double[]>` (for the neural-network classifier family, backed by
`Jube.Engine.AdvancedAnalytics.Classification.NeuralNetworkClassifierRecall.FromSnapshot(...).AsFunction()`).
Calling that one delegate once per packed row *is* "process random data frame against neural network model": there
is no `as.h2o()` staging step because there is no separate model server to stage into, and no manual min-max rescale
because the recall pipeline stores and reapplies its own training-time standardisation per
[Shared Statistical Primitives](../Reference/FoundationAnalytics.md)'s preprocessing note. Recall itself is engine
backed and has no HTTP operation in the services — an agent (or, for this simulation use case, a batch job that
draws simulated rows and calls the same delegate) drives it directly, the same way this script drives `compute()`
directly rather than through a web request.

### Procedure 3: Filter the data frame for activations and produce summary statistics to prescribe

With every simulated row now carrying a predicted activation, the rows the model actually activated can be pulled out
into their own data frame, and summary statistics run against just those rows describe what an activation scenario
typically looks like — the prescriptive payoff of the whole exercise.

```R
suppressPackageStartupMessages(library(dplyr))

load("/work/monte_carlo_state2.RData")

SimulatedAndActivated <- filter(SimulatedDataFrame, SimulatedActivations == 1)

cat("Activated rows:", nrow(SimulatedAndActivated), "of", nrow(SimulatedDataFrame), "\n\n")

cat("summary(Count_Transactions_1_Day) restricted to activated rows:\n")
print(summary(SimulatedAndActivated$Count_Transactions_1_Day))

cat("\nsummary(Transaction_Amt) restricted to activated rows:\n")
print(summary(SimulatedAndActivated$Transaction_Amt))

cat("\nFor comparison, summary(Transaction_Amt) across all simulated rows:\n")
print(summary(SimulatedDataFrame$Transaction_Amt))
```

Console output:

```
Activated rows: 73120 of 100000 

summary(Count_Transactions_1_Day) restricted to activated rows:
    Min.  1st Qu.   Median     Mean  3rd Qu.     Max. 
 0.05358  4.95409  7.22072  7.38370  9.75887 15.91924 

summary(Transaction_Amt) restricted to activated rows:
    Min.  1st Qu.   Median     Mean  3rd Qu.     Max. 
   1.333  430.135  751.157  809.006 1142.545 2015.873 

For comparison, summary(Transaction_Amt) across all simulated rows:
    Min.  1st Qu.   Median     Mean  3rd Qu.     Max. 
   1.333  383.329  683.123  757.109 1075.402 2015.873 
```

In this simulated run, activated rows have a noticeably higher median `Transaction_Amt` (751 against 683 across the
whole simulated population) — exactly the kind of prescriptive read this filtered summary is meant to produce.

**Tool equivalent:** the filter step itself — keeping only the rows whose recalled score crosses the
activation threshold — is a host-side mask over the array `Predict` already returned, not a tool call; there is no
`AdvancedAnalyticsTools` "filter rows by predicate" method, honestly, because the recalled scores are already sitting in
memory as a `double[]` the caller indexed with. What the product does give a real tool for is the second half of this
procedure — summarising the retained rows. Any of
`Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics.TableAndTransformTools`'s
`"DescribeColumn"` or `"SummariseColumns"` tools, or `QuantileRankAndFrequencyTools`'s `"FiveNumberSummary"`, run
against the masked rows reproduce `summary()`'s min/quartiles/mean/max in one call, on either a single column
(`DescribeColumn`) or every column of the filtered set at once (`SummariseColumns`) — giving a rounded picture of the
whole activation scenario rather than repeating
`summary()` column by column. This is exactly the recall-and-prescribe pattern `AdvancedAnalytics` supports end to end:
score simulated rows through the `AnalyticsRecall`-style `Func<double[], double[]>` delegate from Procedure 2, then hand
the activated subset to the descriptive-statistics tool group to say what the activation scenario looks like — the same
two-step shape as this procedure, with a genuine product tool at each step except the mask itself.
