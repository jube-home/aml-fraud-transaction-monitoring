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
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace Jube.Data.Cache
{
    public class CachePayloadRepository(string connectionString, ILog log)
    {
        public async Task CreateIndexAsync(string name, string date, string expression, int entityAnalysisModelId)
        {
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                var sql = "create index \"" + name + "\" on \"CachePayload\"" +
                          " (\"EntityAnalysisModelId\"," + date + " DESC," + expression + ")" +
                          " where \"EntityAnalysisModelId\" = " + entityAnalysisModelId;

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
        }

        public async Task<List<string>> GetIndexesAsync()
        {
            var connection = new NpgsqlConnection(connectionString);
            var value = new List<string>();
            try
            {
                await connection.OpenAsync();

                var sql = "select indexname" +
                          " from pg_indexes " +
                          " where tablename =  'CachePayload';";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    if (!reader.IsDBNull(0))
                        value.Add(reader.GetValue(0).ToString());

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return value;
        }

        public async Task InsertAsync(int entityAnalysisModelId, Dictionary<string, object> payload,
            DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid)
        {
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                var sql = "insert into\"CachePayload\"(\"EntityAnalysisModelId\",\"Json\",\"ReferenceDate\"," +
                          "\"CreatedDate\",\"EntityAnalysisModelInstanceEntryGuid\")" +
                          " values((@entityAnalysisModelId),(@json),(@referenceDate),(@createdDate)," +
                          "(@entityAnalysisModelInstanceEntryGuid))";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
                command.Parameters.AddWithValue("json", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload));
                command.Parameters.AddWithValue("referenceDate", referenceDate);
                command.Parameters.AddWithValue("createdDate", DateTime.Now);
                command.Parameters.AddWithValue("entityAnalysisModelInstanceEntryGuid",
                    entityAnalysisModelInstanceEntryGuid);

                await command.PrepareAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task UpsertAsync(int entityAnalysisModelId, Dictionary<string, object> payload,
            DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid)
        {
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                var sql = "insert into\"CachePayload\"(\"EntityAnalysisModelId\",\"Json\",\"ReferenceDate\"," +
                          "\"CreatedDate\",\"EntityAnalysisModelInstanceEntryGuid\")" +
                          " values((@entityAnalysisModelId),(@json),(@referenceDate),(@createdDate)," +
                          "(@entityAnalysisModelInstanceEntryGuid)) " +
                          "ON CONFLICT (\"EntityAnalysisModelInstanceEntryGuid\") " +
                          " DO UPDATE set \"Json\" = (@json), \"UpdatedDate\" = (@updatedDate)";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
                command.Parameters.AddWithValue("json", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload));
                command.Parameters.AddWithValue("referenceDate", referenceDate);
                command.Parameters.AddWithValue("createdDate", DateTime.Now);
                command.Parameters.AddWithValue("updatedDate", DateTime.Now);
                command.Parameters.AddWithValue("entityAnalysisModelInstanceEntryGuid",
                    entityAnalysisModelInstanceEntryGuid);

                await command.PrepareAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<List<string>> GetDistinctKeysAsync(int entityAnalysisModelId, string key, DateTime dateFrom,
            DateTime dateTo)
        {
            var connection = new NpgsqlConnection(connectionString);
            var value = new List<string>();
            try
            {
                await connection.OpenAsync();

                var sql = "select \"EntryKeyValue\" " +
                          " from \"CachePayloadLatest\"" +
                          " where \"EntityAnalysisModelId\" = (@entityAnalysisModelId) and " +
                          "\"EntryKey\" = (@key)" +
                          " and \"UpdatedDate\" > (@dateFrom)" +
                          " and \"UpdatedDate\" <= (@dateTo)";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("key", key);
                command.Parameters.AddWithValue("dateFrom", dateFrom);
                command.Parameters.AddWithValue("dateTo", dateTo);
                command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);

                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    if (!reader.IsDBNull(0))
                        value.Add(reader.GetValue(0).ToString());

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return value;
        }

        public async Task<List<string>> GetDistinctKeysAsync(int entityAnalysisModelId, string key, DateTime dateBefore)
        {
            var connection = new NpgsqlConnection(connectionString);
            var value = new List<string>();
            try
            {
                await connection.OpenAsync();

                var sql = "select \"EntryKeyValue\" " +
                          " from \"CachePayloadLatest\"" +
                          " where \"EntityAnalysisModelId\" = (@entityAnalysisModelId)" +
                          " and \"EntryKey\" = (@key)" +
                          " and \"UpdatedDate\" < (@dateBefore)";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("key", key);
                command.Parameters.AddWithValue("dateBefore", dateBefore);
                command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    if (!reader.IsDBNull(0))
                        value.Add(reader.GetValue(0).ToString());

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return value;
        }


        public async Task<List<string>> GetDistinctKeysAsync(int entityAnalysisModelId, string key)
        {
            var connection = new NpgsqlConnection(connectionString);
            var value = new List<string>();
            try
            {
                await connection.OpenAsync();

                var sql = "select \"EntryKeyValue\" ->> (@key)" +
                          " from \"CachePayloadLatest\"" +
                          " where \"EntityAnalysisModelId\" = (@entityAnalysisModelId) " +
                          " and \"EntryKey\" = (@key) ";
                          

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
                command.Parameters.AddWithValue("key", key);
                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    if (!reader.IsDBNull(0))
                        value.Add(reader.GetValue(0).ToString());

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return value;
        }

        public async Task<List<Dictionary<string, object>>> GetSqlByKeyValueLimitAsync(string sql,
            string key, string value, string order, int limit)
        {
            var connection = new NpgsqlConnection(connectionString);
            var documents = new List<Dictionary<string, object>>();
            try
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("key", key);
                command.Parameters.AddWithValue("value", value);
                command.Parameters.AddWithValue("order", order);
                command.Parameters.AddWithValue("limit", limit);
                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var document = new Dictionary<string, object>();
                    for (var index = 0; index < reader.FieldCount; index++)
                        if (!reader.IsDBNull(index))
                        {
                            if (document.ContainsKey(reader.GetName(index))) continue;
                            
                            document.Add(reader.GetName(index), reader.GetValue(index));
                        }

                    documents.Add(document);
                }

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return documents;
        }
        
        public async Task<List<Dictionary<string, object>>> GetSqlByKeyValueLimitAsyncExcludeCurrent(string sqlSelect,
            string sqlFrom,string sqlOrderBy,
            string key, string value, string order, int limit,
            Guid entityInconsistentAnalysisModelInstanceEntryGuid)
        {
            var connection = new NpgsqlConnection(connectionString);
            var documents = new List<Dictionary<string, object>>();
            try
            {
                await connection.OpenAsync();

                var sql = sqlSelect + sqlFrom + " and \"EntityAnalysisModelInstanceEntryGuid\" " +
                          "!= @entityAnalysisModelInstanceEntryGuid " + sqlOrderBy;
                
                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("key", key);
                command.Parameters.AddWithValue("value", value);
                command.Parameters.AddWithValue("order", order);
                command.Parameters.AddWithValue("limit", limit);
                command.Parameters.AddWithValue("entityAnalysisModelInstanceEntryGuid", entityInconsistentAnalysisModelInstanceEntryGuid);
                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var document = new Dictionary<string, object>();
                    for (var index = 0; index < reader.FieldCount; index++)
                        if (!reader.IsDBNull(index))
                        {
                            if (document.ContainsKey(reader.GetName(index))) continue;

                            document.Add(reader.GetName(index), reader.GetValue(index));
                        }

                    documents.Add(document);
                }

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return documents;
        }

        public async Task<List<Dictionary<string, object>>> GetInitialCountsAsync(int entityAnalysisModelId)
        {
            var connection = new NpgsqlConnection(connectionString);
            var documents = new List<Dictionary<string, object>>();
            try
            {
                await connection.OpenAsync();

                var sql = "select count(*) as \"Count\",max(\"ReferenceDate\") as \"Max\"," +
                          "min(\"ReferenceDate\") as \"Min\" from \"CachePayload\" " +
                          "where \"EntityAnalysisModelId\" = (@entityAnalysisModelId)";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
                await command.PrepareAsync();

                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var document = new Dictionary<string, object>();
                    for (var index = 0; index < reader.FieldCount; index++)
                        if (!reader.IsDBNull(index))
                        {
                            if (!document.ContainsKey(reader.GetName(index)))
                            {
                                document.Add(reader.GetName(index), reader.GetValue(index));
                            }
                        }

                    documents.Add(document);
                }

                await reader.CloseAsync();
                await reader.DisposeAsync();
                await command.DisposeAsync();
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            return documents;
        }
    }
}