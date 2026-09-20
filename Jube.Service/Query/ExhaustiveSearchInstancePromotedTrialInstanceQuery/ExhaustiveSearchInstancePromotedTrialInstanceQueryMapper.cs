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

using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery
{
    internal static class ExhaustiveSearchInstancePromotedTrialInstanceQueryMapper
    {
        public static ExhaustiveSearchInstancePromotedTrialInstanceQueryDto ToDto(
            global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceQuery.Dto source) => new()
        {
            Id = source.Id,
            ExhaustiveSearchInstanceTrialInstanceId = source.ExhaustiveSearchInstanceTrialInstanceId,
            Active = source.Active,
            Score = FiniteNumber.Of(source.Score),
            TopologyComplexity = source.TopologyComplexity,
            Json = source.Json,
            CreatedDate = source.CreatedDate
        };

        public static List<ExhaustiveSearchInstancePromotedTrialInstanceQueryDto> ToDto(
            IEnumerable<global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceQuery.Dto> source) =>
            source.Select(ToDto).ToList();
    }
}