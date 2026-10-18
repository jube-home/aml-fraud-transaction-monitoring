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
using System.Reflection;
using System.Text.Json;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.AdvancedAnalyticsPreservationCurvePointsQuery;
using Jube.Data.Query.AdvancedAnalyticsPreservationIterationsQuery;
using Jube.Data.Query.AdvancedAnalyticsPreservationMetricsQuery;
using Jube.Data.Query.AdvancedAnalyticsPreservationStepsQuery;
using Jube.Data.Query.AdvancedAnalyticsPreservationTrainedModelVersionsQuery;
using Jube.Data.Query.AdvancedAnalyticsPreservationTrialsQuery;
using Jube.Data.Query.AdvancedAnalyticsPreservationVariablesQuery;
using Jube.Data.Repository;
using Jube.Preservation.Exceptions;
using Jube.Preservation.Models;
using LinqToDB;
using LinqToDB.Mapping;

namespace Jube.Preservation
{
    internal sealed class AdvancedAnalyticsPreservation(DbContext dbContext, int tenantRegistryId, string userName)
    {
        private const string InterruptedMessage =
            "The run was still in progress when the package was exported and was cancelled on import.";

        private readonly AdvancedAnalyticsPreservationRepository repository =
            new(dbContext, tenantRegistryId, userName);

        public async Task<AdvancedAnalyticsPayload> ExportAsync(CancellationToken token)
        {
            var instances = await repository.ListInstancesAsync(token).ConfigureAwait(false);
            var trainedModels = await repository.ListTrainedModelsAsync(token).ConfigureAwait(false);
            var versions = await new AdvancedAnalyticsPreservationTrainedModelVersionsQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(token).ConfigureAwait(false);
            var runs = await repository.ListTrainingRunsAsync(token).ConfigureAwait(false);
            var steps = await new AdvancedAnalyticsPreservationStepsQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(token).ConfigureAwait(false);
            var trials = await new AdvancedAnalyticsPreservationTrialsQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(token).ConfigureAwait(false);
            var iterations = await new AdvancedAnalyticsPreservationIterationsQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(token).ConfigureAwait(false);
            var variables = await new AdvancedAnalyticsPreservationVariablesQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(token).ConfigureAwait(false);
            var metrics = await new AdvancedAnalyticsPreservationMetricsQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(token).ConfigureAwait(false);
            var curvePoints = await new AdvancedAnalyticsPreservationCurvePointsQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(token).ConfigureAwait(false);

            var exportedRunIds = runs.Select(s => s.Id).ToHashSet();

            foreach (var version in versions.Where(w => w.AdvancedAnalyticsTrainingRunId.HasValue
                                                        && !exportedRunIds.Contains(w.AdvancedAnalyticsTrainingRunId
                                                            .Value)))
            {
                version.AdvancedAnalyticsTrainingRunId = null;
            }

            foreach (var instance in instances)
            {
                instance.TenantRegistryId = 0;
            }

            foreach (var trainedModel in trainedModels)
            {
                trainedModel.TenantRegistryId = 0;
            }

            foreach (var version in versions)
            {
                version.TenantRegistryId = 0;
            }

            foreach (var run in runs)
            {
                run.TenantRegistryId = 0;
            }

            foreach (var step in steps)
            {
                step.TenantRegistryId = 0;
            }

            foreach (var trial in trials)
            {
                trial.TenantRegistryId = 0;
            }

            foreach (var iteration in iterations)
            {
                iteration.TenantRegistryId = 0;
            }

            foreach (var variable in variables)
            {
                variable.TenantRegistryId = 0;
            }

            foreach (var metric in metrics)
            {
                metric.TenantRegistryId = 0;
            }

            foreach (var curvePoint in curvePoints)
            {
                curvePoint.TenantRegistryId = 0;
            }

            DateTimeKinds.MarkUtc(instances);
            DateTimeKinds.MarkUtc(trainedModels);
            DateTimeKinds.MarkUtc(versions);
            DateTimeKinds.MarkUtc(runs);
            DateTimeKinds.MarkUtc(steps);
            DateTimeKinds.MarkUtc(trials);
            DateTimeKinds.MarkUtc(iterations);
            DateTimeKinds.MarkUtc(variables);
            DateTimeKinds.MarkUtc(metrics);
            DateTimeKinds.MarkUtc(curvePoints);

            return new AdvancedAnalyticsPayload
            {
                Instances = instances,
                TrainedModels = trainedModels,
                TrainedModelVersions = versions,
                TrainingRuns = runs,
                TrainingRunSteps = steps,
                TrainingRunTrials = trials,
                TrainingRunIterations = iterations,
                TrainingRunVariables = variables,
                TrainingRunMetrics = metrics,
                TrainingRunCurvePoints = curvePoints
            };
        }

