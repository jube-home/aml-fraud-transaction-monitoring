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

public class CacheReferenceDate(string connectionString, ILog log) : ICacheReferenceDate
{
    public async Task UpsertReferenceDate(int tenantRegistryId, int entityAnalysisModelId, DateTime referenceDate)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            connection.Open();

            var sql =
                "insert into \"CacheReferenceDate\"(\"EntityAnalysisModelId\",\"ReferenceDate\",\"UpdatedDate\")" +
                "values((@entityAnalysisModelId),(@referenceDate),(@updatedDate)) " +
                " ON CONFLICT (\"EntityAnalysisModelId\") " +
                " DO UPDATE set \"ReferenceDate\" = (@referenceDate)," +
                "\"UpdatedDate\" = (@updatedDate)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("EntityAnalysisModelTtlCounterId", referenceDate);
            command.Parameters.AddWithValue("updatedDate", DateTime.Now);
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
            await connection.DisposeAsync();
        }
    }

    public async Task<DateTime?> GetReferenceDate(int tenantRegistryId, int entityAnalysisModelId)
    {
        var connection = new NpgsqlConnection(connectionString);
        DateTime value = default;
        try
        {
            await connection.OpenAsync();

            var sql = "select \"ReferenceDate\" from \"CacheReferenceDate\"" +
                      " where \"EntityAnalysisModelId\" = (@entityAnalysisModelId); ";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);

            await command.PrepareAsync();

            var scalarReturnValue = await command.ExecuteScalarAsync();
            if (scalarReturnValue != null) value = scalarReturnValue.AsDateTime();
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
}