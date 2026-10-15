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

namespace Accord.Math.Integration
{
    /// <summary>
    ///     Romberg's method for numerical integration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In numerical analysis, Romberg's method (Romberg 1955) is used to estimate
    ///         the definite integral <c>∫_a^b(x) dx</c> by applying Richardson extrapolation
    ///         repeatedly on the trapezium rule or the rectangle rule (midpoint rule). The
    ///         estimates generate a triangular array. Romberg's method is a Newton–Cotes
    ///         formula – it evaluates the integrand at equally spaced points. The integrand
    ///         must have continuous derivatives, though fairly good results may be obtained
    ///         if only a few derivatives exist. If it is possible to evaluate the integrand
    ///         at unequally spaced points, then other methods such as Gaussian quadrature
    ///         and Clenshaw–Curtis quadrature are generally more accurate.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Romberg's_method">
    ///                         Wikipedia, The Free Encyclopedia. Romberg's method. Available on:
    ///                         http://en.wikipedia.org/wiki/Romberg's_method
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Let's say we would like to compute the definite integral of the function
    ///         <c>f(x) = cos(x)</c> in the interval -1 to +1 using a variety of integration
    ///         methods, including the <see cref="TrapezoidalRule" />, <see cref="RombergMethod" />
    ///         and <see cref="NonAdaptiveGaussKronrod" />. Those methods can compute definite
    ///         integrals where the integration interval is finite:
    ///     </para>
    ///     <code>
    /// // Declare the function we want to integrate
    /// Func&lt;double, double> f = (x) => Math.Cos(x);
    /// 
    /// // We would like to know its integral from -1 to +1
    /// double a = -1, b = +1;
    /// 
    /// // Integrate!
    /// double trapez  = TrapezoidalRule.Integrate(f, a, b, steps: 1000); // 1.6829414
    /// double romberg = RombergMethod.Integrate(f, a, b);                // 1.6829419
    /// double nagk    = NonAdaptiveGaussKronrod.Integrate(f, a, b);      // 1.6829419
    /// </code>
    ///     <para>
    ///         Moreover, it is also possible to calculate the value of improper integrals
    ///         (it is, integrals with infinite bounds) using <see cref="InfiniteAdaptiveGaussKronrod" />,
    ///         as shown below. Let's say we would like to compute the area under the Gaussian
    ///         curve from -infinite to +infinite. While this function has infinite bounds, this
    ///         function is known to integrate to 1.
    ///     </para>
    ///     <code>
    /// // Declare the Normal distribution's density function (which is the Gaussian's bell curve)
    /// Func&lt;double, double> g = (x) => (1 / Math.Sqrt(2 * Math.PI)) * Math.Exp(-(x * x) / 2);
    /// 
    /// // Integrate!
    /// double iagk = InfiniteAdaptiveGaussKronrod.Integrate(g,
    ///     Double.NegativeInfinity, Double.PositiveInfinity);   // Result should be 0.99999...
    /// </code>
    /// </example>
    /// <seealso cref="TrapezoidalRule" />
    /// <seealso cref="NonAdaptiveGaussKronrod" />
    /// <seealso cref="InfiniteAdaptiveGaussKronrod" />
    /// <seealso cref="MonteCarloIntegration" />
    public class RombergMethod : IUnivariateIntegration, INumericalIntegration
    {
        private readonly double[] s;
        private DoubleRange range;

        /// <summary>
        ///     Constructs a new <see cref="RombergMethod">Romberg's integration method</see>.
        /// </summary>
        public RombergMethod()
            : this(6)
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="RombergMethod">Romberg's integration method</see>.
        /// </summary>
        /// <param name="function">The unidimensional function whose integral should be computed.</param>
        public RombergMethod(Func<double, double> function)
            : this(6, function)
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="RombergMethod">Romberg's integration method</see>.
        /// </summary>
        /// <param name="function">The unidimensional function whose integral should be computed.</param>
        /// <param name="a">The beginning of the integration interval.</param>
        /// <param name="b">The ending of the integration interval.</param>
        public RombergMethod(Func<double, double> function, double a, double b)
            : this(6, function, a, b)
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="RombergMethod">Romberg's integration method</see>.
        /// </summary>
        /// <param name="steps">The number of steps used in Romberg's method. Default is 6.</param>
        public RombergMethod(int steps)
        {
            s = new double[steps];
            Range = new DoubleRange(0, 1);
        }

