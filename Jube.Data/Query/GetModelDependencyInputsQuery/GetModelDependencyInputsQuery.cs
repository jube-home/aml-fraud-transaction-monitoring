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

namespace Jube.Data.Query.GetModelDependencyInputsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Poco;
    using Repository;
    using SyntaxTree;
    using Models;

    public class GetModelDependencyInputsQuery(DbContext dbContext, int tenantRegistryId, string userName)
    {
        private static readonly Dictionary<string, EntityApprovalKind> approvalKindOf = new()
        {
            ["RequestXPath"] = EntityApprovalKind.EntityAnalysisModelRequestXPath,
            ["InlineFunction"] = EntityApprovalKind.EntityAnalysisModelInlineFunction,
            ["GatewayRule"] = EntityApprovalKind.EntityAnalysisModelGatewayRule,
            ["AbstractionRule"] = EntityApprovalKind.EntityAnalysisModelAbstractionRule,
            ["AbstractionCalculation"] = EntityApprovalKind.EntityAnalysisModelAbstractionCalculation,
            ["TtlCounter"] = EntityApprovalKind.EntityAnalysisModelTtlCounter,
            ["ActivationRule"] = EntityApprovalKind.EntityAnalysisModelActivationRule,
            ["Sanction"] = EntityApprovalKind.EntityAnalysisModelSanction,
            ["List"] = EntityApprovalKind.EntityAnalysisModelList,
            ["Dictionary"] = EntityApprovalKind.EntityAnalysisModelDictionary,
            ["HttpAdaptation"] = EntityApprovalKind.EntityAnalysisModelHttpAdaptation,
            ["ExhaustiveAdaptation"] = EntityApprovalKind.ExhaustiveSearchInstance
        };

        public async Task<ModelDependencyInputsDto> ExecuteAsync(int entityAnalysisModelId,
            CancellationToken token = default, int approvalsRequired = ApprovalsRequiredResolver.Default)
        {
            var dto = new ModelDependencyInputsDto();

            foreach (var x in await new EntityAnalysisModelRequestXpathRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("RequestXPath", x.Id, x.Name, x.Active, x.Version);
            }

            await AddInlineScriptPropertiesAsync(dto, entityAnalysisModelId, approvalsRequired, token)
                .ConfigureAwait(false);

            foreach (var f in await new EntityAnalysisModelInlineFunctionRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("InlineFunction", f.Id, f.Name, f.Active, f.Version);
                dto.Text("InlineFunction", f.Id, 1, f.FunctionScript, null);
            }

            foreach (var g in await new EntityAnalysisModelGatewayRuleRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("GatewayRule", g.Id, g.Name, g.Active, g.Version);
                dto.Text("GatewayRule", g.Id, 2,
                    g.RuleScriptTypeId == 1 ? g.BuilderRuleScript : g.CoderRuleScript, null);
            }

            foreach (var a in await new EntityAnalysisModelAbstractionRuleRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                         .ConfigureAwait(false))
            {
                dto.Add("AbstractionRule", a.Id, a.Name, a.Active, a.Version);
                dto.Text("AbstractionRule", a.Id, 3, a.RuleScriptTypeId == 1 ? a.BuilderRuleScript : a.CoderRuleScript,
                    a.RuleScriptTypeId == 1);

                if (a.Search != 1)
                {
                    continue;
                }

                dto.Reference("AbstractionRule", a.Id, "SearchKey", "Payload", a.SearchKey);
                if (a.SearchFunctionTypeId != 1)
                {
                    dto.Reference("AbstractionRule", a.Id, "SearchFunctionKey", "Payload", a.SearchFunctionKey);
                }
            }

            foreach (var c in await new EntityAnalysisModelAbstractionCalculationRepository(dbContext,
                             tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                         .ConfigureAwait(false))
            {
                dto.Add("AbstractionCalculation", c.Id, c.Name, c.Active, c.Version);
                dto.Text("AbstractionCalculation", c.Id, 4, c.FunctionScript, null);
            }

            var ttlCounters = (await new EntityAnalysisModelTtlCounterRepository(dbContext, tenantRegistryId)
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false)).ToList();
            foreach (var t in ttlCounters)
            {
                dto.Add("TtlCounter", t.Id, t.Name, t.Active, t.Version);
                dto.Reference("TtlCounter", t.Id, "TtlCounterDataName", "Payload", t.TtlCounterDataName);
            }

            var caseWorkflows = (await new CaseWorkflowRepository(dbContext, tenantRegistryId)
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false)).ToList();
            foreach (var w in caseWorkflows)
            {
                dto.Add("CaseWorkflow", w.Id, w.Name, w.Active);
            }

            foreach (var r in await new EntityAnalysisModelActivationRuleRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                         .ConfigureAwait(false))
            {
                dto.Add("ActivationRule", r.Id, r.Name, r.Active, r.Version);
                dto.Text("ActivationRule", r.Id, 5, r.RuleScriptTypeId == 1 ? r.BuilderRuleScript : r.CoderRuleScript,
                    null);
                dto.Reference("ActivationRule", r.Id, "ResponseElevationKey", "Payload", r.ResponseElevationKey);

                if (r.EnableTtlCounter == 1 && r.EntityAnalysisModelTtlCounterGuid != Guid.Empty)
                {
                    var counter = ttlCounters.FirstOrDefault(t => t.Guid == r.EntityAnalysisModelTtlCounterGuid);
                    if (counter != null)
                    {
                        dto.Reference("ActivationRule", r.Id, "TtlCounterIncrement", "TTLCounter", counter.Name);
                    }
                    else if (r.EntityAnalysisModelGuidTtlCounter == Guid.Empty)
                    {
                        dto.Reference("ActivationRule", r.Id, "TtlCounterIncrement", "TTLCounter",
                            r.EntityAnalysisModelTtlCounterGuid.ToString());
                    }
                }

                if (r.EnableCaseWorkflow != 1)
                {
                    continue;
                }

                dto.Reference("ActivationRule", r.Id, "CaseKey", "Payload", r.CaseKey);
                if (r.CaseWorkflowGuid != Guid.Empty)
                {
                    dto.Reference("ActivationRule", r.Id, "CaseWorkflow", "CaseWorkflow",
                        caseWorkflows.FirstOrDefault(w => w.Guid == r.CaseWorkflowGuid)?.Name ??
                        r.CaseWorkflowGuid.ToString());
                }
            }

            foreach (var s in await new EntityAnalysisModelSanctionRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("Sanction", s.Id, s.Name, s.Active, s.Version);
                dto.Reference("Sanction", s.Id, "SanctionDataName", "Payload", s.MultipartStringDataName);
            }

            foreach (var l in await new EntityAnalysisModelListRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("List", l.Id, l.Name, l.Active, l.Version);
            }

            foreach (var d in await new EntityAnalysisModelDictionaryRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("Dictionary", d.Id, d.Name, d.Active, d.Version);
                dto.Reference("Dictionary", d.Id, "DictionaryDataName", "Payload", d.DataName);
            }

            foreach (var h in await new EntityAnalysisModelHttpAdaptationRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("HttpAdaptation", h.Id, h.Name, h.Active, h.Version);
            }

            foreach (var e in await new ExhaustiveSearchInstanceRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("ExhaustiveAdaptation", e.Id, e.Name, e.Active, e.Version);
            }

            foreach (var p in await new EntityAnalysisModelReprocessingRuleRepository(dbContext, userName)
                         .GetByEntityAnalysisModelIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.Add("ReprocessingRule", p.Id, p.Name, p.Active);
                dto.Text("ReprocessingRule", p.Id, 2, p.RuleScriptTypeId == 1 ? p.BuilderRuleScript : p.CoderRuleScript,
                    null);
            }

            await ApplyApprovalsAsync(dto, approvalsRequired, token).ConfigureAwait(false);

            return dto;
        }

        private async Task ApplyApprovalsAsync(ModelDependencyInputsDto dto, int approvalsRequired,
            CancellationToken token)
        {
            var repository = new EntityApprovalRepository(dbContext, tenantRegistryId);

            foreach (var group in dto.Entities
                         .Where(e => e.Version.HasValue && approvalKindOf.ContainsKey(e.Kind))
                         .GroupBy(e => e.Kind))
            {
                var kind = approvalKindOf[group.Key];
                var current = group.ToDictionary(e => e.Id, e => e.Version.Value);

                var rows = await repository
                    .GetRowsByKindAndEntityIdsAsync(kind, current.Keys.ToList(), token).ConfigureAwait(false);

                var effective = EntityApprovalResolver.EffectiveVersionsByEntity(rows, current, approvalsRequired);

                foreach (var entity in group)
                {
                    entity.Approved = effective.TryGetValue(entity.Id, out var version)
                                      && version == entity.Version.Value;
                }
            }
        }

        private async Task AddInlineScriptPropertiesAsync(ModelDependencyInputsDto dto, int entityAnalysisModelId,
            int approvalsRequired, CancellationToken token)
        {
            var inlineScriptRepository = new EntityAnalysisInlineScriptRepository(dbContext);
            var wirings = new List<(EntityAnalysisModelInlineScript Wiring, EntityAnalysisInlineScript Script)>();

            foreach (var modelInlineScript in await new EntityAnalysisModelInlineScriptRepository(dbContext,
                             tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                if (!modelInlineScript.EntityAnalysisInlineScriptId.HasValue)
                {
                    continue;
                }

                var inlineScript = await inlineScriptRepository
                    .GetByIdAsync(modelInlineScript.EntityAnalysisInlineScriptId.Value, token).ConfigureAwait(false);
                if (inlineScript == null)
                {
                    continue;
                }

                wirings.Add((modelInlineScript, inlineScript));
            }

            if (wirings.Count == 0)
            {
                return;
            }

            var current = wirings.ToDictionary(w => w.Wiring.Id, w => w.Wiring.Version ?? 1);
            var rows = await new EntityApprovalRepository(dbContext, tenantRegistryId)
                .GetRowsByKindAndEntityIdsAsync(EntityApprovalKind.EntityAnalysisModelInlineScript,
                    current.Keys.ToList(), token).ConfigureAwait(false);
            var effective = EntityApprovalResolver.EffectiveVersionsByEntity(rows, current, approvalsRequired);

            foreach (var (wiring, script) in wirings)
            {
                var approved = effective.TryGetValue(wiring.Id, out var version)
                               && version == current[wiring.Id];

                foreach (var property in SyntaxTreeHelpers.GetPublicProperties(script.Code, script.LanguageId == 2))
                {
                    var before = dto.Entities.Count;
                    dto.Add("InlineScriptProperty", wiring.Id, property.Key, wiring.Active, current[wiring.Id]);
                    if (dto.Entities.Count > before)
                    {
                        dto.Entities[^1].Approved = approved;
                    }
                }
            }
        }
    }
}