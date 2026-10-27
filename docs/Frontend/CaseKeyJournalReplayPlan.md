# Case Key Journal: replay on the map (plan)

Status: stages 1 and 4 and the saved replay are built (see "Decisions taken and what is built" and "Track endpoint and saved replays"); nothing from the plan is left unbuilt. It answers the open questions in `CaseKeyJournalDesign.md` ("Replay the journal") from what the code does today, and ends with the decisions that are the owner's.

## What is already there

- **The Watcher is already an embeddable component.** `ActivationWatcher.vue` takes `source`, `history`, `replayFrom`, `replayTo`, `keyFilter`, `keyValueFilter`, `trails`, `heat`, `travelLimitKmh` (default 900 km/h), `tall` and switches for the controls, chart and feed. `WatcherPage.vue` is just one host. So no split of the component is needed to embed it.
- **A source is already an interface.** `WatcherSource` is `replay(query)` returning raw items, plus an optional `stream`. The activation source reads `/api/ActivationWatcher/Replay` and a live stream. A journal source is a second implementation with no stream.
- **The engine of the map speaks `Activation`.** `useWatcher`, `state.ts` (`ingest`, `latestPerEntity`, `trailsOf`, `withinWindow`) and `geo.ts` (`segmentOf`, haversine, the impossible-travel test of distance of at least 50 km and speed above the limit) all work on the `Activation` shape: id, key, key value, entity, latitude, longitude, previous point, a summary line, response elevation, colours, time. Improbable travel is already computed there, from `previous`, or from the order of points per entity when no `previous` is given.

## Where a position comes from

The engine finds the location of a transaction from the model, not from the Case Workflow:

1. the model's request XPath whose data type is 6 (latitude) and the one whose data type is 7 (longitude), by name; else
2. an inline script property marked `[Latitude]` and `[Longitude]`;

and reads that name from the transaction payload. A transaction with neither, or with 0,0, is not located, and the Watcher already treats those as unlocated. The Archive JSON holds the payload by the same names, so the journal can read the same fields for every archived transaction.

Two things follow:

- The journal's own field role "Geography" (role 6 on the Case Workflow XPath) is a different thing: a label on a column for the analytics. It does not say which column is latitude and which is longitude, so it cannot be the position source.
- The user sees a payload column only if their roles can. The journal must not leak a location the user cannot otherwise see. Decision D1.

## Proposed design

### Point

A journal point is an `Activation` with these meanings, so the map, trails, heat, fit, fade and the speed test work unchanged:

| `Activation` field | Journal meaning |
|---|---|
| `id` | the transaction's entry Guid |
| `key`, `keyValue` | the lane's key and value (case key, or a merged key) |
| `entity` | the lane letter and value, so each lane draws its own trail |
| `latitude`, `longitude`, `located` | the resolved position |
| `previous` | the previous located point of the same lane, worked out by the source in time order |
| `ruleSummary` | the activations of the transaction, then the highlight reasons |
| `responseElevation` | the transaction's elevation |
| `backColor`, `foreColor` | the bold line and cell colours of the grid, so a highlighted transaction is the same colour on the map |
| `createdAt`, `createdDate` | the transaction's reference date |

Two small additions to the type, both optional so the activation source is unaffected: `lane` (letter) and `pinned`. The popover for a journal point shows tags, pin, distance and speed from the previous point, and an Open button that opens the transaction drawer.

### Source

`journalSource(view)`, where `view` is the journal's current state: key, window, merged lanes, pins, lens and the shared filter. Two stages:

**Stage 1, from the loaded rows (client only).** The rows endpoint tells the client which two payload columns are latitude and longitude (a new `meta.location` of `{ latitude: 'ColumnName', longitude: 'ColumnName' }`, or null with a reason). The source takes the filtered grid rows (`gridRows`), converts them, and computes `previous` per lane. No new endpoint, and it replays exactly what the grid shows, including the filter, lanes and pins. Limit: the grid's 1,000 rows.

