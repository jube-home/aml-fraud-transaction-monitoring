---
layout: default
title: Model Integrity
nav_order: 23
parent: Models
grand_parent: Configuration
---

# Model Integrity

The **Models > Integrity** page checks the basics of a model and lists anything that is wrong or may not work as
intended. It needs the **View Model Integrity** permission.

<!-- TODO(docs): the Razor Integrity page opened on three tabs; the Vue page is a canvas of collapsible sections (Findings, Engine, Dependencies, Pending approvals) with a section index and a verdict band. Confirm the wording below against the real page and the section order. -->
<!-- TODO(screenshot): file=model-integrity-canvas.png route=/models/story state="Integrity canvas for one model, verdict band, Findings and Engine sections open, Dependencies and Pending closed" -->

The page opens on the first model (or the model given as `EntityAnalysisModelId` in the address); choose another and
click **Check** to check it. The page has these sections, the findings and the engine open at first, each a grid whose
rows expand:

- **Findings**: one row per entity with findings (or the model itself, or an engine instance), with its worst severity,
  its categories and how many errors, warnings and information findings it has. Expand a row for its findings: severity,
  the check, category and what was found. For example, a rule that names a TTL counter that does not exist, a rule the
  engine could not compile at its last synchronisation, TTL counters that are disabled for the model, or a search key
  whose TTL silently shortens an abstraction rule's window.

<!-- TODO(screenshot): file=model-integrity-findings.png route=/models/story state="Findings section with one error row expanded onto its checks" -->

- **Engine**: one row per engine instance, with when it last reported and synchronised and whether it has loaded and
  started the model. Expand a row for how many entities of each kind it has loaded, and expand a kind for each entity's
  name, id and guid (an entity the engine still runs but that has since been deleted shows no guid). Where the engine
  has not loaded something yet, or is still running something deactivated, the findings say so; synchronise the model to
  bring the engine up to date.

<!-- TODO(screenshot): file=model-integrity-engine.png route=/models/story state="Engine section: synchronisation panel above a grid of two instances, one expanded to the loaded entities" -->

- **Dependencies**: a diagram of the whole model, laid out left to right: each entity sits to the left of the
  entities it uses, and the page scrolls down when a column is long. Each box shows the entity's name, coloured by kind;
  settings, such as a search key or a TTL counter's data name, are drawn as dashed lines, inactive entities as dashed
  boxes and names that resolve to nothing as red boxes. Entities that use nothing and that nothing uses are left out of
  the diagram (they are still in the table, and the page says how many).
  A model with more than 40 connected entities opens grouped: one box per kind with a count, such as "Activation rule
  ×9", and one arrow between two groups with how many uses it stands for. Select a group, or the button for its kind
  above the diagram, to show its entities one by one; select the button again to group them. A smaller model opens in
  full. Which kinds are shown is kept in the address, as `expanded`, so a link opens the same view; **Automatic** goes
  back to the default. Entities with findings are always drawn on their own, even inside a grouped kind, with an amber
  ring for warnings and a red ring for errors, so the scale of the model and its problems are both visible at once. When
  a view would be too large to draw legibly, for example every kind expanded on a very large model, the page groups the
  largest kinds until it fits rather than failing. A long column wraps into several columns of at most 12 entities.
  Hover over a box to see what it uses and what uses it, and through which names; hover over a line to see what it
  joins.
  Click a box to highlight it with everything it is connected to and to open its row in the table; click a row in the
  table to highlight the entity in the diagram. Click an empty part of the diagram or press Esc to clear. The diagram is
  fitted to the width of the page and sized to its content; scroll to zoom and drag to move around it. Below it, one row
  per entity, with how many entities it uses, how many use it and how many names it uses that do not exist. Expand a row
  for what it uses and what uses it, through which name, and how (rule text or a setting).

<!-- TODO(screenshot): file=model-integrity-dependencies.png route=/models/story state="Dependencies section with the diagram and one entity highlighted, and the entity table below" -->
<!-- TODO(screenshot): file=model-integrity-dependencies-grouped.png route=/models/story state="Dependencies section for a large model: kind buttons above a grouped diagram with counts, one kind expanded, flagged entities ringed" -->

