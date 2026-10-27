---
layout: default
title: Visualisation Recall
nav_order: 1
parent: Visualisations
grand_parent: Configuration
---

# Visualisation Recall

Jube provides a rudimentary reporting tool that exists to execute SQL against the database and present the result as a
grid, chart, map or template. The philosophy is to make the creation of reports quick given a rudimentary understanding
of SQL, with the presentation drawn by Jube rather than assembled by hand.

A Visualisation is a canvas of tiles, each of which is a Datasource. The Visualisation Registry sets the number of
columns, the width of each column and the height of each row, and every Datasource says how many columns and rows its
tile spans and in which order the tiles come. On a narrow screen the tiles stack one above the other.

## Opening a Visualisation

Visualisation Registry entries toggled to show in the Visualisation Directory are listed there, under Visualisation in the menu for users allowed to recall Visualisations. Choosing one opens it at its own address, which can be bookmarked or shared.
Only active Visualisations and active Datasources are shown, and only those the signed in user's role has been allocated.

## Parameters

When the Visualisation Registry has active Parameters, they are shown as a form above the tiles, filled with their default
values:

| Data type | Input                    | Note                                                                                       |
|-----------|--------------------------|--------------------------------------------------------------------------------------------|
| Text      | A text box               |                                                                                            |
| Integer   | A number box             | Whole numbers only                                                                         |
| Decimal   | A number box             |                                                                                            |
| Date time | A date and time picker   | The default is a number of days, which becomes that many days before now                   |
| Flag      | A switch                 | The default is 1 or true for on                                                            |

A Parameter can be given in the address to start with a value other than its default, by adding its name and the value to the address as in `?Name=West`.

A Parameter marked as required must not be left blank. Choose **Run** to draw the tiles. A Visualisation with no
Parameters runs as soon as it opens.

Each tile is requested separately with the values entered, so the tiles are drawn as each completes, and a tile that
fails, for example because a value is not a whole number or the SQL is refused, says why in its own place while the
others are drawn. Every execution is logged, with its parameter values and how long it took.

## Dashboards

There are as many tiles as there are active Datasources, which allows for the creation of dashboards. Charts and maps
fill their tile, and where a Datasource also shows a grid, the grid appears beneath its chart.

## Case Visualisation

A Visualisation may be allocated to a Case Workflow, to produce a visual consolidation of a case, which is oftentimes
referred to as a dashboard. The Parameters of such a Visualisation are matched from the case payload instead of being
entered. The Case page does not currently show the Visualisation panel; it returns with the next version of case
management.
