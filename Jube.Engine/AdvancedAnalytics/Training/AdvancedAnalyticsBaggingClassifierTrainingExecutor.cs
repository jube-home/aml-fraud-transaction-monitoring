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
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Regression;
using log4net;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Classification.Models;
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsBaggingClassifierTrainingExecutor(
        DbContext dbContext,
        int tenantRegistryId,
        string userName,
        ILog log,
        AdvancedAnalyticsExecutionMessages messages = null)
    {
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

        public Func<double[][], int[], string[], BaggingClassifierOptions, CancellationToken,
            BaggingClassifierResult> Trainer { get; set; } = BaggingClassifierTrainer.Train;

        public async Task<BaggingClassifierExecutionResult> ExecuteAsync(AdvancedAnalyticsTrainingRun run,
            BaggingClassifierTrainingArguments arguments,
            Func<CancellationToken, Task<(double[][] Inputs, int[] Labels)>> loadInput, CancellationToken token)
        {
            (double[][] Inputs, int[] Labels) data;
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

            try
            {
                await BoundaryAsync(run, token).ConfigureAwait(false);

                BaggingClassifierResult result;

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
                    var message = AdvancedAnalyticsTrainingExecutor.Truncate(ex.Message);

                    return await FailAsync(run, message).ConfigureAwait(false);
                }

                return await PersistAsync(run, arguments, result, loadStopwatch.Elapsed, token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await RecordTerminalAsync(run.Id, AdvancedAnalyticsStatus.Cancelled, messages.Cancelled)
                    .ConfigureAwait(false);

                throw;
            }
            catch (Exception)
            {
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
                    $"AdvancedAnalyticsBaggingClassifierTrainingExecutor: could not record {status} for run {runId}",
                    ex);
            }
        }

        private async Task BoundaryAsync(AdvancedAnalyticsTrainingRun run, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var run2 = await runs.GetByIdAsync(run.Id, token).ConfigureAwait(false);

            if (run2 is { StatusId: AdvancedAnalyticsStatus.Cancelling or AdvancedAnalyticsStatus.Cancelled })
            {
                await runs.UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Cancelled, messages.Cancelled,
                    CancellationToken.None).ConfigureAwait(false);

                throw new AdvancedAnalyticsRunStoppedException();
            }
        }

        private async Task<BaggingClassifierExecutionResult> FailAsync(AdvancedAnalyticsTrainingRun run,
            string error)
        {
            var message = AdvancedAnalyticsTrainingExecutor.Truncate(error);
            await RecordTerminalAsync(run.Id, AdvancedAnalyticsStatus.Failed, message).ConfigureAwait(false);

            return new BaggingClassifierExecutionResult
            {
                RunId = run.Id,
                RunGuid = run.Guid,
                Status = AdvancedAnalyticsStatus.Failed,
                Error = message
            };
        }

        private async Task<BaggingClassifierExecutionResult> PersistAsync(AdvancedAnalyticsTrainingRun run,
            BaggingClassifierTrainingArguments arguments, BaggingClassifierResult result,
            TimeSpan loadDuration, CancellationToken token)
        {
            await BoundaryAsync(run, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Load, new Dictionary<string, double>
            {
                [AdvancedAnalyticsDatabaseTrainingTrace.DurationKey] = loadDuration.TotalMilliseconds
            }, token).ConfigureAwait(false);

            await AppendAsync(run, AdvancedAnalyticsStep.Clean, new Dictionary<string, double>
            {
                ["Train.ConstantColumns"] =
                    result.Warnings.Count(w => w.Code == BaggingWarningCodes.ConstantColumn)
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
                ["Train.MaxDepth"] = result.Details.MaxDepth
            }, token).ConfigureAwait(false);

            var validateMetrics = new Dictionary<string, double>();
            AddIfFinite(validateMetrics, "OutOfBag.RocAuc", result.Details.OutOfBagRocAuc);
            AddIfFinite(validateMetrics, "Train.RocAuc", result.Details.TrainMetrics?.RocAuc);
            AddIfFinite(validateMetrics, "Validation.RocAuc", result.Details.ValidationMetrics?.RocAuc);
            AddIfFinite(validateMetrics, "Test.RocAuc", result.Details.TestMetrics?.RocAuc);
            AddIfFinite(validateMetrics, "Test.LogLoss", result.Details.TestMetrics?.LogLoss);
            AddIfFinite(validateMetrics, "Test.BrierScore", result.Details.TestMetrics?.BrierScore);

            await AppendAsync(run, AdvancedAnalyticsStep.Validate, validateMetrics, token).ConfigureAwait(false);

            await BoundaryAsync(run, token).ConfigureAwait(false);

            await variables.InsertRangeAsync(VariableRows(run.Id, result), token).ConfigureAwait(false);

            var serialiseStopwatch = Stopwatch.StartNew();

            var evidence = new AdvancedAnalyticsEvidence<BaggingEvidenceDetails>
            {
                Kind = result.Kind,
                Seed = result.Seed,
                Variables = result.Variables,
                SplitSizes = result.SplitSizes,
                Preprocessing = result.Preprocessing,
                Warnings = result.Warnings,
                Details = result.Details
            };

            var evidenceJson = AdvancedAnalyticsEvidenceJson.Serialise(evidence);
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
                        CalibratorJson = null,
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

            await runs.UpdateProgressAsync(run.Id, 1, CancellationToken.None).ConfigureAwait(false);

            await runs.UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Succeeded, null, CancellationToken.None)
                .ConfigureAwait(false);

            return new BaggingClassifierExecutionResult
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

        private Task AppendAsync(AdvancedAnalyticsTrainingRun run, AdvancedAnalyticsStep step,
            Dictionary<string, double> metrics, CancellationToken token)
        {
            return trace.AppendAsync(
                new AdvancedAnalyticsTrainingTraceEntry(run.Guid, (int)step, step.ToString(), metrics), token);
        }

        private static void AddIfFinite(Dictionary<string, double> target, string key, double? value)
        {
            if (value.HasValue && double.IsFinite(value.Value))
            {
                target[key] = value.Value;
            }
        }

        private static string VariablesJsonOf(BaggingClassifierTrainingArguments arguments,
            string[] variablesInOrder)
        {
            return AdvancedAnalyticsVariableRowsJson.Build(variablesInOrder, arguments.VariableNames!,
                arguments.ProcessingTypeIds!, arguments.TransformationIds, arguments.TransformationParametersJson);
        }

        private static List<AdvancedAnalyticsTrainingRunVariable> VariableRows(int runId,
            BaggingClassifierResult result)
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
                    ImputationValue = FiniteOrNull(result.FillValues[j]),
                    MissingCount = result.MissingCounts[j],
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