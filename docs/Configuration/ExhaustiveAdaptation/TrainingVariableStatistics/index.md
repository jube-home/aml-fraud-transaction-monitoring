---
layout: default
title: Exhaustive Adaptation Training Variable Statistics
nav_order: 3
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive Adaptation Training Variable Statistics

The variable statistics answer two questions that are asked before any model is looked at: what does the data look like, and which of the variables tell the positive class from the negative. A variable, as introduced in [Concepts](../Concepts/index.html), is one numeric input to the model, for example the count of declined transactions in a day, and the statistics describe each variable on its own: its typical value, how widely it spreads, how it is shaped, and how strongly it moves with the class.

One of the first tasks in training a model in Exhaustive is the process of establishing statistics. Statistics are important for each variable as they are used in Z Score normalisation of variables, which is an important concept in Machine Learning generally. The statistics are however comprehensively recorded, which provides rich insight about the dataset and problem domain more generally.

Keep in mind that statistics are calculated after the initial sample, but before class filtering, which means that the statistics are a representation of the domain at large, rather than the positive class.

## Where it lives

Navigate through the menu to Models, open the model, then the Exhaustive Adaptation hub card, and open the Exhaustive Adaptation. The statistics are presented under the heading Variable statistics in the results section What it learned, which is available once correlation analysis has finished, as described in [Training Configuration](../TrainingConfiguration/index.html).

## The variables grid

The grid has a row for each variable, 25 to a page, with a Search variables box that narrows the rows by the name of the variable. A string field of the payload that has been exploded into indicator variables is listed once for each of its values, in the form of the field and the value (for example Payload.Channel = Web).

| Column             | Description                                                                                                                                              |
|--------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| Variable           | The name of the variable, prefixed by its origin (for example Abstraction.CountTransactionsDecline).                                                     |
| Correlation        | The Spearman Rank Correlation between the variable and the class, coloured as set out under Colour of correlation.                                       |
| Mean               | The mean of the variable across the sample.                                                                                                              |
| Standard deviation | The standard deviation of the variable, which together with the mean is the basis of Z Score normalisation.                                              |
| Minimum            | The smallest value in the sample.                                                                                                                        |
| Maximum            | The largest value in the sample.                                                                                                                         |
| Kurtosis           | The weight of the tails of the distribution, a high value suggesting that extreme values are a feature of the variable.                                  |
| Skewness           | The asymmetry of the distribution, a positive value suggesting a long tail of larger values, as is typical of counts and amounts.                        |
| IQR                | The interquartile range, being the spread of the middle half of the values, which is not disturbed by extreme values in the manner of the standard deviation. |
| Distinct values    | The number of distinct values, where a value of 2 suggests a binary variable.                                                                            |
| Normalisation      | The type of normalisation applied to the variable. Continuous variables are normalised by Z Score, and binary variables are not.                         |

The figures are shown to three decimal places, save for Distinct values, which is a whole number. The grid presents 25 variables to a page, with a pager beneath it for moving between the pages, and a variable may also be reached by typing part of its name in Search variables, the search being made across every variable and not only those of the page shown.

Where no variable was recorded the grid states No variables were recorded., where the search matches nothing the grid is empty, and where the variables cannot be read the page states The variables could not be loaded.

The Correlation is read by its sign and its size. A positive figure means the variable tends to be higher for the positive class, a negative figure that it tends to be lower, and a figure near zero that the variable, taken alone, says little. The size runs from 0 to 1 either side of zero. In the example of FraudScore, the Correlation of 0.352 for Abstraction.CountTransactionsDeclined1Day says that declined transactions are moderately more frequent among the records tagged as fraud, which accords with intuition, whereas a figure above 0.9, shown in red, ought to prompt the question of whether the variable is in effect the class itself under another name, and would henceforth not be available at the moment a real transaction is scored.

{% comment %}TODO(verify): the page formerly stated that the variables are presented in the order of their correlation with the class descending (strongest first), whereas GetExhaustiveSearchInstanceVariableQuery orders by Id. Confirm on a running system the order in which the grid presents the variables, and whether clicking the Correlation heading sorts, then state it here.{% endcomment %}

