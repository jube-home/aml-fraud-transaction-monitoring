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
using System.Runtime.CompilerServices;

namespace Jube.Engine.Observability
{
    public sealed class SamplerScope : IDisposable
    {
        private readonly Activity activity;
        private readonly string sampler;
        private readonly long startTs;
        private string outcome = "ok";

        private SamplerScope(string sampler, string instance, string callerMember, string callerFilePath,
            int callerLineNumber)
        {
            this.sampler = sampler;
            activity = EngineDiagnostics.ActivitySource.StartActivity($"InfrastructureHealthMetrics.{sampler}");
            activity?.SetTag("jube.sampler", sampler);
            activity?.SetTag("jube.instance", instance);
            EngineDiagnostics.TagCodeLocation(activity, callerMember, callerFilePath, callerLineNumber);
            startTs = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            var ms = Stopwatch.GetElapsedTime(startTs).TotalMilliseconds;

            var tags = new TagList { { "sampler", sampler }, { "outcome", outcome } };
            EngineDiagnostics.SamplerCount.Add(1, tags);
            EngineDiagnostics.SamplerDuration.Record(ms, tags);

            activity?.SetTag("jube.outcome", outcome);
            activity?.SetTag("jube.duration.ms", ms);
            activity?.Dispose();
        }

        public static SamplerScope Start(string sampler, string instance,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            return new SamplerScope(sampler, instance, callerMember, callerFilePath, callerLineNumber);
        }

        public void Skipped()
        {
            outcome = "skipped";
        }

        public void Rows(int n)
        {
            activity?.SetTag("jube.row.count", n);
        }

        public void Tag(string key, object value)
        {
            activity?.SetTag(key, value);
        }

        public void Error(Exception ex)
        {
            outcome = "error";
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }
}