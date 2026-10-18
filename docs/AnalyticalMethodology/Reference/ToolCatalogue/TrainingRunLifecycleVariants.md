---
layout: default
title: Training Run Lifecycle Variants
nav_order: 17
parent: Reference
grand_parent: Analytical Methodology
---

# Training run lifecycle variants

Every trainable model family in Advanced Analytics exposes a `Submit<Family>Training` tool that queues an
asynchronous training run, and every one of those tools is already documented in full, by name, in
`docs/AgentHierarchy/tool-inventory.md`. Once a run is queued, the ordinary way to follow it is the three generic
lifecycle tools that work across every family uniformly -- `GetAdvancedAnalyticsTrainingRunStatus`,
`CancelAdvancedAnalyticsTrainingRun` and `GetAdvancedAnalyticsTrainingRunResult` -- which are also fully documented
there, and several families additionally have their `Submit` tool discussed in depth in their own methodology page
under `docs/AnalyticalMethodology/`. None of that is repeated here.

What is not documented anywhere else is that six of the training-run tool files additionally expose their own,
family-scoped triple of status/cancel/result tools, under names that look generic but are not the three tools just
named: `LogisticRegressionTrainingRunTools`, `KMeansClustererTrainingRunTools`, `GaussianMixtureDensityTrainingRunTools`,
`MahalanobisAnomalyTrainingRunTools`, `LinearRegressionTrainingRunTools` and `PrincipalComponentTrainingRunTools`. All
eighteen of these tools call, underneath, into a service that opens its own
`AdvancedAnalyticsTrainingRunRepository` and `AdvancedAnalyticsTrainedModelVersionRepository` against the same
`AdvancedAnalyticsTrainingRun` table and the same run id sequence that the shared
`AdvancedAnalyticsSharedTrainingRunService` behind the three generic tools also reads and writes. A run id returned
by any family's `Submit` tool is therefore a valid argument to any of these family-scoped tools, to the three generic
tools, or to another family's triple interchangeably -- nothing about the row itself is family-specific, and the
generic `GetAdvancedAnalyticsTrainingRunStatus`/`CancelAdvancedAnalyticsTrainingRun` will find and act on it exactly
as the family tool would. Where the two paths genuinely diverge is the result: the generic
`GetAdvancedAnalyticsTrainingRunResult` tool returns its evidence through
`AdvancedAnalyticsSharedTrainingRunResultDto.Evidence`, declared as a bare `object`, because that one return type has
to cover every model kind in the product; the family-scoped `Get<Family>TrainingRunResult` tools instead return a
result type whose `Evidence` property is declared against the concrete evidence type for that one family (for
example `AdvancedAnalyticsEvidence<KMeansEvidenceDetails>` or `AdvancedAnalyticsEvidence<PrincipalComponentEvidenceDetails>`).
The JSON that comes back carries the same underlying fields either way -- the generic tool's result is deserialised
from the same `MetricsJson` through the same per-kind mapping -- but only the family-scoped tool's declared return
shape tells the calling model, in advance, exactly which named fields to expect. Calling the family tool on a run id
that was never submitted through that family succeeds at the status/cancel level (the row is still found) but its
result will carry no evidence, because the stored version was trained and evidenced under a different model kind than
the one that tool's result type expects.

## Logistic regression

`LogisticRegressionTrainingRunTools` depends on `IAdvancedAnalyticsTrainingRuns`, an interface implemented by
`AdvancedAnalyticsTrainingRunsGateway`, which wraps `AdvancedAnalyticsTrainingService` -- the same service behind
`SubmitLogisticRegressionTraining`. The interface packages submit, status, cancel and result as one cohesive contract
for this one family, which is why the tool class carries its own triple rather than taking a second, shared
dependency alongside it. A logistic regression run produces a trained classifier together with held-out metrics,
coefficient significance, calibration diagnostics and fit warnings.

`GetTrainingRunStatus` reads the same run row as `GetAdvancedAnalyticsTrainingRunStatus` would, through
`AdvancedAnalyticsTrainingService.GetRunAsync`, and returns it as a `TrainingRunStatusView`: run id, status, elapsed
seconds, error and, once succeeded, the trained model version id. The view carries fewer fields than the generic
tool's `AdvancedAnalyticsTrainingRunStatusDto` -- no run guid, job mode, created/started/completed timestamps or
submitted parameters -- but the status value itself is identical, because both paths read the same row. Calling the
generic tool on a logistic run id, or this tool on a run id from any other family, works exactly the same way at the
status level.

`CancelTrainingRun` cancels through `AdvancedAnalyticsTrainingService.CancelRunAsync`, which calls the same
`AdvancedAnalyticsTrainingRunRepository.StopAsync` that `CancelAdvancedAnalyticsTrainingRun` calls. A queued run is
cancelled at once and a running one is asked to stop at its next step boundary or inside the fit; there is no
difference in effect between cancelling a logistic run through this tool or through the generic one, only a
difference in which View shape the caller gets back.

