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
    public class MetropolisHasting<TObservation, TProposalDistribution, TTargetDistribution>
        : MetropolisHasting<TObservation, TProposalDistribution>
        where TProposalDistribution : ISampleableDistribution<TObservation[]>
        where TTargetDistribution : IMultivariateDistribution<TObservation[]>
    {
        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="MetropolisHasting{TObservation, TProposalDistribution, TTargetDistribution}" /> class.
        /// </summary>
        /// <param name="target">The target distribution whose samples should be generated.</param>
        /// <param name="proposal">The proposal distribution that is used to generate new parameter samples to be explored.</param>
        public MetropolisHasting(TTargetDistribution target, TProposalDistribution proposal)
        {
            this.Target = target;
            Initialize(target.Dimension, target.LogProbabilityFunction, proposal);
        }

        /// <summary>
        ///     Gets the target distribution whose samples must be generated.
        /// </summary>
        public TTargetDistribution Target { get; }
    }
}