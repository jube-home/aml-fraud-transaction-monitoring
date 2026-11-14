---
layout: default
title: Model Drift Monitoring
nav_order: 17
parent: Concepts
---

# Model Drift Monitoring

A model that was sound when it was promoted can quietly stop being sound when the population it scores changes. Drift
monitoring watches for that. It is off by default; set `EnableModelDriftMonitoring` to `True` to turn it on.

## What is measured

For every model with recent archived transactions, and for every Exhaustive adaptation score and every Advanced
Analytics adaptation score archived with them, the job compares two windows:

- the recent window, `ModelDriftRecentDays` long (7 by default), ending now; and
- the baseline window, `ModelDriftBaselineDays` long (28 by default), ending where the recent window begins.

The comparison is the population stability index (PSI). The baseline scores are cut into ten equal-frequency bins, the
share of each window falling in each bin is taken (floored at 0.0001 so an empty bin cannot blow up the logarithm), and
PSI is the sum of `(recent share - baseline share) x ln(recent share / baseline share)`.

| PSI               | Status            |
|-------------------|-------------------|
| under 0.1         | Stable            |
| 0.1 to under 0.25 | Moderate drift    |
| 0.25 or more      | Significant drift |

A score is skipped, and nothing is recorded, when either window holds fewer than `ModelDriftMinimumSamples` scores (200
by default). At most `ModelDriftMaximumSamples` scores (newest first) are read for each window.

## What is recorded and who is told

Every check writes a row to `ModelDriftObservation` (model, source, score name, date, both counts, PSI and status). The
first time a score reaches significant drift, and then at most once a day, each user holding the Read Write Model
permission in that tenant receives an in-app notification naming the model, the score and the PSI. The check runs every
`WaitModelDrift` milliseconds (daily by default) and reads the archive on the main connection.

<!-- TODO(screenshot): The bell showing a model drift notification. -->

## What it is not

The drift history appears on the model's card (see Model Governance and Model Cards).

<!-- TODO(docs): drift is the score distribution only. Input-variable drift, AUC and calibration against outcomes, and a baseline stored at promotion are not built. -->

PSI says the scores moved. It does not say the model is wrong; the population may legitimately have changed. Treat a
significant notification as a prompt to review the model with the Backtest and the case outcomes.
