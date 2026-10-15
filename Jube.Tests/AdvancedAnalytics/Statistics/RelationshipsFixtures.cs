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

using System;
using FluentAssertions;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public static class RelationshipsFixtures
    {
        public static readonly double[] AnscombeOneX = [10, 8, 13, 9, 11, 14, 6, 4, 12, 7, 5];

        public static readonly double[] AnscombeOneY =
            [8.04, 6.95, 7.58, 8.81, 8.33, 9.96, 7.24, 4.26, 10.84, 4.82, 5.68];

        public static readonly double[][] LongleyPredictors =
        [
            [83.0, 234289, 2356, 1590, 107608, 1947],
            [88.5, 259426, 2325, 1456, 108632, 1948],
            [88.2, 258054, 3682, 1616, 109773, 1949],
            [89.5, 284599, 3351, 1650, 110929, 1950],
            [96.2, 328975, 2099, 3099, 112075, 1951],
            [98.1, 346999, 1932, 3594, 113270, 1952],
            [99.0, 365385, 1870, 3547, 115094, 1953],
            [100.0, 363112, 3578, 3350, 116219, 1954],
            [101.2, 397469, 2904, 3048, 117388, 1955],
            [104.6, 419180, 2822, 2857, 118734, 1956],
            [108.4, 442769, 2936, 2798, 120445, 1957],
            [110.8, 444546, 4681, 2637, 121950, 1958],
            [112.6, 482704, 3813, 2552, 123366, 1959],
            [114.2, 502601, 3931, 2514, 125368, 1960],
            [115.7, 518173, 4806, 2572, 127852, 1961],
            [116.9, 554894, 4007, 2827, 130081, 1962]
        ];

        public static readonly double[] LongleyEmployed =
        [
            60323, 61122, 60171, 61187, 63221, 63639, 64989, 63761, 66019, 67857, 68169, 66513, 68655, 69564, 69331,
            70551
        ];

        public static readonly string[] LongleyNames =
            ["deflator", "gnp", "unemployed", "armed", "population", "year"];

        public static void Rejects(Action action, string parameter)
        {
            action.Should().Throw<ArgumentException>().Where(e => e.ParamName == parameter);
        }
    }
}