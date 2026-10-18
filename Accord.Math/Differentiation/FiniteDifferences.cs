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
using System.Threading;

namespace Accord.Math.Differentiation
{
    /// <summary>
    ///     Derivative approximation by finite differences.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Numerical differentiation is a technique of numerical analysis to produce an estimate
    ///         of the derivative of a mathematical function or function subroutine using values from
    ///         the function and perhaps other knowledge about the function.
    ///     </para>
    ///     <para>
    ///         A finite difference is a mathematical expression of the form f(x + b) − f(x + a). If a
    ///         finite difference is divided by b − a, one gets a difference quotient. The approximation
    ///         of derivatives by finite differences plays a central role in finite difference methods
    ///         for the numerical solution of differential equations, especially boundary value problems.
    ///     </para>
    ///     <para>
    ///         This class implements Newton's finite differences method for approximating the derivatives
    ///         of a multivariate function. A simplified version of the class is also available for
    ///         <see cref="Derivative(Func{double,double}, double, int)">
    ///             univariate functions through
    ///             its Derivative static methods
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Finite_difference">
    ///                         Wikipedia, The Free Encyclopedia. Finite difference. Available on:
    ///                         http://en.wikipedia.org/wiki/Finite_difference
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Trent F. Guidry, Calculating derivatives of a function numerically. Available on:
    ///                     http://www.trentfguidry.net/post/2009/07/12/Calculate-derivatives-function-numerically.aspx
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Math\Differentiation\FiniteDifferencesTest.cs" region="doc_gradient" />
    ///     <code source="Unit Tests\Accord.Tests.Math\Differentiation\FiniteDifferencesTest.cs" region="doc_hessian" />
    /// </example>
    /// <seealso cref="Integration" />
    public class FiniteDifferences
    {
        private const double DEFAULT_STEPSIZE = 1e-2;
        private const int DEFAULT_NPOINTS = 3;
        private const int DEFAULT_ORDER = 1;

        private static readonly double[][,] coefficientCache = CreateCoefficients(3);
        private int center;

        private double[][,] coefficients; // differential coefficients
        private int derivativeOrder; // whether to compute first, second, ... derivatives

        private ThreadLocal<double[][]> points; // cache for interpolation points

