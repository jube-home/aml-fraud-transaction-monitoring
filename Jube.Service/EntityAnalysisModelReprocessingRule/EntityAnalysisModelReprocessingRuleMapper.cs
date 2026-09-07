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

using Jube.Dto.EntityAnalysisModelReprocessingRule;

namespace Jube.Service.EntityAnalysisModelReprocessingRule
{
    using ReprocessingRulePoco = Data.Poco.EntityAnalysisModelReprocessingRule;

    internal static class EntityAnalysisModelReprocessingRuleMapper
    {
        public static EntityAnalysisModelReprocessingRuleDto? ToDto(ReprocessingRulePoco? reprocessingRule)
        {
            return reprocessingRule is null
                ? null
                : new EntityAnalysisModelReprocessingRuleDto
                {
                    Id = reprocessingRule.Id,
                    EntityAnalysisModelId = reprocessingRule.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = reprocessingRule.Name,
                    Priority = reprocessingRule.Priority.GetValueOrDefault(),
                    Active = reprocessingRule.Active == 1,
                    Locked = reprocessingRule.Locked == 1,
                    RuleScriptTypeId = reprocessingRule.RuleScriptTypeId.GetValueOrDefault(),
                    BuilderRuleScript = reprocessingRule.BuilderRuleScript,
                    Json = reprocessingRule.Json,
                    CoderRuleScript = reprocessingRule.CoderRuleScript,
                    ReprocessingSample = reprocessingRule.ReprocessingSample.GetValueOrDefault(),
                    ReprocessingValue = reprocessingRule.ReprocessingValue.GetValueOrDefault(),
                    ReprocessingInterval = reprocessingRule.ReprocessingInterval,
                    CreatedUser = reprocessingRule.CreatedUser,
                    CreatedDate = ToOffset(reprocessingRule.CreatedDate),
                    Version = reprocessingRule.Version.GetValueOrDefault(),
                    DeletedUser = reprocessingRule.DeletedUser,
                    DeletedDate = ToOffset(reprocessingRule.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelReprocessingRuleDto> ToDto(IEnumerable<ReprocessingRulePoco>? source)
        {
            return (source ?? Enumerable.Empty<ReprocessingRulePoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static ReprocessingRulePoco ToPoco(EntityAnalysisModelReprocessingRuleDto dto)
        {
            return new ReprocessingRulePoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Priority = dto.Priority,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                RuleScriptTypeId = dto.RuleScriptTypeId,
                BuilderRuleScript = dto.BuilderRuleScript,
                Json = string.IsNullOrWhiteSpace(dto.Json) ? null : dto.Json,
                CoderRuleScript = dto.CoderRuleScript,
                ReprocessingSample = dto.ReprocessingSample,
                ReprocessingValue = dto.ReprocessingValue,
                ReprocessingInterval = dto.ReprocessingInterval
            };
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}
