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
    ///     Sigmoid Kernel.
    /// </summary>
    /// <remarks>
    ///     Sigmoid kernel of the form k(x,z) = tanh(a * x'z + c). Sigmoid kernels are only
    ///     conditionally positive definite for some values of a and c, and therefore may not
    ///     induce a reproducing kernel Hilbert space. However, they have been successfully
    ///     used in practice (Schölkopf and Smola, 2002).
    /// </remarks>
    [Serializable]
    public sealed class Sigmoid : KernelBase, IKernel, ICloneable, IEstimable,
        IKernel<Sparse<double>>, IDistance<Sparse<double>>
    {
        private double alpha;
        private double constant;

        /// <summary>
        ///     Constructs a Sigmoid kernel.
        /// </summary>
        public Sigmoid()
            : this(0.01, -System.Math.E)
        {
        }

        /// <summary>
        ///     Constructs a Sigmoid kernel.
        /// </summary>
        /// <param name="alpha">
        ///     Alpha parameter. Typically should be set to
        ///     a small positive value. Default is 0.01.
        /// </param>
        /// <param name="constant">
        ///     Constant parameter. Typically should be set to
        ///     a negative value. Default is -e (Euler's constant).
        /// </param>
        public Sigmoid(double alpha, double constant)
        {
            this.alpha = alpha;
            this.constant = constant;
        }

        /// <summary>
        ///     Gets or sets the kernel's alpha parameter.
        /// </summary>
        /// <remarks>
        ///     In a sigmoid kernel, alpha is a inner product
        ///     coefficient for the hyperbolic tangent function.
        /// </remarks>
        public double Alpha
        {
            get => alpha;
            set => alpha = value;
        }

        /// <summary>
        ///     Gets or sets the kernel's constant term.
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
        ///     Computes the squared distance in feature space
        ///     between two points given in input space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>
        ///     Squared distance between <c>x</c> and <c>y</c> in feature (kernel) space.
        /// </returns>
        public double Distance(Sparse<double> x, Sparse<double> y)
        {
            return Function(x, x) + Function(y, y) - 2 * Function(x, y);
        }

        void IEstimable<double[]>.Estimate(double[][] inputs)
        {
            var s = Estimate(inputs);
            Alpha = s.Alpha;
            Constant = s.Constant;
        }

        /// <summary>
        ///     Sigmoid kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            var sum = 0.0;
            for (var i = 0; i < x.Length; i++)
            {
                sum += x[i] * y[i];
            }

            var value = System.Math.Tanh(alpha * sum + constant);

            return value;
        }

        /// <summary>
        ///     Sigmoid kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(Sparse<double> x, Sparse<double> y)
        {
            var sum = x.Dot(y);

            var value = System.Math.Tanh(alpha * sum + constant);

            return value;
        }

        /// <summary>
        ///     Estimates suitable values for the sigmoid kernel
        ///     by exploring the response area of the tanh function.
        /// </summary>
        /// <param name="inputs">An input data set.</param>
        /// <returns>A Sigmoid kernel initialized with appropriate values.</returns>
        public static Sigmoid Estimate(double[][] inputs)
        {
            var norm = new double[inputs.Length];
            for (var i = 0; i < inputs.Length; i++)
            {
                norm[i] = inputs[i].SquareEuclidean();
            }

            var max = norm.Max();

            var r = -System.Math.E;
            var a = -r / max;

            return new Sigmoid(a, r);
        }

        /// <summary>
        ///     Estimates suitable values for the sigmoid kernel
        ///     by exploring the response area of the tanh function.
        /// </summary>
        /// <param name="inputs">An input data set.</param>
        /// <param name="samples">The size of the subset to use in the estimation.</param>
        /// <param name="range">The interquartile range for the data.</param>
        /// <returns>A Sigmoid kernel initialized with appropriate values.</returns>
        public static Sigmoid Estimate(double[][] inputs, int samples, out DoubleRange range)
        {
            if (samples > inputs.Length)
            {
                throw new ArgumentOutOfRangeException("samples");
            }

            var distances = Products(inputs, samples);

            var q1 = System.Math.Sqrt(
                distances[System.Math.Min(distances.Length - 1, (int)System.Math.Ceiling(0.15 * distances.Length))] /
                2.0);
            var q9 = System.Math.Sqrt(
                distances[System.Math.Min(distances.Length - 1, (int)System.Math.Ceiling(0.85 * distances.Length))] /
                2.0);
            var qm = System.Math.Sqrt(distances.Median(true) / 2.0);

            range = new DoubleRange(q1, q9);

            var max = qm;

            var r = -System.Math.E;
            var a = -r / max;

            return new Sigmoid(a, r);
        }

        /// <summary>
        ///     Computes the set of all distances between
        ///     all points in a random subset of the data.
        /// </summary>
        /// <param name="inputs">The inputs points.</param>
        /// <param name="samples">The number of samples.</param>
        public static double[] Products(double[][] inputs, int samples)
        {
            var idx = Vector.Sample(inputs.Length, samples);
            var idy = Vector.Sample(inputs.Length, samples);

            var products = new double[samples * samples];

            for (var i = 0; i < idx.Length; i++)
            {
                var x = inputs[idx[i]];

                for (var j = 0; j < idy.Length; j++)
                {
                    var y = inputs[idy[j]];

                    double sum = 0;
                    for (var k = 0; k < x.Length; k++)
                    {
                        sum = x[k] * y[k];
                    }

                    products[i * samples + j] = sum;
                }
            }

            Array.Sort(products);

            return products;
        }

        /// <summary>
        ///     Sigmoid kernel function.
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            return System.Math.Tanh(alpha * z + constant);
        }
    }
}