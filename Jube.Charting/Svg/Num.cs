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

using System.Globalization;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Svg
{
    internal static class Num
    {
        public static string F(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ChartingException("A non-finite number reached the SVG writer.");
            }

            var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);

            if (rounded == 0)
            {
                return "0";
            }

            return rounded.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string Percent(double value)
        {
            return F(value * 100) + "%";
        }
    }
}