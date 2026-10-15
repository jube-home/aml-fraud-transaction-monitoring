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

namespace Jube.Analytics
{
    public sealed class AdvancedAnalyticsTrainingSettings
    {
        public const string PollSecondsKey = "AdvancedAnalyticsTrainingPollSeconds";
        public const string MaxConcurrentRunsKey = "AdvancedAnalyticsTrainingMaxConcurrentRuns";
        public const string MaxConcurrentRunsPerTenantKey = "AdvancedAnalyticsTrainingMaxConcurrentRunsPerTenant";
        public const string MaxRunSecondsKey = "AdvancedAnalyticsTrainingMaxRunSeconds";
        public const string HeartbeatSecondsKey = "AdvancedAnalyticsTrainingHeartbeatSeconds";
        public const string StaleSecondsKey = "AdvancedAnalyticsTrainingStaleSeconds";
        public const string InlineMaxRowsKey = "AdvancedAnalyticsTrainingInlineMaxRows";
        public const string InlineMaxBytesKey = "AdvancedAnalyticsTrainingInlineMaxBytes";
        public const string OnlineMaxRowsKey = "AdvancedAnalyticsTrainingOnlineMaxRows";
        private static volatile AdvancedAnalyticsTrainingSettings current = new();

        public static AdvancedAnalyticsTrainingSettings Current
        {
            get => current;
            set => current = value ?? new AdvancedAnalyticsTrainingSettings();
        }

        public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);
        public int MaxConcurrentRuns { get; init; } = 2;
        public int MaxConcurrentRunsPerTenant { get; init; } = 1;
        public TimeSpan MaxRunDuration { get; init; } = TimeSpan.FromMinutes(30);
        public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(10);
        public TimeSpan StaleThreshold { get; init; } = TimeSpan.FromSeconds(120);
        public TimeSpan StaleRecoveryInterval { get; init; } = TimeSpan.FromSeconds(30);
        public TimeSpan ShutdownGrace { get; init; } = TimeSpan.FromSeconds(30);
        public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(60);
        public int InlineMaxRows { get; init; } = 20000;
        public long InlineMaxBytes { get; init; } = 16L * 1024 * 1024;
        public int OnlineMaxRows { get; init; } = 2000;

        public static AdvancedAnalyticsTrainingSettings FromSettings(Func<string, string> appSettings)
        {
            return new AdvancedAnalyticsTrainingSettings
            {
                PollInterval = TimeSpan.FromSeconds(Positive(appSettings, PollSecondsKey, 5)),
                MaxConcurrentRuns = (int)Positive(appSettings, MaxConcurrentRunsKey, 2),
                MaxConcurrentRunsPerTenant = (int)Positive(appSettings, MaxConcurrentRunsPerTenantKey, 1),
                MaxRunDuration = TimeSpan.FromSeconds(Positive(appSettings, MaxRunSecondsKey, 1800)),
                HeartbeatInterval = TimeSpan.FromSeconds(Positive(appSettings, HeartbeatSecondsKey, 10)),
                StaleThreshold = TimeSpan.FromSeconds(Positive(appSettings, StaleSecondsKey, 120)),
                InlineMaxRows = (int)Positive(appSettings, InlineMaxRowsKey, 20000),
                InlineMaxBytes = Positive(appSettings, InlineMaxBytesKey, 16L * 1024 * 1024),
                OnlineMaxRows = (int)Positive(appSettings, OnlineMaxRowsKey, 2000)
            };
        }

        private static long Positive(Func<string, string> appSettings, string key, long fallback)
        {
            var configured = appSettings?.Invoke(key);

            return long.TryParse(configured, out var parsed) && parsed > 0 ? parsed : fallback;
        }
    }
}