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

namespace Jube.Parser.Dependency
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query;
    using Data.Query.Models;
    using log4net;

    public static class ModelDependencyGraphBuilder
    {
        public static async Task<ModelDependencyGraph> BuildAsync(DbContext dbContext, int tenantRegistryId,
            string userName, int entityAnalysisModelId, ILog log, CancellationToken token = default)
        {
            var inputs = await new GetModelDependencyInputsQuery(dbContext, tenantRegistryId, userName)
                .ExecuteAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            var environment = await new GetRuleParseEnvironmentQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(entityAnalysisModelId, RuleParse.ActivationRule, token).ConfigureAwait(false);

            return Build(inputs, environment, log);
        }

        public static ModelDependencyGraph Build(ModelDependencyInputsDto inputs,
            RuleParseEnvironmentDto environment, ILog log)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(environment);

            var entities = inputs.Entities
                .Select(e => new ModelEntity(Enum.Parse<ModelEntityKind>(e.Kind), e.Id, e.Name, e.Active))
                .ToList();

            var byKindAndId = entities
                .GroupBy(e => (e.Kind, e.Id))
                .ToDictionary(g => g.Key, g => g.First());

            var references = new List<ModelReference>();
            foreach (var text in inputs.RuleTexts)
            {
                if (!byKindAndId.TryGetValue((Enum.Parse<ModelEntityKind>(text.Kind), text.Id), out var dependent))
                {
                    continue;
                }

                var parsed = RuleParse.Execute(text.Text, text.RuleParseType, environment, log, null,
                    showOnlyCacheForPayload: text.ShowOnlyCacheForPayload);

                references.AddRange(parsed.References.Select(r =>
                    new ModelReference(dependent, ModelDependencyKind.RuleText, r.Namespace, r.Name, r.Line)));
            }

            foreach (var reference in inputs.References)
            {
                if (!byKindAndId.TryGetValue(
                        (Enum.Parse<ModelEntityKind>(reference.DependentKind), reference.DependentId),
                        out var dependent))
                {
                    continue;
                }

                references.Add(new ModelReference(dependent, Enum.Parse<ModelDependencyKind>(reference.Kind),
                    reference.Namespace, reference.Name));
            }

            return new ModelDependencyGraph(entities, references);
        }
    }
}