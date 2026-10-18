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

namespace Jube.Analytics
{
    public readonly record struct CostMatrix(
        double FalseNegative,
        double FalsePositive,
        double TruePositive = 0d,
        double TrueNegative = 0d)
    {
        public double MinimumRiskThreshold()
        {
            foreach (var cost in new[] { FalseNegative, FalsePositive, TruePositive, TrueNegative })
            {
                if (!Numerics.IsFinite(cost))
                {
                    throw new ArgumentException("Costs must be finite.");
                }
            }

            var costOfFlagging = FalsePositive - TrueNegative;
            var costOfMissing = FalseNegative - TruePositive;

            if (costOfFlagging <= 0 || costOfMissing <= 0)
            {
                throw new ArgumentException(
                    "A false positive must cost more than a true negative and a false negative more than a true positive.");
            }

            return costOfFlagging / (costOfFlagging + costOfMissing);
        }
    }
}