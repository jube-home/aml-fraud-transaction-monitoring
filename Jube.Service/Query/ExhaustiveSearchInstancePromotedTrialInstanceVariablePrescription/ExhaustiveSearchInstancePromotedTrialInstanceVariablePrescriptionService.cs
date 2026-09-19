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
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription
{
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService
    {
        private static readonly int[] permissions = [16];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;

        private readonly global::Jube.Data.Query.
            GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery
            query;

        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService(DbContext dbContext,
            string userName, int tenantRegistryId, PermissionValidation permissionValidation, ILog log,
            ILog auditLog, IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            query =
                new global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery(
                    dbContext, userName);
        }

        public static Task<ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService>
            CreateAsync(DbContext dbContext, string? userName, ILog log,
                IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
                CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(
                typeof(ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionResources
                        .NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionResources
                        .NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the variables of one promoted trial instance of an Exhaustive Search Instance, each " +
                     "with the statistics of the variable, the statistics of its prescription and its " +
                     "sensitivity, ordered by sensitivity descending. Read-only and scoped to the caller's " +
                     "tenant: a promoted trial instance belonging to another tenant yields an empty list.")]
        [ServiceOperation("ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionGet",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto>> GetAsync(
            [Description("Integer Id of the Exhaustive Search Instance Promoted Trial Instance.")]
            int exhaustiveSearchInstancePromotedTrialInstanceId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription",
                "Get", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Get: entry user={userName} id={exhaustiveSearchInstancePromotedTrialInstanceId}");
            }

            try
            {
                EnsurePermitted("ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Get");

                var owned = await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                    .AnyAsync(w => w.Id == exhaustiveSearchInstancePromotedTrialInstanceId
                                   && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                                       .EntityAnalysisModel.TenantRegistryId == tenantRegistryId, token)
                    .ConfigureAwait(false);

                if (!owned)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Get: promoted trial instance {exhaustiveSearchInstancePromotedTrialInstanceId} not found in tenant; returning empty user={userName}");
                    }

                    op.Rows(0);
                    return [];
                }

                var dtos = ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionMapper.ToDto(
                    await query.ExecuteAsync(exhaustiveSearchInstancePromotedTrialInstanceId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Get: {dtos.Count} rows user={userName}");
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
                    $"ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Get: unexpected failure user={userName}",
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
                strings[ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionResources.PermissionDenied],
                permissions);
        }
    }
}