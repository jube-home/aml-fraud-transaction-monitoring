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

using System.ComponentModel;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseWorkflowMacroExecution;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.CaseWorkflowMacroExecution;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using EngineNotification = global::Jube.Case.Notification;
using EngineSendHttpEndpoint = global::Jube.Case.SendHttpEndpoint;

namespace Jube.Service.Query.CaseWorkflowMacroExecution
{
    using EntryPayload = Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
        .EntityAnalysisModelInstanceEntryPayload;
    using EntryPayloadExtensions =
        Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
        .Extensions.EntityAnalysisModelInstanceEntryPayloadExtensions;

    public sealed class CaseWorkflowMacroExecutionService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly CaseRepository caseRepository;
        private readonly CaseWorkflowMacroRepository caseWorkflowMacroRepository;
        private readonly CaseWorkflowStatusRepository caseWorkflowStatusRepository;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly EngineJsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseWorkflowMacroExecutionService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            this.jsonSerializationHelper = jsonSerializationHelper;

            caseRepository = new CaseRepository(dbContext, userName);
            caseWorkflowMacroRepository = new CaseWorkflowMacroRepository(dbContext, userName);
            caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
        }

        public static Task<CaseWorkflowMacroExecutionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, jsonSerializationHelper, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowMacroExecutionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowMacroExecutionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowMacroExecution.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowMacroExecutionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowMacroExecution.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowMacroExecutionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowMacroExecutionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment,
                jsonSerializationHelper);
        }

        [Description("Executes an active case workflow macro against a case: sends the macro's configured " +
                     "notification (email or SMS, with tokens replaced from the case payload) and/or calls its " +
                     "configured HTTP endpoint (POST with the case, status and payload, or GET). The case and " +
                     "macro must belong to the caller's tenant and be visible to the caller's roles. SIDE " +
                     "EFFECTS: outbound notifications and HTTP calls only; no stored data is modified. " +
                     "Delivery failures are logged, not raised. Fails as not found when the case, the macro or " +
                     "the case's status is unknown or not visible. Echoes the request back on success.")]
        [ServiceOperation("CaseWorkflowMacroExecutionExecute", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowMacroExecutionDto> ExecuteAsync(
            [Description("The case and the macro to execute against it.")]
            CaseWorkflowMacroExecutionDto model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacroExecution", "Execute", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"CaseWorkflowMacroExecution.Execute: entry user={userName} caseId={model.CaseId} macroId={model.CaseWorkflowMacroId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowMacroExecution.Execute");
                token.ThrowIfCancellationRequested();

                var existingCase = await caseRepository.GetByIdActiveOnlyAsync(model.CaseId, token)
                    .ConfigureAwait(false);
                if (existingCase == null)
                {
                    throw NotFound();
                }

                var caseWorkflowMacro = await caseWorkflowMacroRepository
                    .GetByIdActiveOnlyAsync(model.CaseWorkflowMacroId, token).ConfigureAwait(false);
                if (caseWorkflowMacro == null)
                {
                    throw NotFound();
                }

                var caseWorkflowStatus = await caseWorkflowStatusRepository
                    .GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token).ConfigureAwait(false);
                if (caseWorkflowStatus == null)
                {
                    throw NotFound();
                }

                EntryPayload? context;
                try
                {
                    context = JsonConvert.DeserializeObject<EntryPayload>(existingCase.Json,
                        jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);
                }
                catch (Exception e) when (e is JsonException or InvalidCastException or NullReferenceException
                                              or FormatException)
                {
                    context = null;
                }

                context ??= new EntryPayload();

                if (caseWorkflowMacro.EnableNotification == 1)
                {
                    var notification = new EngineNotification(log, dynamicEnvironment);
                    var notificationSubject =
                        EntryPayloadExtensions.ReplaceTokens(context, caseWorkflowMacro.NotificationSubject);
                    var notificationDestination =
                        EntryPayloadExtensions.ReplaceTokens(context, caseWorkflowMacro.NotificationDestination);
                    var notificationBody =
                        EntryPayloadExtensions.ReplaceTokens(context, caseWorkflowMacro.NotificationBody);

                    await notification.SendAsync(caseWorkflowMacro.NotificationTypeId ?? 1,
                            notificationDestination, notificationSubject, notificationBody, token)
                        .ConfigureAwait(false);
                }

                if (caseWorkflowMacro.EnableHttpEndpoint == 1)
                {
                    var endpoint = EntryPayloadExtensions.ReplaceTokens(context, caseWorkflowMacro.HttpEndpoint);

                    if (caseWorkflowMacro.HttpEndpointTypeId == 1)
                    {
                        await EngineSendHttpEndpoint.PostAsync(endpoint,
                            PreparePostBodyString(model, existingCase, context, caseWorkflowStatus,
                                caseWorkflowMacro), log).ConfigureAwait(false);
                    }
                    else
                    {
                        await EngineSendHttpEndpoint.GetAsync(endpoint, log).ConfigureAwait(false);
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"CaseWorkflowMacroExecution.Execute: executed macroId={model.CaseWorkflowMacroId} caseId={model.CaseId} user={userName}");
                }

                op.Rows(1);
                return model;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowMacroExecution.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private NotFoundException NotFound()
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"CaseWorkflowMacroExecution.Execute: case, macro or status not found user={userName}");
            }

            return new NotFoundException(strings[CaseWorkflowMacroExecutionResources.NotFound]);
        }

        private string PreparePostBodyString(CaseWorkflowMacroExecutionDto model, Data.Poco.Case existingCase,
            EntryPayload payload, Data.Poco.CaseWorkflowStatus caseWorkflowStatus,
            Data.Poco.CaseWorkflowMacro caseWorkflowMacro)
        {
            var jObject = JObject.FromObject(model, jsonSerializationHelper.ArchiveJsonSerializer);
            jObject["caseWorkflowMacroName"] = caseWorkflowMacro.Name;

            var caseJObject = JObject.FromObject(existingCase, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject.Remove("json");
            jObject["case"] = caseJObject;

            caseJObject["caseWorkflowStatus"] = caseWorkflowStatus.Name;

            var payloadJObject = JObject.FromObject(payload, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject["payload"] = payloadJObject;

            return jObject.ToString();
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[CaseWorkflowMacroExecutionResources.PermissionDenied],
                permissions);
        }
    }
}