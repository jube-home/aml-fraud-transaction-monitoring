---
layout: default
title: Exhaustive Adaptation Training Promoted Model Performance
nav_order: 4
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive Adaptation Training Promoted Model Performance

This page answers the question that matters before a model is trusted with real transactions: how good is it, and what will it cost. A model is never simply right or wrong. It gives every record a score between 0 and 1, and it is the threshold later applied to that score, by an Activation Rule, that turns the score into a decision; a low threshold catches more of the positive class and troubles more genuine customers, a high threshold does the reverse. The charts described here show, from several directions, how well the scores separate the two classes, and what each choice of threshold gives and takes.

Every figure on the page is computed upon the Testing part of the data, being the 20% of records the model never saw in training, as explained in [Concepts](../Concepts/index.html), and all of it describes the one promoted model that is active.

## Terms, and one example

The example carried through this page is the active promoted model of FraudScore, as carried by a clean installation, examined upon 360 test records of which 180 are positive and 180 negative (the classes having been balanced before training). At a threshold of 0.5 the model scores 174 records at or above the threshold, of which 153 are in truth positive and 21 are not, and 186 beneath it, of which 159 are in truth negative and 27 are positive cases that were missed. Every measure that follows is derived from those four counts, or from repeating the count at other thresholds:

| Term                 | Meaning                                                                                                                                  | In the example               | How to read it                                                                                           |
|----------------------|------------------------------------------------------------------------------------------------------------------------------------------|------------------------------|----------------------------------------------------------------------------------------------------------|
| True positive        | A positive record scored at or above the threshold.                                                                                      | 153                          | The cases caught.                                                                                        |
| False positive       | A negative record scored at or above the threshold.                                                                                      | 21                           | Genuine activity that would be interrupted.                                                              |
| True negative        | A negative record scored beneath the threshold.                                                                                          | 159                          | Genuine activity left alone.                                                                             |
| False negative       | A positive record scored beneath the threshold.                                                                                          | 27                           | The cases missed.                                                                                        |
| Accuracy             | The share of all records decided correctly.                                                                                              | (153 + 159) / 360 = 0.87     | Higher is better, yet misleading where one class is rare, as declaring everything negative is then nearly always correct. |
| Precision            | Of the records flagged, the share that were in truth positive.                                                                           | 153 / 174 = 0.88             | Higher is better. Low precision is wasted effort and troubled customers.                                 |
| Recall               | Of the positive records, the share that were flagged. Also called sensitivity, or the true positive rate.                                | 153 / 180 = 0.85             | Higher is better. Low recall is fraud that passes unnoticed.                                             |
| Specificity          | Of the negative records, the share left alone. One minus specificity is the false positive rate.                                         | 159 / 180 = 0.88             | Higher is better.                                                                                        |
| Balanced accuracy    | The mean of Recall and Specificity.                                                                                                      | 0.87                         | Accuracy that is not flattered by a rare class.                                                          |
| F1                   | A single figure combining Precision and Recall, high only where both are high.                                                           | 0.86                         | From 0 to 1, higher is better.                                                                           |
| MCC and Cohen's kappa | Agreement between the decisions and the truth, allowing for the agreement that chance alone would give.                                 | About 0.73                   | 0 is no better than chance and 1 is perfect.                                                             |
| AUC                  | The Area Under the ROC Curve: the probability that a positive record taken at random is scored above a negative record taken at random.  |                              | 0.5 is chance and 1 is perfect. Above 0.9 is strong. It does not depend upon any threshold.              |
| Gini                 | The AUC restated as twice the AUC less one.                                                                                              |                              | 0 is chance and 1 is perfect.                                                                            |
| KS                   | The Kolmogorov–Smirnov statistic: the widest gap between the share of positives and the share of negatives scored beneath a given score. |                              | 0 is no separation and 1 is total separation.                                                            |
| Average precision    | The area beneath the Precision and recall curve.                                                                                         |                              | Higher is better, and it is the fairer summary where the positive class is rare.                         |
| Brier score and Log loss | How far the scores are from the truth when read as probabilities. The Brier skill score restates the Brier score against always answering with the share of positives. | | Lower is better for Brier score and Log loss. A Brier skill score above 0 beats that naive answer.       |
| Expected calibration error | The average gap between a score and the share of records at that score that proved positive.                                       |                              | Lower is better, and near 0 means that a score can be read as a probability.                             |

Model performance is expressed as a score. The score is the mean of percentage classification accuracy and the Spearman Rank Correlation. There are other means to assess overall model performance however, and upon promotion more comprehensive performance statistics are computed, from the testing data kept back during training, and made available for the promoted model that is active.

## Where it lives

