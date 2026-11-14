---
layout: default
title: Backtest
nav_order: 24
parent: Models
grand_parent: Configuration
---

# Backtest

Under the form on the **Activation Rules** page are three sections that open when clicked. Each is shown only to users
with the permission it needs.

<!-- TODO(screenshot): file=rule-authoring-backtest-panel.png route=/models/activation-rules state="Activation rule open, the three sections collapsed under the form" -->

## Backtest

<!-- TODO(screenshot): file=rule-authoring-backtest-form.png route=/models/activation-rules state="Backtest section open: both builders, window, limit, examples, Run backtest" -->
<!-- TODO(screenshot): file=rule-authoring-backtest-instances.png route=/models/activation-rules state="Table of backtests with a Running row (progress, Stop) and a Succeeded row" -->
<!-- TODO(screenshot): file=rule-authoring-backtest-result.png route=/models/activation-rules state="Result of a Succeeded backtest: funnel, confusion matrix, rates, tags, examples" -->
<!-- TODO(screenshot): file=rule-authoring-backtest-explain.png route=/models/activation-rules state="Explain this transaction open on a false positive" -->

Needs the **Run Backtest** permission (permission 61). A backtest runs the rule, as it is in the editor (saved or not),
over archived
transactions and shows how it would have done.

1. **Which transactions**: the same query builder as the reprocessing page. Leave it empty to use every transaction.
2. **What counts as positive**: a second query builder with the model's tags (`Tag.Fraud` and so on) as well as every
   value an activation rule can see. The first tag is chosen for you.
3. Choose the window (the last 7, 30 or 90 days, 12 months or all archived transactions), how many transactions to read
   at most and how many examples to keep, then click **Run backtest**.

The backtest runs in the background on the engine's backtest threads, so it can read hundreds of thousands of
transactions or more. The table of the rule's backtests updates as it goes, with a progress bar and a **Stop** button. A
backtest instance with rule text that differs from the editor is marked *edited*.

Click a backtest to see its results under the table:

- **From archive to firing**: how many transactions were read, kept by the filter, fired on and positive.
- **Confusion matrix**: true positives, false positives, false negatives and true negatives, shaded by their share.
- **Rates**: precision, recall, F1, specificity, fired rate and positive rate.
- **Tags** on the evaluated transactions.
- **Examples** of each outcome. Click one to see whether the filter kept it, whether the rule fired, whether it is
  positive and the values the rule read, which answers "why did it fire?" and "why did it miss?".

How much a backtest may read is set by `BacktestMaxRows`, and the backtest threads by `EnableBacktest` and
`BacktestThreads` (see [Environment Variables](../../../Concepts/EnvironmentVariables/index.html)).

### What a backtest reads, and what it leaves alone

- **Nothing is changed.** The archive and its tags are only read. The rule is run as it is in the editor, without being
  saved. The only thing written is the backtest's own record: its status, progress and result.
- **The archive is read through the report connection.** When `ReportConnectionString` is set, the archived transactions
  and their tags are read through it, so a read replica can carry the load. A replica that lags will not yet show the
  newest transactions. Checking the rule and reading the model's lists and fields use the main connection. Without
  `ReportConnectionString`, everything is read through the main connection.
- **The cache is not used.** Every value comes from the archived transaction and lists come from the database, so
  nothing is read from or written to the cache.

### How the counts are worked out

1. The archive is read a page at a time (`BacktestPageSize`), newest first by reference date. Only one page is held in
   memory, so a backtest's memory does not grow with the number of transactions it reads, and a later page costs no more
   to read than the first.
2. The filter is run on each transaction exactly as a reprocessing rule is. A transaction on which the filter raises an
   error is left out, and counted as a filter error.
3. The rule is run exactly as the engine would run it, with every value as it was when the transaction was invoked:
   payload, TTL counters, abstractions, sanctions, calculations, adaptations and earlier activation rules. Lists are
   read as they are now, not as they were. A rule that raises an error is counted as not fired, as the engine treats it,
   and the error is kept as an example.
