---
layout: default
title: Working Typed Diaries
nav_order: 29
parent: Case Management
grand_parent: Configuration
---

# Working Typed Diaries
A diary on an open case says that something is due by a date. A typed diary also says what it is for, and that decides how the case is ranked: a regulatory clock or a held payment cannot wait behind everything else, so such a case is lifted to the front of the queue until the diary is cleared.

Open the Cases page, choose a case and press Peek. The Diary section at the foot of the peek shows the diary the case has, if any, and lets you set, change or clear it. The date is entered and shown in UTC.

## Types

| Type                | What it is for                                                                                                  | Ranking lane                                                        |
|---------------------|-----------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------|
| Review              | An ordinary reminder to look at the case again.                                                                 | Deadline near within the deadline window, otherwise unchanged.      |
| Customer response   | The date by which the customer is due to answer.                                                                | The same as Review, with the reason naming the customer response.   |
| Regulatory deadline | A mandatory clock, such as a filing deadline.                                                                   | Mandatory, however far away the date is.                            |
| Payment hold        | A payment that has been held and decides at the diary date.                                                     | Live money, ahead of the Mandatory lane.                            |

A diary whose date has passed keeps the case in its lane and marks the deadline as breached, so an overdue regulatory clock or hold stays at the front, and a case that has a breached deadline can be moved on by an escalation rule using the Deadline overdue trigger. The Due column on the Cases page shows the type beside the countdown for a regulatory deadline, payment hold or customer response.

## Rules

* The case must be open. A suspended case already uses its diary to decide when it reopens, so a typed diary cannot be placed on one, and a closed case cannot carry one.
* The case must be visible to you, and not locked to someone else.
* The date may be up to a year in the past, so that a deadline that has already been missed can be logged, and up to ten years ahead.
* Setting the same type and date again changes nothing and records nothing.
* The case ranking is recomputed within one sweep of the case risk worker, and the new assessment is recorded with the trigger Diary or lane threshold.

## The journal

| Event             | Recorded when                                                                                        |
|-------------------|------------------------------------------------------------------------------------------------------|
| Diary             | A diary is set, or its type changes; After holds the type and Before the old type when there was one.|
| Diary Date Change | The date changes; Before and After hold the old and new date.                                        |
| Diary Cleared     | The diary is cleared; Before holds the type that was cleared.                                        |

## Diary expiry on open cases

The expiry automation described under Working Closed, Diary and Suspend moves a suspended case back to Open, or on to Closed, when its diary date passes. It now ignores cases that are already open. Before this change it wrote a Process Expired Case event for every open case with a passed diary date on every pass, which would have filled the journal of every case holding a typed diary past its date.

## Case Diary agent tools

Ask Jooby can set and clear a diary through the same operations, with a confirmation required before either runs. They are held by the Case Lookup and Notes specialist and take the case's Id.
