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

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Defines an interface for an optimization constraint.
    /// </summary>
    public interface IConstraint
    {
        /// <summary>
        ///     Gets the type of the constraint.
        /// </summary>
        ConstraintType ShouldBe { get; }

        /// <summary>
        ///     Gets the value in the right hand
        ///     side of the constraint equation.
        /// </summary>
        double Value { get; }

        /// <summary>
        ///     Gets the violation tolerance for the constraint.
        /// </summary>
        double Tolerance { get; }

        /// <summary>
        ///     Gets the number of variables in the constraint.
        /// </summary>
        int NumberOfVariables { get; }

        /// <summary>
        ///     Calculates the left hand side of the constraint
        ///     equation given a vector x.
        /// </summary>
        /// <param name="x">The vector.</param>
        /// <returns>
        ///     The left hand side of the constraint equation as evaluated at x.
        /// </returns>
        double Function(double[] x);

        /// <summary>
        ///     Calculates the gradient of the constraint
        ///     equation given a vector x
        /// </summary>
        /// <param name="x">The vector.</param>
        /// <returns>The gradient of the constraint as evaluated at x.</returns>
        double[] Gradient(double[] x);
    }
}