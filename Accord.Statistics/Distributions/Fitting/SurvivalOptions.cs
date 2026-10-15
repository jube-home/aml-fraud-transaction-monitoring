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

using Accord.Math;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Distributions.Fitting
{
    /// <summary>
    ///     Options for Survival distributions.
    /// </summary>
    public class SurvivalOptions : IFittingOptions
    {
        /// <summary>
        ///     Default survival estimation method. Returns <see cref="SurvivalEstimator.FlemingHarrington" />.
        /// </summary>
        public const SurvivalEstimator DefaultSurvival = SurvivalEstimator.FlemingHarrington;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SurvivalOptions" /> class.
        /// </summary>
        public SurvivalOptions()
        {
        }

        /// <summary>
        ///     Gets or sets the values for
        ///     the right-censoring variable.
        /// </summary>
        public SurvivalOutcome[] Outcome { get; set; }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }

    /// <summary>
    ///     Options for Empirical Hazard distributions.
    /// </summary>
    public class EmpiricalHazardOptions : SurvivalOptions
    {
        /// <summary>
        ///     Default hazard estimator. Returns <see cref="HazardEstimator.BreslowNelsonAalen" />.
        /// </summary>
        public const HazardEstimator DefaultEstimator = HazardEstimator.BreslowNelsonAalen;

        /// <summary>
        ///     Default tie handling method. Returns <see cref="HazardTiesMethod.Efron" />.
        /// </summary>
        public const HazardTiesMethod DefaultTies = HazardTiesMethod.Efron;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EmpiricalHazardOptions" /> class.
        /// </summary>
        public EmpiricalHazardOptions()
        {
            Estimator = DefaultEstimator;
            Ties = DefaultTies;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EmpiricalHazardOptions" /> class.
        /// </summary>
        public EmpiricalHazardOptions(HazardEstimator estimator, SurvivalOutcome[] output)
        {
            Estimator = estimator;
            Outcome = output;
            Ties = DefaultTies;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EmpiricalHazardOptions" /> class.
        /// </summary>
        public EmpiricalHazardOptions(HazardEstimator estimator, int[] output)
        {
            Estimator = estimator;
            Outcome = Matrix.To<SurvivalOutcome[]>(output);
            Ties = HazardTiesMethod.Breslow;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EmpiricalHazardOptions" /> class.
        /// </summary>
        public EmpiricalHazardOptions(HazardEstimator estimator, HazardTiesMethod ties, SurvivalOutcome[] outcome)
        {
            Estimator = estimator;
            Outcome = outcome;
            Ties = ties;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EmpiricalHazardOptions" /> class.
        /// </summary>
        public EmpiricalHazardOptions(HazardEstimator estimator, HazardTiesMethod ties, int[] output)
        {
            Estimator = estimator;
            Outcome = Matrix.To<SurvivalOutcome[]>(output);
            Ties = ties;
        }

        /// <summary>
        ///     Gets or sets the estimator to be used. Default is <see cref="HazardEstimator.BreslowNelsonAalen" />.
        /// </summary>
        public HazardEstimator Estimator { get; set; }

        /// <summary>
        ///     Gets or sets the tie handling method to be used. Default is <see cref="HazardTiesMethod.Efron" />.
        /// </summary>
        public HazardTiesMethod Ties { get; set; }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}