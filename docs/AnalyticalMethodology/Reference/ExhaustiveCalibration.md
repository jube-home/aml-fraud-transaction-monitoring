---
layout: default
title: Exhaustive Calibration
nav_order: 6
parent: Reference
grand_parent: Analytical Methodology
---

# Exhaustive calibration

Exhaustive's search network is trained and selected on a class-balanced sample and its raw output is not a calibrated
probability (see [Exhaustive Methodology](MethodologySpec.md) §7.5). Every promoted model's score is calibrated to a
probability at the deployment base rate before it reaches a rule, a report or a recall caller.

## Why more than one calibration method

Platt scaling is a good general default, but is not always the best fit: Niculescu-Mizil and Caruana (2005) note
isotonic regression can do better with enough calibration data (it is non-parametric and can represent shapes Platt's
fixed sigmoid cannot), and Kull, Silva Filho and Flach (2017) show beta calibration — also a two or three parameter
form, but asymmetric — often improves on Platt for score families whose miscalibration is not symmetric (their
example is Naive Bayes; networks with saturating activations can show the same asymmetric pattern). An instance can
choose `automatic` (the default, which fits every eligible method on the validation split and picks the one with the
lowest Brier score there), or pin one of `platt`, `isotonic` or `beta` directly.

### The three methods

| Method | Shape | Needs | Reference |
|---|---|---|---|
| Platt | `sigmoid(slope * x + intercept)` | Any score family (raw or logit input) | Platt (1999) |
| Isotonic | non-parametric monotone step function, linear-interpolated between pooled block midpoints, clamped outside the fitted range | Any family; needs more data than Platt to avoid overfitting, since it has no parametric constraint | Niculescu-Mizil and Caruana (2005) |
| Beta | `sigmoid(a*ln(s) + b*ln(1-s) + c)` for a score `s` already bounded in (0, 1) | A bounded sigmoid output family only | Kull, Silva Filho and Flach (2017) |

## Behaviour: the value a model returns is not the raw network output

For every model promoted with a calibrator, the value an Exhaustive model returns — in a rule, a report or a recall
call — is a calibrated probability at the deployment base rate, not the raw network output.

- Anything that thresholds the score sees different values than the raw output would give. At a 2 percent base rate
  very few rows exceed 0.5, so a fixed 0.5 rule fires far less often; the minimum-risk threshold (derived from a cost
  matrix on calibrated probabilities) is the better choice of cut-off at a low base rate.
- A model promoted before calibration existed has no stored calibrator and keeps its raw, uncalibrated score until it
  is retrained.
- A model with a stored calibrator never returns its raw value: the calibrator is monotone, so ranking is preserved,
  but the number itself changes meaning from "network output" to "probability."

## Design

### What is calibrated, on which split

The calibrator is fitted once, at the moment of promotion, on the validation (cross-validation) split — the same
split that drives activation-function choice, topology growth and variable elimination inside a trial, but which
does not itself select the promoted trial (promotion is by score on the "test" split; see
[Exhaustive Methodology](MethodologySpec.md) §7.2). Fitting the calibrator on a split that already drove selection
inside the trial makes the fitted slope very slightly too steep (over-confident): the measured effect of this is
quantified below and found to be small relative to sampling noise on these sample sizes.

### Input mode: raw score or logit

Which form the calibrator is fit on depends on the promoted network's output activation function:

| Output activation shape | Mode | Reason |
|---|---|---|
| Bounded (0, 1) sigmoid | logit of the bounded score | logit of the output is exactly a linear function of the network's pre-activation value, so Platt on it is equivalent to fitting the logistic head directly |
| Unbounded, or bounded (-1, 1) | raw score | logit is undefined outside (0, 1) |
| Hard output (threshold, stochastic) | calibration skipped | a hard or random output is already degenerate; a sigmoid map would only relabel two values |

### Skip rules and minimum sample size

