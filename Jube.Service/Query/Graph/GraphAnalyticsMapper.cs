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

using System.Globalization;
using Jube.Data.Poco;
using Jube.Dto.Options;
using Jube.Dto.Query.Graph;
using Jube.Resources;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Jube.Service.Query.Graph
{
    internal static class GraphAnalyticsMapper
    {
        public static GraphPatternDto ToPattern(GraphPattern pattern, GraphNodeDto? anchor, IStringLocalizer strings)
        {
            var kind = Enum.IsDefined((GraphPatternKind)pattern.KindId) ? ((GraphPatternKind)pattern.KindId).ToString() : null;
            var dto = new GraphPatternDto
            {
                Id = pattern.Id,
                KindId = pattern.KindId,
                Kind = kind,
                Anchor = anchor,
                Score = pattern.Score,
                WindowFrom = Offset(pattern.WindowFrom),
                WindowTo = Offset(pattern.WindowTo),
                Parameters = string.IsNullOrWhiteSpace(pattern.Parameters)
                    ? []
                    : JsonConvert.DeserializeObject<Dictionary<string, double>>(pattern.Parameters) ?? [],
                ArchiveIds = pattern.ArchiveIds?.ToList() ?? [],
                MemberCount = pattern.MemberCount,
                StateId = pattern.StateId,
                CaseId = pattern.CaseId,
                FirstDetectedDate = Offset(pattern.FirstDetectedDate),
                LastDetectedDate = Offset(pattern.LastDetectedDate),
                DecidedUser = pattern.DecidedUser,
                DecidedDate = pattern.DecidedDate.HasValue ? Offset(pattern.DecidedDate.Value) : null
            };
            dto.Sentence = string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentencePattern], kind ?? "Pattern",
                anchor?.Key, anchor?.Value, pattern.MemberCount, pattern.Score, dto.WindowFrom, dto.WindowTo);

            return dto;
        }

        public static GraphCommunityDto ToCommunity(GraphCommunity community, IStringLocalizer strings)
        {
            return new GraphCommunityDto
            {
                Id = community.Id,
                Size = community.Size,
                Density = community.Density,
                ConfirmedCount = community.ConfirmedCount,
                ClosedCount = community.ClosedCount,
                ConfirmedRate = community.ConfirmedRate,
                TenantRateRatio = community.TenantRateRatio,
                Label = community.Label,
                ComputedDate = Offset(community.ComputedDate),
                Sentence = community.TenantRateRatio.HasValue
                    ? string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceCommunityRate],
                        community.Id, community.Size, community.ConfirmedCount, community.ClosedCount,
                        community.TenantRateRatio.Value)
                    : string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceCommunityNoRate],
                        community.Id, community.Size, community.ConfirmedCount, community.ClosedCount)
            };
        }

        public static DateTimeOffset Offset(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }
    }
}
