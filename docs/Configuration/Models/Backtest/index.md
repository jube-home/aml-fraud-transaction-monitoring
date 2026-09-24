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

## Backtest

Needs the **Run Backtest** permission. A backtest runs the rule, as it is in the editor (saved or not), over archived
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

### Backtest instances

A backtest runs on the backtest threads of whichever engine node claims it first, so any number of threads and nodes
share the queue. Tenants are served in turn, and `BacktestMaxConcurrentRunsPerTenant` limits how many of one tenant's
backtests run at once. While a backtest runs, it sends a heartbeat on a timer, a quarter of `BacktestStaleSeconds` and
at most every 10 seconds, however long a page takes. Each heartbeat records progress and checks for a stop.

| Status     | Meaning                                                                                                                                                               |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Pending    | Waiting for a backtest thread. **Stop** cancels it at once.                                                                                                           |
| Running    | Being run. **Stop** stops it after the current page.                                                                                                                  |
| Succeeded  | Finished; click it to see the results.                                                                                                                                |
| Failed     | Could not run, ran longer than `BacktestMaxRunSeconds`, or stopped reporting progress for longer than `BacktestStaleSeconds`, for example because its engine stopped. |
| Cancelling | A stop was requested and the instance is finishing its current page.                                                                                                  |
| Cancelled  | Stopped, or its engine shut down while it ran.                                                                                                                        |

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
