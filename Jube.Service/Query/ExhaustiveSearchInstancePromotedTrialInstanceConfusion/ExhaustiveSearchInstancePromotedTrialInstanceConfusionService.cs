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
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion
{
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceConfusionService
    {
        private static readonly int[] permissions = [16];

        private readonly ILog auditLog;
        private readonly global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceConfusionQuery query;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveSearchInstancePromotedTrialInstanceConfusionService(DbContext dbContext, string userName,
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
            query =
                new global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceConfusionQuery(dbContext,
                    userName);
        }

        public static Task<ExhaustiveSearchInstancePromotedTrialInstanceConfusionService> CreateAsync(
            DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstancePromotedTrialInstanceConfusionService> CreateAsync(
            DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(typeof(ExhaustiveSearchInstancePromotedTrialInstanceConfusionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "ExhaustiveSearchInstancePromotedTrialInstanceConfusion.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotedTrialInstanceConfusionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstancePromotedTrialInstanceConfusion.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotedTrialInstanceConfusionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstancePromotedTrialInstanceConfusionService(dbContext, userName,
                resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Returns the confusion matrix (true/false positive/negative counts) and derived row, column " +
                     "and table ratios for the active promoted trial instance of an Exhaustive Search Instance, " +
                     "scoped to the caller's tenant. Read-only. When no active promoted trial instance exists in " +
                     "the tenant, every value is zero.")]
        [ServiceOperation("ExhaustiveSearchInstancePromotedTrialInstanceConfusionGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto> GetAsync(
            [Description("Id of the Exhaustive Search Instance whose active promoted trial instance confusion " +
                         "matrix is wanted.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstancePromotedTrialInstanceConfusion", "Get",
                userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"ExhaustiveSearchInstancePromotedTrialInstanceConfusion.Get: entry user={userName} id={id}");
            }

            try
            {
                EnsurePermitted("ExhaustiveSearchInstancePromotedTrialInstanceConfusion.Get");
                var dto = ExhaustiveSearchInstancePromotedTrialInstanceConfusionMapper.ToDto(
                    await query.ExecuteAsync(id, token).ConfigureAwait(false));
                op.Rows(1);
                return dto;
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
                    $"ExhaustiveSearchInstancePromotedTrialInstanceConfusion.Get: unexpected failure user={userName} id={id}",
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
                strings[ExhaustiveSearchInstancePromotedTrialInstanceConfusionResources.PermissionDenied], permissions);
        }
    }
}