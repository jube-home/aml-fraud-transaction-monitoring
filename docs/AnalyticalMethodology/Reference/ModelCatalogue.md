---
layout: default
title: Model Catalogue Decisions
nav_order: 3
parent: Reference
grand_parent: Analytical Methodology
---

# Model catalogue decisions: the Bayesian classifier family

A record of why the product's Bayesian-network-style classifiers are built the way they are, from a research pass over
the academic literature on Bayesian network classifiers (Friedman, Geiger and Goldszmidt, "Bayesian Network
Classifiers", 1997; Webb, Boughton and Wang on AODE, 2005; Sahami on k-dependence classifiers, 1996; Jiang, Zhang and
Cai on hidden naive Bayes, 2009).

## General Bayesian network structure search: dropped

A fully general Bayesian network with unrestricted structure search over an arbitrary set of nodes was considered and
explicitly dropped: **"General Bayesian network with structure search... Drop: KDB, TAN, AODE and HNB give most of the
benefit"** of Bayesian-network-style reasoning for the classification use case the product actually needed, at a
fraction of the structure-search cost. Tree-Augmented Naive Bayes (TAN) is the one structure-learning algorithm the
product ships; k-dependence Bayesian classifiers (KDB), Averaged One-Dependence Estimators (AODE) and Hidden Naive
Bayes (HNB) were researched in the same pass but not built, on the same reasoning: each adds a bounded amount of
inter-feature dependence over plain Naive Bayes without the cost or fragility of a general structure search.

## Hierarchical Latent Tree Model (HLTM): researched, not built

A Hierarchical Latent Tree Model classifier — a latent tree structure search plus EM, the largest of the researched
additions — was considered and not built: not present in any general-purpose statistics or machine-learning
library consulted for the catalogue, needing latent tree structure search plus expectation-maximisation, which makes
it substantially more effort than TAN or the other dependence-bounded classifiers above.
