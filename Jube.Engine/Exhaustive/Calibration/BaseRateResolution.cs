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

namespace Jube.Engine.Exhaustive.Calibration
{
    public sealed class BaseRateResolution(double? rate, string source)
    {
        public const string SourceOverride = "Override";
        public const string SourcePopulationCount = "PopulationCount";
        public const string SourceAnomalyFlaggedFraction = "AnomalyFlaggedFraction";
        public const string SourceSamplePrior = "SamplePrior";
        public double? Rate { get; } = rate;
        public string Source { get; } = source;
        public bool IsUsable => Rate is > 0 and < 1;
        public static BaseRateResolution Unavailable { get; } = new(null, SourceSamplePrior);

        public static BaseRateResolution FromCounts(long positives, long negatives)
        {
            var total = positives + negatives;

            if (positives <= 0 || negatives <= 0 || total <= 0)
            {
                return Unavailable;
            }

            return new BaseRateResolution(positives / (double)total, SourcePopulationCount);
        }

        public static BaseRateResolution FromAnomalyLabels(double[] outputs)
        {
            if (outputs == null || outputs.Length == 0)
            {
                return Unavailable;
            }

            var flagged = 0d;

            foreach (var output in outputs)
            {
                flagged += output;
            }

            var fraction = flagged / outputs.Length;

            return fraction is > 0 and < 1
                ? new BaseRateResolution(fraction, SourceAnomalyFlaggedFraction)
                : Unavailable;
        }

        public static BaseRateResolution WithOverride(double? overrideRate, BaseRateResolution resolved)
        {
            return overrideRate is > 0 and < 1
                ? new BaseRateResolution(overrideRate, SourceOverride)
                : resolved ?? Unavailable;
        }
    }
}