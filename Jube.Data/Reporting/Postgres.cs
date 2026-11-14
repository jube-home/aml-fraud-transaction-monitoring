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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Extensions;
using Jube.Data.Reporting.Models;
using Jube.Data.Repository.Exceptions;
using Jube.Dictionary;
using Jube.ResilientNpgsqlConnection;
using Jube.ResilientNpgsqlConnection.Extensions.Jube.ResilientNpgsqlConnection;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace Jube.Data.Reporting
{
    public class Postgres : IDisposable
    {
        public const int MaximumRows = 100000;
        private readonly bool blockProtectedRelations;
        private readonly ResilientNpgsqlConnection.ResilientNpgsqlConnection connection;
        private readonly ReportLimits limits;
        private bool disposed;

        public Postgres(string connectionString, ILog log, bool blockProtectedRelations = false,
            int? maxRetries = null, ReportLimits limits = null)
        {
            this.blockProtectedRelations = blockProtectedRelations;
            this.limits = limits ?? ReportLimits.Default with { StatementTimeoutSeconds = StatementTimeoutSeconds };

            connection =
                new ResilientNpgsqlConnection.ResilientNpgsqlConnection(
                    GuardedConnectionString(connectionString, this.limits.StatementTimeoutSeconds), log,
                    maxRetries);

            try
            {
                connection.Open();
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public static int StatementTimeoutSeconds { get; set; } = 30;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            connection.Dispose();
        }

        private static async Task<DbDataReader> ReadSchemaAsync(ResilientNpgsqlCommand command, string original,
            string submitted, CancellationToken token)
        {
            try
            {
                return await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo, token)
                    .ConfigureAwait(false);
            }
            catch (Exception e) when (PostgresErrorPosition.Annotate(e, original, submitted))
            {
                throw;
            }
        }

        public async Task<List<PostgresColumn>> IntrospectAsync(string sql, Dictionary<string, object> parameters,
            CancellationToken token = default)
        {
            var columns = new List<PostgresColumn>();
            var wrapSql = $"SELECT * FROM ({sql}) b LIMIT 0";

            PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);
            PostgresSqlValidator.AssertSelectOnly(wrapSql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, wrapSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            foreach (var (key, value) in parameters.Where(parameter => sql.Contains("@" + parameter.Key)))
            {
                command.Parameters.AddWithValue(key, value);
            }

            await using var reader = await ReadSchemaAsync(command, sql, sql, token).ConfigureAwait(false);

            var schema = reader.GetColumnSchema();

            foreach (var col in schema)
            {
                columns.Add(new PostgresColumn(col.ColumnName, col.DataTypeName ?? "unknown"));
            }

            return columns;
        }

        public async Task<List<PostgresColumn>> IntrospectWithTableTokenSwitchAsync(string sql,
            Dictionary<string, object> parameters, IReadOnlyDictionary<string, string> allowedTables,
            CancellationToken token = default)
        {
            var rewrittenSql = PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(sql,
                allowedTables);

            var columns = new List<PostgresColumn>();
            var wrapSql = $"SELECT * FROM ({rewrittenSql}) b LIMIT 0";

            PostgresSqlValidator.AssertSelectOnly(wrapSql, blockProtectedRelations, true);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, wrapSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            foreach (var (key, value) in parameters.Where(parameter => rewrittenSql.Contains("@" + parameter.Key)))
            {
                command.Parameters.AddWithValue(key, value);
            }

            await using var reader = await ReadSchemaAsync(command, sql, rewrittenSql, token)
                .ConfigureAwait(false);

            var schema = reader.GetColumnSchema();

            foreach (var col in schema)
            {
                columns.Add(new PostgresColumn(col.ColumnName, col.DataTypeName ?? "unknown"));
            }

            return columns;
        }

        public async Task<Dictionary<string, string>> IntrospectAsync(string sql, List<object> parameters,
            CancellationToken token = default)
        {
            var values = new Dictionary<string, string>();
            var wrapSql = $"SELECT * FROM ({sql}) b LIMIT 0";

            PostgresSqlValidator.AssertSelectOnly(wrapSql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, wrapSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo, token).ConfigureAwait(false);

            var schema = reader.GetColumnSchema();

            foreach (var col in schema)
            {
                values.Add(col.ColumnName, col.DataTypeName ?? "unknown");
            }

            return values;
        }

        public async Task PrepareAsync(string sql, List<object> parameters, CancellationToken token = default)
        {
            PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            await command.PrepareAsync(token);
        }

        public static string BuildArchiveSampleSql(string sql, int tokenCount, bool mockData)
        {
            return BuildArchiveSampleSql(sql, tokenCount, mockData ? SampleSource.Mock(0) : SampleSource.Archive(0));
        }

        public static string BuildArchiveSampleSql(string sql, int tokenCount, SampleSource source)
        {
            return $"select \"Json\" from \"{source.Table}\" where \"{source.KeyColumn}\" = (@{tokenCount - 1})"
                   + " and (" + sql + ")"
                   + $" order by \"EntityAnalysisModelInstanceEntryGuid\" limit (@{tokenCount})";
        }

        public Task<List<string>> ExecuteReturnOnlyJsonFromArchiveSampleAsync(int entityAnalysisModelId,
            string sql,
            string filterTokens,
            int limit, bool mockData, CancellationToken token = default)
        {
            return ExecuteReturnOnlyJsonFromArchiveSampleAsync(
                mockData ? SampleSource.Mock(entityAnalysisModelId) : SampleSource.Archive(entityAnalysisModelId),
                sql, filterTokens, limit, token);
        }

        public async Task<List<string>> ExecuteReturnOnlyJsonFromArchiveSampleAsync(SampleSource source,
            string sql,
            string filterTokens,
            int limit, CancellationToken token = default)
        {
            var value = new List<string>();

            var tokens = JsonConvert.DeserializeObject<List<object>>(filterTokens);
            tokens.Add(source.Key);
            tokens.Add(limit);

            var dynamicGatedSql = BuildArchiveSampleSql(sql, tokens.Count, source);

            PostgresSqlValidator.AssertSelectOnly(dynamicGatedSql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, dynamicGatedSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            for (var i = 0; i < tokens.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), tokens[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                value.Add(reader.GetValue(0).AsString());
            }

            return value;
        }

        public static string BuildArchiveCountSql(string sql, int tokenCount, SampleSource source)
        {
            return $"select count(*) from \"{source.Table}\" where \"{source.KeyColumn}\" = (@{tokenCount})"
                   + " and (" + sql + ")";
        }

        public async Task<long> ExecuteCountFromArchiveAsync(SampleSource source, string sql, string filterTokens,
            CancellationToken token = default)
        {
            var tokens = JsonConvert.DeserializeObject<List<object>>(filterTokens);
            tokens.Add(source.Key);

            var dynamicGatedSql = BuildArchiveCountSql(sql, tokens.Count, source);

            PostgresSqlValidator.AssertSelectOnly(dynamicGatedSql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, dynamicGatedSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            for (var i = 0; i < tokens.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), tokens[i]);
            }

            var result = await command.ExecuteScalarAsync(token).ConfigureAwait(false);

            return Convert.ToInt64(result);
        }

        public async IAsyncEnumerable<JObject> StreamJsonFromArchiveSampleAsync(int entityAnalysisModelId,
            double sample, DateTime? dateFrom, DateTime? dateTo,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            var dynamicGatedSql = """
                                  SELECT a."Json"
                                  FROM "Archive" a
                                  INNER JOIN "EntityAnalysisModel" e ON a."EntityAnalysisModelId" = e."Id"
                                  WHERE e."Id" = @entityAnalysisModelId
                                  AND a."ReferenceDate" BETWEEN @dateFrom AND @dateTo
                                  AND random() < @sample
                                  LIMIT 100000
                                  """;

            PostgresSqlValidator.AssertSelectOnly(dynamicGatedSql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, dynamicGatedSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;
            command.Parameters.AddWithValue("@entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("@sample", sample);
            command.Parameters.AddWithValue("@dateFrom", dateFrom ?? DateTime.UtcNow);
            command.Parameters.AddWithValue("@dateTo", dateTo ?? DateTime.UtcNow.AddDays(-1));

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                yield return JObject.Parse(reader.GetValue(0).AsString());
            }
        }

        public async Task<List<DictionaryNoBoxing<string>>> ExecuteReturnPayloadFromArchiveAfterAsync(
            string sql,
            DateTime adjustedStartDate,
            DateTime lastReferenceDate,
            DateTime snapshotDate,
            DateTime afterReferenceDate,
            Guid afterEntityAnalysisModelInstanceEntryGuid,
            int limit,
            CancellationToken token = default)
        {
            PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            command.Parameters.Add(new NpgsqlParameter("adjustedStartDate", NpgsqlDbType.Timestamp)
                { Value = DateTime.SpecifyKind(adjustedStartDate, DateTimeKind.Unspecified) });
            command.Parameters.Add(new NpgsqlParameter("lastReferenceDate", NpgsqlDbType.Timestamp)
                { Value = DateTime.SpecifyKind(lastReferenceDate, DateTimeKind.Unspecified) });
            command.Parameters.Add(new NpgsqlParameter("snapshotDate", NpgsqlDbType.Timestamp)
                { Value = DateTime.SpecifyKind(snapshotDate, DateTimeKind.Unspecified) });
            command.Parameters.Add(new NpgsqlParameter("afterReferenceDate", NpgsqlDbType.Timestamp)
                { Value = DateTime.SpecifyKind(afterReferenceDate, DateTimeKind.Unspecified) });

            command.Parameters.Add(new NpgsqlParameter("afterEntityAnalysisModelInstanceEntryGuid", NpgsqlDbType.Uuid)
                { Value = afterEntityAnalysisModelInstanceEntryGuid });

            command.Parameters.AddWithValue("limit", limit);

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var value = new List<DictionaryNoBoxing<string>>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                var dictionaryNoBoxing = new DictionaryNoBoxing<string>();

                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (!dictionaryNoBoxing.ContainsKey(reader.GetName(index)))
                    {
                        var clrType = reader.GetFieldType(index);

                        if (await reader.IsDBNullAsync(index, token))
                        {
                            continue;
                        }

                        if (clrType == typeof(int))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsInt());
                        }
                        else if (clrType == typeof(decimal) || clrType == typeof(float) || clrType == typeof(double))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsDouble());
                        }
                        else if (clrType == typeof(bool))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetBoolean(index));
                        }
                        else if (clrType == typeof(string))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsString());
                        }
                        else if (clrType == typeof(DateTime))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsDateTime());
                        }
                        else if (clrType == typeof(Guid))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsGuid());
                        }
                    }
                }

                value.Add(dictionaryNoBoxing);

                if (value.Count > MaximumRows)
                {
                    throw new InvalidOperationException("The result exceeds the maximum number of rows permitted.");
                }
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteByNamedParametersAsync(string sql,
            Dictionary<string, object> parameters, CancellationToken token = default)
        {
            PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            foreach (var (key, o) in parameters)
            {
                command.Parameters.AddWithValue("@" + key, o);
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var value = new List<IDictionary<string, object>>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                IDictionary<string, object> eo = new ExpandoObject();

                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (!eo.ContainsKey(reader.GetName(index)))
                    {
                        eo.Add(reader.GetName(index),
                            await reader.IsDBNullAsync(index, token) ? null : reader.GetValue(index));
                    }
                }

                value.Add(eo);

                if (value.Count > MaximumRows)
                {
                    throw new InvalidOperationException("The result exceeds the maximum number of rows permitted.");
                }
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteWithTableTokenSwitchByNamedParametersAsync(
            string sql, Dictionary<string, object> parameters, IReadOnlyDictionary<string, string> allowedTables,
            CancellationToken token = default)
        {
            var rewrittenSql = PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(sql,
                allowedTables);

            PostgresSqlValidator.AssertSelectOnly(rewrittenSql, blockProtectedRelations, true);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, rewrittenSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            foreach (var (key, o) in parameters)
            {
                command.Parameters.AddWithValue("@" + key, o);
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var value = new List<IDictionary<string, object>>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                IDictionary<string, object> eo = new ExpandoObject();

                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (!eo.ContainsKey(reader.GetName(index)))
                    {
                        eo.Add(reader.GetName(index),
                            await reader.IsDBNullAsync(index, token) ? null : reader.GetValue(index));
                    }
                }

                value.Add(eo);

                if (value.Count > MaximumRows)
                {
                    throw new InvalidOperationException("The result exceeds the maximum number of rows permitted.");
                }
            }

            return value;
        }

        public async Task<PostgresResult> ExecuteLimitedByNamedParametersAsync(string sql,
            Dictionary<string, object> parameters, IReadOnlyDictionary<string, string> allowedTables,
            int maximumRows, CancellationToken token = default)
        {
            if (allowedTables == null)
            {
                PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);
            }

            var effectiveSql = allowedTables == null
                ? sql
                : PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(sql, allowedTables);

            var limitedSql = $"SELECT * FROM ({effectiveSql}) b LIMIT {maximumRows + 1}";

            PostgresSqlValidator.AssertSelectOnly(limitedSql, blockProtectedRelations, allowedTables != null);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, limitedSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            foreach (var (key, o) in parameters)
            {
                command.Parameters.AddWithValue("@" + key, o);
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var columns = new List<PostgresColumn>(reader.FieldCount);

            for (var index = 0; index < reader.FieldCount; index++)
            {
                columns.Add(new PostgresColumn(reader.GetName(index), reader.GetDataTypeName(index)));
            }

            var rows = new List<object[]>();
            var truncated = false;
            long resultBytes = 0;

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                if (rows.Count == maximumRows)
                {
                    truncated = true;
                    break;
                }

                var values = new object[reader.FieldCount];

                for (var index = 0; index < values.Length; index++)
                {
                    values[index] = await reader.IsDBNullAsync(index, token).ConfigureAwait(false)
                        ? null
                        : reader.GetValue(index);

                    var cellBytes = ApproximateBytes(values[index], limits.MaximumCellBytes);

                    if (cellBytes > limits.MaximumCellBytes)
                    {
                        throw SqlValidationFailedException.Coded(DatasourceMessages.CellTooLarge,
                            columns[index].Name, limits.MaximumCellBytes);
                    }

                    resultBytes += cellBytes;
                }

                if (resultBytes > limits.MaximumResultBytes)
                {
                    throw SqlValidationFailedException.Coded(DatasourceMessages.ResultTooLarge,
                        limits.MaximumResultBytes);
                }

                rows.Add(values);
            }

            return new PostgresResult(columns, rows, truncated);
        }

        public Task<List<IDictionary<string, object>>> ExecuteWithTableTokenSwitchAsync(string sql,
            IReadOnlyDictionary<string, string> allowedTables, CancellationToken token = default)
        {
            var rewrittenSql = PostgresSqlValidator.RewriteTableReferences(sql, allowedTables);

            return ExecuteAlreadyTokenSwitchedAsync(rewrittenSql, token);
        }

        public async Task<List<IDictionary<string, object>>> ExecuteAlreadyTokenSwitchedAsync(
            string rewrittenSql, CancellationToken token = default)
        {
            PostgresSqlValidator.AssertSelectOnly(rewrittenSql, blockProtectedRelations, true);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, rewrittenSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var value = new List<IDictionary<string, object>>();
            long resultBytes = 0;

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                IDictionary<string, object> eo = new ExpandoObject();

                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (!eo.ContainsKey(reader.GetName(index)))
                    {
                        var cell = await reader.IsDBNullAsync(index, token) ? null : reader.GetValue(index);
                        var cellBytes = ApproximateBytes(cell, limits.MaximumCellBytes);

                        if (cellBytes > limits.MaximumCellBytes)
                        {
                            throw new InvalidOperationException(
                                $"The column '{reader.GetName(index)}' holds a value over the " +
                                $"{limits.MaximumCellBytes} byte limit. Select less of it, for example a JSON key " +
                                "or a substring, rather than the whole value.");
                        }

                        resultBytes += cellBytes;
                        eo.Add(reader.GetName(index), cell);
                    }
                }

                if (resultBytes > limits.MaximumResultBytes)
                {
                    throw new InvalidOperationException(
                        $"The result exceeds the {limits.MaximumResultBytes} byte limit. Aggregate, filter or " +
                        "select fewer columns.");
                }

                value.Add(eo);

                if (value.Count > MaximumRows)
                {
                    throw new InvalidOperationException("The result exceeds the maximum number of rows permitted.");
                }
            }

            return value;
        }

        public async Task<List<string>> ExplainWithTableTokenSwitchAsync(string sql,
            IReadOnlyDictionary<string, string> allowedTables, CancellationToken token = default)
        {
            var rewrittenSql = PostgresSqlValidator.RewriteTableReferences(sql, allowedTables);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, "EXPLAIN " + rewrittenSql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var lines = new List<string>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                if (!await reader.IsDBNullAsync(0, token).ConfigureAwait(false))
                {
                    lines.Add(reader.GetString(0));
                }
            }

            return lines;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteByOrderedParametersAsync(string sql,
            List<object> parameters, CancellationToken token = default)
        {
            PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var value = new List<IDictionary<string, object>>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                IDictionary<string, object> eo = new ExpandoObject();

                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (!eo.ContainsKey(reader.GetName(index)))
                    {
                        eo.Add(reader.GetName(index),
                            await reader.IsDBNullAsync(index, token) ? null : reader.GetValue(index));
                    }
                }

                value.Add(eo);

                if (value.Count > MaximumRows)
                {
                    throw new InvalidOperationException("The result exceeds the maximum number of rows permitted.");
                }
            }

            return value;
        }

        public async Task<int?> ExecuteScalarIdAsync(string sql,
            List<object> parameters, CancellationToken token = default)
        {
            PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            var id = await command.ExecuteScalarAsync(token).ConfigureAwait(false);

            if (id == null)
            {
                return null;
            }

            return (int)id;
        }

        public static long ApproximateBytes(object value, long ceiling)
        {
            switch (value)
            {
                case null:
                    return 0;
                case string text:
                    return text.Length > ceiling ? text.Length : Encoding.UTF8.GetByteCount(text);
                case byte[] bytes:
                    return bytes.Length;
                case Array array:
                {
                    long total = 0;

                    foreach (var item in array)
                    {
                        total += ApproximateBytes(item, ceiling - total);

                        if (total > ceiling)
                        {
                            return total;
                        }
                    }

                    return total;
                }
                default:
                    return 16;
            }
        }

        private static string GuardedConnectionString(string connectionString, int statementTimeoutSeconds)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            var settings =
                $"-c default_transaction_read_only=on -c statement_timeout={statementTimeoutSeconds * 1000} -c lock_timeout=5000";

            builder.Options = string.IsNullOrWhiteSpace(builder.Options) ? settings : builder.Options + " " + settings;

            return builder.ConnectionString;
        }

        private async Task<GuardScope> BeginGuardAsync(CancellationToken token)
        {
            await using (var begin = new ResilientNpgsqlCommand(connection,
                             $"BEGIN READ ONLY; SET LOCAL statement_timeout = {limits.StatementTimeoutSeconds * 1000}; SET LOCAL lock_timeout = 5000"))
            {
                await begin.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }

            return new GuardScope(connection);
        }
    }
}