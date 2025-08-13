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
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Npgsql;
using Exception = System.Exception;

namespace Jube.Data.Cache.Postgres.Callback;

public class CacheCallbackRepository(
    string connectionString,
    ILog log,
    ConcurrentDictionary<Guid, Callback> concurrentDictionary = null)
{
    private static void ManageDictionary(ConcurrentDictionary<Guid, Callback> concurrentDictionary, string value)
    {
        var splits = value.Split(",", 2);

        if (splits.Length > 1)
        {
            var callback = new Callback
            {
                CreatedDate = DateTime.Now,
                Payload = splits[1]
            };

            concurrentDictionary.TryAdd(Guid.Parse(splits[0]), callback);
        }
        else
        {
            concurrentDictionary.TryRemove(Guid.Parse(splits[0]), out _);
        }
    }

    public async Task ListenForCallbacksAsync()
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            connection.Notification += (_, e)
                => ManageDictionary(concurrentDictionary, e.Payload);

            await using (var cmd = new NpgsqlCommand("LISTEN callback", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            while (true) await connection.WaitAsync();
        }
        catch (Exception ex)
        {
            log.Error($"Cache SQL: Has created an exception as {ex}.");
        }

        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    public async Task InsertAsync(byte[] json, Guid entityAnalysisModelInstanceEntryGuid)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sqlNotify =
                $"NOTIFY callback, '{entityAnalysisModelInstanceEntryGuid:N},{Encoding.UTF8.GetString(json)}'";

            var commandNotify = new NpgsqlCommand(sqlNotify);
            commandNotify.Connection = connection;
            await commandNotify.ExecuteNonQueryAsync();
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

    public async Task DeleteAsync(Guid entityAnalysisModelInstanceEntryGuid)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var sqlNotify = $"NOTIFY callback, '{entityAnalysisModelInstanceEntryGuid:N}'";

            var commandNotify = new NpgsqlCommand(sqlNotify);
            commandNotify.Connection = connection;
            await commandNotify.ExecuteNonQueryAsync();
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