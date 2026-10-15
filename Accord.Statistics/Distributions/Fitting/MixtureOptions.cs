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

namespace Accord.Statistics.Distributions.Fitting
{
    /// <summary>
    ///     Estimation options for <see cref="Univariate.Mixture{T}">univariate</see>
    ///     and <see cref="Multivariate.MultivariateMixture{T}">multivariate</see>
    ///     <see cref="IMixture{T}">mixture distributions</see>.
    /// </summary>
    [Serializable]
    public class MixtureOptions : IFittingOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MixtureOptions" /> class.
        /// </summary>
        public MixtureOptions()
        {
            Threshold = 1e-3;
            ParallelOptions = new ParallelOptions();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MixtureOptions" /> class.
        /// </summary>
        /// <param name="threshold">
        ///     The convergence criterion for the
        ///     Expectation-Maximization algorithm. Default is 1e-3.
        /// </param>
        public MixtureOptions(double threshold)
            : this()
        {
            Threshold = threshold;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MixtureOptions" /> class.
        /// </summary>
        /// <param name="threshold">
        ///     The convergence criterion for the
        ///     Expectation-Maximization algorithm. Default is 1e-3.
        /// </param>
        /// <param name="innerOptions">
        ///     The fitting options for the inner
        ///     component distributions of the mixture density.
        /// </param>
        public MixtureOptions(double threshold, IFittingOptions innerOptions)
            : this(threshold)
        {
            InnerOptions = innerOptions;
        }

        /// <summary>
        ///     Gets or sets the convergence criterion for the
        ///     Expectation-Maximization algorithm. Default is 1e-3.
        /// </summary>
        /// <value>The convergence threshold.</value>
        public double Threshold { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     to be performed by the Expectation-Maximization
        ///     algorithm. Default is zero (iterate until convergence).
        /// </summary>
        public int MaxIterations
        {
#pragma warning disable 612, 618
            get { return Iterations; }
            set { Iterations = value; }
#pragma warning restore 612, 618
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     to be performed by the Expectation-Maximization
        ///     algorithm. Default is zero (iterate until convergence).
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations { get; set; }

        /// <summary>
        ///     Gets or sets the parallelization options to be used when fitting.
        /// </summary>
        /// <value>The parallel options.</value>
        public ParallelOptions ParallelOptions { get; set; }

        /// <summary>
        ///     Gets or sets the fitting options for the inner
        ///     component distributions of the mixture density.
        /// </summary>
        /// <value>The fitting options for inner distributions.</value>
        public IFittingOptions InnerOptions { get; set; }

        /// <summary>
        ///     Gets or sets whether to make computations using the log
        ///     -domain. This might improve accuracy on large datasets.
        /// </summary>
        public bool Logarithm { get; set; }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}