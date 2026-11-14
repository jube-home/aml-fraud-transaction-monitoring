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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription
{
    [Description("One variable of a promoted trial instance of an Exhaustive Search Instance, with the " +
                 "statistics of the variable itself, the statistics of its prescription and its sensitivity.")]
    [MessagePackObject]
    public class ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto
    {
        [Description("Integer Id of the Exhaustive Search Instance Variable.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Name of the variable.")] [Key(1)] public string? Name { get; set; }
        [Description("Mean of the variable.")] [Key(2)] public double VariableMean { get; set; }

        [Description("Standard deviation of the variable.")]
        [Key(3)]
        public double VariableStandardDeviation { get; set; }

        [Description("Maximum of the variable.")]
        [Key(4)]
        public double VariableMaximum { get; set; }

        [Description("Minimum of the variable.")]
        [Key(5)]
        public double VariableMinimum { get; set; }

        [Description("Mean of the prescription, zero when there is none.")]
        [Key(6)]
        public double PrescriptionMean { get; set; }

        [Description("Standard deviation of the prescription, zero when there is none.")]
        [Key(7)]
        public double PrescriptionStandardDeviation { get; set; }

        [Description("Maximum of the prescription, zero when there is none.")]
        [Key(8)]
        public double PrescriptionMaximum { get; set; }

        [Description("Minimum of the prescription, zero when there is none.")]
        [Key(9)]
        public double PrescriptionMinimum { get; set; }

        [Description("Sensitivity of the variable, zero when there is none. Results are ordered by this, descending.")]
        [Key(10)]
        public double Sensitivity { get; set; }

        [Description("True for the prescription simulated toward the positive class, false for the negative class, " +
                     "null when there is no prescription.")]
        [Key(11)]
        public bool? Positive { get; set; }

        [Description(
            "Integer Id of the promoted trial instance variable, used to fetch its prescription histogram chart.")]
        [Key(12)]
        public int PromotedTrialInstanceVariableId { get; set; }

        [Description("Histogram bins of the prescription, empty when there is none.")]
        [Key(13)]
        public List<ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionHistogramValueDto>
            HistogramValues { get; set; } = [];
    }
}