        public Task DeleteExistingAsync(CancellationToken token)
        {
            return repository.SoftDeleteAllOfTenantAsync(token);
        }

        public async Task<IReadOnlyDictionary<int, int>> ImportAsync(AdvancedAnalyticsPayload payload,
            IReadOnlyDictionary<int, int> entityAnalysisModelIdMap, CancellationToken token)
        {
            var instances = Materialise(payload.Instances, "Instances");
            var trainedModels = Materialise(payload.TrainedModels, "TrainedModels");
            var versions = Materialise(payload.TrainedModelVersions, "TrainedModelVersions");
            var runs = Materialise(payload.TrainingRuns, "TrainingRuns");
            var steps = Materialise(payload.TrainingRunSteps, "TrainingRunSteps");
            var trials = Materialise(payload.TrainingRunTrials, "TrainingRunTrials");
            var iterations = Materialise(payload.TrainingRunIterations, "TrainingRunIterations");
            var variables = Materialise(payload.TrainingRunVariables, "TrainingRunVariables");
            var metrics = Materialise(payload.TrainingRunMetrics, "TrainingRunMetrics");
            var curvePoints = Materialise(payload.TrainingRunCurvePoints, "TrainingRunCurvePoints");

            var instanceIds = UniqueIds(instances, s => s.Id, "Instances");
            var trainedModelById = UniqueIds(trainedModels, s => s.Id, "TrainedModels");
            var versionById = UniqueIds(versions, s => s.Id, "TrainedModelVersions");
            var runById = UniqueIds(runs, s => s.Id, "TrainingRuns");
            var trialById = UniqueIds(trials, s => s.Id, "TrainingRunTrials");
            UniqueIds(steps, s => s.Id, "TrainingRunSteps");
            UniqueIds(iterations, s => s.Id, "TrainingRunIterations");
            UniqueIds(variables, s => s.Id, "TrainingRunVariables");
            UniqueIds(metrics, s => s.Id, "TrainingRunMetrics");
            UniqueIds(curvePoints, s => s.Id, "TrainingRunCurvePoints");

            ValidateStrings(instances, "Instances");
            ValidateStrings(trainedModels, "TrainedModels");
            ValidateStrings(versions, "TrainedModelVersions");
            ValidateStrings(runs, "TrainingRuns");
            ValidateStrings(steps, "TrainingRunSteps");
            ValidateStrings(trials, "TrainingRunTrials");
            ValidateStrings(iterations, "TrainingRunIterations");
            ValidateStrings(variables, "TrainingRunVariables");
            ValidateStrings(metrics, "TrainingRunMetrics");
            ValidateStrings(curvePoints, "TrainingRunCurvePoints");

            var liveNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var instance in instances)
            {
                RequireName(instance.Name, "Instances");
                RequireFlag(instance.Active, "Instances.Active");
                RequireFlag(instance.ResponsePayload, "Instances.ResponsePayload");
                RequireFlag(instance.ReportTable, "Instances.ReportTable");

                if (!liveNames.Add(instance.Name))
                {
                    throw Invalid($"Instances contains the name '{instance.Name}' more than once.");
                }
            }

            var versionNumbers = new HashSet<(int, int)>();

            foreach (var trainedModel in trainedModels)
            {
                RequireName(trainedModel.Name, "TrainedModels");
                RequireDefined(trainedModel.KindId, "TrainedModels.Kind");
                RequireDefined(trainedModel.SourceId, "TrainedModels.Source");
                RequireFlag(trainedModel.Active, "TrainedModels.Active");
                RequireParent(instanceIds, trainedModel.AdvancedAnalyticsInstanceId, "TrainedModels", "instance");
            }

