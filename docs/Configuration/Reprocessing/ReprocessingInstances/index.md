---
layout: default
title: Reprocessing Instance
nav_order: 3
parent: Reprocessing
grand_parent: Configuration
---

# Reprocessing Instance

To create a reprocessing request, ensure that the Reprocessing Filter created beforehand is available:

Scroll down and locate the Reprocess button:

<!-- TODO(screenshot): file=model-reprocessing-location-of-reprocess.png route=/models/reprocessing state="Reprocessing Rule selected, scrolled to the Reprocess button" -->
![Image](model-reprocessing-location-of-reprocess.png)

By clicking the Reprocess button, an entry will be made in the database requesting the engine perform reprocessing as
per the filter specification.

Only one reprocessing request may exist at any given point in time, and a new request cannot be created until any
unprocessed requests move to a completed state.

Click the Reprocess button:

<!-- TODO(screenshot): file=model-reprocessing-confirmation-of-reprocessing.png route=/models/reprocessing state="confirmation shown after clicking Reprocess" -->
![Image](model-reprocessing-confirmation-of-reprocessing.png)

An entry will be created showing the reprocessing job request:

<!-- TODO(screenshot): file=model-reprocessing-created-reprocessing-instance.png route=/models/reprocessing state="instances grid with one Not Allocated request" -->
![Image](model-reprocessing-created-reprocessing-instance.png)

It is possible to Delete - which will also stop - a reprocessing request by clicking the Delete button:

<!-- TODO(screenshot): file=model-reprocessing-location-of-delete-button.png route=/models/reprocessing state="instances grid, Delete button on an uncompleted request" -->
![Image](model-reprocessing-location-of-delete-button.png)

The reprocessing job has the following processing steps:

| Name          | Description                                                                                                                                                       |
|---------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Not Allocated | The request has not been picked up by the reprocessing engine.                                                                                                    |
| Allocated     | The request has been picked up by the reprocessing engine.                                                                                                        |
| Initial Count | The initial count has been completed,  to establish created date range and universe of records.                                                                   |
| Processing    | The request has begun processing data.                                                                                                                            |
| Completed     | The request has completed.                                                                                                                                        |
| Failed        | The request could not run: the filter rule does not parse or compile, its interval is not valid, or an unexpected error stopped it. A new request can be created. |

The reprocessing instance table is available and updated as reprocessing takes place:

<!-- TODO(screenshot): file=model-reprocessing-location-of-reprocessing-instances.png route=/models/reprocessing state="instances grid with a request in Processing" -->
![Image](model-reprocessing-location-of-reprocessing-instances.png)

| Name            | Description                                                                                                   |
|-----------------|---------------------------------------------------------------------------------------------------------------|
| Status          | The current status of the reprocessing job.                                                                   |
| Created Date    | The date and time the reprocessing request was created.                                                       |
| Reference Date  | The current date and time being processed in that moment.  This is the reference date specified in the model. |
| Available Count | The total number of records in the universe as brought back during the initial counts.                        |
| Sampled Count   | The total number of records that have been sampled for the purposes of reprocessing.                          |
| Matched Count   | The total number of records that have been matched (after being sampled) for the purposes of reprocessing.    |
| Processed Count | The total number of records reprocessed so far.                                                               |
| Completed Date  | The date and time that the job completed.                                                                     |
| Error Count     | The number of records in error observed during reprocessing.                                                  |

Refreshing the grid by navigating again to the Reprocessing Filter (Models >> Reprocessing, clicking on the Reprocessing
Filter and scrolling down to the instances gird):

<!-- TODO(screenshot): file=model-reprocessing-completed-job-instance.png route=/models/reprocessing state="instances grid with a Completed request" -->
![Image](model-reprocessing-completed-job-instance.png)

It can be seen that the reprocessing job has completed (which will allow new reprocessing jobs to be created for this
reprocessing filter by simply clicking the Reprocess button).

## How reprocessing reads and updates the archive

- **The window.** The newest reference date in the model's archive is found, and the filter's interval is counted back
  from it. Every archived record from that start up to and including the newest reference date is read.
- **Records archived during the run.** Only records archived before the request started are read. Transactions that
  arrive while reprocessing runs are not picked up, so the run has a fixed end.
- **The order.** Records are read oldest first, by reference date and then by entry, a page at a time
  (`ReprocessingBulkLimit` records per page). Each page continues exactly after the last record of the one before, so
  every record in the window is read once, however many records share a reference date, and a later page is read as
  quickly as an early one.
- **The sample.** The filter's sample is a percentage: 100 considers every record, 50 about half, 0 none.
- **What happens to a match.** A sampled record that the filter rule matches is run through the model again. The new
  results of a page's matches are then written to the archive together, in one transaction: each record is updated in
  place, its previous version is kept in the archive's version history, its version is increased, and it is stamped with
  the reprocessing request. Only archive keys whose value changed are rewritten (with a version kept); keys the new
  result no longer produces are removed, for that record only, and keys that did not change are left as they are.
- **The cache.** Reprocessing does not change the cache. A transaction's cached payload, its search key journals and the
  latest transaction for each search key value belong to the transaction as it arrived, and a re-run changes none of
  them, so none are rewritten; neither are the latest reference date, TTL counters or the archive write-ahead log. The
  only trace is the cache's own record of which journals were read.
