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
using Accord.Math.Distances;

namespace Accord.Collections
{
#if !MONO
    /// <summary>
    ///     Vantage-Point Tree.
    /// </summary>
    /// <typeparam name="TPoint">The type for the position vector of each node.</typeparam>
    [Serializable]
    public class VPTree<TPoint> : VPTreeBase<TPoint, VPTreeNode<TPoint>>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="VPTree{TPoint}" /> class.
        /// </summary>
        /// <param name="distance">The distance to use when comparing points.</param>
        public VPTree(IDistance<TPoint> distance)
            : base(distance)
        {
        }

        /// <summary>
        ///     Creates a new vantage-point tree from the given points.
        /// </summary>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">
        ///     Whether to perform operations in place, altering the
        ///     original array of points instead of creating an extra copy.
        /// </param>
        /// <returns>A <see cref="VPTree{TPoint}" /> populated with the given data points.</returns>
        public static VPTree<TPoint> FromData(TPoint[] points, IDistance<TPoint> distance, bool inPlace = false)
        {
            var tree = new VPTree<TPoint>(distance);
            tree.Root = tree.buildFromPoints(points, 0, points.Length, inPlace);
            return tree;
        }
    }
#endif
}