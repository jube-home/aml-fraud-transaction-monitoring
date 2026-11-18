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
using Jube.Dto.Repository.GraphRegistryEdge;
using EdgePoco = Jube.Data.Poco.GraphRegistryEdge;

namespace Jube.Service.Repository.GraphRegistryEdge
{
    internal static class GraphRegistryEdgeMapper
    {
        [return: NotNullIfNotNull(nameof(edgePoco))]
        public static GraphRegistryEdgeDto? ToDto(EdgePoco? edgePoco) => edgePoco is null
            ? null
            : new GraphRegistryEdgeDto
            {
                Id = edgePoco.Id,
                GraphRegistryId = edgePoco.GraphRegistryId.GetValueOrDefault(),
                Guid = edgePoco.Guid,
                Name = edgePoco.Name,
                Active = edgePoco.Active == 1,
                Locked = edgePoco.Locked == 1,
                KindId = edgePoco.KindId ?? 1,
                FromKey = edgePoco.FromKey,
                ToKey = edgePoco.ToKey,
                FromNodeKey = edgePoco.FromNodeKey,
                ToNodeKey = edgePoco.ToNodeKey,
                IconName = edgePoco.IconName,
                FromNodeIcon = edgePoco.FromNodeIcon,
                ToNodeIcon = edgePoco.ToNodeIcon,
                AmountKey = edgePoco.AmountKey,
                MinimumAmount = edgePoco.MinimumAmount.GetValueOrDefault(),
                HalfLifeDays = edgePoco.HalfLifeDays ?? 90,
                KindPrior = edgePoco.KindPrior ?? 1,
                HubThreshold = edgePoco.HubThreshold,
                Hard = edgePoco.Hard == 1,
                Restricted = edgePoco.Restricted == 1,
                CreatedDate = ToOffset(edgePoco.CreatedDate),
                CreatedUser = edgePoco.CreatedUser,
                UpdatedUser = edgePoco.UpdatedUser,
                UpdatedDate = ToOffset(edgePoco.UpdatedDate),
                Version = edgePoco.Version.GetValueOrDefault(),
                DeletedUser = edgePoco.DeletedUser,
                DeletedDate = ToOffset(edgePoco.DeletedDate)
            };

        public static List<GraphRegistryEdgeDto> ToDto(IEnumerable<EdgePoco> source) =>
            source.Select(edgePoco => ToDto(edgePoco)).ToList();

        public static EdgePoco ToPoco(GraphRegistryEdgeDto dto) => new()
        {
            Version = dto.Version,
            Id = dto.Id,
            GraphRegistryId = dto.GraphRegistryId,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            KindId = (byte)dto.KindId,
            FromKey = dto.FromKey?.Trim(),
            ToKey = dto.ToKey?.Trim(),
            FromNodeKey = string.IsNullOrWhiteSpace(dto.FromNodeKey) ? null : dto.FromNodeKey.Trim(),
            ToNodeKey = string.IsNullOrWhiteSpace(dto.ToNodeKey) ? null : dto.ToNodeKey.Trim(),
            IconName = string.IsNullOrWhiteSpace(dto.IconName) ? null : dto.IconName.Trim(),
            FromNodeIcon = string.IsNullOrWhiteSpace(dto.FromNodeIcon) ? null : dto.FromNodeIcon.Trim(),
            ToNodeIcon = string.IsNullOrWhiteSpace(dto.ToNodeIcon) ? null : dto.ToNodeIcon.Trim(),
            AmountKey = string.IsNullOrWhiteSpace(dto.AmountKey) ? null : dto.AmountKey.Trim(),
            MinimumAmount = dto.MinimumAmount,
            HalfLifeDays = dto.HalfLifeDays,
            KindPrior = dto.KindPrior,
            HubThreshold = dto.HubThreshold,
            Hard = (byte)(dto.Hard ? 1 : 0),
            Restricted = (byte)(dto.Restricted ? 1 : 0)
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}
