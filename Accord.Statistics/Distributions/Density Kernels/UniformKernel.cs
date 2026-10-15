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

namespace Accord.Statistics.Distributions.DensityKernels
{
    /// <summary>
    ///     Uniform density kernel.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Comaniciu, Dorin, and Peter Meer. "Mean shift: A robust approach toward
    ///                     feature space analysis." Pattern Analysis and Machine Intelligence, IEEE
    ///                     Transactions on 24.5 (2002): 603-619. Available at:
    ///                     http://ieeexplore.ieee.org/xpls/abs_all.jsp?arnumber=1000236
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Dan Styer, Oberlin College Department of Physics and Astronomy; Volume of a d-dimensional
    ///                     sphere. Last updated 30 August 2007. Available at:
    ///                     http://www.oberlin.edu/physics/dstyer/StatMech/VolumeDSphere.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     David W. Scott, Multivariate Density Estimation: Theory, Practice, and
    ///                     Visualization, Wiley, Aug 31, 1992
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example demonstrates how to use the Mean Shift algorithm with
    ///         a <see cref="UniformKernel">uniform kernel</see> to solve a clustering task:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Clustering\MeanShiftTest.cs" region="doc_sample1" />
    /// </example>
    /// <seealso cref="Univariate.EmpiricalDistribution" />
    /// <seealso cref="EpanechnikovKernel" />
    /// <seealso cref="GaussianKernel" />
    [Serializable]
    public class UniformKernel : IRadiallySymmetricKernel
    {
        private double constant = 0.5;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UniformKernel" /> class.
        /// </summary>
        public UniformKernel()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UniformKernel" /> class.
        /// </summary>
        /// <param name="constant">The normalization constant <c>c</c>.</param>
        public UniformKernel(double constant)
        {
            this.constant = constant;
        }

        /// <summary>
        ///     Gets or sets the kernel's normalization constant.
        /// </summary>
        public double Constant
        {
            get => constant;
            set => constant = value;
        }

        /// <summary>
        ///     Computes the kernel density function.
        /// </summary>
        /// <param name="x">The input point.</param>
        /// <returns>
        ///     A density estimate around <paramref name="x" />.
        /// </returns>
        public double Function(double[] x)
        {
            var norm = 0.0;
            for (var i = 0; i < x.Length; i++)
            {
                norm += x[i] * x[i];
            }

            if (norm <= 1)
            {
                return constant;
            }

            return 0;
        }

        /// <summary>
        ///     Computes the kernel profile function.
        /// </summary>
        /// <param name="x">The point estimate <c>x</c>.</param>
        /// <returns>
        ///     The value of the profile function at point <paramref name="x" />.
        /// </returns>
        public double Profile(double x)
        {
#if DEBUG
            if (x < 0)
            {
                throw new Exception();
            }
#endif

            if (x <= 1)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        ///     Computes the derivative of the kernel profile function.
        /// </summary>
        /// <param name="x">The point estimate <c>x</c>.</param>
        /// <returns>
        ///     The value of the derivative profile function at point <paramref name="x" />.
        /// </returns>
        public double Derivative(double x)
        {
#if DEBUG
            if (x < 0)
            {
                throw new Exception();
            }
#endif

            return 0;
        }
    }
}