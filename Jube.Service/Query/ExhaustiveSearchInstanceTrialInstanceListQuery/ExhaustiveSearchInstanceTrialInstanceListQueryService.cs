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
using Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceListQuery;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceTrialInstanceListQuery;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ExhaustiveSearchInstanceTrialInstanceListQuery
{
    public sealed class ExhaustiveSearchInstanceTrialInstanceListQueryService
    {
        private static readonly int[] permissions = [16];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceRepository promotedRepository;
        private readonly ExhaustiveSearchInstanceTrialInstanceRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveSearchInstanceTrialInstanceListQueryService(DbContext dbContext, string userName,
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

            repository = new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext, tenantRegistryId);
            promotedRepository = new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<ExhaustiveSearchInstanceTrialInstanceListQueryService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstanceTrialInstanceListQueryService> CreateAsync(
            DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(typeof(ExhaustiveSearchInstanceTrialInstanceListQueryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "ExhaustiveSearchInstanceTrialInstanceListQuery.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstanceTrialInstanceListQueryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstanceTrialInstanceListQuery.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstanceTrialInstanceListQueryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstanceTrialInstanceListQueryService(dbContext, userName,
                resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every trial attempt of one Exhaustive Search Instance, promoted or not, newest " +
                     "(highest Id) first. Scoped to the caller's tenant through the owning Entity Analysis Model. " +
                     "Read-only.")]
        [ServiceOperation("ExhaustiveSearchInstanceTrialInstanceListQueryGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<ExhaustiveSearchInstanceTrialInstanceListQueryDto>> GetAsync(
            [Description("Id of the Exhaustive Search Instance whose trial attempts are listed.")]
            int exhaustiveSearchInstanceId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstanceTrialInstanceListQuery", "Get", userName,
                tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"ExhaustiveSearchInstanceTrialInstanceListQuery.Get: entry user={userName} exhaustiveSearchInstanceId={exhaustiveSearchInstanceId}");
            }

            try
            {
                EnsurePermitted("ExhaustiveSearchInstanceTrialInstanceListQuery.Get");

                var trials = await repository.GetByExhaustiveSearchInstanceIdOrderByIdDescAsync(
                    exhaustiveSearchInstanceId, token).ConfigureAwait(false);

                var promotedTrialInstanceIds = await promotedRepository.GetNonDeletedTrialInstanceIdsAsync(
                    trials.Select(s => s.Id), token).ConfigureAwait(false);

                var dtos = trials
                    .Select(s => ExhaustiveSearchInstanceTrialInstanceListQueryMapper.ToDto(s,
                        promotedTrialInstanceIds.Contains(s.Id)))
                    .ToList();

                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"ExhaustiveSearchInstanceTrialInstanceListQuery.Get: {dtos.Count} rows user={userName}");
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

                log.Error($"ExhaustiveSearchInstanceTrialInstanceListQuery.Get: unexpected failure user={userName}",
                    ex);

                throw;
            }
        }

        [Description("Returns detail for one Exhaustive trial attempt: its kind, score, whether it has a stored " +
                     "model diagram, and its parsed evidence (challenger families only). Scoped to the caller's " +
                     "tenant. Read-only.")]
        [ServiceOperation("ExhaustiveSearchInstanceTrialInstanceDetailGet", OperationKind.Read, Idempotent = true)]
        public async Task<ExhaustiveSearchInstanceTrialInstanceDetailDto?> GetDetailAsync(
            [Description("Id of the trial instance whose detail is requested.")]
            int trialInstanceId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstanceTrialInstanceListQuery", "GetDetail",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("ExhaustiveSearchInstanceTrialInstanceListQuery.GetDetail");

                var dto = ExhaustiveSearchInstanceTrialInstanceListQueryMapper.ToDetailDto(
                    await repository.GetByIdAsync(trialInstanceId, token).ConfigureAwait(false));
                op.Rows(dto is null ? 0 : 1);

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
                    $"ExhaustiveSearchInstanceTrialInstanceListQuery.GetDetail: unexpected failure user={userName}",
                    ex);

                throw;
            }
        }

        internal async Task<string?> GetDiagramJsonAsync(int trialInstanceId, CancellationToken token = default)
        {
            EnsurePermitted("ExhaustiveSearchInstanceTrialInstanceListQuery.GetDetail");
            var trial = await repository.GetByIdAsync(trialInstanceId, token).ConfigureAwait(false);

            return trial?.Json;
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
                strings[ExhaustiveSearchInstanceTrialInstanceListQueryResources.PermissionDenied], permissions);
        }
    }
}