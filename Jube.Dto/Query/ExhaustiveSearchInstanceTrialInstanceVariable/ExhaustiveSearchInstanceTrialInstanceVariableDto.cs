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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceVariable
{
    [Description("One variable of the promoted trial instance of an Exhaustive Search Instance, with its " +
                 "normalisation statistics.")]
    public class ExhaustiveSearchInstanceTrialInstanceVariableDto
    {
        [Description("Integer Id of the Exhaustive Search Instance Variable.")]
        public int Id { get; set; }

        [Description("Name of the variable.")] public string? Name { get; set; }
        [Description("Mean of the variable.")] public double Mean { get; set; }

        [Description("Maximum of the variable.")]
        public double Maximum { get; set; }

        [Description("Minimum of the variable.")]
        public double Minimum { get; set; }

        [Description("Standard deviation of the variable.")]
        public double StandardDeviation { get; set; }

        [Description("Integer Id of the normalisation type of the variable.")]
        public byte NormalisationTypeId { get; set; }

        [Description("True when the maximum and minimum of the variable sum to zero.")]
        public bool EmptyRange { get; set; }

        [Description("Position of the variable in the trial instance sequence. Results are ordered by the " +
                     "trial instance variable sequence.")]
        public int VariableSequence { get; set; }

        [Description("Integer Id of the processing type of the variable.")]
        public int ProcessingTypeId { get; set; }

        [Description("Source field of a categorical indicator variable, null for an ordinary variable.")]
        public string? CategoricalSourceName { get; set; }

        [Description(
            "Kind of a categorical indicator variable: 1 level, 2 other, 3 missing, null for an ordinary variable.")]
        public byte? CategoricalKind { get; set; }

        [Description("Level label of a categorical indicator variable, null for an ordinary variable.")]
        public string? CategoricalLevel { get; set; }

        [Description("Label of the reference level of the categorical source field, null for an ordinary variable.")]
        public string? CategoricalReferenceLabel { get; set; }
    }
}