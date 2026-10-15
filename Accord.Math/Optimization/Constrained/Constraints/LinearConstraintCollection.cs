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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Linear Constraint Collection.
    /// </summary>
    public class LinearConstraintCollection : Collection<LinearConstraint>, IEnumerable<LinearConstraint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearConstraintCollection" /> class.
        /// </summary>
        public LinearConstraintCollection()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearConstraintCollection" /> class.
        /// </summary>
        public LinearConstraintCollection(IEnumerable<LinearConstraint> list)
        {
            foreach (var c in list)
            {
                Add(c);
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearConstraintCollection" /> class.
        /// </summary>
        public LinearConstraintCollection(IList<LinearConstraint> list)
            : base(list)
        {
        }

        /// <summary>
        ///     Creates a matrix of linear constraints in canonical form.
        /// </summary>
        /// <param name="numberOfVariables">The number of variables in the objective function.</param>
        /// <param name="b">The vector of independent terms (the right hand side of the constraints).</param>
        /// <param name="equalities">The number of equalities in the matrix.</param>
        /// <returns>The matrix <c>A</c> of linear constraints.</returns>
        public double[,] CreateMatrix(int numberOfVariables, out double[] b, out int equalities)
        {
            return CreateMatrix(numberOfVariables, out b, out _, out equalities);
        }

        /// <summary>
        ///     Creates a matrix of linear constraints in canonical form.
        /// </summary>
        /// <param name="numberOfVariables">The number of variables in the objective function.</param>
        /// <param name="b">The vector of independent terms (the right hand side of the constraints).</param>
        /// <param name="tolerances">The amount each constraint can be violated before the answer is declared close enough.</param>
        /// <param name="equalities">The number of equalities in the matrix.</param>
        /// <returns>The matrix <c>A</c> of linear constraints.</returns>
        public double[,] CreateMatrix(int numberOfVariables, out double[] b,
            out double[] tolerances, out int equalities)
        {
            // First of all, separate the equality constraints from the inequalities.
            var constraintArray = this.ToArray();
            constraintArray.Sort((c1, c2) => c1.ShouldBe.CompareTo(c2.ShouldBe), true);

            var numberOfConstraints = constraintArray.Length;
            var A = new double[numberOfConstraints, numberOfVariables];
            b = new double[numberOfConstraints];
            tolerances = new double[numberOfConstraints];
            equalities = 0;

            for (var i = 0; i < constraintArray.Length; i++)
            {
                var constraint = constraintArray[i];

                if (constraint.NumberOfVariables > numberOfVariables)
                {
                    throw new ArgumentException(
                        "The number of variables in the constraint exceeds the number of variables for the problem.");
                }

                for (var j = 0; j < constraint.VariablesAtIndices.Length; j++)
                {
                    var k = constraint.VariablesAtIndices[j];

                    if (k >= numberOfVariables)
                    {
                        throw new ArgumentException(
                            "The constraint refers to a variable index which is not present on the objective function.");
                    }

                    if (constraint.ShouldBe == ConstraintType.GreaterThanOrEqualTo ||
                        constraint.ShouldBe == ConstraintType.EqualTo)
                    {
                        A[i, k] = constraint.CombinedAs[j];
                        b[i] = constraint.Value;
                        tolerances[i] = constraint.Tolerance;
                    }
                    else if (constraint.ShouldBe == ConstraintType.LesserThanOrEqualTo)
                    {
                        A[i, k] = -constraint.CombinedAs[j];
                        b[i] = -constraint.Value;
                        tolerances[i] = constraint.Tolerance;
                    }
                    else
                    {
                        throw new ArgumentException("The provided constraint type is not supported.");
                    }
                }

                if (constraint.ShouldBe == ConstraintType.EqualTo)
                {
                    equalities++;
                }
            }

            return A;
        }

        /// <summary>
        ///     Creates a <see cref="LinearConstraintCollection" /> from a matrix
        ///     specifying the constraint variables and a vector specifying their
        ///     expected value.
        /// </summary>
        /// <param name="a">The constraint matrix.</param>
        /// <param name="b">The constraint values.</param>
        /// <param name="meq">
        ///     The number of equalities at the start of the
        ///     matrix <paramref name="a" />. Contraints thereafter are taken to be
        ///     less than or equal to the constraint values.
        /// </param>
        [Obsolete(
            "This method is obsolete because the convention is inconsistent with GoldfarbIdnani. Use Create(...) instead.")]
        public static LinearConstraintCollection FromMatrix(double[,] a, double[] b, int meq)
        {
            var numberOfVariables = a.GetLength(1);
            var numberOfConstraints = a.GetLength(0);

            var constraints = new LinearConstraint[numberOfConstraints];
            for (var i = 0; i < constraints.Length; i++)
            {
                constraints[i] = new LinearConstraint(numberOfVariables);
                a.GetRow(i, constraints[i].CombinedAs);
                if (i < meq)
                {
                    constraints[i].ShouldBe = ConstraintType.EqualTo;
                }
                else
                {
                    constraints[i].ShouldBe = ConstraintType.LesserThanOrEqualTo;
                }

                constraints[i].Value = b[i];
            }

            return new LinearConstraintCollection(constraints);
        }

        /// <summary>
        ///     Creates a <see cref="LinearConstraintCollection" /> from a matrix
        ///     specifying the constraint variables and a vector specifying their
        ///     expected value.
        /// </summary>
        /// <param name="a">The constraint matrix.</param>
        /// <param name="b">The constraint values.</param>
        /// <param name="meq">
        ///     The number of equalities at the start of the
        ///     matrix <paramref name="a" />. Contraints thereafter are taken to be
        ///     greater than or equal to the constraint values.
        /// </param>
        public static LinearConstraintCollection Create(double[,] a, double[] b, int meq)
        {
            var numberOfVariables = a.GetLength(1);
            var numberOfConstraints = a.GetLength(0);

            var constraints = new LinearConstraint[numberOfConstraints];
            for (var i = 0; i < constraints.Length; i++)
            {
                constraints[i] = new LinearConstraint(numberOfVariables);
                a.GetRow(i, constraints[i].CombinedAs);
                if (i < meq)
                {
                    constraints[i].ShouldBe = ConstraintType.EqualTo;
                }
                else
                {
                    constraints[i].ShouldBe = ConstraintType.GreaterThanOrEqualTo;
                }

                constraints[i].Value = b[i];
            }

            return new LinearConstraintCollection(constraints);
        }
    }
}