`GetTrainingRunResult` is where the two paths genuinely diverge. It returns a `TrainingRunResultView` whose `Evidence`
is declared as `AdvancedAnalyticsLogisticEvidence?` -- split sizes, train and test metrics, calibration, fit
diagnostics, coefficients and warnings, all named and typed. The generic `GetAdvancedAnalyticsTrainingRunResult`
reads the identical `MetricsJson` for the same run and deserialises it through the same logistic branch of
`AdvancedAnalyticsModelMapper.ReadEvidence`, but exposes it through `AdvancedAnalyticsSharedTrainingRunResultDto.Evidence`,
declared only as `object`. The numbers are the same; only this family's own result tool declares, in its own
signature, what the fields will be.

## K-means clustering

`KMeansClustererTrainingRunTools` depends on `IAdvancedAnalyticsClustererTrainingRuns`, implemented by
`AdvancedAnalyticsKMeansClustererTrainingRunsGateway` over `AdvancedAnalyticsKMeansClustererTrainingService` -- the
same service behind `SubmitKMeansClustererTraining`. As with logistic regression, the interface bundles the whole
lifecycle for this one, unsupervised family into a single dependency. A finished run produces fitted cluster centres
together with per-cluster sizes, inertia and, where cheap to compute, a silhouette score.

`GetClustererTrainingRunStatus` and `CancelClustererTrainingRun` read and stop the same training run row, through
`AdvancedAnalyticsKMeansClustererTrainingService.GetRunAsync`/`CancelRunAsync`, that the generic status and cancel
tools would reach for the identical run id; the returned `ClustererTrainingRunStatusView` is the same trimmed shape
pattern as logistic regression's view, carrying run id, status, elapsed seconds, error and trained version id and
nothing more. There is no functional difference between stopping a k-means run with this tool or with
`CancelAdvancedAnalyticsTrainingRun`.

`GetClustererTrainingRunResult` returns a `ClustererTrainingRunResultView` whose `Evidence` is declared as
`AdvancedAnalyticsEvidence<KMeansEvidenceDetails>?` -- cluster sizes and proportions, inertia and silhouette, named
and typed for this one family. The generic result tool reaches the same stored evidence through the
`KMeansClustererRecall.ModelKind` branch of `AdvancedAnalyticsModelMapper.ReadEvidence`, but, again, only as an
untyped `object` on `AdvancedAnalyticsSharedTrainingRunResultDto`. Calling this family's result tool on a run id that
was not a k-means submission returns a run status with no evidence, since the stored `MetricsJson` would not match
the `KMeansEvidenceDetails` shape this tool expects.

## Gaussian mixture density estimation

`GaussianMixtureDensityTrainingRunTools` takes a single constructor dependency on the concrete
`AdvancedAnalyticsGaussianMixtureDensityTrainingService`, not an interface. That service already owns the run
repository it needs to submit a density-estimation run, and it was given its own `GetRunAsync`, `CancelRunAsync` and
`GetRunResultAsync` methods rather than reaching for a second, shared service, so the tool class built on top of it
inherits the whole lifecycle from the one dependency it already has. A finished run produces a fitted mixture of
Gaussian components with no labels and no calibrated probability, only a raw log-density per row.

`GetGaussianMixtureDensityTrainingRunStatus` and `CancelGaussianMixtureDensityTrainingRun` operate on the same run
row, through the service's own `GetRunAsync`/`CancelRunAsync`, that the generic tools would reach with the identical
run id -- cancellation semantics (cancelled at once if still queued, cancelled at the next step boundary if running)
are identical either way. The returned `AdvancedAnalyticsTrainingRunStatusDto` here is, in fact, the very same DTO
type the generic status tool returns, so this status tool's output is not even trimmed relative to the generic one;
the separation exists purely because the tool class was wired to call this family's own service method rather than
the shared one.

`GetGaussianMixtureDensityTrainingRunResult` returns an `AdvancedAnalyticsGaussianMixtureDensityTrainingRunResultDto`
whose `Evidence` is declared as `AdvancedAnalyticsEvidence<GaussianMixtureDensityEvidenceDetails>?` -- each
component's weight, mean and covariance, mean per-row log-likelihood on each split, BIC, AIC and convergence, named
and typed. The generic result tool reaches the same `MetricsJson` through the
`GaussianMixtureDensityRecall.ModelKind` branch of `AdvancedAnalyticsModelMapper.ReadEvidence`, again only as an
untyped `object`. Pointing this family's result tool at a run id from a different family returns the run's status
with no evidence, since that run's stored evidence will not match the shape this tool deserialises into.

## Mahalanobis anomaly detection

`MahalanobisAnomalyTrainingRunTools` likewise takes a single constructor dependency on the concrete
`AdvancedAnalyticsMahalanobisAnomalyTrainingService`, for the same reason as the Gaussian mixture density case: the
service that submits the run already owns the run repository, and was given its own status/cancel/result methods
rather than a second, shared dependency. A finished run produces a fitted mean and covariance and, when usable labels
were supplied, a Platt calibration from Mahalanobis distance to a probability; without usable labels it reports the
raw distance instead.

