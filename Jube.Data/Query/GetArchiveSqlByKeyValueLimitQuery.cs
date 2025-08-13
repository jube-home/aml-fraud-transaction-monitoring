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
using Npgsql;

namespace Jube.Data.Query;

public class GetArchiveSqlByKeyValueLimitQuery(string connectionString, ILog log)
{
    public async Task<List<Dictionary<string, object>>> Execute(string sql,
        string key, string value, string order, int limit)
    {
        var connection = new NpgsqlConnection(connectionString);
        var values = new List<Dictionary<string, object>>();
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

                values.Add(document);
            }

            await reader.CloseAsync();
            await reader.DisposeAsync();
            await command.DisposeAsync();
        }
        catch (Exception ex)
        {
            log.Error($"Archive SQL: Has created an exception as {ex}.");
        }
        finally
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }

        return values;
    }
}