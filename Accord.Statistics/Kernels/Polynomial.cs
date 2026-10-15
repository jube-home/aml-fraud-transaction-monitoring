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

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Polynomial Kernel.
    /// </summary>
    [Serializable]
    public struct Polynomial : IKernel, IDistance,
        IReverseDistance, ICloneable, ITransform,
        IKernel<Sparse<double>>, IDistance<Sparse<double>>
    {
        private int degree;
        private double constant;

        /// <summary>
        ///     Constructs a new Polynomial kernel of a given degree.
        /// </summary>
        /// <param name="degree">The polynomial degree for this kernel.</param>
        /// <param name="constant">The polynomial constant for this kernel. Default is 1.</param>
        public Polynomial(int degree, double constant)
        {
            this.degree = degree;
            this.constant = constant;
        }

        /// <summary>
        ///     Constructs a new Polynomial kernel of a given degree.
        /// </summary>
        /// <param name="degree">The polynomial degree for this kernel.</param>
        public Polynomial(int degree)
            : this(degree, 1.0)
        {
        }

        /// <summary>
        ///     Gets or sets the kernel's polynomial degree.
        /// </summary>
        public int Degree
        {
            get => degree;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Degree must be positive.");
                }

                degree = value;
            }
        }

        /// <summary>
        ///     Gets or sets the kernel's polynomial constant term.
        /// </summary>
        public double Constant
        {
            get => constant;
            set => constant = value;
        }

        /// <summary>
        ///     Polynomial kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double[] x, double[] y)
        {
            var sum = constant;
            for (var i = 0; i < x.Length; i++)
            {
                sum += x[i] * y[i];
            }

            return System.Math.Pow(sum, degree);
        }

        /// <summary>
        ///     Polynomial kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(Sparse<double> x, Sparse<double> y)
        {
            var sum = x.Dot(y) + constant;
            return System.Math.Pow(sum, degree);
        }

        /// <summary>
        ///     Polynomial kernel function.
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            return System.Math.Pow(z + constant, degree);
        }

        /// <summary>
        ///     Computes the squared distance in feature space
        ///     between two points given in input space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Squared distance between <c>x</c> and <c>y</c> in feature (kernel) space.</returns>
        public double Distance(double[] x, double[] y)
        {
            if (x == y)
            {
                return 0.0;
            }

            var sumx = constant;
            var sumy = constant;
            var sum = constant;

            for (var i = 0; i < x.Length; i++)
            {
                sumx += x[i] * x[i];
                sumy += y[i] * y[i];
                sum += x[i] * y[i];
            }

            var d = degree;

            return System.Math.Pow(sumx, d) + System.Math.Pow(sumy, d) - 2 * System.Math.Pow(sum, d);
        }

        /// <summary>
        ///     Computes the squared distance in feature space
        ///     between two points given in input space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Squared distance between <c>x</c> and <c>y</c> in feature (kernel) space.</returns>
        public double Distance(Sparse<double> x, Sparse<double> y)
        {
            if (x == y)
            {
                return 0.0;
            }

            var sumx = constant + x.Dot(x);
            var sumy = constant + y.Dot(y);
            var sum = constant + x.Dot(y);

            var d = degree;

            return System.Math.Pow(sumx, d) + System.Math.Pow(sumy, d) - 2 * System.Math.Pow(sum, d);
        }

        /// <summary>
        ///     Computes the distance in input space
        ///     between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in feature (kernel) space.</param>
        /// <param name="y">Vector <c>y</c> in feature (kernel) space.</param>
        /// <returns>Distance between <c>x</c> and <c>y</c> in input space.</returns>
        public double ReverseDistance(double[] x, double[] y)
        {
            var sumx = constant;
            var sumy = constant;
            var sum = constant;

            for (var i = 0; i < x.Length; i++)
            {
                sumx += x[i] * x[i];
                sumy += y[i] * y[i];
                sum += x[i] * y[i];
            }

            var q = 1.0 / degree;

            return System.Math.Pow(sumx, q) + System.Math.Pow(sumy, q) - 2.0 * System.Math.Pow(sum, q);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        ///     Projects an input point into feature space.
        /// </summary>
        /// <param name="input">The input point to be projected into feature space.</param>
        /// <returns>
        ///     The feature space representation of the given <paramref name="input" /> point.
        /// </returns>
        public double[] Transform(double[] input)
        {
            switch (degree)
            {
                case 1:
                    return Linear.Transform(input, constant);

                case 2:
                    return Quadratic.Transform(input, constant);

                default:
                    return Transform(input, degree, constant);
            }
        }

        /// <summary>
        ///     Projects a set of input points into feature space.
        /// </summary>
        /// <param name="inputs">The input points to be projected into feature space.</param>
        /// <returns>
        ///     The feature space representation of the given <paramref name="inputs" /> points.
        /// </returns>
        public double[][] Transform(double[][] inputs)
        {
            var r = new double[inputs.Length][];
            for (var i = 0; i < inputs.Length; i++)
            {
                r[i] = Transform(inputs[i]);
            }

            return r;
        }

        /// <summary>
        ///     Projects an input point into feature space.
        /// </summary>
        /// <param name="input">The input point to be projected into feature space.</param>
        /// <param name="constant">The <see cref="Constant" /> parameter of the kernel.</param>
        /// <param name="degree">The <see cref="Degree" /> parameter of the kernel.</param>
        /// <returns>
        ///     The feature space representation of the given <paramref name="input" /> point.
        /// </returns>
        public static double[] Transform(double[] input, int degree, double constant)
        {
            if (constant != 0)
            {
                throw new NotSupportedException(
                    "The explicit feature-space projection function for degrees "
                    + " higher than two is only available for homogeneous kernels"
                    + " (i.e. kernel functions with the constant term set to zero).");
            }

            var n = input.Length;
            var m = (int)System.Math.Pow(n, degree);

            var features = new double[m];

            var index = 0;
            foreach (var s in Combinatorics.Sequences(input.Length, degree))
            {
                double prod = 1;
                for (var i = 0; i < s.Length; i++)
                {
                    prod *= input[s[i]];
                }

                features[index++] = prod;
            }

            return features;
        }
    }
}