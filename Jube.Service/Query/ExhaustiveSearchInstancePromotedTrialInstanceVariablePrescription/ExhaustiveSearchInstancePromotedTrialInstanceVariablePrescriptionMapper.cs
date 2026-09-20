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

using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;

namespace Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription
{
    internal static class ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionMapper
    {
        public static ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto ToDto(
            global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery.Dto
                source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            VariableMean = FiniteNumber.Of(source.VariableMean),
            VariableStandardDeviation = FiniteNumber.Of(source.VariableStandardDeviation),
            VariableMaximum = FiniteNumber.Of(source.VariableMaximum),
            VariableMinimum = FiniteNumber.Of(source.VariableMinimum),
            PrescriptionMean = FiniteNumber.Of(source.PrescriptionMean),
            PrescriptionStandardDeviation = FiniteNumber.Of(source.PrescriptionStandardDeviation),
            PrescriptionMaximum = FiniteNumber.Of(source.PrescriptionMaximum),
            PrescriptionMinimum = FiniteNumber.Of(source.PrescriptionMinimum),
            Sensitivity = FiniteNumber.Of(source.Sensitivity)
        };

        public static List<ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto> ToDto(
            IEnumerable<global::Jube.Data.Query.
                    GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery.Dto>
                source) => source.Select(ToDto).ToList();
    }
}