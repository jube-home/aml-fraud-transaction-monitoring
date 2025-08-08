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
using Npgsql;

namespace Jube.Data.Cache.Postgres;

public class EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueDto
{
    public string AbstractionRuleName { get; set; }
    public string SearchKey { get; set; }
    public string SearchValue { get; set; }
}

public class CacheAbstractionRepository(string connectionString, ILog log) : ICacheAbstractionRepository
{
    public async Task DeleteAsync(int tenantRegistryId, int entityAnalysisModelId, string searchKey,
        string searchValue, string name)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            connection.Open();

            var sql = "delete \"CacheAbstraction\"" +
                      " where \"Name\" = (@name) and \"SearchKey\" = (@searchKey)" +
                      " and \"SearchValue\" = (@searchValue)" +
                      " and \"EntityAnalysisModelId\" = (@entityAnalysisModelId)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("searchKey", searchKey);
            command.Parameters.AddWithValue("searchValue", searchValue);
            command.Parameters.AddWithValue("name", name);
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

    public async Task InsertAsync(int tenantRegistryId, int entityAnalysisModelId, string searchKey,
        string searchValue, string name,
        double value)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sql = "insert into\"CacheAbstraction\"(\"EntityAnalysisModelId\",\"SearchKey\"," +
                      "\"SearchValue\",\"Name\",\"Value\",\"CreatedDate\")" +
                      " values((@entityAnalysisModelId),(@searchKey),(@searchValue)," +
                      "(@name),(@value),(@createdDate)); ";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("searchKey", searchKey);
            command.Parameters.AddWithValue("searchValue", searchValue);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("value", value);
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

    public async Task UpdateAsync(int tenantRegistryId, int entityAnalysisModelId, string searchKey,
        string searchValue, string name, double value)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sql = "update \"CacheAbstraction\" " +
                      $"set \"Value\" = {value},\"UpdatedDate\" = (@updatedDate) " +
                      "where \"Name\" = (@name) and \"SearchKey\" = (@searchKey) " +
                      "and \"SearchValue\" = (@searchValue) " +
                      "and \"EntityAnalysisModelId\" = (@entityAnalysisModelId)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("searchKey", searchKey);
            command.Parameters.AddWithValue("searchValue", searchValue);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("value", value);
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
            await connection.DisposeAsync();
        }
    }

    public async Task<double?> GetByNameSearchNameSearchValueAsync(int tenantRegistryId, int entityAnalysisModelId,
        string name,
        string searchKey, string searchValue)
    {
        var connection = new NpgsqlConnection(connectionString);
        double? value = null;
        try
        {
            await connection.OpenAsync();

            var sql = "select \"Value\" from \"CacheAbstraction\"" +
                      " where \"Name\" = (@name) and \"SearchKey\" = (@searchKey)" +
                      " and \"SearchValue\" = (@searchValue)" +
                      " and \"EntityAnalysisModelId\" = (@entityAnalysisModelId)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("searchKey", searchKey);
            command.Parameters.AddWithValue("searchValue", searchValue);
            command.Parameters.AddWithValue("name", name);
            await command.PrepareAsync();

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) value = (double)reader.GetValue(1);

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

    public async Task<Dictionary<string, double>>
        GetByNameSearchNameSearchValueReturnValueOnlyTreatingMissingAsNullByReturnZeroRecordAsync(
            int tenantRegistryId, int entityAnalysisModelId,
            List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueDto>
                entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests)
    {
        var connection = new NpgsqlConnection(connectionString);
        var value = new Dictionary<string, double>();
        try
        {
            await connection.OpenAsync();

            var sql = "select \"Value\",\"Name\" from \"CacheAbstraction\"" +
                      " where \"EntityAnalysisModelId\" = (@entityAnalysisModelId) and (";

            var command = new NpgsqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelId", entityAnalysisModelId);

            for (var i = 0; i < entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests.Count; i++)
            {
                if (i > 0) sql += " or ";

                sql +=
                    $"(\"Name\" = (@name{i}) " +
                    $"and \"SearchKey\" = (@searchKey{i}) " +
                    $"and \"SearchValue\" = (@searchValue{i}))";

                command.Parameters.AddWithValue($"searchKey{i}",
                    entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests[i].SearchKey);
                command.Parameters.AddWithValue($"searchValue{i}",
                    entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests[i].SearchValue);
                command.Parameters.AddWithValue($"name{i}",
                    entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests[i].AbstractionRuleName);

                value.Add(entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests[i]
                    .AbstractionRuleName, 0);
            }

            command.CommandText = sql + ")";

            await command.PrepareAsync();

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) value[(string)reader.GetValue(1)] = (double)reader.GetValue(0);
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