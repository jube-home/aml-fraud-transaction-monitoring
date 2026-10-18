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
using Accord.Math.Random;

namespace Accord.Statistics.Distributions.Sampling
{
    /// <summary>
    ///     Metropolis-Hasting sampling algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/Metropolis%E2%80%93Hastings_algorithm">
    ///                         Wikipedia, The Free Encyclopedia. Metropolis-Hastings algorithm.
    ///                         Available on: https://en.wikipedia.org/wiki/Metropolis%E2%80%93Hastings_algorithm
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Joint_probability_distribution">
    ///                         Darren Wilkinson, Metropolis Hastings MCMC when the proposal and target have differing support.
    ///                         Available on:
    ///                         https://darrenjw.wordpress.com/2012/06/04/metropolis-hastings-mcmc-when-the-proposal-and-target-have-differing-support/
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public class MetropolisHasting<T> : IRandomNumberGenerator<T[]>
    {
        private long accepts; // steps accepted

        private bool initialized;
        private T[] next;
        private long steps; // steps so far

        /// <summary>
        ///     Initializes a new instance of the <see cref="MetropolisHasting" /> algorithm.
        /// </summary>
        /// <param name="dimensions">The number of dimensions in each observation.</param>
        /// <param name="logDensity">A function specifying the log probability density of the distribution to be sampled.</param>
        /// <param name="proposal">The proposal distribution that is used to generate new parameter samples to be explored.</param>
        public MetropolisHasting(int dimensions, Func<T[], double> logDensity, Func<T[], T[], T[]> proposal)
        {
            Initialize(dimensions, logDensity, proposal);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MetropolisHasting{T}" /> class.
        /// </summary>
        protected MetropolisHasting()
        {
        }

        /// <summary>
        ///     Gets the last successfully generated observation.
        /// </summary>
        public T[] Current { get; private set; }

        /// <summary>
        ///     Gets or sets a factory method to create random number generators used in this instance.
        /// </summary>
        public Random RandomSource { get; set; }

        /// <summary>
        ///     Gets the log-probability of the
        ///     <see cref="Current">
        ///         last successfully
        ///         generated sample
        ///     </see>
        ///     .
        /// </summary>
        public double CurrentValue { get; private set; }

        /// <summary>
        ///     Gets the log-probability density function of the target distribution.
        /// </summary>
        public Func<T[], double> LogProbabilityDensityFunction { get; private set; }

        /// <summary>
        ///     Gets or sets the move proposal distribution.
        /// </summary>
        public Func<T[], T[], T[]> Proposal { get; private set; }

        /// <summary>
        ///     Gets the acceptance rate for the proposals generated
        ///     by the <see cref="Proposal">proposal distribution</see>.
        /// </summary>
        public double AcceptanceRate => accepts / (double)steps;

        /// <summary>
        ///     Gets the number of dimensions in each observation.
        /// </summary>
        public int NumberOfInputs { get; private set; }

        /// <summary>
        ///     Gets or sets how many initial samples will get discarded as part
        ///     of the initial thermalization (warm-up, initialization) process.
        /// </summary>
        public int Discard { get; set; }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public T[][] Generate(int samples)
        {
            return Generate(samples, new T[samples][]);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public T[][] Generate(int samples, T[][] result)
        {
            if (!initialized)
            {
                WarmUp();
            }

            for (var i = 0; i < samples; i++)
            {
                while (!TryGenerate())
                {
                }

                for (var j = 0; j < Current.Length; j++)
                {
                    result[i][j] = Current[j];
                }
            }

            return result;
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>
        ///     A random observation drawn from this distribution.
        /// </returns>
        public T[] Generate()
        {
            if (!initialized)
            {
                WarmUp();
            }

            while (!TryGenerate())
            {
            }

            return Current;
        }

        /// <summary>
        ///     Initializes the algorithm.
        /// </summary>
        protected void Initialize(int dimensions, Func<T[], double> logDensity, Func<T[], T[], T[]> proposal)
        {
            this.NumberOfInputs = dimensions;
            Current = new T[dimensions];
            next = new T[dimensions];
            LogProbabilityDensityFunction = logDensity;
            this.Proposal = proposal;
            RandomSource = Generator.Random;
        }

        /// <summary>
        ///     Attempts to generate a new observation from the target
        ///     distribution, storing its value in the <see cref="Current" />
        ///     property.
        /// </summary>
        /// <param name="sample">A new observation, if the method has succeed; otherwise, null.</param>
        /// <returns>True if the sample was successfully generated; otherwise, returns false.</returns>
        public bool TryGenerate(out T[] sample)
        {
            if (TryGenerate())
            {
                sample = Current;
                return true;
            }

            sample = null;
            return false;
        }

        /// <summary>
        ///     Attempts to generate a new observation from the target
        ///     distribution, storing its value in the <see cref="Current" />
        ///     property.
        /// </summary>
        /// <returns>True if the sample was successfully generated; otherwise, false.</returns>
        public bool TryGenerate()
        {
            var source = RandomSource;

            next = Proposal(Current, next);

            var pNext = LogProbabilityDensityFunction(next);
            var logRatio = pNext - CurrentValue;
            steps++;

            if (System.Math.Log(source.NextDouble()) < logRatio)
            {
                var aux = Current;
                Current = next;
                next = aux;
                CurrentValue = pNext;
                accepts++;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Thermalizes the sample generation process, generating up to
        ///     <see cref="Discard" /> samples and discarding them. This step
        ///     is done automatically upon the first call to any of the
        ///     <see cref="Generate()" /> functions.
        /// </summary>
        public void WarmUp()
        {
            for (var i = 0; i < Discard; i++)
            {
                TryGenerate();
            }

            initialized = true;
        }
    }
}