**Stage 2, the whole key (server).** `GET /api/CaseKeyJournal/Track` returns minimal points (guid, time, latitude, longitude, lane, pinned, elevation, activations, tags, colours) for every transaction of the view up to 20,000, applying the same `JournalFilter` that Tag all matching uses, paged by id like `JournalMatcher`. The server also computes distance and speed from the previous point, so the same numbers appear in the grid and the map, and offers `impossibleOnly` to return just the legs over a speed. This is the endpoint an agent tool can call too (follow the existing tool pattern, with `ServiceToolCatalogue` entries and the hierarchy docs).

Stage 1 is useful on its own and settles the point shape; stage 2 only changes where the points come from.

### Filters

- **Already shared, nothing to build:** the journal filter (any column, including lane, pinned, activations, tags, elevation, value ranges, "has a location" through is not null on the latitude column), the window, activations only, pinned only and the lens.
- **New, replay specific:** a speed limit control (the existing `travelLimitKmh`, default 900), a minimum distance (the 50 km constant exposed), and "improbable legs only", which keeps just the pairs that exceed the limit and the points on either end. These live in the map's own controls, because they are about legs rather than transactions, and are held in the address (`jr...`) like the other journal state.
- **Unlocated transactions** are counted and listed in a line under the map ("12 of 140 transactions have no location") with a button that sets the filter to show them in the grid, and are never drawn at 0,0.

### Placement and behaviour

- A new section on the case page, "Replay", between Journal and Network, collapsed by default and lazy like the others. Not inside the journal card, so the journal's own layout and the left and right panes are unaffected.
- It follows the journal's state (it reads the same address state), so changing the window, lane or filter in the journal redraws the map.
- Selecting a point opens the transaction drawer, and opening a transaction in the grid highlights its point.
- Live is off. Replay controls (from, to, speed, fade, trails, heat, fit) are the Watcher's existing ones.
- The Planned "Replay the journal" card is replaced by the section.

### Tests

- Unit: point mapping (located, 0,0, out of range, previous per lane, ordering), the leg filter, the state parsing.
- Server (stage 2): the track endpoint against real Postgres, including role-hidden location, the filter, lanes, the cap, and a case with no location columns.
- e2e: the section opens, draws a trail for a stubbed key, says how many are unlocated, an impossible leg is marked with a word and not colour only, and axe is clean in both themes. The map itself is canvas, so assertions go through the legend and the counts, as the Watcher page's tests do.
- Docs: the test catalogue rows, the agent tool entries and the endpoint in the hierarchy documents.

### Stages

1. `meta.location` on the rows endpoint, with tests (server).
2. The journal source and point mapping, with unit tests (client).
3. The Replay section embedding the Watcher, the replay-specific filters, the unlocated line, e2e and axe.
4. The Track endpoint and agent tool, switching the source to it when the view is larger than the loaded rows.
5. Drawer and grid links in both directions.

## Decisions for the owner

- **D1. Who may see a location.** Option A (recommended): the journal offers replay only when the two location fields are among the payload columns the user's roles can already see, so nothing new is exposed, and says so otherwise. Option B: the server reads the model's location fields regardless of the user's column roles, on the argument that the Watcher already shows activation locations to anyone with that permission.
- **D2. Stage 1 first, or straight to the server track.** Recommended: stage 1 first, because it is cheap, replays exactly the grid, and fixes the point shape before the endpoint is designed.
- **D3. Placement.** A separate Replay section between Journal and Network (recommended), or inside the journal card above the grid.
- **D4. Default limit.** Keep 900 km/h and 50 km as the improbable-travel test, with the controls to change them, or set per workflow.
- **D5. Lanes.** Each lane draws its own trail and colour (recommended, so a merged device shows where it was against where the account was), or one trail for the whole view.

## Decisions taken and what is built

Owner's decisions (2026-10-26): D1 option A (a location is shown only when both columns are ones the user's roles can see); D2 stage 1 first; D3 a separate Replay section, and because of that the replay has a filter of its own, a standalone widget, so the user can narrow to, say, only card present payments without touching the journal; D4 keep 900 km/h and 50 km; D5 each lane its own trail, with the lanes belonging to the replay and independent of the journal.

