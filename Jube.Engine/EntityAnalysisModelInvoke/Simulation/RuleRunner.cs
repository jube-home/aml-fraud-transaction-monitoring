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
    using Dictionary;
    using EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Reprocessing;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using HttpAdaptationProtocol;
    using log4net;
    using Parser;
    using Parser.Compiler;

    public sealed record RuleRunResult(object Value, Exception Error, bool TimedOut, long DurationMicroseconds);

    public static class RuleRunner
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RuleRunner));

        public static RuleSandbox Sandbox { get; } = new();

        public static RuleRunInputs ToInputs(InvocationContext context, Dictionary<string, List<string>> lists)
        {
            ArgumentNullException.ThrowIfNull(context);

            var inputs = new RuleRunInputs { Lists = lists ?? new Dictionary<string, List<string>>() };
            foreach (var value in context.Values.Values)
            {
                if (value.Value == null)
                {
                    continue;
                }

                var key = value.Field.Key;
                switch (value.Field.Namespace)
                {
                    case "Payload":
                        AddPayload(inputs.Data, key, value.Value);
                        break;
                    case "Dictionary":
                        inputs.Kvp[key] = Convert.ToDouble(value.Value);
                        break;
                    case "TTLCounter":
                        inputs.TtlCounter[key] = Convert.ToDouble(value.Value);
                        break;
                    case "Abstraction":
                        inputs.Abstraction[key] = Convert.ToDouble(value.Value);
                        break;
                    case "AbstractionCalculation":
                        inputs.AbstractionCalculation[key] = Convert.ToDouble(value.Value);
                        break;
                    case "Sanction":
                        inputs.Sanction[key] = Convert.ToDouble(value.Value);
                        break;
                    case "ExhaustiveAdaptation":
                        inputs.ExhaustiveAdaptation[key] = Convert.ToDouble(value.Value);
                        break;
                    case "HTTPAdaptation":
                        inputs.HttpAdaptation[key] = new Adaptation { Value = Convert.ToDouble(value.Value) };
                        break;
                    case "Activation":
                        if (value.Value is true)
                        {
                            inputs.Activation.Add(key);
                        }

                        break;
                }
            }

            return inputs;
        }

        public static async Task<RuleRunResult> RunAsync(RuleParseResult parsed, int ruleParseType, bool reprocessing,
            RuleRunInputs inputs, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(parsed);
            ArgumentNullException.ThrowIfNull(inputs);

            Func<object> invocation = ruleParseType switch
            {
                RuleParse.GatewayRule when reprocessing =>
                    Bind(await Sandbox.GetDelegateAsync<EntityAnalysisModelRuleReprocessingInstance.Match>(parsed,
                            token).ConfigureAwait(false),
                        m => m(inputs.Data, inputs.Lists, new PooledDictionary<string, DictionaryNoBoxing<string>>(),
                            log)),

                RuleParse.GatewayRule =>
                    Bind(await Sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(parsed, token)
                        .ConfigureAwait(false), m => m(inputs.Data, inputs.Lists, inputs.Kvp, log)),

                RuleParse.AbstractionRule =>
                    Bind(await Sandbox.GetDelegateAsync<EntityAnalysisModelAbstractionRule.Match>(parsed, token)
                        .ConfigureAwait(false), m => m(inputs.Data, inputs.Lists, inputs.Kvp, log)),

                RuleParse.AbstractionCalculation =>
                    Bind(await Sandbox.GetDelegateAsync<EntityAnalysisModelAbstractionCalculation.Match>(parsed, token)
                            .ConfigureAwait(false),
                        m => m(inputs.Data, inputs.TtlCounter, inputs.Abstraction, inputs.Lists,
                            inputs.AbstractionCalculation, inputs.Sanction, inputs.Kvp, log)),

                RuleParse.ActivationRule =>
                    Bind(await Sandbox.GetDelegateAsync<EntityAnalysisModelActivationRule.Match>(parsed, token)
                            .ConfigureAwait(false),
                        m => m(inputs.Data, inputs.TtlCounter, inputs.Abstraction, inputs.HttpAdaptation,
                            inputs.ExhaustiveAdaptation, inputs.Lists, inputs.AbstractionCalculation, inputs.Sanction,
                            inputs.Kvp, inputs.Activation, log)),

                RuleParse.InlineFunction =>
                    Bind(await Sandbox.GetDelegateAsync<EntityAnalysisModelInlineFunction.Match>(parsed, token)
                        .ConfigureAwait(false), m => m(inputs.Data, inputs.Lists, inputs.Kvp, log)),
                _ => throw new ArgumentOutOfRangeException(nameof(ruleParseType))
            };

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var value = await Sandbox.RunAsync(invocation, token).ConfigureAwait(false);
                return new RuleRunResult(value, null, false, Microseconds(stopwatch));
            }
            catch (TimeoutException ex)
            {
                return new RuleRunResult(null, ex, true, Microseconds(stopwatch));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new RuleRunResult(null, ex, false, Microseconds(stopwatch));
            }
        }

        private static Func<object> Bind<TDelegate>(TDelegate match, Func<TDelegate, object> call)
            where TDelegate : Delegate
        {
            return () => call(match);
        }

        private static void AddPayload(DictionaryNoBoxing<string> data, string key, object value)
        {
            switch (value)
            {
                case string text:
                    data.TryAdd(key, text);
                    break;
                case int number:
                    data.TryAdd(key, number);
                    break;
                case double number:
                    data.TryAdd(key, number);
                    break;
                case DateTime date:
                    data.TryAdd(key, date);
                    break;
                case bool flag:
                    data.TryAdd(key, flag);
                    break;
                default:
                    data.TryAdd(key, value.ToString());
                    break;
            }
        }

        private static long Microseconds(Stopwatch stopwatch)
        {
            return (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
        }
    }
}