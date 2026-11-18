---
layout: default
title: Generate a Template in Visualisation Datasource
nav_order: 7
parent: Visualisations
grand_parent: Configuration
---

# Generate a Template in Visualisation Datasource

For display that is not a grid, chart or map, the Template kind lays the rows out in markup written by the user. It uses a small dialect borrowed from Vue, and is drawn in the server, which is to say that the template is checked as it is typed, and the result is sent as one response with the rest of the tile. There is no template engine in the browser, and no script written into a template is ever executed.

## Where it lives

A template is chosen in the Show as step of the Datasource builder, described in [Embed SQL and Generate Grid in Datasource](../EmbedSQLGenerateGridInDatasource/index.html), by switching on Show a chart and selecting Template as the Chart kind. An editor labelled Template is then presented in the place of the column roles. A template error is reported beneath the editor with its line and column, and the preview waits until the template is valid.

{% comment %}SHOT id=visualisations-template-editor
     route=/administration/visualisations
     steps=open-row:ExampleVisualisation, open-hub:Datasources, open-row:ExampleList, scroll-to:Show as
     frame=record
     state=the Show as step of the builder with Template selected as the Chart kind and the example list template in the Template editor, with the Preview step beneath drawing the list of rule names and their frequencies, the larger frequencies in the error colour
     conveys=a template is markup written beside the SQL, and the tile is drawn from it in the server as it is typed
     alt=The Template editor of the Datasource builder holding a short list template, with the preview beneath showing each activation rule name alongside its frequency{% endcomment %}

## Example

Assuming the following SQL:

```sql
select "ActivationRuleName", "Frequency" from "ExampleCaseVolumeEntry"
where "ActivationRuleName" LIKE @Name
```

Take the following template:

```html
<ul class="list">
  <li v-for="row in rows">
    <span class="font-semibold">[@ row.ActivationRuleName @]</span>
    <span class="[@ row.Frequency > 100 ? 'text-error' : 'text-secondary' @]">
      [@ row.Frequency | number @]
    </span>
  </li>
</ul>
<p v-if="truncated">Showing the first [@ rowCount @] rows.</p>
```

The list item is repeated for each row returned, the value is coloured by the condition, and the note appears only when the result was cut off.

## What a template can contain

| Element    | Description                                                                                                                                                                                                                                                                                                                                                             |
|------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Tags       | A fixed list of safe tags: `div span p h1` to `h6`, `ul ol li`, `strong em b i small`, `br hr`, `table thead tbody tfoot tr th td caption`, `section article header footer`, `dl dt dd`, `time code pre abbr mark sub sup`. `<template>` may wrap a `v-for` or `v-if` without adding a tag. Anything else, such as `script`, `style`, `a`, `img`, `iframe` or `svg`, is refused. |
| Attributes | `class title role scope colspan rowspan datetime lang dir`, and `aria-` and `data-` attributes. Written plainly, or with a token in the value (`class="badge [@ row.Status @]"`). A value that is exactly one token leaves the attribute off when the result is null or false. The old colon binding (`:class`) is refused. `id`, `style`, links and event handlers are refused. |
| Values     | `[@ expression @]` in text or in an attribute value. Every value is escaped, always, so row data can never add markup. Names, columns and filters are not case sensitive.                                                                                                                                                                                              |
| Repeating  | `v-for="row in rows"` or `v-for="(row, index) in rows"`.                                                                                                                                                                                                                                                                                                                |
| Choosing   | `v-if`, `v-else-if` and `v-else`. `v-else` and `v-else-if` must follow an element with `v-if` or `v-else-if`, and `v-for` and `v-if` cannot share an element (wrap the one in `<template>`).                                                                                                                                                                            |
| Classes    | The styles of the platform, through `class`, as described below.                                                                                                                                                                                                                                                                                                        |

### Classes

The platform's own styles are available through `class`, from a fixed set that is always included in the stylesheet of the application:

* The text colours that are safe on the page: `text-base-content`, `text-primary`, `text-secondary`, `text-error` and `text-info`.
* The surface colours `bg-base-100`, `bg-base-200` and `bg-base-300`.
* Borders and rounding.
* Text sizes `text-xs` to `text-3xl`, font weights, `font-mono` and `tabular-nums`.
* Spacing from 0 to 8 (`p-`, `px-`, `py-`, `m-`, `gap-`, `space-y-` and the like).
* Flex and grid layout with up to four columns.
* The components `badge`, `alert`, `card`, `stats`, `stat`, `list`, `divider`, `kbd`, `status`, `progress`, `table`, `menu`, `timeline` and `steps` with their usual modifiers.

Any other class is kept on the element but has no styling.

## What an expression can use

Names in scope are `rows` (each row an object keyed by the column names), `columns` (each with `name`, `ordinal` and `type`), `rowCount`, `truncated` and loop variables. Any other name is an error, so typing mistakes are found as the template is written.

Expressions can use numbers, text in quotes, `true`, `false` and `null`; members with `row.total`, `row['order total']` for names with spaces, `rows[0]` and `.length`; `! - * / % + -`; the comparisons `< <= > >= == !=`; `&&` and `||`; the conditional `a ? b : c` and brackets. A missing member, an absent row or a division by zero gives nothing rather than an error. There are no function calls and no assignment.

Filters follow a pipe symbol and format the value in the language of the page:

| Filter            | Result                                                             |
|-------------------|--------------------------------------------------------------------|
| `number(n)`       | The number with thousands separators and `n` decimal places        |
| `percent(n)`      | A fraction as a percentage, so 0.25 is 25%                         |
| `currency('GBP')` | The code and the amount to two places, for example `GBP 12,500.50` |
| `date`            | The date, for example `20 Oct 2026`                                |
| `dateTime`        | The date and time in UTC, for example `20 Oct 2026 10:30`          |
| `round(n)`        | The number rounded to `n` places                                   |
| `truncate(n)`     | The text cut to `n` characters with an ellipsis                    |
| `upper`, `lower`  | The text in capitals or lower case                                 |
| `default(x)`      | `x` when the value is missing or empty                             |
| `join(sep)`       | The items of an array joined by the separator                      |

## Limits

| Limit                                   | Value     |
|-----------------------------------------|-----------|
| Characters in a template                | 65,536    |
| Depth to which elements nest            | 24        |
| Depth to which an expression nests      | 32        |
| Steps taken to render                   | 100,000   |
| Characters of output                    | 1,000,000 |

A step is taken for each element, each piece of text and each iteration of a loop. Reaching a limit gives a message stating what to narrow, usually the rows returned. Keeping in mind that a tile reads at most 5,000 rows, a loop over the rows inside another loop over the rows will arrive at the limit on steps.

## Validation

A template that is empty, that is longer than 65,536 characters, or that does not compile is refused upon saving the Datasource. While the template is being written, the first error is given beneath the editor in the language of the user interface, taking the form of the line and column followed by the reason, for example that a tag is not allowed, that a tag is never closed, that a name is not known, or that a filter does not exist.

Should a template fail upon recall, the tile presents the error in its place, and the grid is still shown where it is switched on.

## Permissions and security

Writing a template is part of authoring a Datasource, and requires permission 33, Read Write Visualisation Datasource. Recall is governed as described in [Visualisations Security Policy](../SecurityPolicy/index.html).

The template is treated as untrusted on both sides. The server compiles only the tags, attributes and expressions set out above and escapes every value, and the tile is then drawn from the HTML the server sends, with the browser removing anything outside the list above again before showing it.
