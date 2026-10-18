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

using Jube.Service.RedisKeyActivity.Models;
using StackExchange.Redis;

namespace Jube.Service.RedisKeyActivity
{
    public sealed class RedisKeyActivityRepository(string connectionString) : IDisposable
    {
        private const int MaxSampledKeys = 100;
        private const int ScanPageSize = 50;
        private ConnectionMultiplexer? connection;

        public void Dispose()
        {
            connection?.Dispose();
        }

        public async Task<List<RedisKeyActivityRow>> SampleAsync(string? pattern, CancellationToken token = default)
        {
            connection ??= await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);

            var endpoint = connection.GetEndPoints().First();
            var server = connection.GetServer(endpoint);
            var database = connection.GetDatabase();

            var results = new List<RedisKeyActivityRow>();
            var effectivePattern = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;

            await foreach (var key in server.KeysAsync(pattern: (RedisValue)effectivePattern,
                               pageSize: ScanPageSize).WithCancellation(token).ConfigureAwait(false))
            {
                if (results.Count >= MaxSampledKeys)
                {
                    break;
                }

                var type = (await database.KeyTypeAsync(key).ConfigureAwait(false)).ToString();
                var ttl = await database.KeyTimeToLiveAsync(key).ConfigureAwait(false);

                long? idleTime = null;

                try
                {
                    var idleResult = await database.ExecuteAsync("OBJECT", "IDLETIME", key).ConfigureAwait(false);
                    idleTime = (long?)idleResult;
                }
                catch (RedisServerException)
                {
                }

                long? frequency = null;

                try
                {
                    var freqResult = await database.ExecuteAsync("OBJECT", "FREQ", key).ConfigureAwait(false);
                    frequency = (long?)freqResult;
                }
                catch (RedisServerException)
                {
                }

                results.Add(new RedisKeyActivityRow(
                    key.ToString(), type, idleTime, frequency,
                    ttl.HasValue ? (long)ttl.Value.TotalSeconds : null));
            }

            return results;
        }
    }
}