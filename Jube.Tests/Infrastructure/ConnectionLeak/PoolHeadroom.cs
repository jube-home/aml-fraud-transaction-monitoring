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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Npgsql;

namespace Jube.Test.Infrastructure.ConnectionLeak
{
    public static class PoolHeadroom
    {
        public const int DefaultMaximumPoolSize = 12;

        public static string CreateIsolatedConnectionString(string baseConnectionString,
            int maximumPoolSize = DefaultMaximumPoolSize)
        {
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                ApplicationName = $"jube-leak-{Guid.NewGuid():N}",
                MaxPoolSize = maximumPoolSize,
                MinPoolSize = 0,
                Timeout = 3,
                Pooling = true
            };

            return builder.ConnectionString;
        }

        public static string ApplicationNameOf(string isolatedConnectionString)
        {
            return new NpgsqlConnectionStringBuilder(isolatedConnectionString).ApplicationName;
        }

        public static async Task AssertEveryConnectionIsBackInThePoolAsync(string isolatedConnectionString,
            string scenario)
        {
            var size = new NpgsqlConnectionStringBuilder(isolatedConnectionString).MaxPoolSize;
            var held = new List<NpgsqlConnection>();
            var failures = new List<string>();

            try
            {
                await Task.WhenAll(Enumerable.Range(0, size).Select(_ => RentAsync())).ConfigureAwait(false);
            }
            finally
            {
                foreach (var connection in held)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }

            failures.Should().BeEmpty(
                $"{scenario} must leave every one of the {size} pooled connections returned to the pool, "
                + "but renting them all at once failed, so at least one connection was rented and never returned");

            return;

            async Task RentAsync()
            {
                var connection = new NpgsqlConnection(isolatedConnectionString);

                try
                {
                    await connection.OpenAsync().ConfigureAwait(false);

                    lock (held)
                    {
                        held.Add(connection);
                    }
                }
                catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);

                    lock (failures)
                    {
                        failures.Add(ex.Message);
                    }
                }
            }
        }

        public static async Task<int> CountServerConnectionsAsync(string observerConnectionString,
            string isolatedConnectionString)
        {
            await using var connection = new NpgsqlConnection(observerConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM pg_stat_activity WHERE application_name = @name AND pid <> pg_backend_pid()",
                connection);

            command.Parameters.AddWithValue("name", ApplicationNameOf(isolatedConnectionString));

            return Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false));
        }

        public static void ClearPool(string isolatedConnectionString)
        {
            using var connection = new NpgsqlConnection(isolatedConnectionString);

            NpgsqlConnection.ClearPool(connection);
        }
    }
}
