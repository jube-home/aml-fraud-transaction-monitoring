---
layout: default
title: Generate Chart in Visualisation Datasource
nav_order: 5
parent: Visualisations
grand_parent: Configuration
---

# Generate Chart in Visualisation Datasource

In all instances of Visualisation Datasource recall, SQL is executed and returned as a dataset that can be switched to
present as a grid. In the creation of dashboard type visualisations it is likely some form of chart will be shown too.

There is no chart configuration to write. In the Datasource builder, switch on **Show a chart**, choose the **chart
kind** from the list, and the columns the SQL returns are drawn by position. The chart is drawn on the server as an SVG
document, using the same charting library as the rest of Jube, and follows the light and dark theme of the page.

## Columns are taken by position

Each kind of chart has roles, and the columns returned by the SQL fill them in order. For a bar chart the first column
is the category and the following numeric columns are the values, one series per column. Take the following SQL:

```sql
select "ActivationRuleName", "Frequency"
from "ExampleCaseVolumeEntry"
where "ActivationRuleName" LIKE @Name
```

Choose **Bar chart** and the first column labels the bars and the second sets their length, with nothing more to
specify. The list of chart kinds is split into the kinds that can be drawn from the columns as they stand, and the kinds
that need columns assigned. For a kind that cannot yet be drawn the builder says why, for example that it needs a second
numeric column.

The **Result columns** step shows which role each column is filling, so what will be drawn is always visible.

## Choosing columns for roles

When the SQL returns extra columns, for the grid for instance, or the roles need to be filled in a different order,
choose the column for each role. A role that takes one column offers a list headed by the by-position choice. A role
that takes many, such as the values of a column chart, offers the columns to tick. Roles that are optional are gathered
under Optional columns. A column fills at most one role, so assigning it to a role takes it from another.

Columns that are not assigned to a role are not drawn, and are still available in the grid.

## Splitting rows into series

When the SQL returns long form data, with one row for each category and series pair, give one column the **Series**
role. Its distinct values become the series, drawn from one value column:

```sql
select "Month", "Channel", sum("Amount") as "Total"
from "ExampleTransactions"
group by "Month", "Channel"
```

Each pair must appear once. When the result has more than one row for the same pair the chart is refused with a message
asking for the query to be aggregated in SQL, because the chart will not silently add or drop rows.

## Chart kinds

Every kind that can draw from columns is available:

| Kind                                                                              | Roles filled by the columns, in order                                                       |
|-----------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| Column, Bar, Line, Area, Radar                                                     | Category, then one or more numeric Values (one series each), or a Series column             |
| Stacked and percent stacked Column, Bar and Area                                   | As above, stacked, or as shares of the total                                                |
| Pie, Donut, Funnel, Waterfall                                                      | Category, then one numeric Value                                                            |
| Scatter or bubble                                                                  | X, then one or more Y, an optional Size that makes bubbles, an optional Series              |
| Histogram                                                                          | One or more numeric columns, binned to show the distribution                                |
| Heatmap                                                                            | Row category, Column category, then a numeric Value                                         |
| Combination                                                                        | Category, Values drawn as columns, Lines, and optional Lines on a secondary axis            |
| Box plot                                                                           | Category, Minimum, First quartile, Median, Third quartile, Maximum                          |
| Candlestick                                                                        | Category (often a date), Open, High, Low, Close                                             |
| Polar                                                                              | X as the angle in degrees, then Y as the radius, optional Series                            |
| Gauge                                                                              | A Value from the first row, with optional Target, Minimum and Maximum                       |
| Bullet                                                                             | Optional Category, Value, optional Target                                                   |
| Sparkline                                                                          | Value, with an optional Category for order                                                  |
| Treemap                                                                            | Category, Value, optional Parent group                                                      |
| Gantt                                                                              | Task, Start time, End time, optional Series                                                 |
| Map                                                                                | See the next page                                                                           |
| Template                                                                           | Your own markup, see the page after                                                         |

Roles that take numbers accept integers, decimals and flags, and roles that take times accept dates and times; the
builder only offers columns that fit.

## Limits

Charts have sensible limits on how much they will draw, for example the number of categories or points. When a result
is too large the tile says so and by how much, and the remedy is to aggregate or filter in SQL.

## Recall

Navigate to the Visualisation through the Visualisation Directory and run it. Each tile is requested separately, so a
tile that fails shows its own reason while the others are drawn. The chart takes the tile, and when Show a grid is also
on, the grid appears beneath it.

The text of the chart kinds, their roles and any message about a chart is shown in the language of the page, so far in
English, Polish and Arabic.
