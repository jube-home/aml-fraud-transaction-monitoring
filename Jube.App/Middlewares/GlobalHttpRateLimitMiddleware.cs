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

namespace Jube.App.Middlewares
{
    using System;
    using System.Threading.Tasks;
    using Jube.Cache;
    using Microsoft.AspNetCore.Http;

    public class GlobalHttpRateLimitMiddleware(
        RequestDelegate next,
        GlobalHttpRequestSourceIpThrottle throttle,
        CacheService cacheService,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
    {
        private const string RateLimitName = "GlobalHttp";
        private static readonly TimeProvider Clock = TimeProvider.System;

        public async Task InvokeAsync(HttpContext context)
        {
            var enabled = dynamicEnvironment.AppSettings("EnableGlobalHttpRateLimit")
                .Equals("True", StringComparison.OrdinalIgnoreCase);

            if (!enabled)
            {
                await next(context);
                return;
            }

            var window = ComputeWindow(dynamicEnvironment.AppSettings("GlobalHttpRateLimitInterval"),
                dynamicEnvironment.AppSettings("GlobalHttpRateLimitIntervalValue"));
            var maxRequests = int.Parse(dynamicEnvironment.AppSettings("GlobalHttpRateLimitAttempts"));
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();

            var backplane = dynamicEnvironment.AppSettings("RateLimitBackplane")
                .Equals("True", StringComparison.OrdinalIgnoreCase);

            var exceeded = backplane
                ? await cacheService.CacheRateLimitRepository.IncrementRateLimitCacheAsync(RateLimitName,
                    remoteIp ?? string.Empty, window).ConfigureAwait(false) > maxRequests
                : throttle.TryRecordAndCheckExceeded(remoteIp, maxRequests, window, Clock);

            if (exceeded)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return;
            }

            await next(context);
        }

        private static TimeSpan ComputeWindow(string interval, string intervalValue)
        {
            var parsedValue = double.TryParse(intervalValue, out var value) ? value : 60;

            return interval switch
            {
                "s" => TimeSpan.FromSeconds(parsedValue),
                "n" => TimeSpan.FromMinutes(parsedValue),
                "h" => TimeSpan.FromHours(parsedValue),
                _ => TimeSpan.FromDays(parsedValue)
            };
        }
    }
}