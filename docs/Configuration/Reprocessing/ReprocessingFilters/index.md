---
layout: default
title: Reprocessing Filters
nav_order: 2
parent: Reprocessing
grand_parent: Configuration
---

# Reprocessing Filters

<!-- TODO(docs): all routes in the screenshot markers on the Reprocessing pages are guesses (Models story page); confirm the real routes -->

Reprocessing can demand an overwhelming amount of data and it is extremely important that data be filtered as much as
possible.

Start by navigating to Models >> Reprocessing:

<!-- TODO(screenshot): file=model-reprocessing-reprocessing-top-of-tree.png route=/models/reprocessing state="Reprocessing tree with the Transaction model" -->
![Image](model-reprocessing-reprocessing-top-of-tree.png)

Click on the upper left hand side tree entry showing the Transaction model to add a new Reprocessing filter and
container:

<!-- TODO(screenshot): file=model-reprocessing-empty-reprocessing-rule.png route=/models/reprocessing state="empty Reprocessing Rule form" -->
![Image](model-reprocessing-empty-reprocessing-rule.png)

The Rule constructor has the following limited collections available for filtering:

| Name    | Description                                                                                                  | 
|---------|--------------------------------------------------------------------------------------------------------------|
| Payload | The fields created in the Request XPath page.  The processing will take place as if the transaction was new. |

The reprocessing filter is comprised of the following parameters:

| Value                       | Description                                                                                                                         | Example |
|-----------------------------|-------------------------------------------------------------------------------------------------------------------------------------|---------|
| Reprocessing Sample         | The percentage of sampling to take place in the reprocessing, from 0% (nothing) to 100% (everything).                               | 100%    |
| Reprocessing Interval Type  | The date interval to subtract from the newest archived reference date to arrive at a threshold to process data from.                | d       |
| Reprocessing Interval Value | The date interval value interval to subtract from the newest archived reference date to arrive at a threshold to process data from. | 1       |

The interval type is Minutes (n), Hours (h), Days (d) or Months (m), and the value is counted back from the newest
reference date in the model's archive, not from today.
<!-- TODO(docs): the validator accepts n, h, d, m; the engine also understands s (seconds) and y (years) but they cannot be saved; confirm the screen offers only the four -->

The Reprocessing Rule name is required and limited to 256 characters, and must be unique within the Model. The
Builder or Coder rule script (whichever surface is selected) is required and limited to 65,536 characters.

Complete the page as above parameters or as below:

<!-- TODO(screenshot): file=model-reprocessing-all-approved-one-day-full-sample.png route=/models/reprocessing state="rule All Approved, one day, full sample" -->
![Image](model-reprocessing-all-approved-one-day-full-sample.png)

Scroll down and Click Add to create a version of the filter:

<!-- TODO(screenshot): file=model-reprocessing-created-version-of-reprocessing-rule.png route=/models/reprocessing state="rule saved, empty instances grid" -->
![Image](model-reprocessing-created-version-of-reprocessing-rule.png)

The reprocessing filter will now have been created. The reprocessing request collection is a grid containing all
reprocessing requests made for this filter, which will be empty at this point.