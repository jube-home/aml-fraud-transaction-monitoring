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
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed record LogisticRegressionOptions
    {
        public double ValidationFraction { get; init; } = 0.2;
        public double TestFraction { get; init; } = 0.2;
        public int Seed { get; init; } = 1;
        public double RidgeLambda { get; init; }
        public LogisticClassWeighting ClassWeighting { get; init; } = LogisticClassWeighting.None;

        public LogisticMissingValuePolicy MissingValuePolicy { get; init; } =
            LogisticMissingValuePolicy.ImputeMedianWithIndicators;

        public bool Standardise { get; init; } = true;
        public bool Calibrate { get; init; } = true;
        public double? BaseRateOverride { get; init; }
        public CostMatrix? Costs { get; init; }
        public double? DecisionThreshold { get; init; }
        public int MaximumIterations { get; init; } = 50;
        public double Tolerance { get; init; } = 1e-8;

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

            if (!Finite(RidgeLambda) || RidgeLambda < 0)
            {
                throw new ArgumentException("The ridge strength must be finite and not negative.");
            }

            if (BaseRateOverride is { } rate && (!Finite(rate) || rate <= 0 || rate >= 1))
            {
                throw new ArgumentException("The base rate override must be strictly between 0 and 1.");
            }

            if (DecisionThreshold is { } threshold && (!Finite(threshold) || threshold < 0 || threshold > 1))
            {
                throw new ArgumentException("The decision threshold must be between 0 and 1.");
            }

            if (MaximumIterations < 1)
            {
                throw new ArgumentException("The maximum number of iterations must be at least 1.");
            }

            if (!Finite(Tolerance) || Tolerance <= 0)
            {
                throw new ArgumentException("The tolerance must be positive and finite.");
            }

            Costs?.MinimumRiskThreshold();
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}