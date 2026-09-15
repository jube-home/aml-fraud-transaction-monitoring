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
using System.Threading.Tasks;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    public static class SyncEntityAnalysisModelsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelsAsync(this Context context, int tenantRegistryId)
        {
            try
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Getting all Entity Models from Database.");
                }

                var repository = new EntityAnalysisModelRepository(context.Services.DbContext, tenantRegistryId);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Entity Start: Executing EntityAnalysisModelRepository.Get.");
                }

                var records = await repository.GetAsync(context.Services.CancellationToken).ConfigureAwait(false);
                foreach (var record in records)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Model {record.Id} has been returned,  checking to see if it is active.");
                        }

                        if (record.Active == 1)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {record.Id} has been returned, is active. Proceeding to build model.");
                            }

                            EntityAnalysisModel entityAnalysisModel;

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Checking to see if Model {record.Id} exists in the list of Active Models.");
                            }

                            if (!context.EntityAnalysisModels.ActiveEntityAnalysisModels.TryGetValue(record.Id,
                                    out var model))
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {record.Id} does not exist in the list of Active Models and is being created.");
                                }

                                entityAnalysisModel =
                                    EntityAnalysisModelRecordMapper.CreateEntityAnalysisModel(context, record);
                            }
                            else
                            {
                                entityAnalysisModel = model;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {record.Id} does exist in the list of Active Models and is being updated.");
                                }
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {record.Id} with Entity Analysis Model ID value {entityAnalysisModel.Instance.Id}.");
                            }

                            EntityAnalysisModelRecordMapper.MapRecordFields(record, entityAnalysisModel,
                                context.Services.Log);

                            context.EntityAnalysisModels.ActiveEntityAnalysisModels.TryAdd(
                                entityAnalysisModel.Instance.Id, entityAnalysisModel);
                        }
                        else
                        {
                            var removed = context.EntityAnalysisModels.ActiveEntityAnalysisModels.Remove(record.Id);
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(removed
                                    ? $"Entity Start: Model {record.Id} already exists but is marked as inactive,  hence it has just been removed from the list of active models."
                                    : $"Entity Start: Model {record.Id} is marked as inactive but it does not exist in the list in of Active Models.");
                            }
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Loaded {context.EntityAnalysisModels.ActiveEntityAnalysisModels.Count} active models but need to remove models that have been deleted and are orphaned.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error(
                            $"Entity Start: Model {record.Id} has been returned,  checking to see if it is active has created an error as {ex}.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Executed database procedures to get all models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelsAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(
                        EntityAnalysisModelSynchronisationErrorRepository
                            .EntityAnalysisModelSynchronisationErrorStepEnum.Models, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}