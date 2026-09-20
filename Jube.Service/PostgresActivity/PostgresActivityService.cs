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
using Jube.Data.Helpers;
using Jube.Data.Repository;
using Jube.Dto.Payload;
using Jube.Dto.PostgresActivity;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.PostgresActivity;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.PostgresActivity
{
    public sealed class PostgresActivityService
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

        private PostgresActivityService(string connectionString, string userName, int tenantRegistryId,
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

        public static Task<PostgresActivityService> CreateAsync(DbContext dbContext, string connectionString,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, connectionString, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PostgresActivityService> CreateAsync(DbContext dbContext, string connectionString,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PostgresActivityResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("PostgresActivity.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresActivityResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PostgresActivity.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresActivityResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new PostgresActivityService(connectionString, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every backend currently known to PostgreSQL (pg_stat_activity), live, oldest query " +
                     "first when no sort is given -- not a stored snapshot, so a call five seconds apart can " +
                     "return completely different rows. Includes each row's blocking chain (BlockedByPids, " +
                     "from pg_blocking_pids()) and, when active, how long its current query/transaction has " +
                     "been running. Optionally restrict to a case-insensitive substring search against " +
                     "UserName, ApplicationName or Query. Read-only: this does not offer " +
                     "pg_cancel_backend/pg_terminate_backend. The response also carries the true Total row " +
                     "count and summary Statistics for this DTO's continuous measured columns " +
                     "(TransactionDurationSeconds, QueryDurationSeconds) over the full filtered set -- Pid " +
                     "is excluded despite being numeric, since it identifies an OS process rather than " +
                     "measure a quantity.")]
        [ServiceOperation("PostgresActivityList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<PostgresActivityDto>> ListAsync(
            [Description(
                "Case-insensitive substring match against UserName, ApplicationName or Query; null or empty matches every row.")]
            string? search = null,
            [Description(
                "Column to sort by (e.g. 'queryDurationSeconds', 'pid', 'state'); unrecognised or omitted falls back to oldest-query-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PostgresActivity", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"PostgresActivity.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("PostgresActivity.List");

                await using var repository = new PostgresActivityRepository(connectionString);
                var rows = await repository.GetActivityAsync(token).ConfigureAwait(false);
                rows = rows.Select(r => r with { Query = SensitiveTextRedactor.Redact(r.Query) }).ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();
                    rows = rows.Where(r =>
                        (r.UserName != null && r.UserName.ToLower().Contains(lowerSearch)) ||
                        (r.ApplicationName != null && r.ApplicationName.ToLower().Contains(lowerSearch)) ||
                        (r.Query != null && r.Query.ToLower().Contains(lowerSearch))).ToList();
                }

                var dtos = rows.Select(r => new PostgresActivityDto(
                    r.Pid, r.DatabaseName, r.UserName, r.ApplicationName, r.ClientAddress, r.BackendType, r.State,
                    r.WaitEventType, r.WaitEvent, r.BlockedByPids, r.BackendStartDate, r.TransactionStartDate,
                    r.TransactionDurationSeconds, r.QueryStartDate, r.QueryDurationSeconds, r.Query)).ToList();

                dtos = ApplySort(dtos, sortField, sortDirection);

                var statistics = SummaryStatistics.Build(new Dictionary<string, double[]>
                {
                    ["transactionDurationSeconds"] = dtos.Where(w => w.TransactionDurationSeconds.HasValue)
                        .Select(s => s.TransactionDurationSeconds!.Value).ToArray(),
                    ["queryDurationSeconds"] = dtos.Where(w => w.QueryDurationSeconds.HasValue)
                        .Select(s => s.QueryDurationSeconds!.Value).ToArray()
                });

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PostgresActivity.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<PostgresActivityDto>(dtos, dtos.Count, statistics);
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
                    log.Debug($"PostgresActivity.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"PostgresActivity.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        internal static List<PostgresActivityDto> ApplySort(List<PostgresActivityDto> dtos, string? sortField,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortField))
            {
                return dtos;
            }

            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            IEnumerable<PostgresActivityDto> sorted = sortField switch
            {
                "pid" => OrderBy(dtos, d => d.Pid, descending),
                "databaseName" => OrderBy(dtos, d => d.DatabaseName, descending),
                "userName" => OrderBy(dtos, d => d.UserName, descending),
                "applicationName" => OrderBy(dtos, d => d.ApplicationName, descending),
                "clientAddress" => OrderBy(dtos, d => d.ClientAddress, descending),
                "backendType" => OrderBy(dtos, d => d.BackendType, descending),
                "state" => OrderBy(dtos, d => d.State, descending),
                "waitEventType" => OrderBy(dtos, d => d.WaitEventType, descending),
                "waitEvent" => OrderBy(dtos, d => d.WaitEvent, descending),
                "backendStartDate" => OrderBy(dtos, d => d.BackendStartDate, descending),
                "transactionStartDate" => OrderBy(dtos, d => d.TransactionStartDate, descending),
                "transactionDurationSeconds" => OrderBy(dtos, d => d.TransactionDurationSeconds, descending),
                "queryStartDate" => OrderBy(dtos, d => d.QueryStartDate, descending),
                "queryDurationSeconds" => OrderBy(dtos, d => d.QueryDurationSeconds, descending),
                "query" => OrderBy(dtos, d => d.Query, descending),
                _ => dtos
            };

            return sorted.ToList();
        }

        private static IOrderedEnumerable<PostgresActivityDto> OrderBy<TKey>(List<PostgresActivityDto> dtos,
            Func<PostgresActivityDto, TKey> keySelector, bool descending)
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

            throw new ForbiddenException(strings[PostgresActivityResources.PermissionDenied], permissions);
        }
    }
}