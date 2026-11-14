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

namespace Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceListQuery
{
    [Description("Detail of one Exhaustive trial attempt: enough to render its model diagram and, for a " +
                 "challenger family, its evidence.")]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceTrialInstanceDetailDto
    {
        [Description("Server-assigned integer identifier of the trial instance.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("The model kind this trial attempted: ExhaustiveTopologyNetwork for a Neural Network " +
                     "topology evolution step, or the challenger family name otherwise.")]
        [Key(1)]
        public string? Kind { get; set; }

        [Description("Score achieved by this trial on the testing split.")]
        [Key(2)]
        public double? Score { get; set; }

        [Description("True when a model diagram can be drawn for this trial (its serialised model was stored).")]
        [Key(3)]
        public bool HasDiagram { get; set; }

        [Description("Parsed evidence object for a challenger family trial (the same shape Advanced Analytics " +
                     "evidence uses); null for a Neural Network topology trial, which has no stored evidence.")]
        [Key(4)]
        public object? Evidence { get; set; }
    }
}