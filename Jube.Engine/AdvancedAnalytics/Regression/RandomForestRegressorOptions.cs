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

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed record RandomForestRegressorOptions
    {
        public int TreeCount { get; init; } = 100;
        public int MaxDepth { get; init; } = 6;
        public int MinimumLeafSize { get; init; } = 5;
        public double FeatureFraction { get; init; }
        public double SampleFraction { get; init; } = 1d;
        public double ValidationFraction { get; init; } = 0.2;
        public double TestFraction { get; init; } = 0.2;
        public int Seed { get; init; } = 1;

        internal void Validate()
        {
            if (TreeCount < 1)
            {
                throw new ArgumentException("There must be at least one tree.");
            }

            if (MaxDepth < 1)
            {
                throw new ArgumentException("Maximum depth must be at least 1.");
            }

            if (MinimumLeafSize < 1)
            {
                throw new ArgumentException("Minimum leaf size must be at least 1.");
            }

            if (!double.IsFinite(FeatureFraction) || FeatureFraction < 0 || FeatureFraction > 1)
            {
                throw new ArgumentException(
                    "Feature fraction must be between 0 (the one-third default) and 1.");
            }

            if (!double.IsFinite(SampleFraction) || SampleFraction <= 0 || SampleFraction > 1)
            {
                throw new ArgumentException("Sample fraction must be greater than 0 and at most 1.");
            }

            if (!double.IsFinite(ValidationFraction) || ValidationFraction < 0 || ValidationFraction >= 1)
            {
                throw new ArgumentException("The validation fraction must be at least 0 and below 1.");
            }

            if (!double.IsFinite(TestFraction) || TestFraction < 0 || TestFraction >= 1)
            {
                throw new ArgumentException("The test fraction must be at least 0 and below 1.");
            }

            if (ValidationFraction + TestFraction > 0.9)
            {
                throw new ArgumentException("The validation and test fractions together must not exceed 0.9.");
            }
        }
    }
}