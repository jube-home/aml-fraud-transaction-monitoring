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
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.MachineLearning;
using Accord.Math;

namespace Accord.Statistics.Distributions.Fitting
{
    /// <summary>
    ///     Expectation Maximization algorithm for mixture model fitting.
    /// </summary>
    /// <typeparam name="TObservation">The type of the observations being fitted.</typeparam>
    /// <remarks>
    ///     <para>
    ///         This class implements a generic version of the Expectation-Maximization algorithm
    ///         which can be used with both univariate or multivariate distribution types.
    ///     </para>
    /// </remarks>
    public class ExpectationMaximization<TObservation> : ParallelLearningBase
    {
        /// <summary>
        ///     Creates a new <see cref="ExpectationMaximization{TObservation}" /> algorithm.
        /// </summary>
        /// <param name="coefficients">The initial coefficient values.</param>
        /// <param name="distributions">The initial component distributions.</param>
        public ExpectationMaximization(double[] coefficients,
            IFittableDistribution<TObservation>[] distributions)
        {
            Coefficients = coefficients;
            Distributions = distributions;
            Convergence = new RelativeConvergence(0, 1e-3);
            Gamma = new double[coefficients.Length][];
        }

        /// <summary>
        ///     Gets or sets the fitting options to be used
        ///     when any of the component distributions need
        ///     to be estimated from the data.
        /// </summary>
        public IFittingOptions InnerOptions { get; set; }

        /// <summary>
        ///     Gets or sets convergence properties for
        ///     the expectation-maximization algorithm.
        /// </summary>
        public ISingleValueConvergence Convergence { get; set; }

        /// <summary>
        ///     Gets the current coefficient values.
        /// </summary>
        public double[] Coefficients { get; }

        /// <summary>
        ///     Gets the current component distribution values.
        /// </summary>
        public IFittableDistribution<TObservation>[] Distributions { get; }

        /// <summary>
        ///     Gets the responsibility of each input vector when estimating
        ///     each of the component distributions, in the last iteration.
        /// </summary>
        public double[][] Gamma { get; }

        /// <summary>
        ///     Estimates a mixture distribution for the given observations
        ///     using the Expectation-Maximization algorithm.
        /// </summary>
        /// <param name="observations">The observations from the mixture distribution.</param>
        /// <returns>The log-likelihood of the estimated mixture model.</returns>
        public double Compute(TObservation[] observations)
        {
            return compute(observations, null);
        }

        /// <summary>
        ///     Estimates a mixture distribution for the given observations
        ///     using the Expectation-Maximization algorithm, assuming different
        ///     weights for each observation.
        /// </summary>
        /// <param name="observations">The observations from the mixture distribution.</param>
        /// <param name="weights">The weight of each observation.</param>
        /// <returns>The log-likelihood of the estimated mixture model.</returns>
        public double Compute(TObservation[] observations, double[] weights)
        {
            return compute(observations, weights);
        }

        private double compute(TObservation[] observations, double[] weights)
        {
            // Estimation parameters

            var coefficients = Coefficients;
            var components = Distributions;

            double weightSum = 1;
            if (weights != null)
            {
                weightSum = weights.Sum();
            }

            // 1. Initialize means, covariances and mixing coefficients
            //    and evaluate the initial value of the log-likelihood

            var N = observations.Length;

            // Initialize responsibilities
            var norms = new double[N];
            for (var k = 0; k < Gamma.Length; k++)
            {
                Gamma[k] = new double[N];
            }

            // Clone the current distribution values
            var pi = (double[])coefficients.Clone();
            var pdf = new IFittableDistribution<TObservation>[components.Length];

            for (var i = 0; i < components.Length; i++)
            {
                pdf[i] = components[i];
            }

            // Prepare the iteration
            Convergence.NewValue = LogLikelihood(pi, pdf, observations, weights, weightSum, ParallelOptions);

            var componentOptions = InnerOptions as IComponentOptions;


            // Start
            do
            {
                // 2. Expectation: Evaluate the component distributions 
                //    responsibilities using the current parameter values.

                Parallel.For(0, Gamma.Length, ParallelOptions, k =>
                {
                    var gammak = Gamma[k];
                    for (var i = 0; i < observations.Length; i++)
                    {
                        gammak[i] = pi[k] * pdf[k].ProbabilityFunction(observations[i]);
                    }
                });

                Parallel.For(0, norms.Length, ParallelOptions, i =>
                {
                    double sum = 0;
                    for (var k = 0; k < Gamma.Length; k++)
                    {
                        sum += Gamma[k][i];
                    }

                    norms[i] = sum;
                });


                try
                {
                    Parallel.For(0, Gamma.Length, ParallelOptions, k =>
                    {
                        var gammak = Gamma[k];

                        for (var i = 0; i < gammak.Length; i++)
                        {
                            gammak[i] = norms[i] != 0 ? gammak[i] / norms[i] : 0;
                        }

                        if (weights != null)
                        {
                            for (var i = 0; i < weights.Length; i++)
                            {
                                gammak[i] *= weights[i];
                            }
                        }

                        var sum = gammak.Sum();

                        if (double.IsInfinity(sum) || double.IsNaN(sum))
                        {
                            sum = 0;
                        }


                        // 3. Maximization: Re-estimate the distribution parameters
                        //    using the previously computed responsibilities

                        pi[k] = sum;

                        if (sum == 0)
                        {
                            return;
                        }

                        for (var i = 0; i < gammak.Length; i++)
                        {
                            gammak[i] /= sum;
                        }

                        pdf[k].Fit(observations, gammak, InnerOptions);
                    });
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException is NonPositiveDefiniteMatrixException)
                    {
                        throw ex.InnerException;
                    }

                    throw;
                }

                var sumPi = pi.Sum();
                for (var i = 0; i < pi.Length; i++)
                {
                    pi[i] /= sumPi;
                }

                if (componentOptions != null && componentOptions.Postprocessing != null)
                {
                    componentOptions.Postprocessing(pdf, pi);
                }

                // 4. Evaluate the log-likelihood and check for convergence
                Convergence.NewValue = LogLikelihood(pi, pdf, observations, weights, weightSum, ParallelOptions);
            } while (!Convergence.HasConverged);


