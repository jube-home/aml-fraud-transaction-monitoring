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

using Jube.Dto.OpenTelemetryLogCounter;

namespace Jube.Service.OpenTelemetryLogCounter
{
    using LogCounterPoco = Data.Poco.OpenTelemetryLogCounter;

    internal static class OpenTelemetryLogCounterMapper
    {
        public static OpenTelemetryLogCounterDto? ToDto(LogCounterPoco? logCounter)
        {
            return logCounter is null
                ? null
                : new OpenTelemetryLogCounterDto
                {
                    Id = logCounter.Id,
                    Name = logCounter.Name,
                    Regex = logCounter.Regex,
                    Active = logCounter.Active == 1,
                    Locked = logCounter.Locked == 1,
                    CreatedUser = logCounter.CreatedUser,
                    CreatedDate = ToOffset(logCounter.CreatedDate),
                    UpdatedUser = logCounter.UpdatedUser,
                    UpdatedDate = ToOffset(logCounter.UpdatedDate),
                    Version = logCounter.Version.GetValueOrDefault(),
                    DeletedUser = logCounter.DeletedUser,
                    DeletedDate = ToOffset(logCounter.DeletedDate)
                };
        }

        public static List<OpenTelemetryLogCounterDto> ToDto(IEnumerable<LogCounterPoco>? source)
        {
            return (source ?? Enumerable.Empty<LogCounterPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static LogCounterPoco ToPoco(OpenTelemetryLogCounterDto dto)
        {
            return new LogCounterPoco
            {
                Id = dto.Id,
                Name = dto.Name,
                Regex = dto.Regex,
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