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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Svirfd;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class SvirfdExtensions
    {
        public static async Task<Context> ExecuteSvirfdAsync(this Context context)
        {
            var stopwatch = Stopwatch.StartNew();
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            var looked = 0;

            try
            {
                var plan = context.Snapshot.Svirfd;
                var payload = context.EntityAnalysisModelInstanceEntryPayload;
                var lookup = context.EntityAnalysisModel.Services.Svirfd;
                var remote = lookup is { Enabled: true, Ready: true, Mode: SvirfdMode.Remote };

                foreach (var entity in plan.Entities)
                {
                    IpResponse record = null;

                    var address = payload.Payload.TryGetValue(entity.Field, out var value) ? value.AsString() : null;

                    if (lookup is { Enabled: true, Ready: true } && !string.IsNullOrWhiteSpace(address))
                    {
                        if (remote)
                        {
                            if (lookup.RemoteEngineLookup && plan.RemoteLookupsEnabled)
                            {
                                record = await lookup
                                    .LookupAsync(address, plan.RemoteLookupsEnabled,
                                        System.Threading.CancellationToken.None)
                                    .ConfigureAwait(false);
                            }
                        }
                        else if (!lookup.TryLookup(address, out record))
                        {
                            record = null;
                        }
                    }

                    if (record != null)
                    {
                        if (entity.Narrative)
                        {
                            record = record.Narrative == null ? record with { Narrative = record.Narrate() } : record;
                        }
                        else if (record.Narrative != null)
                        {
                            record = record with { Narrative = null };
                        }
                    }

                    payload.Svirfd[entity.Name] = record;

                    if (entity.ReportTable)
                    {
                        payload.SvirfdArchiveNames.Add(entity.Name);
                    }

                    if (record != null)
                    {
                        looked++;
                    }
                }

                context.TraceLog(
                    $"has looked up {looked} of {plan.Entities.Count} Svirfd entries{(lookup == null || !lookup.Enabled ? " with Svirfd disabled" : lookup.Ready ? $" in {lookup.Mode} mode{(remote && !plan.RemoteLookupsEnabled ? " with remote lookups switched off for the tenant" : string.Empty)}" : " before Svirfd was ready")}.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                context.Log.Error(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has caused an error in Svirfd as {ex}.");
            }
            finally
            {
                stopwatch.Stop();

                if (context.LogSampled)
                {
                    var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                        new InvokeStagePerformance();

                    var microseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));

                    stages.Svirfd = new StageTiming<TaskPerformance>
                    {
                        DurationMicroseconds = microseconds,
                        Items = new Dictionary<string, TaskPerformance>
                        {
                            ["Svirfd"] = new(microseconds,
                                Math.Max(GC.GetAllocatedBytesForCurrentThread() - startBytes, 0))
                        }
                    };
                }
            }

            return context;
        }
    }
}
