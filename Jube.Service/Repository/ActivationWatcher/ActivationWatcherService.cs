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
using Jube.Dto.Repository.ActivationWatcher;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.ActivationWatcher;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.ActivationWatcher
{
    public sealed class ActivationWatcherService
    {
        private const int ReplayLimit = 1000;
        private static readonly int[] permissions = [30];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ActivationWatcherRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ActivationWatcherService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new ActivationWatcherRepository(dbContext, userName);
        }

        public static Task<ActivationWatcherService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ActivationWatcherService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ActivationWatcherResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ActivationWatcher.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[ActivationWatcherResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"ActivationWatcher.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[ActivationWatcherResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ActivationWatcherService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Replays ActivationWatcher rows (live activation events shown on the map/watcher UI) created " +
                     "within the given date range, scoped to the caller's tenant, ordered ascending and capped at " +
                     "1000 rows. Read-only; also re-broadcasts each row over SignalR to the tenant's watcher group.")]
        [ServiceOperation("ActivationWatcherReplay", OperationKind.Read, Idempotent = true)]
        public async Task<IReadOnlyList<ActivationWatcherDto>> ReplayAsync(
            [Description("Inclusive lower bound on CreatedDate; null means no lower bound.")]
            DateTimeOffset? dateFrom,
            [Description("Inclusive upper bound on CreatedDate; null means no upper bound.")]
            DateTimeOffset? dateTo,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ActivationWatcher", "Replay", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"ActivationWatcher.Replay: entry user={userName}");
            }

            try
            {
                EnsurePermitted("ActivationWatcher.Replay");

                var rows = await repository.GetByDateRangeAscendingAsync(dateFrom?.UtcDateTime, dateTo?.UtcDateTime,
                    ReplayLimit, token).ConfigureAwait(false);

                var dtos = rows.Select(ActivationWatcherMapper.ToDto).ToList();

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"ActivationWatcher.Replay: {dtos.Count} rows user={userName}");
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
                if (log.IsDebugEnabled)
                {
                    log.Debug($"ActivationWatcher.Replay: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ActivationWatcher.Replay: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[ActivationWatcherResources.PermissionDenied], permissions);
        }
    }
}