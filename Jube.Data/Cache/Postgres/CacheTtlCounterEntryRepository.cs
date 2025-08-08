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
using Jube.Data.Extension;
using log4net;
using Npgsql;

namespace Jube.Data.Cache.Postgres;

public class CacheTtlCounterEntryRepository(string connectionString, ILog log) : ICacheTtlCounterEntryRepository
{
    public async Task<List<ExpiredTtlCounterEntryDto>> GetExpiredTtlCounterCacheCountsAsync(
        int tenantRegistryId,
        int entityAnalysisModelId,
        int entityAnalysisModelTtlCounterId,
        string dataName, DateTime referenceDate)
    {
        var connection = new NpgsqlConnection(connectionString);
        var value = new List<ExpiredTtlCounterEntryDto>();
        try
        {
            await connection.OpenAsync();

            var sql = "select \"DataValue\",\"Value\",\"ReferenceDate\"" +
                      " from \"CacheTtlCounterEntry\"" +
                      " where \"EntityAnalysisModelTtlCounterId\" = (@entityAnalysisModelTtlCounterId)" +
                      " and \"DataName\" = (@dataName)" +
                      " and \"EntityAnalysisModelId\" = (@entityAnalysisModelId)" +
                      " and \"ReferenceDate\" <= (@referenceDate)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("entityAnalysisModelTtlCounterId", entityAnalysisModelTtlCounterId);
            command.Parameters.AddWithValue("dataName", dataName);
            command.Parameters.AddWithValue("referenceDate", referenceDate);
            await command.PrepareAsync();

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                value.Add(new ExpiredTtlCounterEntryDto
                {
                    DataValue = reader.GetValue(0).ToString() ?? string.Empty,
                    Value = reader.GetValue(1).AsInt(),
                    ReferenceDate = reader.GetValue(2).AsDateTime()
                });

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

    public async Task<int> GetAsync(int tenantRegistryId,
        int entityAnalysisModelId, int entityAnalysisModelTtlCounterId,
        string dataName, string dataValue,
        DateTime referenceDateFrom, DateTime referenceDateTo)
    {
        var connection = new NpgsqlConnection(connectionString);
        var value = 0;
        try
        {
            await connection.OpenAsync();

            var sql = "select COALESCE(sum(\"Value\"),0)::int from \"CacheTtlCounterEntry\" " +
                      " where \"EntityAnalysisModelId\" = (@entityAnalysisModelId) " +
                      "and \"EntityAnalysisModelTtlCounterId\" = (@entityAnalysisModelTtlCounterId) " +
                      "and \"DataName\" = (@dataName) " +
                      "and \"DataValue\" = (@dataValue) " +
                      "and \"ReferenceDate\" >= (@referenceDateFrom) " +
                      "and \"ReferenceDate\" <= (@referenceDateTo) ";

            var command = new NpgsqlCommand();
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("dataName", dataName);
            command.Parameters.AddWithValue("dataValue", dataValue);
            command.Parameters.AddWithValue("entityAnalysisModelTtlCounterId", entityAnalysisModelTtlCounterId);
            command.Parameters.AddWithValue("referenceDateFrom", referenceDateFrom);
            command.Parameters.AddWithValue("referenceDateTo", referenceDateTo);
            command.CommandText = sql;
            command.Connection = connection;
            await command.PrepareAsync();

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                value = (int)reader.GetValue(0);

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

    public async Task UpsertAsync(int tenantRegistryId,
        int entityAnalysisModelId, string dataName, string dataValue,
        int entityAnalysisModelTtlCounterId,
        DateTime referenceDate, int increment)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sql = "insert into \"CacheTtlCounterEntry\"(\"EntityAnalysisModelId\",\"DataName\",\"DataValue\"," +
                      "\"EntityAnalysisModelTtlCounterId\",\"Value\",\"ReferenceDate\",\"UpdatedDate\")" +
                      " values((@entityAnalysisModelId),(@dataName),(@dataValue)," +
                      "(@entityAnalysisModelTtlCounterId),1,(@referenceDate),(@updatedDate)) " +
                      " ON CONFLICT (\"EntityAnalysisModelId\",\"EntityAnalysisModelTtlCounterId\"," +
                      "\"DataName\",\"DataValue\",\"ReferenceDate\") " +
                      " DO UPDATE set \"Value\" = \"CacheTtlCounterEntry\".\"Value\" + " + increment + "";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("dataName", dataName);
            command.Parameters.AddWithValue("dataValue", dataValue);
            command.Parameters.AddWithValue("entityAnalysisModelTtlCounterId", entityAnalysisModelTtlCounterId);
            command.Parameters.AddWithValue("referenceDate", referenceDate);
            command.Parameters.AddWithValue("updatedDate", DateTime.Now);

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

    public async Task DeleteAsync(int tenantRegistryId,
        int entityAnalysisModelId, int entityAnalysisModelTtlCounterId,
        string dataName, string dataValue,
        DateTime referenceDate)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sql = "delete from \"CacheTtlCounterEntry\" " +
                      " where \"EntityAnalysisModelId\" = (@entityAnalysisModelId) " +
                      "and \"EntityAnalysisModelTtlCounterId\" = (@entityAnalysisModelTtlCounterId) " +
                      "and \"DataName\" = (@dataName) " +
                      "and \"DataValue\" = (@dataValue) " +
                      "and \"ReferenceDate\" = (@referenceDate)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("dataName", dataName);
            command.Parameters.AddWithValue("dataValue", dataValue);
            command.Parameters.AddWithValue("entityAnalysisModelTtlCounterId", entityAnalysisModelTtlCounterId);
            command.Parameters.AddWithValue("referenceDate", referenceDate);

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

    public class ExpiredTtlCounterEntryDto
    {
        public DateTime ReferenceDate { get; set; }
        public string DataValue { get; set; }
        public int Value { get; set; }
    }
}