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

using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion
{
    using DataDto = global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceConfusionQuery.Dto;

    public static class ExhaustiveSearchInstancePromotedTrialInstanceConfusionMapper
    {
        public static ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto ToDto(DataDto source)
        {
            return new ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto
            {
                Id = source.Id,
                Score = FiniteNumber.Of(source.Score),
                FalsePositive = source.FalsePositive,
                TruePositive = source.TruePositive,
                FalseNegative = source.FalseNegative,
                TrueNegative = source.TrueNegative,
                TableTotal = source.TableTotal,
                PositiveRowTotal = source.PositiveRowTotal,
                PositiveColumnTotal = source.PositiveColumnTotal,
                NegativeRowTotal = source.NegativeRowTotal,
                NegativeColumnTotal = source.NegativeColumnTotal,
                PositiveRowTableTotal = FiniteNumber.Of(source.PositiveRowTableTotal),
                PositiveColumnTableTotal = FiniteNumber.Of(source.PositiveColumnTableTotal),
                NegativeRowTableTotal = FiniteNumber.Of(source.NegativeRowTableTotal),
                NegativeColumnTableTotal = FiniteNumber.Of(source.NegativeColumnTableTotal),
                TruePositiveRowTotal = FiniteNumber.Of(source.TruePositiveRowTotal),
                TruePositiveColumnTotal = FiniteNumber.Of(source.TruePositiveColumnTotal),
                TruePositiveTableTotal = FiniteNumber.Of(source.TruePositiveTableTotal),
                FalsePositiveRowTotal = FiniteNumber.Of(source.FalsePositiveRowTotal),
                FalsePositiveColumnTotal = FiniteNumber.Of(source.FalsePositiveColumnTotal),
                FalsePositiveTableTotal = FiniteNumber.Of(source.FalsePositiveTableTotal),
                FalseNegativeRowTotal = FiniteNumber.Of(source.FalseNegativeRowTotal),
                FalseNegativeColumnTotal = FiniteNumber.Of(source.FalseNegativeColumnTotal),
                FalseNegativeTableTotal = FiniteNumber.Of(source.FalseNegativeTableTotal),
                TrueNegativeRowTotal = FiniteNumber.Of(source.TrueNegativeRowTotal),
                TrueNegativeColumnTotal = FiniteNumber.Of(source.TrueNegativeColumnTotal),
                TrueNegativeTableTotal = FiniteNumber.Of(source.TrueNegativeTableTotal)
            };
        }
    }
}