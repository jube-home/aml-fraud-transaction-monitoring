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
using Jube.Dto.Repository.SanctionEntrySource;
using SanctionEntrySourcePoco = Jube.Data.Poco.SanctionEntrySource;

namespace Jube.Service.Repository.SanctionEntrySource
{
    internal static class SanctionEntrySourceMapper
    {
        [return: NotNullIfNotNull(nameof(sanctionEntrySource))]
        private static SanctionEntrySourceDto? ToDto(SanctionEntrySourcePoco? sanctionEntrySource)
        {
            return sanctionEntrySource is null
                ? null
                : new SanctionEntrySourceDto
                {
                    Id = sanctionEntrySource.Id,
                    Name = sanctionEntrySource.Name
                };
        }

        public static List<SanctionEntrySourceDto> ToDto(IEnumerable<SanctionEntrySourcePoco>? source)
        {
            return (source ?? Enumerable.Empty<SanctionEntrySourcePoco>()).Select(p => ToDto(p)).ToList();
        }
    }
}