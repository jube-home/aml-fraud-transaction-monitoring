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
using System.Threading;
using LinqToDB.Data;

namespace Jube.Data.Observability
{
    public static class DataDiagnostics
    {
        public const string Name = "Jube.Data";
        private static readonly AssemblyName asm = typeof(DataDiagnostics).Assembly.GetName();
        public static readonly string Version = asm.Version?.ToString() ?? "0.0.0";
        public static readonly Meter Meter = new(Name, Version);

        public static readonly Counter<long> CommandCount =
            Meter.CreateCounter<long>("jube.postgres.client.command.count", "{command}");

        public static readonly Histogram<double> CommandDuration =
            Meter.CreateHistogram<double>("jube.postgres.client.command.duration", "ms");

        private static readonly Lock startLock = new();
        private static bool started;

        public static void Start()
        {
            lock (startLock)
            {
                if (started)
                {
                    return;
                }

                started = true;
                DataConnection.TurnTraceSwitchOn();
                DataConnection.OnTrace = Record;
            }
        }

        private static void Record(TraceInfo info)
        {
            string outcome;
            switch (info.TraceInfoStep)
            {
                case TraceInfoStep.AfterExecute:
                    outcome = "ok";
                    break;
                case TraceInfoStep.Error:
                    outcome = "error";
                    break;
                case TraceInfoStep.BeforeExecute:
                case TraceInfoStep.MapperCreated:
                case TraceInfoStep.Completed:
                default:
                    return;
            }

            var tags = new TagList { { "operation", info.Operation.ToString() }, { "outcome", outcome } };
            CommandCount.Add(1, tags);

            if (info.ExecutionTime is { } elapsed)
            {
                CommandDuration.Record(elapsed.TotalMilliseconds, tags);
            }
        }
    }
}