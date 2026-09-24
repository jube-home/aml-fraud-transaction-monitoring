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
        public static double ConvertUnits(this double @this, string? fromUnit, string? toUnit)
        {
            var from = (fromUnit ?? "").Trim().ToLowerInvariant();
            var to = (toUnit ?? "").Trim().ToLowerInvariant();

            if (UnitSupport.Temperatures.Contains(from) && UnitSupport.Temperatures.Contains(to))
            {
                return UnitSupport.FromKelvin(UnitSupport.ToKelvin(@this, from), to);
            }

            if (!UnitSupport.Units.TryGetValue(from, out var source) ||
                !UnitSupport.Units.TryGetValue(to, out var target) ||
                source.Dimension != target.Dimension)
            {
                return double.NaN;
            }

            return CalculationSupport.Finite(@this * source.Factor / target.Factor);
        }
    }
}