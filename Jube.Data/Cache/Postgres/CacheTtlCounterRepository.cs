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
using System.Threading.Tasks;
using Jube.Data.Cache.Interfaces;
using Jube.Data.Extension;
using log4net;
using Npgsql;

namespace Jube.Data.Cache.Postgres;

public class CacheTtlCounterRepository(string connectionString, ILog log) : ICacheTtlCounterRepository
{
    public async Task DecrementTtlCounterCacheAsync(int tenantRegistryId, int entityAnalysisModelId,
        int entityAnalysisModelTtlCounterId,
        string dataName, string dataValue, int decrement)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            connection.Open();

            var sql = "update \"CacheTtlCounter\"" +
                      " set \"Value\" = \"Value\" - (@decrement)" +
                      " where \"EntityAnalysisModelTtlCounterId\" = (@entityAnalysisModelTtlCounterId)" +
                      " and \"DataName\" = (@dataName)" +
                      " and \"DataValue\" = (@dataValue)" +
                      " and \"EntityAnalysisModelId\" = (@entityAnalysisModelId);";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("EntityAnalysisModelTtlCounterId", entityAnalysisModelTtlCounterId);
            command.Parameters.AddWithValue("dataName", dataName);
            command.Parameters.AddWithValue("dataValue", dataValue);
            command.Parameters.AddWithValue("decrement", decrement);

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
            await connection.DisposeAsync();
        }
    }

    public async Task<int> GetByNameDataNameDataValueAsync(int tenantRegistryId, int entityAnalysisModelId,
        int entityAnalysisModelTtlCounterId, string dataName, string dataValue)
    {
        var connection = new NpgsqlConnection(connectionString);
        var value = 0;
        try
        {
            await connection.OpenAsync();

            var sql = "select \"Value\" from \"CacheTtlCounter\"" +
                      " where \"EntityAnalysisModelTtlCounterId\" = (@entityAnalysisModelTtlCounterId) and \"DataName\" = (@dataName) " +
                      "and \"DataValue\" = (@dataValue) and \"EntityAnalysisModelId\" = (@entityAnalysisModelId);";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("entityAnalysisModelTtlCounterId", entityAnalysisModelTtlCounterId);
            command.Parameters.AddWithValue("dataName", dataName);
            command.Parameters.AddWithValue("dataValue", dataValue);
            await command.PrepareAsync();

            var scalarReturnValue = await command.ExecuteScalarAsync();
            if (scalarReturnValue != null) value = scalarReturnValue.AsInt();
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

    public async Task IncrementTtlCounterCacheAsync(int tenantRegistryId, int entityAnalysisModelId,
        string dataName, string dataValue, int entityAnalysisModelTtlCounterId, int increment,
        DateTime referenceDate)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sql = "insert into \"CacheTtlCounter\"(\"EntityAnalysisModelId\",\"DataName\",\"DataValue\"," +
                      "\"EntityAnalysisModelTtlCounterId\",\"Value\",\"ReferenceDate\",\"UpdatedDate\")" +
                      " values((@entityAnalysisModelId),(@dataName),(@dataValue)," +
                      "(@entityAnalysisModelTtlCounterId),1,(@referenceDate),(@updatedDate)) " +
                      " ON CONFLICT (\"EntityAnalysisModelId\",\"EntityAnalysisModelTtlCounterId\",\"DataName\",\"DataValue\") " +
                      " DO UPDATE set \"Value\" = \"CacheTtlCounter\".\"Value\" + " + increment + "";

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
}