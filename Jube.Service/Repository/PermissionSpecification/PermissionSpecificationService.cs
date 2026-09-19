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
using Jube.Dto.Repository.PermissionSpecification;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.PermissionSpecification;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using SpecificationRepository = Jube.Data.Repository.PermissionSpecificationRepository;

namespace Jube.Service.Repository.PermissionSpecification
{
    public sealed class PermissionSpecificationService
    {
        private static readonly int[] permissions = [36];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly SpecificationRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PermissionSpecificationService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new SpecificationRepository(dbContext);
        }

        public static Task<PermissionSpecificationService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PermissionSpecificationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PermissionSpecificationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("PermissionSpecification.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PermissionSpecificationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"PermissionSpecification.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PermissionSpecificationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new PermissionSpecificationService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every row in the global Permission Specification catalogue -- the fixed set of " +
                     "permission integers a Role can be granted. The catalogue is seeded by database migration " +
                     "and shared system-wide across every tenant; this operation is read-only and the service " +
                     "layer exposes no way to create, update or delete a row.")]
        public async Task<List<PermissionSpecificationDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("PermissionSpecification", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"PermissionSpecification.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("PermissionSpecification.List");
                var dtos = PermissionSpecificationMapper.ToDto(
                    await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PermissionSpecification.List: {dtos.Count} rows user={userName}");
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
                log.Error($"PermissionSpecification.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists rows in the global Permission Specification catalogue, Id-ascending, capped at " +
                     "'take' rows. Call again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("PermissionSpecificationList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<PermissionSpecificationDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PermissionSpecification", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"PermissionSpecification.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("PermissionSpecification.List");
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = PermissionSpecificationMapper
                    .ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(dto => dto.Id)
                    .Where(dto => afterId == null || dto.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<PermissionSpecificationDto>(page);
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
                log.Error($"PermissionSpecification.List: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[PermissionSpecificationResources.PermissionDenied], permissions);
        }
    }
}