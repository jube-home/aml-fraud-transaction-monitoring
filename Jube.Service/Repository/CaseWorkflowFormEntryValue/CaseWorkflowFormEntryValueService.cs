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
using Jube.Dto.Repository.CaseWorkflowFormEntryValue;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormEntryValue;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowFormEntryValueRepository;

namespace Jube.Service.Repository.CaseWorkflowFormEntryValue
{
    public sealed class CaseWorkflowFormEntryValueService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly RuleRepository repository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseWorkflowFormEntryValueService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new RuleRepository(dbContext, userName);
        }

        public static Task<CaseWorkflowFormEntryValueService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowFormEntryValueService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowFormEntryValueResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowFormEntryValue.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowFormEntryValueResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowFormEntryValue.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowFormEntryValueResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowFormEntryValueService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the submitted field name/value rows for a CaseWorkflowFormEntry, newest first, " +
                     "scoped to the caller's tenant and to the Case Workflow/Case Workflow Status role grants the " +
                     "caller holds.")]
        [ServiceOperation("CaseWorkflowFormEntryValueListByCaseWorkflowFormEntryId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<CaseWorkflowFormEntryValueDto>> GetByCaseWorkflowFormEntryIdAsync(
            [Description("Identifier of the CaseWorkflowFormEntry these values belong to.")]
            int caseWorkflowFormEntryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowFormEntryValue", "GetByCaseWorkflowFormEntryId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowFormEntryValue.GetByCaseWorkflowFormEntryId: entry " +
                          $"caseWorkflowFormEntryId={caseWorkflowFormEntryId} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowFormEntryValue.GetByCaseWorkflowFormEntryId");
                var dtos = CaseWorkflowFormEntryValueMapper.ToDto(
                    await repository.GetByCaseWorkflowFormEntryIdActiveOnlyAsync(caseWorkflowFormEntryId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowFormEntryValue.GetByCaseWorkflowFormEntryId: {dtos.Count} rows " +
                              $"caseWorkflowFormEntryId={caseWorkflowFormEntryId} user={userName}");
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
                log.Error($"CaseWorkflowFormEntryValue.GetByCaseWorkflowFormEntryId: unexpected failure " +
                          $"caseWorkflowFormEntryId={caseWorkflowFormEntryId} user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseWorkflowFormEntryValueResources.PermissionDenied], permissions);
        }
    }
}