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

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    using System;
    using System.Threading.Tasks;
    using CaseEscalation;
    using Context;
    using Data.Context;
    using Data.Poco;
    using LinqToDB;
    using System.Linq;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using Newtonsoft.Json;

    public class CaseEscalationStarter(Context context)
    {
        public async Task StartAsync()
        {
            var token = context.Services.TaskCoordinator.CancellationToken;
            var environment = context.Services.DynamicEnvironment;
            var log = context.Services.Log;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                            environment.AppSettings("ConnectionString"), log);

                        var result = await CaseEscalationSweep.RunAsync(dbContext, DateTime.UtcNow,
                            int.Parse(environment.AppSettings("CaseEscalationBatchSize")), log,
                            (caseModel, target) => NotifyAsync(dbContext, caseModel, target), token).ConfigureAwait(false);

                        if (result.Examined > 0 && log.IsInfoEnabled)
                        {
                            log.Info($"Case Escalation: {result.Rules} rules, examined {result.Examined} cases, " +
                                     $"moved {result.Moved}, {result.Failed} failed, {result.Skipped} rules skipped.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"Case Escalation: has created an error inside the loop {ex}");
                    }

                    await Task.Delay(int.Parse(environment.AppSettings("CaseEscalationWait")), token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation CaseEscalationAsync: {ex.Message}");
            }
        }

        private async Task NotifyAsync(DbContext dbContext, Data.Poco.Case caseModel, CaseWorkflowStatus target)
        {
            if (target.EnableNotification != 1 && target.EnableHttpEndpoint != 1)
            {
                return;
            }

            var log = context.Services.Log;

            try
            {
                var payload = JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(caseModel.Json,
                    context.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                caseModel.CaseWorkflowStatusGuid = target.Guid;

                var tenantRegistryId = await dbContext.CaseWorkflow.Where(w => w.Guid == caseModel.CaseWorkflowGuid)
                    .Select(w => w.EntityAnalysisModel.TenantRegistryId.GetValueOrDefault())
                    .FirstOrDefaultAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                var dispatch = await CaseStatusNotifier.PrepareAsync(new CaseStatusDispatchRequest(dbContext,
                    tenantRegistryId, "Case escalation", context.Services.DynamicEnvironment, log, caseModel, target,
                    payload, context.JsonSerializationHelper.ArchiveJsonSerializer),
                    context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                await dispatch.NotifyAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                await dispatch.CallAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Case Escalation: the move of case id {caseModel.Id} succeeded but its notification " +
                          $"failed: {ex}");
            }
        }
    }
}
