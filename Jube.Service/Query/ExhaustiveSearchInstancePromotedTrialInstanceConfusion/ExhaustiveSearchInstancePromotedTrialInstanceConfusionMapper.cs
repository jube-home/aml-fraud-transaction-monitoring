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
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion
{
    public static class ExhaustiveSearchInstancePromotedTrialInstanceConfusionMapper
    {
        public static ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto ToDto(
            ExhaustiveSearchInstancePromotedTrialInstance? source)
        {
            var truePositive = source?.TruePositive.GetValueOrDefault() ?? 0;
            var trueNegative = source?.TrueNegative.GetValueOrDefault() ?? 0;
            var falsePositive = source?.FalsePositive.GetValueOrDefault() ?? 0;
            var falseNegative = source?.FalseNegative.GetValueOrDefault() ?? 0;

            var tableTotal = truePositive + trueNegative + falsePositive + falseNegative;
            var positiveRowTotal = truePositive + falseNegative;
            var positiveColumnTotal = truePositive + falsePositive;
            var negativeRowTotal = falsePositive + trueNegative;
            var negativeColumnTotal = falseNegative + trueNegative;

            return new ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto
            {
                Id = source?.Id ?? 0,
                Score = FiniteNumber.Of(source?.Score.GetValueOrDefault() ?? 0),
                FalsePositive = falsePositive,
                TruePositive = truePositive,
                FalseNegative = falseNegative,
                TrueNegative = trueNegative,
                TableTotal = tableTotal,
                PositiveRowTotal = positiveRowTotal,
                PositiveColumnTotal = positiveColumnTotal,
                NegativeRowTotal = negativeRowTotal,
                NegativeColumnTotal = negativeColumnTotal,
                PositiveRowTableTotal = FiniteNumber.Of(Ratio(positiveRowTotal, tableTotal)),
                PositiveColumnTableTotal = FiniteNumber.Of(Ratio(positiveColumnTotal, tableTotal)),
                NegativeRowTableTotal = FiniteNumber.Of(Ratio(negativeRowTotal, tableTotal)),
                NegativeColumnTableTotal = FiniteNumber.Of(Ratio(negativeColumnTotal, tableTotal)),
                TruePositiveRowTotal = FiniteNumber.Of(Ratio(truePositive, positiveRowTotal)),
                TruePositiveColumnTotal = FiniteNumber.Of(Ratio(truePositive, positiveColumnTotal)),
                TruePositiveTableTotal = FiniteNumber.Of(Ratio(truePositive, tableTotal)),
                FalsePositiveRowTotal = FiniteNumber.Of(Ratio(falsePositive, negativeRowTotal)),
                FalsePositiveColumnTotal = FiniteNumber.Of(Ratio(falsePositive, positiveColumnTotal)),
                FalsePositiveTableTotal = FiniteNumber.Of(Ratio(falsePositive, tableTotal)),
                FalseNegativeRowTotal = FiniteNumber.Of(Ratio(falseNegative, positiveRowTotal)),
                FalseNegativeColumnTotal = FiniteNumber.Of(Ratio(falseNegative, negativeColumnTotal)),
                FalseNegativeTableTotal = FiniteNumber.Of(Ratio(falseNegative, tableTotal)),
                TrueNegativeRowTotal = FiniteNumber.Of(Ratio(trueNegative, negativeRowTotal)),
                TrueNegativeColumnTotal = FiniteNumber.Of(Ratio(trueNegative, negativeColumnTotal)),
                TrueNegativeTableTotal = FiniteNumber.Of(Ratio(trueNegative, tableTotal))
            };
        }

        private static double Ratio(double numerator, int denominator)
        {
            return denominator == 0 ? 0d : Math.Round(numerator / denominator, 2);
        }
    }
}