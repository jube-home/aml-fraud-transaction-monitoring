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
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Regression;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsTrainingExecutor(
        DbContext dbContext,
        int tenantRegistryId,
        string userName,
        ILog log,
        AdvancedAnalyticsExecutionMessages messages = null)
    {
        private const int MaxCurvePoints = 101;
        private const int MaxErrorLength = 2000;

        private static readonly SemaphoreSlim[] modelNameGates =
            [.. Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1))];

        private readonly AdvancedAnalyticsTrainingRunCurvePointRepository curvePoints = new(dbContext, tenantRegistryId,
            userName);

        private readonly AdvancedAnalyticsExecutionMessages messages =
            messages ?? new AdvancedAnalyticsExecutionMessages();

        private readonly AskJoobyAnalyticsTrainedModelRepository models = new(dbContext, tenantRegistryId, userName);
        private readonly AdvancedAnalyticsTrainingRunRepository runs = new(dbContext, tenantRegistryId, userName);

        private readonly IAdvancedAnalyticsTrainingTrace trace =
            new AdvancedAnalyticsDatabaseTrainingTrace(dbContext, tenantRegistryId, userName);

        private readonly AdvancedAnalyticsTrainingRunVariableRepository variables = new(dbContext, tenantRegistryId,
            userName);

        private readonly AskJoobyAnalyticsTrainedModelVersionRepository versions = new(dbContext, tenantRegistryId,
            userName);

        public Func<double[][], int[], string[], LogisticRegressionOptions, CancellationToken,
            LogisticRegressionResult> Trainer { get; set; } =
            LogisticRegressionTrainer.Train;

        private IAdvancedAnalyticsTrialRecorder Recorder { get; set; } = null!;

        public async Task<AdvancedAnalyticsExecutionResult> ExecuteAsync(AdvancedAnalyticsTrainingRun run,
            AdvancedAnalyticsTrainingArguments arguments,
            Func<CancellationToken, Task<AdvancedAnalyticsResolvedTrainingData>> loadInput,
            CancellationToken token)
        {
            Recorder = new DbAdvancedAnalyticsTrialRecorder(dbContext, tenantRegistryId, userName, run.Id);

            AdvancedAnalyticsResolvedTrainingData data;
            var loadStopwatch = Stopwatch.StartNew();

            try
            {
                data = await loadInput(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is AdvancedAnalyticsTrainingInputException or NotSupportedException)
            {
                return await FailAsync(run, ex.Message).ConfigureAwait(false);
            }

            loadStopwatch.Stop();

            var trial = await Recorder.InsertTrialAsync(HyperparametersOf(arguments), token).ConfigureAwait(false);

            try
            {
                await BoundaryAsync(run, token).ConfigureAwait(false);

                LogisticRegressionResult result;

                try
                {
                    var options = arguments.ToOptions();
                    var names = arguments.VariableNames ?? [];

                    result = await AdvancedAnalyticsDedicatedThread
                        .RunAsync(() => Trainer(data.Inputs, data.Labels, names, options, token), token)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                {
                    var message = Truncate(ex.Message);

                    await CompleteTrialQuietlyAsync(trial.Id, AdvancedAnalyticsStatus.Failed, null, message)
                        .ConfigureAwait(false);

                    return await FailAsync(run, message).ConfigureAwait(false);
                }

                var outcome = await PersistAsync(run, arguments, trial.Id, result, data.Inputs,
                    loadStopwatch.Elapsed, token).ConfigureAwait(false);

                return outcome;
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
                log.Error($"AdvancedAnalyticsTrainingExecutor: could not record {status} for run {runId}", ex);
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

        private async Task<AdvancedAnalyticsExecutionResult> FailAsync(AdvancedAnalyticsTrainingRun run, string error)
        {
            var message = Truncate(error);
            await RecordTerminalAsync(run.Id, AdvancedAnalyticsStatus.Failed, message).ConfigureAwait(false);

            return new AdvancedAnalyticsExecutionResult
            {
                RunId = run.Id,
                RunGuid = run.Guid,
                Status = AdvancedAnalyticsStatus.Failed,
                Error = message
            };
        }

        private async Task<AdvancedAnalyticsExecutionResult> PersistAsync(AdvancedAnalyticsTrainingRun run,
            AdvancedAnalyticsTrainingArguments arguments, int trialId, LogisticRegressionResult result,
            double[][] inputs, TimeSpan loadDuration, CancellationToken token)
        {
            await BoundaryAsync(run, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Load, new Dictionary<string, double>
            {
                [AdvancedAnalyticsDatabaseTrainingTrace.DurationKey] = loadDuration.TotalMilliseconds
            }, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Clean, new Dictionary<string, double>
            {
                ["Train.ExcludedVariables"] = result.Preprocessing.Count(p => p.Excluded),
                ["Train.MissingIndicatorColumns"] = result.Preprocessing.Count(p => p.HasMissingIndicator)
            }, token).ConfigureAwait(false);

            await BoundaryAsync(run, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Split, new Dictionary<string, double>
            {
                ["Train.Rows"] = result.SplitSizes.Train,
                ["Train.Positives"] = result.SplitSizes.TrainPositives,
                ["Validation.Rows"] = result.SplitSizes.Validation,
                ["Validation.Positives"] = result.SplitSizes.ValidationPositives,
                ["Test.Rows"] = result.SplitSizes.Test,
                ["Test.Positives"] = result.SplitSizes.TestPositives
            }, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Normalise, new Dictionary<string, double>
            {
                ["Train.Variables"] = result.Variables.Length
            }, token).ConfigureAwait(false);

            await BoundaryAsync(run, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Fit, new Dictionary<string, double>
            {
                ["Train.Iterations"] = result.Fit.Iterations,
                ["Train.Converged"] = result.Fit.Converged ? 1 : 0,
                ["Train.MaximumGradient"] = result.Fit.MaximumGradient,
                ["Train.EventsPerVariable"] = result.Fit.EventsPerVariable
            }, token).ConfigureAwait(false);

            var validateMetrics = new Dictionary<string, double>();
            AddSplitMetrics(validateMetrics, result.TrainMetrics);
            AddSplitMetrics(validateMetrics, result.ValidationMetrics);
            AddSplitMetrics(validateMetrics, result.TestMetrics);

            if (result.Calibration is { Applied: true, Slope: { } slope, Intercept: { } intercept })
            {
                validateMetrics["Validation.PlattSlope"] = slope;
                validateMetrics["Validation.PlattIntercept"] = intercept;
            }

            await AppendAsync(run, AdvancedAnalyticsStep.Validate, validateMetrics, token).ConfigureAwait(false);

            await BoundaryAsync(run, token).ConfigureAwait(false);

            await variables.InsertRangeAsync(VariableRows(run.Id, result, inputs), token).ConfigureAwait(false);

            var curveRows = CurveRows(run.Id, result);

            if (curveRows.Count > 0)
            {
                await curvePoints.InsertRangeAsync(curveRows, token).ConfigureAwait(false);
            }

            var serialiseStopwatch = Stopwatch.StartNew();

            var evidenceJson = AdvancedAnalyticsEvidenceJson.Serialise(
                AdvancedAnalyticsEvidenceJson.FromResult(result));

            var variablesJson = VariablesJsonOf(arguments, result.SnapshotJson);
            var calibratorJson = CalibratorOf(result.SnapshotJson);

            AskJoobyAnalyticsTrainedModel model;
            AskJoobyAnalyticsTrainedModelVersion version;
            var createdModel = false;

            var gate = modelNameGates[(StringComparer.OrdinalIgnoreCase.GetHashCode(
                                           $"{tenantRegistryId}:{run.AskJoobyAnalyticsInstanceId}:{arguments.ModelName}") &
                                       int.MaxValue)
                                      % modelNameGates.Length];

            await gate.WaitAsync(token).ConfigureAwait(false);

            try
            {
                await BoundaryAsync(run, token).ConfigureAwait(false);

                var existing = (await models.ListAsync(run.AskJoobyAnalyticsInstanceId, token)
                        .ConfigureAwait(false))
                    .FirstOrDefault(m => m.KindId == AdvancedAnalyticsKind.Classifier
                                         && string.Equals(m.Name, arguments.ModelName,
                                             StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    model = await models.InsertAsync(new AskJoobyAnalyticsTrainedModel
                    {
                        AskJoobyAnalyticsInstanceId = run.AskJoobyAnalyticsInstanceId,
                        Name = arguments.ModelName!,
                        KindId = AdvancedAnalyticsKind.Classifier,
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
                    version = await versions.InsertAsync(new AskJoobyAnalyticsTrainedModelVersion
                    {
                        AskJoobyAnalyticsTrainedModelId = model.Id,
                        ModelJson = result.SnapshotJson,
                        VariablesJson = variablesJson,
                        MetricsJson = evidenceJson,
                        CalibratorJson = calibratorJson,
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

            var score = result.TestMetrics?.RocAuc ?? result.ValidationMetrics?.RocAuc ?? result.TrainMetrics?.RocAuc;

            await CompleteTrialQuietlyAsync(trialId, AdvancedAnalyticsStatus.Succeeded,
                score.HasValue && double.IsFinite(score.Value) ? score : null, null).ConfigureAwait(false);

            try
            {
                await Recorder.MarkSelectedAsync(trialId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"AdvancedAnalyticsTrainingExecutor: could not select trial {trialId} of run {run.Id}", ex);
            }

            await runs.UpdateProgressAsync(run.Id, 1, CancellationToken.None).ConfigureAwait(false);

            await runs.UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Succeeded, null, CancellationToken.None)
                .ConfigureAwait(false);

            return new AdvancedAnalyticsExecutionResult
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
                log.Error($"AdvancedAnalyticsTrainingExecutor: could not record trial {trialId} as {status}", ex);
            }
        }

        private Task AppendAsync(AdvancedAnalyticsTrainingRun run, AdvancedAnalyticsStep step,
            Dictionary<string, double> metrics, CancellationToken token)
        {
            return trace.AppendAsync(
                new AdvancedAnalyticsTrainingTraceEntry(run.Guid, (int)step, step.ToString(), metrics), token);
        }

        private static string HyperparametersOf(AdvancedAnalyticsTrainingArguments arguments)
        {
            var copy = AdvancedAnalyticsTrainingArguments.FromJson(arguments.ToJson());
            copy.Input = copy.Input?.WithoutData();

            return copy.ToJson();
        }

        private static string VariablesJsonOf(AdvancedAnalyticsTrainingArguments arguments, string snapshotJson)
        {
            var order = JObject.Parse(snapshotJson)["variablesInOrder"]?.Select(t => t.Value<string>() ??
                                throw new InvalidOperationException("The snapshot names a variable with no name."))
                            .ToArray()
                        ?? throw new InvalidOperationException("The snapshot lists no variables.");

            return AdvancedAnalyticsVariableRowsJson.Build(order, arguments.VariableNames!,
                arguments.ProcessingTypeIds!, arguments.TransformationIds, arguments.TransformationParametersJson);
        }

        private static string CalibratorOf(string snapshotJson)
        {
            var calibrator = JObject.Parse(snapshotJson)["calibrator"];

            return calibrator == null || calibrator.Type == JTokenType.Null
                ? null
                : calibrator.ToString(Formatting.None);
        }

        private static void AddSplitMetrics(Dictionary<string, double> target, LogisticSplitMetrics metrics)
        {
            if (metrics == null)
            {
                return;
            }

            var prefix = metrics.Split + ".";
            Add(target, prefix + "Rows", metrics.Rows);
            Add(target, prefix + "Positives", metrics.Positives);
            Add(target, prefix + "RocAuc", metrics.RocAuc);
            Add(target, prefix + "RocAucStandardError", metrics.RocAucStandardError);
            Add(target, prefix + "AveragePrecision", metrics.AveragePrecision);
            Add(target, prefix + "LogLoss", metrics.LogLoss);
            Add(target, prefix + "BrierScore", metrics.BrierScore);
            Add(target, prefix + "ExpectedCalibrationError", metrics.ExpectedCalibrationError);
            Add(target, prefix + "Threshold", metrics.Threshold);
            Add(target, prefix + "Precision", metrics.Precision);
            Add(target, prefix + "Recall", metrics.Recall);
            Add(target, prefix + "F1", metrics.F1);
            Add(target, prefix + "MatthewsCorrelation", metrics.MatthewsCorrelation);
            Add(target, prefix + "CalibrationSlope", metrics.CalibrationSlope);
            Add(target, prefix + "CalibrationIntercept", metrics.CalibrationIntercept);
            Add(target, prefix + "MeanPredictedProbability", metrics.MeanPredictedProbability);
        }

        private static void Add(Dictionary<string, double> target, string key, double? value)
        {
            if (value.HasValue && double.IsFinite(value.Value))
            {
                target[key] = value.Value;
            }
        }

        private static List<AdvancedAnalyticsTrainingRunVariable> VariableRows(int runId,
            LogisticRegressionResult result, double[][] inputs)
        {
            var rows = new List<AdvancedAnalyticsTrainingRunVariable>();

            for (var j = 0; j < result.Preprocessing.Length; j++)
            {
                var info = result.Preprocessing[j];
                double? minimum = null;
                double? maximum = null;

                foreach (var row in result.TrainRows)
                {
                    var value = inputs[row][j];

                    if (!double.IsFinite(value))
                    {
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
                    ImputationValue = FiniteOrNull(info.FillValue),
                    MissingCount = info.MissingInTrain,
                    RowCount = result.TrainRows.Length
                });
            }

            return rows;
        }

        private static double? FiniteOrNull(double value)
        {
            return double.IsFinite(value) ? value : null;
        }

        private static List<AdvancedAnalyticsTrainingRunCurvePoint> CurveRows(int runId,
            LogisticRegressionResult result)
        {
            var rows = new List<AdvancedAnalyticsTrainingRunCurvePoint>();

            AddCurves(rows, runId, AdvancedAnalyticsSplit.Train, result.TrainMetrics, result.TrainPredictions);
            AddCurves(rows, runId, AdvancedAnalyticsSplit.Validation, result.ValidationMetrics,
                result.ValidationPredictions);
            AddCurves(rows, runId, AdvancedAnalyticsSplit.Test, result.TestMetrics, result.TestPredictions);

            return rows;
        }

        private static void AddCurves(List<AdvancedAnalyticsTrainingRunCurvePoint> rows, int runId,
            AdvancedAnalyticsSplit split, LogisticSplitMetrics metrics, LogisticPredictions predictions)
        {
            if (metrics == null)
            {
                return;
            }

            var index = 0;

            foreach (var bin in metrics.ReliabilityBins.Where(b => b.Count > 0))
            {
                rows.Add(new AdvancedAnalyticsTrainingRunCurvePoint
                {
                    AdvancedAnalyticsTrainingRunId = runId,
                    Curve = AdvancedAnalyticsCurve.Calibration,
                    Split = split,
                    PointIndex = index++,
                    X = FiniteOrNull(bin.MeanPredicted ?? double.NaN),
                    Y = FiniteOrNull(bin.ObservedRate ?? double.NaN)
                });
            }

            if (predictions == null || predictions.Labels.Length == 0 || predictions.Labels.Distinct().Count() < 2)
            {
                return;
            }

            var curve = BinaryMetrics.RocCurve(predictions.Probabilities, predictions.Labels);
            var step = Math.Max(1, (int)Math.Ceiling(curve.Length / (double)MaxCurvePoints));
            index = 0;

            for (var i = 0; i < curve.Length; i += step)
            {
                AddRocPoint(rows, runId, split, index++, curve[i]);

                if (i + step >= curve.Length && i != curve.Length - 1)
                {
                    AddRocPoint(rows, runId, split, index++, curve[^1]);
                }
            }
        }

        private static void AddRocPoint(List<AdvancedAnalyticsTrainingRunCurvePoint> rows, int runId,
            AdvancedAnalyticsSplit split, int index, RocPoint point)
        {
            rows.Add(new AdvancedAnalyticsTrainingRunCurvePoint
            {
                AdvancedAnalyticsTrainingRunId = runId,
                Curve = AdvancedAnalyticsCurve.Roc,
                Split = split,
                PointIndex = index,
                X = FiniteOrNull(point.FalsePositiveRate),
                Y = FiniteOrNull(point.TruePositiveRate),
                Threshold = FiniteOrNull(point.Threshold)
            });
        }
    }
}