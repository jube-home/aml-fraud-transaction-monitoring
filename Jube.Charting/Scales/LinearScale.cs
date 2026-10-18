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
    public class LinearScale : IScale
    {
        public LinearScale(double domainMin, double domainMax, double rangeStart, double rangeEnd)
        {
            if (!double.IsFinite(domainMin) || !double.IsFinite(domainMax) || !double.IsFinite(rangeStart) ||
                !double.IsFinite(rangeEnd))
            {
                throw new ChartSpecificationException("Scale bounds must be finite.");
            }

            if (Math.Abs(domainMin - domainMax) < 0.0001)
            {
                var pad = domainMin == 0 ? 1 : Math.Abs(domainMin) * 0.05;
                domainMin -= pad;
                domainMax += pad;
            }

            DomainMin = domainMin;
            DomainMax = domainMax;
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
        }

        public double DomainMin { get; }
        public double DomainMax { get; }
        public double RangeStart { get; }
        public double RangeEnd { get; }

        public virtual double Map(double value)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            return RangeStart + (value - DomainMin) / (DomainMax - DomainMin) * (RangeEnd - RangeStart);
        }

        public virtual double Invert(double position)
        {
            if (Math.Abs(RangeEnd - RangeStart) < 0.0001)
            {
                return DomainMin;
            }

            return DomainMin + (position - RangeStart) / (RangeEnd - RangeStart) * (DomainMax - DomainMin);
        }

        public double Clamp(double value)
        {
            return Math.Clamp(value, Math.Min(RangeStart, RangeEnd), Math.Max(RangeStart, RangeEnd));
        }
    }
}