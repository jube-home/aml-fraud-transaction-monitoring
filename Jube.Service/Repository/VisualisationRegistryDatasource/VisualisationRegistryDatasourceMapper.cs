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
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using DatasourcePoco = Jube.Data.Poco.VisualisationRegistryDatasource;

namespace Jube.Service.Repository.VisualisationRegistryDatasource
{
    internal static class VisualisationRegistryDatasourceMapper
    {
        [return: NotNullIfNotNull(nameof(datasource))]
        public static VisualisationRegistryDatasourceDto? ToDto(DatasourcePoco? datasource)
        {
            return datasource is null
                ? null
                : new VisualisationRegistryDatasourceDto
                {
                    Id = datasource.Id,
                    VisualisationRegistryId = datasource.VisualisationRegistryId.GetValueOrDefault(),
                    Guid = datasource.Guid,
                    Name = datasource.Name,
                    Active = datasource.Active == 1,
                    Locked = datasource.Locked == 1,
                    VisualisationTypeId = datasource.VisualisationTypeId.GetValueOrDefault(),
                    Command = datasource.Command,
                    VisualisationText = datasource.VisualisationText,
                    Priority = datasource.Priority.GetValueOrDefault(),
                    IncludeGrid = datasource.IncludeGrid == 1,
                    IncludeDisplay = datasource.IncludeDisplay == 1,
                    ColumnSpan = datasource.ColumnSpan.GetValueOrDefault(),
                    RowSpan = datasource.RowSpan.GetValueOrDefault(),
                    CreatedUser = datasource.CreatedUser,
                    CreatedDate = ToOffset(datasource.CreatedDate),
                    UpdatedUser = datasource.UpdatedUser,
                    UpdatedDate = ToOffset(datasource.UpdatedDate),
                    Version = datasource.Version.GetValueOrDefault(),
                    DeletedUser = datasource.DeletedUser,
                    DeletedDate = ToOffset(datasource.DeletedDate)
                };
        }

        public static List<VisualisationRegistryDatasourceDto> ToDto(IEnumerable<DatasourcePoco>? source)
        {
            return (source ?? Enumerable.Empty<DatasourcePoco>()).Select(p => ToDto(p)).ToList();
        }

        public static DatasourcePoco ToPoco(VisualisationRegistryDatasourceDto dto)
        {
            return new DatasourcePoco
            {
                Id = dto.Id,
                VisualisationRegistryId = dto.VisualisationRegistryId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                VisualisationTypeId = dto.VisualisationTypeId,
                Command = dto.Command,
                VisualisationText = dto.VisualisationText,
                Priority = dto.Priority,
                IncludeGrid = (byte)(dto.IncludeGrid ? 1 : 0),
                IncludeDisplay = (byte)(dto.IncludeDisplay ? 1 : 0),
                ColumnSpan = dto.ColumnSpan,
                RowSpan = dto.RowSpan
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