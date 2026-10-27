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

using System.Globalization;
using Jube.Cache.Observability;
using Jube.Extensions;
using Jube.ResilientRedisConnection.Interfaces;
using log4net;

namespace Jube.Cache.Redis
{
    public record LastLocation(double Latitude, double Longitude, DateTime SeenAt);

    public class CacheActivationWatcherLastLocationRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log)
    {
        public static readonly TimeSpan Retention = TimeSpan.FromDays(14);
        private const int MaximumKeyValueLength = 256;

        public Task<LastLocation> ExchangeAsync(int tenantRegistryId, string key, string keyValue, double latitude,
            double longitude, DateTime seenAt)
        {
            return CacheDiagnostics.RecordAsync("CacheActivationWatcherLastLocationRepository.ExchangeAsync",
                async () =>
                {
                    try
                    {
                        var redisKey = RedisKeyOf(tenantRegistryId, key, keyValue);

                        var previous = await resilientRedisResilientRedisDatabase.StringGetAsync(redisKey)
                            .ConfigureAwait(false);

                        await resilientRedisResilientRedisDatabase.StringSetAsync(redisKey,
                            string.Create(CultureInfo.InvariantCulture,
                                $"{latitude:R}|{longitude:R}|{seenAt.ToUnixTimeMilliSeconds()}"),
                            Retention).ConfigureAwait(false);

                        return previous.HasValue ? Parse(previous.ToString()) : null;
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Cache Redis: Has created an exception as {ex}.");
                    }

                    return null;
                });
        }

        internal static string RedisKeyOf(int tenantRegistryId, string key, string keyValue)
        {
            var value = keyValue.Length > MaximumKeyValueLength ? keyValue[..MaximumKeyValueLength] : keyValue;

            return $"ActivationWatcherLastLocation:{tenantRegistryId}:{key}:{value}";
        }

        internal static LastLocation Parse(string stored)
        {
            var parts = stored.Split('|');

            if (parts.Length != 3 ||
                !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude) ||
                !long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds))
            {
                return null;
            }

            return new LastLocation(latitude, longitude, milliseconds.FromUnixTimeMilliSeconds());
        }
    }
}
