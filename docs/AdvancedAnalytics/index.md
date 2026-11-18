---
layout: default
title: Advanced Analytics
nav_order: 7
parent: Configuration
has_children: true
---

# Advanced Analytics

Advanced Analytics holds trained models that score transactions inside the invoke pipeline. A model is organised as an
**AskJooby Analytics instance** (allocated to one Jube Model), its **trained models**, and their **versions**.

## Instances

An instance has a name (up to 256 characters, unique within the tenant), an allocated Jube Model, an Active flag and two
output flags (Response payload, Archive key). Permissions: 48 (Analytics) to view and rename, 47 (Promote Model) to
allocate, deallocate, switch live, change the output flags, delete an allocated instance, resubmit and retract.

- An instance can only be Active once allocated to a Model.
- Switching Active, changing Response payload or Archive key, or deleting creates a new version that a checker must
  approve before the engine uses it. Renaming does not.
- A maker cannot approve or reject their own change. Landlord users are exempt.

## Trained models and versions

Each trained model lists versions newest first with held-out metrics and warnings. Two versions can be compared: the
comparison says whether it is fair (same seed, split sizes and variables). The stored evidence, a model diagram, and
calibration and ROC charts are shown per version.

Promotion and rollback of a version are service and agent operations (Promote Model permission, 47); there is no HTTP
endpoint for them. Because the engine scores with whichever version is live, they are refused with `InstanceIsLive`
while the owning instance is live (its current row is Active, or its last approved version is Active). Switch the
instance off first, which a checker must approve, then promote or roll back, then switch it on again, which a checker
approves. An instance that is not live can be promoted freely.

## Model files

Models are stored as JSON only, with no type names honoured, a depth limit of 32 and a size limit of 16 MB of
characters. There is no upload endpoint in this release.

## Right-to-left languages

Narrative paragraphs and model names are isolated so numbers and Latin terms do not reorder in Arabic and Urdu. Charts
are drawn left to right whatever the page direction, so axis labels keep their positions; chart text uses system fonts
with Noto fallbacks for Arabic, Urdu, Devanagari and Georgian, none of which is bundled.
