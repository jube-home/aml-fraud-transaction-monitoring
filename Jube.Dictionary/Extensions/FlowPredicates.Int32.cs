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

namespace Jube.Dictionary.Extensions
{
    internal static partial class FlowPredicates
    {
        internal static bool Int32Greater(int v, int other)
        {
            return v > other;
        }

        internal static bool Int32GreaterOrEqual(int v, int other)
        {
            return v >= other;
        }

        internal static bool Int32Less(int v, int other)
        {
            return v < other;
        }

        internal static bool Int32LessOrEqual(int v, int other)
        {
            return v <= other;
        }

        internal static bool Int32Equal(int v, int other)
        {
            return v == other;
        }

        internal static bool Int32NotEqual(int v, int other)
        {
            return v != other;
        }

        internal static bool Int32Between(int v, int minimum, int maximum)
        {
            return v.Between(minimum, maximum);
        }

        internal static bool Int32InRange(int v, int minimum, int maximum)
        {
            return v.InRange(minimum, maximum);
        }

        internal static bool Int32OutsideRange(int v, int minimum, int maximum)
        {
            return v < minimum || v > maximum;
        }

        internal static bool Int32In(int v, int[] values)
        {
            return v.In(values);
        }

        internal static bool Int32NotIn(int v, int[] values)
        {
            return !v.In(values);
        }

        internal static bool Int32IsZero(int v)
        {
            return v == 0;
        }

        internal static bool Int32IsPositive(int v)
        {
            return v > 0;
        }

        internal static bool Int32IsNegative(int v)
        {
            return v < 0;
        }

        internal static bool Int32IsEven(int v)
        {
            return v % 2 == 0;
        }

        internal static bool Int32IsOdd(int v)
        {
            return v % 2 != 0;
        }

        internal static bool Int32IsMultipleOf(int v, int divisor)
        {
            return divisor != 0 && v % divisor == 0;
        }

        internal static bool Int32IsPrime(int v)
        {
            return v.IsPrime();
        }
    }
}
