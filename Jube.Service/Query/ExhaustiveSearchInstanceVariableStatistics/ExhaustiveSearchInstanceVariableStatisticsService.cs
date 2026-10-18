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
using Jube.Data.Query.GetExhaustiveSearchInstanceVariableStatisticsQuery;
using Jube.Data.Repository;
using Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceVariableStatistics;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ExhaustiveSearchInstanceVariableStatistics
{
    public sealed class ExhaustiveSearchInstanceVariableStatisticsService
    {
        private static readonly int[] permissions = [16];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly GetExhaustiveSearchInstanceVariableStatisticsQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveSearchInstanceVariableStatisticsService(DbContext dbContext, string userName,
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

            query = new GetExhaustiveSearchInstanceVariableStatisticsQuery(
                dbContext, userName);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<ExhaustiveSearchInstanceVariableStatisticsService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstanceVariableStatisticsService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ExhaustiveSearchInstanceVariableStatisticsResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ExhaustiveSearchInstanceVariableStatistics.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstanceVariableStatisticsResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstanceVariableStatistics.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstanceVariableStatisticsResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstanceVariableStatisticsService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description(
            "Lists the variables of one Exhaustive Search Instance, or one variable of it, with the population, " +
            "positive class, negative class and Monte Carlo prescription histograms. " +
            "Read-only and scoped to the caller's tenant: an Exhaustive Search Instance " +
            "belonging to another tenant yields an empty list.")]
        [ServiceOperation("ExhaustiveSearchInstanceVariableStatisticsGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<ExhaustiveSearchInstanceVariableStatisticsDto>> GetAsync(
            [Description("Integer Id of the Exhaustive Search Instance.")]
            int exhaustiveSearchInstanceId,
            [Description("Optional Integer Id of one Exhaustive Search Instance Variable to restrict the result to.")]
            int? exhaustiveSearchInstanceVariableId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstanceVariableStatistics", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"ExhaustiveSearchInstanceVariableStatistics.Get: entry user={userName} id={exhaustiveSearchInstanceId} variable={exhaustiveSearchInstanceVariableId}");
            }

            try
            {
                EnsurePermitted("ExhaustiveSearchInstanceVariableStatistics.Get");

                var dtos = ExhaustiveSearchInstanceVariableStatisticsMapper.ToDto(
                    await query.ExecuteAsync(exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"ExhaustiveSearchInstanceVariableStatistics.Get: {dtos.Count} rows user={userName}");
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
                log.Error($"ExhaustiveSearchInstanceVariableStatistics.Get: unexpected failure user={userName}", ex);
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
                strings[ExhaustiveSearchInstanceVariableStatisticsResources.PermissionDenied], permissions);
        }
    }
}