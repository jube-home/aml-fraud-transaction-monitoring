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

namespace Jube.Analytics.Datasets
{
    public sealed class AdvancedAnalyticsCacheLimits
    {
        public const string TtlSecondsKey = "AdvancedAnalyticsCacheTtlSeconds";
        public const string MaxEntryBytesKey = "AdvancedAnalyticsCacheMaxEntryBytes";
        public const string MaxTenantBytesKey = "AdvancedAnalyticsCacheMaxTenantBytes";
        public const int DefaultTtlSeconds = 3600;
        public const long DefaultMaxEntryBytes = 64L * 1024 * 1024;
        public const long DefaultMaxTenantBytes = 1024L * 1024 * 1024;
        public TimeSpan Ttl { get; init; } = TimeSpan.FromSeconds(DefaultTtlSeconds);
        public long MaxEntryBytes { get; init; } = DefaultMaxEntryBytes;
        public long MaxTenantBytes { get; init; } = DefaultMaxTenantBytes;

        public static AdvancedAnalyticsCacheLimits FromSettings(Func<string, string?> appSettings)
        {
            return new AdvancedAnalyticsCacheLimits
            {
                Ttl = TimeSpan.FromSeconds(ParsePositive(appSettings, TtlSecondsKey, DefaultTtlSeconds)),
                MaxEntryBytes = ParsePositive(appSettings, MaxEntryBytesKey, DefaultMaxEntryBytes),
                MaxTenantBytes = ParsePositive(appSettings, MaxTenantBytesKey, DefaultMaxTenantBytes)
            };
        }

        private static long ParsePositive(Func<string, string?> appSettings, string key, long fallback)
        {
            var configured = appSettings(key);

            return long.TryParse(configured, out var parsed) && parsed > 0 ? parsed : fallback;
        }
    }
}