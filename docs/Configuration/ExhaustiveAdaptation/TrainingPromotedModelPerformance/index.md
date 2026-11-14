---
layout: default
title: Exhaustive Adaptation Training Promoted Model Performance
nav_order: 4
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive Adaptation Training Promoted Model Performance

<!-- TODO(docs): this page predates the Vue port and describes the Razor layout (tabs, grids, buttons); re-read it against the Exhaustive pages under Models and correct names, positions and labels. -->
Model performance is expressed as a score. The score is the mean of percentage classification accuracy and the Spearman
Rank Correlation. There are other means to assess overall model performance however. Model promotion statistics are
computed to make available more comprehensive performance statistics via the Promoted Model Performance tab:

<!-- TODO(screenshot): file=model-exhaustive-location-of-performance-tab.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image LocationOfPerformanceTab.png with the Vue Exhaustive page" -->
![LocationOfPerformanceTab](model-exhaustive-location-of-performance-tab.png)

Clicking on the Promoted Performance Model tab will expose several performance measures:

<!-- TODO(screenshot): file=model-exhaustive-exposed-performance-tab.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image ExposedPerformanceTab.png with the Vue Exhaustive page" -->
![ExposedPerformanceTab](model-exhaustive-exposed-performance-tab.png)

The performance measures are as follows:

| Performance Measure         | Description                                                                                                                                                                                                                                 |
|-----------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Errors Histogram            | During the testing phase of the training process,  the errors made between the predicated value and actual value plotted as a histogram.                                                                                                    |
| Errors Histogram            | During the testing phase of the training process,  the errors made between the predicated value and actual value plotted as a histogram.                                                                                                    |
| Errors Histogram            | During the testing phase of the training process,  the errors made between the predicated value and actual value plotted as a scatter plot.                                                                                                 |
| Confusion Matrix            | During the testing phase of the training process,  the classification predicted vs actual classification organised into a table with all possible states being reflected (true positive, true negative, false positive and false negative). |
| Receiver Optimisation Curve | A curve that compares false positive rates to true positive rates given different thresholds of classification being applied  incrementally,  stepped low to high thresholds increasing by a value specified in the Environment Variables.  |

The Promoted Models tab keeps a journal of all promoted models:

<!-- TODO(screenshot): file=model-exhaustive-location-of-promoted-models-tab.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image LocationOfPromotedModelsTab.png with the Vue Exhaustive page" -->
![Image](model-exhaustive-location-of-promoted-models-tab.png)

Clicking on the tab exposes more details about the models that have been promoted, expressing the same as that in the
Learning Curve:

<!-- TODO(screenshot): file=model-exhaustive-table-of-promoted-models.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image TableOfPromotedModels.png with the Vue Exhaustive page" -->
![Image](model-exhaustive-table-of-promoted-models.png)

The table is ordered from the best performing model to the weakest, then on topology complexity (the number of weights
in the topology) on the very unlikely total deadlock in model performance.

It can be noted that the grid is hierarchical and can be expanded upon:

<!-- TODO(screenshot): file=model-exhaustive-note-expand-grid.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image NoteExpandGrid.png with the Vue Exhaustive page" -->
![NoteExpandGrid.png](model-exhaustive-note-expand-grid.png)

Expanding upon the grid exposes the selected input variables variables that have been included in the model:

<!-- TODO(screenshot): file=model-exhaustive-variables-selected-for-model.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image VariablesSelectedForModel.png with the Vue Exhaustive page" -->
![Image](model-exhaustive-variables-selected-for-model.png)

The variables are presented in the order of most sensitive to least sensitive. The nature of exhaustive exploration is
such that all variables will provide for sensitivity, as poorly generalised models will have been discarded by the
training algorithm.

The variable statistics are included and contrast to prescription statistics. The prescription statistics are summary
statistics having been performed as a consequence of Monte Carlo Model Simulation yielding a positive classification:

<!-- TODO(screenshot): file=model-exhaustive-contrast.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image Contrast.png with the Vue Exhaustive page" -->
![Contrast](model-exhaustive-contrast.png)

In the above example, it can be observed that the sampled data suggests 0.8 Abstraction.CountTransactionsDecline (this
is of course 1 in practice), whereas positive classified by the model suggests a Abstraction.CountTransactionsDecline of
closer to 6, where the inference is that given positive, Abstraction.CountTransactionsDecline tends to be higher.
Prescriptions are to provide some explanatory value to models, given that Neural Networks are often felt to be less
explainable.

It is possible to reject or deactivate a model via the switch in each row of the grid:

<!-- TODO(screenshot): file=model-exhaustive-location-of-active-switch.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image LocationOfActiveSwitch.png with the Vue Exhaustive page" -->
![LocationOfActiveSwitch.png](model-exhaustive-location-of-active-switch.png)

Selecting the switch to false will mean that this model is no longer considered as promoted:

<!-- TODO(screenshot): file=model-exhaustive-deactivated.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image Deactivated.png with the Vue Exhaustive page" -->
![Deactivated.png](model-exhaustive-deactivated.png)

Upon deactivation, and upon clicking the refresh button, the model performance will no longer be available in the
Promoted Model Performance tab nor Promoted Model Testing tab, instead, the next highest performing model will be
presented:

<!-- TODO(screenshot): file=model-exhaustive-many-deselected.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image ManyDeselected.png with the Vue Exhaustive page" -->
![ManyDeselected.png](model-exhaustive-many-deselected.png)

<!-- TODO(screenshot): file=model-exhaustive-next-model-performance.png route=/models state="TrainingPromotedModelPerformance: replace the Razor-era image NextModelPerformance.png with the Vue Exhaustive page" -->
![Image](model-exhaustive-next-model-performance.png)

## Promotion rules

Only one promoted model of an Exhaustive Adaptation is active at a time. Activating one deactivates the rest, and
deactivating the active one activates the promoted model with the next best score. A promoted model that has been
deleted, or whose Exhaustive Adaptation has been deleted, cannot be activated: the request is answered as not found.
Changing the active model needs permission 14.

The active promoted model decides what an Exhaustive Adaptation scores, so it can only be changed while the Exhaustive
Adaptation is switched off. While it is live (active, or approved as active) a change is refused with a conflict and
nothing is changed: switch the Exhaustive Adaptation off, change the promoted model, then switch it on again, each step
going through approval. Asking for the state a promoted model already has is accepted and does nothing. A training run
started from an approved Exhaustive Adaptation still promotes its own best model when it finishes.

<!-- TODO(docs): confirm with the owner that there is to be no emergency rollback of a live Exhaustive Adaptation without a checker (the same trade-off as Advanced Analytics promotion). -->

<!-- TODO(docs): confirm the permission number and the next-best-score rule against the Vue promoted models panel. -->