Calibration is skipped, with a recorded reason, rather than silently fitted on too little data or on a degenerate
input, when: the validation split has a single class; there are fewer than 20 of either class (Peduzzi et al. 1996:
roughly ten events per fitted parameter, and Platt fits two); scores are constant or non-finite; the output activation
is a hard-output family; or the fitted slope is not positive (the model ranks the wrong way on validation, and a
calibrator must never silently invert a model's ranking).

### Prior shift: sample balance versus deployment base rate

Exhaustive trains on a class-balanced sample, so the validation split — and so the fitted calibrator — is at a near
50/50 prior, not the real-world fraud rate. The calibrated probability is shifted from the sample prior to the
deployment base rate by a logit offset (`log(target odds / training odds)`), applied once, at recall. The base rate
itself is a population quantity, resolved from an analyst-supplied override, from the population counts behind the
training sample, or — if neither is available — left unshifted at the sample prior:

| Source | Base rate |
|---|---|
| Analyst override | the override value |
| Labelled population (archive, demo or uploaded dataset) | matching rows divided by (matching plus non-matching) |
| Anomaly-labelled data | the anomaly detector's flagged fraction (a contamination rate, not a fraud rate) |
| Nothing available | no shift; probability stays at the sample prior |

For anomaly-labelled instances the calibrated probability is the probability of being flagged anomalous at the
contamination rate, not a probability of fraud — a distinction the stored result records.

### Recalibration from stored results is not offered

The stored predicted-versus-actual rows are the test split. Fitting a calibrator from them after the fact would leak
the test set into the calibrator and make the already-reported test-split quality figures no longer an honest
held-out check. A model is calibrated once, at promotion; retraining is the only way to recalibrate it.

### References

Platt (1999), probabilistic outputs for support vector machines; Niculescu-Mizil and Caruana (2005), predicting good
probabilities with supervised learning; Saerens, Latinne and Decaestecker (2002), adjusting the outputs of a
classifier to new a priori probabilities; Kull, Silva Filho and Flach (2017), beta calibration; Peduzzi et al. (1996),
events per variable.

## Measured results

Real training on the demonstration dataset (926 genuine and 901 fraud rows), through the full promotion path, three
seeds. The rows are split in half before anything else: the development half is balanced and trained on (as Exhaustive
does), the other half is a held-out deployment population, built to a 2 percent base rate (about 470 rows, 9 fraud)
and supplied to the training run as an explicit base-rate override (since the demonstration data is too small to form
a 2 percent population and still leave a usable balanced training sample). The population is small and has few
positives, so these figures are illustrative, not precise estimates, and because the search itself is not seeded, the
winning configuration — and so the exact figures — vary a little between runs.

Deployment population at a 2 percent base rate, raw versus calibrated-and-shifted:

| Seed | ECE (raw to calibrated) | Brier (raw to calibrated) | Log loss (raw to calibrated) | AUC |
|---|---|---|---|---|
| 1 | 0.2538 to 0.0020 | 0.1197 to 0.0175 | 0.4246 to 0.0735 | 0.8531 (unchanged) |
| 2 | 0.2759 to 0.0097 | 0.1523 to 0.0187 | 1.3108 to 0.0957 | 0.5643 (unchanged) |
| 3 | 0.2416 to 0.0049 | 0.1207 to 0.0179 | 0.4107 to 0.0743 | 0.9035 (unchanged) |

AUC is unchanged because the calibration map is strictly monotone: a probability calibrator makes the predicted
probabilities honest without changing which rows are ranked above which. On a balanced sample the improvement from
Platt scaling alone is small, because a network trained with squared error on balanced data is already close to
calibrated at that prior; the large improvement in the table above is almost entirely the prior (base-rate) shift,
which is only meaningful once the model is deployed against the real, imbalanced population.

Optimism from fitting the calibrator on the split that also drives selection inside a trial, measured by comparing it
against a calibrator fitted on the test rows themselves (the best a two-parameter map can do on that data): the gap
was at most 0.0075 Brier and 0.029 log loss across the three seeds, and in one case the validation fit was *less*
steep than the test fit — the opposite of the over-confidence that selection bias would predict — so the gap observed
is consistent with ordinary sampling noise on a few-hundred-row split rather than a systematic optimism effect.
