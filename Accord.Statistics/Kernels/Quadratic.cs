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
    ///     Quadratic Kernel.
    /// </summary>
    [Serializable]
    public sealed class Quadratic : KernelBase, IKernel,
        IDistance, IReverseDistance, ICloneable, ITransform
    {
        private double constant;

        /// <summary>
        ///     Constructs a new Quadratic kernel.
        /// </summary>
        /// <param name="constant">The polynomial constant for this kernel. Default is 1.</param>
        public Quadratic(double constant)
        {
            this.constant = constant;
        }

        /// <summary>
        ///     Constructs a new Quadratic kernel.
        /// </summary>
        public Quadratic()
            : this(1.0)
        {
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
        ///     Computes the squared distance in input space
        ///     between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in feature (kernel) space.</param>
        /// <param name="y">Vector <c>y</c> in feature (kernel) space.</param>
        /// <returns>Distance between <c>x</c> and <c>y</c> in input space.</returns>
        public override double Distance(double[] x, double[] y)
        {
            if (x == y)
            {
                return 0.0;
            }

            double sumx = constant, sumy = constant, sum = constant;

            for (var i = 0; i < x.Length; i++)
            {
                sumx += x[i] * x[i];
                sumy += y[i] * y[i];
                sum += x[i] * y[i];
            }

            return sumx * sumx + sumy * sumy - 2 * sum * sum;
        }

        /// <summary>
        ///     Quadratic kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            var sum = constant;
            for (var i = 0; i < x.Length; i++)
            {
                sum += x[i] * y[i];
            }

            return sum * sum;
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
            double sumx = 0;
            double sumy = 0;
            double sum = 0;

            for (var i = 0; i < x.Length; i++)
            {
                sumx += x[i] * x[i];
                sumy += y[i] * y[i];
                sum += x[i] * y[i];
            }


            return System.Math.Sqrt(sumx) + System.Math.Sqrt(sumy) - 2.0 * System.Math.Sqrt(sum);
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
            return Transform(input, constant);
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
        ///     Quadratic kernel function.
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            var sum = constant + z;

            return sum * sum;
        }

        /// <summary>
        ///     Projects an input point into feature space.
        /// </summary>
        /// <param name="input">The input point to be projected into feature space.</param>
        /// <param name="constant">The <see cref="Constant" /> parameter of the kernel.</param>
        /// <returns>
        ///     The feature space representation of the given <paramref name="input" /> point.
        /// </returns>
        public static double[] Transform(double[] input, double constant)
        {
            var n = input.Length;
            var m = n * (n + 1) / 2;

            var features = constant == 0 ? new double[m] : new double[m + n + 1];

            for (var i = 0; i < input.Length; i++)
            {
                features[i] = input[i] * input[i];
            }

            var c = input.Length;
            for (var i = 0; i < input.Length; i++)
            {
                for (var j = i + 1; j < input.Length; j++)
                {
                    features[c++] = Constants.Sqrt2 * input[i] * input[j];
                }
            }

            if (constant != 0)
            {
                var sqrt2c = System.Math.Sqrt(2 * constant);

                for (var i = 0; i < input.Length; i++)
                {
                    features[m + i] = input[i] * sqrt2c;
                }

                features[features.Length - 1] = constant;
            }

            return features;
        }
    }
}