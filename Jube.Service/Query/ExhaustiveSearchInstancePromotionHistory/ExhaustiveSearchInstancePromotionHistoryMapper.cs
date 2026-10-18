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

using Jube.Data.Query.GetExhaustiveSearchInstancePromotionHistoryQuery;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotionHistory;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotionHistory
{
    internal static class ExhaustiveSearchInstancePromotionHistoryMapper
    {
        public static ExhaustiveSearchInstancePromotionHistoryDto ToDto(
            GetExhaustiveSearchInstancePromotionHistoryQueryDto source)
        {
            return new ExhaustiveSearchInstancePromotionHistoryDto
            {
                Id = source.Id,
                ExhaustiveSearchInstanceTrialInstanceId = source.ExhaustiveSearchInstanceTrialInstanceId,
                Score = FiniteNumber.Of(source.Score),
                TopologyComplexity = source.TopologyComplexity,
                CreatedDate = source.CreatedDate,
                Active = source.Active,
                TrialInstances = source.TrialInstances,
                TrialInstancesToDate = source.TrialInstancesToDate
            };
        }

        public static List<ExhaustiveSearchInstancePromotionHistoryDto> ToDto(
            IEnumerable<GetExhaustiveSearchInstancePromotionHistoryQueryDto> source)
        {
            return source.Select(ToDto).ToList();
        }
    }
}