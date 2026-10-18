---
layout: default
title: Exhaustive Uploaded Datasets
nav_order: 9
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive uploaded datasets

An Exhaustive instance normally trains and recalls against live transaction data accumulated through the entity analysis
model. This page covers the other path: uploading a static CSV or JSON file as the instance's data source, for offline
exploration, training against a historical extract, or demonstrating a model without a live feed.

## Where it lives

- Parsing and profiling: `Jube.Analytics/Datasets/` (`DatasetCsvParser`, `DatasetJsonParser`, `DatasetContentSniffer`,
  `DatasetProfile`, `DatasetColumnAccumulator`, `Models/DatasetModels.cs`). This project has no dependency on
  `Jube.Data` and knows nothing about Postgres — it only turns a byte stream into rows, a column profile and a list of
  mapping warnings.
- Storage and the upload/confirm workflow: `Jube.Service/Repository/ExhaustiveInstanceDataset/`
  (`ExhaustiveInstanceDatasetService`, `ExhaustiveInstanceDatasetMapper`, `HashingStream`).
- The row store itself: `Jube.Data/Repository/ExhaustiveInstanceDatasetRepository.cs`, behind the
  `IDatasetStore` interface (`Jube.Data/Repository/Interfaces/IDatasetStore.cs`).
- HTTP surface: `Jube.App/Endpoints/ExhaustiveInstanceDatasetEndpoints.cs`, mounted at `/api/ExhaustiveInstanceDataset`
  plus the two format-specific routes `/api/ExhaustiveInstanceCsv` and `/api/ExhaustiveInstanceJson` that the upload
  button posts to directly.
- The Exhaustive page's dataset panel: `Jube.App/Pages/Model/Frame/Exhaustive.cshtml` and
  `Jube.App/wwwroot/js/Model/Frame/ExhaustiveDataset.js`, loaded before `Exhaustive.js` so the dataset picker is ready
  before the rest of the page's script runs.

## Upload, map, confirm

1. **Upload.** The panel's Kendo Upload widget posts the file (`.csv`, `.txt` or `.json`) to
   `/api/ExhaustiveInstanceCsv/Upload` or `/api/ExhaustiveInstanceJson/Upload`. `DatasetContentSniffer` and the relevant
   parser read it as a stream — `DatasetCountingStream` enforces the configured row and byte ceilings (`DatasetLimits`)
   without buffering the whole file — and return a profile: the column names, an inferred
   `DatasetProcessingTypes` guess per column, a handful of distinct sample values, and a `DatasetParseReport` of rows
   skipped or truncated.
2. **Map.** The grid (`DatasetColumnsGrid`) lets the user confirm or override each column's processing type and, for
   categorical columns, see the distinct values `DatasetColumnAccumulator` collected while scanning. Nothing is written
   to the instance yet — the parsed rows and the proposed mapping sit behind an
   `ExhaustiveInstanceDatasetDto`/`ExhaustiveInstanceDatasetMappingDto` pair identified by the dataset's own id.
3. **Confirm.** `ExhaustiveInstanceDatasetService.ConfirmAsync` takes the (possibly edited) column mapping, persists the
   dataset's rows through `IDatasetStore`, and points the owning `ExhaustiveSearchInstance` at it by setting
   `ExhaustiveInstanceCsvId` or `ExhaustiveInstanceJsonId` (whichever format was uploaded) and `DataSourceId`. From this
   point the instance trains and recalls against the uploaded rows exactly as it would against a live feed, through the
   same sampling and training stages.

A dataset can be replaced (upload a new file, confirm it, and the instance's `ExhaustiveInstanceCsvId` /
`ExhaustiveInstanceJsonId` moves to the new one) or removed (`DELETE /api/ExhaustiveInstanceDataset/{id}`, which the
page's `DatasetDeleteButton` calls), returning the instance to whatever data source it had before.

## See also

[Exhaustive data sources](ExhaustiveDataSources.md) covers how `DataSourceId` picks between a live archive feed, the
demo generator and an uploaded dataset, and what Preservation does with an uploaded dataset's rows on export and import.
