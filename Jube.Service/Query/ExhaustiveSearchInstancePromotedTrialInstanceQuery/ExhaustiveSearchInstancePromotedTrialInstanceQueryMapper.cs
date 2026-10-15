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
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery
{
    internal static class ExhaustiveSearchInstancePromotedTrialInstanceQueryMapper
    {
        public static ExhaustiveSearchInstancePromotedTrialInstanceQueryDto ToDto(
            ExhaustiveSearchInstancePromotedTrialInstance source)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstanceQueryDto
            {
                Id = source.Id,
                ExhaustiveSearchInstanceTrialInstanceId = source.ExhaustiveSearchInstanceTrialInstanceId
                    .GetValueOrDefault(),
                Active = source.Active == 1,
                Score = FiniteNumber.Of(source.Score.GetValueOrDefault()),
                TopologyComplexity = source.TopologyComplexity.GetValueOrDefault(),
                Kind = source.KindId?.ToString(),
                Json = source.Json,
                CreatedDate = source.CreatedDate.GetValueOrDefault(),
                CalibrationMethod = source.CalibrationMethodId?.ToString(),
                CalibrationInput = source.CalibrationInputId?.ToString(),
                CalibrationSlope = Finite(source.CalibrationSlope),
                CalibrationIntercept = Finite(source.CalibrationIntercept),
                CalibrationEpsilon = Finite(source.CalibrationEpsilon),
                TrainingPrior = Finite(source.TrainingPrior),
                CalibrationPrior = Finite(source.CalibrationPrior),
                BaseRate = Finite(source.BaseRate),
                BaseRateSource = source.BaseRateSourceId?.ToString(),
                CalibrationSampleSize = source.CalibrationSampleSize,
                CalibrationStatus = source.CalibrationStatusId?.ToString(),
                CalibrationReason = source.CalibrationReason,
                CalibrationQualityJson = source.CalibrationQualityJson,
                CalibrationParametersJson = source.CalibrationParametersJson
            };
        }

        private static double? Finite(double? value)
        {
            return value.HasValue && double.IsFinite(value.Value) ? value : null;
        }

        public static List<ExhaustiveSearchInstancePromotedTrialInstanceQueryDto> ToDto(
            IEnumerable<ExhaustiveSearchInstancePromotedTrialInstance> source)
        {
            return source.Select(ToDto).ToList();
        }
    }
}