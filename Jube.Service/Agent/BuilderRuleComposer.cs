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
using Jube.Data.QueryBuilder;
using Jube.Dto.RuleExecution;
using Jube.Dto.Validation;
using Jube.Parser;
using log4net;

namespace Jube.Service.Agent
{
    internal static class BuilderRuleComposer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BuilderRuleComposer));

        internal static async Task<BuilderRuleResultDto> ComposeAsync(DbContext dbContext, int tenantRegistryId,
            int entityAnalysisModelId, int ruleParseType, string? json, CancellationToken token)
        {
            var completions = await new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext,
                    tenantRegistryId)
                .ExecuteAsync(entityAnalysisModelId, ruleParseType, false, token).ConfigureAwait(false);
            var fields = BuilderRuleTranslator.Fields(completions.Select(c => (c.Name, c.JQueryBuilderDataType)));

            var parsed = BuilderProfile.Parse(json ?? string.Empty, fields, BuilderTarget.RuleText);
            if (!parsed.Valid)
            {
                return new BuilderRuleResultDto
                {
                    Errors = parsed.Errors.Select(e => new ValidationErrorDto
                        { PropertyName = e.Path, ErrorCode = e.Code, Message = e.Message }).ToList()
                };
            }

            var ruleText = BuilderRuleTranslator.Translate(parsed.Group);
            var environment = await new GetRuleParseEnvironmentQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(entityAnalysisModelId, ruleParseType, token).ConfigureAwait(false);
            var compiled = RuleParse.Execute(ruleText, ruleParseType, environment, log,
                RuleParse.DefaultReferences());

            return new BuilderRuleResultDto
            {
                Valid = compiled.Compiled,
                RuleText = ruleText,
                Errors = compiled.Compiled
                    ? []
                    : compiled.ErrorSpans.Count == 0
                        ?
                        [
                            new ValidationErrorDto
                            {
                                PropertyName = "BuilderRuleScript", ErrorCode = "RuleScriptInvalid",
                                Message = compiled.Message ?? string.Empty
                            }
                        ]
                        : compiled.ErrorSpans.Select(s => new ValidationErrorDto
                        {
                            PropertyName = "BuilderRuleScript", ErrorCode = "RuleScriptInvalid", Message = s.Message,
                            Line = s.Line, Start = s.Start, Length = s.Length
                        }).ToList()
            };
        }
    }
}