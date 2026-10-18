---
layout: default
title: Render Chart
nav_order: 6
parent: Ask Jooby
---

# RenderChart: drawing from a cached table, and how it reaches the person

`RenderChart` (`Jube.AskJooby/Tools/Charting/RenderChartTool.cs`) is the one AskJooby tool that draws a picture. It
takes a `data_pointer` -- never rows the model typed out itself -- and produces a chart as an SVG file attached
directly to the turn's reply. The model never sees the picture it drew: it gets back a one-line confirmation of what
was plotted, and is told to describe it in words rather than attempt to reproduce it. This page covers the tool
itself, the eight chart types it supports, and precisely how a chart gets from a tool call to the person's screen.
For the backend SVG library that actually draws the picture (`Jube.Charting`: axes, series, hover, no JavaScript),
see `docs/AnalyticalMethodology/Charting.md`, which this page does not repeat.

## What `RenderChart` takes, and what it does with it

```csharp
public async Task<string> RenderChart(
    string dataPointer, string chartType, string categoryColumn, string valueColumns,
    string? title = null, int? bins = null)
```

* **`dataPointer`** -- a pointer already minted by `RunSqlQuery`, `GetDemoDataset`, `CacheAssembledTable`, or
  carried forward from an earlier turn of the same conversation. See `docs/AskJooby/DataPointerCache.md` for the
  full pointer mechanic; `RenderChart` only ever reads a cached table through `cache.GetAsync(dataPointer)` and
  never sees the query, specialist, or reasoning that produced it.
* **`chartType`** -- one of `column`, `bar`, `line`, `area`, `pie`, `donut`, `scatter`, `histogram` (the exact list
  `AskJoobyChartRenderer.SupportedChartTypes` exposes, `Jube.Service/AskJoobyCharting/AskJoobyChartRenderer.cs`).
  An unrecognised value is rejected before anything is drawn, naming the real list.
* **`categoryColumn`** -- the column labelling each point along the bottom (a day, a rule name, a status). For
  `scatter` this is the numeric x-axis instead of a label axis. Ignored, and left empty, for `histogram`, which has
  no category axis at all -- it bins the raw values of the value column(s) itself.
* **`valueColumns`** -- one numeric column, or several comma-separated ones. Several columns overlay several series
  on one chart (several lines, several histograms side by side) -- the tool's own description gives the example of
  comparing a long baseline window against a recent one by eye.
* **`title`** -- optional, becomes both the chart's own title and (sanitised: letters, digits, `-`, `_` and spaces
  only, collapsed and trimmed, capped at 60 characters) the base of the exported file name, `"chart"` falling back
  when it is blank.
* **`bins`** -- histogram only, and optional even there. Left unset, `Jube.Charting`'s own histogram builder chooses
  bin edges from the data, which the tool's description calls "almost always better" than a caller-supplied count --
  the one case worth overriding it is a reporting threshold that a default binning would straddle and hide.

## The eight chart types, and what each actually needs

`AskJoobyChartRenderer.Render` dispatches on `chartType` before ever touching an axis:

* **`column`** (vertical bars) and **`bar`** (horizontal) -- the two safest defaults for "a count or total per
  category." `bar` reads better when category names are long, since the labels run along the axis rather than
  underneath short bars.
* **`line`** and **`area`** -- for something ordered over time or sequence; both accept several `valueColumns` as
  separate series sharing one category axis.
* **`pie`** and **`donut`** -- a handful of parts of one whole. Both take only the *first* name in `valueColumns`
  (`RenderPie` reads `valueColumns[0]`) -- a pie chart has exactly one value series by construction, so naming more
  than one column here draws a chart from only the first and silently ignores the rest.
* **`scatter`** -- plots each named value column against `categoryColumn` as the numeric x-axis, so for a scatter
  chart specifically `categoryColumn` must itself resolve to a numeric column, not a label.
* **`histogram`** -- the one chart type that reads the *raw* values of its value column(s) rather than one point
  per row, works out its own bin edges (or uses `bins` if given), and needs no `categoryColumn`. It tolerates far
  more rows than the other seven: up to `MaximumHistogramValues` (200,000) raw values, against a `MaximumCategories`
  ceiling of 2,000 rows for every other chart type -- a table over that per-type ceiling is refused outright with an
  instruction to aggregate in SQL or filter down first, rather than drawing an unreadable chart of (for example) raw
  transactions where a chart of counts per day was wanted.

Every chart type resolves its named columns against the cached table's real column names (case-insensitively) before
drawing anything, and fails with the table's actual column list if a name does not match -- so a chart never
silently plots the wrong column or an empty one because of a typo. A value column where every row's value fails to
parse as a number is rejected outright, naming the column, rather than drawing a flat line at zero.

Every chart renders at a fixed 760×420 size, in `ThemeMode.Light`, non-interactive (`Interactive(false)`), with
styles inlined onto the SVG itself (`ChartStyleMode.Inline`) rather than referencing an external stylesheet -- this
matters because the SVG is handed to the person as a standalone file, with no guarantee the page it is viewed in
carries the same CSS a live Jube page would.