            foreach (var version in versions)
            {
                RequireParent(trainedModelById, version.AdvancedAnalyticsTrainedModelId, "TrainedModelVersions",
                    "trained model");

                if (version.VersionNumber < 1)
                {
                    throw Invalid("TrainedModelVersions contains a version number below 1.");
                }

                if (!versionNumbers.Add((version.AdvancedAnalyticsTrainedModelId, version.VersionNumber)))
                {
                    throw Invalid("TrainedModelVersions contains a version number more than once for one model.");
                }

                ValidateJson(version.ModelJson, "TrainedModelVersions.ModelJson", true, false);
                ValidateJson(version.VariablesJson, "TrainedModelVersions.VariablesJson", false, true);
                ValidateJson(version.MetricsJson, "TrainedModelVersions.MetricsJson", false, true);
                ValidateJson(version.CalibratorJson, "TrainedModelVersions.CalibratorJson", false, false);
            }

            foreach (var trainedModel in trainedModels.Where(w => w.ActiveVersionId.HasValue))
            {
                if (!versionById.TryGetValue(trainedModel.ActiveVersionId!.Value, out var activeVersion)
                    || activeVersion.AdvancedAnalyticsTrainedModelId != trainedModel.Id)
                {
                    throw Invalid(
                        "TrainedModels contains an active version that is not a version of that trained model.");
                }
            }

            foreach (var run in runs)
            {
                RequireParent(instanceIds, run.AdvancedAnalyticsInstanceId, "TrainingRuns", "instance");
                RequireDefined(run.KindId, "TrainingRuns.Kind");
                RequireDefined(run.JobMode, "TrainingRuns.JobMode");
                RequireDefined(run.StatusId, "TrainingRuns.Status");
                ValidateJson(run.Arguments, "TrainingRuns.Arguments", false, true);

                if (double.IsNaN(run.Progress) || run.Progress < 0 || run.Progress > 1)
                {
                    throw Invalid("TrainingRuns contains a progress outside 0 to 1.");
                }
            }

            foreach (var step in steps)
            {
                RequireParent(runById, step.AdvancedAnalyticsTrainingRunId, "TrainingRunSteps", "run");
                RequireDefined(step.Step, "TrainingRunSteps.Step");
                RequireDefined(step.StatusId, "TrainingRunSteps.Status");
            }

            foreach (var trial in trials)
            {
                RequireParent(runById, trial.AdvancedAnalyticsTrainingRunId, "TrainingRunTrials", "run");
                RequireDefined(trial.StatusId, "TrainingRunTrials.Status");
                RequireFlag(trial.Selected, "TrainingRunTrials.Selected");
                ValidateJson(trial.Hyperparameters, "TrainingRunTrials.Hyperparameters", false, true);
            }

            foreach (var iteration in iterations)
            {
                RequireParent(runById, iteration.AdvancedAnalyticsTrainingRunId, "TrainingRunIterations", "run");

                RequireTrialOfRun(trialById, iteration.AdvancedAnalyticsTrainingRunTrialId,
                    iteration.AdvancedAnalyticsTrainingRunId, "TrainingRunIterations");
            }

            foreach (var variable in variables)
            {
                RequireParent(runById, variable.AdvancedAnalyticsTrainingRunId, "TrainingRunVariables", "run");
                RequireName(variable.Name, "TrainingRunVariables");
            }

            foreach (var metric in metrics)
            {
                RequireParent(runById, metric.AdvancedAnalyticsTrainingRunId, "TrainingRunMetrics", "run");

                RequireTrialOfRun(trialById, metric.AdvancedAnalyticsTrainingRunTrialId,
                    metric.AdvancedAnalyticsTrainingRunId, "TrainingRunMetrics");

                RequireName(metric.Name, "TrainingRunMetrics");
                RequireDefined(metric.Split, "TrainingRunMetrics.Split");
            }

            foreach (var curvePoint in curvePoints)
            {
                RequireParent(runById, curvePoint.AdvancedAnalyticsTrainingRunId, "TrainingRunCurvePoints", "run");
                RequireDefined(curvePoint.Curve, "TrainingRunCurvePoints.Curve");
                RequireDefined(curvePoint.Split, "TrainingRunCurvePoints.Split");
            }

            var instanceGuids = await ResolveGuidsAsync(instances.Select(s => s.Guid).ToList(),
                repository.InstanceGuidsInUseAsync, token).ConfigureAwait(false);
            var trainedModelGuids = await ResolveGuidsAsync(trainedModels.Select(s => s.Guid).ToList(),
                repository.TrainedModelGuidsInUseAsync, token).ConfigureAwait(false);
            var versionGuids = await ResolveGuidsAsync(versions.Select(s => s.Guid).ToList(),
                repository.TrainedModelVersionGuidsInUseAsync, token).ConfigureAwait(false);
            var runGuids = await ResolveGuidsAsync(runs.Select(s => s.Guid).ToList(),
                repository.TrainingRunGuidsInUseAsync, token).ConfigureAwait(false);

