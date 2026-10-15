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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed class KernelRidgeRegressorOptions
    {
        public double Complexity { get; set; } = 1.0;
        public KernelRidgeRegressorKernel Kernel { get; set; } = KernelRidgeRegressorKernel.Gaussian;
        public double Gamma { get; set; }
        public int Degree { get; set; } = 3;
        public double ValidationFraction { get; set; } = 0.2;
        public double TestFraction { get; set; } = 0.2;
        public int Seed { get; set; } = 1;

        public void Validate()
        {
            if (!double.IsFinite(Complexity) || Complexity <= 0)
            {
                throw new ArgumentException("The complexity must be finite and greater than 0.");
            }

            if (!Enum.IsDefined(Kernel))
            {
                throw new ArgumentException("The kernel is not one of the supported kinds.");
            }

            if (!double.IsFinite(Gamma) || Gamma < 0)
            {
                throw new ArgumentException(
                    "Gamma must be finite and not negative; 0 means an automatic gamma of one over the number of predictors.");
            }

            if (Degree < 1 || Degree > 10)
            {
                throw new ArgumentException("The polynomial degree must be between 1 and 10.");
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
        }
    }
}