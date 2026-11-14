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
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Regression;
using log4net;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsKernelRidgeRegressorTrainingExecutor(
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

        private readonly AskJoobyAnalyticsTrainedModelRepository models = new(dbContext, tenantRegistryId, userName);
        private readonly AdvancedAnalyticsTrainingRunRepository runs = new(dbContext, tenantRegistryId, userName);

        private readonly IAdvancedAnalyticsTrainingTrace trace =
            new AdvancedAnalyticsDatabaseTrainingTrace(dbContext, tenantRegistryId, userName);

        private readonly AdvancedAnalyticsTrainingRunVariableRepository variables = new(dbContext, tenantRegistryId,
            userName);

        private readonly AskJoobyAnalyticsTrainedModelVersionRepository versions = new(dbContext, tenantRegistryId,
            userName);

        public Func<double[][], double[], string[], KernelRidgeRegressorOptions, CancellationToken,
            KernelRidgeRegressorResult> Trainer { get; set; } =
            KernelRidgeRegressorTrainer.Train;

        private IAdvancedAnalyticsTrialRecorder Recorder { get; set; } = null!;

        public Task<AdvancedAnalyticsKernelRidgeRegressorExecutionResult> ExecuteAsync(
            AdvancedAnalyticsTrainingRun run, KernelRidgeRegressorTrainingArguments arguments,
            CancellationToken token)
        {
            return ExecuteAsync(run, arguments, _ =>
            {
                if (arguments.Inputs == null || arguments.Outputs == null)
                {
                    throw new AdvancedAnalyticsTrainingInputException(
                        "The run carries no inline rows to train on.");
                }

                return Task.FromResult((arguments.Inputs, arguments.Outputs));
            }, token);
        }

        public async Task<AdvancedAnalyticsKernelRidgeRegressorExecutionResult> ExecuteAsync(
            AdvancedAnalyticsTrainingRun run, KernelRidgeRegressorTrainingArguments arguments,
            Func<CancellationToken, Task<(double[][] Inputs, double[] Outputs)>> loadInput, CancellationToken token)
        {
            Recorder = new DbAdvancedAnalyticsTrialRecorder(dbContext, tenantRegistryId, userName, run.Id);

            (double[][] Inputs, double[] Outputs) data;
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

                KernelRidgeRegressorResult result;

                try
                {
                    var options = arguments.ToOptions();
                    var names = arguments.VariableNames ?? [];

                    result = await AdvancedAnalyticsDedicatedThread
                        .RunAsync(() => Trainer(data.Inputs, data.Outputs, names, options, token), token)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                {
                    var message = Truncate(ex.Message);

                    await CompleteTrialQuietlyAsync(trial.Id, AdvancedAnalyticsStatus.Failed, null, message)
                        .ConfigureAwait(false);

                    return await FailAsync(run, message).ConfigureAwait(false);
                }

                var outcome = await PersistAsync(run, arguments, trial.Id, result,
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
                log.Error(
                    $"AdvancedAnalyticsKernelRidgeRegressorTrainingExecutor: could not record {status} for run {runId}",
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

        private async Task<AdvancedAnalyticsKernelRidgeRegressorExecutionResult> FailAsync(
            AdvancedAnalyticsTrainingRun run, string error)
        {
            var message = Truncate(error);
            await RecordTerminalAsync(run.Id, AdvancedAnalyticsStatus.Failed, message).ConfigureAwait(false);

            return new AdvancedAnalyticsKernelRidgeRegressorExecutionResult
            {
                RunId = run.Id,
                RunGuid = run.Guid,
                Status = AdvancedAnalyticsStatus.Failed,
                Error = message
            };
        }

        private async Task<AdvancedAnalyticsKernelRidgeRegressorExecutionResult> PersistAsync(
            AdvancedAnalyticsTrainingRun run, KernelRidgeRegressorTrainingArguments arguments,
            int trialId, KernelRidgeRegressorResult result, TimeSpan loadDuration,
            CancellationToken token)
        {
            await BoundaryAsync(run, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Load, new Dictionary<string, double>
            {
                [AdvancedAnalyticsDatabaseTrainingTrace.DurationKey] = loadDuration.TotalMilliseconds
            }, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Clean, new Dictionary<string, double>
            {
                ["Train.Variables"] = result.Preprocessing.Length
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
                ["Train.TrainingRowsInExpansion"] = result.Details.TrainingRowsInExpansion,
                ["Train.RidgePenalty"] = result.Details.RidgePenalty
            }, token).ConfigureAwait(false);

            var validateMetrics = new Dictionary<string, double>();
            AddSplitMetrics(validateMetrics, result.Details.TrainMetrics);
            AddSplitMetrics(validateMetrics, result.Details.ValidationMetrics);
            AddSplitMetrics(validateMetrics, result.Details.TestMetrics);

            await AppendAsync(run, AdvancedAnalyticsStep.Validate, validateMetrics, token).ConfigureAwait(false);

            await BoundaryAsync(run, token).ConfigureAwait(false);

            await variables.InsertRangeAsync(VariableRows(run.Id, result), token).ConfigureAwait(false);

            var serialiseStopwatch = Stopwatch.StartNew();
            var evidenceJson = AdvancedAnalyticsEvidenceJson.Serialise(EvidenceOf(result));
            var variablesJson = VariablesJsonOf(arguments, result.Variables);

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
                    .FirstOrDefault(m => m.KindId == AdvancedAnalyticsKind.Regressor
                                         && string.Equals(m.Name, arguments.ModelName,
                                             StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    model = await models.InsertAsync(new AskJoobyAnalyticsTrainedModel
                    {
                        AskJoobyAnalyticsInstanceId = run.AskJoobyAnalyticsInstanceId,
                        Name = arguments.ModelName!,
                        KindId = AdvancedAnalyticsKind.Regressor,
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

            var score = result.Details.TestMetrics?.RSquared ?? result.Details.ValidationMetrics?.RSquared
                ?? result.Details.TrainMetrics?.RSquared;

            await CompleteTrialQuietlyAsync(trialId, AdvancedAnalyticsStatus.Succeeded,
                score.HasValue && double.IsFinite(score.Value) ? score : null, null).ConfigureAwait(false);

            try
            {
                await Recorder.MarkSelectedAsync(trialId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"AdvancedAnalyticsKernelRidgeRegressorTrainingExecutor: could not select trial {trialId} of run {run.Id}",
                    ex);
            }

            await runs.UpdateProgressAsync(run.Id, 1, CancellationToken.None).ConfigureAwait(false);

            await runs.UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Succeeded, null, CancellationToken.None)
                .ConfigureAwait(false);

            return new AdvancedAnalyticsKernelRidgeRegressorExecutionResult
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

        private static AdvancedAnalyticsEvidence<KernelRidgeRegressorEvidenceDetails> EvidenceOf(
            KernelRidgeRegressorResult result)
        {
            return new AdvancedAnalyticsEvidence<KernelRidgeRegressorEvidenceDetails>
            {
                Kind = result.Kind,
                Seed = result.Seed,
                Variables = result.Variables,
                SplitSizes = new AdvancedAnalyticsSplitSizes(result.SplitSizes.Train,
                    result.SplitSizes.Validation, result.SplitSizes.Test),
                Preprocessing = result.Preprocessing,
                Warnings = result.Warnings,
                Details = result.Details
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
                    $"AdvancedAnalyticsKernelRidgeRegressorTrainingExecutor: could not record trial {trialId} as {status}",
                    ex);
            }
        }

        private Task AppendAsync(AdvancedAnalyticsTrainingRun run, AdvancedAnalyticsStep step,
            Dictionary<string, double> metrics, CancellationToken token)
        {
            return trace.AppendAsync(
                new AdvancedAnalyticsTrainingTraceEntry(run.Guid, (int)step, step.ToString(), metrics), token);
        }

        private static string HyperparametersOf(KernelRidgeRegressorTrainingArguments arguments)
        {
            return arguments.WithoutData().ToJson();
        }

        private static string VariablesJsonOf(KernelRidgeRegressorTrainingArguments arguments,
            string[] variablesInOrder)
        {
            return AdvancedAnalyticsVariableRowsJson.Build(variablesInOrder, arguments.VariableNames!,
                arguments.ProcessingTypeIds!, arguments.TransformationIds, arguments.TransformationParametersJson);
        }

        private static void AddSplitMetrics(Dictionary<string, double> target,
            KernelRidgeRegressorSplitMetrics metrics)
        {
            if (metrics == null)
            {
                return;
            }

            var prefix = metrics.Split + ".";
            Add(target, prefix + "Rows", metrics.Rows);
            Add(target, prefix + "RSquared", metrics.RSquared);
            Add(target, prefix + "AdjustedRSquared", metrics.AdjustedRSquared);
            Add(target, prefix + "RootMeanSquaredError", metrics.RootMeanSquaredError);
            Add(target, prefix + "MeanAbsoluteError", metrics.MeanAbsoluteError);
            Add(target, prefix + "MeanError", metrics.MeanError);
        }

        private static void Add(Dictionary<string, double> target, string key, double? value)
        {
            if (value.HasValue && double.IsFinite(value.Value))
            {
                target[key] = value.Value;
            }
        }

        private static List<AdvancedAnalyticsTrainingRunVariable> VariableRows(int runId,
            KernelRidgeRegressorResult result)
        {
            var rows = new List<AdvancedAnalyticsTrainingRunVariable>();

            for (var j = 0; j < result.Preprocessing.Length; j++)
            {
                var info = result.Preprocessing[j];

                rows.Add(new AdvancedAnalyticsTrainingRunVariable
                {
                    AdvancedAnalyticsTrainingRunId = runId,
                    VariableSequence = j,
                    Name = info.Name,
                    Mean = FiniteOrNull(info.Mean),
                    StandardDeviation = FiniteOrNull(info.StandardDeviation),
                    RowCount = result.SplitSizes.Train
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