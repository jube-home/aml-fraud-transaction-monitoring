---
layout: default
title: Diff Tool
nav_order: 8
parent: Ask Jooby
---

# DiffTool: offloading line-by-line comparison to code, not inference

`DiffTool.DiffText` (`Jube.AskJooby/Tools/Diff/DiffTool.cs`) is a small, stateless utility tool with one job: diff
two blocks of text line by line and hand back only the lines that changed, as JSON, so that a model never has to
paste both versions into its own reasoning and work out what changed by eye. The tool's own description states the
reason for existing plainly: "that costs real inference and this costs none." It is available to nearly every
agent (added inside `BasicResearchTools`, `Jube.AskJooby/Agents/Basic/AgentDefinitions.cs`) and, separately, to the
validator agent ("Jadek") specifically (`ValidatorTools`), which is exactly the kind of caller that would otherwise
need to compare a draft reply against a revised one by reading both in full.

## What it returns

`DiffText(oldText, newText)` returns a JSON array of `DiffHunk` records, one per changed line, never the unchanged
lines around them:

* **`"replaced"`** -- an old line paired with the new line it became (`textOld` and `textNew` both set).
* **`"deleted"`** -- `textOld` only; the line is gone.
* **`"inserted"`** -- `textNew` only; the line is new.
* **`"truncated"`** -- a final entry, present only when there were too many changed lines to list in full (see
  below), carrying a `message` rather than any text.

`lineOld`/`lineNew` are 1-based line numbers in whichever document still has that line, `null` on the side that
does not. Two identical inputs return an empty array, `[]`, not a special "no changes" sentinel.

## Exact comparison, with two deliberate accommodations

Comparison is exact -- whitespace and case both count as a change -- with exactly two things normalised before
diffing, both there specifically to avoid a false positive a line-based diff would otherwise report:

* **A single trailing newline on either input is stripped first** (`StripSingleTrailingNewline`), so that two
  texts differing only in whether the file ends with a final newline are not reported as a phantom last-line
  change.
* **Pairing within a changed block is positional, not by matching similar content** -- the tool's own description
  is explicit that this is "same as any line-based diff (git included)": if 2 lines become 5, the first 2 pair as
  `"replaced"` and the remaining 3 are reported as `"inserted"`, even if, read by a person, one of the 3 new lines
  looks more similar to one of the 2 old lines than its positional pairing suggests. This is a property of the
  underlying library (`DiffPlex`'s `SideBySideDiffBuilder`), surfaced faithfully rather than hidden, so a caller
  reading the JSON is not misled about how the pairing was decided.

## Limits, and what happens at them

Two independent limits exist purely to keep one call bounded:

* **Input size** -- each input text is capped at `MaxInputCharacters` (200,000 characters); over that, the call is
  refused outright with an instruction to narrow the comparison to the section that actually matters, rather than
  diffing a truncated, misleading slice of either text.
* **Hunk count** -- once 500 changed lines (`MaxHunks`) have been collected, the loop stops and appends a single
  `"truncated"` entry rather than continuing to enumerate. The tool's own description frames stopping here as
  informative in its own right: hitting this ceiling is "evidence that the two inputs are largely unrelated rather
  than close revisions of each other," not merely a size problem to work around.

## Scope

`DiffText` is a `public static` method on a `static class` with no constructor dependencies and no I/O -- it is
wired directly with `AIFunctionFactory.Create(DiffTool.DiffText)` wherever it is added, unlike every other tool on
this page, which is constructed from an interface-backed client first. There is nothing behind it to configure, and
nothing it reads or writes beyond the two strings it is given.