        /// <summary>
        ///     Initializes a new instance of the <see cref="FiniteDifferences" /> class.
        /// </summary>
        /// <param name="variables">The number of free parameters in the function.</param>
        public FiniteDifferences(int variables)
        {
            init(variables);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FiniteDifferences" /> class.
        /// </summary>
        /// <param name="variables">The number of free parameters in the function.</param>
        /// <param name="order">The derivative order that should be obtained. Default is 1.</param>
        public FiniteDifferences(int variables, int order)
        {
            init(variables, order: order);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FiniteDifferences" /> class.
        /// </summary>
        /// <param name="variables">The number of free parameters in the function.</param>
        /// <param name="order">The derivative order that should be obtained. Default is 1.</param>
        /// <param name="stepSize">The relative step size used to approximate the derivatives. Default is 0.01.</param>
        public FiniteDifferences(int variables, int order, double stepSize)
        {
            init(variables, order: order, stepSize: stepSize);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FiniteDifferences" /> class.
        /// </summary>
        /// <param name="variables">The number of free parameters in the function.</param>
        /// <param name="function">The function to be differentiated.</param>
        public FiniteDifferences(int variables, Func<double[], double> function)
        {
            init(variables, function);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FiniteDifferences" /> class.
        /// </summary>
        /// <param name="variables">The number of free parameters in the function.</param>
        /// <param name="order">The derivative order that should be obtained. Default is 1.</param>
        /// <param name="function">The function to be differentiated.</param>
        public FiniteDifferences(int variables, Func<double[], double> function, int order)
        {
            init(variables, function, order);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FiniteDifferences" /> class.
        /// </summary>
        /// <param name="variables">The number of free parameters in the function.</param>
        /// <param name="order">The derivative order that should be obtained. Default is 1.</param>
        /// <param name="stepSize">The relative step size used to approximate the derivatives. Default is 0.01.</param>
        /// <param name="function">The function to be differentiated.</param>
        public FiniteDifferences(int variables, Func<double[], double> function, int order, double stepSize)
        {
            init(variables, function, order, stepSize: stepSize);
        }

        /// <summary>
        ///     Gets or sets the function to be differentiated.
        /// </summary>
        public Func<double[], double> Function { get; set; }

        /// <summary>
        ///     Gets or sets the relative step size used to approximate the derivatives. Default is 1e-2.
        ///     Setting this property updates the step size for all parameters at once. To adjust only a
        ///     single parameter, please refer to <see cref="StepSizes" /> instead.
        /// </summary>
        public double StepSize
        {
            get => StepSizes.Max();
            set
            {
                if (StepSizes == null)
                {
                    StepSizes = new double[NumberOfVariables];
                }

                for (var i = 0; i < StepSizes.Length; i++)
                {
                    StepSizes[i] = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the relative step sizes used to approximate the derivatives. Default is 1e-2.
        /// </summary>
        public double[] StepSizes { get; private set; }

        /// <summary>
        ///     Gets or sets the order of the partial derivatives to be
        ///     obtained. Default is 1 (computes the first derivative).
        /// </summary>
        public int Order
        {
            get => derivativeOrder;
            set
            {
                if (value >= NumberOfPoints)
                {
                    throw new ArgumentException(
                        "The order must be less than the number of points being used for interpolation." +
                        " In order to use a higher order, please increase the number of interpolation points first.",
                        "value");
                }

                derivativeOrder = value;
            }
        }

        /// <summary>
        ///     Gets or sets the number of points to be used when
        ///     computing the approximation. Default is 3.
        /// </summary>
        public int NumberOfPoints
        {
            get => coefficients.Length;
            set
            {
                if (value % 2 != 1)
                {
                    throw new ArgumentException("The number of points must be odd.", "value");
                }

                if (derivativeOrder >= value)
                {
                    throw new ArgumentException(
                        "The number of points must be higher than the desired differentiation order." +
                        " In order to use less interpolation points, please decrease the differentiation order first.",
                        "value");
                }

                coefficients = CreateCoefficients(value);
                points = new ThreadLocal<double[][]>(() => Jagged.Zeros(2, coefficients.Length));
                center = (coefficients.Length - 1) / 2;
            }
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="NumberOfPoints" /> instead.
        /// </summary>
        [Obsolete("Please use NumberOfPoints instead.")]
        public int Points
        {
            get => NumberOfPoints;
            set => NumberOfPoints = value;
        }

        /// <summary>
        ///     Gets the number of parameters expected by the <see cref="Function" /> to be differentiated.
        /// </summary>
        public int NumberOfVariables { get; private set; }

        private void init(int variables, Func<double[], double> function = null, int? order = null, int? points = null,
            double? stepSize = null)
        {
            NumberOfVariables = variables;
            Function = function;
            StepSize = stepSize.GetValueOrDefault(DEFAULT_STEPSIZE);
            NumberOfPoints = points.GetValueOrDefault(DEFAULT_NPOINTS);
            Order = order.GetValueOrDefault(DEFAULT_ORDER);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Gradient(double[])" /> instead.
        /// </summary>
        [Obsolete("Please use Gradient(double[]) instead.")]
        public double[] Compute(params double[] x)
        {
            return Gradient(x);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Gradient(double[], double[])" /> instead.
        /// </summary>
        [Obsolete("Please use Gradient(double[], double[]) instead.")]
        public void Compute(double[] x, double[] gradient)
        {
            Gradient(x, gradient);
        }

        /// <summary>
        ///     Computes the gradient at the given point <c>x</c>.
        /// </summary>
        /// <param name="x">The point where to compute the gradient.</param>
        /// <returns>The gradient of the function evaluated at point <c>x</c>.</returns>
        public double[] Gradient(double[] x)
        {
            var gradient = new double[x.Length];
            Gradient(x, gradient);
            return gradient;
        }

        /// <summary>
        ///     Computes the gradient at the given point <paramref name="x" />,
        ///     storing the result at <paramref name="result" />.
        /// </summary>
        /// <param name="x">The point where to compute the gradient.</param>
        /// <param name="result">The gradient of the function evaluated at point <c>x</c>.</param>
        public double[] Gradient(double[] x, double[] result)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (x.Length != NumberOfVariables)
            {
                throw new ArgumentException("The number of dimensions does not match.", "x");
            }

            if (Function == null)
            {
                throw new InvalidOperationException("The Function has not been defined.");
            }

            if (x.Length < result.Length)
            {
                throw new DimensionMismatchException("result",
                    "Gradient vector must have at least the same size as x.");
            }

            if (Function == null)
            {
                throw new InvalidOperationException("The Function has not been defined.");
            }

            var pointCache = points.Value[0];
            var centerValue = Function(x);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = derivative(Function, x, i, pointCache, centerValue);
            }

            return result;
        }

        /// <summary>
        ///     Computes the Hessian matrix at given point <c>x</c>.
        /// </summary>
        /// <param name="x">The point where to compute the gradient.</param>
        /// <returns>The Hessian of the function evaluated at point <c>x</c>.</returns>
        public double[][] Hessian(double[] x)
        {
            return Hessian(x, Jagged.Zeros(x.Length, x.Length));
        }

        /// <summary>
        ///     Computes the Hessian matrix at given point <c>x</c>.
        /// </summary>
        /// <param name="x">The point where to compute the gradient.</param>
        /// <param name="result">The matrix where the Hessian should be stored.</param>
        /// <returns>The Hessian of the function evaluated at point <c>x</c>.</returns>
        public double[][] Hessian(double[] x, double[][] result)
        {
            var pointCache1 = points.Value[0];
            var pointCache2 = points.Value[1];
            var centerValue = Function(x);

            for (var i = 0; i < x.Length; i++)
            {
                var localI = i;
                Func<double[], double> f = x0 => derivative(Function, x0, localI, pointCache2, Function(x0));

                // Process elements at the diagonal
                result[i][i] = derivative(f, x, i, pointCache1, centerValue);

                // Process off-diagonal elements
                for (var j = 0; j < i; j++)
                {
                    var d = derivative(f, x, j, pointCache1, centerValue);
                    result[j][i] = d; // Hessian is symmetric, no need
                    result[i][j] = d; // to compute the derivative twice
                }
            }

            return result;
        }

        /// <summary>
        ///     Computes the derivative at point <c>x_i</c>.
        /// </summary>
        private double derivative(Func<double[], double> func, double[] x, int index, double[] points,
            double centerValue)
        {
            //if (order >= coefficients.Length)
            //{
            //    throw new ArgumentOutOfRangeException("The derivative order needs to be less than the number of " +
            //        "interpolation points. To use a higher order, please adjust the NumberOfPoints property first.");
            //}

            var step = GetUniformlySampledPoints(func, x, index, centerValue, points);

            return Interpolate(coefficients, points, derivativeOrder, center, step);
        }

        private double GetUniformlySampledPoints(Func<double[], double> func, double[] x, int index, double centerValue,
            double[] points)
        {
            // Saves the original parameter value
            var original = x[index];
            var step = StepSizes[index];

            // Create the interpolation points
            for (var i = 0; i < points.Length; i++)
            {
                if (i != center)
                {
                    // Make a small perturbation in the original parameter
                    x[index] = original + (i - center) * step;

                    // Recompute the function to measure its importance
                    points[i] = func(x);
                }
                else
                {
                    // The center point is the original function output, so 
                    points[i] = centerValue; // we can save one function call
                }
            }

            // Reverts the modified value
            x[index] = original;

            return step;
        }

        /// <summary>
        ///     Interpolates the points to obtain an estimative of the <paramref name="order" /> derivative.
        /// </summary>
        private static double Interpolate(double[][,] coefficients, double[] points, int order, int center, double step)
        {
            var sum = 0.0;

            for (var i = 0; i < points.Length; i++)
            {
                var c = coefficients[center][order, i];

                if (c != 0)
                {
                    sum += c * points[i];
                }
            }

            var r = sum / System.Math.Pow(step, order);
            return r;
        }

        /// <summary>
        ///     Creates the interpolation coefficient table for interpolated numerical differentation.
        /// </summary>
        /// <param name="numberOfPoints">The number of points in the tableau.</param>
        public static double[][,] CreateCoefficients(int numberOfPoints)
        {
            if (numberOfPoints % 2 != 1)
            {
                throw new ArgumentException("The number of points must be odd.", "numberOfPoints");
            }

            // Compute difference coefficient table
            var c = new double[numberOfPoints][,]; // [center][order, i];

            var fac = Special.Factorial(numberOfPoints);

            for (var i = 0; i < numberOfPoints; i++)
            {
                var deltas = new double[numberOfPoints, numberOfPoints];

                for (var j = 0; j < numberOfPoints; j++)
                {
                    var h = 1.0;
                    double delta = j - i;

                    for (var k = 0; k < numberOfPoints; k++)
                    {
                        deltas[j, k] = h / Special.Factorial(k);
                        h *= delta;
                    }
                }

                c[i] = deltas.Inverse();

                for (var j = 0; j < numberOfPoints; j++)
                {
                    for (var k = 0; k < numberOfPoints; k++)
                    {
                        c[i][j, k] = System.Math.Round(c[i][j, k] * fac, MidpointRounding.AwayFromZero) / fac;
                    }
                }
            }

            return c;
        }

        /// <summary>
        ///     Computes the derivative for a simpler unidimensional function.
        /// </summary>
        /// <param name="function">The function to be differentiated.</param>
        /// <param name="value">The value <c>x</c> at which the derivative should be evaluated.</param>
        /// <param name="order">The derivative order that should be obtained. Default is 1.</param>
        /// <returns>The derivative of the function at the point <paramref name="value">x</paramref>.</returns>
        public static double Derivative(Func<double, double> function, double value, int order)
        {
            return Derivative(function, value, order, DEFAULT_STEPSIZE);
        }

        /// <summary>
        ///     Computes the derivative for a simpler unidimensional function.
        /// </summary>
        /// <param name="function">The function to be differentiated.</param>
        /// <param name="value">The value <c>x</c> at which the derivative should be evaluated.</param>
        /// <returns>The derivative of the function at the point <paramref name="value">x</paramref>.</returns>
        public static double Derivative(Func<double, double> function, double value)
        {
            return Derivative(function, value, DEFAULT_ORDER);
        }

        /// <summary>
        ///     Computes the derivative for a simpler unidimensional function.
        /// </summary>
        /// <param name="function">The function to be differentiated.</param>
        /// <param name="order">The derivative order that should be obtained. Default is 1.</param>
        /// <param name="stepSize">The relative step size used to approximate the derivatives. Default is 0.01.</param>
        /// <param name="value">The value <c>x</c> at which the derivative should be evaluated.</param>
        /// <returns>The derivative of the function at the point <paramref name="value">x</paramref>.</returns>
        public static double Derivative(Func<double, double> function, double value, int order, double stepSize)
        {
            // This method is specific for univariate functions.
            // TODO: Separate FiniteDifferences into classes for univariate, multivariate and vector-valued functions

            var output = function(value);
            var original = value;

            if (System.Math.Abs(original) > 1e-10)
            {
                stepSize *= System.Math.Abs(original);
            }
            else
            {
                stepSize = 1e-10;
            }


            // Create the interpolation points
            var outputs = new double[coefficientCache.Length];

            var center = (outputs.Length - 1) / 2;
            for (var i = 0; i < outputs.Length; i++)
            {
                if (i != center)
                {
                    // Recompute the function to measure its importance
                    outputs[i] = function(original + (i - center) * stepSize);
                }
                else
                {
                    // The center point is the original function
                    outputs[i] = output;
                }
            }

            return Interpolate(coefficientCache, outputs, order, center, stepSize);
        }

        /// <summary>
        ///     Obtains the gradient function for a multidimensional function.
        /// </summary>
        /// <param name="function">The function to be differentiated.</param>
        /// <param name="variables">The number of parameters for the function.</param>
        /// <param name="order">The derivative order that should be obtained. Default is 1.</param>
        /// <returns>The gradient function of the given <paramref name="function" />.</returns>
        public static Func<double[], double[]> Gradient(Func<double[], double> function, int variables, int order = 1)
        {
            return new FiniteDifferences(variables, function, order).Gradient;
        }

        /// <summary>
        ///     Obtains the Hessian function for a multidimensional function.
        /// </summary>
        /// <param name="function">The function to be differentiated.</param>
        /// <param name="variables">The number of parameters for the function.</param>
        /// <returns>The gradient function of the given <paramref name="function" />.</returns>
        public static Func<double[], double[][]> Hessian(Func<double[], double> function, int variables)
        {
            return new FiniteDifferences(variables, function).Hessian;
        }
    }
}