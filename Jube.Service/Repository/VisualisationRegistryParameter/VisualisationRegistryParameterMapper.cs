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
using Jube.Dto.Repository.VisualisationRegistryParameter;
using VisualisationRegistryParameterPoco = Jube.Data.Poco.VisualisationRegistryParameter;

namespace Jube.Service.Repository.VisualisationRegistryParameter
{
    internal static class VisualisationRegistryParameterMapper
    {
        [return: NotNullIfNotNull(nameof(visualisationRegistryParameter))]
        public static VisualisationRegistryParameterDto? ToDto(
            VisualisationRegistryParameterPoco? visualisationRegistryParameter)
        {
            return visualisationRegistryParameter is null
                ? null
                : new VisualisationRegistryParameterDto
                {
                    Id = visualisationRegistryParameter.Id,
                    VisualisationRegistryId =
                        visualisationRegistryParameter.VisualisationRegistryId.GetValueOrDefault(),
                    Guid = visualisationRegistryParameter.Guid,
                    Name = visualisationRegistryParameter.Name,
                    Active = visualisationRegistryParameter.Active == 1,
                    Locked = visualisationRegistryParameter.Locked == 1,
                    DataTypeId = visualisationRegistryParameter.DataTypeId.GetValueOrDefault(),
                    Required = visualisationRegistryParameter.Required == 1,
                    DefaultValue = visualisationRegistryParameter.DefaultValue,
                    CreatedUser = visualisationRegistryParameter.CreatedUser,
                    CreatedDate = ToOffset(visualisationRegistryParameter.CreatedDate),
                    UpdatedUser = visualisationRegistryParameter.UpdatedUser,
                    UpdatedDate = ToOffset(visualisationRegistryParameter.UpdatedDate),
                    Version = visualisationRegistryParameter.Version.GetValueOrDefault(),
                    DeletedUser = visualisationRegistryParameter.DeletedUser,
                    DeletedDate = ToOffset(visualisationRegistryParameter.DeletedDate)
                };
        }

        public static List<VisualisationRegistryParameterDto> ToDto(
            IEnumerable<VisualisationRegistryParameterPoco>? source)
        {
            return (source ?? Enumerable.Empty<VisualisationRegistryParameterPoco>()).Select(p => ToDto(p)).ToList();
        }

        public static VisualisationRegistryParameterPoco ToPoco(VisualisationRegistryParameterDto dto)
        {
            return new VisualisationRegistryParameterPoco
            {
                Id = dto.Id,
                VisualisationRegistryId = dto.VisualisationRegistryId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                DataTypeId = (byte)dto.DataTypeId,
                Required = (byte)(dto.Required ? 1 : 0),
                DefaultValue = dto.DefaultValue
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