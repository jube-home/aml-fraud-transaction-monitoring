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
using Jube.Data.Query.GetExhaustiveSearchInstancePromotionHistoryQuery;
using Jube.Data.Repository;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotionHistory;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotionHistory;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotionHistory
{
    using HistoryQuery =
        GetExhaustiveSearchInstancePromotionHistoryQuery;

    public sealed class ExhaustiveSearchInstancePromotionHistoryService
    {
        private static readonly int[] permissions = [16];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly HistoryQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveSearchInstancePromotionHistoryService(DbContext dbContext, string userName,
            int tenantRegistryId,
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
            query = new HistoryQuery(dbContext, userName);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<ExhaustiveSearchInstancePromotionHistoryService> CreateAsync(
            DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstancePromotionHistoryService> CreateAsync(
            DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(
                    typeof(ExhaustiveSearchInstancePromotionHistoryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "ExhaustiveSearchInstancePromotionHistory.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotionHistoryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstancePromotionHistory.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotionHistoryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstancePromotionHistoryService(dbContext, userName,
                resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Returns the promotion history of an exhaustive search instance: for every promoted trial " +
                     "instance, in promotion order (Id ascending), the promotion date, score, topology complexity, " +
                     "whether it is the active model and the number of trial instances explored since the previous " +
                     "promotion. Only instances whose entity analysis model belongs to the caller's tenant are " +
                     "visible; any other exhaustive search instance id returns an empty list. Read-only.")]
        [ServiceOperation("ExhaustiveSearchInstancePromotionHistoryGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<ExhaustiveSearchInstancePromotionHistoryDto>> GetAsync(
            [Description("Id of the exhaustive search instance whose promotion history is requested.")]
            int exhaustiveSearchInstanceId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstancePromotionHistory", "Get",
                userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"ExhaustiveSearchInstancePromotionHistory.Get: entry user={userName} exhaustiveSearchInstanceId={exhaustiveSearchInstanceId}");
            }

            try
            {
                EnsurePermitted("ExhaustiveSearchInstancePromotionHistory.Get");

                var dtos = ExhaustiveSearchInstancePromotionHistoryMapper.ToDto(
                    await query.ExecuteAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"ExhaustiveSearchInstancePromotionHistory.Get: {dtos.Count} rows user={userName}");
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

                log.Error(
                    $"ExhaustiveSearchInstancePromotionHistory.Get: unexpected failure user={userName}",
                    ex);

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
                strings[ExhaustiveSearchInstancePromotionHistoryResources.PermissionDenied],
                permissions);
        }
    }
}