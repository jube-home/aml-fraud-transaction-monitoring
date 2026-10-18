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
using Accord.Math;
using Accord.Math.Distances;

namespace Accord.Collections
{
#if !MONO
    /// <summary>
    ///     Vantage-Point Tree.
    /// </summary>
    /// <seealso cref="SPTree" />
    /// <seealso cref="VPTree" />
    /// <seealso cref="KDTree" />
    [Serializable]
    public class VPTree : VPTree<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="VPTree" /> class.
        /// </summary>
        /// <param name="distance">
        ///     The distance to use when comparing points. Default is
        ///     <see cref="Distance.Euclidean(double[], double[])" />.
        /// </param>
        public VPTree(IDistance distance)
            : base(distance)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="VPTree" /> class.
        /// </summary>
        public VPTree()
            : base(new Euclidean())
        {
        }

        /// <summary>
        ///     Creates a new vantage-point tree from the given points.
        /// </summary>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="inPlace">
        ///     Whether to perform operations in place, altering the
        ///     original array of points instead of creating an extra copy.
        /// </param>
        /// <returns>A <see cref="VPTree" /> populated with the given data points.</returns>
        public static VPTree FromData(double[][] points, bool inPlace = false)
        {
            var tree = new VPTree();
            tree.Root = tree.buildFromPoints(points, 0, points.Length, inPlace);
            return tree;
        }

        /// <summary>
        ///     Creates a new vantage-point tree from the given points.
        /// </summary>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="inPlace">
        ///     Whether to perform operations in place, altering the
        ///     original array of points instead of creating an extra copy.
        /// </param>
        /// <returns>A <see cref="VPTree" /> populated with the given data points.</returns>
        public static VPTree<double> FromData(double[] points, bool inPlace = false)
        {
            return VPTree<double>.FromData(points, new Euclidean(), inPlace);
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
        /// <returns>A <see cref="VPTree" /> populated with the given data points.</returns>
        public static VPTree FromData(double[][] points, IDistance distance, bool inPlace = false)
        {
            var tree = new VPTree(distance);
            tree.Root = tree.buildFromPoints(points, 0, points.Length, inPlace);
            return tree;
        }

        /// <summary>
        ///     Creates a new vantage-point tree from the given points.
        /// </summary>
        /// <typeparam name="TPoint">The type for the position vectors.</typeparam>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">
        ///     Whether to perform operations in place, altering the
        ///     original array of points instead of creating an extra copy.
        /// </param>
        /// <returns>A <see cref="VPTree{TPoint}" /> populated with the given data points.</returns>
        public static VPTree<TPoint> FromData<TPoint>(TPoint[] points, IDistance<TPoint> distance, bool inPlace = false)
        {
            return VPTree<TPoint>.FromData(points, distance, inPlace);
        }

        /// <summary>
        ///     Creates a new vantage-point tree from the given points.
        /// </summary>
        /// <typeparam name="TData">The type of the value to be stored.</typeparam>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="values">The corresponding values at each data point.</param>
        /// <param name="inPlace">
        ///     Whether to perform operations in place, altering the
        ///     original array of points instead of creating an extra copy.
        /// </param>
        /// <returns>A <see cref="VPTree{TPoint, TData}" /> populated with the given data points.</returns>
        public static VPTree<double[], TData> FromData<TData>(double[][] points, TData[] values, bool inPlace = false)
        {
            return VPTree<double[], TData>.FromData(points, values, new Euclidean(), inPlace);
        }

        /// <summary>
        ///     Creates a new vantage-point tree from the given points.
        /// </summary>
        /// <typeparam name="TPoint">The type for the position vectors.</typeparam>
        /// <typeparam name="TData">The type of the value to be stored.</typeparam>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="values">The corresponding values at each data point.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">
        ///     Whether to perform operations in place, altering the
        ///     original array of points instead of creating an extra copy.
        /// </param>
        /// <returns>A <see cref="VPTree{TPoint, TData}" /> populated with the given data points.</returns>
        public static VPTree<TPoint, TData> FromData<TPoint, TData>(TPoint[] points, TData[] values,
            IDistance<TPoint> distance, bool inPlace = false)
        {
            return VPTree<TPoint, TData>.FromData(points, values, distance, inPlace);
        }
    }
#endif
}