---
layout: default
title: Generate Map in Visualisation Datasource
nav_order: 6
parent: Visualisations
grand_parent: Configuration
---

# Generate Map in Visualisation Datasource

The Map is one of the chart kinds, drawn on the server in the same way as the others. It is a world map of country
outlines, held inside Jube, so no external map service is called and nothing is sent outside the installation.

Switch on **Show a chart** in the Datasource builder and choose **Map**. A map is drawn from the columns in one of two
ways, and the builder recognises which from the column names.

## Points

Plot points on the map by returning a latitude and a longitude, in degrees. Columns named `lat` or `latitude`, and
`lon`, `lng`, `long` or `longitude`, are found by name wherever they sit in the result. Otherwise assign them in the
roles. Both are required together.

```sql
select "Latitude", "Longitude", "Sum" as "Value", "Channel"
from "ExampleGeoCoded"
```

A numeric **Value** makes each point a bubble sized by the magnitude, an optional **Label** names the point, and a
**Series** column splits the points into coloured groups with a legend.

## Countries

Colour whole countries by returning a country, as an ISO two or three letter code or a name, and a numeric value to
colour them by:

```sql
select "CountryCode" as "Country", count(*) as "Value"
from "ExampleCases"
group by "CountryCode"
```

Columns named `country`, `iso`, `iso2`, `iso3` or `nation` are found by name. The countries with the highest values are
labelled, and a legend shows the colour scale.

A map plots points or countries, not both, and a country map needs its value column. The builder says so when the
columns do not fit.

## Recall

The map is drawn to the width of the tile and its height follows the proportions of the world map. When the grid is
switched on as well, the rows are listed beneath, so the same query shows where and how much.
