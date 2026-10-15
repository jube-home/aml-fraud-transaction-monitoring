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

using Jube.Charting.Exceptions;

namespace Jube.Charting.Scales
{
    public sealed class BandScale
    {
        public BandScale(int count, double rangeStart, double rangeEnd, double padding = 0.2)
        {
            if (count < 1)
            {
                throw new ChartSpecificationException("A band scale needs at least one band.");
            }

            Count = count;
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
            Padding = Math.Clamp(padding, 0, 0.9);
            var span = rangeEnd - rangeStart;
            Step = span / count;
            Bandwidth = Step * (1 - Padding);
            Offset = Step * Padding / 2;
        }

        public int Count { get; }
        public double RangeStart { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public double RangeEnd { get; }
        public double Padding { get; }
        public double Step { get; }
        public double Bandwidth { get; }

        public double Start(int index)
        {
            return RangeStart + Offset + index * Step;
        }

        public double Center(int index)
        {
            return Start(index) + Bandwidth / 2;
        }

        public int Invert(double position)
        {
            var index = (int)Math.Floor((position - RangeStart - Offset) / Step);

            return Math.Clamp(index, 0, Count - 1);
        }

        private double Offset { get; }
    }
}