---
layout: default
title: Reprocessing Filters
nav_order: 2
parent: Reprocessing
grand_parent: Configuration
---

# Reprocessing Filters

Reprocessing can demand an overwhelming amount of data and it is extremely important that data be filtered as much as possible.

A Reprocessing Filter, which the user interface and the API name a Reprocessing Rule, is both the filter and the container for the reprocessing requests made of it. It specifies how far back in the Archive to read, what proportion of the records to sample, and a rule that a sampled record must match before it is run through the model again.

## Where it lives

Navigate through the menu to Models, open the model, then the Reprocessing hub card. Creating, changing and deleting a Reprocessing Filter follows the common manner described in [Child Objects](../../../Navigation/ChildObjects/index.html), with two particulars:

* A Reprocessing Filter is not subject to approval, and the Approval section is not presented for it.
* A change to a Reprocessing Filter supersedes it rather than updating it in place, which is to say that the changed filter is given a new identifier. Saving a change, or deleting the filter, cancels any of its reprocessing requests that have not completed, as described in [Reprocessing Instance](../ReprocessingInstances/index.html).

{% comment %}TODO(verify): the Reprocessing Rule DTO carries no approval and states that an update assigns a new Id, which departs from the common manner of Child Objects (approval, and update in place with a copy in a Version table). Confirm both on a running system, including that the story remains on the superseded record's successor after Save changes.{% endcomment %}

{% comment %}SHOT id=reprocessing-filters-record
     route=/models
     steps=open-default-model, open-hub:Reprocessing, open-first-row
     frame=record
     state=a Reprocessing Filter named All Approved opened beneath the default model, showing the Identity group, the Rule group with a single condition in the Builder, and the Reprocessing group with the sample at 100, the value at 1 and the interval at Days
     conveys=a Reprocessing Filter is a rule over the payload together with a sample and a window counted back from the newest archived record
     alt=A Reprocessing Filter record showing its name and switches, a rule built in the Builder, and beneath it a sample slider, an interval value and a choice of Minutes, Hours, Days or Months{% endcomment %}

## The rule

The rule is written in the Builder or the Coder, in the manner of other rules. The Rule constructor has the following limited collections available for filtering:

| Name    | Description                                                                                                 |
|---------|-------------------------------------------------------------------------------------------------------------|
| Payload | The fields created in the Request XPath page. The processing will take place as if the transaction was new. |

The rule returns True where the archived record is to be reprocessed, and only records that have first been sampled are tested against it.

## Fields

The form is arranged in the groups Identity, Rule and Reprocessing. The reprocessing filter is comprised of the following parameters:

| Value                 | Description                                                                                                                                                                                             | Example      |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|
| Name                  | The name of the Reprocessing Filter. Required, limited to 256 characters, and unique within the Model without regard to case.                                                                           | All Approved |
| Active                | Whether requests made of the filter are picked up by the reprocessing task. Active is off for a new Reprocessing Filter and must be switched on, as a request of an inactive filter is never allocated. | True         |
| Priority              | The relative order in which waiting requests are picked up for a model, where the request of the filter with the lower value is picked up first.                                                        | 0            |
| Rule                  | The rule, in the Builder or the Coder, as above.                                                                                                                                                        |              |
| Reprocessing sample   | The percentage of sampling to take place in the reprocessing, from 0% (nothing) to 100% (everything). It is 100 for a new filter.                                                                       | 100          |
| Reprocessing value    | The date interval value to subtract from the newest archived reference date to arrive at a threshold to process data from.                                                                              | 1            |
| Reprocessing interval | The date interval type to subtract from the newest archived reference date to arrive at a threshold to process data from: Minutes, Hours, Days or Months. It is Days for a new filter.                  | Days         |

The interval is counted back from the newest reference date in the model's archive, not from today. In the API the interval type is given as a letter, being Minutes (n), Hours (h), Days (d) or Months (m). The engine also understands Seconds (s) and Years (y), yet they are not offered by the form and cannot be saved.

## Validation

A Reprocessing Filter is validated in the server, and is rejected where:

* The Name is empty, longer than 256 characters, or already in use within the Model.
* The Priority is negative.
* The rule of the surface selected, Builder or Coder, is empty or longer than 65,536 characters. Only the rule of the surface selected is required.
* The Reprocessing sample is not between 0 and 100.
* The Reprocessing value is negative, or beyond the largest value permitted for the interval type.
* The Reprocessing interval is not one of the four offered.

## Example

A filter to reprocess every record archived in the day leading up to the newest record, in full, would be completed as follows:

| Value                 | Example                         |
|-----------------------|---------------------------------|
| Name                  | All Approved                    |
| Active                | True                            |
| Rule                  | A condition upon the Payload matching the records of interest. |
| Reprocessing sample   | 100                             |
| Reprocessing value    | 1                               |
| Reprocessing interval | Days                            |

Upon the filter being created, its collection of reprocessing requests is empty, and a request is made of it as described in [Reprocessing Instance](../ReprocessingInstances/index.html).

## Permissions and security

Creating, changing and deleting a Reprocessing Filter, including validating, parsing, testing and building its rule, requires permission 26, Read Write Reprocessing Rule. Reading the filters is allowed to a role holding any one of permission 26, permission 73, Trigger Reprocessing Run, or permission 74, Stop or Delete Reprocessing Run, such that a role that may only trigger a run can see the filter it is triggering. The Models menu item is presented to a role holding any one of the three.
