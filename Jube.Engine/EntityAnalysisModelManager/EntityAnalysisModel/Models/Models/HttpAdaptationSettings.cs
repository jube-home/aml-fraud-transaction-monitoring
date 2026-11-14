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
using Jube.Case.Outbound;
using Jube.Data.Poco;
using Jube.ResilientHttp;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models
{
    public sealed record HttpAdaptationSettings
    {
        public const int DefaultTimeoutMilliseconds = 1000;

        public string BaseUrl { get; init; }
        public string Refusal { get; init; }
        public bool IsTenantUrl { get; init; }
        public bool ValidateSsl { get; init; }
        public bool AllowInsecureTargets { get; init; }
        public int TimeoutMilliseconds { get; init; }
        public HttpResilienceOptions Resilience { get; init; }

        public static HttpAdaptationSettings Resolve(DynamicEnvironment.DynamicEnvironment environment,
            TenantRegistry tenant)
        {
            var globalUrl = environment.AppSettings("HttpAdaptationUrl");
            var globalTimeout = ParseTimeout(environment.AppSettings("HttpAdaptationTimeout"));
            var globalValidateSsl = ParseBoolean(environment.AppSettings("HttpAdaptationValidateSsl"), true);
            var allowInsecureFlag = ParseBoolean(environment.AppSettings(TenantBaseUrlPolicy.AllowInsecureTargetsKey),
                false);

            if (!string.IsNullOrWhiteSpace(tenant?.HttpAdaptationUrl))
            {
                var tenantTimeout = tenant.HttpAdaptationTimeoutMilliseconds is > 0
                    ? tenant.HttpAdaptationTimeoutMilliseconds.Value
                    : globalTimeout;

                return new HttpAdaptationSettings
                {
                    BaseUrl = tenant.HttpAdaptationUrl.Trim(),
                    Refusal = TenantBaseUrlPolicy.Check(tenant.HttpAdaptationUrl, allowInsecureFlag),
                    IsTenantUrl = true,
                    ValidateSsl = tenant.HttpAdaptationValidateSsl != 0,
                    AllowInsecureTargets = allowInsecureFlag,
                    TimeoutMilliseconds = tenantTimeout,
                    Resilience = ResilienceFrom(environment, tenantTimeout)
                };
            }

            return new HttpAdaptationSettings
            {
                BaseUrl = globalUrl,
                Refusal = TenantBaseUrlPolicy.Check(globalUrl, true),
                IsTenantUrl = false,
                ValidateSsl = globalValidateSsl,
                AllowInsecureTargets = true,
                TimeoutMilliseconds = globalTimeout,
                Resilience = ResilienceFrom(environment, globalTimeout)
            };
        }

        public static HttpResilienceOptions ResilienceFrom(DynamicEnvironment.DynamicEnvironment environment,
            int timeoutMilliseconds)
        {
            var ratio = ParseDouble(environment.AppSettings("ResilientHttpFailureRatio"), 0.5);

            return new HttpResilienceOptions
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds),
                FailureRatio = ratio is > 0 and <= 1 ? ratio : 0.5,
                MinimumThroughput = Math.Max(2,
                    ParseInt(environment.AppSettings("ResilientHttpMinimumThroughput"), 5)),
                SamplingDuration = TimeSpan.FromSeconds(Math.Max(1,
                    ParseInt(environment.AppSettings("ResilientHttpSamplingDurationSeconds"), 30))),
                BreakDuration = TimeSpan.FromSeconds(Math.Max(1,
                    ParseInt(environment.AppSettings("ResilientHttpBreakDurationSeconds"), 30))),
                MaxRetries = Math.Max(0, ParseInt(environment.AppSettings("ResilientHttpMaxRetries"), 0)),
                RetryDelay = TimeSpan.FromMilliseconds(Math.Max(1,
                    ParseInt(environment.AppSettings("ResilientHttpRetryDelayMilliseconds"), 100)))
            };
        }

        public static int ParseTimeout(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
                   parsed > 0
                ? parsed
                : DefaultTimeoutMilliseconds;
        }

        private static bool ParseBoolean(string value, bool fallback)
        {
            return bool.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static double ParseDouble(string value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }
    }
}
