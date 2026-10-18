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

using Jube.Charting.Interfaces;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Scales
{
    public sealed class LogScale : IScale
    {
        private readonly double logMax;
        private readonly double logMin;

        public LogScale(double domainMin, double domainMax, double rangeStart, double rangeEnd)
        {
            if (!(domainMin > 0) || !(domainMax > 0) || !double.IsFinite(domainMin) || !double.IsFinite(domainMax))
            {
                throw new ChartSpecificationException("A log scale needs a positive finite domain.");
            }

            if (Math.Abs(domainMin - domainMax) < 0.0001)
            {
                domainMin /= 10;
                domainMax *= 10;
            }

            DomainMin = domainMin;
            DomainMax = domainMax;
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
            logMin = Math.Log10(domainMin);
            logMax = Math.Log10(domainMax);
        }

        public double DomainMin { get; }
        public double DomainMax { get; }
        public double RangeStart { get; }
        public double RangeEnd { get; }

        public double Map(double value)
        {
            if (!(value > 0) || double.IsInfinity(value))
            {
                return double.NaN;
            }

            return RangeStart + (Math.Log10(value) - logMin) / (logMax - logMin) * (RangeEnd - RangeStart);
        }

        public double Invert(double position)
        {
            var t = (position - RangeStart) / (RangeEnd - RangeStart);

            return Math.Pow(10, logMin + t * (logMax - logMin));
        }
    }
}