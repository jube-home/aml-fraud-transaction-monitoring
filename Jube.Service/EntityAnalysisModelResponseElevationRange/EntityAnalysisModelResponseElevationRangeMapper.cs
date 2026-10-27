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
using Jube.Dto.EntityAnalysisModelResponseElevationRange;

namespace Jube.Service.EntityAnalysisModelResponseElevationRange
{
    using RangePoco = Data.Poco.EntityAnalysisModelResponseElevationRange;

    internal static class EntityAnalysisModelResponseElevationRangeMapper
    {
        [return: NotNullIfNotNull(nameof(range))]
        public static EntityAnalysisModelResponseElevationRangeDto? ToDto(RangePoco? range)
        {
            return range is null
                ? null
                : new EntityAnalysisModelResponseElevationRangeDto
                {
                    Id = range.Id,
                    Guid = range.Guid,
                    EntityAnalysisModelId = range.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = range.Name,
                    Minimum = range.Minimum.GetValueOrDefault(),
                    Maximum = range.Maximum.GetValueOrDefault(),
                    Label = Label(range.Name, range.Minimum.GetValueOrDefault(), range.Maximum.GetValueOrDefault()),
                    Decline = range.Decline == 1,
                    Hold = range.Hold == 1,
                    Active = range.Active == 1,
                    Locked = range.Locked == 1,
                    Content = range.Content,
                    Redirect = range.Redirect,
                    ForeColor = range.ForeColor,
                    BackColor = range.BackColor,
                    EnableHttpEndpoint = range.EnableHttpEndpoint == 1,
                    HttpEndpoint = range.HttpEndpoint,
                    HttpEndpointTypeId = range.HttpEndpointTypeId.GetValueOrDefault(),
                    HttpEndpointHeaderName = range.HttpEndpointHeaderName,
                    HttpEndpointCredentialVariable = range.HttpEndpointCredentialVariable,
                    EnableNotification = range.EnableNotification == 1,
                    NotificationTypeId = range.NotificationTypeId.GetValueOrDefault(),
                    NotificationDestination = range.NotificationDestination,
                    NotificationSubject = range.NotificationSubject,
                    NotificationBody = range.NotificationBody,
                    CreatedUser = range.CreatedUser,
                    CreatedDate = ToOffset(range.CreatedDate),
                    UpdatedUser = range.UpdatedUser,
                    UpdatedDate = ToOffset(range.UpdatedDate),
                    Version = range.Version.GetValueOrDefault(),
                    DeletedUser = range.DeletedUser,
                    DeletedDate = ToOffset(range.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelResponseElevationRangeDto> ToDto(IEnumerable<RangePoco>? source)
        {
            return (source ?? Enumerable.Empty<RangePoco>()).Select(p => ToDto(p)).ToList();
        }

        public static RangePoco ToPoco(EntityAnalysisModelResponseElevationRangeDto dto)
        {
            return new RangePoco
            {
                Version = dto.Version,
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Minimum = dto.Minimum,
                Maximum = dto.Maximum,
                Decline = (byte)(dto.Decline ? 1 : 0),
                Hold = (byte)(dto.Hold ? 1 : 0),
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                Content = dto.Content,
                Redirect = dto.Redirect,
                ForeColor = dto.ForeColor,
                BackColor = dto.BackColor,
                EnableHttpEndpoint = (byte)(dto.EnableHttpEndpoint ? 1 : 0),
                HttpEndpoint = dto.HttpEndpoint,
                HttpEndpointTypeId = (byte)dto.HttpEndpointTypeId,
                HttpEndpointHeaderName = dto.HttpEndpointHeaderName,
                HttpEndpointCredentialVariable = dto.HttpEndpointCredentialVariable,
                EnableNotification = (byte)(dto.EnableNotification ? 1 : 0),
                NotificationTypeId = (byte)dto.NotificationTypeId,
                NotificationDestination = dto.NotificationDestination,
                NotificationSubject = dto.NotificationSubject,
                NotificationBody = dto.NotificationBody
            };
        }

        private static string Label(string? name, int minimum, int maximum)
        {
            var levels = minimum == maximum ? $"{minimum}" : $"{minimum}-{maximum}";

            return $"{name} ({levels})";
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}
