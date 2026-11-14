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
using Jube.Dto.Repository.EntityAnalysisModelGraphRecall;
using RecallPoco = Jube.Data.Poco.EntityAnalysisModelGraphRecall;

namespace Jube.Service.Repository.EntityAnalysisModelGraphRecall
{
    internal static class EntityAnalysisModelGraphRecallMapper
    {
        [return: NotNullIfNotNull(nameof(recallPoco))]
        public static EntityAnalysisModelGraphRecallDto? ToDto(RecallPoco? recallPoco) => recallPoco is null
            ? null
            : new EntityAnalysisModelGraphRecallDto
            {
                Id = recallPoco.Id,
                EntityAnalysisModelId = recallPoco.EntityAnalysisModelId.GetValueOrDefault(),
                Guid = recallPoco.Guid,
                Name = recallPoco.Name,
                Active = recallPoco.Active == 1,
                Locked = recallPoco.Locked == 1,
                RequestXPathName = recallPoco.RequestXPathName,
                NodeKey = recallPoco.NodeKey,
                GraphRegistryGuid = recallPoco.GraphRegistryGuid,
                CounterpartName = recallPoco.CounterpartName,
                ResponsePayload = recallPoco.ResponsePayload == 1,
                ReportTable = recallPoco.ReportTable == 1,
                CreatedDate = ToOffset(recallPoco.CreatedDate),
                CreatedUser = recallPoco.CreatedUser,
                UpdatedUser = recallPoco.UpdatedUser,
                UpdatedDate = ToOffset(recallPoco.UpdatedDate),
                Version = recallPoco.Version.GetValueOrDefault(),
                DeletedUser = recallPoco.DeletedUser,
                DeletedDate = ToOffset(recallPoco.DeletedDate)
            };

        public static List<EntityAnalysisModelGraphRecallDto> ToDto(IEnumerable<RecallPoco> source) =>
            [.. source.Select(recallPoco => ToDto(recallPoco))];

        public static RecallPoco ToPoco(EntityAnalysisModelGraphRecallDto dto) => new()
        {
            Version = dto.Version,
            Id = dto.Id,
            EntityAnalysisModelId = dto.EntityAnalysisModelId,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            RequestXPathName = dto.RequestXPathName,
            NodeKey = dto.NodeKey,
            GraphRegistryGuid = dto.GraphRegistryGuid,
            CounterpartName = dto.CounterpartName,
            ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
            ReportTable = (byte)(dto.ReportTable ? 1 : 0)
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}
