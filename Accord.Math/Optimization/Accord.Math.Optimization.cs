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

using System.Runtime.CompilerServices;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Contains classes for constrained and unconstrained optimization. Includes
    ///     <see cref="ConjugateGradient">Conjugate Gradient (CG)</see>,
    ///     <see cref="BoundedBroydenFletcherGoldfarbShanno">
    ///         Bounded
    ///     </see>
    ///     and <see cref="BroydenFletcherGoldfarbShanno">Unbounded Broyden–Fletcher–Goldfarb–Shanno (BFGS)</see>,
    ///     gradient-free optimization methods such as <see cref="Cobyla" /> and the
    ///     <see cref="GoldfarbIdnani">
    ///         Goldfarb-Idnani
    ///     </see>
    ///     solver for Quadratic Programming (QP) problems.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This namespace contains different methods for solving both constrained and unconstrained
    ///         optimization problems. For unconstrained optimization, methods available include
    ///         <see cref="ConjugateGradient">Conjugate Gradient (CG)</see>,
    ///         <see cref="BoundedBroydenFletcherGoldfarbShanno">
    ///             Bounded
    ///         </see>
    ///         and <see cref="BroydenFletcherGoldfarbShanno">Unbounded Broyden–Fletcher–Goldfarb–Shanno (BFGS)</see>,
    ///         <see cref="ResilientBackpropagation">Resilient Backpropagation</see> and a simplified implementation of the
    ///         <see cref="TrustRegionNewtonMethod">Trust Region Newton Method (TRON)</see>.
    ///     </para>
    ///     <para>
    ///         For constrained optimization problems, methods available include the
    ///         <see cref="AugmentedLagrangian">
    ///             Augmented Lagrangian method
    ///         </see>
    ///         for general non-linear optimization, <see cref="Cobyla" /> for
    ///         gradient-free non-linear optimization, and the <see cref="GoldfarbIdnani">Goldfarb-Idnani</see>
    ///         method for solving Quadratic Programming (QP) problems.
    ///     </para>
    ///     <para>
    ///         This namespace also contains optimizers specialized for least squares problems, such as
    ///         <see cref="GaussNewton">
    ///             Gauss Newton
    ///         </see>
    ///         and the <see cref="LevenbergMarquardt">Levenberg-Marquart</see> least squares solvers.
    ///     </para>
    ///     <para>
    ///         For univariate problems, standard search algorithms are also available, such as
    ///         <see cref="BrentSearch">
    ///             Brent
    ///         </see>
    ///         and <see cref="BinarySearch">Binary search</see>.
    ///     </para>
    ///     <para>
    ///         The namespace class diagram is shown below.
    ///     </para>
    ///     <img src="..\diagrams\classes\Accord.Math.Optimization.png" />
    /// </remarks>
    /// <seealso cref="Math" />
    /// <seealso cref="Differentiation" />
    /// <seealso cref="Integration" />
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}