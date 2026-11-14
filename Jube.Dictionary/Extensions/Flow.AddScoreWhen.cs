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

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Adds the weight to the pipeline's score only if the condition is true.")]
        public static Flow<T> AddScoreWhen<T>(this Flow<T> flow,
            [Description("The condition that must hold for the score to change.")] bool condition,
            [Description("How much to add to the score; negative subtracts.")] double weight)
        {
            return flow.IsDecided || !condition ? flow : flow.WithScore(flow.Score + weight);
        }
    }
}
