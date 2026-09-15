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

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jube.Engine.Observability
{
    public static class EngineDiagnostics
    {
        public const string Name = "Jube.Engine";
        private static readonly AssemblyName asm = typeof(EngineDiagnostics).Assembly.GetName();

        private static readonly string version = asm.Version?.ToString() ?? "0.0.0";

        public static readonly ActivitySource ActivitySource = new(Name, version);

        public static readonly Meter Meter = new(Name, version);

        public static readonly Histogram<double> StageDuration =
            Meter.CreateHistogram<double>("jube.engine.stage.duration", "ms");

        public static readonly Histogram<double> ArchiverStageDuration =
            Meter.CreateHistogram<double>("jube.engine.archiver.stage.duration", "ms");

        public static readonly Counter<long> ArchiverWarnCount =
            Meter.CreateCounter<long>("jube.engine.archiver.warn.count");

        public static readonly Histogram<double> CaseCreationStageDuration =
            Meter.CreateHistogram<double>("jube.engine.casecreation.stage.duration", "ms");

        public static readonly Counter<long> CaseCreationWarnCount =
            Meter.CreateCounter<long>("jube.engine.casecreation.warn.count");

        public static readonly Histogram<double> TaskDuration =
            Meter.CreateHistogram<double>("jube.engine.task.duration", "ms");

        public static readonly Histogram<double> ResponseTimePipelineDuration =
            Meter.CreateHistogram<double>("jube.engine.responsetimepipeline.duration", "ms");

        public static readonly Counter<long> LogsWarnThresholdExceeded =
            Meter.CreateCounter<long>("jube.engine.logs.warn.count");

        public static readonly Counter<long> LogsInfoSampledCount =
            Meter.CreateCounter<long>("jube.engine.logs.info.count");

        public static readonly Counter<long> InvokeTaskFaultedCount =
            Meter.CreateCounter<long>("jube.engine.invoke.task.faulted.count");

        public static readonly Counter<long> SamplerCount =
            Meter.CreateCounter<long>("jube.engine.sampler.count", "{operation}");

        public static readonly Histogram<double> SamplerDuration =
            Meter.CreateHistogram<double>("jube.engine.sampler.duration", "ms");

        public static readonly Counter<long> RedisCommandCount =
            Meter.CreateCounter<long>("jube.redis.command.count", "{command}");

        public static readonly Histogram<double> RedisCommandDuration =
            Meter.CreateHistogram<double>("jube.redis.command.duration", "ms");

        public static void TagCodeLocation(Activity activity, string function, string filePath, int lineNumber)
        {
            activity?.SetTag("code.function", function);
            activity?.SetTag("code.filepath", filePath);
            activity?.SetTag("code.lineno", lineNumber);
        }

        public static void TagCurrentCodeLocation(Activity activity,
            [CallerMemberName] string function = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            TagCodeLocation(activity, function, filePath, lineNumber);
        }
    }
}