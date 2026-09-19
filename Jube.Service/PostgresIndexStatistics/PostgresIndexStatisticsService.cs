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
using Jube.Data.Query.GetPostgresIndexStatisticsQuery;
using Jube.Data.Repository;
using Jube.Dto.Payload;
using Jube.Dto.PostgresIndexStatistics;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.PostgresIndexStatistics;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.PostgresIndexStatistics
{
    public sealed class PostgresIndexStatisticsService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PostgresIndexStatisticsService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static Task<PostgresIndexStatisticsService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PostgresIndexStatisticsService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PostgresIndexStatisticsResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("PostgresIndexStatistics.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresIndexStatisticsResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PostgresIndexStatistics.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PostgresIndexStatisticsResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new PostgresIndexStatisticsService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists live per-index Postgres statistics for every user index in the database: scan " +
                     "counts, tuples read/fetched, size, and whether it's unique/primary-key/unused " +
                     "(IndexScans = 0). Queried live from pg_stat_user_indexes joined with pg_index -- not " +
                     "stored history. Not scoped to any Model or tenant. Most recent physical order when no " +
                     "sort is given. The response also carries the true Total row count and summary " +
                     "Statistics for this DTO's continuous measured columns (IndexScans, IndexTuplesRead, " +
                     "IndexTuplesFetched, IndexSizeBytes) over the full result set.")]
        [ServiceOperation("PostgresIndexStatisticsList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<PostgresIndexStatisticsDto>> ListAsync(
            [Description(
                "Column to sort by (e.g. 'indexScans', 'indexSizeBytes', 'tableName'); unrecognised or omitted leaves the query's own order.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PostgresIndexStatistics", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"PostgresIndexStatistics.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("PostgresIndexStatistics.List");

                var rows = await new GetPostgresIndexStatisticsQuery(dbContext).ExecuteAsync(token)
                    .ConfigureAwait(false);

                var dtos = rows.Select(r => new PostgresIndexStatisticsDto(
                    r.SchemaName, r.TableName, r.IndexName, r.IndexScans.GetValueOrDefault(),
                    r.IndexTuplesRead.GetValueOrDefault(), r.IndexTuplesFetched.GetValueOrDefault(),
                    r.IndexSizeBytes.GetValueOrDefault(), r.IsUnique.GetValueOrDefault(),
                    r.IsPrimary.GetValueOrDefault(), r.IsUnused.GetValueOrDefault())).ToList();

                dtos = ApplySort(dtos, sortField, sortDirection);

                var statistics = SummaryStatistics.Build(new Dictionary<string, double[]>
                {
                    ["indexScans"] = dtos.Select(s => (double)s.IndexScans).ToArray(),
                    ["indexTuplesRead"] = dtos.Select(s => (double)s.IndexTuplesRead).ToArray(),
                    ["indexTuplesFetched"] = dtos.Select(s => (double)s.IndexTuplesFetched).ToArray(),
                    ["indexSizeBytes"] = dtos.Select(s => (double)s.IndexSizeBytes).ToArray()
                });

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PostgresIndexStatistics.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<PostgresIndexStatisticsDto>(dtos, dtos.Count, statistics);
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
                    log.Debug($"PostgresIndexStatistics.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"PostgresIndexStatistics.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static List<PostgresIndexStatisticsDto> ApplySort(List<PostgresIndexStatisticsDto> dtos,
            string? sortField, string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortField))
            {
                return dtos;
            }

            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            IEnumerable<PostgresIndexStatisticsDto> sorted = sortField switch
            {
                "schemaName" => OrderBy(dtos, d => d.SchemaName, descending),
                "tableName" => OrderBy(dtos, d => d.TableName, descending),
                "indexName" => OrderBy(dtos, d => d.IndexName, descending),
                "indexScans" => OrderBy(dtos, d => d.IndexScans, descending),
                "indexTuplesRead" => OrderBy(dtos, d => d.IndexTuplesRead, descending),
                "indexTuplesFetched" => OrderBy(dtos, d => d.IndexTuplesFetched, descending),
                "indexSizeBytes" => OrderBy(dtos, d => d.IndexSizeBytes, descending),
                "isUnique" => OrderBy(dtos, d => d.IsUnique, descending),
                "isPrimary" => OrderBy(dtos, d => d.IsPrimary, descending),
                "isUnused" => OrderBy(dtos, d => d.IsUnused, descending),
                _ => dtos
            };

            return sorted.ToList();
        }

        private static IOrderedEnumerable<PostgresIndexStatisticsDto> OrderBy<TKey>(
            List<PostgresIndexStatisticsDto> dtos, Func<PostgresIndexStatisticsDto, TKey> keySelector,
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

            throw new ForbiddenException(strings[PostgresIndexStatisticsResources.PermissionDenied], permissions);
        }
    }
}