- **Pending approvals**: the changes to this model's entities that are waiting for approval, grouped by kind, which a
  user holding the approval permission can approve or reject here
  (see [Approvals](../../Administration/Approvals/index.html)).
  <!-- TODO(docs): verify the Approvals link target exists and that the pending-approvals section is described correctly; this section is reviewed with the approvals slice. -->

The grids sort and filter like the other grids in Jube. The page reads its data from
`/api/EntityAnalysisModelIntegrity`, `/api/EntityAnalysisModelEngineState` and
`/api/EntityAnalysisModelDependencyGraph`, which call the same service the agent tools use.

Nothing on the page changes the model.

## The checks

Each finding has a severity: **Error** means something does not work, **Warning** means something may not work as
intended, and **Info** is for information only. The findings are listed errors first.

| Check                                                | Severity                                 | Category      | What it means                                                                                                                                                                                             |
|------------------------------------------------------|------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Uses a name that does not exist                      | Error (Info when the entity is inactive) | Dependencies  | Rule text or a setting names something that does not exist, so the rule would not compile or the setting would not work.                                                                                  |
| Uses an inactive entity                              | Warning                                  | Dependencies  | An active entity uses an inactive one, which the engine does not load.                                                                                                                                    |
| Uses an entity that is not yet approved              | Warning                                  | Dependencies  | An active entity uses one whose change is waiting for approval, which the engine does not run until it is approved.                                                                                       |
| Not used by anything                                 | Info                                     | Dependencies  | An active list, dictionary, TTL counter, abstraction rule, abstraction calculation, inline function or adaptation that nothing in the model uses.                                                         |
| Did not compile in the engine                        | Error                                    | Compilation   | The engine could not compile an active rule at its last synchronisation, so it is not running. The engine's own compile error is given.                                                                   |
| Model is inactive                                    | Info                                     | Configuration | The model is inactive, so the engine does not load it.                                                                                                                                                    |
| No active activation rules                           | Info                                     | Configuration | No transaction can activate.                                                                                                                                                                              |
| TTL counters are disabled                            | Warning                                  | Configuration | TTL counters are disabled for the model, so an active counter is never incremented or read, and rules see 0.                                                                                              |
| Search key history is shorter than the rule window   | Warning                                  | Configuration | A search key keeps history for less time than an abstraction rule's interval, so the engine silently uses the shorter window (see [Abstraction Rules](../AbstractionRules/index.html#the-search-window)). |
| Search key keeps no history for the rule window      | Warning                                  | Configuration | The search key has no TTL, so the abstraction rule's window collapses to the transaction's reference date.                                                                                                |
| Engine state is unavailable                          | Info                                     | Engine        | No engine runs alongside this instance and none has recorded what it loaded, so only the engine instances' heartbeats can be checked.                                                                     |
| No engine instance has synchronised                  | Warning                                  | Engine        | No engine instance has synchronised the tenant's models.                                                                                                                                                  |
| Engine instance has stopped reporting                | Error                                    | Engine        | An engine instance has not reported for more than 2 minutes but less than 60. After 60 minutes it drops out of the findings and is removed at the next synchronisation.                                   |
| Model is not loaded by an engine instance            | Warning                                  | Engine        | An engine instance that is reporting has not loaded the (active) model. An instance that has stopped reporting is shown as stopped instead.                                                               |
| Model is loaded but not started                      | Warning                                  | Engine        | An engine instance has loaded the model but not started it.                                                                                                                                               |
| Not loaded by an engine instance                     | Warning                                  | Engine        | An active entity is not loaded by an engine instance: it is waiting for synchronisation, did not compile or, for an activation rule, is not approved.                                                     |
| Engine still runs an entity that is no longer active | Warning                                  | Engine        | An engine instance is still running an entity that is no longer active, until the model is synchronised.                                                                                                  |
| Waiting for approval                                 | Info                                     | Engine        | An entity (or a change to it) is waiting for approval, so the engine does not run it, or still runs the last approved version.                                                                            |

