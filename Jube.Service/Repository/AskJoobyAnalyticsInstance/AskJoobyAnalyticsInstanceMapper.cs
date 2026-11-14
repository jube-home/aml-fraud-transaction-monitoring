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
using Jube.Service.Repository.AskJoobyAnalyticsInstance.Models;

namespace Jube.Service.Repository.AskJoobyAnalyticsInstance
{
    internal static class AskJoobyAnalyticsInstanceMapper
    {
        [return: NotNullIfNotNull(nameof(instance))]
        public static AskJoobyAnalyticsInstanceDto? ToDto(Jube.Data.Poco.AskJoobyAnalyticsInstance? instance)
        {
            return instance is null
                ? null
                : new AskJoobyAnalyticsInstanceDto
                {
                    Id = instance.Id,
                    Guid = instance.Guid,
                    Name = instance.Name,
                    Active = instance.Active == 1,
                    EntityAnalysisModelId = instance.EntityAnalysisModelId,
                    ResponsePayload = instance.ResponsePayload == 1,
                    ReportTable = instance.ReportTable == 1,
                    CreatedUser = instance.CreatedUser,
                    CreatedDate = ToOffset(instance.CreatedDate),
                    UpdatedUser = null,
                    UpdatedDate = ToOffset(instance.UpdatedDate),
                    Version = instance.Version,
                    DeletedUser = instance.DeletedUser,
                    DeletedDate = ToOffset(instance.DeletedDate)
                };
        }

        public static List<AskJoobyAnalyticsInstanceDto> ToDto(
            IEnumerable<Jube.Data.Poco.AskJoobyAnalyticsInstance>? source)
        {
            return (source ?? []).Select(p => ToDto(p)).ToList();
        }

        public static Jube.Data.Poco.AskJoobyAnalyticsInstance ToPoco(AskJoobyAnalyticsInstanceDto dto,
            int? entityAnalysisModelId)
        {
            return new Jube.Data.Poco.AskJoobyAnalyticsInstance
            {
                Id = dto.Id,
                Name = dto.Name!,
                EntityAnalysisModelId = entityAnalysisModelId,
                Active = (byte)(dto.Active ? 1 : 0),
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
                ReportTable = (byte)(dto.ReportTable ? 1 : 0)
            };
        }

        public static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}