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
using Npgsql;

namespace Jube.Service.Query.CaseNarrative
{
    public sealed class CaseNarrativeAdvisoryLock : IAsyncDisposable
    {
        private readonly NpgsqlConnection connection;
        private readonly string key;

        private CaseNarrativeAdvisoryLock(NpgsqlConnection connection, string key)
        {
            this.connection = connection;
            this.key = key;
        }

        public static async Task<CaseNarrativeAdvisoryLock?> TryAcquireAsync(string connectionString, string key,
            CancellationToken token = default)
        {
            var connection = new NpgsqlConnection(connectionString);

            try
            {
                await connection.OpenAsync(token).ConfigureAwait(false);

                await using var attempt = new NpgsqlCommand("select pg_try_advisory_lock(hashtext(@key))", connection);
                attempt.Parameters.AddWithValue("key", key);

                if (await attempt.ExecuteScalarAsync(token).ConfigureAwait(false) is true)
                {
                    return new CaseNarrativeAdvisoryLock(connection, key);
                }
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);

                throw;
            }

            await connection.DisposeAsync().ConfigureAwait(false);

            return null;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var release = new NpgsqlCommand("select pg_advisory_unlock(hashtext(@key))", connection);
                release.Parameters.AddWithValue("key", key);
                await release.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            finally
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