4. What counts as positive is tested against the transaction's values and its tags. Tags are `True` or `False`.
   Comparisons are exact, and a field with no value never matches. Because this test is not written as rule text, it
   cannot use the operators that take a model list or another field, and its values must be written out rather than
   naming a field.

The counts are exact numbers; the rates shown are worked out from them on the page.

### When a backtest is refused

The rule, the filter and what counts as positive are all checked, and the rule compiled, before anything runs. If any of
them is not valid, each problem is reported against the part it is in, and nothing runs:

| Code                                                           | Meaning                                                                                                                                            |
|----------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| `RuleTextRequired`                                             | There is no rule text to backtest ("A rule to backtest is required.").                                                                             |
| `RuleTypeInvalid`                                              | The rule is not a gateway, activation or reprocessing rule.                                                                                        |
| `RangeInvalid`                                                 | The start of the window is later than its end.                                                                                                     |
| `RuleScriptInvalid`                                            | The rule text, or the filter, does not parse or compile. The message gives the line and position.                                                  |
| `FieldUnknown`, `OperatorInvalid`, `ValueInvalid` and the like | A condition in the filter or in what counts as positive names a field that is not available, or an operator or value that does not suit the field. |

### Tuning a threshold from outcomes

A backtest answers "how does this rule do?". Tuning answers "which threshold does best?". Write the rule text with a
placeholder where the number goes, for example `If (Payload.Amount > {{threshold}}) Then`, set the class to a confirmed
outcome (a tag such as `Tag.Fraud = True`, which closed cases write back), and give up to twelve candidate numbers. The
sweep runs the same backtest once for each candidate, in turn, and returns for each the four confusion matrix cells
together with precision (the share of firings that were positive) and recall (the share of positives that fired).
Precision is empty when the rule never fired and recall is empty when there were no positives.

Nothing is stored and nothing is changed: the rule on the model is untouched. To act on a result, save the chosen
variant as a new activation rule marked **Shadow** (see Shadow Mode in the Basic Activation Rules page), watch it
against live traffic, and promote it through approval. AskJooby can run the sweep through the same permission (61) and
propose a variant, but it cannot apply one.

On the rule page, the Backtest tab has a Tune a threshold section under the filter and positive-class builders. When the
rule text contains a placeholder such as `{{threshold}}` it shows the placeholder, takes up to twelve comma-separated
numbers, runs the sweep over the chosen window and shows each candidate's firings, precision, recall and four matrix
cells. **Save as shadow rule** on a row calls `POST /api/EntityAnalysisModelActivationRule/SaveShadowVariant`, which
creates a new activation rule named `<rule> shadow <value>` as a hand-written (Coder) rule with the number filled in and
the **Shadow** switch on, copying the other settings from the rule you are on. Like any new rule it has no effect until
a checker approves it, and a shadow rule has none even then; promote it by switching Shadow off through approval. A rule
built in the visual builder is copied as hand-written text, because the sweep works on text. At most two sweeps run at
once for a tenant; a further request is refused with a busy message until one finishes.

<!-- TODO(docs): the sweep replaces one placeholder with plain numbers only; sweeping several thresholds together and the analyst-hours cost per variant are not built. -->
<!-- TODO(screenshot): The rule page showing a threshold sweep as a table of candidates with precision and recall. -->

### Backtest instances

A backtest runs on the backtest threads of whichever engine node claims it first, so any number of threads and nodes
share the queue. Tenants are served in turn, and `BacktestMaxConcurrentRunsPerTenant` limits how many of one tenant's
backtests run at once. While a backtest runs, it sends a heartbeat on a timer, a quarter of `BacktestStaleInterval`/
`BacktestStaleIntervalValue` and at most every 10 seconds, however long a page takes. Each heartbeat records progress
and checks for a stop.

