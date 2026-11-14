---
layout: default
title: Entity Resolution
nav_order: 19
parent: Concepts
---

# Entity Resolution

Two nodes in the graph can be the same real-world entity: two accounts opened with one national id, or the same customer
name typed two ways. Entity resolution finds such pairs, shows the evidence, and lets an analyst merge them. It is
reversible and never silent.

## Candidates

A candidate (`GraphEntityMatch`) records two nodes, where it came from, an estimated probability that they are the same
entity (the posterior), the evidence as JSON, and a decision.

- **Shared hard identifier.** Two nodes of the same key (two accounts) that are linked through a graph edge definition
  marked **Hard** to the same identifier node (a national id, a card, a phone). The identifier must not be a hub, and no
  more than `GraphEntityMatchMaximumCluster` nodes may share it: an identifier shared by dozens of nodes is too common
  to be evidence. The posterior is 0.999. The evidence names the identifier and how many records share it.
- **Fuzzy comparison.** For the key names in `GraphEntityMatchFuzzyKeys`, node values are folded (case, punctuation and
  accents ignored, using the same folding as sanctions screening) and compared by Fellegi-Sunter weights: an exact
  agreement after folding weighs log2 (m/u) with u taken from how common the value is (term-frequency adjustment), a
  close agreement (Jaro-Winkler or reordered-token similarity at or above 0.92) weighs less, and the posterior comes
  from the prior odds that two nodes match plus the weights. Only candidates at or above
  `GraphEntityMatchMinimumPosterior` are queued. Nodes are only compared when they share a token that is not too common,
  so the work stays bounded. Fuzzy candidates are never confirmed automatically.

Set `EnableGraphEntityResolution` to `True` to run the job (hourly by default). Re-running never duplicates a candidate
or reopens a decided one.

## Deciding

`GET /api/GraphEntityMatch` lists candidates, optionally by decision (1 Pending, 2 Confirmed, 3 Rejected), best first;
`GET /api/GraphEntityMatch/ByNodeId/{id}` lists a node's. `POST /api/GraphEntityMatch/Decide` confirms, rejects, or
undoes (back to pending) with an optional note, and needs the graph resolve permission (51). Candidates touching a case
node the caller cannot read are hidden.

Confirming merges: every node in a group of confirmed pairs gets the same `GraphNode.ResolvedEntityId`, the lowest node
id in the group, and pairs that chain (A with B, B with C) become one entity. Rejecting or undoing takes a node out of
the group again. With `GraphEntityMatchAutoConfirmHard` on, hard-identifier candidates are confirmed by `System` without
an analyst; everything else still needs a person.

<!-- TODO(docs): not built: drawing a resolved entity as one node with a records badge on the graph canvas, the resolution drawer on the graph, an agent tool, re-estimating the weights from analyst decisions, comparing more than one field (name with date of birth and address, which is what lets two soft agreements outweigh one), and a size cap that sends oversized groups to review. Resolved entity ids are stored and returned, so nothing downstream has to wait for the canvas. -->

## The page

Administration > Entity resolution lists the proposed matches, pending by default, with the two nodes, whether they
matched on a shared hard identifier or by fuzzy comparison, and the posterior probability. Confirm merges the two nodes
into one resolved entity and Reject records that they are different people; either takes an optional note. A decided
match stays in the Confirmed or Rejected list, and the filter at the top switches between the three.

<!-- TODO(screenshot): The Entity resolution page listing a pending fuzzy match. -->
