# Visualisation embedded in the case page

A Case Workflow can present a Visualisation Registry on its cases (`EnableVisualisation` and `VisualisationRegistryGuid`). The case page draws it as the first card, under "Dashboard". Without one there is no card and the sections are numbered from 1.

## Where a visualisation is shown

The Visualisation Registry form has a "Shown in" choice (`ContextKindId`):

- **Directory** (1, the default): listed and recalled on its own. It has no built-in parameter.
- **Case** (2): embedded as the dashboard of a Case.

## The one built-in parameter

When a registry is shown in a Case, every datasource receives `@Id`, the id of the Case. A registry shown in the Directory does not receive it, so a datasource that mentions `@Id` fails there instead of running with a null.

Nothing else about the case is supplied. The SQL traces what it needs from the id, through the tables the datasource allow-list already permits:

```sql
select a."Json" -> 'payload' ->> 'Amount' as "Amount", a."CreatedDate"
from "Case" c
join "Archive" a on a."Json" -> 'payload' ->> c."CaseKey" = c."CaseKeyValue"
where c."Id" = @Id
```

Master matched every Visualisation Parameter by name against every value of the case payload. That is gone.

The server checks the id before binding it: the case must belong to the caller's tenant, otherwise `@Id` is bound as null and the datasource returns nothing. A built-in `@Id` replaces a declared Parameter of the same name. The introspection and preview runs in the datasource builder bind `@Id` as 0 for a registry shown in a Case, so the SQL still validates.

## Parameters belong to the viewer

Declared Parameters are the viewer's own inputs. An embedded dashboard runs at once with their defaults and shows them as a filter bar, so a case dashboard can have its own date range or threshold with the case fixed behind the scenes.

## How the page asks

The card request (`DatasourceCardRequestDto`) carries `ContextId`. The dashboard component takes a `contextId` and `autoRun`, and sends the id with every tile.

## Not done

- The Case Workflow's registry dropdown still lists every registry rather than only those shown in a Case.