`GetMahalanobisAnomalyTrainingRunStatus` and `CancelMahalanobisAnomalyTrainingRun` read and stop the same run row that
the generic tools would for the identical run id, again returning the very same `AdvancedAnalyticsTrainingRunStatusDto`
type the generic status tool returns -- there is no shape difference here at all, only the fact that the call is
routed through this family's own service method.

`GetMahalanobisAnomalyTrainingRunResult` returns an `AdvancedAnalyticsMahalanobisAnomalyTrainingRunResultDto` whose
`Evidence` carries distance percentiles, the covariance condition number and calibration, named and typed for this
family specifically. The generic result tool reaches the same evidence through the
`MahalanobisAnomalyRecall.ModelKind` branch of `AdvancedAnalyticsModelMapper.ReadEvidence` -- which, notably, also
validates that the deserialised evidence has split sizes and a calibration block before returning it, the same
defensive check this family's own `GetRunResultAsync` does not repeat -- but still only as an untyped `object` on the
shared result DTO. Pointing this tool at a run id trained as a different kind returns the status with no evidence.

## Linear regression

`LinearRegressionTrainingRunTools` takes a dependency on the concrete `AdvancedAnalyticsLinearRegressionTrainingService`,
the same pattern as the two services above, and additionally re-shapes every one of its three lifecycle calls into
the tool layer's own View types (`TrainingRunStatusView` and a dedicated `LinearRegressionTrainingRunResultView`)
rather than returning the service's DTOs directly, the way the Gaussian mixture density and Mahalanobis tools do. A
finished run produces an ordinary least squares fit with held-out metrics and coefficient significance; linear
regression has no classification threshold or calibration step, since its outcome is continuous rather than a class
label.

`GetLinearRegressionTrainingRunStatus` calls `AdvancedAnalyticsLinearRegressionTrainingService.GetRunAsync` for the
same run row the generic tool would read, then maps the result down into the trimmed `TrainingRunStatusView` shape
used by the logistic and k-means families (run id, status, elapsed seconds, error, trained version id), dropping the
run guid, job mode and timestamp fields the generic `AdvancedAnalyticsTrainingRunStatusDto` carries. The underlying
status is identical whichever tool reads it.

`CancelLinearRegressionTrainingRun` cancels through the same service's `CancelRunAsync`, mapped through the same
trimming, with the same cancellation semantics (immediate if queued, cooperative at the next step boundary or inside
the fit if running) as every other family here and as the generic cancel tool.

`GetLinearRegressionTrainingRunResult` returns a `LinearRegressionTrainingRunResultView` whose `Evidence` is declared
as `AdvancedAnalyticsEvidence<LinearRegressionEvidenceDetails>?` -- split sizes, train and test metrics and
coefficient significance, named and typed for this family. The generic result tool reaches the same `MetricsJson`
through the `MultipleLinearRegressionRecall.ModelKind` branch of `AdvancedAnalyticsModelMapper.ReadEvidence`, again
only as an untyped `object`. A run id from any other family returns a status with no evidence through this tool, for
the same reason as the families above.

## Principal component analysis

`PrincipalComponentTrainingRunTools` depends on `IAdvancedAnalyticsPrincipalComponentTrainingRuns`, implemented by
`AdvancedAnalyticsPrincipalComponentTrainingRunsGateway` over `AdvancedAnalyticsPrincipalComponentTrainingService` --
the same service behind `SubmitPrincipalComponentTraining` -- following the same bundled-interface pattern as
logistic regression and k-means. Principal component analysis is unsupervised and has no label or correct answer; a
finished run produces a set of fitted, uncorrelated components together with the share of variance each one explains.

`GetPrincipalComponentTrainingRunStatus` and `CancelPrincipalComponentTrainingRun` read and stop the same run row,
through the gateway over `AdvancedAnalyticsPrincipalComponentTrainingService.GetRunAsync`/`CancelRunAsync`, that the
generic tools would for an identical run id, returning the same trimmed `PrincipalComponentTrainingRunStatusView`
shape (run id, status, elapsed seconds, error, trained version id) as the other interface-bundled families. Stopping
a principal component run through this tool has the same effect as stopping it through
`CancelAdvancedAnalyticsTrainingRun`.

`GetPrincipalComponentTrainingRunResult` returns a `PrincipalComponentTrainingRunResultView` whose `Evidence` is
declared as `AdvancedAnalyticsEvidence<PrincipalComponentEvidenceDetails>?` -- every component's eigenvalue, the
cumulative variance explained, how many components were retained, and mean per-row reconstruction error on each
split, named and typed for this family. The generic result tool reaches the same evidence through the
`PrincipalComponentRecall.ModelKind` branch of `AdvancedAnalyticsModelMapper.ReadEvidence`, again only as an untyped
`object` on the shared result DTO. A run id submitted under a different family returns a status with no evidence
through this tool, for the same reason given for every family above.
