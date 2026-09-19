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
using Jube.Dto.Query.CaseWorkflowDisplayExecution;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.CaseWorkflowDisplayExecution;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;

namespace Jube.Service.Query.CaseWorkflowDisplayExecution
{
    public sealed class CaseWorkflowDisplayExecutionService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly CaseRepository caseRepository;
        private readonly CaseWorkflowDisplayRepository caseWorkflowDisplayRepository;
        private readonly CaseWorkflowStatusRepository caseWorkflowStatusRepository;
        private readonly EngineJsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseWorkflowDisplayExecutionService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, EngineJsonSerializationHelper jsonSerializationHelper)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.jsonSerializationHelper = jsonSerializationHelper;
            caseRepository = new CaseRepository(dbContext, userName);
            caseWorkflowDisplayRepository = new CaseWorkflowDisplayRepository(dbContext, userName);
            caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
        }

        public static Task<CaseWorkflowDisplayExecutionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            EngineJsonSerializationHelper jsonSerializationHelper, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                jsonSerializationHelper, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowDisplayExecutionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, EngineJsonSerializationHelper jsonSerializationHelper,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowDisplayExecutionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowDisplayExecution.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowDisplayExecutionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"CaseWorkflowDisplayExecution.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowDisplayExecutionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowDisplayExecutionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, jsonSerializationHelper);
        }

        [Description("Renders a case workflow display: loads the case, the active display template and the case's " +
                     "workflow status (each restricted to the caller's tenant and roles), then returns the display " +
                     "HTML with its [@Payload.x@]-style tokens replaced from the case's stored payload. Read only.")]
        [ServiceOperation("CaseWorkflowDisplayExecutionExecute", OperationKind.Read, Idempotent = true)]
        public async Task<string> ExecuteAsync(
            [Description("The case and display to render.")]
            CaseWorkflowDisplayExecutionDto model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplayExecution", "Execute", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"CaseWorkflowDisplayExecution.Execute: entry user={userName} caseId={model.CaseId} displayId={model.CaseWorkflowDisplayId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplayExecution.Execute");

                var existingCase = await caseRepository.GetByIdActiveOnlyAsync(model.CaseId, token)
                    .ConfigureAwait(false);
                if (existingCase == null)
                {
                    throw NotFound("case", model.CaseId);
                }

                var caseWorkflowDisplay = await caseWorkflowDisplayRepository
                    .GetByIdActiveOnlyAsync(model.CaseWorkflowDisplayId, token).ConfigureAwait(false);
                if (caseWorkflowDisplay == null)
                {
                    throw NotFound("display", model.CaseWorkflowDisplayId);
                }

                var caseWorkflowStatus = await caseWorkflowStatusRepository
                    .GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token).ConfigureAwait(false);
                if (caseWorkflowStatus == null)
                {
                    throw NotFound("status", model.CaseId);
                }

                EntityAnalysisModelInstanceEntryPayload? payload;
                try
                {
                    payload = JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(
                        existingCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);
                }
                catch (Exception e) when (e is JsonException or InvalidCastException or NullReferenceException
                                              or FormatException)
                {
                    payload = null;
                }

                payload ??= new EntityAnalysisModelInstanceEntryPayload();

                var replacedHtml = payload.ReplaceTokens(caseWorkflowDisplay.Html,
                    value => System.Net.WebUtility.HtmlEncode(value));

                op.Rows(1);
                return replacedHtml;
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
                log.Error($"CaseWorkflowDisplayExecution.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private NotFoundException NotFound(string what, int id)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"CaseWorkflowDisplayExecution.Execute: {what} {id} not found or not visible user={userName}");
            }

            return new NotFoundException(strings[CaseWorkflowDisplayExecutionResources.NotFound]);
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

            throw new ForbiddenException(strings[CaseWorkflowDisplayExecutionResources.PermissionDenied], permissions);
        }
    }
}