            var newLikelihood = Convergence.NewValue;
            if (double.IsNaN(newLikelihood) || double.IsInfinity(newLikelihood))
            {
                throw new ConvergenceException("Fitting did not converge.");
            }


            // Become the newly fitted distribution.
            for (var i = 0; i < pi.Length; i++)
            {
                Coefficients[i] = pi[i];
            }

            for (var i = 0; i < pdf.Length; i++)
            {
                Distributions[i] = pdf[i];
            }

            return newLikelihood;
        }

        /// <summary>
        ///     Computes the log-likelihood of the distribution
        ///     for a given set of observations.
        /// </summary>
        public static double LogLikelihood(double[] pi, IDistribution<TObservation>[] pdf, TObservation[] observations)
        {
            return LogLikelihood(pi, pdf, observations, null, 0, new ParallelOptions());
        }

        /// <summary>
        ///     Computes the log-likelihood of the distribution
        ///     for a given set of observations.
        /// </summary>
        public static double LogLikelihood(double[] pi, IDistribution<TObservation>[] pdf,
            TObservation[] observations, ParallelOptions parallelOptions)
        {
            return LogLikelihood(pi, pdf, observations, null, 0, parallelOptions);
        }

        /// <summary>
        ///     Computes the log-likelihood of the distribution
        ///     for a given set of observations.
        /// </summary>
        public static double LogLikelihood(double[] pi, IDistribution<TObservation>[] pdf,
            TObservation[] observations, double[] weights, double weightSum)
        {
            return LogLikelihood(pi, pdf, observations, weights, weightSum, new ParallelOptions());
        }

        /// <summary>
        ///     Computes the log-likelihood of the distribution
        ///     for a given set of observations.
        /// </summary>
        public static double LogLikelihood(double[] pi, IDistribution<TObservation>[] pdf,
            TObservation[] observations, double[] weights, double weightSum, ParallelOptions parallelOptions)
        {
            var logLikelihood = 0.0;

#if NET35
            for (int i = 0; i < observations.Length; i++)
            {
                var x = observations[i];
                double w = 1.0;

                if (weights != null)
                {
                    w = weights[i];

                    if (w == 0)
                        continue;
                }

                double sum = 0.0;
                for (int k = 0; k < pi.Length; k++)
                    sum += pi[k] * pdf[k].ProbabilityFunction(x);

                if (sum > 0)
                    logLikelihood += Math.Log(sum) * w;
            }
#else
            var syncObj = new object();

            Parallel.For(0, observations.Length, parallelOptions,
                () => 0.0,
                (i, status, partial) =>
                {
                    var x = observations[i];
                    var w = 1.0;

                    if (weights != null)
                    {
                        w = weights[i];

                        if (w == 0)
                        {
                            return partial;
                        }
                    }

                    var sum = 0.0;
                    for (var k = 0; k < pi.Length; k++)
                    {
                        sum += pi[k] * pdf[k].ProbabilityFunction(x);
                    }

                    if (sum > 0)
                    {
                        return partial + System.Math.Log(sum) * w;
                    }

                    return partial;
                },
                partial =>
                {
                    lock (syncObj)
                    {
                        logLikelihood += partial;
                    }
                }
            );
#endif

            Debug.Assert(!double.IsNaN(logLikelihood));

            if (weights != null)
            {
                Debug.Assert(weightSum != 0);

                return logLikelihood / weightSum;
            }

            return logLikelihood;
        }
    }
}