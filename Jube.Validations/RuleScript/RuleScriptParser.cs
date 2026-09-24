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

using FluentValidation;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Data.Query.Models;
using Jube.Parser;
using log4net;

namespace Jube.Validations.RuleScript
{
    public sealed class RuleScriptParser(DbContext dbContext, int tenantRegistryId)
    {
        public const string RuleSetName = "RuleScript";
        public const string SaveRuleSets = "default," + RuleSetName;
        public const string ErrorCode = "RuleScriptInvalid";

        private static readonly ILog log = LogManager.GetLogger(typeof(RuleScriptParser));

        private readonly Dictionary<(int, int), RuleParseEnvironmentDto> environments = new();

        public async Task ValidateAsync<T>(ValidationContext<T> context, int entityAnalysisModelId,
            int ruleParseType, string? ruleText, string propertyName, bool? showOnlyCacheForPayload,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (entityAnalysisModelId <= 0 || string.IsNullOrWhiteSpace(ruleText))
            {
                return;
            }

            var result = await ParseAsync(entityAnalysisModelId, ruleParseType, ruleText, showOnlyCacheForPayload,
                token).ConfigureAwait(false);

            if (result.Compiled)
            {
                return;
            }

            if (result.ErrorSpans.Count == 0)
            {
                context.AddFailure(new ValidationFailure(propertyName, result.Message)
                {
                    ErrorCode = ErrorCode
                });
                return;
            }

            foreach (var span in result.ErrorSpans)
            {
                context.AddFailure(new ValidationFailure(propertyName, span.Message)
                {
                    ErrorCode = ErrorCode,
                    CustomState = span
                });
            }
        }

        public async Task<RuleParseResult> ParseAsync(int entityAnalysisModelId, int ruleParseType, string ruleText,
            bool? showOnlyCacheForPayload, CancellationToken token)
        {
            if (!environments.TryGetValue((entityAnalysisModelId, ruleParseType), out var environment))
            {
                environment = await new GetRuleParseEnvironmentQuery(dbContext, tenantRegistryId)
                    .ExecuteAsync(entityAnalysisModelId, ruleParseType, token).ConfigureAwait(false);
                environments[(entityAnalysisModelId, ruleParseType)] = environment;
            }

            return RuleParse.Execute(ruleText, ruleParseType, environment, log, RuleParse.DefaultReferences(),
                showOnlyCacheForPayload: showOnlyCacheForPayload);
        }
    }
}