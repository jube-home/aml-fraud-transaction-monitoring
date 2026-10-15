---
layout: default
title: Neural Networks
nav_order: 17
parent: Analytical Methodology
---

# Neural Networks

The original R training guide treated feed-forward neural networks as a "universal predictive analytics method" —
equally applicable to numeric prediction and classification, at the cost of being harder to explain as the network
grows. It reused the same FDX equity dataset from earlier regression pages and the same FraudRisk transaction dataset
from earlier classification pages, so the improvement a neural network buys over a plain regression or logistic model
could be judged directly. Six procedures follow:
train a single-hidden-layer network, plot its topology, recall it with `compute()` and read the correlation between
prediction and outcome, train a deeper multi-layer network for comparison, train a network as a binary classifier, and
activate that classifier into a confusion matrix.

The original bundle's CSV files (`PC_FDX_Close_200x1D_Close_50x1D_10.csv`, `FraudRisk.csv`) are not present in this
environment, so the R below builds synthetic data frames with the same column layout instead of loading them — `Skew_3`,
`Max_4`, `PointStep_16`, `Close_3`, `Close_4`,
`PointStep_17_ZScore`, `PointStep_15`, `TypicalValue_4`, `Range_4`, `Range_2` and a continuous
`Dependent` for FDX; the same ten fraud-signal columns and a binary `Dependent` for FraudRisk. The
`neuralnet()` calls, `compute()` calls and correlation/confusion-matrix results below are all genuine, run against that
synthetic data, not transcribed or estimated — so the numbers differ from the original guide's (which reported a 0.65
and 0.91 correlation, and a 79.9% fraud detection rate), but the same mechanics and the same direction of improvement
(deeper network, better fit) hold.

**Extra package required:** `neuralnet` is not part of this guide's shared pre-installed package set. It was installed
for this module with `install.packages("neuralnet", lib="/rlibs")`, which pulled in one dependency, `Deriv`. Version
installed: neuralnet 1.44.2.

In passing: Jube's own long-standing **Exhaustive Search** model family also trains a feed-forward neural network, but
by a different, evolved-topology method documented in
[Exhaustive Methodology](Reference/MethodologySpec.md) — this page's mapping below is to
the newer `AdvancedAnalytics` neural-network training services, not to Exhaustive.

### Procedure 1: Train a Neural Network

Training a neural network with `neuralnet()` resembles building a regression model, with two nuances: the dot notation
for "all variables" does not work (a manually written formula is required), and the function is single-threaded and can
take a long time on data frames with many rows or many variables. This example trains a network with one hidden layer of
four processing elements against ten variables already known (from the earlier correlation-based variable selection
modules) to relate to the dependent variable.

```R
library(neuralnet, lib.loc = "/rlibs")

set.seed(13)
n <- 400
FDX <- data.frame(
  Skew_3 = runif(n),
  Max_4 = runif(n),
  PointStep_16 = runif(n),
  Close_3 = runif(n),
  Close_4 = runif(n),
  PointStep_17_ZScore = runif(n),
  PointStep_15 = runif(n),
  TypicalValue_4 = runif(n),
  Range_4 = runif(n),
  Range_2 = runif(n)
)
signal <- with(FDX, 0.35 * Skew_3 + 0.25 * Max_4 + 0.15 * PointStep_16 + 0.1 * Close_3 -
  0.2 * Close_4 + 0.3 * PointStep_17_ZScore - 0.1 * PointStep_15 + 0.2 * TypicalValue_4 -
  0.15 * Range_4 + 0.1 * Range_2)
signal <- (signal - min(signal)) / (max(signal) - min(signal))
FDX$Dependent <- pmin(pmax(signal + rnorm(n, 0, 0.08), 0), 1)

str(FDX)

NeuralNetworkFourByOne <- neuralnet(Dependent ~ Skew_3 + Max_4 + PointStep_16 + Close_3 +
  Close_4 + PointStep_17_ZScore + PointStep_15 + TypicalValue_4 + Range_4 + Range_2,
  data = FDX, hidden = 4)

cat("Converged in", NeuralNetworkFourByOne$result.matrix["steps", 1], "steps\n")
cat("Error (SSE):", NeuralNetworkFourByOne$result.matrix["error", 1], "\n")
```

