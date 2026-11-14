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
using System.Globalization;
using Jube.ResilientHttp;
using Svirfd;

namespace Jube.Svirfd
{
    public sealed class SvirfdSettings
    {
        public bool Enabled { get; init; } = true;
        public string ApiKey { get; init; } = string.Empty;
        public SvirfdMode Mode { get; init; } = SvirfdMode.Auto;
        public string BaseUrl { get; init; } = "https://svirfd.com";
        public string Directory { get; init; } = string.Empty;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromHours(1);
        public string Site { get; init; } = "jube";
        public bool RemoteEngineLookup { get; init; } = true;
        public TimeSpan RemoteLookupTimeout { get; init; } = TimeSpan.FromSeconds(2);
        public TimeSpan RemoteCacheLifetime { get; init; } = TimeSpan.FromMinutes(60);
        public int RemoteCacheMaximumEntries { get; init; } = 100000;
        public bool WaitForReady { get; init; }
        public string CacheDirectory { get; init; } = string.Empty;
        public TimeSpan LocalRetryPause { get; init; } = TimeSpan.FromSeconds(30);
        public TimeSpan StaleAfter { get; init; } = TimeSpan.FromHours(3);
        public bool BackgroundStart { get; init; } = true;
        public TimeSpan StartupWait { get; init; } = TimeSpan.FromSeconds(60);

        public static SvirfdSettings From(Func<string, string?> appSettings)
        {
            return new SvirfdSettings
            {
                Enabled = Flag(appSettings("EnableSvirfd"), true),
                ApiKey = appSettings("SvirfdApiKey") ?? string.Empty,
                Mode = Enum.TryParse<SvirfdMode>(appSettings("SvirfdMode"), true, out var mode) ? mode : SvirfdMode.Auto,
                BaseUrl = Text(appSettings("SvirfdBaseUrl"), "https://svirfd.com"),
                Directory = appSettings("SvirfdDirectory") ?? string.Empty,
                PollInterval = Milliseconds(appSettings("SvirfdPollInterval"), TimeSpan.FromHours(1)),
                Site = Text(appSettings("SvirfdSite"), "jube"),
                RemoteEngineLookup = Flag(appSettings("EnableSvirfdRemoteEngineLookup"), true),
                RemoteLookupTimeout = Milliseconds(appSettings("SvirfdRemoteLookupTimeout"), TimeSpan.FromSeconds(2)),
                RemoteCacheLifetime = Milliseconds(appSettings("SvirfdRemoteCacheMinutes"), TimeSpan.FromMinutes(60),
                    60000),
                RemoteCacheMaximumEntries = Integer(appSettings("SvirfdRemoteCacheMaximumEntries"), 100000),
                WaitForReady = Flag(appSettings("SvirfdWaitForReady"), false),
                CacheDirectory = appSettings("SvirfdCacheDirectory") ?? string.Empty,
                LocalRetryPause = Milliseconds(appSettings("SvirfdLocalRetryPause"), TimeSpan.FromSeconds(30)),
                StaleAfter = Milliseconds(appSettings("SvirfdStaleAfter"), TimeSpan.FromHours(3)),
                StartupWait = Milliseconds(appSettings("SvirfdStartupWait"), TimeSpan.FromSeconds(60))
            };
        }

        public const string UserAgentApplication = "Jube";
        public static readonly TimeSpan MinimumRemoteLookupTimeout = TimeSpan.FromMilliseconds(50);

        public bool BaseUrlIsSecure =>
            Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.UserInfo) &&
            (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));

        public HttpResilienceOptions ToResilienceOptions()
        {
            return HttpResilienceDefaults.Shared with
            {
                Timeout = RemoteLookupTimeout < MinimumRemoteLookupTimeout
                    ? MinimumRemoteLookupTimeout
                    : RemoteLookupTimeout,
                MaxRetries = 0
            };
        }

        public SvirfdOptions ToOptions()
        {
            return new SvirfdOptions
            {
                Mode = Mode,
                BaseUrl = BaseUrl,
                ApiKey = ApiKey,
                Directory = Directory,
                PollInterval = PollInterval,
                Site = Site,
                Application = UserAgentApplication,
                CacheDirectory = CacheDirectory,
                LocalRetryPause = LocalRetryPause,
                StaleAfter = StaleAfter,
                BackgroundStart = BackgroundStart
            };
        }

        private static bool Flag(string? value, bool fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : string.Equals(value.Trim(), "True", StringComparison.OrdinalIgnoreCase);
        }

        private static string Text(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int Integer(string? value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
                ? parsed
                : fallback;
        }

        private static TimeSpan Milliseconds(string? value, TimeSpan fallback, double scale = 1)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
                   parsed > 0
                ? TimeSpan.FromMilliseconds(parsed * scale)
                : fallback;
        }
    }
}
