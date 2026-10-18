/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Jube.Data.Context;
using Jube.Data.Helpers;
using Jube.Data.Query.GetPostgresLocksQuery;
using Jube.Data.Repository;
using Jube.Dto.Payload;
using Jube.Dto.PostgresLock;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.PostgresLock;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.PostgresLock
{
    public sealed class PostgresLockService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly string connectionString;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PostgresLockService(string connectionString, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.connectionString = connectionString;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;
        public static bool RequiresLandlord => true;

        public static Task<PostgresLockService> CreateAsync(DbContext dbContext, string connectionString,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, connectionString, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PostgresLockService> CreateAsync(DbContext dbContext, string connectionString,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PostgresLockResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("PostgresLock.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresLockResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PostgresLock.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresLockResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new PostgresLockService(connectionString, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every lock PostgreSQL currently knows about (pg_locks), live -- not a stored " +
                     "snapshot. Ungranted (waiting) rows sort first, since those are the ones worth looking " +
                     "at: a row with Granted=false is a backend stuck waiting on another one, which " +
                     "PostgresActivityList's BlockedByPids already names by pid but not by what is actually " +
                     "being contended -- this answers that: which relation, which lock mode. Optionally " +
                     "restrict to a case-insensitive substring search against UserName, Query or " +
                     "RelationName. Read-only.")]
        [ServiceOperation("PostgresLockList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<PostgresLockDto>> ListAsync(
            [Description(
                "Case-insensitive substring match against UserName, Query or RelationName; null or empty matches every row.")]
            string? search = null,
            [Description(
                "Column to sort by (e.g. 'granted', 'pid', 'relationName'); unrecognised or omitted falls back to ungranted-first, then pid.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PostgresLock", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"PostgresLock.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("PostgresLock.List");

                await using var query = new GetPostgresLocksQuery(connectionString);
                var rows = await query.ExecuteAsync(token).ConfigureAwait(false);
                rows = rows.Select(r => r with { Query = SensitiveTextRedactor.Redact(r.Query) }).ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();

                    rows = rows.Where(r =>
                        (r.UserName != null && r.UserName.ToLower().Contains(lowerSearch)) ||
                        (r.Query != null && r.Query.ToLower().Contains(lowerSearch)) ||
                        (r.RelationName != null && r.RelationName.ToLower().Contains(lowerSearch))).ToList();
                }

                var dtos = rows.Select(r => new PostgresLockDto(
                    r.Pid, r.UserName, r.Query, r.LockType, r.Mode, r.Granted, r.RelationName,
                    r.VirtualTransactionId, r.FastPath)).ToList();

                dtos = ApplySort(dtos, sortField, sortDirection);

                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"PostgresLock.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<PostgresLockDto>(dtos, dtos.Count, new PayloadStatistics(new()));
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
                    log.Debug($"PostgresLock.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"PostgresLock.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        internal static List<PostgresLockDto> ApplySort(List<PostgresLockDto> dtos, string? sortField,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortField))
            {
                return dtos.OrderBy(d => d.Granted).ThenBy(d => d.Pid).ToList();
            }

            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            IEnumerable<PostgresLockDto> sorted = sortField switch
            {
                "pid" => OrderBy(dtos, d => d.Pid, descending),
                "userName" => OrderBy(dtos, d => d.UserName, descending),
                "query" => OrderBy(dtos, d => d.Query, descending),
                "lockType" => OrderBy(dtos, d => d.LockType, descending),
                "mode" => OrderBy(dtos, d => d.Mode, descending),
                "granted" => OrderBy(dtos, d => d.Granted, descending),
                "relationName" => OrderBy(dtos, d => d.RelationName, descending),
                "virtualTransactionId" => OrderBy(dtos, d => d.VirtualTransactionId, descending),
                "fastPath" => OrderBy(dtos, d => d.FastPath, descending),
                _ => dtos
            };

            return sorted.ToList();
        }

        private static IOrderedEnumerable<PostgresLockDto> OrderBy<TKey>(List<PostgresLockDto> dtos,
            Func<PostgresLockDto, TKey> keySelector, bool descending)
        {
            return descending ? dtos.OrderByDescending(keySelector) : dtos.OrderBy(keySelector);
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (landlord only) user={userName}");
            }

            throw new ForbiddenException(strings[PostgresLockResources.PermissionDenied], permissions);
        }
    }
}