- **Errors.** A record whose filter rule or re-run fails is counted in Error Count, not in Matched Count, and the run
  carries on. If a page's archive write fails, or a matched record's archive row has been deleted in the meantime, those
  records are counted as errors too.
- **Stopping.** Deleting the request stops it within one record once the progress is next written (every 10 seconds); a
  deleted request stays deleted and is not marked completed.

### Who may start, stop and delete a request

Three permissions govern reprocessing, so a role can be allowed to run a filter without being allowed to change it:

| Permission                      | Number | Allows                                                                                                                                                     |
|---------------------------------|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Read Write Reprocessing Rule    | 26     | Create, edit, delete, validate, parse, test and build the rule of a filter. Editing or deleting a rule supersedes it and cancels its uncompleted requests. |
| Trigger Reprocessing Run        | 73     | Trigger a run (create a request, validate one, update one).                                                                                                |
| Stop or Delete Reprocessing Run | 74     | Delete a request, which stops it if it is running.                                                                                                         |

Reading (list, open, the model tree, and the SQL tables behind them) is allowed to a role holding any one of the three,
so a role that may only trigger can see the filters and the runs it is triggering. Roles that held 26 before the split
were given 73 and 74 by the upgrade, so nothing they could do changed. Granting any of the three follows the same rule
as
every other permission: a caller can only grant, or give a role carrying, a permission the caller holds. Everything is
limited to the caller's own tenant, and a request, its status, its counts and its dates are assigned by the server:
values supplied by a client are ignored. A second request for a filter that already has an uncompleted one is refused
(conflict), through the screen and through the Ask Jooby tool alike.
<!-- TODO(docs): owner to confirm the three permission names as they appear in the Roles screen; permission names are stored in English in the catalogue and are not translated -->
<!-- TODO(docs): owner to confirm that permission 26 can stop a run indirectly by editing or deleting its rule, and that this is acceptable -->
<!-- TODO(screenshot): file=model-reprocessing-permissions-roles.png route=/administration/security state="Permissions card of a Role with 26, 73 and 74 ticked separately" -->
<!-- TODO(screenshot): file=model-reprocessing-trigger-only.png route=/models/reprocessing state="Signed in with only Trigger Reprocessing Run: the filter form read-only, no New or Delete on the filter, and the hint that some actions are hidden" -->
<!-- TODO(screenshot): file=model-reprocessing-conflict-refusal.png route=/models/reprocessing state="Reprocess clicked twice, second refused as already has an uncompleted instance" -->

### Interrupted runs, restart and removal

- **Shutdown.** If the service is stopped while a request is running, the request goes back to Not Allocated and starts
  again from the beginning of its window when the service is next up. Starting again is safe: the archive is updated in
  place, keys are replaced for the same request, and nothing else is written, so no record is counted twice.
- **Crash.** A request that shows Allocated, Initial Count or Processing, and whose progress has not moved for 30
  minutes, is treated as abandoned and goes back to Not Allocated. Progress is written at least every 10 seconds while
  records are being processed, so a live run is never taken over.
- **Unexpected error.** The request is marked Failed, not left running, so it does not block a new request for the
  filter.
- **Editing or deleting the filter.** Saving a new version of a filter, or deleting it, deletes its uncompleted requests
  (a running one stops, a waiting one never starts). Completed and Failed requests are kept.
- **Page size.** `ReprocessingBulkLimit` is the number of records read per page, default 10,000. A value that is not a
  positive whole number falls back to 10,000 and anything above 100,000 is held at 100,000, so memory use on a very
  large archive is bounded.

<!-- TODO(docs): owner to confirm the 30 minute abandonment window and whether it should be a setting -->
<!-- TODO(docs): the history of finished requests is attached to the filter version they ran under, so after the filter is edited the grid for the new version starts empty (existing behaviour of versioned filters); confirm this is acceptable -->

### Behaviour across volume

`Jube.Tests/Volume/ReprocessingVolumeTests.cs` (category `Service`) seeds the archive of a synchronised copy of the
example model, reprocesses it with a rule that matches about 5% of records, and checks that every record is read
exactly once, that exactly the matches are updated and updated once each, that every page but the last is a full
page, and that memory stays bounded. It seeds 100,000 records by default, which `JubeReprocessingVolumeRows` raises
for a deeper run.

What it deliberately does not do is assert a throughput. It is a functional test that happens to need volume to be
meaningful — paging, exactly-once processing and bounded memory are only interesting over more rows than fit in one
page — and a records-per-second floor measured on whatever hardware happens to be running says very little about
the code. The previous floor of 2,000 records a second demonstrated the point: on a four-core runner the test
recorded 741, and the test's own stage breakdown showed why, with sanctions screening taking 34.5 ms of each of the
4,914 re-invocations. The number being measured was sanctions latency, not reprocessing throughput. Performance
belongs in a suite of its own, run on hardware whose capacity is known, rather than in a correctness gate where it
can only produce false alarms.

On a development machine, 1,000,000 records were read and 48,677 matches re-run and updated in 1 minute 25 seconds
(about 11,800 records a second, 570 re-runs a second), in 200 pages of 5,000, with pages read in about 45 ms from first
to last and the heap growing by 74 MB. Writing each match's archive record in its own transaction took more than twice
as long (3 minutes 40 seconds); writing a page's matches together removed most of that cost.
