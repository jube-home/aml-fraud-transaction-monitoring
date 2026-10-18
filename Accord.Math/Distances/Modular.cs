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

namespace Accord.Math.Distances
{
    /// <summary>
    ///     Modular distance (shortest distance between two marks on a circle).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The modular distance is the distance of two member in a circular space,
    ///         or a ring. Roughly, it can be understood as the minimum difference between
    ///         two numbers in this circular space, either going “clockwise” or “counter-clockwise”.
    ///         For example, in a circle, the modular distance between 1° and 359° is 2°
    ///         (and not 358°).
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://crsouza.com/2009/09/modulo-and-modular-distance-in-c">
    ///                         http://crsouza.com/2009/09/modulo-and-modular-distance-in-c
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Serializable]
    public struct Modular : IDistance<double>, IDistance<int>, ICloneable
    {
        /// <summary>
        ///     Gets the maximum value that the distance can
        ///     have before it wraps around in the circle.
        /// </summary>
        public int Modulo { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Modular" /> class.
        /// </summary>
        /// <param name="modulo">
        ///     The maximum value that the distance can
        ///     have before it wraps around in the circle (i.e. 360).
        /// </param>
        public Modular(int modulo)
        {
            this.Modulo = modulo;
        }

        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double x, double y)
        {
            return System.Math.Min(Tools.Mod(x - y, Modulo), Tools.Mod(y - x, Modulo));
        }

        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(int x, int y)
        {
            return System.Math.Min(Tools.Mod(x - y, Modulo), Tools.Mod(y - x, Modulo));
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new Modular(Modulo);
        }
    }
}