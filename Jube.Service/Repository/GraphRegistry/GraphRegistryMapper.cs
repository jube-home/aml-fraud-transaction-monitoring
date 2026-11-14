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
using Jube.Dto.Repository.GraphRegistry;
using RegistryPoco = Jube.Data.Poco.GraphRegistry;

namespace Jube.Service.Repository.GraphRegistry
{
    internal static class GraphRegistryMapper
    {
        [return: NotNullIfNotNull(nameof(registryPoco))]
        public static GraphRegistryDto? ToDto(RegistryPoco? registryPoco) => registryPoco is null
            ? null
            : new GraphRegistryDto
            {
                Id = registryPoco.Id,
                Name = registryPoco.Name,
                Active = registryPoco.Active == 1,
                Locked = registryPoco.Locked == 1,
                ContextKindId = registryPoco.ContextKindId ?? 1,
                DefaultLayoutId = registryPoco.DefaultLayoutId ?? 1,
                DefaultHops = registryPoco.DefaultHops ?? 2,
                DefaultWindowDays = registryPoco.DefaultWindowDays ?? 30,
                RenderCap = registryPoco.RenderCap ?? 150,
                HubPolicyId = registryPoco.HubPolicyId ?? 1,
                Guid = registryPoco.Guid,
                CreatedDate = ToOffset(registryPoco.CreatedDate),
                CreatedUser = registryPoco.CreatedUser,
                UpdatedUser = registryPoco.UpdatedUser,
                UpdatedDate = ToOffset(registryPoco.UpdatedDate),
                Version = registryPoco.Version.GetValueOrDefault(),
                DeletedUser = registryPoco.DeletedUser,
                DeletedDate = ToOffset(registryPoco.DeletedDate)
            };

        public static List<GraphRegistryDto> ToDto(IEnumerable<RegistryPoco> source) =>
            source.Select(registryPoco => ToDto(registryPoco)).ToList();

        public static RegistryPoco ToPoco(GraphRegistryDto dto) => new()
        {
            Version = dto.Version,
            Id = dto.Id,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            ContextKindId = (byte)(dto.ContextKindId is 1 or 2 ? dto.ContextKindId : 1),
            DefaultLayoutId = (byte)dto.DefaultLayoutId,
            DefaultHops = (byte)dto.DefaultHops,
            DefaultWindowDays = dto.DefaultWindowDays,
            RenderCap = dto.RenderCap,
            HubPolicyId = (byte)dto.HubPolicyId
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}
