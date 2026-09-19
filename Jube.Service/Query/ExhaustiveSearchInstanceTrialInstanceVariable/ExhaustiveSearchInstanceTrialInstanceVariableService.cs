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
using Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceVariable;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceTrialInstanceVariable;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariable
{
    public sealed class ExhaustiveSearchInstanceTrialInstanceVariableService
    {
        private static readonly int[] permissions = [16];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveSearchInstanceTrialInstanceVariableService(DbContext dbContext, string userName,
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
            query = new global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(
                dbContext, userName);
        }

        public static Task<ExhaustiveSearchInstanceTrialInstanceVariableService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstanceTrialInstanceVariableService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ExhaustiveSearchInstanceTrialInstanceVariableResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ExhaustiveSearchInstanceTrialInstanceVariable.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstanceTrialInstanceVariableResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstanceTrialInstanceVariable.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstanceTrialInstanceVariableResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstanceTrialInstanceVariableService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the variables of the currently promoted (active, most recently promoted) trial " +
                     "instance of one Exhaustive Search Instance, ordered by trial instance variable sequence, " +
                     "excluding removed variables. Read-only and scoped to the caller's tenant: an Exhaustive " +
                     "Search Instance belonging to another tenant, or one with no promoted trial instance, " +
                     "yields an empty list.")]
        [ServiceOperation("ExhaustiveSearchInstanceTrialInstanceVariableGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<ExhaustiveSearchInstanceTrialInstanceVariableDto>> GetAsync(
            [Description("Integer Id of the Exhaustive Search Instance.")]
            int exhaustiveSearchInstanceId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstanceTrialInstanceVariable", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"ExhaustiveSearchInstanceTrialInstanceVariable.Get: entry user={userName} id={exhaustiveSearchInstanceId}");
            }

            try
            {
                EnsurePermitted("ExhaustiveSearchInstanceTrialInstanceVariable.Get");
                var dtos = ExhaustiveSearchInstanceTrialInstanceVariableMapper.ToDto(
                    await query.ExecuteByExhaustiveSearchInstanceIdAsync(exhaustiveSearchInstanceId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"ExhaustiveSearchInstanceTrialInstanceVariable.Get: {dtos.Count} rows user={userName}");
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
                log.Error($"ExhaustiveSearchInstanceTrialInstanceVariable.Get: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(
                strings[ExhaustiveSearchInstanceTrialInstanceVariableResources.PermissionDenied], permissions);
        }
    }
}