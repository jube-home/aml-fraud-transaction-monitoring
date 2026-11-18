---
layout: default
title: Reprocessing Instance
nav_order: 3
parent: Reprocessing
grand_parent: Configuration
---

# Reprocessing Instance

A reprocessing request, henceforth an instance, is an entry made in the database requesting that the engine perform reprocessing as per the specification of a [Reprocessing Filter](../ReprocessingFilters/index.html). The filter is the container for its instances, and it must be available, and Active, before a request is made of it.

Only one uncompleted reprocessing request may exist for a filter at any given point in time, and a new request cannot be created until any unprocessed request moves to a Completed or Failed state. A second request for a filter that already has an uncompleted one is refused as a conflict.

## Where it lives

{% comment %}TODO(verify): the Vue user interface on this branch presents the Reprocessing hub card and the form of a Reprocessing Filter, yet has no Reprocess button, no grid of instances beneath the filter and no control to delete an instance: EntityAnalysisModelReprocessingRuleInstance is not registered as a child of the filter, and nothing under Jube.App/Vue/src calls /api/EntityAnalysisModelReprocessingRuleInstance. Owner to decide whether the instances surface is to be restored (the permissions for it are already mapped in permissions/areaOperations.ts); this section then needs the names of the controls, and a shot of the instances of a filter is to be requested.{% endcomment %}

Instances are created, read and deleted through the API, or through the Ask Jooby tool for the same purpose. A request is made by posting the identifier of the filter, every other value being assigned by the server:

```http
POST https://localhost:5001/api/EntityAnalysisModelReprocessingRuleInstance/ByExistingUpdateUncompleted
Content-Type: application/json

{
  "entityAnalysisModelReprocessingRuleId": 1
}
```

The instance is created in the status Not Allocated. The instances of a filter are read by `GET /api/EntityAnalysisModelReprocessingRuleInstance/ByEntityAnalysisModelReprocessingId`, a single instance by `GET /api/EntityAnalysisModelReprocessingRuleInstance/{id}`, and an instance is deleted, which will also stop it, by `DELETE /api/EntityAnalysisModelReprocessingRuleInstance/{id}`.

{% comment %}TODO(verify): confirm against a running system the casing of the request body and the name of the query string parameter taken by ByEntityAnalysisModelReprocessingId, and add both to the examples above.{% endcomment %}

## Status

The reprocessing job has the following processing steps:

| Name          | Description                                                                                                                                                       |
|---------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Not Allocated | The request has not been picked up by the reprocessing engine.                                                                                                    |
| Allocated     | The request has been picked up by the reprocessing engine.                                                                                                        |
| Initial Count | The initial count has been completed, to establish created date range and universe of records.                                                                    |
| Processing    | The request has begun processing data.                                                                                                                            |
| Completed     | The request has completed.                                                                                                                                        |
| Failed        | The request could not run: the filter rule does not parse or compile, its interval is not valid, or an unexpected error stopped it. A new request can be created. |

In the API the status is returned as a number, from 0 for Not Allocated through to 4 for Completed and 5 for Failed. Where more than one request is waiting for a model, that of the filter with the lowest Priority is picked up first.

## Fields

The instance is updated as reprocessing takes place, and has the following values, all of which are assigned by the server:

| Name            | Description                                                                                                   |
|-----------------|---------------------------------------------------------------------------------------------------------------|
| Status          | The current status of the reprocessing job.                                                                   |
| Created Date    | The date and time the reprocessing request was created.                                                       |
| Started Date    | The date and time the reprocessing engine started working the request, being empty until it is allocated.     |
| Reference Date  | The current date and time being processed in that moment. This is the reference date specified in the model.  |
| Available Count | The total number of records in the universe as brought back during the initial counts.                        |
| Sampled Count   | The total number of records that have been sampled for the purposes of reprocessing.                          |
| Matched Count   | The total number of records that have been matched (after being sampled) for the purposes of reprocessing.    |
| Processed Count | The total number of records reprocessed so far.                                                               |
| Completed Date  | The date and time that the job completed.                                                                     |
| Error Count     | The number of records in error observed during reprocessing.                                                  |

The counts are read as a funnel, each being drawn from the one before. By way of example, suppose a filter with a Reprocessing sample of 50, a window of one day and a rule matching payments above a certain amount, requested of a model whose archive holds 280 records in that window. The Available Count settles at 280 upon the status reaching Initial Count. As the status moves to Processing the Sampled Count rises towards about 140, being half of the records, and the Matched Count towards however many of those the rule returns True for, say 37, each of which is run through the model again and written back to the archive. The Reference Date advances from the oldest record of the window to the newest as the run proceeds, which is the plainest sign of progress, and the Error Count remains at 0 unless a record fails. Upon Completed, the 37 archived records carry the results of the model as it is configured now, each at a version one higher than before, and the remaining 243 are untouched.

Upon the status reaching Completed it can be seen that the reprocessing job has finished, which will allow a new reprocessing request to be created for the filter.

## How reprocessing reads and updates the archive

The manner in which a request is carried out is as follows:

