---
layout: default
title: Model Drift Monitoring
nav_order: 17
parent: Concepts
---

# Model Drift Monitoring

A model that was sound when it was promoted can quietly stop being sound when the population that it scores changes, without anything in the model itself having been altered. Drift monitoring exists to watch for that, by comparing the scores a model has produced of late with the scores it produced beforehand. It is off by default, and is turned on by setting the `EnableModelDriftMonitoring` Environment Variable to `True`.

## Where it lives

Drift monitoring has no page of its own, and nothing is configured for it in the user interface. It is a background job, set entirely by Environment Variables, and what it finds surfaces in two places:

* The Notifications bell, where a user holding permission 6, Read Write Model, is told of a score that has drifted significantly, in the form Model {model}: the score {score} has drifted significantly (PSI {psi}).
* The model card, downloaded from Administration, then Model governance, using Model card on the row of the model, which carries the drift history of each score as a chart and a table, as described in [Model Governance and Model Cards](../ModelGovernance/index.html).

## What is measured

For every model with recent archived transactions, and for every Exhaustive adaptation score and every Advanced Analytics adaptation score archived with them, the job compares two windows:

* The recent window, `ModelDriftRecentDays` long (7 by default), ending now.
* The baseline window, `ModelDriftBaselineDays` long (28 by default), ending where the recent window begins.

The comparison is the population stability index (PSI). The baseline scores are cut into ten equal-frequency bins, the share of each window falling in each bin is taken (floored at 0.0001 such that an empty bin cannot blow up the logarithm), and PSI is the sum of `(recent share - baseline share) x ln(recent share / baseline share)`. It can be seen that the baseline is not fixed at the point of promotion, rather it moves along behind the recent window, such that the comparison is always of the last week against the four weeks before it.

In plain words the calculation is as follows. The scores of the baseline are put in order and divided into ten bands holding a tenth of them each, which is what equal-frequency means: the lowest tenth of scores, the next tenth, and so on to the highest. Were nothing to have changed, a tenth of the scores of the recent week would fall in each band likewise, and the PSI would be 0. The PSI grows as the recent week piles up in some bands and thins out in others, and it does not matter in which direction: scores drifting upwards and scores drifting downwards both raise it.

Two examples give a sense of scale. Suppose that the highest band, which held 10% of the baseline, holds 20% of the recent week, the other nine bands thinning evenly to make room. The PSI is about 0.08, which is Stable, notwithstanding that twice as many transactions as before are scoring in the highest band. Suppose instead that it holds 30%. The PSI is about 0.27, which is Significant drift. It follows that the measure is deliberately slow to alarm, and that a Significant drift reflects a large change in how the model is scoring the population.

A PSI is always zero or more and has no upper limit, and it is a number without a unit: it is neither a percentage nor a probability.

| PSI               | Status            | What to do                                                                                              |
|-------------------|-------------------|---------------------------------------------------------------------------------------------------------|
| under 0.1         | Stable            | Nothing.                                                                                                |
| 0.1 to under 0.25 | Moderate drift    | Nobody is notified. It is seen on the model card, and is worth watching from one check to the next.     |
| 0.25 or more      | Significant drift | The holders of permission 6 are notified. The model should be reviewed, as described under What it is not below. |

A score is skipped, and nothing is recorded, when either window holds fewer than `ModelDriftMinimumSamples` scores (200 by default). At most `ModelDriftMaximumSamples` scores (newest first, 50000 by default) are read for each window.

## What is recorded and who is told

Every check writes a row to the `ModelDriftObservation` table (model, source, score name, date, both counts, PSI and status), the source being 1 for an Exhaustive adaptation score and 2 for an Advanced Analytics adaptation score.

Upon a score reaching significant drift, having not been significant at the check before, each user holding permission 6, Read Write Model, in that tenant receives an in-app notification, of high priority, naming the model, the score and the PSI. A score that remains significant from one check to the next does not notify again, and no user is notified of the same score more than once a day. Moderate drift is recorded yet notifies nobody.

The check runs every `WaitModelDrift` milliseconds (86400000, daily, by default), the first check taking place five minutes after the application starts, and reads the archive on the main connection.

| Environment Variable         | Default  | Description                                                        |
|------------------------------|----------|--------------------------------------------------------------------|
| `EnableModelDriftMonitoring` | False    | Whether the job runs.                                              |
| `WaitModelDrift`             | 86400000 | The wait between checks, in milliseconds.                          |
| `ModelDriftRecentDays`       | 7        | The length of the recent window, in days.                          |
| `ModelDriftBaselineDays`     | 28       | The length of the baseline window, in days.                        |
| `ModelDriftMinimumSamples`   | 200      | The scores needed in each window for a check to be made.           |
| `ModelDriftMaximumSamples`   | 50000    | The most scores read for each window, newest first.                |

## What it is not

Drift, as measured here, is of the distribution of the score only. The drift of the input variables is not measured, nor are AUC and calibration against outcomes, and there is no baseline fixed at the point of promotion. A consequence of the moving baseline should be kept in mind: a model that drifts slowly and steadily is compared each week with a baseline that has itself drifted, and may never be reported, and a model that shifted abruptly five weeks ago is by now compared with a baseline made wholly of the shifted scores, and reads Stable again.

It follows also from what is measured that only a model with a score has anything to watch. A model made of rules alone, with no Exhaustive or Advanced Analytics adaptation, is never checked, and neither is a score with fewer than 200 values in either window. On a clean installation, therefore, turning drift monitoring on records nothing, the demonstration transactions carrying no adaptation score, and the model card has no drift history to show.

PSI says that the scores moved. It does not say that the model is wrong, keeping in mind that the population may legitimately have changed. A significant notification should be treated as a prompt to review the model with the [Backtest](../../Configuration/Models/Backtest/index.html) and the case outcomes, and not as a verdict.

## Permissions and security

The notification is sent to holders of permission 6, Read Write Model, within the tenant of the model, and the same permission is required to download the model card on which the drift history appears. Observations are scoped to the tenant of the model.