Console output:

```
'data.frame':	400 obs. of  11 variables:
 $ Skew_3             : num  0.7103 0.2461 0.3896 0.0914 0.9621 ...
 $ Max_4              : num  0.849 0.147 0.794 0.781 0.327 ...
 $ PointStep_16       : num  0.584 0.779 0.566 0.471 0.737 ...
 $ Close_3            : num  0.8308 0.0757 0.6656 0.7158 0.5642 ...
 $ Close_4            : num  0.9288 0.9694 0.5354 0.634 0.0228 ...
 $ PointStep_17_ZScore: num  0.2786 0.0166 0.8677 0.4752 0.8943 ...
 $ PointStep_15       : num  0.181 0.649 0.411 0.684 0.742 ...
 $ TypicalValue_4     : num  0.0824 0.6092 0.2023 0.6519 0.522 ...
 $ Range_4            : num  0.941 0.6019 0.0324 0.648 0.8165 ...
 $ Range_2            : num  0.566 0.437 0.126 0.201 0.736 ...
 $ Dependent          : num  0.505 0.194 0.584 0.488 0.826 ...
Converged in 3975 steps
Error (SSE): 1.060863
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsNeuralNetworkRegressorTrainingService` / tool
`SubmitNeuralNetworkRegressorTraining` on `AdvancedAnalyticsNeuralNetworkRegressorTrainingDto` — queues a
single-hidden-layer
feed-forward network for continuous prediction (rows, variable names and a
`HiddenUnits` count in place of `hidden=`, plus epochs, learning rate, standardisation and a train/validation/test
split), trained asynchronously by a background worker rather than blocking the caller for the training run's duration.

### Procedure 2: Plotting a Neural Network

The `neuralnet` object supports the base `plot()` function, drawing the network's nodes and connections. Despite the
reputation neural networks have for being unexplainable, the topology plot for a small network with a single hidden
layer is quite readable and reproducible by hand; that readability degrades as more layers and processing elements are
added.

```R
pdf(NULL)
plot(NeuralNetworkFourByOne)
invisible(dev.off())
cat("Rendered with plot() to a graphics device (text-only doc: not reproduced as an image)\n")
cat("Weight matrices (hidden layer, output layer):\n")
for (i in seq_along(NeuralNetworkFourByOne$weights[[1]])) {
  w <- NeuralNetworkFourByOne$weights[[1]][[i]]
  cat("  matrix", i, "dim:", paste(dim(w), collapse = " x "), "\n")
}
```

Console output:

```
Rendered with plot() to a graphics device (text-only doc: not reproduced as an image)
Weight matrices (hidden layer, output layer):
  matrix 1 dim: 11 x 4
  matrix 2 dim: 5 x 1
```

**AdvancedAnalytics equivalent:** `Jube.Charting.Diagrams.NetworkDiagram` (built via
`Diagram.Network()`/`NetworkDiagramBuilder`, in `Jube.Charting/Diagrams/NetworkDiagram.cs` and
`NetworkDiagramBuilder.cs`) — a real network/graph diagram chart type, with `Input`/`Hidden`/`Output`
layers, per-edge weights and per-node biases. It is wired up for this exact model family in
`Jube.Service.ModelDiagrams.Adapters.NeuralNetworkPipelineKindAdapter`, which reads a trained
`NeuralNetworkClassifierPipeline`/`NeuralNetworkRegressorPipeline` model's stored input weights, hidden biases, output
weights and output bias straight out of its snapshot JSON and renders them as an SVG network diagram (labelled "Neural
network regressor"/"Neural network classifier", with the sigmoid activation and unit counts in the diagram's own
subtitle) — no plot window, no image file, an SVG string served by the product's model-diagram page.

### Procedure 3: Recalling a Neural Network with `compute()` and Understanding Performance

`compute()` is used instead of `predict()` to recall a trained network, and returns a list rather than a plain vector or
matrix. The recall function is unforgiving of a data frame that still contains the dependent variable — passing it
produces a `%*%` non-conformable-arguments error — so the input frame must be subset to the independent variables first.
Because this is a numeric prediction, not classification, correlation between the recalled output and the actual
dependent variable is the performance measure.

