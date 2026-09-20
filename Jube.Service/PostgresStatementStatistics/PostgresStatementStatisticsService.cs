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
using Jube.Dto.PostgresStatementStatistics;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.PostgresStatementStatistics;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.PostgresStatementStatistics
{
    public sealed class PostgresStatementStatisticsService
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

        private PostgresStatementStatisticsService(string connectionString, string userName, int tenantRegistryId,
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

        public static Task<PostgresStatementStatisticsService> CreateAsync(DbContext dbContext,
            string connectionString, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, connectionString, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PostgresStatementStatisticsService> CreateAsync(DbContext dbContext,
            string connectionString, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PostgresStatementStatisticsResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("PostgresStatementStatistics.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresStatementStatisticsResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PostgresStatementStatistics.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresStatementStatisticsResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new PostgresStatementStatisticsService(connectionString, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists per-query-shape aggregate statistics from pg_stat_statements -- calls, rows, " +
                     "execution time min/mean/max/stddev, cache-hit vs disk-read bytes, temp-file spill bytes " +
                     "and WAL bytes, accumulated across all history since the extension was created or last " +
                     "reset (not a live per-instant view like PostgresActivity -- a running total). Sorted " +
                     "worst-total-time first when no sort is given, the direct complement to PostgresActivity's " +
                     "'what is running right now': this answers 'which query shape is worst overall'. " +
                     "Optionally restrict to a case-insensitive substring search against UserName, DatabaseName " +
                     "or Query. Read-only: does not expose pg_stat_statements_reset(). The response also " +
                     "carries the true Total row count and summary Statistics for this DTO's continuous " +
                     "measured columns over the full filtered set -- QueryId is excluded despite being " +
                     "numeric, since it identifies a query shape rather than measure a quantity.")]
        [ServiceOperation("PostgresStatementStatisticsList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<PostgresStatementStatisticsDto>> ListAsync(
            [Description(
                "Case-insensitive substring match against UserName, DatabaseName or Query; null or empty matches every row.")]
            string? search = null,
            [Description(
                "Column to sort by (e.g. 'meanExecTimeMilliseconds', 'calls', 'query'); unrecognised or omitted falls back to worst-total-time-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PostgresStatementStatistics", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"PostgresStatementStatistics.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("PostgresStatementStatistics.List");

                await using var repository = new PostgresStatementStatisticsRepository(connectionString);
                var rows = await repository.GetStatementStatisticsAsync(token).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();
                    rows = rows.Where(r =>
                        (r.UserName != null && r.UserName.ToLower().Contains(lowerSearch)) ||
                        (r.DatabaseName != null && r.DatabaseName.ToLower().Contains(lowerSearch)) ||
                        (r.Query != null && r.Query.ToLower().Contains(lowerSearch))).ToList();
                }

                var dtos = rows.Select(r => new PostgresStatementStatisticsDto(
                    r.QueryId, r.DatabaseName, r.UserName, SensitiveTextRedactor.Redact(r.Query), r.Calls, r.Rows,
                    r.TotalExecTimeMilliseconds,
                    r.MeanExecTimeMilliseconds, r.MinExecTimeMilliseconds, r.MaxExecTimeMilliseconds,
                    r.StddevExecTimeMilliseconds, r.SharedBlksHitBytes, r.SharedBlksReadBytes, r.TempBlksReadBytes,
                    r.TempBlksWrittenBytes, r.WalBytes)).ToList();

                dtos = ApplySort(dtos, sortField, sortDirection);

                var statistics = SummaryStatistics.Build(new Dictionary<string, double[]>
                {
                    ["calls"] = dtos.Select(s => (double)s.Calls).ToArray(),
                    ["rows"] = dtos.Select(s => (double)s.Rows).ToArray(),
                    ["totalExecTimeMilliseconds"] = dtos.Select(s => s.TotalExecTimeMilliseconds).ToArray(),
                    ["meanExecTimeMilliseconds"] = dtos.Select(s => s.MeanExecTimeMilliseconds).ToArray(),
                    ["minExecTimeMilliseconds"] = dtos.Select(s => s.MinExecTimeMilliseconds).ToArray(),
                    ["maxExecTimeMilliseconds"] = dtos.Select(s => s.MaxExecTimeMilliseconds).ToArray(),
                    ["stddevExecTimeMilliseconds"] = dtos.Select(s => s.StddevExecTimeMilliseconds).ToArray(),
                    ["sharedBlksHitBytes"] = dtos.Select(s => (double)s.SharedBlksHitBytes).ToArray(),
                    ["sharedBlksReadBytes"] = dtos.Select(s => (double)s.SharedBlksReadBytes).ToArray(),
                    ["tempBlksReadBytes"] = dtos.Select(s => (double)s.TempBlksReadBytes).ToArray(),
                    ["tempBlksWrittenBytes"] = dtos.Select(s => (double)s.TempBlksWrittenBytes).ToArray(),
                    ["walBytes"] = dtos.Select(s => (double)s.WalBytes).ToArray()
                });

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PostgresStatementStatistics.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<PostgresStatementStatisticsDto>(dtos, dtos.Count, statistics);
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
                    log.Debug($"PostgresStatementStatistics.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"PostgresStatementStatistics.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        internal static List<PostgresStatementStatisticsDto> ApplySort(List<PostgresStatementStatisticsDto> dtos,
            string? sortField, string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortField))
            {
                return dtos;
            }

            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            IEnumerable<PostgresStatementStatisticsDto> sorted = sortField switch
            {
                "queryId" => OrderBy(dtos, d => d.QueryId, descending),
                "databaseName" => OrderBy(dtos, d => d.DatabaseName, descending),
                "userName" => OrderBy(dtos, d => d.UserName, descending),
                "query" => OrderBy(dtos, d => d.Query, descending),
                "calls" => OrderBy(dtos, d => d.Calls, descending),
                "rows" => OrderBy(dtos, d => d.Rows, descending),
                "totalExecTimeMilliseconds" => OrderBy(dtos, d => d.TotalExecTimeMilliseconds, descending),
                "meanExecTimeMilliseconds" => OrderBy(dtos, d => d.MeanExecTimeMilliseconds, descending),
                "minExecTimeMilliseconds" => OrderBy(dtos, d => d.MinExecTimeMilliseconds, descending),
                "maxExecTimeMilliseconds" => OrderBy(dtos, d => d.MaxExecTimeMilliseconds, descending),
                "stddevExecTimeMilliseconds" => OrderBy(dtos, d => d.StddevExecTimeMilliseconds, descending),
                "sharedBlksHitBytes" => OrderBy(dtos, d => d.SharedBlksHitBytes, descending),
                "sharedBlksReadBytes" => OrderBy(dtos, d => d.SharedBlksReadBytes, descending),
                "tempBlksReadBytes" => OrderBy(dtos, d => d.TempBlksReadBytes, descending),
                "tempBlksWrittenBytes" => OrderBy(dtos, d => d.TempBlksWrittenBytes, descending),
                "walBytes" => OrderBy(dtos, d => d.WalBytes, descending),
                _ => dtos
            };

            return sorted.ToList();
        }

        private static IOrderedEnumerable<PostgresStatementStatisticsDto> OrderBy<TKey>(
            List<PostgresStatementStatisticsDto> dtos, Func<PostgresStatementStatisticsDto, TKey> keySelector,
            bool descending)
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

            throw new ForbiddenException(strings[PostgresStatementStatisticsResources.PermissionDenied], permissions);
        }
    }
}