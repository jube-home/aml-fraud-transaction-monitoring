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

namespace Jube.Data.Reporting
{
    using Jube.Data.Reporting.Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Dynamic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dictionary;
    using Extension;
    using log4net;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Npgsql;
    using NpgsqlTypes;
    using ResilientNpgsqlConnection;
    using ResilientNpgsqlConnection.Extensions.Jube.ResilientNpgsqlConnection;

    public class Postgres : IDisposable
    {
        private readonly ResilientNpgsqlConnection connection;
        private readonly bool blockProtectedRelations;
        private readonly bool parserAssertSelectOnly;
        private bool disposed;

        public static int StatementTimeoutSeconds { get; set; } = 30;

        public const int MaximumRows = 100000;

        private static string GuardedConnectionString(string connectionString, bool guarded)
        {
            if (!guarded)
            {
                return connectionString;
            }

            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var settings =
                $"-c default_transaction_read_only=on -c statement_timeout={StatementTimeoutSeconds * 1000} -c lock_timeout=5000";
            builder.Options = string.IsNullOrWhiteSpace(builder.Options) ? settings : builder.Options + " " + settings;
            return builder.ConnectionString;
        }

        private async Task<GuardScope> BeginGuardAsync(CancellationToken token)
        {
            if (!parserAssertSelectOnly)
            {
                return new GuardScope(null);
            }

            await using (var begin = new ResilientNpgsqlCommand(connection,
                             $"BEGIN READ ONLY; SET LOCAL statement_timeout = {StatementTimeoutSeconds * 1000}; SET LOCAL lock_timeout = 5000"))
            {
                await begin.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }

            return new GuardScope(connection);
        }

        public Postgres(string connectionString, ILog log, bool parserAssertSelectOnly,
            bool blockProtectedRelations = false)
        {
            this.parserAssertSelectOnly = parserAssertSelectOnly;
            this.blockProtectedRelations = blockProtectedRelations;
            connection =
                new ResilientNpgsqlConnection(GuardedConnectionString(connectionString, parserAssertSelectOnly), log);
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

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            connection.Dispose();
        }

        public async Task<Dictionary<string, string>> IntrospectAsync(string sql, Dictionary<string, object> parameters,
            CancellationToken token = default)
        {
            var values = new Dictionary<string, string>();
            var wrapSql = $"SELECT * FROM ({sql}) b LIMIT 0";

            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(wrapSql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, wrapSql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;

            foreach (var (key, value) in parameters.Where(parameter => sql.Contains("@" + parameter.Key)))
            {
                command.Parameters.AddWithValue(key, value);
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

        public async Task<Dictionary<string, string>> IntrospectAsync(string sql, List<object> parameters,
            CancellationToken token = default)
        {
            var values = new Dictionary<string, string>();
            var wrapSql = $"SELECT * FROM ({sql}) b LIMIT 0";

            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(wrapSql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, wrapSql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;

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
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            await command.PrepareAsync(token);
        }

        public async Task<List<string>> ExecuteReturnOnlyJsonFromArchiveSampleAsync(int entityAnalysisModelId,
            string sql,
            string filterTokens,
            int limit, bool mockData, CancellationToken token = default)
        {
            var value = new List<string>();

            var tokens = JsonConvert.DeserializeObject<List<object>>(filterTokens);
            tokens.Add(entityAnalysisModelId);
            tokens.Add(limit);

            var tableName = mockData ? "MockArchive" : "Archive";

            var dynamicGatedSql =
                $"select \"Json\" from \"{tableName}\" where \"EntityAnalysisModelId\" = (@{tokens.Count - 1})"
                + " and (" + sql + ")"
                + $" order by \"EntityAnalysisModelInstanceEntryGuid\" limit (@{tokens.Count})";

            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(dynamicGatedSql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, dynamicGatedSql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;

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

        public async Task<List<JObject>> ExecuteReturnOnlyJsonFromArchiveSampleAsync(int entityAnalysisModelId,
            double sample, DateTime? dateFrom, DateTime? dateTo, CancellationToken token = default)
        {
            var value = new List<JObject>();

            var dynamicGatedSql = """
                                  SELECT a."Json"
                                  FROM "Archive" a
                                  INNER JOIN "EntityAnalysisModel" e ON a."EntityAnalysisModelId" = e."Id"
                                  WHERE e."Id" = @entityAnalysisModelId
                                  AND a."ReferenceDate" BETWEEN @dateFrom AND @dateTo
                                  AND random() < @sample
                                  LIMIT 100000
                                  """;

            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(dynamicGatedSql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, dynamicGatedSql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;
            command.Parameters.AddWithValue("@entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("@sample", sample);
            command.Parameters.AddWithValue("@dateFrom", dateFrom ?? DateTime.UtcNow);
            command.Parameters.AddWithValue("@dateTo", dateTo ?? DateTime.UtcNow.AddDays(-1));

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                value.Add(JObject.Parse(reader.GetValue(0).AsString()));
            }

            return value;
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
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;

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
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;

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

        public async Task<List<IDictionary<string, object>>> ExecuteByOrderedParametersAsync(string sql,
            List<object> parameters, CancellationToken token = default)
        {
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;

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
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);
            }

            await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);

            await using var command = new ResilientNpgsqlCommand(connection, sql);
            command.CommandTimeout = StatementTimeoutSeconds + 5;

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
    }
}