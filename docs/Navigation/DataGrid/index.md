---
layout: default
title: Data Grids
nav_order: 8
parent: Navigation
---

# Data Grids

<!-- TODO(docs): owner to read and finalise this whole page; drafted from code only (PayloadGrid.vue, DataGrid.vue, GridExportButtons.vue, GridModelEndpoints.cs, GridModelRegistry.cs), not from the running UI. -->

Most list screens that show monitoring, performance, audit and payload data are drawn by one grid. The grid is described
by the server, not by the page: the browser asks `GET /api/GridModel/{area}` for the columns, filters, search box,
default sort, row cap and time window, then calls the area's own list endpoint for the rows.

<!-- TODO(screenshot): file=data-grid-payload-grid.png route=/administration/performance state="a payload grid with the From and To window, one select filter, search box, Apply, Clear, Refresh and the Excel and CSV buttons" -->
![Payload grid](data-grid-payload-grid.png)

## Filters

- **From and To**: shown only when the area has a time field. The default window is set per area (`windowMinutes`). The
  values are sent to the server as
  UTC. <!-- TODO(docs): confirm the default window per area and the format shown to users in each locale. -->
- **Select and lookup filters**: choose one value, or All. A lookup filter reads its choices from another endpoint.
- **Search**: free text, trimmed; an empty search is not sent.
- **Apply** sends the filters. **Clear** restores the defaults. **Refresh** repeats the last request. Pressing Enter in
  a field applies.

## Row cap and statistics

The server returns at most the area's `take` rows (default 2000, the list endpoints clamp the value they receive). When
the total is larger than the rows returned the grid says so, for example "2000 of 51234 rows shown; narrow the window or
filters to see the rest". <!-- TODO(docs): confirm wording and that the cap is the same on every area. -->

Where the area returns summary statistics, a collapsible Statistics panel shows min, max, mean, median, standard
deviation and a small distribution per numeric column; opening the full chart draws a histogram on the server as SVG.

<!-- TODO(screenshot): file=data-grid-statistics.png route=/administration/performance state="Statistics panel open with the distribution column and one histogram opened" -->
![Statistics](data-grid-statistics.png)

## Sorting and paging

Click a column heading to sort. The sort is sent to the server (only columns the server whitelists are accepted) and
also applied to the rows shown. The pager shows 10, 25, 50 or 100 rows per page (100 by default), first, previous, next
and last. In right-to-left languages the pager arrows are
mirrored. <!-- TODO(docs): confirm mirrored arrows against the running UI in Arabic. -->

## Export

**Excel** writes the rows currently in the grid with a styled header, column widths, real numbers and dates, a frozen
header row and a filter. **CSV** writes the same rows, plain, with a byte order mark so Excel opens UTF-8 correctly.
Text cells that begin with `=`, `+`, `-`, `@`, a tab or a carriage return are written with a leading apostrophe in the
CSV so a spreadsheet does not run them as a formula; in the Excel file such cells are stored as text. Export is done in
the browser, from the rows already loaded, so it exports the capped rows, not the full
table. <!-- TODO(docs): confirm the file names (area name .csv and .xlsx) and that the buttons are disabled when there are no rows. -->

<!-- TODO(screenshot): file=data-grid-export.png route=/administration/performance state="Excel and CSV buttons, and an opened CSV in a spreadsheet showing an apostrophe-prefixed cell" -->
![Export](data-grid-export.png)

## Languages

Column headings, filter labels and area titles are translated on the server when a translation rule exists for the
language, and stay English
otherwise. <!-- TODO(docs): state which areas have Polish, Arabic and Georgian headings once the server resources are filled. -->
Right-to-left languages mirror the layout. Numbers, dates, IP addresses and identifiers inside Arabic rows are
left-to-right text. <!-- TODO(docs): verify against the running UI. -->

## Charts

Charts are drawn by the server as SVG and shown in light or dark to match the theme. The chart text (axis titles) is
currently English in every language. <!-- TODO(docs): remove this sentence when the server chart text is localised. -->

## Value lists

The Values grid for lists and dictionaries (add, edit, delete, approve, reject, resubmit, retract, with maker-checker
states) is a separate
widget. <!-- TODO(docs): ValueGrid belongs with the maker-checker documentation; decide where it is documented. -->

<!-- TODO(docs): docs/Navigation/ReportTable/index.md is claimed by this slice in slices.json but documents Archive Key and SQL, not the grids. Decide its owner; its screenshots are Razor-era. -->
