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

namespace Accord.Statistics.Distributions.DensityKernels
{
    /// <summary>
    ///     Common interface for radially symmetric kernels.
    /// </summary>
    /// <seealso cref="GaussianKernel" />
    /// <seealso cref="EpanechnikovKernel" />
    /// <seealso cref="UniformKernel" />
    public interface IRadiallySymmetricKernel : IDensityKernel
    {
        /// <summary>
        ///     Computes the kernel profile function.
        /// </summary>
        /// <param name="x">The point estimate <c>x</c>.</param>
        /// <returns>The value of the profile function at point <paramref name="x" />.</returns>
        double Profile(double x);

        /// <summary>
        ///     Computes the derivative of the kernel profile function.
        /// </summary>
        /// <param name="x">The point estimate <c>x</c>.</param>
        /// <returns>The value of the derivative profile function at point <paramref name="x" />.</returns>
        double Derivative(double x);
    }
}