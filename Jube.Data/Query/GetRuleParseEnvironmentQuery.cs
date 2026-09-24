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

namespace Jube.Data.Query
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Repository;
    using SyntaxTree;
    using Models;

    public class GetRuleParseEnvironmentQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<RuleParseEnvironmentDto> ExecuteAsync(int entityAnalysisModelId, int ruleParseType,
            CancellationToken token = default)
        {
            var environment = new RuleParseEnvironmentDto
            {
                Tokens = (await new RuleScriptTokenRepository(dbContext).GetAsync(token).ConfigureAwait(false))
                    .Select(s => s.Token).ToList(),
                RequestXPaths = await RequestXPathsAsync(entityAnalysisModelId, token).ConfigureAwait(false),
                InlineScriptProperties = await InlineScriptPropertiesAsync(entityAnalysisModelId, token)
                    .ConfigureAwait(false),
                InlineFunctions = await InlineFunctionsAsync(entityAnalysisModelId, token).ConfigureAwait(false),
                Lists = (await new EntityAnalysisModelListRepository(dbContext, tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList(),
                Dictionaries = (await new EntityAnalysisModelDictionaryRepository(dbContext, tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList()
            };

            if (ruleParseType > 3)
            {
                environment.TtlCounters = (await new EntityAnalysisModelTtlCounterRepository(dbContext,
                            tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                environment.AbstractionRules = (await new EntityAnalysisModelAbstractionRuleRepository(dbContext,
                            tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                        .ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                environment.Sanctions = (await new EntityAnalysisModelSanctionRepository(dbContext, tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
            }

            if (ruleParseType > 4)
            {
                environment.AbstractionCalculations = (await new EntityAnalysisModelAbstractionCalculationRepository(
                            dbContext, tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                        .ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                environment.HttpAdaptations = (await new EntityAnalysisModelHttpAdaptationRepository(dbContext,
                            tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                environment.ExhaustiveAdaptations = (await new ExhaustiveSearchInstanceRepository(dbContext,
                            tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                environment.ActivationRules = (await new EntityAnalysisModelActivationRuleRepository(dbContext,
                            tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                        .ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
            }

            return environment;
        }

        private async Task<Dictionary<string, RuleParseEnvironmentRequestXPathDto>> RequestXPathsAsync(
            int entityAnalysisModelId,
            CancellationToken token)
        {
            var values = new Dictionary<string, RuleParseEnvironmentRequestXPathDto>();
            foreach (var xPath in await new EntityAnalysisModelRequestXPathRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token)
                         .ConfigureAwait(false))
            {
                values.TryAdd(xPath.Name, new RuleParseEnvironmentRequestXPathDto
                {
                    DataTypeId = xPath.DataTypeId ?? 1,
                    DefaultValue = xPath.DefaultValue,
                    Cache = xPath.Cache == 1
                });
            }

            return values;
        }

        private async Task<Dictionary<string, int>> InlineScriptPropertiesAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            var values = new Dictionary<string, int>();
            var modelInlineScripts = await new EntityAnalysisModelInlineScriptRepository(dbContext, tenantRegistryId)
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);
            var inlineScriptRepository = new EntityAnalysisInlineScriptRepository(dbContext);

            foreach (var modelInlineScript in modelInlineScripts)
            {
                if (!modelInlineScript.EntityAnalysisInlineScriptId.HasValue)
                {
                    continue;
                }

                var inlineScript = await inlineScriptRepository
                    .GetByIdAsync(modelInlineScript.EntityAnalysisInlineScriptId.Value, token)
                    .ConfigureAwait(false);
                foreach (var publicProperty in SyntaxTreeHelpers.GetPublicProperties(inlineScript.Code,
                             inlineScript.LanguageId == 2))
                {
                    values.Add(publicProperty.Key, publicProperty.Value.DataTypeId);
                }
            }

            return values;
        }

        private async Task<Dictionary<string, int>> InlineFunctionsAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            var values = new Dictionary<string, int>();
            var functions = await new EntityAnalysisModelInlineFunctionRepository(dbContext, tenantRegistryId)
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            foreach (var function in functions)
            {
                if (function.ReturnDataTypeId != null)
                {
                    values.TryAdd(function.Name, function.ReturnDataTypeId.Value);
                }
            }

            return values;
        }
    }
}