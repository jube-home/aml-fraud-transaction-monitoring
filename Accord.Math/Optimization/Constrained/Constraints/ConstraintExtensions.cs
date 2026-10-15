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

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Extension methods on the <see cref="IConstraint" /> interface.
    /// </summary>
    public static class ConstraintExtensions
    {
        /// <summary>
        ///     Gets how much the constraint is being violated.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <param name="input">The function point.</param>
        /// <returns>
        ///     How much the constraint is being violated at the given point. Positive
        ///     value means the constraint is not being violated with the returned slack,
        ///     while a negative value means the constraint is being violated by the returned
        ///     amount.
        /// </returns>
        public static double GetViolation(this IConstraint constraint, double[] input)
        {
            var fx = constraint.Function(input);

            switch (constraint.ShouldBe)
            {
                case ConstraintType.EqualTo:
                    return System.Math.Abs(fx - constraint.Value);

                case ConstraintType.GreaterThanOrEqualTo:
                    return fx - constraint.Value;

                case ConstraintType.LesserThanOrEqualTo:
                    return constraint.Value - fx;
            }

            throw new NotSupportedException();
        }

        /// <summary>
        ///     Gets whether this constraint is being violated
        ///     (within the current tolerance threshold).
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <param name="input">The function point.</param>
        /// <returns>
        ///     True if the constraint is being violated, false otherwise.
        /// </returns>
        public static bool IsViolated(this IConstraint constraint, double[] input)
        {
            return constraint.GetViolation(input) + constraint.Tolerance < 0;
        }
    }
}