            var newInstanceIds = new Dictionary<int, int>();

            foreach (var instance in instances)
            {
                var oldId = instance.Id;
                var oldVersion = instance.Version;
                var oldModelId = instance.EntityAnalysisModelId;
                instance.Guid = instanceGuids.Next(instance.Guid);

                int? newModelId = oldModelId.HasValue &&
                                  entityAnalysisModelIdMap.TryGetValue(oldModelId.Value, out var mapped)
                    ? mapped
                    : null;

                newInstanceIds[oldId] = await repository.InsertInstanceAsync(instance, newModelId, token)
                    .ConfigureAwait(false);

                if (oldVersion > 1)
                {
                    instance.Version = oldVersion;
                    await dbContext.UpdateAsync(instance, token: token).ConfigureAwait(false);
                }
            }

            var newRunIds = new Dictionary<int, int>();
            var runInstanceIds = new Dictionary<int, int>();

            foreach (var run in runs)
            {
                var oldId = run.Id;
                var newInstanceId = newInstanceIds[run.AdvancedAnalyticsInstanceId];

                run.Guid = runGuids.Next(run.Guid);
                run.AdvancedAnalyticsInstanceId = newInstanceId;
                run.InputPointer = null;
                run.HeartbeatDate = null;
                run.ClaimedBy = null;
                run.ClaimedDate = null;
                MakeTerminal(run);

                newRunIds[oldId] = await repository.InsertTrainingRunAsync(run, token).ConfigureAwait(false);
                runInstanceIds[oldId] = newInstanceId;
            }

            var newTrialIds = new Dictionary<int, int>();

            foreach (var trial in trials)
            {
                var oldId = trial.Id;
                var oldRunId = trial.AdvancedAnalyticsTrainingRunId;

                trial.AdvancedAnalyticsInstanceId = runInstanceIds[oldRunId];
                trial.AdvancedAnalyticsTrainingRunId = newRunIds[oldRunId];
                trial.StatusId = Terminal(trial.StatusId);

                newTrialIds[oldId] = await repository.InsertTrialAsync(trial, token).ConfigureAwait(false);
            }

            foreach (var step in steps)
            {
                var oldRunId = step.AdvancedAnalyticsTrainingRunId;

                step.AdvancedAnalyticsInstanceId = runInstanceIds[oldRunId];
                step.AdvancedAnalyticsTrainingRunId = newRunIds[oldRunId];
                step.StatusId = Terminal(step.StatusId);

                await repository.InsertStepAsync(step, token).ConfigureAwait(false);
            }

            foreach (var variable in variables)
            {
                var oldRunId = variable.AdvancedAnalyticsTrainingRunId;

                variable.AdvancedAnalyticsInstanceId = runInstanceIds[oldRunId];
                variable.AdvancedAnalyticsTrainingRunId = newRunIds[oldRunId];

                await repository.InsertVariableAsync(variable, token).ConfigureAwait(false);
            }

            foreach (var iteration in iterations)
            {
                var oldRunId = iteration.AdvancedAnalyticsTrainingRunId;

                iteration.AdvancedAnalyticsInstanceId = runInstanceIds[oldRunId];
                iteration.AdvancedAnalyticsTrainingRunId = newRunIds[oldRunId];

                iteration.AdvancedAnalyticsTrainingRunTrialId = iteration.AdvancedAnalyticsTrainingRunTrialId.HasValue
                    ? newTrialIds[iteration.AdvancedAnalyticsTrainingRunTrialId.Value]
                    : null;

                await repository.InsertIterationAsync(iteration, token).ConfigureAwait(false);
            }

            foreach (var metric in metrics)
            {
                var oldRunId = metric.AdvancedAnalyticsTrainingRunId;

                metric.AdvancedAnalyticsInstanceId = runInstanceIds[oldRunId];
                metric.AdvancedAnalyticsTrainingRunId = newRunIds[oldRunId];

                metric.AdvancedAnalyticsTrainingRunTrialId = metric.AdvancedAnalyticsTrainingRunTrialId.HasValue
                    ? newTrialIds[metric.AdvancedAnalyticsTrainingRunTrialId.Value]
                    : null;

                await repository.InsertMetricAsync(metric, token).ConfigureAwait(false);
            }