        /// <summary>
        ///     Constructs a new <see cref="RombergMethod">Romberg's integration method</see>.
        /// </summary>
        /// <param name="steps">The number of steps used in Romberg's method. Default is 6.</param>
        /// <param name="function">The unidimensional function whose integral should be computed.</param>
        public RombergMethod(int steps, Func<double, double> function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            Range = new DoubleRange(0, 1);
            Function = function;
            s = new double[steps];
        }

        /// <summary>
        ///     Constructs a new <see cref="RombergMethod">Romberg's integration method</see>.
        /// </summary>
        /// <param name="steps">The number of steps used in Romberg's method. Default is 6.</param>
        /// <param name="function">The unidimensional function whose integral should be computed.</param>
        /// <param name="a">The beginning of the integration interval.</param>
        /// <param name="b">The ending of the integration interval.</param>
        public RombergMethod(int steps, Func<double, double> function, double a, double b)
        {
            if (double.IsInfinity(a) || double.IsNaN(a))
            {
                throw new ArgumentOutOfRangeException("a");
            }

            if (double.IsInfinity(b) || double.IsNaN(b))
            {
                throw new ArgumentOutOfRangeException("b");
            }

            Function = function;
            Range = new DoubleRange(a, b);
            s = new double[steps];
        }

        /// <summary>
        ///     Gets or sets the number of steps used
        ///     by Romberg's method. Default is 6.
        /// </summary>
        public int Steps => s.Length;

        /// <summary>
        ///     Gets or sets the unidimensional function
        ///     whose integral should be computed.
        /// </summary>
        public Func<double, double> Function { get; set; }

        /// <summary>
        ///     Gets the numerically computed result of the
        ///     definite integral for the specified function.
        /// </summary>
        public double Area { get; private set; }

        /// <summary>
        ///     Gets or sets the input range under
        ///     which the integral must be computed.
        /// </summary>
        public DoubleRange Range
        {
            get => range;
            set
            {
                if (double.IsInfinity(range.Min) || double.IsNaN(range.Min))
                {
                    throw new ArgumentOutOfRangeException("value", "Minimum is out of range.");
                }

                if (double.IsInfinity(range.Max) || double.IsNaN(range.Max))
                {
                    throw new ArgumentOutOfRangeException("value", "Maximum is out of range.");
                }

                range = value;
            }
        }

        /// <summary>
        ///     Computes the area of the function under the selected <see cref="Range" />.
        ///     The computed value will be available at this object's <see cref="Area" />.
        /// </summary>
        /// <returns>
        ///     True if the integration method succeeds, false otherwise.
        /// </returns>
        public bool Compute()
        {
            for (var i = 0; i < s.Length; i++)
            {
                s[i] = 1;
            }

            var a = range.Min;
            var b = range.Max;

            for (var k = 0; k < s.Length; k++)
            {
                var sum = s[0];
                s[0] = TrapezoidalRule.Integrate(Function, a, b, 1 << k);

                for (var i = 1; i <= k; i++)
                {
                    var p = (int)System.Math.Pow(4, i);
                    s[k] = (p * s[i - 1] - sum) / (p - 1);

                    sum = s[i];
                    s[i] = s[k];
                }
            }

            Area = s[s.Length - 1];

            return true;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new RombergMethod(Steps,
                Function, Range.Min, Range.Max);

            return clone;
        }

        /// <summary>
        ///     Computes the area under the integral for the given function,
        ///     in the given integration interval, using Romberg's method.
        /// </summary>
        /// <param name="func">The unidimensional function whose integral should be computed.</param>
        /// <param name="a">The beginning of the integration interval.</param>
        /// <param name="b">The ending of the integration interval.</param>
        /// <returns>The integral's value in the current interval.</returns>
        public static double Integrate(Func<double, double> func, double a, double b)
        {
            return Integrate(func, a, b, 6);
        }

        /// <summary>
        ///     Computes the area under the integral for the given function,
        ///     in the given integration interval, using Romberg's method.
        /// </summary>
        /// <param name="steps">The number of steps used in Romberg's method. Default is 6.</param>
        /// <param name="func">The unidimensional function whose integral should be computed.</param>
        /// <param name="a">The beginning of the integration interval.</param>
        /// <param name="b">The ending of the integration interval.</param>
        /// <returns>The integral's value in the current interval.</returns>
        public static double Integrate(Func<double, double> func, double a, double b, int steps)
        {
            var romberg = new RombergMethod(steps, func, a, b);

            romberg.Compute();

            return romberg.Area;
        }
    }
}