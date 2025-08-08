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
using Jube.Data.Cache.Interfaces;
using log4net;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace Jube.Data.Cache.Postgres;

public class CachePayloadLatestRepository(string connectionString, ILog log) : ICachePayloadLatestRepository
{
    public async Task UpsertAsync(int tenantRegistryId, int entityAnalysisModelId, DateTime referenceDate,
        Guid entityAnalysisModelInstanceEntryGuid, string entryKey, string entryKeyValue)
    {
        await UpsertAsync(tenantRegistryId, entityAnalysisModelId, null, referenceDate,
            entityAnalysisModelInstanceEntryGuid,
            entryKey, entryKeyValue);
    }

    public async Task UpsertAsync(int tenantRegistryId, int entityAnalysisModelId,
        Dictionary<string, object> payload,
        DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid, string entryKey, string entryKeyValue)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sql = "insert into \"CachePayloadLatest\"(\"EntityAnalysisModelId\",\"Json\",\"ReferenceDate\"," +
                      "\"UpdatedDate\",\"EntityAnalysisModelInstanceEntryGuid\",\"EntryKey\",\"EntryKeyValue\",\"Counter\")" +
                      " values((@entityAnalysisModelId),(@json),(@referenceDate),(@updatedDate)," +
                      "(@entityAnalysisModelInstanceEntryGuid),(@entryKey),(@entryKeyValue),1) " +
                      "ON CONFLICT (\"EntityAnalysisModelId\",\"EntryKey\",\"EntryKeyValue\") " +
                      " DO UPDATE set \"Json\" = (@json), \"UpdatedDate\" = (@updatedDate)," +
                      "\"ReferenceDate\" = (@referenceDate),\"Counter\"=\"CachePayloadLatest\".\"Counter\"+1";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("json", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload));
            command.Parameters.AddWithValue("referenceDate", referenceDate);
            command.Parameters.AddWithValue("entryKeyValue", entryKeyValue);
            command.Parameters.AddWithValue("entryKey", entryKey);
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

    public async Task<List<string>> GetDistinctKeysAsync(int tenantRegistryId, int entityAnalysisModelId,
        string key, DateTime dateFrom,
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

    public async Task<List<string>> GetDistinctKeysAsync(int tenantRegistryId, int entityAnalysisModelId,
        string key, DateTime dateBefore)
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

    public async Task<List<string>> GetDistinctKeysAsync(int tenantRegistryId, int entityAnalysisModelId,
        string key)
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

    public async Task DeleteByReferenceDate(int tenantRegistryId, int entityAnalysisModelId,
        DateTime referenceDate, DateTime thresholdReferenceDate, int limit,
        List<(string name, string interval, int intervalValue)> searchKeys)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            // ReSharper disable once StringLiteralTypo
            var sql = "with i as (select ctid from \"CachePayloadLatest\" " +
                      "where \"ReferenceDate\" <= (@referenceDate) limit (@limit)) " +
                      // ReSharper disable once StringLiteralTypo
                      "delete from \"CachePayloadLatest\" " +
                      // ReSharper disable once StringLiteralTypo
                      "where ctid in " +
                      // ReSharper disable once StringLiteralTypo
                      "(select CTID from i)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("referenceDate", thresholdReferenceDate);
            command.Parameters.AddWithValue("limit", limit);

            int? rowsAffected = null;
            while (rowsAffected > 0 || rowsAffected == null) rowsAffected = await command.ExecuteNonQueryAsync();

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
    }
}