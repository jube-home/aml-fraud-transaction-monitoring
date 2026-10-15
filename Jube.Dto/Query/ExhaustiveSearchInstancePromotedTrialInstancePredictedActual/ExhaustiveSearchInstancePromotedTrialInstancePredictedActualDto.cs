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

namespace Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
{
    public class ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto
    {
        [Description("Predicted value produced by the promoted neural network trial instance for one training row.")]
        public double Predicted { get; set; }

        [Description("Actual (observed) value for the same training row.")]
        public double Actual { get; set; }

        [Description("Prediction error for the row, calculated as Actual minus Predicted.")]
        public double Error { get; set; }

        [Description("Raw (reverse-normalised) value of the requested continuous variable for this row, when a " +
                     "variable was requested; null otherwise or when this row has no stored value for it.")]
        public double? Value { get; set; }
    }
}