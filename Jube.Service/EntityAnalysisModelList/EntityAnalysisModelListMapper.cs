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

using Jube.Dto.EntityAnalysisModelList;

namespace Jube.Service.EntityAnalysisModelList
{
    using ListPoco = Data.Poco.EntityAnalysisModelList;

    internal static class EntityAnalysisModelListMapper
    {
        public static EntityAnalysisModelListDto? ToDto(ListPoco? list)
        {
            return list is null
                ? null
                : new EntityAnalysisModelListDto
                {
                    Id = list.Id,
                    EntityAnalysisModelGuid = list.EntityAnalysisModelGuid,
                    Name = list.Name,
                    Active = list.Active == 1,
                    Locked = list.Locked == 1,
                    CreatedUser = list.CreatedUser,
                    CreatedDate = ToOffset(list.CreatedDate),
                    UpdatedUser = list.UpdatedUser,
                    UpdatedDate = ToOffset(list.UpdatedDate),
                    Version = list.Version.GetValueOrDefault(),
                    DeletedUser = list.DeletedUser,
                    DeletedDate = ToOffset(list.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelListDto> ToDto(IEnumerable<ListPoco>? source)
        {
            return (source ?? Enumerable.Empty<ListPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static ListPoco ToPoco(EntityAnalysisModelListDto dto)
        {
            return new ListPoco
            {
                Id = dto.Id,
                EntityAnalysisModelGuid = dto.EntityAnalysisModelGuid,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0)
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