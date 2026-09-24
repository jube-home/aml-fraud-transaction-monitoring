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

The page opens on the first model (or the model given as `EntityAnalysisModelId` in the address); choose another and
click **Check** to check it. The page has three tabs, each a grid whose rows expand:

- **Findings**: one row per entity with findings (or the model itself, or an engine instance), with its worst severity,
  its categories and how many errors, warnings and information findings it has. Expand a row for its findings: severity,
  the check, category and what was found. For example, a rule that names a TTL counter that does not exist, a rule the
  engine could not compile at its last synchronisation, TTL counters that are disabled for the model, or a search key
  whose TTL silently shortens an abstraction rule's window.
- **Engine**: one row per engine instance, with when it last reported and synchronised and whether it has loaded and
  started the model. Expand a row for how many entities of each kind it has loaded, and expand a kind for each entity's
  name, id and guid (an entity the engine still runs but that has since been deleted shows no guid). Where the engine
  has not loaded something yet, or is still running something deactivated, the findings say so; synchronise the model to
  bring the engine up to date.
- **Dependencies**: a diagram of the model, laid out left to right from each entity to the entities it uses. Each box
  shows the entity's kind (coloured by kind) and name; settings, such as a search key or a TTL counter's data name, are
  drawn as dashed lines, inactive entities as grey dashed boxes and names that resolve to nothing as red dashed boxes.
  Entities that use nothing and that nothing uses are left out of the diagram (they are still in the table). Hover over
  a box to see what it uses and what uses it, and through which names; hover over a line to see what it joins. Click a
  box to highlight it with everything it is connected to and to open its row in the table; click a row in the table to
  highlight the entity in the diagram. Click an empty part of the diagram or press Esc to clear. The diagram is fitted
  to the width of the page and sized to its content; a large model opens at the top, and you scroll to zoom and drag to
  move around it. Below it, one row per entity, with how many entities it uses, how many use it and how many names it
  uses that do not exist. Expand a row for what it uses and what uses it, through which name, and how (rule text or a
  setting). At most the 150 most connected entities are drawn, and the page says so when some are left out.

The grids sort and filter like the other grids in Jube. The page reads its data from
`/api/EntityAnalysisModelIntegrity`, `/api/EntityAnalysisModelEngineState` and
`/api/EntityAnalysisModelDependencyGraph`, which call the same service the agent tools use.

Nothing on the page changes the model.

## The checks

Each finding has a severity: **Error** means something does not work, **Warning** means something may not work as
intended, and **Info** is for information only. The findings are listed errors first.

| Check                                                | Severity                                     | Category      | What it means                                                                                                                                                   |
|------------------------------------------------------|----------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Uses a name that does not exist                      | Error (Info when the entity is inactive)     | Dependencies  | Rule text or a setting names something that does not exist, so the rule would not compile or the setting would not work.                                        |
| Uses an inactive entity                              | Warning                                      | Dependencies  | An active entity uses an inactive one, which the engine does not load.                                                                                          |
| Not used by anything                                 | Info                                         | Dependencies  | An active list, dictionary, TTL counter, abstraction rule, abstraction calculation, inline function or adaptation that nothing in the model uses.               |
| Did not compile in the engine                        | Error                                        | Compilation   | The engine could not compile an active rule at its last synchronisation, so it is not running. The engine's own compile error is given.                          |
| Model is inactive                                    | Info                                         | Configuration | The model is inactive, so the engine does not load it.                                                                                                          |
| No active activation rules                           | Info                                         | Configuration | No transaction can activate.                                                                                                                                    |
| TTL counters are disabled                            | Warning                                      | Configuration | TTL counters are disabled for the model, so an active counter is never incremented or read, and rules see 0.                                                    |
| Search key history is shorter than the rule window   | Warning                                      | Configuration | A search key keeps history for less time than an abstraction rule's interval, so the engine silently uses the shorter window (see [Abstraction Rules](../AbstractionRules/index.html#the-search-window)). |
| Search key keeps no history for the rule window      | Warning                                      | Configuration | The search key has no TTL, so the abstraction rule's window collapses to the transaction's reference date.                                                      |
| Engine state is unavailable                          | Info                                         | Engine        | No engine runs alongside this instance and none has recorded what it loaded, so only the engine instances' heartbeats can be checked.                          |
| No engine instance has synchronised                  | Warning                                      | Engine        | No engine instance has synchronised the tenant's models.                                                                                                        |
| Engine instance has stopped reporting                | Warning                                      | Engine        | An engine instance has not reported for more than 10 minutes.                                                                                                   |
| Model is not loaded by an engine instance            | Warning                                      | Engine        | An engine instance has not loaded the (active) model.                                                                                                           |
| Model is loaded but not started                      | Warning                                      | Engine        | An engine instance has loaded the model but not started it.                                                                                                     |
| Not loaded by an engine instance                     | Warning                                      | Engine        | An active entity is not loaded by an engine instance: it is waiting for synchronisation, did not compile or, for an activation rule, is not approved.           |
| Engine still runs an entity that is no longer active | Warning                                      | Engine        | An engine instance is still running an entity that is no longer active, until the model is synchronised.                                                        |

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
