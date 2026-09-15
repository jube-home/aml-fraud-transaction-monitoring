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

using Jube.Dto.OpenTelemetryExclude;

namespace Jube.Service.OpenTelemetryExclude
{
    using ExcludePoco = Data.Poco.OpenTelemetryExclude;

    internal static class OpenTelemetryExcludeMapper
    {
        public static OpenTelemetryExcludeDto? ToDto(ExcludePoco? exclude)
        {
            return exclude is null
                ? null
                : new OpenTelemetryExcludeDto
                {
                    Id = exclude.Id,
                    Name = exclude.Name,
                    Active = exclude.Active == 1,
                    Locked = exclude.Locked == 1,
                    CreatedUser = exclude.CreatedUser,
                    CreatedDate = ToOffset(exclude.CreatedDate),
                    UpdatedUser = exclude.UpdatedUser,
                    UpdatedDate = ToOffset(exclude.UpdatedDate),
                    Version = exclude.Version.GetValueOrDefault(),
                    DeletedUser = exclude.DeletedUser,
                    DeletedDate = ToOffset(exclude.DeletedDate)
                };
        }

        public static List<OpenTelemetryExcludeDto> ToDto(IEnumerable<ExcludePoco>? source)
        {
            return (source ?? Enumerable.Empty<ExcludePoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static ExcludePoco ToPoco(OpenTelemetryExcludeDto dto)
        {
            return new ExcludePoco
            {
                Id = dto.Id,
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