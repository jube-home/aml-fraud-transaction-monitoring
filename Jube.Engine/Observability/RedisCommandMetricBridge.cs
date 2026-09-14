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
using System.Diagnostics;

namespace Jube.Engine.Observability
{
    public static class RedisCommandMetricBridge
    {
        private const string RedisActivitySourceName = "OpenTelemetry.Instrumentation.StackExchangeRedis";

        private static ActivityListener listener;

        public static void Start()
        {
            listener?.Dispose();

            var newListener = new ActivityListener
            {
                ShouldListenTo = source =>
                    string.Equals(source.Name, RedisActivitySourceName, StringComparison.Ordinal),
                Sample = (ref _) => ActivitySamplingResult.AllData,
                ActivityStopped = Record
            };

            ActivitySource.AddActivityListener(newListener);
            listener = newListener;
        }

        private static void Record(Activity activity)
        {
            var tags = new TagList
            {
                { "command", activity.OperationName },
                { "outcome", activity.Status == ActivityStatusCode.Error ? "error" : "ok" }
            };

            EngineDiagnostics.RedisCommandCount.Add(1, tags);
            EngineDiagnostics.RedisCommandDuration.Record(activity.Duration.TotalMilliseconds, tags);
        }
    }
}