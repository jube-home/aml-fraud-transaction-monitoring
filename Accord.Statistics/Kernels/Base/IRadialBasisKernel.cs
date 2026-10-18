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

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Interface for Radial Basis Function kernels.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A radial basis function (RBF) is a real-valued function whose value depends only
    ///         on the distance from the origin, so that <c>ϕ(x) = ϕ(||x||)</c>; or alternatively
    ///         on the distance from some other point <c>c</c>, called a center, so that
    ///         <c>ϕ(x,c) = ϕ(||x−c||)</c>. Any function ϕ that satisfies the property
    ///         <c>ϕ(x) = ϕ(||x||)</c> is a radial function. The norm is usually Euclidean distance,
    ///         although other distance functions are also possible.
    ///     </para>
    ///     <para>
    ///         Examples of radial basis kernels include:
    ///     </para>
    ///     <list type="bullet">
    ///         <description>
    ///             <see cref="Circular" />
    ///         </description>
    ///         <description>
    ///             <see cref="Gaussian" />
    ///         </description>
    ///         <description>
    ///             <see cref="Log" />
    ///         </description>
    ///         <description>
    ///             <see cref="Multiquadric" />
    ///         </description>
    ///         <description>
    ///             <see cref="Power " />
    ///         </description>
    ///         <description>
    ///             <see cref="RationalQuadratic" />
    ///         </description>
    ///         <description>
    ///             <see cref="Spherical" />
    ///         </description>
    ///         <description>
    ///             <see cref="SquaredSinc" />
    ///         </description>
    ///         <description>
    ///             <see cref="SymmetricTriangle" />
    ///         </description>
    ///         <description>
    ///             <see cref="TStudent" />
    ///         </description>
    ///         <description>
    ///             <see cref="Wave" />
    ///         </description>
    ///     </list>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Radial basis functions. Available on:
    ///                     https://en.wikipedia.org/wiki/Radial_basis_function
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Kernels\GaussianTest.cs" region="doc_rbf" />
    /// </example>
    public interface IRadialBasisKernel : IKernel
    {
        /// <summary>
        ///     The kernel function.
        /// </summary>
        /// <param name="z">Distance <c>z</c> between two vectors in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        double Function(double z);
    }
}