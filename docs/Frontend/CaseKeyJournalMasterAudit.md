# Case Key Journal: audit against master

Compares the Kendo journal on master (`Jube.App/wwwroot/js/Case/Case.js`, `GetCaseJournalQuery`, `SessionCaseJournal`) with the Vue journal on this branch. Findings only; nothing here is built yet.

## Summary

| # | Topic | Master | This branch | Verdict |
|---|---|---|---|---|
| 1 | Conditional colours (cell, row, bold line) | Works, client-side after data bound | Server computes `CellFormat` and `BoldLine` on the Payload lens, nothing in `Jube.Vue` reads them | Missing. Server half done, client half absent |
| 2 | Column filtering | Kendo filter menu per column, typed operators, Excel export honours it | Tabulator with no header filters; only the one external `filterField` | Missing |
| 3 | Column move, resize, hide, saved per user | `reorderable`, `columnMenu`, resize; layout saved to `SessionCaseJournal` per user per workflow | `DataGrid` has no movable columns and no persistence; the `SessionCaseJournal` endpoint exists but the journal does not call it | Missing |
| 4 | (not raised) | | | |
| 5 | Colours in the side panel | Not on master | Not present | New |
| 6 | Lanes beyond D | Not on master | Hard-wired to A to D: `JournalMerge.MaximumLanes = 3`, `laneLetters = ['A','B','C','D']` | Limit is arbitrary, easy to lift |
| 7 | Checkbox or switch for column headers | Kendo column menu checkboxes | Switches (`FeatureSwitch`, daisyUI `toggle`) are the settings idiom; checkboxes are row selection and filters | Use switches |

## 1. Colours from configuration

Configuration lives on `CaseWorkflowXPath`: `ConditionalRegularExpressionFormatting`, `RegularExpression`, `ConditionalFormatForeColor`, `ConditionalFormatBackColor`, `ForeRowColorScope`, `BackRowColorScope`, `BoldLineMatched`, `BoldLineFormatForeColor`, `BoldLineFormatBackColor`.

Master behaviour, to be reproduced exactly:

- **Cell format.** A row gets a `CellFormat` entry when the regular expression matches that XPath's value. Scope decides what is painted: both row scopes true paints the whole row with both colours; back scope only paints the row background and the cell text colour; fore scope only paints the row text colour and the cell background; neither paints just the cell.
- **Bold line.** The row carries a `BoldLine` entry when the XPath has `BoldLineMatched`. It applies, in bold, only when the row's value for that key equals the value of the same key in the transaction the case was raised on (`ResponsePayload`). This is how rows sharing, for example, the same device as the triggering transaction stand out.
- Colours are free-form CSS colour strings typed by an administrator.

This branch: `JournalLensReader` already emits `CellFormat` and `BoldLine` (payload lens only), so the server half exists and is unchanged from master. Gaps:

