---
layout: default
title: Exhaustive Adaptation Training Promoted Models Performance
nav_order: 5
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive Adaptation Training Promoted Models Performance

<!-- TODO(docs): this page predates the Vue port and describes the Razor layout (tabs, grids, buttons); re-read it against the Exhaustive pages under Models and correct names, positions and labels. -->
Promoted models can be manually tested via the Promoted Models Testing tab:

<!-- TODO(screenshot): file=model-exhaustive-location-of-testing.png route=/models state="TrainingPromotedModelTesting: replace the Razor-era image LocationOfTesting.png with the Vue Exhaustive page" -->
![LocationOfTesting](model-exhaustive-location-of-testing.png)

Clicking on the tab will present the model variables as sliders:

<!-- TODO(screenshot): file=model-exhaustive-sliders.png route=/models state="TrainingPromotedModelTesting: replace the Razor-era image Sliders.png with the Vue Exhaustive page" -->
![Sliders](model-exhaustive-sliders.png)

The sliders are set to their optimal prescription values for the purpose of classifying positive:

<!-- TODO(screenshot): file=model-exhaustive-default-values.png route=/models state="TrainingPromotedModelTesting: replace the Razor-era image DefaultValues.png with the Vue Exhaustive page" -->
![DefaultValues.png](model-exhaustive-default-values.png)

Moving the sliders will present the collated values of all sliders to the model to return a score:

<!-- TODO(screenshot): file=model-exhaustive-score.png route=/models state="TrainingPromotedModelTesting: replace the Razor-era image Score.png with the Vue Exhaustive page" -->
![Score.png](model-exhaustive-score.png)

The higher the score, the more likely the simulation is a positive case (e.g. fraud).

The intention of the manual simulation is to process several intuitive scenarios and observe the score to change equally
intuitively, although keeping in mind that the model is largely bias to anomaly.

Given a score behaving counter intuitively or not flexibly enough, it can of be deactivated, and the next highest
priority score will be considered promoted.