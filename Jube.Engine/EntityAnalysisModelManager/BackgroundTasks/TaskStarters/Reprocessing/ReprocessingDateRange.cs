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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Reprocessing
{
    using System;

    public static class ReprocessingDateRange
    {
        public static DateTime? StartDate(DateTime lastReferenceDate, string intervalType, int intervalValue)
        {
            if (intervalValue < 0)
            {
                return null;
            }

            try
            {
                return intervalType switch
                {
                    "d" => lastReferenceDate.AddDays(-intervalValue),
                    "h" => lastReferenceDate.AddHours(-intervalValue),
                    "n" => lastReferenceDate.AddMinutes(-intervalValue),
                    "s" => lastReferenceDate.AddSeconds(-intervalValue),
                    "m" => lastReferenceDate.AddMonths(-intervalValue),
                    "y" => lastReferenceDate.AddYears(-intervalValue),
                    _ => null
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTime.MinValue;
            }
        }

        public static bool Sampled(double samplePercentage, double draw)
        {
            return draw * 100 < samplePercentage;
        }
    }
}