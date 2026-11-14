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

namespace Jube.Dto.Options
{
    using System.ComponentModel;

    public enum GraphPatternKind
    {
        [Description("Fan-out")] FanOut = 1,
        [Description("Fan-in")] FanIn = 2,
        [Description("Gather-scatter")] GatherScatter = 3,
        [Description("Scatter-gather")] ScatterGather = 4,
        [Description("Temporal cycle")] TemporalCycle = 5,
        [Description("Pass-through")] PassThrough = 6,
        [Description("Smurfing, type one")] SmurfTypeOne = 7,
        [Description("Smurfing, type two")] SmurfTypeTwo = 8,
        [Description("Structuring window")] StructuringWindow = 9,
        [Description("Dense block")] DenseBlock = 10
    }
}