```R
ComputedModel <- compute(NeuralNetworkFourByOne,
  FDX[, c("Skew_3", "Max_4", "PointStep_16", "Close_3", "Close_4",
          "PointStep_17_ZScore", "PointStep_15", "TypicalValue_4", "Range_4", "Range_2")])
cat("class(ComputedModel):", class(ComputedModel), "\n")
cat("names(ComputedModel):", paste(names(ComputedModel), collapse = ", "), "\n")

FDXPredeictions <- ComputedModel$net.result
cat("class(FDXPredeictions):", paste(class(FDXPredeictions), collapse = " "), "\n")
cat("dim(FDXPredeictions):", paste(dim(FDXPredeictions), collapse = " x "), "\n")

correlation <- cor(FDXPredeictions, FDX$Dependent, use = "complete", method = "pearson")
cat("cor(FDXPredeictions, FDX$Dependent):", correlation, "\n")
```

Console output:

```
class(ComputedModel): list
names(ComputedModel): neurons, net.result
class(FDXPredeictions): matrix array
dim(FDXPredeictions): 400 x 1
cor(FDXPredeictions, FDX$Dependent): 0.928771
```

In this neuralnet version (1.44.2), `compute()$net.result` comes back already as a plain numeric matrix, not a list
needing `unlist()` first — that extra step in the original guide's description no longer applies to a `compute()` call,
only to `net.result` read directly off the trained object itself (see procedure 6). The single-hidden-layer network here
reaches a Pearson correlation of 0.93 between prediction and actual on this synthetic data, a large uplift over using
any one input variable alone.

**Tool equivalent:** none for `compute()` itself — recall is not a separate step the caller invokes. The
trained `AdvancedAnalyticsNeuralNetworkRegressorTrainingService` run already scores its own held-out validation and test
splits during training and records the resulting metrics (including a correlation/R² style fit measure) as part of the
run's evidence, readable once the run's status is `Succeeded`; there is no live "call compute on arbitrary rows" tool in
this module's scope.

### Procedure 4: Training a Deeper Neural Network

Where procedure 1 used a single hidden layer, deep learning uses several. A vector passed to `hidden=`
gives one entry per hidden layer, each entry being that layer's processing-element count. This example keeps the same
ten inputs and trains a three-hidden-layer network of 8, 6 and 4 processing elements. Deep networks are lazier and
costlier to recall — which matters in high-throughput scoring — so this extra depth is only worth it if it demonstrably
improves the fit.

```R
set.seed(13)
NeuralNetworkDeep <- neuralnet(Dependent ~ Skew_3 + Max_4 + PointStep_16 + Close_3 +
  Close_4 + PointStep_17_ZScore + PointStep_15 + TypicalValue_4 + Range_4 + Range_2,
  data = FDX, hidden = c(8, 6, 4))

cat("Converged in", NeuralNetworkDeep$result.matrix["steps", 1], "steps\n")
cat("Error (SSE):", NeuralNetworkDeep$result.matrix["error", 1], "\n")
cat("Weight matrices (3 hidden layers, output layer):\n")
for (i in seq_along(NeuralNetworkDeep$weights[[1]])) {
  w <- NeuralNetworkDeep$weights[[1]][[i]]
  cat("  matrix", i, "dim:", paste(dim(w), collapse = " x "), "\n")
}

ComputedModelDeep <- compute(NeuralNetworkDeep,
  FDX[, c("Skew_3", "Max_4", "PointStep_16", "Close_3", "Close_4",
          "PointStep_17_ZScore", "PointStep_15", "TypicalValue_4", "Range_4", "Range_2")])
FDXPredeictionsDeep <- ComputedModelDeep$net.result
correlation_deep <- cor(FDXPredeictionsDeep, FDX$Dependent, use = "complete", method = "pearson")
cat("cor(FDXPredeictionsDeep, FDX$Dependent):", correlation_deep, "\n")
```

Console output:

```
Converged in 2539 steps
Error (SSE): 0.6727612
Weight matrices (3 hidden layers, output layer):
  matrix 1 dim: 11 x 8
  matrix 2 dim: 9 x 6
  matrix 3 dim: 7 x 4
  matrix 4 dim: 5 x 1
cor(FDXPredeictionsDeep, FDX$Dependent): 0.9554449
```

The deeper network improved correlation from 0.93 to 0.96 and converged in fewer steps, at the cost of four weight
matrices to reason about instead of two.