            foreach (var curvePoint in curvePoints)
            {
                var oldRunId = curvePoint.AdvancedAnalyticsTrainingRunId;

                curvePoint.AdvancedAnalyticsInstanceId = runInstanceIds[oldRunId];
                curvePoint.AdvancedAnalyticsTrainingRunId = newRunIds[oldRunId];

                await repository.InsertCurvePointAsync(curvePoint, token).ConfigureAwait(false);
            }

            var activations = trainedModels
                .Where(w => w.ActiveVersionId.HasValue)
                .Select(s => (OldModelId: s.Id, OldActiveVersionId: s.ActiveVersionId!.Value, Active: s.Active == 1))
                .ToList();

            var newTrainedModelIds = new Dictionary<int, int>();

            foreach (var trainedModel in trainedModels)
            {
                var oldId = trainedModel.Id;

                trainedModel.Guid = trainedModelGuids.Next(trainedModel.Guid);
                trainedModel.AdvancedAnalyticsInstanceId = newInstanceIds[trainedModel.AdvancedAnalyticsInstanceId];

                newTrainedModelIds[oldId] = await repository.InsertTrainedModelAsync(trainedModel, token)
                    .ConfigureAwait(false);
            }

            var newVersionIds = new Dictionary<int, int>();

            foreach (var version in versions)
            {
                var oldId = version.Id;

                version.Guid = versionGuids.Next(version.Guid);
                version.AdvancedAnalyticsTrainedModelId = newTrainedModelIds[version.AdvancedAnalyticsTrainedModelId];

                version.AdvancedAnalyticsTrainingRunId = version.AdvancedAnalyticsTrainingRunId.HasValue
                                                         && newRunIds.TryGetValue(
                                                             version.AdvancedAnalyticsTrainingRunId.Value,
                                                             out var mappedRunId)
                    ? mappedRunId
                    : null;

                newVersionIds[oldId] = await repository.InsertTrainedModelVersionAsync(version, token)
                    .ConfigureAwait(false);
            }

            foreach (var (oldModelId, oldActiveVersionId, active) in activations)
            {
                await repository.SetTrainedModelActiveVersionAsync(newTrainedModelIds[oldModelId],
                    newVersionIds[oldActiveVersionId], active, token).ConfigureAwait(false);
            }

            return newInstanceIds;
        }

        private static List<T> Materialise<T>(IEnumerable<T>? source, string what)
        {
            if (source == null)
            {
                return [];
            }

            var list = source.ToList();

            if (list.Count > AdvancedAnalyticsPreservationLimits.MaximumRowsPerCollection)
            {
                throw Invalid(
                    $"{what} holds more than {AdvancedAnalyticsPreservationLimits.MaximumRowsPerCollection} rows.");
            }

            if (list.Any(a => a == null))
            {
                throw Invalid($"{what} holds an empty row.");
            }

            return list;
        }

        private static Dictionary<long, T> UniqueIds<T>(List<T> rows, Func<T, long> id, string what)
        {
            var byId = new Dictionary<long, T>(rows.Count);

            foreach (var row in rows)
            {
                if (!byId.TryAdd(id(row), row))
                {
                    throw Invalid($"{what} holds the same row identifier more than once.");
                }
            }

            return byId;
        }

        private static void RequireParent<T>(Dictionary<long, T> parents, long id, string what, string parent)
        {
            if (!parents.ContainsKey(id))
            {
                throw Invalid($"{what} refers to a {parent} that is not in the package.");
            }
        }

        private static void RequireTrialOfRun(Dictionary<long, AdvancedAnalyticsTrainingRunTrial> trials,
            int? trialId, int runId, string what)
        {
            if (!trialId.HasValue)
            {
                return;
            }

            if (!trials.TryGetValue(trialId.Value, out var trial) || trial.AdvancedAnalyticsTrainingRunId != runId)
            {
                throw Invalid($"{what} refers to a trial that is not a trial of the same run.");
            }
        }

