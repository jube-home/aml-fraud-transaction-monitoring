---
layout: default
title: Exhaustive Data Sources
nav_order: 8
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive data sources

Every `ExhaustiveSearchInstance` has a `DataSourceId` (`Jube.Dto.ExhaustiveSearchInstance.ExhaustiveDataSource`)
deciding what `Jube.Engine/Exhaustive/Stages/SampleStage.cs` samples from when the instance trains:

- **Archive (0)** — the default. The instance samples from the entity analysis model's own archived transaction history,
  the same data it will recall against in production.
- **DemoDataset (1)** — `MockDataLoader` generates a synthetic dataset for the model instead. Useful for a walkthrough
  or a sales demonstration where no real transaction history exists yet. This per-instance choice replaced the old
  global `UseMockDataExhaustive` environment variable, which is gone from the code and the configuration surface.
- **UploadedDataset (2)** — the instance trains against a confirmed CSV or JSON file uploaded through the dataset panel;
  see [Exhaustive uploaded datasets](ExhaustiveJsonDatasets.md) for the upload/map/confirm flow.

## Preservation impact

An export carries an uploaded dataset's rows (`ExhaustiveInstanceCsv`/`ExhaustiveInstanceJson` in the preservation
payload) alongside the owning `ExhaustiveSearchInstance` whenever the export's `Exhaustive` option is set — the same
option that carries the rest of the Exhaustive tree. The instance's `ExhaustiveInstanceCsvId` and
`ExhaustiveInstanceJsonId` foreign keys are remapped to whatever id the dataset gets on import, exactly like every other
Exhaustive foreign key.

If the instance's `DataSourceId` is `UploadedDataset` but the dataset itself was never exported (for example a dataset
still mid-upload and not yet confirmed when the export ran), the import cannot resolve the missing foreign key. Rather
than fail the whole import over one instance's unavailable dataset, the importer nulls
`ExhaustiveInstanceCsvId`/`ExhaustiveInstanceJsonId` and resets `DataSourceId` back to `Archive` on that instance, so it
imports cleanly and falls back to sampling from the archive the next time it trains.
