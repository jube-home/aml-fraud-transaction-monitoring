---
layout: default
title: Reprocessing Eligibility
nav_order: 1
parent: Reprocessing
grand_parent: Configuration
---

# Reprocessing Eligibility

Reprocessing allows for the contents of the Archive table to be reprocessed one by one retroactively, which is useful if
a new rule has been created and some historic management is required.

It should be noted that reprocess is computationally expensive and should not be used in place of a forward only
transaction or event methodology. A common use case is to facilitate the reprocessing of an extremely targeted
specification, such as a particular high risk client or customer subset. It should also be noted that reprocessing takes
its data from the archive and reads the cache, but does not write to it: a replayed transaction does not re-add its
payload to the cache, does not move the latest reference date, does not increment TTL counters and does not claim the
idempotency keys used by case creation and notifications. The extent to which the cache covers the time frame being
reprocessed should be considered, because abstraction and TTL counter values are read from the cache as it is now.
<!-- TODO(docs): confirm with the owner that reading the cache as it is now (not as it was at the transaction's reference date) is the intended semantics of replay; it is the behaviour of the code -->

There are a number of ways to remove load from the reprocessing task.

The steps for reprocessing are firstly, setting an Activation Rule to be eligible for reprocessing. Navigate to the
Activation Rule Volume1DayUSDForIPOver100:

<!-- TODO(screenshot): file=model-reprocessing-activation-rule-ready-to-be-updated.png route=/models/activation-rules state="Activation Rule Volume1DayUSDForIPOver100 open" -->
![Image](model-reprocessing-activation-rule-ready-to-be-updated.png)

Scroll down the Activation Rule and locate the Reprocessing switch:

<!-- TODO(screenshot): file=model-reprocessing-location-of-reprocessing-switch.png route=/models/activation-rules state="Enable Reprocessing switch located" -->
![Image](model-reprocessing-location-of-reprocessing-switch.png)

Ensure that the Enable Reprocessing is switched to true for this Activation Rule to be eligible for reprocessing.

Reprocessing jobs are responsible for fetching data from the Archive table, filtering and performing model invocation
for each event returned:

When reprocessing takes place, model invocation is almost exactly the same as if it were being called real-time, and on
a similar one by one basis in order of transaction processing date. However, when it comes to executing Activation Rules
as part of model invocation, only rules that have been marked as Reprocessing are evaluated.

To set a rule to be eligible for reprocessing, it is a simple matter of updating the Reprocessing check-box in the
Activation Rule. Scroll down to enable reprocessing by setting the switch to true:

<!-- TODO(screenshot): file=model-reprocessing-enable-reprocessing-switch.png route=/models/activation-rules state="Enable Reprocessing switched on" -->
![Image](model-reprocessing-enable-reprocessing-switch.png)

Scroll down and update the Activation Rule to create a version:

<!-- TODO(screenshot): file=model-reprocessing-updated-activation-rule.png route=/models/activation-rules state="Activation Rule saved as a new version" -->
![Image](model-reprocessing-updated-activation-rule.png)

It is important that the model is synchronised, as reprocessing relied upon the Activation Rule configurations,
henceforth, navigate to Models >> Synchronisation and enact.

While the transaction is sourced from the Archive, it is important to note that the transaction is replayed through the
invoke engine on the same basis as a real-time transaction. It follows that care must be taken to ensure that time
frames of eligibility in the Cache exceed that filtered from the Archive. The availability of cache data must be
sufficient to provide coverage for not only the transaction, but crucially the maximum range required of abstraction
rules. In the absence of the transaction existing in the cache it is not added (reprocessing never writes the cache),
so abstraction rules would assume a standing start for any history the cache no longer holds, and may not be wholly
accurate for the older part of the window.

Only these side effects are suppressed on a replay, and each is checked by a test: cache writes, TTL counter increments,
the activation watcher, case creation (a replay never creates a case), response elevation stamping and dispatch (holds,
notifications and webhooks), and the write-ahead log. Activation Rules not marked for reprocessing are
suppressed for the run. The archive record itself is updated in place and its previous version is kept.
<!-- TODO(docs): owner to check this list reads correctly for a compliance audience; source is IterateActivationRulesExtensions, ActivationRuleTtlCounterExtensions, ActivationRuleCreateCaseObjectExtensions, ResponseElevationRangeExtensions, ExecuteCacheEntriesExtensions -->

