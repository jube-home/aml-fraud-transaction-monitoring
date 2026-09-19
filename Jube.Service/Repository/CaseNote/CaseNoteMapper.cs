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
using Jube.Dto.Repository.CaseNote;
using RulePoco = Jube.Data.Poco.CaseNote;

namespace Jube.Service.Repository.CaseNote
{
    internal static class CaseNoteMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static CaseNoteDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseNoteDto
            {
                Id = rulePoco.Id,
                Note = rulePoco.Note,
                ActionId = rulePoco.ActionId.GetValueOrDefault(),
                PriorityId = rulePoco.PriorityId.GetValueOrDefault(),
                CreatedDate = ToOffset(rulePoco.CreatedDate),
                CaseKey = rulePoco.CaseKey,
                CreatedUser = rulePoco.CreatedUser,
                CaseKeyValue = rulePoco.CaseKeyValue,
                CaseId = rulePoco.CaseId.GetValueOrDefault(),
                Payload = null
            };

        public static List<CaseNoteDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(CaseNoteDto caseNoteDto) => new()
        {
            Note = caseNoteDto.Note,
            ActionId = caseNoteDto.ActionId,
            PriorityId = caseNoteDto.PriorityId,
            CaseKey = caseNoteDto.CaseKey,
            CaseKeyValue = caseNoteDto.CaseKeyValue,
            CaseId = caseNoteDto.CaseId
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}