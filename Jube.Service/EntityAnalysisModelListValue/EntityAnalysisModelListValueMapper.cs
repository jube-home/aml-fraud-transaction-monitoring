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

using Jube.Dto.EntityAnalysisModelListValue;

namespace Jube.Service.EntityAnalysisModelListValue
{
    using ListValuePoco = Data.Poco.EntityAnalysisModelListValue;

    internal static class EntityAnalysisModelListValueMapper
    {
        public static EntityAnalysisModelListValueDto? ToDto(ListValuePoco? listValue)
        {
            return listValue is null
                ? null
                : new EntityAnalysisModelListValueDto
                {
                    Id = listValue.Id,
                    EntityAnalysisModelListId = listValue.EntityAnalysisModelListId.GetValueOrDefault(),
                    ListValue = listValue.ListValue,
                    DeleteExpiryDate = ToOffset(listValue.DeleteExpiryDate),
                    CreatedUser = listValue.CreatedUser,
                    CreatedDate = ToOffset(listValue.CreatedDate),
                    UpdatedUser = listValue.UpdatedUser,
                    UpdatedDate = ToOffset(listValue.UpdatedDate),
                    Version = listValue.Version.GetValueOrDefault(),
                    DeletedUser = listValue.DeletedUser,
                    DeletedDate = ToOffset(listValue.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelListValueDto> ToDto(IEnumerable<ListValuePoco>? source)
        {
            return (source ?? Enumerable.Empty<ListValuePoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static ListValuePoco ToPoco(EntityAnalysisModelListValueDto dto)
        {
            return new ListValuePoco
            {
                Id = dto.Id,
                EntityAnalysisModelListId = dto.EntityAnalysisModelListId,
                ListValue = dto.ListValue,
                DeleteExpiryDate = dto.DeleteExpiryDate?.UtcDateTime
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