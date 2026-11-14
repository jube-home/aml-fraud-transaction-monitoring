/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Query.GetPostgresSchemaSnapshotQuery;
using Npgsql;

namespace Jube.Data.Reporting.Check
{
    public static class SqlSchemaSnapshotCache
    {
        private static readonly TimeSpan lifetime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan failureLifetime = TimeSpan.FromSeconds(30);
        private static readonly ConcurrentDictionary<string, (SqlSchemaSnapshot Snapshot, DateTime Expires)> entries = new();

        public static string DefaultConnectionString { get; set; }

        public static SqlSchemaSnapshot GetDefault()
        {
            return Get(DefaultConnectionString);
        }

        public static void Invalidate()
        {
            entries.Clear();
        }

        public static async Task<SqlSchemaSnapshot> GetAsync(string connectionString, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            var key = KeyOf(connectionString);

            if (entries.TryGetValue(key, out var cached) && cached.Expires > DateTime.UtcNow)
            {
                return cached.Snapshot;
            }

            try
            {
                var rows = await new GetPostgresSchemaSnapshotQuery(connectionString).ExecuteAsync(token)
                    .ConfigureAwait(false);

                var snapshot = new SqlSchemaSnapshot(rows);
                entries[key] = (snapshot, DateTime.UtcNow + lifetime);

                return snapshot;
            }
            catch (Exception e) when (e is NpgsqlException or InvalidOperationException or TimeoutException)
            {
                entries[key] = (null, DateTime.UtcNow + failureLifetime);

                return null;
            }
        }

        public static SqlSchemaSnapshot Get(string connectionString)
        {
            return GetAsync(connectionString).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static string KeyOf(string connectionString)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(connectionString)));
        }
    }
}
