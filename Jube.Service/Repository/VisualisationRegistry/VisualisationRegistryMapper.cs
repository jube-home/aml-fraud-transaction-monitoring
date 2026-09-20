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
using Jube.Dto.Repository.VisualisationRegistry;
using RegistryPoco = Jube.Data.Poco.VisualisationRegistry;

namespace Jube.Service.Repository.VisualisationRegistry
{
    internal static class VisualisationRegistryMapper
    {
        [return: NotNullIfNotNull(nameof(registryPoco))]
        public static VisualisationRegistryDto? ToDto(RegistryPoco? registryPoco) => registryPoco is null
            ? null
            : new VisualisationRegistryDto
            {
                Id = registryPoco.Id,
                Name = registryPoco.Name,
                Active = registryPoco.Active == 1,
                Locked = registryPoco.Locked == 1,
                ShowInDirectory = registryPoco.ShowInDirectory == 1,
                Columns = registryPoco.Columns.GetValueOrDefault(),
                ColumnWidth = registryPoco.ColumnWidth.GetValueOrDefault(),
                RowHeight = registryPoco.RowHeight.GetValueOrDefault(),
                Guid = registryPoco.Guid,
                CreatedDate = ToOffset(registryPoco.CreatedDate),
                CreatedUser = registryPoco.CreatedUser,
                UpdatedUser = registryPoco.UpdatedUser,
                UpdatedDate = ToOffset(registryPoco.UpdatedDate),
                Version = registryPoco.Version.GetValueOrDefault(),
                DeletedUser = registryPoco.DeletedUser,
                DeletedDate = ToOffset(registryPoco.DeletedDate)
            };

        public static List<VisualisationRegistryDto> ToDto(IEnumerable<RegistryPoco> source) =>
            source.Select(registryPoco => ToDto(registryPoco)).ToList();

        public static RegistryPoco ToPoco(VisualisationRegistryDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            ShowInDirectory = (byte)(dto.ShowInDirectory ? 1 : 0),
            Columns = dto.Columns,
            ColumnWidth = dto.ColumnWidth,
            RowHeight = dto.RowHeight
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}