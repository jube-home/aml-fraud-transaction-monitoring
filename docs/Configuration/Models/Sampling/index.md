---
layout: default
title: Sampling
nav_order: 21
parent: Models
grand_parent: Configuration
---

# Sampling

<!-- TODO(docs): every screenshot on this page is of the Razor UI. In the Vue UI the sample download is a card on the model's Operate area (Models story page), not a Models > Sampling menu: retake all of them and rewrite the navigation sentence. -->

The Sampling page is a lightweight CSV export tool that extracts a representative sample of data for a given date range
and flattens out all model elements to a CSV format for independent ingestion into analytical tools such as R.

Sampling takes the following steps:

* A query is predicated on a defined random selection between a given date range.
* The query returns the JSON from the Archive table.
* All model elements, such as Payload, Abstraction etc, are extracted from the JSON via their JSONPath reprentations,
  before being transposed to a CSV representaton as their datatype (for example, strings are enclosed in double quotes).
* The CSV, including header record, is exported as a file stream, and downloade by the browser.

Navigate Models >>> Sampling:

<!-- TODO(screenshot): file=sampling-card.png route=/models/story state="Sample card for one model: from, to, rate slider at 2%, Download button" -->
<!-- TODO(docs): the Vue card has no model dropdown (the model is the one open on the story page), a default window of the last day and a default rate of 2%. Verify and rewrite the next paragraphs. -->

![LocationOfSampleMenu.png](LocationOfSampleMenu.png)

Select the model to construct the sample for (important as parsing of the Archive JSON varies remarkably by model
configuration):

![LocationOfModelDropdown.png](LocationOfModelDropdown.png)

The date filtering is performed based on the Reference Date and not the Created Date in the Archive. Select the Date
Range
using the Date Time Picker:

![LocationOfDateOptions.png](LocationOfDateOptions.png)

The sample is the percentage within the date range selected using a random digit in the underlying query. Use the slider
control to select the sample coverage, which is 100% as follows (all records):

![LocationOfSampleSlider.png](LocationOfSampleSlider.png)

Click the Download button:

![LocationOfDownloadButton.png](LocationOfDownloadButton.png)

On clicking of download the query will be run in the background, and a CSV file downloaded:

![Download.png](Download.png)

![FileInCalc.png](FileInCalc.png)

This file handles nesting from the model objects with a . seperator in the header and is otherwise a compliance string
escaped CSV which can be ingested into analytical tooling such as R.

## Limits and safeguards

- The download needs permission 40. A model that is not in the caller's tenant, or has been deleted, is reported as not
  found.
- The window is on the Reference Date, the end must be after the start, and the rate is between 0 and 1.
- At most 100,000 archive rows are read per download, and the archive rows are streamed, one at a time, into the
  file, which is built in memory. The file may not grow past `SampleMaxBytes` (32 MiB by default); a sample that would
  is refused with a message asking for a narrower date range or a lower rate, rather than being cut short, so a file
  that downloads is always complete up to the 100,000 row limit.
  <!-- TODO(docs): owner to confirm 32 MiB and 100,000 are the right defaults. Arithmetic: an archive row carries the whole payload and every derived value (commonly several kilobytes of JSON), so 100,000 rows are hundreds of megabytes of JSON, several times that once parsed, per request; a CSV row holds only the model's own columns. -->
- A text cell that starts with `=`, `+`, `-`, `@`, a tab or a carriage return and is not a number is prefixed with a
  single quote so that a spreadsheet does not evaluate it as a formula.
- Each download, successful or not, is recorded in the Entity Analysis Model Sample Execution Log with the window, the
  rate,
  the number of rows and the error when there was one.
- The query reads the reporting database when `ReportConnectionString` is set, otherwise the main database.
  <!-- TODO(docs): link to the ReportConnectionString environment variable entry. -->
- A payload field whose JSONPath uses a regular expression filter (`=~`) is stopped after 250 milliseconds and the cell
  is left empty or zero, so a pattern cannot hold the download up.
