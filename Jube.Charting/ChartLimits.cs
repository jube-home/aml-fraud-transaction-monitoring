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

using Jube.Charting.Exceptions;

namespace Jube.Charting
{
    public static class ChartLimits
    {
        public const int MaxPoints = 50_000;
        public const int MaxSeries = 64;
        public const int MaxScoredObservations = 2_000_000;
        public const int MaxCategories = 2_000;
        public const int MaxHeatmapCells = 40_000;
        public const int MaxOutputCharacters = 4_000_000;
        public const int MaxTextCharacters = 2_000;
        public const int InteractivePointsPerSeries = 150;
        public const int InteractiveMarksPerChart = 1_200;

        internal static void Points(long count)
        {
            if (count > MaxPoints)
            {
                throw new ChartLimitException("point count", count, MaxPoints);
            }
        }

        internal static void Observations(long count)
        {
            if (count > MaxScoredObservations)
            {
                throw new ChartLimitException("scored observation count", count, MaxScoredObservations);
            }
        }

        internal static void Series(long count)
        {
            if (count > MaxSeries)
            {
                throw new ChartLimitException("series count", count, MaxSeries);
            }
        }

        internal static void Categories(long count)
        {
            if (count > MaxCategories)
            {
                throw new ChartLimitException("category count", count, MaxCategories);
            }
        }

        internal static void Cells(long count)
        {
            if (count > MaxHeatmapCells)
            {
                throw new ChartLimitException("heatmap cell count", count, MaxHeatmapCells);
            }
        }

        internal static void Output(long characters)
        {
            if (characters > MaxOutputCharacters)
            {
                throw new ChartLimitException("output size in characters", characters, MaxOutputCharacters);
            }
        }
    }
}