What changed in the plan because of D3 and D5: the replay is independent of the journal's state, so stage 1 no longer reads the journal's loaded rows. It has its own window, its own merged keys and its own filter, held in the address as `rw`, `rm` and `rf` (the journal keeps `jw`, `jm`, `jf`), and fetches its own rows from the existing rows endpoint (payload lens, up to 1,000 rows). The shared filter component `JournalFilterBuilder` is the standalone filter widget: it now takes a `name` (so two can live on a page without clashing) and its own help text.

Built:

- Server: `meta.location` on the rows endpoint, `{ latitude, longitude }` naming the two payload columns, or null. The engine's rule is followed: the model's request XPaths of data type 6 and 7, matched to the Case Workflow XPaths the caller can see (`$.payload.Name` in any of its spellings). Null when either is missing or hidden. Tests: `JournalLocationTests` and a service test with a hidden column.
- Client: `cases/replay` with `state.ts` (address state), `points.ts` (rows to watcher points: ordered by time, previous located point per lane, each lane its own entity, unlocated rows left for the map to count, the journal's colours and highlight reasons carried) and `ReplaySection.vue` (window, keys, filter, the count and the Watcher embedded with the replay as its source, live off, history on, all points, trails on, rule chart off).
- The case page has a Replay section between Journal and Network, collapsed by default. The "Planned: Replay the journal" card is gone.
- A race fixed on the way: `DataGrid` lost a column change that arrived while Tabulator was still building (a saved column layout arriving late did not apply). It now re-syncs the columns when the table is built.

Not yet built: the speed and distance controls beyond the Watcher's own (defaults kept), "improbable legs only", the server `Track` endpoint for more than 1,000 rows, links between the grid, the map and the drawer, and the saved replay below.

## Saved replay for the case narrative (to decide)

The owner thinks a replay should be savable to help the case narrative. Proposed: a per-case, shared record like a pin (`CaseKeyJournalReplay`, soft delete, a small cap per case, refused while the case is locked to another user), holding the definition (window, merged keys, filter) and a snapshot of the findings at the time it was saved: how many transactions were drawn and unlocated, the number of impossible legs with the worst one (from, to, kilometres, hours, speed), and the lanes. Compose reads these findings as facts, as it reads pins, so the narrative can say "three legs exceeded 900 km/h, the worst 1,420 km/h between London and Lagos in 4 hours" with the numbers computed rather than written by the model. Questions: whether to keep the definition only (recomputed when read) or also the snapshot (frozen, auditable), and whether the saved replay should also be shown to a reviewer as the map. The snapshot needs the stage 4 server computation to be trustworthy beyond 1,000 rows.

## Track endpoint and saved replays (built)

Owner's answers (2026-10-26): the saved replay is just its settings, worked out on demand, and available to the narrative; a reviewer sees the map the same way, by recreating it from the settings.

- **Track endpoint.** `GET /api/CaseKeyJournal/Track` (`CaseKeyJournalTrackService`, tool `CaseKeyJournalTrackGet`) reads the view in time order, up to 20,000 transactions that pass the filter on the Archive, from at most 200,000 read, with the same `ArchiveFilter` as the journal and Tag all matching (see `CaseKeyJournalArchiveFilterPlan.md`). The payload columns the caller cannot see are not read, and the location is reported only when both columns are visible. Each point carries the lane, the elevation, the activations and the bold line and cell format highlights, plus `Latitude` and `Longitude`, null when the transaction is not located. The server also computes the movement (`JournalTravel`): the haversine distance, the hours and the speed between consecutive located transactions of each lane, a leg being impossible at 50 km or more and faster than 900 km/h, and an instant leg having no speed to name. The summary holds the leg and impossible counts and the five fastest impossible legs. `SummaryOnly` returns just that.
- **What the map uses.** The Replay section now draws from the track, so it is no longer capped at the grid's 1,000 rows and the filter is applied on the server. The rows query remains for the filter's columns and the location. Counts and the worst leg come from the track's summary.
- **Saved replays.** `CaseKeyJournalReplay` (migration 20263108060000, soft delete) holds, per case: a name, the window in days (7, 30, 90, 365), the merged keys and the filter, as the same JSON the server evaluates. Nothing of the movement is stored. `CaseKeyJournalReplayService`: list, save (up to 10 a case, refused while the case is locked to another user), delete, and `CaseKeyJournalReplaySummaryGet`, which recreates the replay from its settings (the window counted back from the start of today, UTC) and returns the track summary, so the case narrative can quote computed numbers such as the worst leg. The Replay section can save the current settings with a name, list, open and delete saved replays.
- **Agent.** A new specialist, `case-journal-replay` (Zbyshko), under the Investigation Supervisor, holds the five tools (the other two journal specialists were full or near full at 15). Save and delete need confirmation.
- **Not done:** the speed and distance controls and "improbable legs only" (the defaults of 900 km/h and 50 km are kept, as decided); the links between the map, the grid and the drawer; showing the narrative the map itself rather than its numbers; truncation is reported but the 20,000 cap is not exercised by a test.

## Speed and distance controls (built)

The two limits that decide what counts as movement that could not have happened, 900 km/h and 50 km by default, can now be changed on the Replay section: a speed limit (1 to 10,000 km/h) and the shortest leg that can be impossible (0 to 20,000 km). They sit in the address as `rs` and `rd` (written only when they differ from the defaults), are clamped to the range the server accepts, and have a Reset limits button and a tooltip. Changing them redraws the map (the Watcher takes `travelLimitKmh` and a new `minimumKilometres`) and refetches the track, whose summary is computed on the server with the same limits, so the count of impossible legs and the worst leg always agree with the trails. They are saved with a replay (`LimitKmh` and `MinimumKilometres`, null meaning the default, migration 20263108070000) and brought back when it is opened, and the narrative's `CaseKeyJournalReplaySummaryGet` uses them, so a stricter replay keeps counting legs the stricter way. An out-of-range value is refused by the server (`TravelLimitInvalid`).

## Improbable legs only (built)

A switch on the Replay section keeps only the legs that are impossible at the speed limit and shortest leg set there, together with the transactions at either end of each, and hides every ordinary journey. Each key keeps its own trail, a transaction that ends one impossible leg and starts another is kept once, and a transaction with no usable location is never an end. The legs are worked out in the browser from the points the track returned, with the same distance and speed rule as the map (`onImpossibleLegs` in `cases/replay/points.ts`), so the switch can never disagree with the trails the Watcher draws. A line says how many transactions are shown, or that no leg breaks the limits, which is an answer in itself. It is kept in the address (`ro`) and saved with a replay (`ImpossibleOnly`, migration 20263108080000), so a reviewer who opens the replay sees the same thing. The track summary and the narrative's numbers are not affected by it: they always count every leg.

A server-side version (the track returning just the impossible legs) was considered and not built, because the track already returns the points and the browser holds up to 20,000 of them without trouble.

## Links between the map, the grid and the drawer (built)

- **Grid to map.** When the key can be located (the rows endpoint reports `meta.location`), each row of the journal grid has a Map action beside Open. It opens the Replay section if it is collapsed, scrolls to it and selects that transaction on the map, flying to it. The Replay section is independent of the journal, so if the transaction is outside the replay's own window, filter or limit it is simply not found and nothing is selected.
- **Drawer to map.** The transaction drawer has a Show on map button, shown under the same condition, doing the same for the open transaction.
- **Map to drawer.** Selecting a point on the map, by clicking it or from the feed, shows a bar under the controls naming the transaction (its time and which key's trail it is on) with an Open transaction button. It opens the Journal section if it is collapsed, puts the transaction in the address (`jt`) so the drawer opens, and scrolls there.
- **How the map finds a transaction.** A point's id ends with the transaction's Guid, so the Watcher takes a `focusSuffix` and selects the point whose id ends with it as soon as the points arrive, and the Replay section matches the selected point back to the track's row to learn the journal Id. The page owns the opening and scrolling of the two sections, because it owns which sections are open.
- **Row mark in the grid.** The journal grid marks the row of the transaction open in the drawer and the row of the transaction selected on the map, with an inset bar and an outline in the primary colour (a shape, not only a colour) and `aria-current` on the row, and moves to the page that holds it and scrolls it into view when the mark changes. `DataGrid` takes a `mark` (a field and the values to mark), so any grid can use it; the page carries the map's selection to the journal.
