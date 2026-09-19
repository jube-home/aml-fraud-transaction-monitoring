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
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram
{
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService
    {
        private static readonly int[] permissions = [16];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService(DbContext dbContext, string userName,
            int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
        }

        public static Task<ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService> CreateAsync(
            DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService> CreateAsync(
            DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(
                    typeof(ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService(dbContext, userName,
                resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Returns a ten-bin histogram of the prediction errors (actual minus predicted) of the " +
                     "active promoted trial instance of an exhaustive search instance, so model accuracy can be " +
                     "judged at a glance. Each entry is a bin lower bound and its frequency. Read-only, scoped to " +
                     "the caller's tenant; an exhaustive search instance from another tenant yields no result.")]
        [ServiceOperation("ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto>> GetAsync(
            [Description(
                "Identifier of the exhaustive search instance whose promoted trial instance errors are binned.")]
            int exhaustiveSearchInstanceId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram", "Get",
                userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.Get: entry user={userName} exhaustiveSearchInstanceId={exhaustiveSearchInstanceId}");
            }

            try
            {
                EnsurePermitted("ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.Get");
                var dtos = ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramMapper.ToDto(
                    await new global::Jube.Data.Query.
                        GetExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramQuery(
                            dbContext, userName).ExecuteAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.Get: {dtos.Count} rows user={userName}");
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
                    $"ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.Get: unexpected failure user={userName}",
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
                strings[ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramResources.PermissionDenied],
                permissions);
        }
    }
}