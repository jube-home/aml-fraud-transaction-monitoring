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

namespace Jube.Validations
{
    public static class IntervalLimits
    {
        public const int MaxFetchLimit = 1_000_000;

        public static int Max(string? interval)
        {
            return interval switch
            {
                "s" => 2_000_000_000,
                "n" => 52_560_000,
                "h" => 876_000,
                "d" => 36_500,
                "m" => 1_200,
                "y" => 100,
                _ => 36_500
            };
        }

        public static int Max(char interval)
        {
            return Max(interval.ToString());
        }
    }
}