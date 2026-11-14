---
layout: default
title: Working Similar Cases
nav_order: 36
parent: Case Management
grand_parent: Configuration
---

# Working Similar Cases

When a case opens, the first question is usually whether anything like it has been seen before, and what was decided.
The Similar cases list on the case page answers that. It shows the cases of the same Case Workflow that resemble the
one being viewed, how alike each is, how the closed ones were decided and what the two cases share.

It is context to read. It never changes the likelihood, the priority or the lane of a case, it does not block a
decision, and it creates, links, moves and closes nothing.

## Where it shows

* The page header carries a small badge, for example "12 similar". Choosing it jumps to the list.
* The Similar cases figure at the top of the Case detail views section reads, for example, "5 / 6", meaning five of the
  six similar closed cases were confirmed. Beneath it the earlier cases on the same key are still counted separately.
* The Similar cases list in the same section shows the ten most similar cases, most alike first. Each row has the case
  number, which opens that case, its key value, whether the match is Strong or Moderate, its status or outcome, when it
  closed, and the facts the two cases share.

Fewer than five similar closed cases is too few to judge by, so the page says that instead of quoting a ratio. A case
that has only just been created reads "not compared yet" until the next pass of the sweep, which is within a minute by
default.

## How two cases are compared

Each case is described by a short list of plain facts about itself, called its similarity vector. Nothing is configured:
every field the transactions of the case carry takes part.

| Kind     | The fact                                                                       | What sharing it says              |
|----------|--------------------------------------------------------------------------------|-----------------------------------|
| Rule     | Each Activation Rule that fired visibly on any transaction of the case key     | The same typology fired           |
| Model    | The highest score each model gave, as a band                                   | The same model raised it          |
| Tag      | Each Tag on the case or on its transactions                                    | An analyst called them the same   |
| Entity   | Each value of a payload field that is an Archive Key, such as a device         | The cases share a real thing      |
| Category | Each value of any other text or true or false payload field, such as a channel | The same kind of activity         |
| Measure  | The sum, mean and largest value of each numeric payload field, as size bands   | The same size of activity         |
| Feature  | Each Abstraction Rule, Abstraction Calculation and TTL Counter value, banded   | The same behaviour on the account |
| Band     | The value at stake as a size band, and the lane when it is a mandatory one     | The same seriousness              |

Two cases are similar when they share facts, with three refinements:

* **Rare facts count for more.** A rule that fires on most cases of the workflow says almost nothing when two cases
  share it. A device only three cases have ever used says a great deal. The weight of a fact is worked out from how
  many cases of the workflow carry it.
* **A fact only one case carries is ignored.** A transaction reference or a timestamp never matches anything, so it is
  left out rather than being allowed to drag every comparison down. A field with more than twelve different values
  inside one case is left out for that case for the same reason.
* **One common fact is never a match.** Two cases must share enough weight before they are called similar at all.

For each kind the shared weight is divided by the total weight, and the kinds are averaged, giving a similarity from 0
(nothing shared) to 1 (identical). The page shows it as Strong or Moderate. It is a similarity and not a probability.

A row is marked **Linked** when the two cases share an Entity fact, or when their keys sit in the same community of the
network graph. Linked means the cases touch the same real thing; the other matches only look alike.

## The vector follows the case

While a case is open its vector is rebuilt from every transaction on its key, so a new transaction, a new Activation or
a new Tag changes what it is compared with. When the case is closed the vector is rebuilt one last time and then left
as it stood, so a closed case is always compared as it was when it was decided.

## What is stored

The values of Entity and Category facts are never stored. Only the field name and a keyed hash of the value are, which
is enough to tell that two cases share a value and not enough to read it. The reasons on the page therefore name the
field, for example "Same Device", and not the value. Rule, model and Tag names are stored as they are.

The hash is keyed with the `CaseSimilarityHashingKey` Environment Variable, a long random value that belongs to this
feature alone. It has no default. Until it is set no similarity vector is built and the page shows no similar cases.
Set it once and keep it: a closed case is never rebuilt, so after a change of key the open cases stop matching the
closed ones on Entity and Category facts.

## Who sees what

Reading similar cases needs the Read Write Case permission and the case must be visible to you. Cases are only compared
within one Case Workflow. A similar case is listed, and counted in the figures, only when your role is granted its
current status; the existence of a case you cannot see is never revealed.

AskJooby reaches the same information through the `CaseSimilarityGet` tool, and a case narrative may say how many
earlier cases resembled this one and how they were decided, without identifying them.

The programming interface is `GET /api/CaseSimilarity/{caseId}`.

## Settings

| Environment Variable         | Default | Meaning                                                                        |
|------------------------------|---------|--------------------------------------------------------------------------------|
| EnableCaseSimilarity         | True    | Whether the jobs server keeps the similarity vectors up to date                |
| WaitCaseSimilarity           | 60000   | Milliseconds between passes of the sweep                                       |
| CaseSimilarityRefreshMinutes | 30      | How old the vector of an open case may be before it is rebuilt                 |
| CaseSimilarityWindowDays     | 90      | How many days before the creation of a case its transactions are read from     |
| CaseSimilarityMaximumRows    | 500     | The most transactions read for one case, newest first                          |
| CaseSimilarityMaximumCases   | 500     | The most cases one pass rebuilds                                               |
| CaseSimilarityHashingKey     | null    | The key Entity and Category values are hashed with; nothing is built without it |

The case key of the workflow must be an Archive Key for the transactions of a case to be read. Where it is not, the
vector holds only the Tags and the Band facts.

<!-- TODO(docs): the kind weights and the Strong and Moderate thresholds are fixed defaults that have only been checked on a synthetic world; tune them from a tenant's own closed cases with the leave-one-out harness and consider making them per workflow. -->
<!-- TODO(docs): three payload fields carrying the same number count three times inside the Measure kind; decide whether correlated fields should be collapsed. -->
<!-- TODO(docs): free text (notes, closure reasons) takes no part; an embedding kind is possible if the harness shows it adds anything. -->
<!-- TODO(docs): the sweep notices change by age, so an open case can lag a new transaction by up to CaseSimilarityRefreshMinutes. -->
<!-- TODO(docs): changing CaseSimilarityHashingKey changes every stored hash, so open cases stop matching closed ones on Entity and Category facts; there is no rebuild-all switch yet. -->
<!-- TODO(screenshot): file=case-similar-cases.png route=/cases/{id} state="the Similar cases list with a Strong linked confirmed case and a Moderate open case, and the header badge" -->
