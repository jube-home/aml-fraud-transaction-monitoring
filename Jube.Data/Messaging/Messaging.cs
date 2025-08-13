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
using System.Text;
using log4net;
using Npgsql;

namespace Jube.Data.Messaging;

public class Messaging
{
    private readonly string _connectionString;
    private readonly ILog _log;

    public Messaging(string connectionString, ILog log)
    {
        _connectionString = connectionString;
        _log = log;
    }

    public void SendActivation(byte[] json)
    {
        var connection = new NpgsqlConnection(_connectionString);
        try
        {
            connection.Open();

            var sqlNotify = $"NOTIFY activation, '{Encoding.UTF8.GetString(json)}'";

            var commandNotify = new NpgsqlCommand(sqlNotify);
            commandNotify.Connection = connection;
            commandNotify.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _log.Error($"Cache Activation Watcher: Has created an exception as {ex}.");
        }
        finally
        {
            connection.Close();
        }
    }
}