**Tool equivalent:** none for the multi-layer topology itself.
`AdvancedAnalyticsNeuralNetworkRegressorTrainingDto` (and its classifier counterpart) expose exactly one hidden layer,
sized by a single `HiddenUnits` value from 1 to 128 — there is no vector-of-layers option, so a three-hidden-layer
network as trained here has no shipped equivalent. Widening
`HiddenUnits` within that one layer is the closest supported lever; going deeper than one hidden layer stays R-only.

### Procedure 5: Training a Classification Model

Neural networks are universal classifiers as well as regressors, but every input and output must be numeric —
categorical inputs need pivoting to 0/1 first, since `neuralnet()` cannot interpret factors directly. This example
trains a network with one hidden layer of four processing elements against ten variables known to correlate with a
binary fraud outcome, on a data set with a deliberately balanced fraud rate.

```R
set.seed(98)
n2 <- 900
FraudRisk <- data.frame(
  Count_Unsafe_Terminals_1_Day = rpois(n2, 1.2),
  High_Risk_Country = rbinom(n2, 1, 0.3),
  Foreign = rbinom(n2, 1, 0.35),
  Authenticated = rbinom(n2, 1, 0.6),
  Has_Been_Abroad = rbinom(n2, 1, 0.25),
  Transaction_Amt = round(rlnorm(n2, meanlog = 4, sdlog = 1), 2),
  Different_Country_Transactions_1_Week = rpois(n2, 0.8),
  Different_Decline_Reasons_1_Day = rpois(n2, 0.6),
  Count_Transactions_Declined_1_Day = rpois(n2, 1.0),
  Count_In_Person_1_Day = rpois(n2, 2.0)
)

logit <- with(FraudRisk, -2.2 +
  0.6 * Count_Unsafe_Terminals_1_Day +
  1.4 * High_Risk_Country +
  0.9 * Foreign -
  1.1 * Authenticated +
  0.8 * Has_Been_Abroad +
  0.0008 * Transaction_Amt +
  0.7 * Different_Country_Transactions_1_Week +
  0.5 * Different_Decline_Reasons_1_Day +
  0.4 * Count_Transactions_Declined_1_Day -
  0.15 * Count_In_Person_1_Day)
p <- 1 / (1 + exp(-logit))
FraudRisk$Dependent <- rbinom(n2, 1, p)

cat("Row count:", nrow(FraudRisk), "\n")
cat("Fraud rate:", mean(FraudRisk$Dependent), "\n")

normalise <- function(x) (x - min(x)) / (max(x) - min(x))
predictors <- c("Count_Unsafe_Terminals_1_Day", "High_Risk_Country", "Foreign", "Authenticated",
  "Has_Been_Abroad", "Transaction_Amt", "Different_Country_Transactions_1_Week",
  "Different_Decline_Reasons_1_Day", "Count_Transactions_Declined_1_Day", "Count_In_Person_1_Day")
FraudRiskScaled <- FraudRisk
FraudRiskScaled[predictors] <- lapply(FraudRisk[predictors], normalise)

FraudRiskNeuralNetwork <- neuralnet(Dependent ~ Count_Unsafe_Terminals_1_Day +
  High_Risk_Country + Foreign + Authenticated + Has_Been_Abroad + Transaction_Amt +
  Different_Country_Transactions_1_Week + Different_Decline_Reasons_1_Day +
  Count_Transactions_Declined_1_Day + Count_In_Person_1_Day, data = FraudRiskScaled, hidden = 4)

cat("Converged in", FraudRiskNeuralNetwork$result.matrix["steps", 1], "steps\n")
cat("Error (SSE):", FraudRiskNeuralNetwork$result.matrix["error", 1], "\n")
```

Console output:

```
Row count: 900
Fraud rate: 0.45
Converged in 8407 steps
Error (SSE): 72.8707
```

