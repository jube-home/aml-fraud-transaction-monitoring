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
using Jube.Engine.AdvancedAnalytics.Clustering.Models;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    public sealed class MeanShiftClustererOptions
    {
        public double Bandwidth { get; set; } = 2.0;
        public MeanShiftKernel Kernel { get; set; } = MeanShiftKernel.Uniform;
        public int MaxNeighbours { get; set; }
        public int MaximumIterations { get; set; } = 100;
        public double Tolerance { get; set; } = 1e-3;
        public bool Standardise { get; set; } = true;
        public double ValidationFraction { get; set; } = 0.2;
        public double TestFraction { get; set; } = 0.2;
        public int Seed { get; set; } = 1;

        public void Validate()
        {
            if (!double.IsFinite(Bandwidth) || Bandwidth <= 0)
            {
                throw new ArgumentException("Bandwidth must be finite and greater than zero.");
            }

            if (!Enum.IsDefined(typeof(MeanShiftKernel), Kernel))
            {
                throw new ArgumentException("Kernel must be one of the recognised kernel functions.");
            }

            if (MaxNeighbours < 0)
            {
                throw new ArgumentException("Maximum neighbours must not be negative.");
            }

            if (MaximumIterations < 1)
            {
                throw new ArgumentException("Maximum iterations must be at least 1.");
            }

            if (!double.IsFinite(Tolerance) || Tolerance < 0)
            {
                throw new ArgumentException("Tolerance must be finite and not negative.");
            }

            foreach (var fraction in new[] { ValidationFraction, TestFraction })
            {
                if (!double.IsFinite(fraction) || fraction < 0 || fraction >= 1)
                {
                    throw new ArgumentException("Validation and test fractions must be between 0 and 1 (exclusive).");
                }
            }

            if (ValidationFraction + TestFraction > 0.9)
            {
                throw new ArgumentException("Validation and test fractions together must not exceed 0.9.");
            }
        }
    }
}