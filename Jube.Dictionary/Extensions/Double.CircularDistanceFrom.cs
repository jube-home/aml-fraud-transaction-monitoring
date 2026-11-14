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
        [Description("The shortest distance between the value and the other around a circle of the period, for hours of the day or bearings; NaN when the period is not positive.")]
        public static double CircularDistanceFrom(this double @this,
            [Description("The other point on the circle.")] double other,
            [Description("The length of the circle, for example 24 for hours or 360 for degrees.")] double period)
        {
            if (double.IsNaN(@this) || double.IsNaN(other) || !(period > 0))
            {
                return double.NaN;
            }

            var difference = Math.Abs(@this - other) % period;

            return Math.Min(difference, period - difference);
        }
    }
}