| Status     | Meaning                                                                                                                                                                                                                            |
|------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Pending    | Waiting for a backtest thread. **Stop** cancels it at once.                                                                                                                                                                        |
| Running    | Being run. **Stop** stops it after the current page.                                                                                                                                                                               |
| Succeeded  | Finished; click it to see the results.                                                                                                                                                                                             |
| Failed     | Could not run, ran longer than `BacktestMaxRunInterval`/`BacktestMaxRunIntervalValue`, or stopped reporting progress for longer than `BacktestStaleInterval`/`BacktestStaleIntervalValue`, for example because its engine stopped. |
| Cancelling | A stop was requested and the instance is finishing its current page.                                                                                                                                                               |
| Cancelled  | Stopped, or its engine shut down while it ran.                                                                                                                                                                                     |

A failed or cancelled backtest is never started again automatically; run it again.

## Dependencies

What the saved rule uses, with any name that does not exist highlighted, and what uses it, directly or through other
entities. A use through another entity is shown with the entity it passes through: for example, when a TTL counter
counts on the field `AccountId` and an activation rule tests that counter, the activation rule depends on `AccountId`
through the TTL counter. The same model-wide picture is on the **Dependencies** tab of
[Model Integrity](../ModelIntegrity/index.html), and it is also what stops an entity from being deleted while something
still uses it (see [Child Objects](../../../Navigation/ChildObjects/index.html#deleting-an-entity-that-is-in-use)).

## Integrity

Needs the **View Model Integrity** permission. The [Model Integrity](../ModelIntegrity/index.html) findings about this
rule, such as a rule the engine could not compile or has not loaded.

The three sections read and submit through `/api/EntityAnalysisModelBacktest`, `/api/EntityAnalysisModelDependency` and
`/api/EntityAnalysisModelIntegrity`, which call the same services the agent tools use and apply the same permissions.

## Limits and who can see what

<!-- TODO(docs): the owner should confirm every sentence in this section against a running system. -->

- **Tenant.** A backtest only ever reads the archive of a model in the caller's own tenant, and the instance, the result
  and the Stop request are looked up by tenant. Another tenant's model, instance or archived transaction behaves as if
  it
  did not exist (404).
- **The rule text is checked as the engine checks it.** The rule, the filter and the Explain request are compiled with
  the same allowed words as a saved rule (the rule script token list), so a rule the engine would refuse to load cannot
  be backtested; the compiled rule then runs in the same sandbox, with a time limit of five seconds per transaction. A
  rule that exceeds it stops the backtest, with the reason recorded.
- **Volume.** A submitted backtest reads at most `BacktestMaxRows` transactions (one million by default), a page at a
  time (`BacktestPageSize`), for at most `BacktestMaxRunInterval`. Each tenant's backtests run one at a time by default
  (`BacktestMaxConcurrentRunsPerTenant`). An immediate backtest (used by the agent tools) reads at most
  `BacktestOnlineMaxRows` transactions and stops after `BacktestOnlineMaxSeconds` seconds.
- **Messages.** The refusal messages for the window, the rule type, the missing rule text, the unknown transaction and
  an
  over-large immediate limit are translated into the language of the request. Messages from the rule compiler are
  English.
- **Examples** show an entry key and a reference date. **Explain** shows the values the rule read from the archived
  transaction, so it needs the same permission as the backtest.

- **Queue.** At most `BacktestMaxPendingPerModel` (5) backtests of a model and `BacktestMaxPendingPerTenant` (20) of a
  tenant may be Pending, Running or Cancelling at once. A further submit is refused with the message
  `PendingLimitModel` or `PendingLimitTenant`, in the language of the request, and nothing is stored; stop or wait for
  one to finish.

<!-- TODO(docs): confirm the queue limits and their defaults with the owner. -->
<!-- TODO(docs): the statuses (Pending, Running, Succeeded and so on) are shown translated in the table; confirm the wording in each language. -->
<!-- TODO(docs): the page describes Activation Rules only; the same panel is offered for other rule types if the page passes them. Confirm where else it appears. -->
