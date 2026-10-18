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

using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;

namespace Jube.Engine.AdvancedAnalytics.Sequences
{
    internal static class AdvancedAnalyticsSequenceLayout
    {
        public static int[] CopyLengths(int[] sequenceLengths, long totalElements, string valueName)
        {
            if (sequenceLengths == null || sequenceLengths.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("A sequence set needs at least one sequence.");
            }

            long sum = 0;

            for (var i = 0; i < sequenceLengths.Length; i++)
            {
                if (sequenceLengths[i] < 1)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Sequence {i} has length {sequenceLengths[i]}; empty sequences are not allowed.");
                }

                sum += sequenceLengths[i];
            }

            if (sum != totalElements)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Sequence lengths add up to {sum} but there are {totalElements} {valueName}.");
            }

            return (int[])sequenceLengths.Clone();
        }

        public static int[] Offsets(int[] lengths)
        {
            var offsets = new int[lengths.Length];
            var running = 0;

            for (var i = 0; i < lengths.Length; i++)
            {
                offsets[i] = running;
                running += lengths[i];
            }

            return offsets;
        }
    }
}