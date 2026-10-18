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
    public sealed record MahalanobisAnomalyOptions
    {
        public double ValidationFraction { get; init; } = 0.2;
        public double TestFraction { get; init; } = 0.2;
        public int Seed { get; init; } = 1;
        public bool Standardise { get; init; } = true;
        public bool Calibrate { get; init; } = true;
        public double RidgeEpsilon { get; init; } = 1e-6;
        public double HighConditionNumberThreshold { get; init; } = 1e6;

        internal void Validate()
        {
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

            if (!Finite(RidgeEpsilon) || RidgeEpsilon <= 0)
            {
                throw new ArgumentException("The ridge epsilon must be positive and finite.");
            }

            if (!Finite(HighConditionNumberThreshold) || HighConditionNumberThreshold <= 1)
            {
                throw new ArgumentException("The high condition number threshold must be finite and above 1.");
            }
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}