- The client never reads them.
- The server does not send the triggering transaction's value, so the bold line comparison cannot be made. The cheapest fix is to resolve it server side and send `BoldLineMatched: true` per row.
- Other lenses carry no formatting. Decision needed: payload lens only, or follow the key into every lens.
- Free-form colours conflict with the product rule that colour carries state only and passes 4.5:1 contrast in both themes. Proposal: apply the configured colours as given (it is the administrator's choice), but add a word or icon for state, and compute readable text over a configured background when no foreground is set.

## 2. Column filtering, and the watcher

Master offers Kendo's filter menu on every column, with operators by type (text: contains, equals, starts, ends, empty, null; number and date comparisons). Tabulator supports header filters natively. Proposal: a filter model held in the URL like the other journal state, applied client side to the loaded rows, with the same operator set. Filters must also drive the exports, the tag-all matcher (`JournalMatcher` already takes a filter on the server) and the strip and charts.

Watcher: `watcher/source.ts` already defines a `WatcherSource` with `replay` and an optional `stream`, so a journal source is a second implementation that maps filtered journal rows to plots. That fits the plan to have one component fed by whatever the journal currently shows, so the filters, lanes and window decide what is replayed. It needs a position per row, which is the open question already in the design TODOs.

## 3. Column order and saved layout

Master saves the full Kendo column array (field, title, width, hidden, order) to `POST /api/SessionCaseJournal` on hide, show, resize and reorder, and reads it back with `GET /api/SessionCaseJournal/ByCasesWorkflowGuid/{guid}`. Columns new since the save are appended; columns that no longer exist are dropped; `BoldLine`, `CellFormat` and `Tag` are hidden by default.

This branch: the endpoint and table are unchanged and usable. Missing: movable columns, a column chooser, and the save and restore. One wrinkle: the Vue journal has lenses, each with a different column set. Proposal: save one layout per lens inside the same JSON document, keyed by lens, so nothing about the table changes.

## 5. Colours in the side panel

The transaction drawer shows values by group, not by configured column. To carry colours into it, the drawer needs the same `CellFormat` and `BoldLine` entries for the open row, and applies them to the value the colour belongs to. Row scope colours would tint the drawer header.

## 6. Lanes

The cap is the constant `MaximumLanes = 3` plus a four-letter alphabet and four colour classes. Lifting it means a longer alphabet, a palette that stays distinguishable (past about six lanes colour alone stops working, so the letter badge must carry it), and a higher server cap with a sensible ceiling to protect the key lookup, because each lane is another containment query. Proposal: 8 lanes (A to H), colours reused cyclically, letter always shown.

## 7. Checkboxes or switches

The product uses switches for settings (`FeatureSwitch`, daisyUI `toggle`, 16 files) and checkboxes for selecting rows and for filters. Column visibility is a setting, so the column chooser should use switches.

## Proposed order

1. Column state: movable, chooser with switches, per-lens saved layout through `SessionCaseJournal`.
2. Column filters with the shared filter model.
3. Colours: client rendering of `CellFormat` and `BoldLine`, server-resolved bold line match, drawer colours.
4. Lanes to 8.
5. Watcher fed by the filtered journal.

## Owner decisions (2026-10-25)

- **Colours.** Apply the configured colours and always give away the reason (a word or icon saying which rule or value caused it), so colour is never the only signal. The bold line match is resolved on the server: a row arrives already knowing whether it matched, and why.
- **Filtering is lifted out of the grid.** One journal filter drives the grid, strip, charts, exports, Tag all matching and the watcher, so it is a shared model, not a Tabulator header feature. The filter is edited in a dedicated builder, like the case filter builder, because optionality and cascade are hard to present as per-column menus. The existing journal state will need some redesign to route everything through the one filter.
- **Lanes** work well as they are; the lift beyond four lanes stays on the list but is not urgent.
- **Side panel.** Liked; some cosmetic snags to be collected.

## Design for the shared filter

- **Model.** A group of rules (and/or, nesting) over journal columns, held as JSON in the URL state (`jf`). Each rule is column, operator, value, typed by the column (text, number, date, boolean, tag, lane, activation).
- **One evaluator.** A pure function in TypeScript applies the model to rows for the grid, strip, charts and watcher. The same model is sent to `JournalMatcher` on the server for Tag all matching and exports, with an equivalent C# evaluator. A shared test table of rule, row and expected result runs against both so they cannot drift.
- **Builder.** `JournalFilterBuilder` reuses `FilterBuilderIsland` (jQuery QueryBuilder) with the journal's columns as the filter list, the same operator set as master (contains, equals, starts, ends, empty, null, and the comparisons), plus journal-specific filters (has activation, pinned, lane, tag). Opened from a Filter button beside the lens tabs, with the active rules summarised as removable chips so the state is visible without opening it.
- **Cascade.** The builder works on the unfiltered loaded rows, so counts on the lens tabs, the strip and charts always describe the filtered set, and the watcher plots exactly the filtered rows.
- **Open.** Whether the analytics (medians, stands-out) should be computed over the filtered set or stay over the full window; the safe default is the full window with the filtered count shown beside it, because filtering to the unusual rows would otherwise make them look normal.

## Next step

Column state first (movable columns, chooser with switches, per-lens saved layout through `SessionCaseJournal`), then the shared filter, then colours.

## Shared filter: built, and what is left

Built (client side): `filter.ts` holds the model (a QueryBuilder group), the pure evaluator and the description; `JournalFilterBuilder.vue` is the dialog; the filter lives in the address as `jf`. It drives the grid, Select all, the exports and the count. Operators: equals, not equals, contains, does not contain, begins, ends, empty, not empty, null, not null, and the four comparisons for numbers and dates; booleans are yes or no; tags are searched as a list. Text is compared without regard to case.

Decisions taken on the way:

- The analytics (strip, charts, peers, stands-out) keep the whole window, and the filter bar shows how many of the loaded transactions remain. The lens tab counts also stay on the whole window, because the other lenses' rows are not loaded.
- A rule on a column the current lens does not have sees a missing value, so it matches only the negative and null operators. The filter belongs to the lens being viewed.
- Tag all matching honours the filter: the server applies the same filter, so the count in the dialog and the job agree with the grid. Select all and tagging selected rows also work on the filtered set.

Server evaluator (built): `JournalFilter` in `Jube.Service/Query/CaseKeyJournal` parses the same JSON (the rule group plus the kind of each column used, up to 100 rules, 6 levels and 20,000 characters) and applies the same semantics as the browser. `docs` and tests share one table of cases, `Jube.Vue/src/cases/journal/__tests__/filterCases.json`, run by both the Vitest suite and `JournalFilterTests`, so the two evaluators cannot drift. Tag all matching sends the filter, the lens and the case, the job stores them (`FilterJson`, `FilterLens`, `FilterCaseId`, migration 20263108050000), and the runner rebuilds each matched transaction as the journal does, through the new `JournalRowBuilder`, so the filter sees exactly the columns the grid shows. Lane letters are worked out first when a filter refers to the lane, and pinned transactions are read when it refers to Pinned. Only the columns the user's roles may see are readable, because the payload columns come from the role-filtered XPath list.

Known differences between the evaluators: a date typed without a zone is read as UTC on the server and in the browser's local zone in JavaScript, and a number typed in a form JavaScript accepts but .NET does not (such as hex) is not a number on the server. The builder only offers numeric input for number columns, so neither arises in practice.

Left to do:

- **Watcher.** It will consume the filtered rows through a second `WatcherSource`.
- **Saved filters.** Not requested yet; the model would serialise into `SessionCaseJournal` like the layout.
