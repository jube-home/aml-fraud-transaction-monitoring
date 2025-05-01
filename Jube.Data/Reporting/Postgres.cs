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
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Extension;
using Newtonsoft.Json;
using Npgsql;

namespace Jube.Data.Reporting
{
    public class Postgres(string connectionString)
    {
        public async Task<Dictionary<string, string>> IntrospectAsync(string sql, Dictionary<string, object> parameters)
        {
            var connection = new NpgsqlConnection(connectionString);
            var values = new Dictionary<string, string>();
            try
            {
                await connection.OpenAsync();

                var tableName = "Temp_" + Guid.NewGuid().ToString("N");

                var wrapSql = $"select * into TEMPORARY TABLE {tableName} from (select * from ({sql}) b LIMIT 0) c";
                var commandTempTable = new NpgsqlCommand(wrapSql);
                commandTempTable.Connection = connection;

                foreach (var (key, value) in parameters.Where(parameter => sql.Contains("@" + parameter.Key)))
                    commandTempTable.Parameters.AddWithValue(key, value);

                await commandTempTable.ExecuteNonQueryAsync();

                var introspectionSql = "SELECT attname, format_type(atttypid, atttypmod) AS type" +
                                       " FROM pg_attribute" +
                                       $" WHERE attrelid = '{tableName}'::regclass" +
                                       " AND attnum > 0 " +
                                       " AND NOT attisdropped " +
                                       " ORDER BY attnum";


                var command = new NpgsqlCommand(introspectionSql);
                command.Connection = connection;

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    values.Add(reader.GetValue(0).AsString(), reader.GetValue(1).AsString());

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return values;
        }

        public async Task PrepareAsync(string sql, List<object> parameters)
        {
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand();
                command.Connection = connection;

                for (var i = 0; i < parameters.Count; i++)
                {
                    var paramName = "@param" + (i + 1);
                    sql = sql.Replace("@" + (i + 1), paramName);
                    command.Parameters.AddWithValue(paramName, parameters[i]);
                }

                command.CommandText = sql;
                await command.PrepareAsync();
            }
            catch
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
        }

        public async Task<List<string>> ExecuteReturnOnlyJsonFromArchiveSampleAsync(int entityAnalysisModelId,
            string filterSql,
            string filterTokens,
            int limit, bool mockData)
        {
            var value = new List<string>();
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                var tokens = JsonConvert.DeserializeObject<List<object>>(filterTokens);
                tokens.Add(entityAnalysisModelId);
                tokens.Add(limit);

                var tableName = mockData ? "MockArchive" : "Archive";

                var sql =
                    $"select \"Json\" from \"{tableName}\" where \"EntityAnalysisModelId\" = (@{tokens.Count - 1})"
                    + " and " + filterSql
                    + $" order by \"EntityAnalysisModelInstanceEntryGuid\" limit (@{limit})";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;

                for (var i = 0; i < tokens.Count; i++) command.Parameters.AddWithValue("@" + (i + 1), tokens[i]);

                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) value.Add(reader.GetValue(0).AsString());

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteReturnPayloadFromArchiveWithSkipLimitAsync(
            string sql,
            DateTime adjustedStartDate,
            int skip,
            int limit)
        {
            var value = new List<IDictionary<string, object>>();
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("adjustedStartDate", adjustedStartDate);
                command.Parameters.AddWithValue("limit", limit);
                command.Parameters.AddWithValue("skip", skip);
                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var eo = new ExpandoObject() as IDictionary<string, object>;
                    for (var index = 0; index < reader.FieldCount; index++)
                        if (!eo.ContainsKey(reader.GetName(index)))
                        {
                            eo.Add(reader.GetName(index), reader.IsDBNull(index) ? null : reader.GetValue(index));
                        }

                    value.Add(eo);
                }

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteByNamedParametersAsync(string sql,
            Dictionary<string, object> parameters)
        {
            var value = new List<IDictionary<string, object>>();
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;

                foreach (var (key, o) in parameters)
                    command.Parameters.AddWithValue("@" + key, o);

                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var eo = new ExpandoObject() as IDictionary<string, object>;
                    for (var index = 0; index < reader.FieldCount; index++)
                        if (!eo.ContainsKey(reader.GetName(index)))
                        {
                            eo.Add(reader.GetName(index), reader.IsDBNull(index) ? null : reader.GetValue(index));
                        }

                    value.Add(eo);
                }

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteByOrderedParametersAsync(string sql,
            List<object> parameters)
        {
            var value = new List<IDictionary<string, object>>();
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;

                for (var i = 0; i < parameters.Count; i++)
                    command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);

                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var eo = new ExpandoObject() as IDictionary<string, object>;
                    for (var index = 0; index < reader.FieldCount; index++)
                        if (!eo.ContainsKey(reader.GetName(index)))
                        {
                            eo.Add(reader.GetName(index), reader.IsDBNull(index) ? null : reader.GetValue(index));
                        }

                    value.Add(eo);
                }

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return value;
        }
    }
}