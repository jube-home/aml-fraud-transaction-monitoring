---
layout: default
title: Graph
nav_order: 12
has_children: true
parent: Configuration
---

# Graph

A rule looks at one transaction, and at the history of one key, such as an account. Fraud and money laundering are however seldom the work of one account: a mule account is one of several operated from the same telephone, the proceeds of a scam leave through three accounts in an afternoon, and a customer who appears unremarkable shares a card with a customer whose case was closed as fraud last month. None of this can be seen by looking at the account alone, as it is a property of the relationships between accounts.

Graph, which is to say network analysis, exists to answer the questions that follow from this, in the manner an investigator would ask them:

* Who else is this account connected to, and by what: a device, an email address, a card, a payment?
* Is anything nearby already known to be bad, and how near?
* Where did the money come from, and where did it go next?
* Is this a recognisable shape of behaviour, such as money scattered through several accounts and gathered again?

To answer them, Jube derives a network from the transactions it has already stored. Nothing further is sent by the integrating system, and no separate graph database is installed: an administrator states which fields of the transaction are to be treated as things (an account, a device) and which pairs of fields are to be treated as links between them, and the network is thereafter built and kept up to date in the background.

## How the pages fit together

The pages are best read in the order given, as each relies upon the terms introduced by the one before:

| Page                                                  | For whom                    | What it covers                                                                                                                                    |
|-------------------------------------------------------|-----------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| [Graph Concepts](Concepts/index.html)                 | Everyone                    | What a graph is, and every term used thereafter (node, edge, hop, hub, weight, motif and so on), carried through one worked example.              |
| [Graph Registry](GraphRegistry/index.html)            | Administrators              | The configuration of a graph and of its edges under Administration, Graph setup, and the pointing of a Case Workflow at a graph.                  |
| [Exploring a Graph](ExploringAGraph/index.html)       | Analysts and investigators  | The Graph page: every control of the explorer, its states, its limits and its permissions.                                                        |
| [Graph on a Case](GraphOnACase/index.html)            | Analysts                    | The Network section of the case page, and the manner in which it works together with the Case Key Journal.                                        |

Two further pages describe features that rely upon the same network, and are not repeated here:

* [Graph Recall](../Models/GraphRecall/index.html) makes the facts the network holds about a value (how near it is to confirmed fraud, whether it is a hub, the patterns it belongs to) available to rules and to the response at the time a transaction is processed.
* [Entity Resolution](../../Concepts/EntityResolution/index.html) finds pairs of nodes that may be one and the same real-world entity, and allows an analyst to merge them.

## The demonstration network

A clean installation is seeded with a small network, named Demonstration payments network, built around an account ACC-441 that shares a device with three other accounts and passes funds onwards through a chain of accounts to an exchange. Every page of this section uses that network for its examples, so that each statement can be followed on a fresh installation. The network is described in full in [Graph Concepts](Concepts/index.html).

The demonstration network is seeded only where the installation had no transactions stored at the time of the migration, and where the default model and its Case Workflow exist.

The network is seeded as transactions dated from the day of installation, and is built from them by the background jobs in the first minutes after the application is first started. It follows that the examples read as written on a fresh installation, and that on one that has aged by some weeks the transactions have begun to leave the 30 days the explorer opens on. What to expect in each event is set out under What to expect of it on screen in [Graph Concepts](Concepts/index.html).

## The questions, and where each is answered

For a reader arriving with a question in place of a term, the table that follows gives the control that answers it. Each is on the Graph page and in the Network section of a case alike, the rail excepted, which is on the Graph page only.

| Question                                                     | Control                                 | Read                                                                                    |
|--------------------------------------------------------------|-----------------------------------------|-----------------------------------------------------------------------------------------|
| What is this account connected to?                           | Draw, at 1 hop and then 2 hops          | Shapes for the kinds of thing, lines for the links.                                     |
| Which of all this matters most?                              | Relevance                               | The nodes outlined, and Interest in the Selected node panel, 1 being the seed.          |
| Is anything nearby known to be bad?                          | Select a node                           | The star on a node, and Hops to confirmed, the lower the more pressing.                 |
| How exactly are these two connected?                         | Paths to…                               | The fewest hops first.                                                                  |
| Where did the money go, or come from?                        | Follow the money, Forward or Backward   | The tranches, step by step, with Pass-through against an account that passed it on.     |
| What is everything linked to this one node, exactly?         | Explode                                 | One row for each counterpart, with counts, totals and dates.                            |
| In what order did it happen?                                 | The time strip, Play                    | The links appearing in the order they were first seen.                                  |
| Is this a known shape of laundering?                         | The Patterns tab of the rail            | The kind of pattern and its score.                                                      |
| Why is a public wifi or a supermarket not joining everybody? | Show hubs, and the Hubs tab of the rail | The count of hubs hidden.                                                               |
