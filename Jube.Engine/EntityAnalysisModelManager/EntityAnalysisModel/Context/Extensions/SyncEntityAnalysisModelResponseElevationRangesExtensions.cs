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
    using System.Web;
    using Data.Repository;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

    public static class SyncEntityAnalysisModelResponseElevationRangesExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelResponseElevationRangesAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.ModelsInScope())
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing fetch of all Response Elevation Ranges for entity model {key}.");
                    }

                    var repository = new EntityAnalysisModelResponseElevationRangeRepository(context.Services.DbContext);

                    var records = await repository.GetApprovedByEntityAnalysisModelIdOrderByIdAsync(key,
                        context.ApprovalsRequired,
                        context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowRanges = new List<EntityAnalysisModelResponseElevationRange>();

                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (record.Active != 1 || !record.Minimum.HasValue || !record.Maximum.HasValue)
                            {
                                continue;
                            }

                            var decline = record.Decline == 1;

                            shadowRanges.Add(new EntityAnalysisModelResponseElevationRange
                            {
                                Id = record.Id,
                                Guid = record.Guid,
                                Name = record.Name ?? $"ResponseElevationRange_{record.Id}",
                                Minimum = record.Minimum.Value,
                                Maximum = record.Maximum.Value,
                                Decline = decline,
                                Hold = !decline && record.Hold == 1,
                                Content = record.Content,
                                Redirect = record.Redirect,
                                ForeColor = record.ForeColor,
                                BackColor = record.BackColor,
                                EnableHttpEndpoint = record.EnableHttpEndpoint == 1,
                                HttpEndpoint = record.HttpEndpoint,
                                HttpEndpointTypeId = record.HttpEndpointTypeId ?? 1,
                                HttpEndpointHeaderName = record.HttpEndpointHeaderName,
                                HttpEndpointCredentialVariable = record.HttpEndpointCredentialVariable,
                                EnableNotification = record.EnableNotification == 1,
                                NotificationTypeId = record.NotificationTypeId ?? 1,
                                NotificationDestination = record.NotificationDestination,
                                NotificationSubject = record.NotificationSubject,
                                NotificationBody = record.NotificationBody == null
                                    ? null
                                    : HttpUtility.HtmlDecode(record.NotificationBody)
                            });

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Response Elevation Range {record.Id} has been added to a shadow list of Response Elevation Ranges.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Response Elevation Range ID {record.Id} returned for model {key} has created an error as {ex}.");
                        }
                    }

                    context.Snapshots.Builder(key, value).EntityAnalysisModelResponseElevationRanges = shadowRanges;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"SyncEntityAnalysisModelResponseElevationRangesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(
                        EntityAnalysisModelSynchronisationErrorRepository
                            .EntityAnalysisModelSynchronisationErrorStepEnum.ResponseElevationRanges, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
