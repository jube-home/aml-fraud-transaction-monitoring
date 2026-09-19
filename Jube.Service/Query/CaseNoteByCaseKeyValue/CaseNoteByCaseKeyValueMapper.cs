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

using Jube.Dto.Query.CaseNoteByCaseKeyValue;
using Jube.Service.Security;

namespace Jube.Service.Query.CaseNoteByCaseKeyValue
{
    using DataDto = global::Jube.Data.Query.GetCaseNoteByCaseKeyValueQuery.Dto;

    internal static class CaseNoteByCaseKeyValueMapper
    {
        public static CaseNoteByCaseKeyValueDto ToDto(DataDto source) => new()
        {
            Id = source.Id,
            CaseId = source.CaseId,
            CreatedDate = source.CreatedDate,
            CreatedUser = source.CreatedUser,
            Note = HtmlSanitiser.Sanitise(source.Note),
            ActionId = source.ActionId,
            Action = source.Action,
            PriorityId = source.PriorityId,
            Priority = source.Priority
        };

        public static List<CaseNoteByCaseKeyValueDto> ToDto(IEnumerable<DataDto> source) =>
            source.Select(ToDto).ToList();
    }
}