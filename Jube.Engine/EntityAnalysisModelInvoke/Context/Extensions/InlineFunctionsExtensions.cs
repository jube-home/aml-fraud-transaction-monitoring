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
using Jube.Cryptography;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using EntityAnalysisModelInlineFunction =
        EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineFunction;

    public static class InlineFunctionsExtensions
    {
        public static Context ExecuteInlineFunctions(this Context context)
        {
            var stopwatch = Stopwatch.StartNew();
            var items = new Dictionary<string, TaskPerformance>();

            try
            {
                IterateAndProcess(context, items);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.TraceLog($"has experienced an error invoking inline functions as {ex}.");
            }
            finally
            {
                stopwatch.Stop();

                if (context.LogSampled)
                {
                    var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                        new InvokeStagePerformance();

                    stages.InlineFunctions = new StageTiming<TaskPerformance>
                    {
                        DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                        Items = items
                    };
                }
            }

            return context;
        }

        private static void IterateAndProcess(Context context, Dictionary<string, TaskPerformance> items)
        {
            context.TraceLog($"is going to check for inline functions.");

            foreach (var inlineFunction in context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineFunctions)
            {
                context.TraceLog($"is going to invoke inline function {inlineFunction.Id}.");

                try
                {
                    object output = null;
                    var timed = TaskHelper.MeasureTimeAndMemoryAllocated(TaskType.InlineFunction, () =>
                    {
                        output = ReflectRuleHelper.Execute(inlineFunction, context.EntityAnalysisModel,
                            context.EntityAnalysisModelInstanceEntryPayload,
                            context.EntityAnalysisModelInstanceEntryPayload.Dictionary, context.Log);
                    });
                    items[inlineFunction.Name] = new TaskPerformance(timed.ComputeTime, timed.ThreadMemory);

                    context.TraceLog(
                        $"has invoked inline function {inlineFunction.Id} and returned a value of {output}.");

                    PopulateAllValues(context, inlineFunction, output);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.TraceLog(
                        $"has invoked inline function {inlineFunction.Id} but it has created an error as {ex}.");
                }
            }
        }

        private static void PopulateAllValues(Context context, EntityAnalysisModelInlineFunction inlineFunction,
            object output)
        {
            if (inlineFunction.ReturnDataTypeId == 1 && output != null)
            {
                output = inlineFunction.EncryptionId switch
                {
                    1 => context.EntityAnalysisModel.Services.AesEncryption.Encrypt(output.ToString() ?? "",
                        IvMode.Deterministic),
                    2 => context.EntityAnalysisModel.Services.AesEncryption.Encrypt(output.ToString() ?? "",
                        IvMode.Random),
                    _ => output
                };
            }

            var writtenToPayload = PopulateCachePayloadDocumentStore(context, inlineFunction, output);

            if (inlineFunction.ReturnDataTypeId == 1 && writtenToPayload)
            {
                context.EntityAnalysisModel.ResolveDictionaryValueForField(
                    context.EntityAnalysisModelInstanceEntryPayload, context.Log, inlineFunction.Name);
            }

            if (inlineFunction.ReportTable)
            {
                PopulateArchiveKeys(context, inlineFunction, output);
            }
        }

        private static void PopulateArchiveKeys(Context context, EntityAnalysisModelInlineFunction inlineFunction,
            object output)
        {
            switch (inlineFunction.ReturnDataTypeId)
            {
                case 1:
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 3,
                        Key = inlineFunction.Name,
                        KeyValueString = output == null ? null : Convert.ToString(output),
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    context.TraceLog(
                        $"has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as string.");

                    break;
                case 2:
                    if (output != null)
                    {
                        context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                        {
                            ProcessingTypeId = 3,
                            Key = inlineFunction.Name,
                            KeyValueInteger = (int)output,
                            EntityAnalysisModelInstanceEntryGuid =
                                context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                        });

                        context.TraceLog(
                            $"has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as integer.");
                    }

                    break;
                case 3:
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 3,
                        Key = inlineFunction.Name,
                        KeyValueFloat = Convert.ToDouble(output),
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    context.TraceLog(
                        $"has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as double.");

                    break;
                case 4:
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 3,
                        Key = inlineFunction.Name,
                        KeyValueDate = DateTime.SpecifyKind(Convert.ToDateTime(output), DateTimeKind.Utc),
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    context.TraceLog(
                        $"has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as date.");

                    break;
                case 5:
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 3,
                        Key = inlineFunction.Name,
                        KeyValueBoolean = Convert.ToByte(output),
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    context.TraceLog(
                        $"has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as boolean.");

                    break;
            }
        }

        private static bool PopulateCachePayloadDocumentStore(Context context,
            EntityAnalysisModelInlineFunction inlineFunction, object output)
        {
            if (!context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(inlineFunction.Name))
            {
                switch (inlineFunction.ReturnDataTypeId)
                {
                    case 1:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(inlineFunction.Name,
                            output == null ? null : Convert.ToString(output));

                        context.TraceLog(
                            $"has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as string.");

                        break;
                    case 2:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(inlineFunction.Name,
                            Convert.ToInt32(output));

                        context.TraceLog(
                            $"has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as integer.");

                        break;
                    case 3:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(inlineFunction.Name,
                            Convert.ToDouble(output));

                        context.TraceLog(
                            $"has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as double.");

                        break;
                    case 4:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(
                            inlineFunction.Name,
                            DateTime.SpecifyKind(Convert.ToDateTime(output), DateTimeKind.Utc));

                        context.TraceLog(
                            $"has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as date.");

                        break;
                    case 5:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(inlineFunction.Name,
                            Convert.ToBoolean(output));

                        context.TraceLog(
                            $"has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as boolean.");

                        break;
                }

                return true;
            }

            context.TraceLog(
                $"has invoked inline function {inlineFunction.Id} but has not added to payload as name {inlineFunction.Name} already exists.");

            return false;
        }
    }
}