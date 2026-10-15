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
    public sealed class LinearSupportVectorClassifierOptions
    {
        public double Complexity { get; set; } = 1.0;
        public bool Standardise { get; set; } = true;
        public double ValidationFraction { get; set; } = 0.2;
        public double TestFraction { get; set; } = 0.2;
        public int Seed { get; set; } = 1;
        public bool Calibrate { get; set; } = true;
        public double DecisionThreshold { get; set; } = 0.5;

        public void Validate()
        {
            if (!double.IsFinite(Complexity) || Complexity <= 0)
            {
                throw new ArgumentException("The complexity must be positive and finite.");
            }

            foreach (var fraction in new[] { ValidationFraction, TestFraction })
            {
                if (!double.IsFinite(fraction) || fraction < 0 || fraction >= 1)
                {
                    throw new ArgumentException("Validation and test fractions must be at least 0 and below 1.");
                }
            }

            if (ValidationFraction + TestFraction > 0.9)
            {
                throw new ArgumentException("Validation and test fractions together must not exceed 0.9.");
            }

            if (!double.IsFinite(DecisionThreshold) || DecisionThreshold < 0 || DecisionThreshold > 1)
            {
                throw new ArgumentException("The decision threshold must be between 0 and 1.");
            }
        }
    }
}