        private static void RequireName(string? name, string what)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                name.Length > AdvancedAnalyticsPreservationLimits.MaximumNameCharacters)
            {
                throw Invalid(
                    $"{what} holds a name that is empty or longer than {AdvancedAnalyticsPreservationLimits.MaximumNameCharacters} characters.");
            }
        }

        private static void RequireFlag(byte value, string what)
        {
            if (value > 1)
            {
                throw Invalid($"{what} must be 0 or 1.");
            }
        }

        private static void RequireDefined<T>(T value, string what) where T : struct, Enum
        {
            if (!Enum.IsDefined(value))
            {
                throw Invalid($"{what} holds a value that is not defined.");
            }
        }

        private static void ValidateJson(string? json, string what, bool required, bool binary)
        {
            if (json == null)
            {
                if (required)
                {
                    throw Invalid($"{what} is required.");
                }

                return;
            }

            if (json.Length > AdvancedAnalyticsPreservationLimits.MaximumJsonCharacters)
            {
                throw Invalid(
                    $"{what} is larger than {AdvancedAnalyticsPreservationLimits.MaximumJsonCharacters} characters.");
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                throw Invalid($"{what} is empty.");
            }

            if (binary && json.Contains("\\u0000", StringComparison.Ordinal))
            {
                throw Invalid($"{what} holds a null character escape.");
            }

            try
            {
                using var document = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    MaxDepth = AdvancedAnalyticsPreservationLimits.MaximumJsonDepth
                });
            }
            catch (JsonException)
            {
                throw Invalid(
                    $"{what} is not well formed JSON within the depth limit of {AdvancedAnalyticsPreservationLimits.MaximumJsonDepth}.");
            }
        }

        private static void ValidateStrings<T>(List<T> rows, string what)
        {
            foreach (var row in rows)
            {
                foreach (var (property, limit) in StringProperties<T>.All)
                {
                    var value = (string?)property.GetValue(row);

                    if (value == null)
                    {
                        continue;
                    }

                    if (value.Length > limit)
                    {
                        throw Invalid($"{what}.{property.Name} is longer than {limit} characters.");
                    }

                    if (value.Contains('\0'))
                    {
                        throw Invalid($"{what}.{property.Name} holds a null character.");
                    }
                }
            }
        }

        private static async Task<GuidAllocator> ResolveGuidsAsync(List<Guid> guids,
            Func<IReadOnlyCollection<Guid>, CancellationToken, Task<HashSet<Guid>>> inUse, CancellationToken token)
        {
            return new GuidAllocator(await inUse(guids, token).ConfigureAwait(false));
        }

        private static InvalidAdvancedAnalyticsPreservationException Invalid(string message)
        {
            return new InvalidAdvancedAnalyticsPreservationException(message);
        }

        private static AdvancedAnalyticsStatus Terminal(AdvancedAnalyticsStatus status)
        {
            return status is AdvancedAnalyticsStatus.Pending or AdvancedAnalyticsStatus.Running
                or AdvancedAnalyticsStatus.Cancelling
                ? AdvancedAnalyticsStatus.Cancelled
                : status;
        }

        private static void MakeTerminal(AdvancedAnalyticsTrainingRun run)
        {
            var terminal = Terminal(run.StatusId);

            if (terminal == run.StatusId)
            {
                return;
            }

            run.StatusId = terminal;
            run.CompletedDate ??= DateTime.UtcNow;
            run.Error ??= InterruptedMessage;
        }

        private sealed class GuidAllocator(HashSet<Guid> inUse)
        {
            private readonly HashSet<Guid> seen = [];

            public Guid Next(Guid candidate)
            {
                if (candidate == Guid.Empty || inUse.Contains(candidate) || !seen.Add(candidate))
                {
                    var fresh = Guid.NewGuid();
                    seen.Add(fresh);

                    return fresh;
                }

                return candidate;
            }
        }

        private static class StringProperties<T>
        {
            public static readonly IReadOnlyList<(PropertyInfo Property, int Limit)> All =
                typeof(T).GetProperties()
                    .Where(w => w.PropertyType == typeof(string) && w.CanRead
                                                                 && w.GetCustomAttribute<ColumnAttribute>() != null)
                    .Select(s => (s, IsJson(s)
                        ? AdvancedAnalyticsPreservationLimits.MaximumJsonCharacters
                        : AdvancedAnalyticsPreservationLimits.MaximumTextCharacters))
                    .ToList();

            private static bool IsJson(PropertyInfo property)
            {
                var dataType = property.GetCustomAttribute<DataTypeAttribute>()?.DataType;

                return dataType is DataType.Json or DataType.BinaryJson;
            }
        }
    }
}