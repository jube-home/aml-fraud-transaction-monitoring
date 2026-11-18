---
layout: default
title: Generate Map in Visualisation Datasource
nav_order: 6
parent: Visualisations
grand_parent: Configuration
---

# Generate Map in Visualisation Datasource

The Map is one of the chart kinds, drawn in the server in the same manner as the others. It is a world map of country outlines, held inside Jube, hence no external map service is called and nothing is sent outside the installation.

## Where it lives

A map is chosen in the Show as step of the Datasource builder, described in [Embed SQL and Generate Grid in Datasource](../EmbedSQLGenerateGridInDatasource/index.html), by switching on Show a chart and selecting Map as the Chart kind. A map is drawn from the columns in one of two ways, as points or as countries, and the builder recognises which from the names of the columns.

## Points

Points are plotted on the map by returning a latitude and a longitude, in degrees. Columns named `lat` or `latitude`, and `lon`, `lng`, `long` or `longitude`, are found by name wherever they sit in the result. Otherwise they are assigned in the roles, as described in [Generate Chart in Visualisation Datasource](../GenerateChartInDatasource/index.html). Both are required together.

```sql
select "Latitude", "Longitude", "Sum" as "Value", "City"
from "ExampleGeoCoded"
```

A numeric Value makes each point a bubble sized by the magnitude, an optional Label names the point (the City in the example above), and a Series column splits the points into coloured groups with a legend.

## Countries

Whole countries are coloured by returning a country, as an ISO two or three letter code or a name, and a numeric value to colour them by:

```sql
select "CountryCode" as "Country", count(*) as "Value"
from "ExampleCases"
group by "CountryCode"
```

Columns named `country`, `iso`, `iso2`, `iso3` or `nation` are found by name. The twelve countries with the highest values are labelled, and a legend shows the colour scale.

## Roles

| Role      | Description                                                              | Found by name                        |
|-----------|--------------------------------------------------------------------------|--------------------------------------|
| Latitude  | The latitude of a point, in degrees.                                     | `lat`, `latitude`                    |
| Longitude | The longitude of a point, in degrees.                                    | `lon`, `lng`, `long`, `longitude`    |
| Country   | An ISO country code or a country name.                                   | `country`, `iso`, `iso2`, `iso3`, `nation` |
| Value     | A numeric value, sizing a point or colouring a country.                  |                                      |
| Label     | A text label naming a point.                                             |                                      |
| Series    | A column whose distinct values split the points into coloured groups.    |                                      |

## Validation

A map plots points or countries, not both, and the builder states as much when the columns do not fit:

* A map of points needs both a latitude and a longitude column.
* A map needs either a latitude and a longitude column, or a country column.
* A map plots either points or countries, not both.
* A map of countries needs a numeric value column to colour them by.

## Recall

The map is drawn to the width of the tile and its height follows the proportions of the world map. When the grid is switched on as well, the rows are listed beneath, so the same query shows where and how much.

{% comment %}SHOT id=visualisations-map-points-tile
     route=/administration/visualisations
     steps=open-row:ExampleVisualisation, open-hub:Datasources, open-row:ExampleMap, scroll-to:Preview
     frame=record
     state=the Preview step of the builder for a Datasource over the ExampleGeoCoded table with Map as the Chart kind, drawing the world map of country outlines with bubbles sized by value at each latitude and longitude
     conveys=a map is drawn in the server from a latitude and a longitude, with a value sizing each point
     alt=A world map of country outlines with bubbles of differing sizes plotted at the locations returned by the SQL{% endcomment %}

## Permissions and security

The Map is subject to the same permissions as any other chart, which is to say permission 33, Read Write Visualisation Datasource, to author, and recall as described in [Visualisations Security Policy](../SecurityPolicy/index.html). As the map is held inside Jube, the locations returned by the SQL are not disclosed to any third party in drawing it.
