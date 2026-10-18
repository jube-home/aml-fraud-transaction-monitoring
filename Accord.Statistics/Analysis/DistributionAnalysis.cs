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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Comparers;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Reflection;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Distribution fitness analysis.
    /// </summary>
    /// <remarks>
    ///     The distribution analysis class can be used to perform a battery
    ///     of distribution fitting tests in order to check from which distribution
    ///     a sample is more likely to have come from.
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\DistributionAnalysisTest.cs" region="doc_learn" />
    /// </example>
    /// <seealso cref="KolmogorovSmirnovTest" />
    /// <seealso cref="ChiSquareTest" />
    /// <seealso cref="AndersonDarlingTest" />
    [Serializable]
    public class DistributionAnalysis
    {
        private double[] data;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributionAnalysis" /> class.
        /// </summary>
        /// <param name="observations">The observations to be fitted against candidate distributions.</param>
        [Obsolete("Please use the default parameterless constructor instead.")]
        public DistributionAnalysis(double[] observations)
            : this()
        {
            data = observations;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributionAnalysis" /> class.
        /// </summary>
        public DistributionAnalysis()
        {
            Distributions = new List<IFittableDistribution<double>>
            {
                new NormalDistribution(),
                new UniformContinuousDistribution(),
                new GammaDistribution(),
                new GumbelDistribution(),
                new PoissonDistribution()
            };

            Options = new Dictionary<IFittableDistribution<double>, IFittingOptions>();
        }

        /// <summary>
        ///     Gets the tested distribution names.
        /// </summary>
        /// <value>
        ///     The distribution names.
        /// </value>
        public string[] DistributionNames { get; private set; }

        /// <summary>
        ///     Gets the estimated distributions.
        /// </summary>
        /// <value>
        ///     The estimated distributions.
        /// </value>
        public List<IFittableDistribution<double>> Distributions { get; set; }

        /// <summary>
        ///     Gets or sets a mapping of fitting options that should be
        ///     used when attempting to estimate each of the distributions
        ///     in <see cref="Distributions" />.
        /// </summary>
        public Dictionary<IFittableDistribution<double>, IFittingOptions> Options { get; set; }

        /// <summary>
        ///     Gets the <see cref="KolmogorovSmirnovTest">Kolmogorov-Smirnov tests</see>
        ///     performed against each of the candidate distributions.
        /// </summary>
        public KolmogorovSmirnovTest[] KolmogorovSmirnov { get; private set; }

        /// <summary>
        ///     Gets the <see cref="ChiSquareTest">Chi-Square tests</see>
        ///     performed against each of the candidate distributions.
        /// </summary>
        public ChiSquareTest[] ChiSquare { get; private set; }

        /// <summary>
        ///     Gets the <see cref="AndersonDarlingTest">Anderson-Darling tests</see>
        ///     performed against each of the candidate distributions.
        /// </summary>
        public AndersonDarlingTest[] AndersonDarling { get; private set; }

        /// <summary>
        ///     Gets the rank of each distribution according to the Kolmogorov-Smirnov
        ///     test statistic. A value of 0 means the distribution is the most likely.
        /// </summary>
        public int[] KolmogorovSmirnovRank { get; private set; }

        /// <summary>
        ///     Gets the rank of each distribution according to the Chi-Square
        ///     test statistic. A value of 0 means the distribution is the most likely.
        /// </summary>
        public int[] ChiSquareRank { get; private set; }

        /// <summary>
        ///     Gets the rank of each distribution according to the Anderson-Darling
        ///     test statistic. A value of 0 means the distribution is the most likely.
        /// </summary>
        public int[] AndersonDarlingRank { get; private set; }

        /// <summary>
        ///     Gets the goodness of fit for each candidate distribution.
        /// </summary>
        public GoodnessOfFitCollection GoodnessOfFit { get; private set; }

        /// <summary>
        ///     Obsolete. Please use the <see cref="Learn(double[], double[])" /> method instead.
        /// </summary>
        [Obsolete("Please use the Learn(x) method instead.")]
        public void Compute()
        {
            compute(data, null);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public GoodnessOfFitCollection Learn(double[] x, double[] weights = null)
        {
            compute(x, weights);
            return GoodnessOfFit;
        }

        private void compute(double[] data, double[] weights)
        {
            var fail = new bool[Distributions.Count];

            // Step 1. Fit all candidate distributions to the data.
            for (var i = 0; i < Distributions.Count; i++)
            {
                var distribution = Distributions[i];

                Options.TryGetValue(distribution, out var options);

                try
                {
                    distribution.Fit(data, weights, options);
                }
                catch
                {
                    // TODO: Maybe revisit the decision to swallow exceptions here.
                    fail[i] = true;
                }
            }

            // Step 2. Use statistical tests to see how well each
            //         distribution was able to model the data.

            KolmogorovSmirnov = new KolmogorovSmirnovTest[Distributions.Count];
            ChiSquare = new ChiSquareTest[Distributions.Count];
            AndersonDarling = new AndersonDarlingTest[Distributions.Count];
            DistributionNames = new string[Distributions.Count];

            var ks = new double[Distributions.Count];
            var cs = new double[Distributions.Count];
            var ad = new double[Distributions.Count];

            var measures = new List<GoodnessOfFit>();
            for (var i = 0; i < Distributions.Count; i++)
            {
                ks[i] = double.NegativeInfinity;
                cs[i] = double.NegativeInfinity;
                ad[i] = double.NegativeInfinity;

                var d = Distributions[i] as IUnivariateDistribution<double>;

                if (d == null)
                {
                    continue;
                }

                DistributionNames[i] = GetName(d.GetType());

                if (fail[i])
                {
                    continue;
                }

                var ms = 5000;
                var index = i;

                run(() =>
                {
                    KolmogorovSmirnov[index] = new KolmogorovSmirnovTest(data, d);
                    ks[index] = -KolmogorovSmirnov[index].Statistic;
                }, ms);

                run(() =>
                {
                    ChiSquare[index] = new ChiSquareTest(data, d);
                    cs[index] = -ChiSquare[index].Statistic;
                }, ms);

                run(() =>
                {
                    AndersonDarling[index] = new AndersonDarlingTest(data, d);
                    ad[index] = AndersonDarling[index].Statistic;
                }, ms);

                if (double.IsNaN(ks[i]))
                {
                    ks[i] = double.NegativeInfinity;
                }

                if (double.IsNaN(cs[i]))
                {
                    cs[i] = double.NegativeInfinity;
                }

                if (double.IsNaN(ad[i]))
                {
                    ad[i] = double.NegativeInfinity;
                }

                measures.Add(new GoodnessOfFit(this, i));
            }

            KolmogorovSmirnovRank = getRank(ks);
            ChiSquareRank = getRank(cs);
            AndersonDarlingRank = getRank(ad);

            measures.Sort();

            GoodnessOfFit = new GoodnessOfFitCollection(measures);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void run(Action a, int timeoutMilliseconds)
        {
#if NET35
            try
            {
                a();
            }
            catch
            {
            }
#else
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    a();
                }
                catch
                {
                }
            });

            task.Wait(timeoutMilliseconds);
#endif
        }

        private int[] getRank(double[] ks)
        {
            var idx = Vector.Range(0, Distributions.Count);
            Array.Sort(ks, idx, new GeneralComparer(ComparerDirection.Descending));

            var rank = new int[idx.Length];
            for (var i = 0; i < rank.Length; i++)
            {
                rank[i] = Array.IndexOf(idx, i);
            }

            return rank;
        }

        /// <summary>
        ///     Gets all univariate distributions (types implementing
        ///     <see cref="IUnivariateDistribution" />) loaded in the
        ///     current domain.
        /// </summary>
        public static Type[] GetUnivariateDistributions()
        {
            return DistributionInfo.GetDistributionsInheritingFromBaseType(typeof(IUnivariateDistribution));
        }

        /// <summary>
        ///     Gets all multivariate distributions (types implementing
        ///     <see cref="IMultivariateDistribution" />) loaded in the
        ///     current domain.
        /// </summary>
        public static Type[] GetMultivariateDistributions()
        {
            return DistributionInfo.GetDistributionsInheritingFromBaseType(typeof(IMultivariateDistribution));
        }

        /// <summary>
        ///     Gets a distribution's name in a human-readable form.
        /// </summary>
        /// <param name="distribution">The distribution whose name must be obtained.</param>
        public static string GetName(Type distribution)
        {
            return distribution.Name.Replace("Distribution", "");
        }

        /// <summary>
        ///     Gets the index of the first distribution with the given name.
        /// </summary>
        public IFittableDistribution<double> GetFirstIndex(string name)
        {
            return Distributions.Find(d => d.GetType().Name == name);
        }
    }
}