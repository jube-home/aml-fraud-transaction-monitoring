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
using Accord.Statistics.Distributions.Univariate;

namespace Jube.Engine.Exhaustive.Statistics
{
    internal static class TriangularDistributionFactory
    {
        internal static TriangularDistribution BuildTriangularDistribution(double min, double max, double mode)
        {
            if (!double.IsFinite(min) || !double.IsFinite(max) || !double.IsFinite(mode) || !(min < max))
            {
                return null;
            }

            return new TriangularDistribution(min, max, Math.Clamp(mode, min, max));
        }
    }
}