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

namespace Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceListQuery
{
    [Description("One trial attempt of an Exhaustive Search Instance, whether or not it went on to be promoted: " +
                 "either a Neural Network topology evolution step or a challenger model family training attempt.")]
    public class ExhaustiveSearchInstanceTrialInstanceListQueryDto
    {
        [Description("Server-assigned integer identifier of the trial instance.")]
        public int Id { get; set; }

        [Description("UTC date and time the trial instance was created.")]
        public DateTime CreatedDate { get; set; }

        [Description("The model kind this trial attempted: ExhaustiveTopologyNetwork for a Neural Network " +
                     "topology evolution step, or the challenger family name (e.g. RandomForest) otherwise. " +
                     "Null for a trial recorded before this was captured.")]
        public string? Kind { get; set; }

        [Description("Score achieved by this trial on the testing split. Null for a trial recorded before this " +
                     "was captured.")]
        public double? Score { get; set; }

        [Description("True when this trial went on to be promoted (is, or was, the best trial at the time).")]
        public bool Promoted { get; set; }
    }
}