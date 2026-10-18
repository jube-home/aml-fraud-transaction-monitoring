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

namespace Jube.Engine.AdvancedAnalytics.Anomaly
{
    public sealed record IsolationForestAnomalyOptions
    {
        public int TreeCount { get; init; } = 100;
        public int SubsampleSize { get; init; } = 256;
        public double ValidationFraction { get; init; } = 0.2;
        public double TestFraction { get; init; } = 0.2;
        public int Seed { get; init; } = 1;
        public bool Calibrate { get; init; } = true;

        internal void Validate()
        {
            if (TreeCount < 1)
            {
                throw new ArgumentException("There must be at least one tree.");
            }

            if (SubsampleSize < 2)
            {
                throw new ArgumentException("The subsample size must be at least 2.");
            }

            if (!Finite(ValidationFraction) || ValidationFraction < 0 || ValidationFraction >= 1)
            {
                throw new ArgumentException("The validation fraction must be at least 0 and below 1.");
            }

            if (!Finite(TestFraction) || TestFraction < 0 || TestFraction >= 1)
            {
                throw new ArgumentException("The test fraction must be at least 0 and below 1.");
            }

            if (ValidationFraction + TestFraction > 0.9)
            {
                throw new ArgumentException("The validation and test fractions together must not exceed 0.9.");
            }
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}