Navigate through the menu to Models, open the model, then the Exhaustive Adaptation hub card, and open the Exhaustive Adaptation. Two of the results sections described in [Training Configuration](../TrainingConfiguration/index.html) are the subject of this page: How well it detects, which presents the performance of the active promoted model, and Which model is live, which keeps a journal of all promoted models.

## How well it detects

The section answers whether the model finds what it should, and at what cost. It is a series of charts, each drawn in the server and each preceded, where there is something to say, by a sentence or two of narrative written from the figures of the chart.

| Chart                | Shows                                                                                                                                                                                                                                                                                                                     | How to read it                                                                                                                                                                                                                                             |
|----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Performance measures | A panel of figures headed by the number of test rows and the share in the positive class. Under Ranking, independent of any threshold: AUC with its 95% interval, Gini, KS, Average precision, Brier score, Brier skill score, Log loss and Expected calibration error. Under At threshold: Accuracy, Balanced accuracy, Precision, Recall, Specificity, F1, MCC and Cohen's kappa. A Calibration group follows for a calibrated model. | The ranking figures describe the model whatever threshold a rule later applies to the score, and are the figures by which two models are fairly compared. The threshold figures describe the decisions that would be made at the threshold stated.       |
| Confusion matrix     | During the testing phase of the training process, the classification predicted vs actual classification organised into a table with all possible states being reflected (true positive, true negative, false positive and false negative).                                                                                 | The diagonal holds the correct decisions. A false positive is a genuine case that would be interrupted, and a false negative is a positive case that would be missed.                                                                                      |
| Score distribution   | The scores of the positive class and of the negative class, each as its share of the class.                                                                                                                                                                                                                               | The further apart the two shapes, the better the model separates the classes. The overlap is where mistakes are made, whatever the threshold.                                                                                                              |
| ROC                  | The Receiver Operating Characteristic curve, which compares false positive rates to true positive rates given different thresholds of classification being applied incrementally, stepped from low to high.                                                                                                                | A curve that bows towards the top left is the stronger model, and the diagonal is no better than chance. The area beneath the curve is the AUC.                                                                                                            |
| Precision and recall | Precision against recall across the thresholds.                                                                                                                                                                                                                                                                           | The more honest picture where the positive class is rare: it shows how much precision must be given up to find more of the positive class.                                                                                                                 |
| Calibration          | The score against the share of cases at that score that proved positive.                                                                                                                                                                                                                                                  | Points lying along the diagonal mean that a score can be read as a probability. The caption states the manner of calibration, or the reason there is none, as described in [Recall](../Recall/index.html).                                                |
| Gains                | The cumulative share of the positive class found as the cases are worked from the highest score downwards.                                                                                                                                                                                                                | How much of the positive class is found in, for example, the top tenth of cases by score, which is the practical question for a team with a fixed capacity.                                                                                                |
| Lift                 | By decile of score, how many times more of the positive class is found than by selecting at random.                                                                                                                                                                                                                       | A lift well above 1 in the first deciles, falling away quickly, is the mark of a model that concentrates the positive class at the top.                                                                                                                    |
| Kolmogorov–Smirnov   | The cumulative share of each class by score.                                                                                                                                                                                                                                                                              | The widest gap between the two curves is the KS statistic, and the score at which it falls is the point of greatest separation between the classes.                                                                                                        |
| Threshold sweep      | Precision, recall, F1 and MCC at every decision threshold, with the threshold in use marked.                                                                                                                                                                                                                              | Where to set the threshold of a rule: the measures trade against each other as the threshold moves, and the chart shows what is given and what is taken.                                                                                                   |
| Value captured       | The cumulative amount of a chosen variable captured as the cases are worked from the highest score downwards. Drawn only once a variable has been chosen as the basis.                                                                                                                                                     | Whether the model finds the cases that matter by value, and not only by number.                                                                                                                                                                            |

In the example, the Confusion matrix holds 153 and 159 on its diagonal and 21 and 27 off it. Gains and Lift are best understood together: were the 360 records sorted from the highest score downwards and only the first 36 worked, a strong model would find nearly all 36 to be positive, which is 20% of the 180 positives found in 10% of the work. The Gains chart would henceforth pass close to the point 10% across and 20% up, where selecting at random would give 10%, and the Lift of the first decile would be close to 2. Keeping in mind that half of these test records are positive, 2 is the most the lift can be here; upon a deployment where 1 in 100 is positive the same model would show a far greater lift, which is why lift is read against the share in the positive class stated at the head of Performance measures. The Threshold sweep, lastly, would show Recall falling and Precision rising as the threshold is moved from 0.5 towards 1, and the point at which the two cross is a reasonable first choice of threshold where neither kind of mistake is the costlier.

