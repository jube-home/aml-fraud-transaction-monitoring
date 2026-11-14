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

using Jube.Dto.Repository.CaseTag;

namespace Jube.Service.Repository.CaseTag
{
    public static class CaseTagMapper
    {
        public static CaseTagDto ToDto(Data.Poco.CaseTag tag) => new()
        {
            Id = tag.Id, CaseId = tag.CaseId, Name = tag.Name, CreatedUser = tag.CreatedUser, CreatedDate = tag.CreatedDate
        };

        public static List<CaseTagDto> ToDto(IEnumerable<Data.Poco.CaseTag> tags) => tags.Select(ToDto).ToList();
    }
}
