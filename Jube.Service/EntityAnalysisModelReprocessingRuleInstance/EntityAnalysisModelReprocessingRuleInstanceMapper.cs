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

using Jube.Dto.EntityAnalysisModelReprocessingRuleInstance;

namespace Jube.Service.EntityAnalysisModelReprocessingRuleInstance
{
    using ReprocessingRuleInstancePoco = Data.Poco.EntityAnalysisModelReprocessingRuleInstance;

    internal static class EntityAnalysisModelReprocessingRuleInstanceMapper
    {
        public static EntityAnalysisModelReprocessingRuleInstanceDto? ToDto(
            ReprocessingRuleInstancePoco? reprocessingRuleInstance)
        {
            return reprocessingRuleInstance is null
                ? null
                : new EntityAnalysisModelReprocessingRuleInstanceDto
                {
                    Id = reprocessingRuleInstance.Id,
                    EntityAnalysisModelReprocessingRuleId =
                        reprocessingRuleInstance.EntityAnalysisModelReprocessingRuleId.GetValueOrDefault(),
                    StatusId = reprocessingRuleInstance.StatusId.GetValueOrDefault(),
                    StartedDate = ToOffset(reprocessingRuleInstance.StartedDate),
                    AvailableCount = (int)reprocessingRuleInstance.AvailableCount.GetValueOrDefault(),
                    SampledCount = (int)reprocessingRuleInstance.SampledCount.GetValueOrDefault(),
                    MatchedCount = (int)reprocessingRuleInstance.MatchedCount.GetValueOrDefault(),
                    ProcessedCount = (int)reprocessingRuleInstance.ProcessedCount.GetValueOrDefault(),
                    CompletedDate = ToOffset(reprocessingRuleInstance.CompletedDate),
                    ErrorCount = (int)reprocessingRuleInstance.ErrorCount.GetValueOrDefault(),
                    ReferenceDate = ToOffset(reprocessingRuleInstance.ReferenceDate),
                    CreatedUser = reprocessingRuleInstance.CreatedUser,
                    CreatedDate = ToOffset(reprocessingRuleInstance.CreatedDate),
                    UpdatedDate = ToOffset(reprocessingRuleInstance.UpdatedDate),
                    Version = reprocessingRuleInstance.Version,
                    DeletedUser = reprocessingRuleInstance.DeletedUser,
                    DeletedDate = ToOffset(reprocessingRuleInstance.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelReprocessingRuleInstanceDto> ToDto(
            IEnumerable<ReprocessingRuleInstancePoco>? source)
        {
            return (source ?? Enumerable.Empty<ReprocessingRuleInstancePoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static ReprocessingRuleInstancePoco ToPoco(EntityAnalysisModelReprocessingRuleInstanceDto dto)
        {
            return new ReprocessingRuleInstancePoco
            {
                Id = dto.Id,
                EntityAnalysisModelReprocessingRuleId = dto.EntityAnalysisModelReprocessingRuleId,
                StatusId = (byte)dto.StatusId,
                StartedDate = dto.StartedDate?.UtcDateTime,
                AvailableCount = dto.AvailableCount,
                SampledCount = dto.SampledCount,
                MatchedCount = dto.MatchedCount,
                ProcessedCount = dto.ProcessedCount,
                CompletedDate = dto.CompletedDate?.UtcDateTime,
                ErrorCount = dto.ErrorCount,
                ReferenceDate = dto.ReferenceDate?.UtcDateTime
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