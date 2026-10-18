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

namespace Accord.Math.Decompositions
{
    /// <summary>
    ///     Common interface for matrix decompositions which
    ///     can be used to solve linear systems of equations
    ///     involving jagged array matrices.
    /// </summary>
    public interface ISolverArrayDecomposition<T> where T : struct
    {
        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        T[][] Solve(T[][] value);

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        T[] Solve(T[] value);

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = B</c> where B is a diagonal matrix.
        /// </summary>
        T[][] SolveForDiagonal(T[] diagonal);

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = I</c>.
        /// </summary>
        T[][] Inverse();

        /// <summary>
        ///     Computes <c>(Xt * X)^1</c> (the inverse of the covariance matrix). This
        ///     matrix can be used to determine standard errors for the coefficients when
        ///     solving a linear set of equations through any of the <see cref="Solve(T[][])" />
        ///     methods.
        /// </summary>
        T[][] GetInformationMatrix();

        /// <summary>
        ///     Reverses the decomposition, reconstructing the original matrix <c>X</c>.
        /// </summary>
        T[][] Reverse();
    }
}