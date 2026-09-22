---
layout: default
title: Geospatial and Context
nav_order: 8
parent: Rule Taxonomy
---

# Geospatial and Context

**Definition:** physical distance, travel, or boundary. Use it for anything location based.

**Construction:** `Double` geospatial extensions directly on `Payload` coordinates for a single comparison, aggregated
through an Abstraction Rule when comparing against a longer term pattern. See the [Rule Taxonomy](../RuleTaxonomy.html).

| Question an analyst asks                                              | Rule Taxonomy type that answers it                                                |
|-----------------------------------------------------------------------|-----------------------------------------------------------------------------------|
| Is this payment within 5 km of the customer's home?                   | Geospatial: `IsWithinRadiusKm`                                                    |
| Could the customer physically have travelled between the two events?  | Geospatial: `ImpliedTravelSpeedKmh`                                               |
| How many different countries in 24 hours?                             | Not this type: [Cardinality](Cardinality.html)                                    |
| Is the transaction at an unusual time of day?                         | Context (below): `DateTime` extensions                                            |

## Geospatial: Single Comparison Between Two Points

Coordinates are `Double` payload values, so the geospatial steps are available on them directly. The value the pipeline
starts on is the first latitude, and the arguments are the first longitude, the second latitude, the second longitude,
then the limit, in that order. See the [Rule Pipeline Reference](../RulePipelineReference.html). Further than 500 km from a fixed
point, such as a branch:

```vb
Matched = Payload.Latitude.MatchDistanceKmAbove(Payload.Longitude, 51.5074, -0.1278, 500)
```

A boundary test against a radius, using `OutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm)`:

```vb
Matched = Payload.Latitude.MatchOutsideRadiusKm(Payload.Longitude, Payload.HomeLatitude, Payload.HomeLongitude, 50)
```

`WithinRadiusKm` and `DistanceKmBelow` are the inverses. Each step has a miles form (`WithinRadiusMiles`,
`OutsideRadiusMiles`, `DistanceMilesAbove`, `DistanceMilesBelow`) taking the same arguments. The distance is great circle,
and is not a road distance.

## Geospatial: Impossible Travel

Travel between two events needs the earlier event's coordinates and the elapsed time.
`ImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour)` tests whether the implied speed
exceeds a plausible ceiling:

```vb
Matched = Payload.Latitude.MatchImpliedSpeedAbove(Payload.Longitude, Payload.PreviousLatitude, Payload.PreviousLongitude, Payload.HoursSincePrevious, 900)
```

The previous coordinates and elapsed time must be in the payload, either supplied by the sender, produced by an Inline
Function, or retrieved from history by an Abstraction Rule using the `Actual Value` Function Type with an Offset of Last.
The Abstraction route is the aggregated form described above: it compares the current event with a longer term pattern
held in the cache, at the cost of a cache retrieval per Search Key. See
[Abstraction Rules](../../Configuration/Models/AbstractionRules/index.html). Zero elapsed hours is treated as infinite speed, so two events at
different places at the same instant match. `ImpliedSpeedBelow` is the inverse.

## Context: Time and Calendar

Context rules use `DateTime` steps on payload dates, for example whether an event is at a weekend, outside business
hours, the end of a month, or older than a number of days. These are cheap, need no cache, and are best consumed as an input to
another type, for example a large transfer combined with an out of hours event:

```vb
Matched = Payload.TransactionDate.RequireIsWeekendInZone(Payload.CustomerTimeZone).Against(Payload.CurrencyAmount).MatchGreater(10000)
```

The steps are `IsWeekday`, `IsWeekend`, `IsMorning`, `IsAfternoon`, `WithinBusinessHours(startHour, endHour)`,
`OutsideBusinessHours`, `IsToday`, `IsPast`, `IsFuture`, `OlderThanDays`, `YoungerThanDays`, `WithinDaysOf`, `DayOfWeekIn`,
`IsStartOfMonth` and `IsEndOfMonth`, with `HourOfDay()` and `AgeInDays()` as transformers. The plain extension methods
have different names (for example `IsWeekendDay`).

A customer in another timezone experiences a different hour, so a business hours or weekend test should be made in the
customer's timezone. The `InZone` forms read the value as UTC, convert it to the named time zone (an IANA or Windows
identifier such as `America/New_York`), and then test, and an unknown zone fails the test:

```vb
Matched = Payload.TransactionDate.MatchOutsideWeekdayBusinessHoursInZone(Payload.CustomerTimeZone, 8, 18)
```

The `InZone` steps are `WithinBusinessHoursInZone`, `OutsideBusinessHoursInZone`, `WithinWeekdayBusinessHoursInZone`,
`OutsideWeekdayBusinessHoursInZone`, `IsWeekdayInZone` and `IsWeekendInZone`, and the `Weekday` forms without a zone use
the value's own clock, so a weekend counts as outside hours. `ToZone(zoneId)` converts the value and continues the
pipeline, so that any other `DateTime` test can be made in local time:

```vb
Matched = Payload.TransactionDate.ToZone(Payload.CustomerTimeZone).MatchIsMorning()
```

The value is read as UTC, so the payload date must be UTC. Without an `InZone` form or `ToZone`, the test reasons in the
clock of the value as supplied. Age and tenure derived this way are the cross cutting Identity and Demographic
variables described in the [Rule Taxonomy](../RuleTaxonomy.html).

## Geospatial and Context: Common Faults

Symptoms of a misbuilt Geospatial or Context rule, with the likely cause.

| Symptom in a Geospatial or Context rule         | Likely cause                                                                                                        |
|-------------------------------------------------|---------------------------------------------------------------------------------------------------------------------|
| Distances are wildly wrong                      | Latitude and longitude arguments are swapped, or the coordinates are in a projection other than decimal degrees.    |
| The impossible travel rule fires constantly     | Elapsed time is in the wrong unit (hours are required), or the previous location is stale.                           |
| The business hours rule is wrong for customers  | The test is not an `InZone` form or `ToZone`, so it reasons in the supplied clock; or the payload date is not UTC; or the zone identifier is unknown, which fails the test. |
| The rule does not match on missing coordinates  | A null or `NaN` coordinate fails the test, so the rule does not match. Decide whether that is the outcome wanted.   |
