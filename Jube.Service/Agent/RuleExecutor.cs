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

using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Dto.RuleExecution;
using Jube.Dto.Validation;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Parser;
using Jube.Service.Query.EntityAnalysisModelInvocationContext;
using log4net;

namespace Jube.Service.Agent
{
    internal static class RuleExecutor
    {
        internal const string ContextModelMismatch = "ContextModelMismatch";

        private static readonly ILog log = LogManager.GetLogger(typeof(RuleExecutor));

        internal static async Task<RuleExecutionResultDto> ExecuteAsync(DbContext dbContext, int tenantRegistryId,
            int entityAnalysisModelId, int ruleParseType, string? ruleText, string propertyName,
            bool? showOnlyCacheForPayload, bool reprocessing, InvocationContextDto? context,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.EntityAnalysisModelId != entityAnalysisModelId)
            {
                return Refused("Context", ContextModelMismatch,
                    $"The context belongs to model {context.EntityAnalysisModelId}, but the rule belongs to model " +
                    $"{entityAnalysisModelId}; build the context for the rule's model.");
            }

            var environment = await new GetRuleParseEnvironmentQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(entityAnalysisModelId, ruleParseType, token).ConfigureAwait(false);

            var parsed = RuleParse.Execute(ruleText ?? string.Empty, ruleParseType, environment, log,
                RuleParse.EngineReferences(ruleParseType), false, showOnlyCacheForPayload, true, reprocessing);

            if (!parsed.Compiled)
            {
                return new RuleExecutionResultDto
                {
                    Errors = parsed.ErrorSpans.Count == 0
                        ? [Error(propertyName, "RuleScriptInvalid", parsed.Message ?? "The rule did not compile.")]
                        : parsed.ErrorSpans.Select(s => new ValidationErrorDto
                        {
                            PropertyName = propertyName, ErrorCode = "RuleScriptInvalid", Message = s.Message,
                            Line = s.Line, Start = s.Start, Length = s.Length
                        }).ToList(),
                    NamesRead = parsed.References.Select(r => r.CompletionName).ToList()
                };
            }

            var lists = await new GetModelListsQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(entityAnalysisModelId, token).ConfigureAwait(false);
            var invocationContext = EntityAnalysisModelInvocationContextMapper.ToContext(context);
            var run = await RuleRunner.RunAsync(parsed, ruleParseType, reprocessing,
                RuleRunner.ToInputs(invocationContext, lists), token).ConfigureAwait(false);

            var namesRead = parsed.References.Select(r => r.CompletionName).ToList();
            return new RuleExecutionResultDto
            {
                Compiled = true,
                Result = run.Error == null ? InvocationContextBuilder.FormatValue(run.Value) : null,
                ResultType = run.Error == null ? run.Value?.GetType().Name : null,
                RuntimeError = run.Error == null ? null : $"{run.Error.GetType().Name}: {run.Error.Message}",
                TimedOut = run.TimedOut,
                DurationMicroseconds = run.DurationMicroseconds,
                NamesRead = namesRead,
                UnsetNamesRead = parsed.References
                    .Where(r => r.Namespace != RuleReference.List &&
                                (!invocationContext.Values.TryGetValue(r.CompletionName, out var value) ||
                                 value.Value == null))
                    .Select(r => r.CompletionName).ToList(),
                EngineBehaviour = EngineBehaviour(run, ruleParseType)
            };
        }

        private static string? EngineBehaviour(RuleRunResult run, int ruleParseType)
        {
            if (run.TimedOut)
            {
                return "The engine has no time limit for a rule, so a rule this slow would hold up every " +
                       "invocation of the model.";
            }

            if (run.Error == null)
            {
                return null;
            }

            var fallback = ruleParseType switch
            {
                RuleParse.InlineFunction => "Nothing",
                RuleParse.AbstractionCalculation => "0",
                _ => "False, so the rule is treated as not matched"
            };
            return $"The engine catches this error, logs it and returns {fallback}.";
        }

        private static RuleExecutionResultDto Refused(string propertyName, string errorCode, string message)
        {
            return new RuleExecutionResultDto { Errors = [Error(propertyName, errorCode, message)] };
        }

        private static ValidationErrorDto Error(string propertyName, string errorCode, string message)
        {
            return new ValidationErrorDto { PropertyName = propertyName, ErrorCode = errorCode, Message = message };
        }
    }
}