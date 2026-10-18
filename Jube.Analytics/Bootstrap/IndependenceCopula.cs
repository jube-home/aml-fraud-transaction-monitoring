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

using Jube.Analytics.Bootstrap.Interfaces;

namespace Jube.Analytics.Bootstrap
{
    internal sealed class IndependenceCopula : IBivariateCopula
    {
        public static readonly IndependenceCopula Instance = new();
        public string Family => "Independence";
        public double Parameter => 0d;

        public double LogDensity(double u, double v)
        {
            return 0d;
        }

        public double ConditionalCdf(double u, double v)
        {
            return v;
        }

        public double InverseConditionalCdf(double u, double w)
        {
            return w;
        }
    }
}