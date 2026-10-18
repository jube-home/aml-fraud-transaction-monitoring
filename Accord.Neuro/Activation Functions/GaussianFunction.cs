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
using Accord.Neuro.Networks;
using Accord.Neuro.Neurons;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Neuro.ActivationFunctions
{
    /// <summary>
    ///     Gaussian stochastic activation function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Gaussian activation function can be used to create
    ///         <see cref="StochasticNeuron">
    ///             Stochastic Neurons
    ///         </see>
    ///         , which can in turn be used to create
    ///         <see cref="DeepBeliefNetwork">
    ///             Deep Belief Networks
    ///         </see>
    ///         and
    ///         <see cref="RestrictedBoltzmannMachine">
    ///             Restricted Boltzmann
    ///             Machines
    ///         </see>
    ///         . In contrast to the <see cref="BernoulliFunction" />, the Gaussian can be used
    ///         to model continuous inputs in Deep Belief Networks. If, however, the inputs of the problem
    ///         being learned are discrete in nature, the use of a Bernoulli function would be more indicated.
    ///     </para>
    ///     <para>
    ///         The Gaussian activation function is modeled after a
    ///         <see cref="NormalDistribution">
    ///             Gaussian (Normal) probability distribution
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         This function assumes output variables have been
    ///         normalized to have zero mean and unit variance.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Create a Gaussian function with slope alpha = 4.2
    ///   GaussianFunction function = new GaussianFunction(4.2);
    ///   
    ///   // Computes the function output (linear, y = alpha * x)
    ///   double y = function.Function(x: 0.2); // 4.2 * 2 = 0.48
    ///   
    ///   // Draws a sample from a Gaussian distribution with
    ///   // mean given by the function output y (previously 0.48)
    ///   double z = function.Generate(x: 0.4); // (random, between 0 and 1)
    ///   
    ///   // Please note that the above is completely equivalent 
    ///   // to computing the line below (remember, 0.48 == y)
    ///   double w = function.Generate2(y: 0.48); // (random, between 0 and 1)
    ///   
    ///   
    ///   // We can also compute the derivative of the sigmoid function
    ///   double d = function.Derivative(x: 0.2); // 4.2 (the slope)
    ///   
    ///   // Or compute the derivative given the functions' output y
    ///   double e = function.Derivative2(y: 0.2); // 4.2 (the slope)
    /// </code>
    /// </example>
    /// <seealso cref="BernoulliFunction" />
    /// <seealso cref="NormalDistribution" />
    /// <seealso cref="DeepBeliefNetwork" />
    [Serializable]
    public class GaussianFunction : IStochasticFunction
    {
        // linear slope value
        private double alpha = 1;

        // function output range
        private DoubleRange range = new(-1, +1);

        /// <summary>
        ///     Creates a new <see cref="GaussianFunction" />.
        /// </summary>
        /// <param name="alpha">The linear slope value. Default is 1.</param>
        public GaussianFunction(double alpha)
        {
            this.alpha = alpha;
        }

        /// <summary>
        ///     Creates a new <see cref="GaussianFunction" />.
        /// </summary>
        public GaussianFunction()
            : this(1.0)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GaussianFunction" /> class.
        /// </summary>
        public GaussianFunction(double alpha, DoubleRange range)
        {
            Alpha = alpha;
            Range = range;
        }

        /// <summary>
        ///     Linear slope value.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is set to <b>1</b>.</para>
        /// </remarks>
        public double Alpha
        {
            get => alpha;
            set => alpha = value;
        }

        /// <summary>
        ///     Function output range.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is set to [-1;+1]</para>
        /// </remarks>
        public DoubleRange Range
        {
            get => range;
            set => range = value;
        }

        /// <summary>
        ///     Calculates function value.
        /// </summary>
        /// <param name="x">Function input value.</param>
        /// <returns>Function output value, <i>f(x)</i>.</returns>
        /// <remarks>The method calculates function value at point <paramref name="x" />.</remarks>
        public double Function(double x)
        {
            var y = alpha * x;

            if (y > range.Max)
            {
                return range.Max;
            }

            if (y < range.Min)
            {
                return range.Min;
            }

            return y;
        }

        /// <summary>
        ///     Samples a value from the function given a input value.
        /// </summary>
        /// <param name="x">Function input value.</param>
        /// <returns>
        ///     Draws a random value from the function.
        /// </returns>
        public double Generate(double x)
        {
            // assume zero-mean noise
            var y = alpha * x + NormalDistribution.Random();

            if (y > range.Max)
            {
                y = range.Max;
            }
            else if (y < range.Min)
            {
                y = range.Min;
            }

            return y;
        }

        /// <summary>
        ///     Samples a value from the function given a function output value.
        /// </summary>
        /// <param name="y">
        ///     Function output value - the value, which was obtained
        ///     with the help of <see cref="Function" /> method.
        /// </param>
        /// <returns>
        ///     Draws a random value from the function.
        /// </returns>
        public double Generate2(double y)
        {
            y = y + NormalDistribution.Random();

            if (y > range.Max)
            {
                y = range.Max;
            }
            else if (y < range.Min)
            {
                y = range.Min;
            }

            return y;
        }

        /// <summary>
        ///     Calculates function derivative.
        /// </summary>
        /// <param name="x">Function input value.</param>
        /// <returns>Function derivative, <i>f'(x)</i>.</returns>
        /// <remarks>The method calculates function derivative at point <paramref name="x" />.</remarks>
        public double Derivative(double x)
        {
            var y = alpha * x;

            if (y <= range.Min || y >= range.Max)
            {
                return 0;
            }

            return alpha;
        }

        /// <summary>
        ///     Calculates function derivative.
        /// </summary>
        /// <param name="y">
        ///     Function output value - the value, which was obtained
        ///     with the help of <see cref="Function" /> method.
        /// </param>
        /// <returns>Function derivative, <i>f'(x)</i>.</returns>
        /// <remarks>
        ///     <para>
        ///         The method calculates the same derivative value as the
        ///         <see cref="Derivative" /> method, but it takes not the input <b>x</b> value
        ///         itself, but the function value, which was calculated previously with
        ///         the help of <see cref="Function" /> method.
        ///     </para>
        ///     <para>
        ///         <note>
        ///             Some applications require as function value, as derivative value,
        ///             so they can save the amount of calculations using this method to calculate derivative.
        ///         </note>
        ///     </para>
        /// </remarks>
        public double Derivative2(double y)
        {
            if (y <= range.Min || y >= range.Max)
            {
                return 0;
            }

            return alpha;
        }
    }
}