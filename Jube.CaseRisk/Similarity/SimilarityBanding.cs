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

namespace Jube.CaseRisk.Similarity
{
    public static class SimilarityBanding
    {
        public static string? Band(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return null;
            }

            if (value == 0)
            {
                return "0";
            }

            var step = Math.Floor(Math.Log10(Math.Abs(value)) * 2) / 2;

            return (value < 0 ? "-e" : "+e") + step.ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static string? Share(double value)
        {
            return value is >= 0 and <= 1
                ? "q" + Math.Min(3, (int)Math.Floor(value * 4)).ToString(CultureInfo.InvariantCulture)
                : Band(value);
        }
    }
}
