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

using System.Diagnostics.CodeAnalysis;
using Jube.Dto.Repository.CaseWorkflow;
using RulePoco = Jube.Data.Poco.CaseWorkflow;

namespace Jube.Service.Repository.CaseWorkflow
{
    internal static class CaseWorkflowMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static CaseWorkflowDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseWorkflowDto
            {
                Id = rulePoco.Id,
                Name = rulePoco.Name,
                Active = rulePoco.Active == 1,
                Locked = rulePoco.Locked == 1,
                EnableVisualisation = rulePoco.EnableVisualisation == 1,
                VisualisationRegistryGuid = rulePoco.VisualisationRegistryGuid,
                EntityAnalysisModelId = rulePoco.EntityAnalysisModelId.GetValueOrDefault(),
                Guid = rulePoco.Guid,
                CreatedDate = ToOffset(rulePoco.CreatedDate),
                CreatedUser = rulePoco.CreatedUser,
                UpdatedUser = rulePoco.UpdatedUser,
                UpdatedDate = ToOffset(rulePoco.UpdatedDate),
                Version = rulePoco.Version.GetValueOrDefault(),
                DeletedUser = rulePoco.DeletedUser,
                DeletedDate = ToOffset(rulePoco.DeletedDate)
            };

        public static List<CaseWorkflowDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(CaseWorkflowDto caseWorkflowDto) => new()
        {
            Id = caseWorkflowDto.Id,
            Name = caseWorkflowDto.Name,
            Active = (byte)(caseWorkflowDto.Active ? 1 : 0),
            Locked = (byte)(caseWorkflowDto.Locked ? 1 : 0),
            EnableVisualisation = (byte)(caseWorkflowDto.EnableVisualisation ? 1 : 0),
            VisualisationRegistryGuid = caseWorkflowDto.VisualisationRegistryGuid,
            EntityAnalysisModelId = caseWorkflowDto.EntityAnalysisModelId
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}