| Aspect                           | Behaviour                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
|----------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| The window                       | The newest reference date in the model's archive is found, and the filter's interval is counted back from it. Every archived record from that start up to and including the newest reference date is read.                                                                                                                                                                                                                                                                              |
| Records archived during the run  | Only records archived before the request started are read. Transactions that arrive while reprocessing runs are not picked up, so the run has a fixed end.                                                                                                                                                                                                                                                                                                                              |
| The order                        | Records are read oldest first, by reference date and then by entry, a page at a time (`ReprocessingBulkLimit` records per page). Each page continues exactly after the last record of the one before, so every record in the window is read once, however many records share a reference date, and a later page is read as quickly as an early one.                                                                                                                                     |
| The sample                       | The filter's sample is a percentage: 100 considers every record, 50 about half, 0 none.                                                                                                                                                                                                                                                                                                                                                                                                  |
| What happens to a match          | A sampled record that the filter rule matches is run through the model again. The new results of a page's matches are then written to the archive together, in one transaction: each record is updated in place, its previous version is kept in the archive's version history, its version is increased, and it is stamped with the reprocessing request. Only archive keys whose value changed are rewritten (with a version kept); keys the new result no longer produces are removed, for that record only, and keys that did not change are left as they are. |
| The cache                        | Reprocessing does not change the cache. A transaction's cached payload, its search key journals and the latest transaction for each search key value belong to the transaction as it arrived, and a re-run changes none of them, so none are rewritten; neither are the latest reference date, TTL counters or the archive write-ahead log. The only trace is the cache's own record of which journals were read.                                                                       |
| Errors                           | A record whose filter rule or re-run fails is counted in Error Count, not in Matched Count, and the run carries on. If a page's archive write fails, or a matched record's archive row has been deleted in the meantime, those records are counted as errors too.                                                                                                                                                                                                                      |
| Stopping                         | Deleting the request stops it within one record once the progress is next written (every 10 seconds); a deleted request stays deleted and is not marked completed.                                                                                                                                                                                                                                                                                                                      |

## Interrupted runs, restart and removal

A request that is interrupted is dealt with on the following basis, keeping in mind that starting again is always safe: the archive is updated in place, keys are replaced for the same request, and nothing else is written, so no record is counted twice.

| Event                           | Behaviour                                                                                                                                                                                                                                                             |
|---------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Shutdown                        | If the service is stopped while a request is running, the request goes back to Not Allocated and starts again from the beginning of its window when the service is next up.                                                                                           |
| Crash                           | A request that shows Allocated, Initial Count or Processing, and whose progress has not moved for 30 minutes, is treated as abandoned and goes back to Not Allocated. Progress is written at least every 10 seconds while records are being processed, so a live run is never taken over. |
| Unexpected error                | The request is marked Failed, not left running, so it does not block a new request for the filter.                                                                                                                                                                    |
| Editing or deleting the filter  | Saving a new version of a filter, or deleting it, deletes its uncompleted requests (a running one stops, a waiting one never starts). Completed and Failed requests are kept.                                                                                         |

The number of records read per page is set by the ReprocessingBulkLimit Environment Variable, which is 10,000 by default. A value that is not a positive whole number falls back to 10,000 and anything above 100,000 is held at 100,000, so memory use on a very large archive is bounded.

{% comment %}TODO(verify): owner to confirm the 30 minute abandonment window and whether it should be a setting.{% endcomment %}
{% comment %}TODO(verify): the history of finished requests is attached to the filter version they ran under, so after the filter is edited the instances of the new version start empty (existing behaviour of versioned filters); owner to confirm this is acceptable.{% endcomment %}

## Behaviour across volume

`Jube.Tests/Volume/ReprocessingVolumeTests.cs` (category `Service`) seeds the archive of a synchronised copy of the example model, reprocesses it with a rule that matches about 5% of records, and checks that every record is read exactly once, that exactly the matches are updated and updated once each, that every page but the last is a full page, and that memory stays bounded. It seeds 100,000 records by default, which `JubeReprocessingVolumeRows` raises for a deeper run.

What it deliberately does not do is assert a throughput. It is a functional test that happens to need volume to be meaningful, as paging, exactly-once processing and bounded memory are only interesting over more rows than fit in one page, and a records-per-second floor measured on whatever hardware happens to be running says very little about the code. The previous floor of 2,000 records a second demonstrated the point: on a four-core runner the test recorded 741, and the test's own stage breakdown showed why, with sanctions screening taking 34.5 ms of each of the 4,914 re-invocations. The number being measured was sanctions latency, not reprocessing throughput. Performance belongs in a suite of its own, run on hardware whose capacity is known, rather than in a correctness gate where it can only produce false alarms.

On a development machine, 1,000,000 records were read and 48,677 matches re-run and updated in 1 minute 25 seconds (about 11,800 records a second, 570 re-runs a second), in 200 pages of 5,000, with pages read in about 45 ms from first to last and the heap growing by 74 MB. Writing each match's archive record in its own transaction took more than twice as long (3 minutes 40 seconds); writing a page's matches together removed most of that cost.

## Permissions and security

Three permissions govern reprocessing, for the purposes of allowing a role to run a filter without being allowed to change it:

| Permission                      | Number | Allows                                                                                                                                                     |
|---------------------------------|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Read Write Reprocessing Rule    | 26     | Create, edit, delete, validate, parse, test and build the rule of a filter. Editing or deleting a rule supersedes it and cancels its uncompleted requests. |
| Trigger Reprocessing Run        | 73     | Trigger a run (create a request, validate one, update one).                                                                                                |
| Stop or Delete Reprocessing Run | 74     | Delete a request, which stops it if it is running.                                                                                                         |

Reading (list, open, and the SQL tables behind them) is allowed to a role holding any one of the three, so a role that may only trigger can see the filters and the runs it is triggering. Roles that held 26 before the split were given 73 and 74 by the upgrade, so nothing they could do changed. Granting any of the three follows the same rule as every other permission: a caller can only grant, or give a role carrying, a permission the caller holds.

Everything is limited to the caller's own tenant, and a request, its status, its counts and its dates are assigned by the server, which is to say that values supplied by a client are ignored. A second request for a filter that already has an uncompleted one is refused (conflict), through the API and through the Ask Jooby tool alike.

{% comment %}TODO(verify): owner to confirm that permission 26 can stop a run indirectly by editing or deleting its rule, and that this is acceptable.{% endcomment %}
