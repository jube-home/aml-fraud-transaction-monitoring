---
layout: default
title: TTL Counter Activation Rule Incrementation
nav_order: 15
parent: Models
grand_parent: Configuration
---

# TTL Counter Activation Rule Incrementation

<!-- TODO(docs): this whole page pre-dates the Vue UI; owner to read end to end and re-check every Razor-era sentence (menu paths, tab names, button labels) against the Models story page -->
In previous documentation the TTL Counter definition was introduced and a TTL Counter by the name of IP was created.

To remind, navigate to Models >> Abstraction >> TTL Counters:

<!-- TODO(screenshot): file=model-abstraction-ttlcounter-definition.png route=/models state="TTL Counter form for IP as defined for this walkthrough" -->
![Image](model-abstraction-ttlcounter-definition.png)

This TTL Counter definition will be incremented on the match of Volume1DayUSDForIPOver100. Navigate to the
Volume1DayUSDForIPOver100 Activation Rule:

<!-- TODO(screenshot): file=model-abstraction-activation-rule-to-use-for-ttlcounter.png route=/models state="Activation Rules card with the rule that will increment the counter selected" -->
![Image](model-abstraction-activation-rule-to-use-for-ttlcounter.png)

Scroll down to the section where the Increment TTL Counter check box exists:

<!-- TODO(screenshot): file=model-abstraction-ttlcounter-location-in-activation-rule.png route=/models state="Activation Rule form with the TTL Counter group expanded" -->
![Image](model-abstraction-ttlcounter-location-in-activation-rule.png)

Check Increment TTL Counter to expose the options for incrementing TTL Counters:

<!-- TODO(screenshot): file=model-abstraction-exposed-ttlcounter-properties.png route=/models state="Activation Rule TTL Counter switch on showing the Model and Counter pickers" -->
![Image](model-abstraction-exposed-ttlcounter-properties.png)

It is worthy of note that TTL Counters can be incremented cross model, which assumes that the model being incremented
has the IP field available to both models. In this case, select the IP for the same model:

<!-- TODO(screenshot): file=model-abstraction-select-ipfor-model-ttlcounter.png route=/models state="TTL Counter picker with IP selected" -->
![Image](model-abstraction-select-ipfor-model-ttlcounter.png)

Scroll down and click Update a create new version of the Activation Rule:

<!-- TODO(screenshot): file=model-abstraction-updated-activation-rule-for-ttlcounter.png route=/models state="Activation Rule after Save with the TTL Counter increment set" -->
![Image](model-abstraction-updated-activation-rule-for-ttlcounter.png)

Synchronise the model via Entity >> Synchronisation and repeat the HTTP POST to
endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc)
for response as follows:
<!-- TODO(docs): the menu path or synchronisation step above is Razor-era wording; confirm against the Vue Models story page, and whether synchronisation is still a manual step -->

<!-- TODO(screenshot): file=model-abstraction-ttlcounter-has-not-advanced.png route=/models state="Invocation response with the TTL Counter still at 0 before synchronisation" -->
![Image](model-abstraction-ttlcounter-has-not-advanced.png)

The Activation Rule should of course be showing as activated, however, the TTL Counter will have been incremented also.

As incrementation takes place AFTER the activation of the rule, it can be seen that the TTL Counter has not been
incremented at the point the response is written out, as the count was TTL counts were fetched BEFORE the invocation of
the TTL counter.

Repeat the HTTP POST to
endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc)
for response as follows:
<!-- TODO(docs): the menu path or synchronisation step above is Razor-era wording; confirm against the Vue Models story page, and whether synchronisation is still a manual step -->

<!-- TODO(screenshot): file=model-abstraction-increment-after-ip.png route=/models state="Invocation response with the TTL Counter advanced after the activation rule matched" -->
![Image](model-abstraction-increment-after-ip.png)

It can be seen that the TTL counter was incremented on the first post, and is now available for the subsequent
transaction.

As introduced elsewhere in this documentation, TTL Counters exist as a lightweight means to maintain state and history
for a given key:

<!-- TODO(screenshot): file=model-abstraction-note-availability-for-rule-construction.png route=/models state="Rule builder palette showing the TTL Counter available as a field" -->
![Image](model-abstraction-note-availability-for-rule-construction.png)

<!-- TODO(docs): the Activation Rule stores the Model Guid and the TTL Counter Guid it increments. Review finding: the increment writes the live counter under the evaluating Model's Guid while the decrement entry uses the rule's target Model Guid, so a rule that targets a counter on a different Model is not read back by that Model. Owner to decide whether cross-model increment is supported and document it either way -->
<!-- TODO(docs): when Activation Rule Idempotency is enabled, a duplicate invocation of the same transaction is not counted twice; describe and confirm -->
