---
layout: default
title: Advanced Analytics
nav_order: 7
parent: Configuration
has_children: true
---

# Advanced Analytics

<!-- TODO(docs): whole page is a draft written from code; owner to read and finalise. -->

Advanced Analytics holds trained models that score transactions inside the invoke pipeline. A model is organised as an
**AskJooby Analytics instance** (allocated to one Jube Model), its **trained models**, and their **versions**.

<!-- TODO(docs): confirm the menu path and nav_order for this page. -->

## Instances

<!-- TODO(screenshot): file=model-advanced-analytics-instances.png route=/models/:id state="Advanced Analytics section, two instances, one pending deletion" -->
![Instances](model-advanced-analytics-instances.png)

An instance has a name (up to 256 characters, unique within the tenant), an allocated Jube Model, an Active flag and two
output flags (Response payload, Archive key). Permissions: 48 (Analytics) to view and rename, 47 (Promote Model) to
allocate, deallocate, switch live, change the output flags, delete an allocated instance, resubmit and retract.

<!-- TODO(docs): verify permission numbers 48 (Analytics) and 47 (Promote Model) against the PermissionSpecification seed. -->

- An instance can only be Active once allocated to a Model.
- Switching Active, changing Response payload or Archive key, or deleting creates a new version that a checker must
  approve before the engine uses it. Renaming does not.
- A maker cannot approve or reject their own change. Landlord users are exempt.

<!-- TODO(docs): landlord self-approval is deliberate in code and tested; owner to confirm it should be documented. -->

## Trained models and versions

<!-- TODO(screenshot): file=model-advanced-analytics-versions.png route=/models/:id state="Versions table with live marker and comparison" -->
![Versions](model-advanced-analytics-versions.png)

Each trained model lists versions newest first with held-out metrics and warnings. Two versions can be compared: the
comparison says whether it is fair (same seed, split sizes and variables). The stored evidence, a model diagram, and
calibration and ROC charts are shown per version.

Promotion and rollback of a version are service and agent operations (Promote Model permission, 47); there is no HTTP
endpoint for them. Because the engine scores with whichever version is live, they are refused with `InstanceIsLive`
while the owning instance is live (its current row is Active, or its last approved version is Active). Switch the
instance off first, which a checker must approve, then promote or roll back, then switch it on again, which a checker
approves. An instance that is not live can be promoted freely.

<!-- TODO(docs): confirm this off, promote, on sequence is acceptable operationally; there is no emergency rollback of a live instance without a checker. -->

## Model files

Models are stored as JSON only, with no type names honoured, a depth limit of 32 and a size limit of 16 MB of
characters. There is no upload endpoint in this release.

<!-- TODO(docs): the Source column shows Upload; confirm what produces it. -->

## Right-to-left languages

Narrative paragraphs and model names are isolated so numbers and Latin terms do not reorder in Arabic and Urdu. Charts
are drawn left to right whatever the page direction, so axis labels keep their positions; chart text uses system fonts
with Noto fallbacks for Arabic, Urdu, Devanagari and Georgian, none of which is bundled.

<!-- TODO(docs): confirm whether non-Latin fonts should be bundled with the app. -->

<!-- TODO(screenshot): file=model-advanced-analytics-rtl.png route=/models/:id state="Arabic locale, versions and performance sections" -->