The charts are drawn in the order of the table.

### Detection rate basis

Above the charts, Detection rate basis decides what the detection rate is a rate of. Count (default) shows detection rate by row. Choosing a variable shows it by the summed value of that variable instead, across Score distribution, ROC, Gains, Lift, Kolmogorov–Smirnov and Value captured. It follows that, by choosing an amount, it can be seen how much of the value at risk the model captures, rather than how many of the cases. The list offers Count (default) followed by every variable of the Exhaustive Adaptation, the choice redraws the six charts named at once, and it is not remembered upon the page being left. Performance measures, Confusion matrix, Precision and recall, Calibration and Threshold sweep are always by row.

The threshold in use for the figures At threshold, the Confusion matrix and the Threshold sweep is that of the ExhaustiveValidationTestingActivationThreshold Environment Variable, which is 0.5 by default.

{% comment %}SHOT id=exhaustive-performance-measures
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:How well it detects
     frame=section:How well it detects
     state=the top of the How well it detects section of a trained demonstration adaptation, showing the Detection rate basis control set to Count (default), the Performance measures panel with its Ranking and At threshold groups, and the Confusion matrix
     conveys=the performance of the live model is summarised first as figures, split between those independent of a threshold and those at the threshold
     alt=A panel of performance figures including AUC, Gini, KS, precision, recall and F1 grouped by whether they depend upon a threshold, beside a confusion matrix of predicted against actual classification{% endcomment %}

{% comment %}SHOT id=exhaustive-performance-roc-and-distribution
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:How well it detects, scroll-to:Score distribution
     frame=section:How well it detects
     state=the Score distribution and ROC charts side by side, each with its narrative above, for a model with good yet imperfect separation
     conveys=the separation of the classes by score and the ROC curve are two views of the same strength of the model
     alt=A chart of the distribution of scores for the positive and negative class with a modest overlap, beside a ROC curve bowing towards the top left above the diagonal of chance{% endcomment %}

{% comment %}SHOT id=exhaustive-performance-gains-lift-sweep
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:How well it detects, scroll-to:Gains
     frame=section:How well it detects
     state=the Gains, Lift, Kolmogorov–Smirnov and Threshold sweep charts of a trained demonstration adaptation, the Threshold sweep showing the marker of the threshold in use
     conveys=the later charts answer operational questions: how much is found for a given workload, and where the threshold should sit
     alt=Four charts: cumulative gains rising above the diagonal, lift by decile falling from left to right, the cumulative share of each class by score, and precision, recall, F1 and MCC plotted across decision thresholds{% endcomment %}

{% comment %}TODO(verify): the Errors Histogram (the errors between predicted and actual value as a histogram) and the plot of predicted against actual value, which this page formerly listed, are still served by /api/Chart/Exhaustive yet are not among the charts drawn in the Vue section How well it detects. Owner to decide whether they are to be restored to the user interface.{% endcomment %}

## Which model is live

The Promoted models grid keeps a journal of all promoted models, expressing the same as that in the Learning Curve. The table is ordered from the best performing model to the weakest, then on topology complexity (the number of weights in the topology) on the very unlikely total deadlock in model performance.

| Column              | Description                                                                                                              |
|---------------------|--------------------------------------------------------------------------------------------------------------------------|
| Active              | A switch, on for the one promoted model that is live.                                                                    |
| Model type          | Neural Network (evolved topology), or the family of the challenger that was promoted.                                    |
| Created             | The date and time of the promotion.                                                                                      |
| Score               | The score of the model.                                                                                                  |
| Topology complexity | The number of weights in the topology.                                                                                   |
| Calibration         | The manner in which the score was calibrated upon promotion.                                                             |

Above the grid the page states: Only one promoted model is active at a time: activating one deactivates the rest, and deactivating the active one promotes the next best score in its place. The Score is shown to four decimal places. Until a first trial has been promoted the grid states No model has been promoted yet., and where the promoted models cannot be read the page states The promoted models could not be loaded.

In the example, FraudScore having been promoted twice, the grid holds two rows with the score of 0.8002 at the head and its Active switch on, the one beneath it being the earlier, weaker model, of a score of 0.7844, with Active off.

### Prescriptions

The grid is hierarchical and can be expanded upon. Expanding upon a promoted model exposes Prescription by variable, being the selected input variables that have been included in the model.

The variables are presented in the order of most sensitive to least sensitive. The nature of exhaustive exploration is such that all variables will provide for sensitivity, as poorly generalised models will have been discarded by the training algorithm.

