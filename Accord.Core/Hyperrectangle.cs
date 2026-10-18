// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Globalization;

namespace Accord.Math
{
    /// <summary>
    ///     Hyperrectangle structure.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In geometry, an n-orthotope (also called a hyperrectangle or a box) is the generalization of a rectangle for
    ///         higher
    ///         dimensions, formally defined as the Cartesian product of intervals.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors, "Hyperrectangle," Wikipedia, The Free Encyclopedia,
    ///                     https://en.wikipedia.org/w/index.php?title=Hyperrectangle
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="IFormattable" />
    public struct Hyperrectangle : ICloneable, IEquatable<Hyperrectangle>, IFormattable
    {
        /// <summary>
        ///     Gets the minimum point defining the lower bound of the hyperrectangle.
        /// </summary>
        public double[] Min { get; }

        /// <summary>
        ///     Gets the maximum point defining the upper bound of the hyperrectangle.
        /// </summary>
        public double[] Max { get; }

        /// <summary>
        ///     Gets the number of dimensions of the hyperrectangle.
        /// </summary>
        public int NumberOfDimensions => Min.Length;

        /// <summary>
        ///     Gets the length of each dimension. The length of the first dimension
        ///     can be referred as the width, the second as the height, and so on.
        /// </summary>
        public double[] GetLength()
        {
            var length = new double[Min.Length];
            for (var i = 0; i < length.Length; i++)
            {
                length[i] = Max[i] - Min[i];
            }

            return length;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Hyperrectangle" /> struct.
        /// </summary>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle..</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public Hyperrectangle(double x, double y, double width, double height)
        {
            Min = new[] { x, y };
            Max = new[] { x + width, y + height };
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Hyperrectangle" /> struct.
        /// </summary>
        /// <param name="min">The minimum point in the hyperrectangle (the lower bound).</param>
        /// <param name="max">The maximum point in the hyperrectangle (the upper bound).</param>
        /// <param name="copy">
        ///     Whether the passed vectors should be copied into this instance
        ///     or used as-is. Default is true (elements from the given vectors will be copied
        ///     into new array instances).
        /// </param>
        public Hyperrectangle(double[] min, double[] max, bool copy = true)
        {
            if (min.Length != max.Length)
            {
                throw new DimensionMismatchException("max");
            }

            if (copy)
            {
                this.Min = (double[])min.Clone();
                this.Max = (double[])max.Clone();
            }
            else
            {
                this.Min = min;
                this.Max = max;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Hyperrectangle" /> struct from minimum and maximum values.
        /// </summary>
        /// <param name="min">The minimum point in the hyperrectangle (the lower bound).</param>
        /// <param name="max">The maximum point in the hyperrectangle (the upper bound).</param>
        /// <param name="copy">
        ///     Whether the passed vectors should be copied into this instance
        ///     or used as-is. Default is true (elements from the given vectors will be copied
        ///     into new array instances).
        /// </param>
        public static Hyperrectangle FromMinAndMax(double[] min, double[] max, bool copy = true)
        {
            return new Hyperrectangle(min, max, copy);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Hyperrectangle" /> struct from a minimum value and a size.
        /// </summary>
        /// <param name="min">The minimum point in the hyperrectangle (the lower bound).</param>
        /// <param name="size">The size of each dimension (i.e. width, height, and so on).</param>
        /// <param name="copy">
        ///     Whether the passed vectors should be copied into this instance
        ///     or used as-is. Default is true (elements from the given vectors will be copied
        ///     into new array instances).
        /// </param>
        public static Hyperrectangle FromMinAndLength(double[] min, double[] size, bool copy = true)
        {
            if (copy)
            {
                min = (double[])min.Clone();
                size = (double[])size.Clone();
            }

            for (var i = 0; i < size.Length; i++)
            {
                size[i] = min[i] + size[i];
            }

            return new Hyperrectangle(min, size, false);
        }

        /// <summary>
        ///     Determines if this rectangle intersects with rect.
        /// </summary>
        public bool IntersectsWith(Hyperrectangle rect)
        {
            for (var i = 0; i < Min.Length; i++)
            {
                var amini = Min[i];
                var amaxi = Max[i];

                var bmini = rect.Min[i];
                var bmaxi = rect.Max[i];

                if (amini >= bmaxi || amaxi < bmini)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Determines if the specified point is contained within this Hyperrectangle structure.
        /// </summary>
        public bool Contains(params double[] point)
        {
            for (var i = 0; i < point.Length; i++)
            {
                var mini = Min[i];
                var maxi = Max[i];

                var pointi = point[i];

                if (pointi < mini || pointi >= maxi)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(Hyperrectangle other)
        {
            if (Min.Length != other.Min.Length)
            {
                return false;
            }

            for (var i = 0; i < Min.Length; i++)
            {
                if (Min[i] != other.Min[i])
                {
                    return false;
                }

                if (Max[i] != other.Max[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new Hyperrectangle((double[])Min.Clone(), (double[])Max.Clone());
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return ToString("G");
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">
        ///     The format to use.-or- A null reference (Nothing in Visual Basic) to use the default format
        ///     defined for the type of the <see cref="T:System.IFormattable" /> implementation.
        /// </param>
        /// <param name="formatProvider">
        ///     The provider to use to format the value.-or- A null reference (Nothing in Visual Basic) to
        ///     obtain the numeric format information from the current locale setting of the operating system.
        /// </param>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public string ToString(string format, IFormatProvider formatProvider = null)
        {
            if (formatProvider == null)
            {
                formatProvider = CultureInfo.CurrentCulture;
            }

            if (NumberOfDimensions == 2)
            {
                return string.Format(formatProvider, format,
                    "X = {0} Y = {1} Width = {2} Height = {3}",
                    Min[0], Min[1], Max[0] - Min[0], Max[1] - Min[1]);
            }

            return string.Format(formatProvider, format,
                "Min = {0} Max = {1} (Length = {2})",
                Min, Max, GetLength());
        }
    }
}