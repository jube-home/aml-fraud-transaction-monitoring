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

using System.ComponentModel;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription
{
    [Description("One variable of a promoted trial instance of an Exhaustive Search Instance, with the " +
                 "statistics of the variable itself, the statistics of its prescription and its sensitivity.")]
    public class ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto
    {
        [Description("Integer Id of the Exhaustive Search Instance Variable.")]
        public int Id { get; set; }

        [Description("Name of the variable.")] public string? Name { get; set; }

        [Description("Mean of the variable.")] public double VariableMean { get; set; }

        [Description("Standard deviation of the variable.")]
        public double VariableStandardDeviation { get; set; }

        [Description("Maximum of the variable.")]
        public double VariableMaximum { get; set; }

        [Description("Minimum of the variable.")]
        public double VariableMinimum { get; set; }

        [Description("Mean of the prescription, zero when there is none.")]
        public double PrescriptionMean { get; set; }

        [Description("Standard deviation of the prescription, zero when there is none.")]
        public double PrescriptionStandardDeviation { get; set; }

        [Description("Maximum of the prescription, zero when there is none.")]
        public double PrescriptionMaximum { get; set; }

        [Description("Minimum of the prescription, zero when there is none.")]
        public double PrescriptionMinimum { get; set; }

        [Description("Sensitivity of the variable, zero when there is none. Results are ordered by this, descending.")]
        public double Sensitivity { get; set; }
    }
}