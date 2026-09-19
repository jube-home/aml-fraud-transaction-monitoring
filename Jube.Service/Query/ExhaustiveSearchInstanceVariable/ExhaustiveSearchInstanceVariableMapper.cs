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

using Jube.Dto.Query.ExhaustiveSearchInstanceVariable;

namespace Jube.Service.Query.ExhaustiveSearchInstanceVariable
{
    internal static class ExhaustiveSearchInstanceVariableMapper
    {
        public static ExhaustiveSearchInstanceVariableDto ToDto(
            global::Jube.Data.Query.GetExhaustiveSearchInstanceVariableQuery.Dto source) => new()
        {
            Id = source.Id,
            Mean = FiniteNumber.Of(source.Mean),
            StandardDeviation = FiniteNumber.Of(source.StandardDeviation),
            Name = source.Name,
            Kurtosis = FiniteNumber.Of(source.Kurtosis),
            Skewness = FiniteNumber.Of(source.Skewness),
            Maximum = FiniteNumber.Of(source.Maximum),
            Minimum = FiniteNumber.Of(source.Minimum),
            Iqr = FiniteNumber.Of(source.Iqr),
            NormalisationType = source.NormalisationType,
            DistinctValues = source.DistinctValues,
            Correlation = FiniteNumber.Of(source.Correlation),
            CorrelationAbsRank = source.CorrelationAbsRank,
            HistogramValues = source.HistogramValues.Select(h => new ExhaustiveSearchInstanceVariableHistogramValueDto
            {
                Frequency = h.Frequency,
                Bin = FiniteNumber.Of(h.Bin)
            }).ToList()
        };

        public static List<ExhaustiveSearchInstanceVariableDto> ToDto(
            IEnumerable<global::Jube.Data.Query.GetExhaustiveSearchInstanceVariableQuery.Dto> source) =>
            source.Select(ToDto).ToList();
    }
}