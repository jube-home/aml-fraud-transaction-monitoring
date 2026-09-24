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
        public static double NthRoot(this double @this, double degree)
        {
            if (double.IsNaN(@this) || !double.IsFinite(degree) || degree == 0)
            {
                return double.NaN;
            }

            if (@this < 0)
            {
                var isOddInteger = Math.Abs(degree % 2) == 1;
                return isOddInteger ? CalculationSupport.Finite(-Math.Pow(-@this, 1d / degree)) : double.NaN;
            }

            return CalculationSupport.Finite(Math.Pow(@this, 1d / degree));
        }
    }
}