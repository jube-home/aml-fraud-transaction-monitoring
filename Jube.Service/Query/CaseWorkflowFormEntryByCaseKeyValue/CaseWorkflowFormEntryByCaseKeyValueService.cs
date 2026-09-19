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
using Jube.Dto.Query.CaseWorkflowFormEntryByCaseKeyValue;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.CaseWorkflowFormEntryByCaseKeyValue;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseWorkflowFormEntryByCaseKeyValue
{
    public sealed class CaseWorkflowFormEntryByCaseKeyValueService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly global::Jube.Data.Query.GetCaseWorkflowFormEntryByCaseKeyValueQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseWorkflowFormEntryByCaseKeyValueService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            query = new global::Jube.Data.Query.GetCaseWorkflowFormEntryByCaseKeyValueQuery(dbContext, userName);
        }

        public static Task<CaseWorkflowFormEntryByCaseKeyValueService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowFormEntryByCaseKeyValueService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowFormEntryByCaseKeyValueResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowFormEntryByCaseKeyValue.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[CaseWorkflowFormEntryByCaseKeyValueResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"CaseWorkflowFormEntryByCaseKeyValue.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[CaseWorkflowFormEntryByCaseKeyValueResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowFormEntryByCaseKeyValueService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the Case Workflow Form Entries made against Cases matching a Case Key and Case Key " +
                     "Value, newest case first. Results are limited to the caller's tenant and to Case Workflows " +
                     "and Case Workflow Statuses for which the caller's role holds access. Read-only.")]
        [ServiceOperation("CaseWorkflowFormEntryByCaseKeyValueGet", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowFormEntryByCaseKeyValueDto>> GetAsync(
            [Description("The case key.")] string? key,
            [Description("The case key value.")] string? value,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowFormEntryByCaseKeyValue", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowFormEntryByCaseKeyValue.Get: entry key={key} value={value} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowFormEntryByCaseKeyValue.Get");
                var dtos = CaseWorkflowFormEntryByCaseKeyValueMapper.ToDto(
                    await query.ExecuteAsync(key ?? string.Empty, value ?? string.Empty, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowFormEntryByCaseKeyValue.Get: {dtos.Count} rows user={userName}");
                }

                return dtos;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"CaseWorkflowFormEntryByCaseKeyValue.Get: unexpected failure user={userName}", ex);
                throw;
            }
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

            throw new ForbiddenException(strings[CaseWorkflowFormEntryByCaseKeyValueResources.PermissionDenied],
                permissions);
        }
    }
}