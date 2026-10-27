---
layout: default
title: Generate a Template in Visualisation Datasource
nav_order: 7
parent: Visualisations
grand_parent: Configuration
---

# Generate a Template in Visualisation Datasource

For display that is not a grid, chart or map, the **Template** kind lays the rows out in markup you write. It uses a
small dialect borrowed from Vue, and is drawn on the server: the template is checked as you type, and the result is
sent as one response with the rest of the tile.

Switch on **Show a chart** in the Datasource builder, choose **Template** and write the markup. A template error is
reported beside the editor with its line and column, and the preview waits until the template is valid.

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

The list item is repeated for each row returned, the value is coloured by the condition, and the note appears only when
the result was cut off.

## What a template can contain

* **Tags.** A fixed list of safe tags: `div span p h1` to `h6`, `ul ol li`, `strong em b i small`, `br hr`,
  `table thead tbody tfoot tr th td caption`, `section article header footer`, `dl dt dd`, `time code pre abbr mark sub sup`.
  `<template>` may wrap a `v-for` or `v-if` without adding a tag. Anything else, such as `script`, `style`, `a`, `img`,
  `iframe` or `svg`, is refused.
* **Attributes.** `class title role scope colspan rowspan datetime lang dir`, and `aria-` and `data-` attributes.
  Written plainly, or with a token in the value (`class="badge [@ row.Status @]"`). A value that is exactly one token
  leaves the attribute off when the result is null or false. The old colon binding (`:class`) is refused. `id`, `style`,
  links and event handlers are refused.
* **Values.** `[@ expression @]` in text or in an attribute value. Every value is escaped, always, so row data can never add markup. Names, columns and filters are not case sensitive.
* **Repeating.** `v-for="row in rows"` or `v-for="(row, index) in rows"`.
* **Choosing.** `v-if`, `v-else-if` and `v-else`. `v-else` and `v-else-if` must follow an element with `v-if` or
  `v-else-if`, and `v-for` and `v-if` cannot share an element (wrap the one in `<template>`).
* **Classes.** The platform's own styles are available through `class`, from a fixed set that is always included in the
  application's stylesheet: the text colours that are safe on the page (`text-base-content`, `text-primary`,
  `text-secondary`, `text-error`, `text-info`), the surface colours `bg-base-100`, `bg-base-200` and `bg-base-300`,
  borders and rounding, text sizes `text-xs` to `text-3xl`, font weights, `font-mono` and `tabular-nums`, spacing from 0 to
  8 (`p-`, `px-`, `py-`, `m-`, `gap-`, `space-y-` and the like), flex and grid layout with up to four columns, and the
  components `badge`, `alert`, `card`, `stats`, `stat`, `list`, `divider`, `kbd`, `status`, `progress`, `table`, `menu`,
  `timeline` and `steps` with their usual modifiers. Any other class is kept on the element but has no styling.

## What an expression can use

Names in scope are `rows` (each row an object keyed by the column names), `columns` (each with `name`, `ordinal` and
`type`), `rowCount`, `truncated` and loop variables. Any other name is an error, so typos are found as you type.

Expressions can use numbers, text in quotes, `true`, `false` and `null`; members with `row.total`, `row['order total']`
for names with spaces, `rows[0]` and `.length`; `! - * / % + -`; the comparisons `< <= > >= == !=`; `&&` and `||`; the
conditional `a ? b : c` and brackets. A missing member, an absent row or a division by zero gives nothing rather than
an error.

Filters follow a pipe symbol and format the value in the language of the page:

| Filter          | Result                                                               |
|-----------------|----------------------------------------------------------------------|
| `number(n)`     | The number with thousands separators and `n` decimal places          |
| `percent(n)`    | A fraction as a percentage, so 0.25 is 25%                           |
| `currency('GBP')` | The code and the amount to two places, for example `GBP 12,500.50`  |
| `date`          | The date, for example `20 Oct 2026`                                  |
| `dateTime`      | The date and time in UTC, for example `20 Oct 2026 10:30`            |
| `round(n)`      | The number rounded to `n` places                                     |
| `truncate(n)`   | The text cut to `n` characters with an ellipsis                      |
| `upper`, `lower`| The text in capitals or lower case                                   |
| `default(x)`    | `x` when the value is missing or empty                               |
| `join(sep)`     | The items of an array joined by the separator                        |

## Limits

A template is at most 65,536 characters, elements nest at most 24 deep, and the render is limited in the number of steps
and the size of its output. Reaching a limit gives a message saying what to narrow, usually the rows returned.

The tile is drawn from the HTML the server sends, and the browser removes anything outside the list above again before
showing it.
