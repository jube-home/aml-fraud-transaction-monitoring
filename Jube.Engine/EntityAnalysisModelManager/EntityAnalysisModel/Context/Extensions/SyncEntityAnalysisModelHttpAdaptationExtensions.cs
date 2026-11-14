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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Data.Repository;
    using LinqToDB;
    using LinqToDB.Async;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

    public static class SyncEntityAnalysisModelHttpAdaptationExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelHttpAdaptationAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsHttpAdaptations = [];

                var tenantSettings = new Dictionary<int, HttpAdaptationSettings>();

                foreach (var (key, value) in context.ModelsInScope())
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and promoted by the HttP Adaptation.");
                    }

                    var repository = new EntityAnalysisModelHttpAdaptationRepository(context.Services.DbContext);

                    var tenantRegistryId = value.Instance.TenantRegistryId;

                    if (!tenantSettings.TryGetValue(tenantRegistryId, out var settings))
                    {
                        var tenant = await context.Services.DbContext.TenantRegistry
                            .FirstOrDefaultAsync(w => w.Id == tenantRegistryId
                                                      && (w.Deleted == 0 || w.Deleted == null),
                                context.Services.CancellationToken).ConfigureAwait(false);

                        settings = HttpAdaptationSettings.Resolve(context.Services.DynamicEnvironment, tenant);
                        tenantSettings[tenantRegistryId] = settings;

                        if (settings.Refusal != null)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Tenant {tenantRegistryId} has an HTTP Adaptation base URL refused by the outbound policy ({settings.Refusal}); its adaptations will not be recalled.");
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelHttpAdaptationRepository.GetByEntityAnalysisModelIdOrderByPriority for entity model key of {key}.");
                    }

                    var records = await repository.GetApprovedByEntityAnalysisModelIdOrderByPriorityAsync(key,
                        context.ApprovalsRequired,
                        context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityAnalysisModelAdaptations = new List<EntityAnalysisModelHttpAdaptation>();

                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Adaptation ID ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Adaptation ID ID {record.Id} returned for model {key} is active.");
                            }

                            if (record.HttpEndpoint != null &&
                                !EntityAnalysisModelHttpAdaptation.IsPlainPath(record.HttpEndpoint))
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: Adaptation ID {record.Id} returned for model {key} has an HTTP Endpoint that is not a plain path and has been skipped.");

                                continue;
                            }

                            if (settings.Refusal != null)
                            {
                                continue;
                            }

                            var entityAnalysisModelAdaptation = new EntityAnalysisModelHttpAdaptation(10,
                                settings.ValidateSsl, settings.TimeoutMilliseconds, settings.AllowInsecureTargets,
                                tenantRegistryId)
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelAdaptation.Name =
                                    $"Adaptation_{entityAnalysisModelAdaptation.Id}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation{entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.Name}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelAdaptation.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.Name}.");
                                }
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelAdaptation.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelAdaptation.ResponsePayload = record.ResponsePayload == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.ResponsePayload}.");
                                }
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelAdaptation.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.ReportTable}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelAdaptation.ReportTable = record.ReportTable == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.ReportTable}.");
                                }
                            }

                            if (!record.Priority.HasValue)
                            {
                                entityAnalysisModelAdaptation.Priority = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set DEFAULT Priority as {entityAnalysisModelAdaptation.Priority}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelAdaptation.Priority = record.Priority.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Priority as {entityAnalysisModelAdaptation.Priority}.");
                                }
                            }

                            if (record.HttpEndpoint == null)
                            {
                                entityAnalysisModelAdaptation.HttpEndpoint = "";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Machine Learning Hook Type ID {entityAnalysisModelAdaptation.Id} set DEFAULT Http Endpoint  as {entityAnalysisModelAdaptation.HttpEndpoint}.");
                                }
                            }
                            else
                            {
                                var validHost = settings.BaseUrl.EndsWith('/')
                                    ? settings.BaseUrl
                                    : settings.BaseUrl + "/";

                                var validUrl = record.HttpEndpoint.StartsWith('/')
                                    ? record.HttpEndpoint.Remove(0, 1)
                                    : record.HttpEndpoint;

                                var rPlumberEndpoint = $"{validHost}{validUrl}";

                                entityAnalysisModelAdaptation.BreakerKey =
                                    $"{tenantRegistryId}|{new Uri(rPlumberEndpoint).Authority}";
                                entityAnalysisModelAdaptation.Resilience = settings.Resilience;

                                entityAnalysisModelAdaptation.HttpEndpoint = rPlumberEndpoint;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Machine Learning Hook Type ID {entityAnalysisModelAdaptation.Id} set Http Endpoint as {entityAnalysisModelAdaptation.HttpEndpoint}.");
                                }
                            }

                            shadowEntityAnalysisModelAdaptations.Add(entityAnalysisModelAdaptation);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Exhaustive Search Instance Trial Instance ID  {entityAnalysisModelAdaptation.Id} has been added to a shadow list of Adaptations.");
                            }

                            context.Services.Parser.EntityAnalysisModelsHttpAdaptations.Add(
                                entityAnalysisModelAdaptation.Name);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Exhaustive Search Instance Trial Instance ID  {entityAnalysisModelAdaptation.Id} has added {entityAnalysisModelAdaptation.Name} to parser.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Adaptation ID ID {record.Id} returned for model {key} as created an error as {ex}.");
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it will now be allocated the fields in the order that they appeared in model training.");
                    }

                    context.Snapshots.Builder(key, value).EntityAnalysisModelAdaptations =
                        shadowEntityAnalysisModelAdaptations;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it has now be allocated the fields in the order that they appeared in model training from the shadow list of these variables.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Entity Start: Completed adding Adaptations and Exhaustive Neural Networks to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelHttpAdaptationAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(
                        EntityAnalysisModelSynchronisationErrorRepository
                            .EntityAnalysisModelSynchronisationErrorStepEnum.HttpAdaptation, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}