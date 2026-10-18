---
layout: default
title: Hypothesis Tests Catalogue
nav_order: 4
parent: Reference
grand_parent: Analytical Methodology
---

# Hypothesis tests catalogue

The product's semantic tool layer for classical and non-parametric inference: every test tool returns a test name,
statistic and its name, p-value, degrees of freedom, the tail used (two-sided, greater or less), alpha, whether the
null is rejected, the point estimate and null value, an effect size with its name, a two-sided confidence interval at
1-alpha, the sample sizes, warnings and a one-sentence interpretation built from the numbers. Chi-square, F, Levene,
Bartlett and normality tests report the upper (or, for Shapiro-Wilk, lower) tail, because their reference
distributions are only tested in that one direction. Confidence intervals on a one-sided test are still the two-sided
interval at 1-alpha, computed from a two-sided companion test.

## Tools

Every tool name below is documented individually, with its exact signature, in
[the tool catalogue](ToolCatalogue/HypothesisTests.md).

Means and variances: one- and two-sample t-tests (Welch by default, pooled optional, and from-summary variants),
paired t-test, one- and two-sample z-tests, one- and two-proportion z-tests, the exact binomial test, two-variance F
test, Levene's test, Bartlett's test, one-way and two-way ANOVA, Grubbs' outlier test, and effect size for two
samples or from summary statistics.

Ranks and distributions: Mann-Whitney U, Wilcoxon signed-rank (one-sample and paired), the sign test (one-sample,
paired and from counts), one- and two-sample Kolmogorov-Smirnov, Shapiro-Wilk, Anderson-Darling, Lilliefors and
Jarque-Bera normality tests, a chi-square distribution fit test, and the Kruskal-Wallis test.

Categorical and agreement: chi-square goodness of fit and independence, Fisher's exact test, McNemar's test, the
Bowker symmetry test, Stuart-Maxwell and Bhapkar homogeneity tests, Cohen's kappa and weighted kappa with their
comparison and homogeneity tests, and AUC significance and comparison tests.

Power, intervals and corrections: power analysis for one- and two-sample t- and z-tests, confidence intervals for a
mean, proportion, variance or median, Bonferroni, Holm and Benjamini-Hochberg p-value adjustment, and conversions
between a p-value and a test statistic or critical value.

Selection: a tool that recommends the right hypothesis test for a stated analysis goal and data shape.

## Correctness judgement calls

Several of these tools correct or work around real defects or non-standard conventions in the general-purpose
statistics library they are built on, against the textbook definition of each test:

- The one-sample sign test uses the tail directly, rather than the library's own inverted-tail convention, which
  otherwise gives a *greater* median the *lower* binomial tail — the library's own documented example reports p = 0.99
  for a sample with 5 of 19 values above the median under that convention, where the exact binomial value is 0.03178.
- The two-sample power analysis for the z-test uses the two-sample sample-size formula, matching the textbook value of
  63 per group for an effect size of 0.5 at 80 percent power; the library applies the one-sample formula here instead,
  which disagrees with its own power calculation by a factor of two.
- The two-way ANOVA enumeration in the library mislabels its models against the textbook: what it calls the
  mixed-effects model is the fixed-effects model, and vice versa. The tool exposes the textbook names mapped onto the
  correct underlying values rather than the library's own, swapped, labels.
- The library's ROC area statistic is discretised and flipped above 0.5; the AUC tools use the exact placement-value
  statistic (the mean of the positive class's pseudo-accuracies) and the library's own DeLong variance instead.
- The library has no Kruskal-Wallis test, Jarque-Bera test, proportion, variance or median confidence interval, or
  p-value adjustment methods. Each is a small, independently tested composition of the library's own ranks, moments
  and distributions (chi-square, binomial, beta, normal) plus the standard textbook formula.
- Lilliefors p-values are Monte Carlo estimated and can differ in the third decimal between calls.

## Not covered

No implementation exists for repeated-measures ANOVA, the Friedman test, Cochran's Q, Hotelling's T-squared, the
Ljung-Box test, or a dedicated correlation-significance test.
