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
    ///     Laplacian Kernel.
    /// </summary>
    [Serializable]
    public sealed class Laplacian : KernelBase, IKernel,
        IRadialBasisKernel, IDistance, ICloneable, IEstimable, IReverseDistance,
        IKernel<Sparse<double>>, IDistance<Sparse<double>>
    {
        private double gamma;
        private double sigma;

        /// <summary>
        ///     Constructs a new Laplacian Kernel
        /// </summary>
        public Laplacian()
            : this(1)
        {
        }

        /// <summary>
        ///     Constructs a new Laplacian Kernel
        /// </summary>
        /// <param name="sigma">The sigma slope value.</param>
        public Laplacian(double sigma)
        {
            Sigma = sigma;
        }

        /// <summary>
        ///     Gets or sets the sigma value for the kernel. When setting
        ///     sigma, gamma gets updated accordingly (gamma = 0.5*/sigma^2).
        /// </summary>
        public double Sigma
        {
            get => sigma;
            set
            {
                sigma = value;
                gamma = 1.0 / sigma;
            }
        }

        /// <summary>
        ///     Gets or sets the gamma value for the kernel. When setting
        ///     gamma, sigma gets updated accordingly (gamma = 0.5*/sigma^2).
        /// </summary>
        public double Gamma
        {
            get => gamma;
            set
            {
                gamma = value;
                sigma = 1.0 / gamma;
            }
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
        /// <returns>Squared distance between <c>x</c> and <c>y</c> in input space.</returns>
        public override double Distance(double[] x, double[] y)
        {
            if (x == y)
            {
                return 0.0;
            }

            var norm = 0.0;
            for (var i = 0; i < x.Length; i++)
            {
                var d = x[i] - y[i];
                norm += d * d;
            }

            norm = System.Math.Sqrt(norm);

            return 2 - 2 * System.Math.Exp(-gamma * norm);
        }

        /// <summary>
        ///     Computes the squared distance in input space
        ///     between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in feature (kernel) space.</param>
        /// <param name="y">Vector <c>y</c> in feature (kernel) space.</param>
        /// <returns>Squared distance between <c>x</c> and <c>y</c> in input space.</returns>
        public double Distance(Sparse<double> x, Sparse<double> y)
        {
            if (x == y)
            {
                return 0.0;
            }

            var norm = Math.Distance.Euclidean(x, y);

            return 2 - 2 * System.Math.Exp(-gamma * norm);
        }

        void IEstimable<double[]>.Estimate(double[][] inputs)
        {
            var l = Estimate(inputs);
            Sigma = l.Sigma;
        }

        /// <summary>
        ///     Laplacian Kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            // Optimization in case x and y are
            // exactly the same object reference.

            if (x == y)
            {
                return 1.0;
            }

            var norm = 0.0;
            for (var i = 0; i < x.Length; i++)
            {
                var d = x[i] - y[i];
                norm += d * d;
            }

            norm = System.Math.Sqrt(norm);

            return System.Math.Exp(-gamma * norm);
        }

        /// <summary>
        ///     Laplacian Kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(Sparse<double> x, Sparse<double> y)
        {
            // Optimization in case x and y are
            // exactly the same object reference.

            if (x == y)
            {
                return 1.0;
            }

            var norm = Math.Distance.Euclidean(x, y);

            return System.Math.Exp(-gamma * norm);
        }

        /// <summary>
        ///     Laplacian Kernel function.
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            return System.Math.Exp(-gamma * z);
        }

        /// <summary>
        ///     Computes the squared distance in input space
        ///     between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in feature (kernel) space.</param>
        /// <param name="y">Vector <c>y</c> in feature (kernel) space.</param>
        /// <returns>
        ///     Squared distance between <c>x</c> and <c>y</c> in input space.
        /// </returns>
        public double ReverseDistance(double[] x, double[] y)
        {
            if (x == y)
            {
                return 0.0;
            }

            var norm = 0.0;
            for (var i = 0; i < x.Length; i++)
            {
                var d = x[i] - y[i];
                norm += d * d;
            }

            return 1.0 / -gamma * System.Math.Log(1.0 - 0.5 * norm);
        }

        /// <summary>
        ///     Computes the distance in input space given
        ///     a distance computed in feature space.
        /// </summary>
        /// <param name="df">Distance in feature space.</param>
        /// <returns>Distance in input space.</returns>
        public double ReverseDistance(double df)
        {
            return 1.0 / -gamma * System.Math.Log(1.0 - 0.5 * df);
        }

        /// <summary>
        ///     Estimate appropriate values for sigma given a data set.
        /// </summary>
        /// <remarks>
        ///     This method uses a simple heuristic to obtain appropriate values
        ///     for sigma in a radial basis function kernel. The heuristic is shown
        ///     by Caputo, Sim, Furesjo and Smola, "Appearance-based object
        ///     recognition using SVMs: which kernel should I use?", 2002.
        /// </remarks>
        /// <param name="inputs">The data set.</param>
        /// <returns>A Laplacian kernel initialized with an appropriate sigma value.</returns>
        public static Laplacian Estimate(double[][] inputs)
        {
            return Estimate(inputs, inputs.Length, out _);
        }

        /// <summary>
        ///     Estimate appropriate values for sigma given a data set.
        /// </summary>
        /// <remarks>
        ///     This method uses a simple heuristic to obtain appropriate values
        ///     for sigma in a radial basis function kernel. The heuristic is shown
        ///     by Caputo, Sim, Furesjo and Smola, "Appearance-based object
        ///     recognition using SVMs: which kernel should I use?", 2002.
        /// </remarks>
        /// <param name="inputs">The data set.</param>
        /// <param name="samples">The number of random samples to analyze.</param>
        /// <param name="range">The range of suitable values for sigma.</param>
        /// <returns>A Laplacian kernel initialized with an appropriate sigma value.</returns>
        public static Laplacian Estimate(double[][] inputs, int samples, out DoubleRange range)
        {
            if (samples > inputs.Length)
            {
                throw new ArgumentOutOfRangeException("samples");
            }

            var distances = Gaussian.Distances(inputs, samples);

            var q1 = distances[
                System.Math.Min(distances.Length - 1, (int)System.Math.Ceiling(0.15 * distances.Length))];
            var q9 = distances[
                System.Math.Min(distances.Length - 1, (int)System.Math.Ceiling(0.85 * distances.Length))];
            var qm = distances.Median(true);

            range = new DoubleRange(q1, q9);

            return new Laplacian(qm);
        }
    }
}