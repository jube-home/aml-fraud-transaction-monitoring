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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper.Internal;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Engine.AdvancedAnalytics.Regression;
using Newtonsoft.Json;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    public static class SyncAdvancedAnalyticsInstancesExtensions
    {
        private const string SyncUserName = "Jube.Engine";

        public static async Task<Context> SyncAdvancedAnalyticsInstancesAsync(this Context context)
        {
            var loaded = 0;

            try
            {
                context.Services.Log.Info("Entity Start: Advanced Analytics sync started.");

                context.Services.Parser.EntityAnalysisModelsAdvancedAnalyticsAdaptations = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    var tenantRegistryId = value.Instance.TenantRegistryId;

                    var instanceRepository = new AdvancedAnalyticsInstanceRepository(context.Services.DbContext,
                        tenantRegistryId, SyncUserName);
                    var trainedModelRepository = new AdvancedAnalyticsTrainedModelRepository(
                        context.Services.DbContext, tenantRegistryId, SyncUserName);
                    var versionRepository = new AdvancedAnalyticsTrainedModelVersionRepository(
                        context.Services.DbContext, tenantRegistryId, SyncUserName);

                    var instances = await instanceRepository
                        .GetApprovedByEntityAnalysisModelIdOrderByIdAsync(key, context.ApprovalsRequired,
                            context.Services.CancellationToken)
                        .ConfigureAwait(false);

                    var shadow = new List<AdvancedAnalyticsModel>();

                    foreach (var instance in instances)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        if (instance.Active != 1)
                        {
                            continue;
                        }

                        try
                        {
                            var trainedModels = await trainedModelRepository
                                .ListAsync(instance.Id, context.Services.CancellationToken).ConfigureAwait(false);

                            foreach (var trainedModel in trainedModels)
                            {
                                context.Services.CancellationToken.ThrowIfCancellationRequested();

                                if (trainedModel.Active != 1 || trainedModel.ActiveVersionId == null)
                                {
                                    continue;
                                }

                                try
                                {
                                    var version = await versionRepository
                                        .GetActiveByModelIdAsync(trainedModel.Id, context.Services.CancellationToken)
                                        .ConfigureAwait(false);

                                    if (version == null)
                                    {
                                        continue;
                                    }

                                    var built = BuildAdvancedAnalyticsModel(instance, trainedModel, version,
                                        ex => context.Services.Log.Error(
                                            $"Entity Start: Advanced Analytics trained model {trainedModel.Id} version {version.Id} has created an error during loading as {ex}."));

                                    if (built == null)
                                    {
                                        continue;
                                    }

                                    if (shadow.Any(a => a.Name == built.Name))
                                    {
                                        context.Services.Log.Error(
                                            $"Entity Start: Advanced Analytics trained model {trainedModel.Id} is skipped as the name {built.Name} is already used by another loaded model of entity model {key}.");

                                        continue;
                                    }

                                    shadow.Add(built);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Advanced Analytics trained model {built.Id} has added {built.Name} to shadow collection.");
                                    }

                                    context.Services.Parser.EntityAnalysisModelsAdvancedAnalyticsAdaptations
                                        .TryAdd(built.Name);
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    context.Services.Log.Error(
                                        $"Entity Start: Advanced Analytics trained model {trainedModel.Id} returned for model {key} has created an error as {ex}.");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Advanced Analytics instance {instance.Id} returned for model {key} has created an error as {ex}.");
                        }
                    }

                    context.Snapshots.Builder(key, value).AdvancedAnalyticsModels = shadow;
                    loaded += shadow.Count;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncAdvancedAnalyticsInstancesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(
                        EntityAnalysisModelSynchronisationErrorRepository
                            .EntityAnalysisModelSynchronisationErrorStepEnum
                            .AdvancedAnalyticsInstances, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            context.Services.Log.Info(
                $"Entity Start: Advanced Analytics sync ended with {loaded} Advanced Analytics models loaded.");

            return context;
        }

        internal static List<AdvancedAnalyticsVariableRow> ParseVariableRows(string variablesJson)
        {
            if (string.IsNullOrWhiteSpace(variablesJson))
            {
                throw new JsonException("The version has no variables json.");
            }

            var rows = AdvancedAnalyticsEvidenceJson.Deserialise<List<AdvancedAnalyticsVariableRow>>(variablesJson);

            if (rows == null)
            {
                throw new JsonException("The version variables json is empty.");
            }

            return rows;
        }

        internal static AdvancedAnalyticsModel BuildAdvancedAnalyticsModel(
            AdvancedAnalyticsInstance instance,
            AdvancedAnalyticsTrainedModel trainedModel,
            AdvancedAnalyticsTrainedModelVersion version,
            Action<Exception> onLoadError = null)
        {
            try
            {
                var variableRows = ParseVariableRows(version.VariablesJson);

                return BuildAdvancedAnalyticsModel(instance, trainedModel, version, variableRows, onLoadError);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onLoadError?.Invoke(ex);

                return null;
            }
        }

        internal static AdvancedAnalyticsModel BuildAdvancedAnalyticsModel(
            AdvancedAnalyticsInstance instance,
            AdvancedAnalyticsTrainedModel trainedModel,
            AdvancedAnalyticsTrainedModelVersion version,
            IReadOnlyList<AdvancedAnalyticsVariableRow> variableRows,
            Action<Exception> onLoadError = null)
        {
            try
            {
                var recall = AdvancedAnalyticsModelRegistry.Build(version.ModelJson);

                if (variableRows == null || variableRows.Count != recall.Variables.Count)
                {
                    throw new InvalidOperationException(
                        $"The version lists {variableRows?.Count ?? 0} variables but the model expects {recall.Variables.Count}.");
                }

                var model = new AdvancedAnalyticsModel
                {
                    Id = trainedModel.Id,
                    Guid = trainedModel.Guid,
                    AdvancedAnalyticsInstanceId = instance.Id,
                    VersionId = version.Id,
                    Kind = recall.Kind,
                    Name = trainedModel.Name == null
                        ? $"AdvancedAnalytics_{trainedModel.Id}"
                        : trainedModel.Name.Replace(" ", "_"),
                    ResponsePayload = instance.ResponsePayload == 1,
                    ReportTable = instance.ReportTable == 1,
                    Recall = recall.Predict,
                    Explain = recall.Explain
                };

                for (var i = 0; i < variableRows.Count; i++)
                {
                    if (!string.Equals(variableRows[i].Name, recall.Variables[i], StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Variable {i} is {variableRows[i].Name} in the version but {recall.Variables[i]} in the model.");
                    }

                    model.VariablesInOrder.Add(new AdvancedAnalyticsModelVariable
                    {
                        Name = variableRows[i].Name,
                        ProcessingTypeId = variableRows[i].ProcessingTypeId,
                        TransformationId = variableRows[i].TransformationId,
                        TransformationParametersJson = variableRows[i].TransformationParametersJson
                    });
                }

                return model;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onLoadError?.Invoke(ex);

                return null;
            }
        }
    }
}