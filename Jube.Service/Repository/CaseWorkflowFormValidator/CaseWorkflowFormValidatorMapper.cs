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
using Jube.Dto.Repository.CaseWorkflowFormValidator;
using RulePoco = Jube.Data.Poco.CaseWorkflowFormValidator;

namespace Jube.Service.Repository.CaseWorkflowFormValidator
{
    internal static class CaseWorkflowFormValidatorMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static CaseWorkflowFormValidatorDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseWorkflowFormValidatorDto
            {
                Id = rulePoco.Id,
                CaseWorkflowId = rulePoco.CaseWorkflowId,
                CaseWorkflowFormGuid = rulePoco.CaseWorkflowFormGuid,
                Name = rulePoco.Name,
                Active = rulePoco.Active == 1,
                FieldName = rulePoco.FieldName,
                KindId = rulePoco.KindId.GetValueOrDefault(),
                Argument1 = rulePoco.Argument1,
                Argument2 = rulePoco.Argument2,
                Sql = rulePoco.Sql,
                Message = rulePoco.Message,
                Guid = rulePoco.Guid,
                CreatedDate = ToOffset(rulePoco.CreatedDate),
                CreatedUser = rulePoco.CreatedUser,
                UpdatedUser = rulePoco.UpdatedUser,
                UpdatedDate = ToOffset(rulePoco.UpdatedDate),
                Version = rulePoco.Version.GetValueOrDefault(),
                DeletedUser = rulePoco.DeletedUser,
                DeletedDate = ToOffset(rulePoco.DeletedDate)
            };

        public static List<CaseWorkflowFormValidatorDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(CaseWorkflowFormValidatorDto dto) => new()
        {
            Version = dto.Version,
            Id = dto.Id,
            CaseWorkflowId = dto.CaseWorkflowId,
            CaseWorkflowFormGuid = dto.CaseWorkflowFormGuid,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            FieldName = dto.FieldName,
            KindId = (byte)dto.KindId,
            Argument1 = dto.Argument1,
            Argument2 = dto.Argument2,
            Sql = dto.Sql,
            Message = dto.Message
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}
