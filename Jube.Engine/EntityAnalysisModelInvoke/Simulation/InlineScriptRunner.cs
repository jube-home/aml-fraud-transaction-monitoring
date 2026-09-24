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

namespace Jube.Engine.EntityAnalysisModelInvoke.Simulation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Poco;
    using EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
    using EngineInlineScript =
        EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript.
        EntityAnalysisModelInlineScript;
    using log4net;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;

    public sealed record InlineScriptRunResult(
        bool Compiled,
        string CompileErrors,
        bool Succeeded,
        IReadOnlyDictionary<string, object> Properties,
        Exception Error,
        bool TimedOut,
        long DurationMicroseconds);

    public static class InlineScriptRunner
    {
        private static readonly Type[] supportedTypes =
            [typeof(string), typeof(int), typeof(bool), typeof(DateTime), typeof(double)];

        public static async Task<InlineScriptRunResult> RunAsync(string code, byte languageId, string dependencies,
            string binaryPath, string frameworkPath, RuleRunInputs inputs, DateTime? referenceDate,
            TimeSpan timeout, ILog log, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(inputs);

            var script = new EngineInlineScript
            {
                InlineScriptCode = code,
                LanguageId = languageId,
                Dependencies = dependencies
            };

            var compile = script.CompileAndConfigure(
                InlineScriptCompilationExtensions.BuildDependencyArray(binaryPath, frameworkPath, dependencies), log);

            if (compile.Errors != null)
            {
                return new InlineScriptRunResult(false, compile.ErrorsSummary, false, Empty(), null, false, 0);
            }

            if (script.InlineScriptType == null || script.ActivatorDelegate == null ||
                script.ExecuteAsyncDelegate == null)
            {
                return new InlineScriptRunResult(false, "No public class implements IInlineScript.", false, Empty(),
                    null, false, 0);
            }

            var context = new Context.Context
            {
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = inputs.Data,
                    ReferenceDate = referenceDate ?? DateTime.UtcNow,
                    Dictionary = inputs.Kvp,
                    TtlCounter = inputs.TtlCounter,
                    Abstraction = inputs.Abstraction,
                    AbstractionCalculation = inputs.AbstractionCalculation,
                    Sanction = inputs.Sanction,
                    ExhaustiveAdaptation = inputs.ExhaustiveAdaptation,
                    HttpAdaptation = inputs.HttpAdaptation,
                    ArchiveKeys = new List<ArchiveKey>()
                },
                Log = log,
                Stopwatch = Stopwatch.StartNew(),
                Random = new Random()
            };

            var stopwatch = Stopwatch.StartNew();
            object instance;
            bool succeeded;
            try
            {
                instance = script.ActivatorDelegate();
                succeeded = await Task.Run(() => script.ExecuteAsyncDelegate(instance, context), token)
                    .WaitAsync(timeout, token).ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                return new InlineScriptRunResult(true, null, false, Empty(), ex, true, Microseconds(stopwatch));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new InlineScriptRunResult(true, null, false, Empty(), ex, false, Microseconds(stopwatch));
            }

            var properties = new Dictionary<string, object>(StringComparer.Ordinal);
            if (succeeded)
            {
                foreach (var (name, property) in script.EntityAnalysisModelInlineScriptPropertyAttributes)
                {
                    var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    if (!supportedTypes.Contains(type))
                    {
                        continue;
                    }

                    var value = property.GetValueDelegate(instance);
                    if (value != null)
                    {
                        properties[name] = value;
                    }
                }
            }

            return new InlineScriptRunResult(true, null, succeeded, properties, null, false, Microseconds(stopwatch));
        }

        private static IReadOnlyDictionary<string, object> Empty()
        {
            return new Dictionary<string, object>();
        }

        private static long Microseconds(Stopwatch stopwatch)
        {
            return (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
        }
    }
}