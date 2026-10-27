---
layout: default
title: Working Quality Review Samples
nav_order: 30
parent: Case Management
grand_parent: Configuration
---

# Working Quality Review Samples
The ranking puts the cases it believes matter in front of analysts first, so the cases it ranks low are the ones it never lets anyone look at. If it is wrong about them, nobody finds out. A quality review sample fixes that by picking a small share of low-priority cases at random and placing them in the queue, so that someone does look, and so that what they find can be counted.

## How a case is picked

The case risk worker decides once for each case, the first time it assesses the case while it is low priority, which means in the Standard lane with a Priority below `CaseRiskQaMaxPriority` (25 by default). It then draws a number for the case that depends only on the case and its workflow, and picks the case when the number is below the sample rate of the workflow:

* The rate is the workflow's Quality review sample rate, or `CaseRiskQaSampleRate` (0.05, five in a hundred) where the workflow leaves it empty. A rate of 0 switches sampling off, and cases are not decided while it is off, so switching it on later still samples them.
* Because the decision is made once, raising or lowering the rate changes only the cases decided afterwards, and a case is never picked twice.
* A case that is urgent, due soon or high priority is never eligible, so the sample never takes attention from the cases that need it.
* Every pick is recorded as a QA Sampled event on the case, in the name of Administrator, with the rate in force.

## Working a sample

A picked case is flagged QA sample on the Cases page, with the reason beside it, and is ordered after the urgent, returned and due-soon cases but ahead of the ordinary Standard cases, so it is reached in the normal course of skimming. The Filters card in the rail has a Quality review filter that lists only the picked cases that are still open. Nothing else about the case changes: it is worked, closed and given an outcome like any other.

## What it tells you

The Quality tab of the Cases page has a Quality review sample section. It counts the picked cases, how many are still open, how many have closed with a confirmed or not confirmed outcome, and the share confirmed. Beside it is the share confirmed among all closed cases with an outcome.

Because the sample was picked without regard to the ranking, its confirmed share is a fair estimate for the low-priority cases the ranking would otherwise never have shown anyone. If it is close to the overall share, a low score is not separating those cases from the rest and the ranking is not doing its job at the bottom of the queue. If it is much lower, a low score means what it says. The section says so when fewer than ten picked cases have closed with an outcome, because a share from so few is not reliable.

This does not correct the ranking quality and calibration charts. Those still reflect only the cases that were worked, and weighting them to remove that bias is not done.
