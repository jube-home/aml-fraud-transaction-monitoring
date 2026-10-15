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

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public sealed record NaiveBayesClassifierOptions
    {
        public double ValidationFraction { get; init; } = 0.2;
        public double TestFraction { get; init; } = 0.2;
        public int Seed { get; init; } = 1;
        public double VarianceRegularisation { get; init; } = 1e-9;
        public bool Calibrate { get; init; } = true;
        public double DecisionThreshold { get; init; } = 0.5;

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

            if (!Finite(VarianceRegularisation) || VarianceRegularisation < 0)
            {
                throw new ArgumentException("The variance regularisation must be finite and not negative.");
            }

            if (!Finite(DecisionThreshold) || DecisionThreshold < 0 || DecisionThreshold > 1)
            {
                throw new ArgumentException("The decision threshold must be between 0 and 1.");
            }
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}