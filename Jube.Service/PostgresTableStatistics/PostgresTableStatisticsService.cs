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
using Jube.Data.Query.GetPostgresTableStatisticsQuery;
using Jube.Data.Repository;
using Jube.Dto.Payload;
using Jube.Dto.PostgresTableStatistics;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.PostgresTableStatistics;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.PostgresTableStatistics
{
    public sealed class PostgresTableStatisticsService
    {
        private static readonly int[] permissions = [27];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PostgresTableStatisticsService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static Task<PostgresTableStatisticsService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PostgresTableStatisticsService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PostgresTableStatisticsResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("PostgresTableStatistics.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresTableStatisticsResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PostgresTableStatistics.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresTableStatisticsResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new PostgresTableStatisticsService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists live per-table Postgres statistics for every user table in the database: " +
                     "sequential vs index scan counts, live/dead row estimates, table/index/total sizes, " +
                     "vacuum/analyze history, and a LikelyMissingIndex heuristic flag (more sequential scans " +
                     "than index scans on a table with over 1000 live rows). Queried live from " +
                     "pg_stat_user_tables -- not stored history. Not scoped to any Model or tenant. Most " +
                     "recent physical order when no sort is given. The response also carries the true Total " +
                     "row count and summary Statistics for this DTO's continuous measured columns " +
                     "(SequentialScans, SequentialTuplesRead, IndexScans, IndexTuplesFetched, LiveTupleCount, " +
                     "DeadTupleCount, TableSizeBytes, IndexesSizeBytes, TotalSizeBytes) over the full result " +
                     "set.")]
        [ServiceOperation("PostgresTableStatisticsList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<PostgresTableStatisticsDto>> ListAsync(
            [Description(
                "Column to sort by (e.g. 'sequentialScans', 'totalSizeBytes', 'tableName'); unrecognised or omitted leaves the query's own order.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PostgresTableStatistics", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"PostgresTableStatistics.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("PostgresTableStatistics.List");

                var rows = await new GetPostgresTableStatisticsQuery(dbContext).ExecuteAsync(token)
                    .ConfigureAwait(false);

                var dtos = rows.Select(r => new PostgresTableStatisticsDto(
                    r.SchemaName, r.TableName, r.SequentialScans.GetValueOrDefault(),
                    r.SequentialTuplesRead.GetValueOrDefault(), r.IndexScans.GetValueOrDefault(),
                    r.IndexTuplesFetched.GetValueOrDefault(), r.LiveTupleCount.GetValueOrDefault(),
                    r.DeadTupleCount.GetValueOrDefault(), r.TableSizeBytes.GetValueOrDefault(),
                    r.IndexesSizeBytes.GetValueOrDefault(), r.TotalSizeBytes.GetValueOrDefault(), r.LastVacuum,
                    r.LastAutoVacuum, r.LastAnalyze, r.LastAutoAnalyze,
                    r.LikelyMissingIndex.GetValueOrDefault())).ToList();

                dtos = ApplySort(dtos, sortField, sortDirection);

                var statistics = SummaryStatistics.Build(new Dictionary<string, double[]>
                {
                    ["sequentialScans"] = dtos.Select(s => (double)s.SequentialScans).ToArray(),
                    ["sequentialTuplesRead"] = dtos.Select(s => (double)s.SequentialTuplesRead).ToArray(),
                    ["indexScans"] = dtos.Select(s => (double)s.IndexScans).ToArray(),
                    ["indexTuplesFetched"] = dtos.Select(s => (double)s.IndexTuplesFetched).ToArray(),
                    ["liveTupleCount"] = dtos.Select(s => (double)s.LiveTupleCount).ToArray(),
                    ["deadTupleCount"] = dtos.Select(s => (double)s.DeadTupleCount).ToArray(),
                    ["tableSizeBytes"] = dtos.Select(s => (double)s.TableSizeBytes).ToArray(),
                    ["indexesSizeBytes"] = dtos.Select(s => (double)s.IndexesSizeBytes).ToArray(),
                    ["totalSizeBytes"] = dtos.Select(s => (double)s.TotalSizeBytes).ToArray()
                });

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PostgresTableStatistics.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<PostgresTableStatisticsDto>(dtos, dtos.Count, statistics);
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
                    log.Debug($"PostgresTableStatistics.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"PostgresTableStatistics.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static List<PostgresTableStatisticsDto> ApplySort(List<PostgresTableStatisticsDto> dtos,
            string? sortField, string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortField))
            {
                return dtos;
            }

            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            IEnumerable<PostgresTableStatisticsDto> sorted = sortField switch
            {
                "schemaName" => OrderBy(dtos, d => d.SchemaName, descending),
                "tableName" => OrderBy(dtos, d => d.TableName, descending),
                "sequentialScans" => OrderBy(dtos, d => d.SequentialScans, descending),
                "sequentialTuplesRead" => OrderBy(dtos, d => d.SequentialTuplesRead, descending),
                "indexScans" => OrderBy(dtos, d => d.IndexScans, descending),
                "indexTuplesFetched" => OrderBy(dtos, d => d.IndexTuplesFetched, descending),
                "liveTupleCount" => OrderBy(dtos, d => d.LiveTupleCount, descending),
                "deadTupleCount" => OrderBy(dtos, d => d.DeadTupleCount, descending),
                "tableSizeBytes" => OrderBy(dtos, d => d.TableSizeBytes, descending),
                "indexesSizeBytes" => OrderBy(dtos, d => d.IndexesSizeBytes, descending),
                "totalSizeBytes" => OrderBy(dtos, d => d.TotalSizeBytes, descending),
                "lastVacuum" => OrderBy(dtos, d => d.LastVacuum, descending),
                "lastAutoVacuum" => OrderBy(dtos, d => d.LastAutoVacuum, descending),
                "lastAnalyze" => OrderBy(dtos, d => d.LastAnalyze, descending),
                "lastAutoAnalyze" => OrderBy(dtos, d => d.LastAutoAnalyze, descending),
                "likelyMissingIndex" => OrderBy(dtos, d => d.LikelyMissingIndex, descending),
                _ => dtos
            };

            return sorted.ToList();
        }

        private static IOrderedEnumerable<PostgresTableStatisticsDto> OrderBy<TKey>(
            List<PostgresTableStatisticsDto> dtos, Func<PostgresTableStatisticsDto, TKey> keySelector,
            bool descending)
        {
            return descending ? dtos.OrderByDescending(keySelector) : dtos.OrderBy(keySelector);
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

            throw new ForbiddenException(strings[PostgresTableStatisticsResources.PermissionDenied], permissions);
        }
    }
}