The predictor columns were min-max scaled to 0-1 before training (`FraudRiskScaled`) — `neuralnet`
converges far more reliably against similarly-scaled inputs than against a mix of raw counts and a log-normal
transaction amount.

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsNeuralNetworkClassifierTrainingService` / tool
`SubmitNeuralNetworkClassifierTraining` on `AdvancedAnalyticsNeuralNetworkClassifierTrainingDto` — the classifier
counterpart of
procedure 1's mapping: one hidden layer sized by `HiddenUnits`, binary `Labels` instead of a continuous outcome, the
same epochs/learning-rate/standardisation options, plus a `Calibrate` flag that Platt-calibrates the network's raw
output to a genuine probability on the validation split (a step this R procedure has no equivalent of at all — the raw
`neuralnet` output below is not a calibrated probability, see procedure 6).

### Procedure 6: Activating a Classification Model and Appraising Performance

Recalling the trained classifier returns a numeric value per record; thresholding it at 0.5 and tabulating against the
actual outcome produces a confusion matrix to appraise the classifier.

```R
FraudRiskPredictions <- FraudRiskNeuralNetwork$net.result
cat("class(FraudRiskPredictions):", class(FraudRiskPredictions), "\n")
cat("length(FraudRiskPredictions):", length(FraudRiskPredictions), "\n")
cat("head(FraudRiskPredictions[[1]]):\n")
print(head(FraudRiskPredictions[[1]]))

IsFraud <- ifelse(unlist(FraudRiskPredictions) > 0.5, 1, 0)

confusion <- table(Actual = FraudRisk$Dependent, Predicted = IsFraud)
print(confusion)

accuracy <- sum(diag(confusion)) / sum(confusion)
fraud_recall <- confusion["1", "1"] / sum(confusion["1", ])
cat(sprintf("Overall accuracy: %.3f\n", accuracy))
cat(sprintf("Of %d actual fraud cases, %d were classified correctly (%.1f%% recall)\n",
  sum(confusion["1", ]), confusion["1", "1"], 100 * fraud_recall))
```

Console output:

```
class(FraudRiskPredictions): list
length(FraudRiskPredictions): 1
head(FraudRiskPredictions[[1]]):
            [,1]
[1,]  0.39418964
[2,] -0.07983988
[3,]  0.57514640
[4,]  0.01905772
[5,]  1.10198154
[6,]  0.96596537
      Predicted
Actual   0   1
     0 384 111
     1 122 283
Overall accuracy: 0.741
Of 405 actual fraud cases, 283 were classified correctly (69.9% recall)
```

Two things worth being plain about, from what was actually captured rather than what the original guide described:
reading `$net.result` straight off the trained object (rather than through a
`compute()` call, as procedure 3 did) returns a list of one matrix — one entry per training repetition — so `[[1]]` is
needed before `head()` shows anything useful; and because `neuralnet()` was called here with its default
`linear.output = TRUE`, the raw values are not squashed into `[0, 1]` at all (row 5 above is 1.10, row 2 is -0.08) —
they only happen to cluster near that range because the target itself is binary. The 0.5 threshold still produces a
sensible confusion matrix (`table()` used here in place of the original's `gmodels::CrossTable`, which needs a package
outside this guide's set for no more than a cosmetic difference in how the same cross-tabulation prints), but nothing
here should be read as
"neuralnet returns calibrated probabilities" — it does not, by default. Compare
`AdvancedAnalyticsNeuralNetworkClassifierTrainingDto`'s `Calibrate` option below.

**Tool equivalent:** tool `"EvaluateNeuralNetworkClassifier"`
(`Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelEvaluationTools`) fits and evaluates a network ad hoc
against held-out rows in one call, returning ROC AUC, a confusion table and calibration evidence directly — the path an
agent reaches for to answer "how well would this predict" without training or promoting anything.
`Jube.Engine.AdvancedAnalytics.Classification.NeuralNetworkClassifierRecall`
(model kind `"NeuralNetworkClassifierPipeline"`) is a separate, later path: the live scoring used once a version is
actually promoted, with no ad hoc tool of its own — it recalls a single row through the stored input weights, hidden
biases, output weights and output bias, applying the same Platt calibration fitted at training time so the value
returned really is a probability, not a raw sigmoid activation. Held-out confusion-matrix-style evidence (and the ROC
curve behind it) is computed once, during training, by the model's own evaluation step and stored with the run rather
than assembled ad hoc afterwards; `Jube.Charting.Charts.CalibrationChartBuilder` and the ROC chart in `Jube.Charting`
render that evidence rather than a hand-built `table()`/`CrossTable()`.
