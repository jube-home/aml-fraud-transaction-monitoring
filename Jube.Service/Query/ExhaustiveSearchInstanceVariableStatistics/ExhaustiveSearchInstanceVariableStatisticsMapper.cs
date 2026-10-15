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

using Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics;

namespace Jube.Service.Query.ExhaustiveSearchInstanceVariableStatistics
{
    internal static class ExhaustiveSearchInstanceVariableStatisticsMapper
    {
        public static ExhaustiveSearchInstanceVariableStatisticsDto ToDto(
            Jube.Data.Query.GetExhaustiveSearchInstanceVariableStatisticsQuery.
                GetExhaustiveSearchInstanceVariableStatisticsQueryDto source)
        {
            return new ExhaustiveSearchInstanceVariableStatisticsDto
            {
                Id = source.Id,
                Name = source.Name,
                Population = new ExhaustiveSearchInstanceVariableStatisticsSummaryDto
                {
                    Mean = FiniteNumber.Of(source.Mean),
                    StandardDeviation = FiniteNumber.Of(source.StandardDeviation),
                    Minimum = FiniteNumber.Of(source.Minimum),
                    Maximum = FiniteNumber.Of(source.Maximum),
                    Iqr = FiniteNumber.Of(source.Iqr)
                },
                PopulationBins = Bins(source.Population),
                ClassificationBins = Bins(source.ClassificationBins),
                AnomalyBins = Bins(source.AnomalyBins),
                NegativeBins = Bins(source.AlignedNegative),
                PositiveBins = Bins(source.AlignedPositive),
                PrescriptionBins = Bins(source.PrescriptionBins),
                Classification = Summary(source.Classification),
                Anomaly = Summary(source.Anomaly),
                Prescription = Summary(source.Prescription)
            };
        }

        public static List<ExhaustiveSearchInstanceVariableStatisticsDto> ToDto(
            IEnumerable<Jube.Data.Query.GetExhaustiveSearchInstanceVariableStatisticsQuery.
                GetExhaustiveSearchInstanceVariableStatisticsQueryDto> source)
        {
            return source.Select(ToDto).ToList();
        }

        private static List<ExhaustiveSearchInstanceVariableStatisticsBinDto> Bins(
            IEnumerable<Jube.Data.Query.GetExhaustiveSearchInstanceVariableStatisticsQuery.HistogramBin>
                source)
        {
            return source.Select(b => new ExhaustiveSearchInstanceVariableStatisticsBinDto
            {
                Start = FiniteNumber.Of(b.Start), End = FiniteNumber.Of(b.End), Frequency = b.Frequency
            }).ToList();
        }

        private static ExhaustiveSearchInstanceVariableStatisticsSummaryDto? Summary(
            Jube.Data.Query.GetExhaustiveSearchInstanceVariableStatisticsQuery.Summary? source)
        {
            return source == null
                ? null
                : new ExhaustiveSearchInstanceVariableStatisticsSummaryDto
                {
                    Mean = FiniteNumber.Of(source.Mean),
                    StandardDeviation = FiniteNumber.Of(source.StandardDeviation),
                    Minimum = FiniteNumber.Of(source.Minimum),
                    Maximum = FiniteNumber.Of(source.Maximum),
                    Iqr = FiniteNumber.Of(source.Iqr)
                };
        }
    }
}