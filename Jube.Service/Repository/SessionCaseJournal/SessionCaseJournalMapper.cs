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
using Jube.Dto.Repository.SessionCaseJournal;
using RulePoco = Jube.Data.Poco.SessionCaseJournal;

namespace Jube.Service.Repository.SessionCaseJournal
{
    internal static class SessionCaseJournalMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static SessionCaseJournalDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new SessionCaseJournalDto
            {
                Id = rulePoco.Id,
                Json = rulePoco.Json,
                CreatedUser = rulePoco.CreatedUser,
                CreatedDate = ToOffset(rulePoco.CreatedDate),
                CaseWorkflowGuid = rulePoco.CaseWorkflowGuid.GetValueOrDefault()
            };

        public static RulePoco ToPoco(SessionCaseJournalDto dto) => new()
        {
            Id = dto.Id,
            Json = dto.Json,
            CreatedUser = dto.CreatedUser,
            CreatedDate = dto.CreatedDate?.UtcDateTime,
            CaseWorkflowGuid = dto.CaseWorkflowGuid
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}