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
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Constraint type.
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>
        ///     Equality constraint.
        /// </summary>
        EqualTo = 0,

        /// <summary>
        ///     Inequality constraint specifying a greater than or equal to relationship.
        /// </summary>
        GreaterThanOrEqualTo,

        /// <summary>
        ///     Inequality constraint specifying a lesser than or equal to relationship.
        /// </summary>
        LesserThanOrEqualTo
    }

    /// <summary>
    ///     Constraint with only linear terms.
    /// </summary>
    /// <example>
    ///     <para>
    ///         Linear constraints are commonly used in optimisation routines.
    ///         The framework provides support for linear constraints to be specified
    ///         using a <see cref="string" /> representation, an <see cref="Expression" />
    ///         or using a vector of constraint values.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Math\Optimization\LinearConstraintTest.cs" region="doc_example" />
    /// </example>
    /// <seealso cref="LinearConstraintCollection" />
    public partial class LinearConstraint : IConstraint
    {
        /// <summary>
        ///     Gets the default constant violation tolerance (1e-12).
        /// </summary>
        public const double DefaultTolerance = 1e-12;

        private double[] combinedAs;
        private double[] grad;

        private int[] indices;

        private LinearConstraint()
        {
            Tolerance = DefaultTolerance;
        }

        /// <summary>
        ///     Constructs a new linear constraint.
        /// </summary>
        /// <param name="numberOfVariables">The number of variables in the constraint.</param>
        public LinearConstraint(int numberOfVariables)
            : this()
        {
            NumberOfVariables = numberOfVariables;
            indices = Vector.Range(numberOfVariables);
            combinedAs = Vector.Ones(numberOfVariables);
            ShouldBe = ConstraintType.GreaterThanOrEqualTo;
        }

        /// <summary>
        ///     Constructs a new linear constraint.
        /// </summary>
        /// <param name="coefficients">
        ///     The scalar coefficients specifying
        ///     how variables should be combined in the constraint.
        /// </param>
        public LinearConstraint(params double[] coefficients)
            : this()
        {
            NumberOfVariables = coefficients.Length;
            indices = Vector.Range(0, coefficients.Length);
            CombinedAs = coefficients;
            ShouldBe = ConstraintType.GreaterThanOrEqualTo;
        }

        /// <summary>
        ///     Constructs a new linear constraint.
        /// </summary>
        /// <param name="function">
        ///     The objective function to which
        ///     this constraint refers to.
        /// </param>
        /// <param name="constraint">
        ///     A <see cref="System.String" />
        ///     specifying this constraint, such as "ax + b = c".
        /// </param>
        /// <param name="format">
        ///     The culture information specifying how
        ///     numbers written in the <paramref name="constraint" /> should
        ///     be parsed. Default is CultureInfo.InvariantCulture.
        /// </param>
        public LinearConstraint(IObjectiveFunction function, string constraint, CultureInfo format)
            : this()
        {
            parseString(function, constraint, format);
        }

        /// <summary>
        ///     Constructs a new linear constraint.
        /// </summary>
        /// <param name="function">
        ///     The objective function to which
        ///     this constraint refers to.
        /// </param>
        /// <param name="constraint">
        ///     A <see cref="System.String" />
        ///     specifying this constraint, such as "ax + b = c".
        /// </param>
        public LinearConstraint(IObjectiveFunction function, string constraint)
            : this(function, constraint, CultureInfo.InvariantCulture)
        {
        }

        /// <summary>
        ///     Constructs a new linear constraint.
        /// </summary>
        /// <param name="function">
        ///     The objective function to which this
        ///     constraint refers to.
        /// </param>
        /// <param name="constraint">
        ///     A <see cref="Expression{T}" /> specifying
        ///     this constraint in the form of a lambda expression.
        /// </param>
        public LinearConstraint(IObjectiveFunction function, Expression<Func<bool>> constraint)
            : this()
        {
            parseExpression(function, constraint);
        }

        /// <summary>
        ///     Gets the index of the variables (in respective to the
        ///     objective function) of the variables participating
        ///     in this constraint.
        /// </summary>
        public int[] VariablesAtIndices
        {
            get => indices;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Length != NumberOfVariables)
                {
                    throw new DimensionMismatchException("value");
                }

                indices = value;
                grad = null;
            }
        }

        /// <summary>
        ///     Gets the scalar coefficients combining the
        ///     <see cref="VariablesAtIndices">
        ///         variables
        ///     </see>
        ///     specified by the constraints.
        /// </summary>
        public double[] CombinedAs
        {
            get => combinedAs;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Length != NumberOfVariables)
                {
                    throw new DimensionMismatchException("value");
                }

                combinedAs = value;
                grad = null;
            }
        }

        /// <summary>
        ///     Gets the number of variables in the constraint.
        /// </summary>
        public int NumberOfVariables { get; private set; }

        /// <summary>
        ///     Gets the type of the constraint.
        /// </summary>
        public ConstraintType ShouldBe { get; set; }

        /// <summary>
        ///     Gets the value to be compared to the combined values
        ///     of the variables.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        ///     Gets the violation tolerance for the constraint. Equality
        ///     constraints should set this to a small positive value.
        /// </summary>
        public double Tolerance { get; set; }

        /// <summary>
        ///     Calculates the left hand side of the constraint
        ///     equation given a vector x.
        /// </summary>
        /// <param name="x">The vector.</param>
        /// <returns>
        ///     The left hand side of the constraint equation as evaluated at x.
        /// </returns>
        public double Function(double[] x)
        {
            double sum = 0;

            for (var i = 0; i < indices.Length; i++)
            {
                var index = indices[i];
                var val = x[index];
                var a = CombinedAs[i];

                sum += val * a;
            }

            return sum;
        }

        /// <summary>
        ///     Calculates the gradient of the constraint.
        /// </summary>
        /// <param name="x">The vector.</param>
        /// <returns>The gradient of the constraint.</returns>
        public double[] Gradient(double[] x)
        {
            if (grad == null)
            {
                if (x.Length == indices.Length && indices.IsEqual(Vector.Range(x.Length)))
                {
                    grad = combinedAs;
                }
                else
                {
                    var tmp = new double[x.Length];

                    for (var i = 0; i < indices.Length; i++)
                    {
                        var index = indices[i];
                        tmp[index] = CombinedAs[i];
                    }

                    grad = tmp;
                }
            }

            return grad;
        }

        /// <summary>
        ///     Attempts to create a <see cref="LinearConstraint" />
        ///     from a <see cref="System.String" /> representation.
        /// </summary>
        /// <param name="str">The string containing the constraint in textual form.</param>
        /// <param name="function">The objective function to which this constraint refers to.</param>
        /// <param name="constraint">The resulting constraint, if it could be parsed.</param>
        /// <returns>
        ///     <c>true</c> if the function could be parsed
        ///     from the string, <c>false</c> otherwise.
        /// </returns>
        public static bool TryParse(string str,
            IObjectiveFunction function, out LinearConstraint constraint)
        {
            return TryParse(str, CultureInfo.InvariantCulture, function, out constraint);
        }

        /// <summary>
        ///     Attempts to create a <see cref="LinearConstraint" />
        ///     from a <see cref="System.String" /> representation.
        /// </summary>
        /// <param name="str">The string containing the constraint in textual form.</param>
        /// <param name="function">The objective function to which this constraint refers to.</param>
        /// <param name="constraint">The resulting constraint, if it could be parsed.</param>
        /// <param name="culture">
        ///     The culture information specifying how
        ///     numbers written in the <paramref name="constraint" /> should
        ///     be parsed. Default is CultureInfo.InvariantCulture.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the function could be parsed
        ///     from the string, <c>false</c> otherwise.
        /// </returns>
        public static bool TryParse(string str, CultureInfo culture,
            IObjectiveFunction function, out LinearConstraint constraint)
        {
            // TODO: implement this method without the try-catch block.

            try
            {
                constraint = new LinearConstraint(function, str, culture);
            }
            catch (FormatException)
            {
                constraint = null;
                return false;
            }

            return true;
        }

        private void parseString(IObjectiveFunction function, string constraint, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(constraint))
            {
                throw new FormatException("Constraint is empty.");
            }

            var f = constraint.Replace("*", string.Empty).Replace(" ", string.Empty);

            if (f[0] != '-' && f[0] != '+')
            {
                f = f.Insert(0, "+");
            }

            ConstraintType type;

            if (f.Contains(">="))
            {
                type = ConstraintType.GreaterThanOrEqualTo;
            }
            else if (f.Contains("<="))
            {
                type = ConstraintType.LesserThanOrEqualTo;
            }
            else if (f.Contains("="))
            {
                type = ConstraintType.EqualTo;
            }
            else
            {
                throw new FormatException("Invalid constraint type.");
            }


            var terms = new Dictionary<string, double>();

            var separator = culture.NumberFormat.NumberDecimalSeparator;

            var r = new Regex(@"[\-\+][\s]*(\d*\" + separator + @"{0,1}\d+)?[\s]*([a-zA-Z])*");
            var number = new Regex(@"\d*\" + separator + @"{0,1}\d+");
            var symbol = new Regex(@"[a-zA-Z]");
            var comp = new Regex(@"(>=|<=|=)");


            var sides = comp.Split(f);
            var lhs = sides[0];
            var rhs = sides[2];

            var value = double.Parse(rhs, culture);

            var matches = r.Matches(lhs, 0);

            foreach (Match m in matches)
            {
                var term = m.Value;

                double scalar = term[0] == '-' ? -1 : 1;

                // Extract value
                var coeff = number.Matches(term);

                foreach (Match c in coeff)
                {
                    scalar *= double.Parse(c.Value, culture);
                }

                // Extract symbols
                var symbols = symbol.Matches(term);

                if (symbols.Count == 1)
                {
                    terms.Add(symbols[0].Value, scalar);
                }
                else
                {
                    if (coeff.Count == 1)
                    {
                        value -= scalar;
                    }
                    else if (term != "+")
                    {
                        throw new FormatException("Unexpected expression.");
                    }
                }
            }

            var indices = new List<int>();
            var scalars = new List<double>();

            foreach (var term in terms)
            {
                indices.Add(function.Variables[term.Key]);
                scalars.Add(term.Value);
            }

            NumberOfVariables = indices.Count;
            VariablesAtIndices = indices.ToArray();
            CombinedAs = scalars.ToArray();
            ShouldBe = type;
            Value = value;
        }

        private void parseExpression(IObjectiveFunction function, Expression<Func<bool>> constraint)
        {
            ConstraintType type;

            switch (constraint.Body.NodeType)
            {
                case ExpressionType.Equal:
                    type = ConstraintType.EqualTo; break;

                case ExpressionType.LessThanOrEqual:
                    type = ConstraintType.LesserThanOrEqualTo; break;

                case ExpressionType.GreaterThanOrEqual:
                    type = ConstraintType.GreaterThanOrEqualTo; break;

                default:
                    throw new FormatException("Unexpected expression.");
            }

            var b = constraint.Body as BinaryExpression;
            var terms = new Dictionary<string, double>();
            double value = 0;
            parse(terms, b.Left, ref value);

            var c = b.Right as ConstantExpression;
            value = (double)c.Value - value;

            var indices = new List<int>();
            var scalars = new List<double>();

            foreach (var term in terms)
            {
                indices.Add(function.Variables[term.Key]);
                scalars.Add(term.Value);
            }

            NumberOfVariables = indices.Count;
            VariablesAtIndices = indices.ToArray();
            CombinedAs = scalars.ToArray();
            ShouldBe = type;
            Value = value;
        }

        private static string parse(Dictionary<string, double> terms, Expression expr, ref double value)
        {
            if (expr == null)
            {
                return null;
            }

            var eb = expr as BinaryExpression;
            var em = expr as MemberExpression;
            var eu = expr as UnaryExpression;

            if (em != null) // member expression
            {
                var term = em.Member.Name;
                terms[term] = 1;
                return term;
            }

            if (eb != null) // binary expression
            {
                if (expr.NodeType == ExpressionType.Multiply)
                {
                    return parseMultiply(terms, eb, ref value);
                }

                if (expr.NodeType == ExpressionType.Add)
                {
                    parseAdd(terms, eb, ref value);
                }
                else if (expr.NodeType == ExpressionType.Subtract)
                {
                    parseSubtract(terms, eb, ref value);
                }
            }
            else if (eu != null) // unary expression
            {
                if (expr.NodeType == ExpressionType.UnaryPlus)
                {
                    parseUnaryPlus(terms, eu, ref value);
                }
                else if (expr.NodeType == ExpressionType.Negate)
                {
                    parseNegate(terms, eu, ref value);
                }
                else
                {
                    throw new FormatException("Unexpected expression.");
                }
            }
            else
            {
                throw new FormatException("Unexpected expression.");
            }

            return null;
        }
    }
}