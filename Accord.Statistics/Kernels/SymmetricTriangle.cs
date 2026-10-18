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

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Symmetric Triangle Kernel.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Chaudhuri et al, A Comparative Study of Kernels for the Multi-class Support Vector
    ///                     Machine, 2008. Available on: http://www.computer.org/portal/web/csdl/doi/10.1109/ICNC.2008.803
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Serializable]
    public sealed class SymmetricTriangle : KernelBase, IKernel, IRadialBasisKernel, ICloneable
    {
        private double gamma;

        /// <summary>
        ///     Constructs a new Symmetric Triangle Kernel
        /// </summary>
        public SymmetricTriangle(double gamma)
        {
            Gamma = gamma;
        }

        /// <summary>
        ///     Gets or sets the gamma value for the kernel.
        /// </summary>
        public double Gamma
        {
            get => gamma;
            set => gamma = value;
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
        ///     Symmetric Triangle Kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            var norm = 0.0;
            for (var i = 0; i < x.Length; i++)
            {
                var d = x[i] - y[i];
                norm += d * d;
            }

            var z = 1.0 - gamma * System.Math.Sqrt(norm);

            return z > 0 ? z : 0;
        }

        /// <summary>
        ///     Symmetric Triangle Kernel function.
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            var zz = 1.0 - gamma * z;

            return zz > 0 ? zz : 0;
        }
    }
}