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
using Jube.Data.Cache.Dto;
using Jube.Data.Cache.Interfaces;
using log4net;
using Npgsql;

namespace Jube.Data.Cache.Postgres;

public class CacheSanctionRepository(string connectionString, ILog log) : ICacheSanctionRepository
{
    public async Task<CacheSanctionDto> GetByMultiPartStringDistanceThresholdAsync(int tenantRegistryId,
        int entityAnalysisModelId, string multiPartString,
        int distanceThreshold)
    {
        var connection = new NpgsqlConnection(connectionString);
        CacheSanctionDto value = null;
        try
        {
            await connection.OpenAsync();

            var sql = "select \"Id\",\"Value\",\"CreatedDate\" from\"CacheSanction\"" +
                      " where \"MultiPartString\" = (@multiPartString)" +
                      " and \"DistanceThreshold\" = (@distanceThreshold)" +
                      " and \"EntityAnalysisModelId\" = (@entityAnalysisModelId)" +
                      " order by \"CreatedDate\" desc" +
                      " limit 1";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("multiPartString", multiPartString);
            command.Parameters.AddWithValue("distanceThreshold", distanceThreshold);
            await command.PrepareAsync();

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                value = new CacheSanctionDto
                {
                    CreatedDate = Convert.ToDateTime(reader.GetValue(2))
                };

                if (!reader.IsDBNull(1)) value.Value = (double)reader.GetValue(1);
            }

            await reader.CloseAsync();
            await reader.DisposeAsync();
            await command.DisposeAsync();

            return value;
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

    public async Task InsertAsync(int tenantRegistryId, int entityAnalysisModelId, string multiPartString,
        int distanceThreshold, double? value)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sql = "insert into \"CacheSanction\"(" +
                      "\"Value\",\"MultiPartString\",\"DistanceThreshold\",\"CreatedDate\"," +
                      "\"EntityAnalysisModelId\")" +
                      " values((@value),(@multiPartString),(@distanceThreshold),(@createdDate)," +
                      "(@entityAnalysisModelId))";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("value", value.HasValue ? value : DBNull.Value);
            command.Parameters.AddWithValue("multiPartString", multiPartString);
            command.Parameters.AddWithValue("distanceThreshold", distanceThreshold);
            command.Parameters.AddWithValue("createdDate", DateTime.Now);
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);

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

    public async Task UpdateAsync(int tenantRegistryId, int entityAnalysisModelId, string multiPartString,
        int distanceThreshold, double? value)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sql = "update \"CacheSanction\"" +
                      " set \"Value\" = (@value), " +
                      " \"CreatedDate\" = (@createdDate) " +
                      " where \"EntityAnalysisModelId\" = (@entityAnalysisModelId) and " +
                      "\"MultiPartString\" = (@multiPartString) and " +
                      "\"DistanceThreshold\" = (@distanceThreshold)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("multiPartString", multiPartString);
            command.Parameters.AddWithValue("distanceThreshold", distanceThreshold);
            command.Parameters.AddWithValue("value", value.HasValue ? value : DBNull.Value);
            command.Parameters.AddWithValue("createdDate", DateTime.Now);

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
}