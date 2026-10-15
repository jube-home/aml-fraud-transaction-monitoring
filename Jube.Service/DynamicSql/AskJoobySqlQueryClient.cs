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

using System.Diagnostics;
using Jube.AskJooby.SqlQuery.Models;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Jube.Data.Context;
using Jube.Data.Query.GetPostgresRelationStatisticsByNameQuery;
using Jube.Data.Query.GetPostgresStatementStatisticsByRelationQuery;
using Jube.Data.Query.GetPostgresTableCommentsByNameQuery;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using log4net;
using Npgsql;

namespace Jube.Service.DynamicSql
{
    public sealed class AskJoobySqlQueryClient(
        string connectionString,
        DbContext dbContext,
        ILog log,
        string userName,
        Func<int[], bool> hasPermission,
        int? conversationId = null) : ISqlQueryClient
    {
        private static readonly SqlQueryHistory noHistory = new(false, 0, 0, 0, 0, 0);

        public IReadOnlyList<string> ListQueryableTables()
        {
            return DynamicSqlTableAccess.BuildAllowedTables(userName, hasPermission, conversationId)
                .Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        public async Task<IReadOnlyDictionary<string, string>> TableCommentsAsync(IReadOnlyList<string> tables,
            CancellationToken token = default)
        {
            var allowedTables = DynamicSqlTableAccess.BuildAllowedTables(userName, hasPermission, conversationId);

            var permitted = tables.Where(allowedTables.ContainsKey).ToList();

            if (permitted.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            var rows = await new GetPostgresTableCommentsByNameQuery(connectionString)
                .ExecuteAsync(permitted, token).ConfigureAwait(false);

            return rows.ToDictionary(row => row.TableName, row => row.Comment);
        }

        public async Task<SqlValidation> ValidateAsync(string sql, CancellationToken token = default)
        {
            try
            {
                await ExplainLinesAsync(sql, token).ConfigureAwait(false);

                return new SqlValidation(true, null, null);
            }
            catch (PostgresException ex)
            {
                return new SqlValidation(false, ex.MessageText, ex.SqlState);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new SqlValidation(false, ex.Message, null);
            }
        }

        public async Task<SqlPlanSummary> ExplainAsync(string sql, CancellationToken token = default)
        {
            return SqlPlanParser.Parse(await ExplainLinesAsync(sql, token).ConfigureAwait(false));
        }

        public async Task<IReadOnlyList<SqlTableStatistic>> TableStatisticsAsync(string sql,
            CancellationToken token = default)
        {
            var relations = PermittedRelations(sql);

            if (relations.Count == 0)
            {
                return [];
            }

            var rows = await new GetPostgresRelationStatisticsByNameQuery(connectionString)
                .ExecuteAsync(relations, token).ConfigureAwait(false);

            return rows
                .Select(row => new SqlTableStatistic(row.RelationName, row.EstimatedRows, row.IndexScans,
                    row.SequentialScans))
                .ToList();
        }

        public async Task<SqlQueryHistory> QueryHistoryAsync(string sql, CancellationToken token = default)
        {
            var relations = PermittedRelations(sql);

            if (relations.Count == 0)
            {
                return noHistory;
            }

            var row = await new GetPostgresStatementStatisticsByRelationQuery(connectionString)
                .ExecuteAsync(relations, token).ConfigureAwait(false);

            if (row is null || row.Calls == 0)
            {
                return noHistory;
            }

            var blocks = row.SharedBlocksHit + row.SharedBlocksRead;

            return new SqlQueryHistory(
                true,
                row.Calls,
                row.TotalExecutionMilliseconds / row.Calls,
                row.MaximumExecutionMilliseconds,
                row.Rows / row.Calls,
                blocks <= 0 ? 100 : row.SharedBlocksHit / blocks * 100);
        }

        public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteAsync(string sql,
            CancellationToken token = default)
        {
            var stopwatch = Stopwatch.StartNew();
            string? rewrittenSql = null;

            try
            {
                var allowedTables =
                    DynamicSqlTableAccess.BuildAllowedTables(userName, hasPermission, conversationId);

                rewrittenSql = PostgresSqlValidator.RewriteTableReferences(sql, allowedTables);

                using var postgres = new Postgres(connectionString, log);

                var rows = await postgres
                    .ExecuteAlreadyTokenSwitchedAsync(rewrittenSql, token)
                    .ConfigureAwait(false);

                stopwatch.Stop();

                await TryLogAsync(rewrittenSql, true, rows.Count, stopwatch.ElapsedMilliseconds, null, token)
                    .ConfigureAwait(false);

                return rows
                    .Select(row => (IReadOnlyDictionary<string, object?>)row.ToDictionary(
                        pair => pair.Key, pair => pair.Value))
                    .ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                stopwatch.Stop();

                await TryLogAsync(rewrittenSql ?? sql, false, null, stopwatch.ElapsedMilliseconds, ex.Message, token)
                    .ConfigureAwait(false);

                throw;
            }
        }

        private IReadOnlyList<string> PermittedRelations(string sql)
        {
            var allowedTables =
                DynamicSqlTableAccess.BuildAllowedTables(userName, hasPermission, conversationId);

            return PostgresSqlValidator.CollectTableReferences(sql)
                .Where(allowedTables.ContainsKey)
                .ToList();
        }

        private async Task<IReadOnlyList<string>> ExplainLinesAsync(string sql, CancellationToken token)
        {
            var allowedTables =
                DynamicSqlTableAccess.BuildAllowedTables(userName, hasPermission, conversationId);

            using var postgres = new Postgres(connectionString, log);

            return await postgres.ExplainWithTableTokenSwitchAsync(sql, allowedTables, token).ConfigureAwait(false);
        }

        private async Task TryLogAsync(string sqlToLog, bool succeeded, int? rowCount, long durationMs,
            string? errorMessage, CancellationToken token)
        {
            try
            {
                await new DynamicSqlQueryLogRepository(dbContext)
                    .InsertAsync(sqlToLog, conversationId, userName, succeeded, rowCount, durationMs, errorMessage,
                        token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobySqlQueryClient: failed to write query log for user={userName}", ex);
                }
            }
        }
    }
}