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

namespace Jube.Test.Charting.Performance
{
    public static class PerformanceData
    {
        public static readonly double[] Scores =
        [
            0.95, 0.9, 0.9, 0.85, 0.8, 0.75, 0.7, 0.7, 0.65, 0.6, 0.55, 0.5, 0.5, 0.45, 0.4, 0.35, 0.3, 0.3, 0.25, 0.2,
            0.15, 0.15, 0.1, 0.1, 0.05, 0.05, 0.92, 0.62, 0.58, 0.42, 0.38, 0.33, 0.28, 0.22, 0.18, 0.12, 0.08, 0.03,
            0.88, 0.48
        ];

        public static readonly bool[] Actual =
        [
            true, true, false, true, false, true, true, false, false, true, false, true, false, false, true, false,
            false, false, false, false, false, false, false, false, false, false, true, true, false, false, true,
            false, false, false, false, false, false, false, true, false
        ];
    }
}