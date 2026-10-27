---
layout: default
title: Embed SQL and Generate Grid in Datasource
nav_order: 4
parent: Visualisations
grand_parent: Configuration
---

# Embed SQL and Generate Grid in Datasource

Datasources are the tiles inside a Visualisation. At their core each is an SQL statement executed against the reporting
database and presented as a grid, a chart, a map or a template, or any combination of a grid with one of the others.

All of it is drawn on the server. The browser asks for a datasource and receives one response holding everything the
tile needs: the chart already drawn as an SVG document, the rows when the grid is switched on, the state of the tile
and the stylesheet the chart uses. Nothing is drawn by scripts in the browser, and there is no chart configuration to
write.

## The SQL

As introduced beforehand, the following SQL is to be executed:

```sql
select * from "ExampleCaseVolumeEntry"
where "ActivationRuleName" LIKE @Name
```

The tokenization is performed on the basis of the Visualisation Registry Parameters created, and matched on the basis
of the name. A token such as `@Name` is only replaced when the Visualisation Registry has a Parameter of exactly that
name (letters are case sensitive, and spaces in a Parameter name become underscores in the token); an undefined token
is left in the SQL, which the database then refuses, for example with `column "name" does not exist`.

The SQL must be a single `SELECT` statement: statements that write, several statements, functions that sleep or read
the server, the system catalogues and the identity and credential tables are refused, and the statement runs in a read
only transaction limited to 30 seconds (see Software Validations). A tile reads at most 5,000 rows. When the result is
larger the tile says it was truncated, so aggregate or filter in SQL rather than draw thousands of points.

Every column needs its own name: give each an alias, for example `AS "Total"`. Two columns with the same name are
refused.

## The Datasource builder

A Datasource is created beneath its Visualisation Registry. The SQL, the chart and the grid are edited together in one
builder laid out as four steps down a single page:

1. **SQL command.** Type the statement. It is checked as you type, a moment after you stop, against the reporting
   database, and the status beside the editor says whether the command is valid, and if not, repeats the database's
   own message and its SQLSTATE code (never the statement itself) together with the line and column of the problem, which
   is also underlined in the editor, for example
   `The reporting database rejected the SQL command: column "missing" does not exist (42703).` SQL that the safety check
   refuses (not a single SELECT, or a blocked function or table) is reported as such.
2. **Result columns.** Each column the SQL returns, in order, with its type and the chart role it fills, if any.
3. **Show as.** Switch on a chart, a grid, or both, and choose what the chart is. See the following pages.
4. **Preview.** The tile exactly as the Visualisation will present it. When the Visualisation Registry has Parameters, a
   form above the tile offers them, filled with their defaults, so the tile can be tried with other values by choosing Run.

A Datasource must show a chart, a grid or both, and its SQL must be valid. The preview does not appear until then, and
Save is held back while the Datasource form says it is incomplete.

The Visualisation Registry Datasource accepts the following values:

| Value       | Description                                                                                                                                         |
|-------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| Name        | The title of the tile. Unique within the Visualisation Registry.                                                                                    |
| Active      | Whether the Datasource takes part in the Visualisation.                                                                                             |
| Locked      | A locked Datasource cannot be edited or deleted.                                                                                                    |
| Priority    | The order of presentation of the datasource tile, lowest first.                                                                                     |
| Column Span | Given the number of columns specified in the Visualisation Registry, the number of those columns that should be taken up by this datasource tile.   |
| Row Span    | Given rows are unbounded, with the height of the rows specified in the Visualisation Registry, the number of rows that the tile should overlay.      |
| SQL         | The SQL to be executed, with Parameters added with the @ symbol.                                                                                    |
| Show a grid | Include a grid containing the full dataset returned by the SQL (up to 5,000 rows).                                                                  |
| Show a chart| Draw the result as a chart, map or template. Choose the kind and, where needed, which columns fill each role.                                       |

Saving creates a version of the Datasource. On save the SQL is tested again and the columns, with their data types, are
recorded, and the chart column assignments are checked against the columns the SQL really returns.

## The grid

With only the grid switched on the tile shows the response of the SQL as a grid filling the tile. Numbers are right
aligned, dates are shown in the language of the page, flags are shown as Yes or No and columns holding arrays are shown
as comma separated lists. The grid can be sorted, filtered and exported, and pages when there are many rows.

When a chart is switched on as well, the chart takes the top of the tile and the grid appears beneath it. The reason to
have both in the same tile is to reuse the SQL and present a chart's raw data alongside the chart. When the grid is
switched off none of the rows are sent to the browser.

Given the use of parameter queries and the validation of SQL on save, the prospect of errors during execution is
minimised (although execution performance is available, with each datasource execution being logged).
