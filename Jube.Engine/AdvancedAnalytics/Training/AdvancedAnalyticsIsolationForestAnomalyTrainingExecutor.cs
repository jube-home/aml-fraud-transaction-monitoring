/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using log4net;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsIsolationForestAnomalyTrainingExecutor(
        DbContext dbContext,
        int tenantRegistryId,
        string userName,
        ILog log,
        AdvancedAnalyticsExecutionMessages messages = null)
    {
        private const int MaxErrorLength = 2000;

        private static readonly SemaphoreSlim[] modelNameGates =
            [.. Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1))];

        private readonly AdvancedAnalyticsExecutionMessages messages =
            messages ?? new AdvancedAnalyticsExecutionMessages();

        private readonly AdvancedAnalyticsTrainedModelRepository models = new(dbContext, tenantRegistryId, userName);
        private readonly AdvancedAnalyticsTrainingRunRepository runs = new(dbContext, tenantRegistryId, userName);

        private readonly IAdvancedAnalyticsTrainingTrace trace =
            new AdvancedAnalyticsDatabaseTrainingTrace(dbContext, tenantRegistryId, userName);

        private readonly AdvancedAnalyticsTrainingRunVariableRepository variables = new(dbContext, tenantRegistryId,
            userName);

        private readonly AdvancedAnalyticsTrainedModelVersionRepository versions = new(dbContext, tenantRegistryId,
            userName);

        public Func<double[][], int[], string[], IsolationForestAnomalyOptions, CancellationToken,
            IsolationForestAnomalyResult> Trainer { get; set; } = IsolationForestAnomalyTrainer.Train;

        private IAdvancedAnalyticsTrialRecorder Recorder { get; set; } = null!;

        public async Task<AdvancedAnalyticsIsolationForestAnomalyExecutionResult> ExecuteAsync(
            AdvancedAnalyticsTrainingRun run, IsolationForestAnomalyTrainingArguments arguments, double[][] inputs,
            int[] labels, CancellationToken token)
        {
            Recorder = new DbAdvancedAnalyticsTrialRecorder(dbContext, tenantRegistryId, userName, run.Id);

            var loadStopwatch = Stopwatch.StartNew();
            loadStopwatch.Stop();

            var trial = await Recorder.InsertTrialAsync(HyperparametersOf(arguments), token).ConfigureAwait(false);

            try
            {
                await BoundaryAsync(run, token).ConfigureAwait(false);

                IsolationForestAnomalyResult result;

                try
                {
                    var options = arguments.ToOptions();
                    var names = arguments.VariableNames ?? [];

                    result = await AdvancedAnalyticsDedicatedThread
                        .RunAsync(() => Trainer(inputs, labels, names, options, token), token)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                {
                    var message = Truncate(ex.Message);

                    await CompleteTrialQuietlyAsync(trial.Id, AdvancedAnalyticsStatus.Failed, null, message)
                        .ConfigureAwait(false);

                    return await FailAsync(run, message).ConfigureAwait(false);
                }

                return await PersistAsync(run, arguments, trial.Id, result, inputs, loadStopwatch.Elapsed, token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await CompleteTrialQuietlyAsync(trial.Id, AdvancedAnalyticsStatus.Cancelled, null, messages.Cancelled)
                    .ConfigureAwait(false);

                throw;
            }
            catch (Exception)
            {
                await CompleteTrialQuietlyAsync(trial.Id, AdvancedAnalyticsStatus.Failed, null,
                    messages.FailedUnexpectedly).ConfigureAwait(false);

                await RecordTerminalAsync(run.Id, AdvancedAnalyticsStatus.Failed, messages.FailedUnexpectedly)
                    .ConfigureAwait(false);

                throw;
            }
        }

        public async Task RecordTerminalAsync(int runId, AdvancedAnalyticsStatus status, string error)
        {
            try
            {
                await runs.UpdateStatusAsync(runId, status, error, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"AdvancedAnalyticsIsolationForestAnomalyTrainingExecutor: could not record {status} for run {runId}",
                    ex);
            }
        }

        public static string Truncate(string message)
        {
            return message.Length <= MaxErrorLength ? message : message[..MaxErrorLength];
        }

        private async Task BoundaryAsync(AdvancedAnalyticsTrainingRun run, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (await AdvancedAnalyticsRunStatus.IsStoppingOrStoppedOrCancelledAsync(Recorder, runs, run.Id,
                    messages.Cancelled, token).ConfigureAwait(false))
            {
                throw new AdvancedAnalyticsRunStoppedException();
            }
        }

        private async Task<AdvancedAnalyticsIsolationForestAnomalyExecutionResult> FailAsync(
            AdvancedAnalyticsTrainingRun run, string error)
        {
            var message = Truncate(error);
            await RecordTerminalAsync(run.Id, AdvancedAnalyticsStatus.Failed, message).ConfigureAwait(false);

            return new AdvancedAnalyticsIsolationForestAnomalyExecutionResult
            {
                RunId = run.Id,
                RunGuid = run.Guid,
                Status = AdvancedAnalyticsStatus.Failed,
                Error = message
            };
        }

        private async Task<AdvancedAnalyticsIsolationForestAnomalyExecutionResult> PersistAsync(
            AdvancedAnalyticsTrainingRun run, IsolationForestAnomalyTrainingArguments arguments, int trialId,
            IsolationForestAnomalyResult result, double[][] inputs, TimeSpan loadDuration, CancellationToken token)
        {
            await BoundaryAsync(run, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Load, new Dictionary<string, double>
            {
                [AdvancedAnalyticsDatabaseTrainingTrace.DurationKey] = loadDuration.TotalMilliseconds
            }, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Clean, new Dictionary<string, double>
            {
                ["Train.ExcludedVariables"] = result.Preprocessing.Count(p => p.Excluded)
            }, token).ConfigureAwait(false);

            await BoundaryAsync(run, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Split, new Dictionary<string, double>
            {
                ["Train.Rows"] = result.SplitSizes.Train,
                ["Validation.Rows"] = result.SplitSizes.Validation,
                ["Test.Rows"] = result.SplitSizes.Test
            }, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Normalise, new Dictionary<string, double>
            {
                ["Train.Variables"] = result.Variables.Length
            }, token).ConfigureAwait(false);

            await BoundaryAsync(run, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Fit, new Dictionary<string, double>
            {
                ["Train.TreeCount"] = result.Details.TreeCount,
                ["Train.SubsampleSize"] = result.Details.SubsampleSize
            }, token).ConfigureAwait(false);

            var validateMetrics = new Dictionary<string, double>();

            if (result.Details.TestScores is { } testScores)
            {
                validateMetrics["Test.ScoreP50"] = testScores.P50;
                validateMetrics["Test.ScoreP90"] = testScores.P90;
                validateMetrics["Test.ScoreP99"] = testScores.P99;
            }

            if (result.Details.TestLabelledMetrics is { RocAuc: { } auc })
            {
                validateMetrics["Test.RocAuc"] = auc;
            }

            if (result.Details.Calibration.Applied)
            {
                if (result.Details.Calibration.Slope is { } slope)
                {
                    validateMetrics["Validation.PlattSlope"] = slope;
                }

                if (result.Details.Calibration.Intercept is { } intercept)
                {
                    validateMetrics["Validation.PlattIntercept"] = intercept;
                }
            }

            await AppendAsync(run, AdvancedAnalyticsStep.Validate, validateMetrics, token).ConfigureAwait(false);

            await BoundaryAsync(run, token).ConfigureAwait(false);

            await variables.InsertRangeAsync(VariableRows(run.Id, result, inputs), token).ConfigureAwait(false);

            var serialiseStopwatch = Stopwatch.StartNew();
            var evidenceJson = AdvancedAnalyticsEvidenceJson.Serialise(result.ToEvidence());
            var variablesJson = VariablesJsonOf(arguments, result.Variables);

            AdvancedAnalyticsTrainedModel model;
            AdvancedAnalyticsTrainedModelVersion version;
            var createdModel = false;

            var gate = modelNameGates[
                (StringComparer.OrdinalIgnoreCase.GetHashCode(
                    $"{tenantRegistryId}:{run.AdvancedAnalyticsInstanceId}:{arguments.ModelName}") & int.MaxValue) %
                modelNameGates.Length];

            await gate.WaitAsync(token).ConfigureAwait(false);

            try
            {
                await BoundaryAsync(run, token).ConfigureAwait(false);

                var existing = (await models.ListAsync(run.AdvancedAnalyticsInstanceId, token).ConfigureAwait(false))
                    .FirstOrDefault(m => m.KindId == AdvancedAnalyticsKind.AnomalyDetector
                                         && string.Equals(m.Name, arguments.ModelName,
                                             StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    model = await models.InsertAsync(new AdvancedAnalyticsTrainedModel
                    {
                        AdvancedAnalyticsInstanceId = run.AdvancedAnalyticsInstanceId,
                        Name = arguments.ModelName!,
                        KindId = AdvancedAnalyticsKind.AnomalyDetector,
                        SourceId = AdvancedAnalyticsSource.Agent,
                        CorrelationKey = arguments.CorrelationKey
                    }, token).ConfigureAwait(false);

                    createdModel = true;
                }
                else
                {
                    model = existing;
                }

                try
                {
                    version = await versions.InsertAsync(new AdvancedAnalyticsTrainedModelVersion
                    {
                        AdvancedAnalyticsTrainedModelId = model.Id,
                        ModelJson = result.SnapshotJson,
                        VariablesJson = variablesJson,
                        MetricsJson = evidenceJson,
                        AdvancedAnalyticsTrainingRunId = run.Id
                    }, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (createdModel)
                    {
                        await models.DeleteAsync(model.Id, CancellationToken.None).ConfigureAwait(false);
                    }

                    throw;
                }
            }
            finally
            {
                gate.Release();
            }

            serialiseStopwatch.Stop();

            await AppendAsync(run, AdvancedAnalyticsStep.Serialise, new Dictionary<string, double>
            {
                [AdvancedAnalyticsDatabaseTrainingTrace.DurationKey] = serialiseStopwatch.Elapsed.TotalMilliseconds
            }, CancellationToken.None).ConfigureAwait(false);

            var score = result.Details.TestLabelledMetrics?.RocAuc;

            await CompleteTrialQuietlyAsync(trialId, AdvancedAnalyticsStatus.Succeeded,
                score.HasValue && double.IsFinite(score.Value) ? score : null, null).ConfigureAwait(false);

            try
            {
                await Recorder.MarkSelectedAsync(trialId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"AdvancedAnalyticsIsolationForestAnomalyTrainingExecutor: could not select trial {trialId} of run {run.Id}",
                    ex);
            }

            await runs.UpdateProgressAsync(run.Id, 1, CancellationToken.None).ConfigureAwait(false);

            await runs.UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Succeeded, null, CancellationToken.None)
                .ConfigureAwait(false);

            return new AdvancedAnalyticsIsolationForestAnomalyExecutionResult
            {
                RunId = run.Id,
                RunGuid = run.Guid,
                Status = AdvancedAnalyticsStatus.Succeeded,
                Model = model,
                CreatedModel = createdModel,
                Version = version,
                Result = result
            };
        }

        private async Task CompleteTrialQuietlyAsync(int trialId, AdvancedAnalyticsStatus status, double? score,
            string error)
        {
            try
            {
                await Recorder.CompleteTrialAsync(trialId, status, score, error, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"AdvancedAnalyticsIsolationForestAnomalyTrainingExecutor: could not record trial {trialId} as {status}",
                    ex);
            }
        }

        private Task AppendAsync(AdvancedAnalyticsTrainingRun run, AdvancedAnalyticsStep step,
            Dictionary<string, double> metrics, CancellationToken token)
        {
            return trace.AppendAsync(
                new AdvancedAnalyticsTrainingTraceEntry(run.Guid, (int)step, step.ToString(), metrics), token);
        }

        private static string HyperparametersOf(IsolationForestAnomalyTrainingArguments arguments)
        {
            return arguments.WithoutData().ToJson();
        }

        private static string VariablesJsonOf(IsolationForestAnomalyTrainingArguments arguments,
            string[] variablesInOrder)
        {
            return AdvancedAnalyticsVariableRowsJson.Build(variablesInOrder, arguments.VariableNames!,
                arguments.ProcessingTypeIds!, arguments.TransformationIds, arguments.TransformationParametersJson);
        }

        private static List<AdvancedAnalyticsTrainingRunVariable> VariableRows(int runId,
            IsolationForestAnomalyResult result, double[][] inputs)
        {
            var rows = new List<AdvancedAnalyticsTrainingRunVariable>();

            for (var j = 0; j < result.Preprocessing.Length; j++)
            {
                var info = result.Preprocessing[j];
                double? minimum = null;
                double? maximum = null;
                var missing = 0;

                foreach (var row in result.TrainRows)
                {
                    var value = inputs[row][j];

                    if (!double.IsFinite(value))
                    {
                        missing++;
                        continue;
                    }

                    minimum = minimum.HasValue ? Math.Min(minimum.Value, value) : value;
                    maximum = maximum.HasValue ? Math.Max(maximum.Value, value) : value;
                }

                rows.Add(new AdvancedAnalyticsTrainingRunVariable
                {
                    AdvancedAnalyticsTrainingRunId = runId,
                    VariableSequence = j,
                    Name = info.Name,
                    Mean = FiniteOrNull(info.Mean),
                    StandardDeviation = FiniteOrNull(info.StandardDeviation),
                    Minimum = minimum,
                    Maximum = maximum,
                    MissingCount = missing,
                    RowCount = result.TrainRows.Length
                });
            }

            return rows;
        }

        private static double? FiniteOrNull(double value)
        {
            return double.IsFinite(value) ? value : null;
        }
    }
}