{% comment %}SHOT id=exhaustive-variable-statistics-grid
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:What it learned
     frame=section:What it learned
     state=the Variable statistics grid of a trained demonstration adaptation, showing the Search variables box, the Counts instead of share of class switch and a page of variables with the Correlation column carrying red, orange and green badges
     conveys=each variable carries a full set of summary statistics, and the strength of its association with the class is visible at a glance
     alt=A grid of training variables listing for each its correlation with the class as a coloured badge, mean, standard deviation, minimum, maximum, kurtosis, skewness, interquartile range, distinct values and normalisation{% endcomment %}

## The detail of a variable

The grid is hierarchical and each row may be expanded upon, by the control at the start of the row, which reads Show summary and, once expanded, Hide summary. The expansion of the row presents four charts for the variable, above an analysis of the extent to which the variable relates to the others. More than one row can be expanded at a time.

Two terms are needed for the charts. A bin is one of the equal ranges into which the values of the variable are divided, the height of a bar being the number, or the share, of records whose value falls within that range. The model profile is the set of simulated records that the active promoted model scored as positive in the Monte Carlo simulation described in [Concepts](../Concepts/index.html), which is to say what the model has come to regard as a positive case.

| Chart                 | Shows                                                                                                                                                                                                                                                                                                              | How to read it                                                                                                                                                                                                                                   |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Histogram             | The frequency of the values of the variable across the sample, by bin.                                                                                                                                                                                                                                             | The shape of the variable in the domain at large: where the values concentrate, and how long the tail is.                                                                                                                                        |
| Share of each class   | The Positive class and the Negative class, binned on the same bins, each shown as its share of the class. Positive means rows matching the class definition, or the rows flagged by the anomaly model, and Negative is everything else in the sample.                                                             | Where the two classes sit apart, the variable discriminates. Where the two shapes lie on top of one another, the variable alone says little about the class.                                                                                     |
| Monte Carlo           | The model profile, being the values for which the promoted model scores above 0.5 over simulated inputs, drawn as a density against the classes on a shared axis.                                                                                                                                                  | Whether the model has learned the positive class as it is found in the data: a model profile lying over the Positive class is a model that agrees with the sample.                                                                               |
| Monte Carlo summary   | The Mean, Standard deviation, Minimum, Maximum and Interquartile range, each expressed in standard deviations of all rows, compared between the classes and the model profile.                                                                                                                                     | A statistic far from zero for the Positive class or the model profile shows how far, and in which direction, a positive case departs from the ordinary.                                                                                          |

The switch Counts instead of share of class, above the grid, redraws Share of each class and Monte Carlo as counts by bin. Keep in mind that the positive class is ordinarily far smaller than the negative, which is why the share of the class is the default: as counts, the positive class may be barely visible.

The Monte Carlo charts are empty for an Exhaustive Adaptation having no promoted model, and for a variable that the promoted model does not use. For an Exhaustive Adaptation trained before the histograms of each class were kept, the classes were binned separately, and are henceforth shown as densities.

{% comment %}SHOT id=exhaustive-variable-statistics-detail
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:What it learned, scroll-to:What it learned, expand-first-row
     frame=section:What it learned
     state=a variable of the demonstration adaptation expanded, showing the Histogram, Share of each class, Monte Carlo and Monte Carlo summary charts, the positive and negative classes visibly apart, and the first rows of Most correlated with beneath
     conveys=one expansion shows how a variable is distributed, how it differs by class and what the model made of it
     alt=Four charts for one training variable: a histogram of its values, the share of the positive and negative class by bin, the model profile against the classes, and summary statistics in standard deviations, above a list of the variables most correlated with it{% endcomment %}

### Multi-collinearity

Beneath the charts, the grid Most correlated with returns, for each other variable, the correlation to the variable expanded. The variables are ordered by absolute correlation, with strongest correlations first, and where there is nothing to show it states No other variable is correlated with this one.

### Colour of correlation

Colour formatting exists, in the Correlation column of both grids and upon the absolute value of the correlation, on the following basis:

* Red > 0.9.
* Orange > 0.7.
* Else Green.

The colour is a warning of strength and not a judgement of merit. In the grid Most correlated with, red means that the two variables carry very nearly the same information, orange that they overlap substantially, and green that each has something of its own to say. Two Abstraction Rules counting the same transactions over one day and over two days would, by way of example, be expected to appear in red against one another.

Understanding the relationships between variables gives opportunity to collapse some variables into each other, for example by updating Abstraction Rules.

## Permissions and security

Reading the variable statistics and their charts requires permission 16, Read Write Exhaustive, and everything returned is limited to the tenant of the user.
