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

using Jube.Data.Poco;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve
{
    internal static class ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveMapper
    {
        public static ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto ToDto(
            ExhaustiveSearchInstancePromotedTrialInstance source) =>
            new()
            {
                Score = FiniteNumber.Of(Math.Round(source.Score.GetValueOrDefault(), 2)),
                CreatedDate = source.CreatedDate.GetValueOrDefault()
            };

        public static List<ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto> ToDto(
            IEnumerable<ExhaustiveSearchInstancePromotedTrialInstance> source) =>
            source.Select(ToDto).ToList();
    }
}