The comparison with the engine covers request XPaths, inline functions, gateway rules, abstraction rules, abstraction
calculations, TTL counters, sanctions, HTTP adaptations and activation rules.

## Where the engine's state comes from

The engine may run in the same process as the user interface and API, or on separate nodes. The page reads what the
engine has loaded from one of two places:

- When the engine runs in the same process, what it has loaded is read from it directly.
- Otherwise, at every synchronisation, each engine instance records, for each model it has loaded, whether it has
  started it and which entities it has loaded, one row per instance and model in `EntityAnalysisModelEngineSnapshot`.
  These records are read instead. When the engine does run in the same process, the records of the other instances
  are still used.

When neither is available, the **Engine state is unavailable** finding is shown and only the heartbeats are checked.
The heartbeats and synchronisation dates come from the node status entries, and compile results from the `Compiled`
and `CompileError` columns the engine writes at synchronisation (see
[Rule Compilation Tokens and Extensions](../RuleCompilationAlgorithm/index.html#compile-diagnostics)).

<!-- TODO(docs): the check codes come from `IntegrityCode` in Jube.Engine/Integrity/Models; the titles and messages are localised on the server (`Title_*` and `Message_*` in EntityAnalysisModelIntegrityResources) and the table above is the English text. The Category column and the entity kind labels are still English identifiers. -->

## Synchronisation

The Engine section holds a synchronisation panel. Engine instances compile a model's rules and load its entities at a
**synchronisation**; until one runs, a change made on the page is not what the engine is running. The panel needs the
**Synchronisation** permission (permission 5) to schedule anything; without it the instances are shown and nothing can
be
scheduled.

- **Synchronise now** schedules a synchronisation for this model for the current time.
- Choose a date and time and click **Schedule** to schedule one for later. The date cannot be more than one year ahead.
- On the tenant-level panel (the Models list) the same buttons synchronise every model in the tenant.

Each click adds one row to the schedule (there is no de-duplication, so two clicks are two rows). A tenant may add at
most `SynchronisationScheduleMaxPerTenant` rows (100 by default) and one model at most
`SynchronisationScheduleMaxPerModel` (30) within `SynchronisationScheduleWindowMinutes` (10); past either the request is
refused with a message and HTTP 429.
<!-- TODO(docs): confirm the refusal message wording and the limit defaults with the owner. --> Every engine instance
polls the schedule; an instance honours the rows that have become due since its last synchronisation. A row for one
model synchronises only that model and leaves the tenant's other models as they are; if a row for the whole tenant is
also due, the whole tenant is synchronised. An instance that has never synchronised the tenant honours every row that is
due. The grid below the buttons lists the instances that have reported in the last hour, with the time of the last
heartbeat and the last synchronisation, and a state: **Unavailable** (no heartbeat for 2 minutes), **Pending** (a due
synchronisation has not been honoured yet) or **Synchronised**.

Each instance stamps its synchronisation with the time it asked for pending rows, not the time it finished, so a row
scheduled while a long synchronisation is running is picked up by the next one.

<!-- TODO(screenshot): file=model-integrity-synchronise.png route=/models/story state="Synchronisation panel: date picker, Schedule, Synchronise now, Refresh, grid with one Synchronised and one Unavailable instance" -->
<!-- TODO(docs): confirm the button captions (integrity.sync.* keys), that the tenant panel is on the Models list, and the one-year limit message wording. -->
<!-- TODO(docs): "poll interval" is the ModelSynchronisationWait setting; document its default in the environment variables page and link it here. -->
<!-- TODO(docs): per-model synchronisation was added on the datasource-builder branch; check whether a release-notes or upgrade note is needed. -->

## Permissions

| Action                                          | Permission                |
|-------------------------------------------------|---------------------------|
| Integrity check, engine state, dependency graph | 60 (View Model Integrity) |
| Download a sample                               | 40                        |
| Schedule synchronisation, see instance status   | 5                         |

<!-- TODO(docs): verify the permission names against the Permissions catalogue; the numbers are read from the services' RequiredPermissions. -->
