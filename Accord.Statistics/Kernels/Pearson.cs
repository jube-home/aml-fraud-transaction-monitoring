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

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Pearson VII universal kernel (PUK).
    /// </summary>
    [Serializable]
    public sealed class Pearson : KernelBase, IKernel, IDistance,
        ICloneable
    {
        private double constant;
        private double omega;
        private double sigma;

        /// <summary>
        ///     Constructs a new Pearson VII universal kernel.
        /// </summary>
        /// <param name="omega">The Pearson's omega parameter w. Default is 1.</param>
        /// <param name="sigma">The Pearson's sigma parameter s. Default is 1.</param>
        public Pearson(double omega, double sigma)
        {
            this.omega = omega;
            this.sigma = sigma;
            constant = 2 * System.Math.Sqrt(System.Math.Pow(2, 1 / omega) - 1) / sigma;
        }

        /// <summary>
        ///     Constructs a new Pearson VII universal kernel.
        /// </summary>
        public Pearson()
            : this(1, 1)
        {
        }

        /// <summary>
        ///     Gets or sets the kernel's parameter omega. Default is 1.
        /// </summary>
        public double Omega
        {
            get => omega;
            set
            {
                omega = value;
                constant = 2 * System.Math.Sqrt(System.Math.Pow(2, 1 / omega) - 1) / sigma;
            }
        }

        /// <summary>
        ///     Gets or sets the kernel's parameter sigma. Default is 1.
        /// </summary>
        public double Sigma
        {
            get => sigma;
            set
            {
                sigma = value;
                constant = 2 * System.Math.Sqrt(System.Math.Pow(2, 1 / omega) - 1) / sigma;
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
        ///     Pearson Universal kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            //Inner product
            double xx = 0;
            double yy = 0;
            double xy = 0;
            for (var i = 0; i < x.Length; i++)
            {
                var u = x[i] * x[i];
                var v = y[i] * y[i];
                var uv = x[i] * y[i];
                xx += u;
                yy += v;
                xy += uv;
            }

            var m = constant * System.Math.Sqrt(-2.0 * xy + xx + yy);
            return 1.0 / System.Math.Pow(1.0 + m * m, omega);
        }

        /// <summary>
        ///     Pearson Universal function.
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            return 1 / System.Math.Pow(1 + z * constant * (z * constant), omega);
        }
    }
}