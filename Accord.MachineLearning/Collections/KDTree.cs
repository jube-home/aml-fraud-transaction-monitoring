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
    /// <summary>
    ///     Convenience class for k-dimensional tree static methods. To
    ///     create a new KDTree, specify the generic parameter as in
    ///     <see cref="KDTree{T}" />.
    /// </summary>
    /// <remarks>
    ///     Please check the documentation page for <see cref="KDTree{T}" />
    ///     for examples, usage and actual remarks about kd-trees.
    /// </remarks>
    /// <seealso cref="KDTree{T}" />
    /// <seealso cref="SPTree" />
    /// <seealso cref="VPTree" />
    [Serializable]
    public class KDTree : KDTreeBase<KDTreeNode>
    {
        /// <summary>
        ///     Creates a new <see cref="KDTree" />.
        /// </summary>
        /// <param name="dimensions">The number of dimensions in the tree.</param>
        public KDTree(int dimensions)
            : base(dimensions)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="KDTree" />.
        /// </summary>
        /// <param name="dimension">The number of dimensions in the tree.</param>
        /// <param name="root">The root node, if already existent.</param>
        public KDTree(int dimension, KDTreeNode root)
            : base(dimension, root)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="KDTree" />.
        /// </summary>
        /// <param name="dimension">The number of dimensions in the tree.</param>
        /// <param name="root">The root node, if already existent.</param>
        /// <param name="count">The number of elements in the root node.</param>
        /// <param name="leaves">The number of leaves linked through the root node.</param>
        public KDTree(int dimension, KDTreeNode root, int count, int leaves)
            : base(dimension, root, count, leaves)
        {
        }

        /// <summary>
        ///     Adds a new point to this tree.
        /// </summary>
        /// <param name="position">A double-vector with the same number of elements as dimensions in the tree.</param>
        public void Add(double[] position)
        {
            AddNode(position);
        }

        /// <summary>
        ///     Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="inPlace">
        ///     Whether the given <paramref name="points" /> vector
        ///     can be ordered in place. Passing true will change the original order of
        ///     the vector. If set to false, all operations will be performed on an extra
        ///     copy of the vector.
        /// </param>
        /// <returns>A <see cref="KDTree{T}" /> populated with the given data points.</returns>
        public static KDTree<T> FromData<T>(double[][] points, bool inPlace = false)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            if (points.Length == 0)
            {
                throw new ArgumentException("Insufficient points for creating a tree.");
            }

            int leaves;

            var root = KDTree<T>.CreateRoot(points, inPlace, out leaves);

            return new KDTree<T>(points[0].Length, root, points.Length, leaves);
        }

        /// <summary>
        ///     Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="inPlace">
        ///     Whether the given <paramref name="points" /> vector
        ///     can be ordered in place. Passing true will change the original order of
        ///     the vector. If set to false, all operations will be performed on an extra
        ///     copy of the vector.
        /// </param>
        /// <returns>A <see cref="KDTree{T}" /> populated with the given data points.</returns>
        public static KDTree FromData(double[][] points, bool inPlace = false)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            if (points.Length == 0)
            {
                throw new ArgumentException("Insufficient points for creating a tree.");
            }

            int leaves;

            var root = CreateRoot(points, inPlace, out leaves);

            return new KDTree(points[0].Length, root, points.Length, leaves);
        }

        /// <summary>
        ///     Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="values">The corresponding values at each data point.</param>
        /// <param name="inPlace">
        ///     Whether the given <paramref name="points" /> vector
        ///     can be ordered in place. Passing true will change the original order of
        ///     the vector. If set to false, all operations will be performed on an extra
        ///     copy of the vector.
        /// </param>
        /// <returns>A <see cref="KDTree{T}" /> populated with the given data points.</returns>
        public static KDTree<T> FromData<T>(double[][] points, T[] values, bool inPlace = false)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            if (points.Length == 0)
            {
                throw new ArgumentException("Insufficient points for creating a tree.");
            }

            int leaves;

            var root = KDTree<T>.CreateRoot(points, values, inPlace, out leaves);

            return new KDTree<T>(points[0].Length, root, points.Length, leaves);
        }

        /// <summary>
        ///     Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">
        ///     Whether the given <paramref name="points" /> vector
        ///     can be ordered in place. Passing true will change the original order of
        ///     the vector. If set to false, all operations will be performed on an extra
        ///     copy of the vector.
        /// </param>
        /// <returns>A <see cref="KDTree{T}" /> populated with the given data points.</returns>
        public static KDTree FromData(double[][] points, IMetric<double[]> distance,
            bool inPlace = false)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            if (distance == null)
            {
                throw new ArgumentNullException("distance");
            }

            if (points.Length == 0)
            {
                throw new ArgumentException("Insufficient points for creating a tree.");
            }

            int leaves;

            var root = CreateRoot(points, inPlace, out leaves);

            return new KDTree(points[0].Length, root, points.Length, leaves)
            {
                Distance = distance
            };
        }

        /// <summary>
        ///     Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="values">The corresponding values at each data point.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">
        ///     Whether the given <paramref name="points" /> vector
        ///     can be ordered in place. Passing true will change the original order of
        ///     the vector. If set to false, all operations will be performed on an extra
        ///     copy of the vector.
        /// </param>
        /// <returns>A <see cref="KDTree{T}" /> populated with the given data points.</returns>
        public static KDTree<T> FromData<T>(double[][] points, T[] values,
            IMetric<double[]> distance, bool inPlace = false)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (distance == null)
            {
                throw new ArgumentNullException("distance");
            }

            int leaves;

            var root = KDTree<T>.CreateRoot(points, values, inPlace, out leaves);

            return new KDTree<T>(points[0].Length, root, points.Length, leaves)
            {
                Distance = distance
            };
        }

        /// <summary>
        ///     Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">
        ///     Whether the given <paramref name="points" /> vector
        ///     can be ordered in place. Passing true will change the original order of
        ///     the vector. If set to false, all operations will be performed on an extra
        ///     copy of the vector.
        /// </param>
        /// <returns>A <see cref="KDTree{T}" /> populated with the given data points.</returns>
        public static KDTree<T> FromData<T>(double[][] points, IMetric<double[]> distance,
            bool inPlace = false)
        {
            if (distance == null)
            {
                throw new ArgumentNullException("distance");
            }

            int leaves;

            var root = KDTree<T>.CreateRoot(points, inPlace, out leaves);

            return new KDTree<T>(points[0].Length, root, points.Length, leaves)
            {
                Distance = distance
            };
        }
    }
}