The variable statistics are included and contrast to prescription statistics. The prescription statistics are summary statistics having been performed as a consequence of Monte Carlo Model Simulation, for the simulations yielding a positive classification and for those yielding a negative classification:

| Column        | Description                                                                              |
|---------------|------------------------------------------------------------------------------------------|
| Variable      | The name of the variable.                                                                |
| Variable mean | The mean of the variable in the sample.                                                  |
| Variable SD   | The standard deviation of the variable in the sample.                                    |
| Variable min  | The smallest value of the variable in the sample.                                        |
| Variable max  | The largest value of the variable in the sample.                                         |
| Positive mean | The mean of the variable across the simulations classified positive by the model.        |
| Positive SD   | The standard deviation across the simulations classified positive.                       |
| Negative mean | The mean of the variable across the simulations classified negative by the model.        |
| Negative SD   | The standard deviation across the simulations classified negative.                       |
| Sensitivity   | The extent to which the score moves as the variable moves.                               |

By way of example, the sampled data might suggest a Variable mean of 0.8 for Abstraction.CountTransactionsDecline (this is of course 1 in practice), whereas positive classified by the model suggests a Positive mean for Abstraction.CountTransactionsDecline of closer to 6, where the inference is that given positive, Abstraction.CountTransactionsDecline tends to be higher. Prescriptions are to provide some explanatory value to models, given that Neural Networks are often felt to be less explainable.

The figures are shown to three decimal places. Sensitivity is read relative to the other variables of the same model: the variable at the head of the list is the one whose movement moves the score the most, and is the first to try in [Promoted Model Testing](../TrainingPromotedModelTesting/index.html). A Positive mean far from the Variable mean, measured against the Variable SD, marks a variable upon which the model leans; a Positive mean and a Negative mean that are close to one another mark a variable that contributes little taken alone. Where nothing was recorded the grid states No prescription was recorded for this model., and where it cannot be read, The prescription could not be loaded.

A variable can in turn be expanded for two charts: the Prescription histogram, being the frequency of the values of the variable across the simulations, and the Prescription summary, being its summary statistics expressed in standard deviations.

{% comment %}SHOT id=exhaustive-promoted-models-prescription
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:Which model is live, scroll-to:Which model is live, expand-first-row
     frame=section:Which model is live
     state=the Promoted models grid with several promoted models, exactly one having Active on, and the first model expanded to show Prescription by variable with the Variable mean and Positive mean columns visibly differing for at least one variable
     conveys=every promoted model is kept, one is live, and each explains itself by contrasting the sample with what the model classifies as positive
     alt=A grid of promoted models with an Active switch, model type, date, score, topology complexity and calibration, the first expanded to list its variables with the mean in the sample beside the mean where the model classifies positive{% endcomment %}

## Promotion rules

It is possible to reject or deactivate a model via the Active switch in each row of the grid. Only one promoted model of an Exhaustive Adaptation is active at a time. Activating one deactivates the rest, and deactivating the active one activates the promoted model with the next best score, such that the model so deactivated is no longer considered as promoted. Upon deactivation the performance of that model is no longer presented in How well it detects, nor is it the model tested in What if…, and instead the next highest performing model is presented. A promoted model that has been deleted, or whose Exhaustive Adaptation has been deleted, cannot be activated: the request is answered as not found.

The active promoted model decides what an Exhaustive Adaptation scores, so it can only be changed while the Exhaustive Adaptation is switched off. While it is live (active, or approved as active) a change is refused with a conflict and nothing is changed, the reason being given above the grid as: This Exhaustive adaptation is live. Switch it off, change the promoted model and switch it on again through approval. It follows that the Exhaustive Adaptation is switched off, the promoted model changed, and the Exhaustive Adaptation switched on again, each step going through approval. Asking for the state a promoted model already has is accepted and does nothing. A training run started from an approved Exhaustive Adaptation still promotes its own best model when it finishes.

{% comment %}TODO(verify): owner to confirm that there is to be no emergency rollback of a live Exhaustive Adaptation without a checker (the same trade-off as Advanced Analytics promotion).{% endcomment %}

## Permissions and security

Reading the performance charts and the Promoted models requires permission 16, Read Write Exhaustive. Changing the active promoted model requires permission 14. The Active switch is presented to every user who can read the grid, yet it is disabled for a user without permission 14, and the server refuses such a user with You do not have permission to change promoted models. Any other failure is given as The promoted model could not be updated.

{% comment %}TODO(verify): permission 14 is named Read Write Abstraction Calculation in the permission catalogue, so changing the active promoted model is at present governed by the Abstraction Calculation permission (ExhaustiveSearchInstancePromotedTrialInstanceService.writePermissions). Owner to decide whether 14 is intended.{% endcomment %}