## The model never sees the picture -- it only gets told what was drawn

`RenderChart`'s own description is explicit and repeated: "You do not get the picture back and cannot see it; once
this returns, just say what the chart shows and let it appear on your reply." The tool's return value is a one-line
summary -- row/column counts and which columns were plotted, or, for a histogram, how many raw values were binned
-- followed by an instruction to describe the chart in words and never attempt to reproduce it as SVG, ASCII art, or
chart markup. This is also why the tool's description tells the model never to read the cached rows first "just in
case": `RenderChart` draws straight from the cache, so the picture always matches the data regardless of whether the
model inspected it, and reading the rows first would only cost tokens for no benefit. `InspectDataSchema` exists for
the one case that still matters -- not knowing the column names to pass as `categoryColumn`/`valueColumns`.

## How the SVG actually reaches the person: the attachment mechanic

A rendered chart is attached to the turn's reply, and a chart drawn by a spawned Joobrik is not redrawn by the agent
that spawned it. The mechanic has four parts, each in a different layer of the stack:

1. **Drawing produces bytes, not a file reference.** `renderer.Render(table, specification)` returns the finished
   SVG as a `byte[]` -- nothing is written to disk by the renderer itself.
2. **The bytes are persisted as a conversation file.** `RenderChart` hands those bytes to `IFileExportClient`
   (`exportClient.ExportAsync(svg, safeName, "image/svg+xml")`), whose concrete implementation,
   `AskJoobyFileExportClient` (`Jube.Service/AskJoobyExport/AskJoobyFileExportClient.cs`), inserts one row into the
   `AskJoobyFile` table -- the raw bytes, the sanitised name, the content type, and the `AskJoobyConversationId` it
   belongs to -- and returns that row's integer id. This is the exact same export path `ExportCachedTableToCsv`
   uses for a CSV download; a rendered chart and an exported CSV are both just rows in `AskJoobyFile`, distinguished
   only by `ContentType`/`Extension`.
3. **The file is recorded against the whole turn, not the one tool call.** `RenderChart` calls
   `exportSink.Record(fileId, safeName)` on the `IExportSink` (concrete type `ExportSink`,
   `Jube.AskJooby/Tools/Export/Interfaces/IExportSink.cs`) it was constructed with. One `ExportSink` instance is
   created once per turn, in `AskJoobyConversationService`, and threaded into `RunContext.ExportSink` for every
   agent and every spawned sub-agent (Joobrik) that runs during that turn -- `ExportSink.Record` is lock-guarded
   specifically because several agents, including ones running concurrently via `SpawnAgents`, can call it during
   the same turn. This is exactly why `RenderChart`'s own description says attachment "is turn-wide, so a chart
   drawn by a spawned Joobrik reaches them without the agent that spawned it redrawing it" -- and why its closing
   instruction is "if one reports back that it has already drawn something, do not draw it again": redrawing the
   same chart would just produce a second, redundant attachment, not a missing one.
4. **Everything recorded on the sink is attached to the saved reply, and the frontend renders `.svg` inline.** Once
   the turn's model loop finishes, `AskJoobyConversationService` reads `exportSink.Files`, turns each
   `ExportedFile(Id, Name)` into an `AskJoobyMessageAttachmentDto`, and both saves them against the assistant
   message (`fileRepository.AttachToMessageAsync`) and streams them down as part of that message's payload. The
   browser side (`AskJoobyWidget.js`/`AskJooby.js`, function `PrependAttachments`/`AskJoobyWidgetPrependAttachments`)
   checks each attachment's file name: anything matching `/\.svg$/i` is fetched from
   `GET /api/AskJoobyConversation/Files/{id}` and rendered inline as an `<img>` (`RenderSvgAttachment`); everything
   else becomes a plain download chip (`📎 <name>`) linking to the same endpoint. A rendered chart is therefore a
   `.svg`-named `AskJoobyFile` row that happens to render inline because of its extension, not because of anything
   `RenderChart` itself does differently from `ExportCachedTableToCsv` -- the branch lives entirely in the frontend.

So the full chain for one `RenderChart` call is: SVG bytes → `AskJoobyFile` row (backend database) →
`ExportSink.Record` (turn-scoped, shared across every agent and Joobrik in the turn) → `AskJoobyMessageAttachmentDto`
on the saved assistant message → streamed to the browser → rendered inline because the name ends in `.svg`. Nothing
in this chain requires the model that called `RenderChart` to still be running, or to be the same agent that started
the turn -- which is exactly the property the tool's own description leans on.

## Cross-reference

* `docs/AnalyticalMethodology/Charting.md` -- the `Jube.Charting` SVG library itself: axes, series, hover behaviour,
  the "no JavaScript" design. This page does not repeat that material.
* `docs/AskJooby/DataPointerCache.md` -- what a `data_pointer` is and how a table gets cached in the first place.
* `docs/AskJooby/SqlQueryTools.md` -- `RunSqlQuery`, the most common way a pointer reaches `RenderChart`.
